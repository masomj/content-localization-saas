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
        return Enum.TryParse<AppRole>(raw, true, out var parsed) ? parsed : AppRole.Viewer;
    }

    public static bool TryResolveFromClaims(ClaimsPrincipal principal, out AppRole role)
    {
        role = AppRole.Viewer;
        if (principal?.Identity?.IsAuthenticated != true) return false;

        var raw = principal.FindFirst("app_role")?.Value
                  ?? principal.FindFirst(ClaimTypes.Role)?.Value
                  ?? principal.FindFirst("role")?.Value;

        if (string.IsNullOrWhiteSpace(raw)) return false;
        if (!Enum.TryParse<AppRole>(raw, true, out role)) return false;
        return true;
    }
}
