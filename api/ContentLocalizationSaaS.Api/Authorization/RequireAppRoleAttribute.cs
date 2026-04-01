using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace ContentLocalizationSaaS.Api.Authorization;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequireAppRoleAttribute : TypeFilterAttribute
{
    public RequireAppRoleAttribute(AppRole minimumRole)
        : base(typeof(RequireAppRoleFilter))
    {
        Arguments = new object[] { minimumRole };
    }
}

public sealed class RequireAppRoleFilter(AppRole minimumRole, AppDbContext db, IOptions<AuthOptions> options) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var legacyEnabled = options?.Value.LegacyHeaderAuthEnabled ?? true;
        var isAuthenticated = context.HttpContext.User?.Identity?.IsAuthenticated == true;

        AppRole current;
        if (AppRoleResolver.TryResolveFromClaims(context.HttpContext.User, out var claimsRole))
        {
            current = claimsRole;
        }
        else if (TryResolveWorkspaceRole(context.HttpContext, db, out var wsRole))
        {
            current = await wsRole;
        }
        else if (legacyEnabled)
        {
            current = AppRoleResolver.ResolveFromHeader(context.HttpContext);
        }
        else if (isAuthenticated)
        {
            // Authenticated via OIDC but no role claim or workspace membership found.
            // Default to Editor so authenticated users can use the app while role
            // assignment in Keycloak is being configured.
            current = AppRole.Editor;
        }
        else
        {
            current = AppRole.Viewer;
        }

        if (!AppRoleResolver.HasAtLeastRole(current, minimumRole))
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden"
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }

    private static bool TryResolveWorkspaceRole(HttpContext context, AppDbContext db, out Task<AppRole> roleTask)
    {
        roleTask = Task.FromResult(AppRole.Viewer);

        var principal = context.User;
        if (principal?.Identity?.IsAuthenticated != true) return false;

        var email = principal.FindFirst(ClaimTypes.Email)?.Value
                    ?? principal.FindFirst("email")?.Value;
        if (string.IsNullOrWhiteSpace(email)) return false;

        var workspaceIdRaw = context.Request.Headers["X-Workspace-Id"].ToString();
        if (!Guid.TryParse(workspaceIdRaw, out var workspaceId)) return false;

        roleTask = ResolveFromMembership(db, email.Trim().ToLowerInvariant(), workspaceId);
        return true;
    }

    private static async Task<AppRole> ResolveFromMembership(AppDbContext db, string email, Guid workspaceId)
    {
        var membership = await db.WorkspaceMemberships
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.Email == email && m.IsActive);

        if (membership is null) return AppRole.Viewer;

        return Enum.TryParse<AppRole>(membership.Role, true, out var role) ? role : AppRole.Viewer;
    }
}
