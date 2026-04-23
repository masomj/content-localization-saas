using ContentLocalizationSaaS.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ContentLocalizationSaaS.Api.Middleware;

/// <summary>
/// Action filter that checks entitlement limits before allowing resource creation.
/// Usage: [EntitlementGuard(EntitlementCheck.Project)] on controller actions.
/// </summary>
public enum EntitlementCheck
{
    User,
    Project,
    FigmaBoard,
    FrameOrComponent
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class EntitlementGuardAttribute : Attribute, IAsyncActionFilter
{
    private readonly EntitlementCheck _check;

    public EntitlementGuardAttribute(EntitlementCheck check)
    {
        _check = check;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var entitlements = context.HttpContext.RequestServices.GetRequiredService<IEntitlementService>();

        // Extract workspaceId from route
        if (!context.ActionArguments.TryGetValue("workspaceId", out var wsIdObj)
            && !context.RouteData.Values.TryGetValue("workspaceId", out wsIdObj))
        {
            // Try to get from query string
            var queryWsId = context.HttpContext.Request.Query["workspaceId"].FirstOrDefault();
            if (queryWsId is not null && Guid.TryParse(queryWsId, out var parsedId))
                wsIdObj = parsedId;
        }

        if (wsIdObj is null || !Guid.TryParse(wsIdObj.ToString(), out var workspaceId))
        {
            context.Result = new BadRequestObjectResult(new { error = "workspaceId is required for entitlement check" });
            return;
        }

        var allowed = _check switch
        {
            EntitlementCheck.User => await entitlements.CanAddUserAsync(workspaceId),
            EntitlementCheck.Project => await entitlements.CanCreateProjectAsync(workspaceId),
            EntitlementCheck.FigmaBoard => await entitlements.CanAddFigmaBoardAsync(workspaceId),
            EntitlementCheck.FrameOrComponent => await entitlements.CanAddFrameOrComponentAsync(workspaceId),
            _ => true
        };

        if (!allowed)
        {
            var snapshot = await entitlements.GetEntitlementsAsync(workspaceId);
            context.Result = new ObjectResult(new
            {
                error = "plan_limit_reached",
                message = $"Your {snapshot.CurrentTier} plan limit has been reached for {_check}. Upgrade to Pro to continue.",
                currentTier = snapshot.CurrentTier.ToString(),
                check = _check.ToString(),
                upgradeRequired = true
            })
            {
                StatusCode = 403
            };
            return;
        }

        await next();
    }
}
