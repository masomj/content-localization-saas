using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace ContentLocalizationSaaS.Api.Authorization;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequireAppRoleAttribute(AppRole minimumRole) : Attribute, IAsyncAuthorizationFilter
{
    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var sp = context.HttpContext.RequestServices;
        var options = sp.GetService(typeof(IOptions<AuthOptions>)) as IOptions<AuthOptions>;
        var legacyEnabled = options?.Value.LegacyHeaderAuthEnabled ?? true;

        AppRole current;
        if (AppRoleResolver.TryResolveFromClaims(context.HttpContext.User, out var claimsRole))
        {
            current = claimsRole;
        }
        else if (legacyEnabled)
        {
            current = AppRoleResolver.ResolveFromHeader(context.HttpContext);
        }
        else
        {
            current = AppRole.Viewer;
        }

        if (current < minimumRole)
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

        return Task.CompletedTask;
    }
}
