using ContentLocalizationSaaS.Api.Middleware;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/observability")]
public sealed class ObservabilityController(AppDbContext db) : ControllerBase
{
    [HttpGet("metrics")]
    public async Task<IActionResult> Metrics(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var since24h = now.AddHours(-24);

        var pendingWebhooks = await db.WebhookDeliveryLogs.CountAsync(x => x.Status == "pending", cancellationToken);
        var deadLetterWebhooks = await db.WebhookDeliveryLogs.CountAsync(x => x.Status == "dead_letter", cancellationToken);
        var failedWebhooks24h = await db.WebhookDeliveryLogs.CountAsync(x => x.Status == "dead_letter" && x.CreatedUtc >= since24h, cancellationToken);
        var unreadNotifications = await db.UserNotifications.CountAsync(x => !x.IsRead, cancellationToken);

        var correlationId = HttpContext.Items[ObservabilityMiddleware.CorrelationHeader]?.ToString()
                            ?? HttpContext.Response.Headers[ObservabilityMiddleware.CorrelationHeader].ToString();

        return Ok(new
        {
            timestampUtc = now,
            correlationId,
            metrics = new
            {
                pendingWebhooks,
                deadLetterWebhooks,
                failedWebhooks24h,
                unreadNotifications
            }
        });
    }
}
