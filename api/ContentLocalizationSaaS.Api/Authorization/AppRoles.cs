using System.Security.Claims;
using System.Text.Json;

namespace ContentLocalizationSaaS.Api.Authorization;

public enum AppRole
{
    Viewer = 0,
    Editor = 1,
    Reviewer = 2,
    Admin = 3
}

public static class AppRoleResolver
{
    public const string HeaderName = "X-User-Role";

    public static AppRole ResolveFromHeader(HttpContext context)
    {
        var raw = context.Request.Headers[HeaderName].ToString();
        return TryParseRole(raw, out var parsed) ? parsed : AppRole.Viewer;
    }

    public static bool TryResolveFromClaims(ClaimsPrincipal principal, out AppRole role)
    {
        role = AppRole.Viewer;
        if (principal?.Identity?.IsAuthenticated != true) return false;

        var candidateRoles = new List<string>();

        candidateRoles.AddRange(principal.FindAll("app_role").Select(c => c.Value));
        candidateRoles.AddRange(principal.FindAll(ClaimTypes.Role).Select(c => c.Value));
        candidateRoles.AddRange(principal.FindAll("role").Select(c => c.Value));
        candidateRoles.AddRange(principal.FindAll("roles").Select(c => c.Value));

        foreach (var claim in principal.FindAll("realm_access"))
        {
            candidateRoles.AddRange(ParseKeycloakRoleArray(claim.Value, "roles"));
        }

        foreach (var claim in principal.FindAll("resource_access"))
        {
            candidateRoles.AddRange(ParseKeycloakClientRoles(claim.Value));
        }

        var highest = AppRole.Viewer;
        var found = false;
        foreach (var candidate in candidateRoles)
        {
            if (!TryParseRole(candidate, out var parsed))
            {
                continue;
            }

            if (Rank(parsed) > Rank(highest))
            {
                highest = parsed;
            }

            found = true;
        }

        role = highest;
        return found;
    }

    public static bool HasAtLeastRole(AppRole current, AppRole required)
    {
        if (current == AppRole.Admin) return true;
        return Rank(current) >= Rank(required);
    }

    private static int Rank(AppRole role) => role switch
    {
        AppRole.Admin => 300,
        AppRole.Editor => 200,
        AppRole.Reviewer => 100,
        AppRole.Viewer => 0,
        _ => 0
    };

    private static bool TryParseRole(string? raw, out AppRole role)
    {
        role = AppRole.Viewer;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        if (string.Equals(raw, "Reader", StringComparison.OrdinalIgnoreCase))
        {
            role = AppRole.Viewer;
            return true;
        }

        if (raw.StartsWith("intercopy-", StringComparison.OrdinalIgnoreCase))
        {
            raw = raw["intercopy-".Length..];
        }

        return Enum.TryParse(raw, true, out role);
    }

    private static IEnumerable<string> ParseKeycloakRoleArray(string json, string propertyName)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty(propertyName, out var rolesElement) || rolesElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return rolesElement
                .EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString() ?? string.Empty)
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToArray();
        }
        catch
        {
            return [];
        }
    }

    private static IEnumerable<string> ParseKeycloakClientRoles(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return [];
            }

            var values = new List<string>();
            foreach (var client in doc.RootElement.EnumerateObject())
            {
                if (!client.Value.TryGetProperty("roles", out var rolesElement) || rolesElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                values.AddRange(rolesElement
                    .EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString() ?? string.Empty)
                    .Where(e => !string.IsNullOrWhiteSpace(e)));
            }

            return values;
        }
        catch
        {
            return [];
        }
    }
}
