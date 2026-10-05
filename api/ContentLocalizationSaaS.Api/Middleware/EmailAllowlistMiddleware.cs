using System.Security.Claims;
using ContentLocalizationSaaS.Application;

namespace ContentLocalizationSaaS.Api.Middleware;

/// <summary>
/// Blocks authenticated users whose email isn't on the configured allowlist (AccessAllowlist:Emails).
/// Does nothing when the allowlist is empty, so production behaviour is unchanged.
/// Requests without a signed-in user (health checks, API-token CI exports, webhooks) pass through
/// because they're authorised separately.
/// </summary>
public sealed class EmailAllowlistMiddleware(RequestDelegate next, EmailAllowlist allowlist, ILogger<EmailAllowlistMiddleware> logger)
{
    public const string ErrorCode = "email_not_allowlisted";

    public async Task InvokeAsync(HttpContext context)
    {
        var user = context.User;
        if (!allowlist.IsEnabled || user?.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var email = user.FindFirst("email")?.Value ?? user.FindFirst(ClaimTypes.Email)?.Value;
        if (allowlist.IsAllowed(email))
        {
            await next(context);
            return;
        }

        logger.LogWarning("Blocked request from non-allowlisted user {Email} to {Path}", email ?? "(no email claim)", context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new
        {
            error = ErrorCode,
            title = "This environment is invite-only",
            status = StatusCodes.Status403Forbidden
        });
    }
}
