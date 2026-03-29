using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace ContentLocalizationSaaS.Api.Controllers;

// ---------------------------------------------------------------
// Device Authorization Flow (RFC 8628) — proxy endpoints.
// The Figma plugin cannot call Keycloak directly (CORS restrictions
// inside the iframe sandbox), so the API proxies the requests.
// ---------------------------------------------------------------

public sealed record DeviceAuthStartResponse(
    string UserCode,
    string VerificationUri,
    string? VerificationUriComplete,
    string DeviceCode,
    int ExpiresIn,
    int Interval);

public sealed record DeviceAuthPollRequest(string DeviceCode);

public sealed record DeviceAuthPollResponse(
    string Status,
    string? AccessToken = null,
    string? RefreshToken = null,
    int? ExpiresIn = null,
    DeviceAuthUser? User = null);

public sealed record DeviceAuthUser(string Sub, string? Email, string? Name);

public sealed record DeviceAuthRefreshRequest(string RefreshToken);

public sealed record DeviceAuthRefreshResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn);

[ApiController]
[Route("api/device-auth")]
[Microsoft.AspNetCore.Cors.EnableCors("PluginCors")]
public sealed class DeviceAuthController(IHttpClientFactory httpClientFactory, IConfiguration configuration) : ControllerBase
{
    private const string KeycloakClientId = "intercopy-device";

    private string GetKeycloakBaseUrl()
    {
        // Auth__Oidc__Issuer = http://localhost:8080/realms/intercopy
        var issuer = configuration["Auth:Oidc:Issuer"] ?? "http://localhost:8080/realms/intercopy";
        return issuer.TrimEnd('/');
    }

    /// <summary>
    /// Start the device authorization flow.
    /// Calls Keycloak's device authorization endpoint and returns the user code + verification URI.
    /// </summary>
    [HttpPost("start")]
    public async Task<IActionResult> Start(CancellationToken cancellationToken)
    {
        var keycloak = GetKeycloakBaseUrl();
        var client = httpClientFactory.CreateClient("Keycloak");

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = KeycloakClientId,
        });

        var response = await client.PostAsync(
            $"{keycloak}/protocol/openid-connect/auth/device",
            form,
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode,
                new { error = "keycloak_device_auth_failed", details = body });
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var result = new DeviceAuthStartResponse(
            UserCode: root.GetProperty("user_code").GetString()!,
            VerificationUri: root.GetProperty("verification_uri").GetString()!,
            VerificationUriComplete: root.TryGetProperty("verification_uri_complete", out var vuriComplete)
                ? vuriComplete.GetString()
                : null,
            DeviceCode: root.GetProperty("device_code").GetString()!,
            ExpiresIn: root.GetProperty("expires_in").GetInt32(),
            Interval: root.TryGetProperty("interval", out var interval)
                ? interval.GetInt32()
                : 5);

        return Ok(result);
    }

    /// <summary>
    /// Poll for token completion.
    /// The plugin calls this every N seconds until the user completes browser auth.
    /// </summary>
    [HttpPost("poll")]
    public async Task<IActionResult> Poll([FromBody] DeviceAuthPollRequest request, CancellationToken cancellationToken)
    {
        var keycloak = GetKeycloakBaseUrl();
        var client = httpClientFactory.CreateClient("Keycloak");

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = KeycloakClientId,
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
            ["device_code"] = request.DeviceCode,
        });

        var response = await client.PostAsync(
            $"{keycloak}/protocol/openid-connect/token",
            form,
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            using var errorDoc = JsonDocument.Parse(body);
            var errorRoot = errorDoc.RootElement;
            var error = errorRoot.TryGetProperty("error", out var errProp)
                ? errProp.GetString()
                : "unknown";

            // Standard RFC 8628 pending / slow_down responses
            if (error == "authorization_pending" || error == "slow_down")
            {
                return Ok(new DeviceAuthPollResponse(Status: "pending"));
            }

            if (error == "expired_token")
            {
                return Ok(new DeviceAuthPollResponse(Status: "expired"));
            }

            // Any other error
            return StatusCode((int)response.StatusCode,
                new { error = "keycloak_token_error", details = body });
        }

        // Success — extract tokens and user info from the access token JWT
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var accessToken = root.GetProperty("access_token").GetString()!;
        var refreshToken = root.TryGetProperty("refresh_token", out var rtProp)
            ? rtProp.GetString()
            : null;
        var expiresIn = root.GetProperty("expires_in").GetInt32();

        // Decode JWT payload to extract user info
        var user = DecodeJwtUser(accessToken);

        return Ok(new DeviceAuthPollResponse(
            Status: "complete",
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresIn: expiresIn,
            User: user));
    }

    /// <summary>
    /// Refresh an expired access token using a refresh token.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] DeviceAuthRefreshRequest request, CancellationToken cancellationToken)
    {
        var keycloak = GetKeycloakBaseUrl();
        var client = httpClientFactory.CreateClient("Keycloak");

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = KeycloakClientId,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = request.RefreshToken,
        });

        var response = await client.PostAsync(
            $"{keycloak}/protocol/openid-connect/token",
            form,
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode,
                new { error = "keycloak_refresh_failed", details = body });
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        return Ok(new DeviceAuthRefreshResponse(
            AccessToken: root.GetProperty("access_token").GetString()!,
            RefreshToken: root.TryGetProperty("refresh_token", out var rtProp)
                ? rtProp.GetString()!
                : request.RefreshToken,
            ExpiresIn: root.GetProperty("expires_in").GetInt32()));
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    private static DeviceAuthUser DecodeJwtUser(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2) return new DeviceAuthUser("unknown", null, null);

            var payload = parts[1];
            // Pad base64url to standard base64
            payload = payload.Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var bytes = Convert.FromBase64String(payload);
            using var doc = JsonDocument.Parse(bytes);
            var root = doc.RootElement;

            var sub = root.TryGetProperty("sub", out var subProp) ? subProp.GetString() : "unknown";
            var email = root.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
            var name = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
            if (name == null)
            {
                var given = root.TryGetProperty("given_name", out var gn) ? gn.GetString() : null;
                var family = root.TryGetProperty("family_name", out var fn) ? fn.GetString() : null;
                if (given != null || family != null) name = $"{given} {family}".Trim();
            }

            return new DeviceAuthUser(sub ?? "unknown", email, name);
        }
        catch
        {
            return new DeviceAuthUser("unknown", null, null);
        }
    }
}
