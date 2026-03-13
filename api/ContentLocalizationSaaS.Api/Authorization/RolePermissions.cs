namespace ContentLocalizationSaaS.Api.Authorization;

public sealed record PermissionMatrix(bool CanRead, bool CanWrite, bool CanReview, bool CanAdmin);

public static class RolePermissions
{
    public static PermissionMatrix For(AppRole role) => role switch
    {
        AppRole.Admin => new(true, true, true, true),
        AppRole.Reviewer => new(true, false, true, false),
        AppRole.Editor => new(true, true, false, false),
        _ => new(true, false, false, false)
    };
}
