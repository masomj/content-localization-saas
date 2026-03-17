using System.Security.Claims;

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

        var raw = principal.FindFirst("app_role")?.Value
                  ?? principal.FindFirst(ClaimTypes.Role)?.Value
                  ?? principal.FindFirst("role")?.Value;

        return TryParseRole(raw, out role);
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

        return Enum.TryParse(raw, true, out role);
    }
}
