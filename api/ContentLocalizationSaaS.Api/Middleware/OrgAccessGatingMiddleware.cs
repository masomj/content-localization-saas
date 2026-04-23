using ContentLocalizationSaaS.Application.Abstractions;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Middleware;

/// <summary>
/// EP11-S5: Pro Org Access Gating.
/// When a workspace's subscription is cancelled/expired past grace period,
/// all write operations (POST/PUT/PATCH/DELETE) are blocked — workspace becomes read-only.
/// Does not apply to billing endpoints (so users can still upgrade/pay).
/// </summary>
public sealed class OrgAccessGatingMiddleware
{
    private readonly RequestDelegate _next;

    // Paths exempt from gating (billing/upgrade endpoints must remain accessible)
    private static readonly string[] ExemptPrefixes =
    [
        "/api/billing",
        "/api/plans",
        "/api/auth",
        "/api/workspaces/", // allow workspace-level GET + billing sub-routes
    ];

    public OrgAccessGatingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // Only gate write operations
        var method = context.Request.Method;
        if (method is "GET" or "HEAD" or "OPTIONS")
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;

        // Allow billing/auth/plans endpoints through
        if (IsExempt(path))
        {
            await _next(context);
            return;
        }

        // Extract workspace context — try header first, then route
        Guid? workspaceId = null;

        var wsHeader = context.Request.Headers["X-Workspace-Id"].FirstOrDefault();
        if (Guid.TryParse(wsHeader, out var headerWsId))
            workspaceId = headerWsId;

        // If we can't determine workspace, let the request through
        // (individual controllers handle their own auth)
        if (workspaceId is null)
        {
            await _next(context);
            return;
        }

        var entitlements = context.RequestServices.GetRequiredService<IEntitlementService>();
        var snapshot = await entitlements.GetEntitlementsAsync(workspaceId.Value);

        // Only gate Pro workspaces that have lapsed past grace
        if (snapshot.CurrentTier == PlanTier.Pro
            && snapshot.BillingStatus is SubscriptionStatus.Cancelled or SubscriptionStatus.Expired
            && !snapshot.IsGracePeriod)
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "org_suspended",
                message = "Your workspace is suspended due to lapsed billing. All write operations are blocked. Please update your payment to restore access.",
                billingStatus = snapshot.BillingStatus.ToString(),
                upgradeRequired = true,
                readOnly = true
            });
            return;
        }

        await _next(context);
    }

    private static bool IsExempt(string path)
    {
        foreach (var prefix in ExemptPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                // Special case: allow billing sub-routes under workspace paths
                if (prefix == "/api/workspaces/" && path.Contains("/billing", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (prefix != "/api/workspaces/")
                    return true;
            }
        }
        return false;
    }
}
