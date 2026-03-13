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

    public static AppRole Resolve(HttpContext context)
    {
        var raw = context.Request.Headers[HeaderName].ToString();
        return Enum.TryParse<AppRole>(raw, true, out var parsed) ? parsed : AppRole.Admin;
    }
}
