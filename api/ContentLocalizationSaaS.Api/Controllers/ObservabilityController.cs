using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Api.Middleware;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/observability")]
public sealed class ObservabilityController(AppDbContext db) : ControllerBase
{
    [HttpGet("status")]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> Status([FromQuery] int windowHours = 24, CancellationToken cancellationToken = default)
    {
        Response.Headers.CacheControl = "no-store";

        var clampedWindowHours = Math.Clamp(windowHours, 1, 168);
        Response.Headers["X-Window-Hours"] = clampedWindowHours.ToString();

        var now = DateTime.UtcNow;
        var since24h = now.AddHours(-clampedWindowHours);

        var deadLetterWebhooks = await db.WebhookDeliveryLogs.CountAsync(x => x.Status == "dead_letter", cancellationToken);
        var failedWebhooks24h = await db.WebhookDeliveryLogs.CountAsync(x => x.Status == "dead_letter" && x.CreatedUtc >= since24h, cancellationToken);
        var deliveredWebhooks24h = await db.WebhookDeliveryLogs.CountAsync(x => x.Status == "delivered" && x.CreatedUtc >= since24h, cancellationToken);
        var pendingOldestCreatedUtc = await db.WebhookDeliveryLogs
            .Where(x => x.Status == "pending")
            .OrderBy(x => x.CreatedUtc)
            .Select(x => (DateTime?)x.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var pendingOldestAgeMinutes = pendingOldestCreatedUtc.HasValue
            ? Math.Round((now - pendingOldestCreatedUtc.Value).TotalMinutes, 2)
            : 0;

        var totalTerminal24h = deliveredWebhooks24h + failedWebhooks24h;
        var webhookSuccessRate24h = totalTerminal24h == 0
            ? 1.0
            : Math.Round((double)deliveredWebhooks24h / totalTerminal24h, 4);

        var degraded = deadLetterWebhooks > 0 || pendingOldestAgeMinutes > 30 || webhookSuccessRate24h < 0.99;

        return Ok(new
        {
            timestampUtc = now,
            windowHours = clampedWindowHours,
            degraded,
            summary = new
            {
                deadLetterWebhooks,
                pendingOldestAgeMinutes,
                webhookSuccessRate24h
            }
        });
    }

    [HttpGet("metrics")]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> Metrics(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";

        var now = DateTime.UtcNow;
        var since24h = now.AddHours(-24);

        var pendingWebhooks = await db.WebhookDeliveryLogs.CountAsync(x => x.Status == "pending", cancellationToken);
        var deadLetterWebhooks = await db.WebhookDeliveryLogs.CountAsync(x => x.Status == "dead_letter", cancellationToken);
        var failedWebhooks24h = await db.WebhookDeliveryLogs.CountAsync(x => x.Status == "dead_letter" && x.CreatedUtc >= since24h, cancellationToken);
        var unreadNotifications = await db.UserNotifications.CountAsync(x => !x.IsRead, cancellationToken);
        var idempotencyReplayEvents = await db.IdempotencyRecords
            .Where(x => x.HitCount > 1)
            .CountAsync(cancellationToken);
        var idempotencyReplayHits = await db.IdempotencyRecords
            .Where(x => x.HitCount > 1)
            .SumAsync(x => (int?)x.HitCount - 1, cancellationToken) ?? 0;

        var deliveredWebhooks24h = await db.WebhookDeliveryLogs.CountAsync(x => x.Status == "delivered" && x.CreatedUtc >= since24h, cancellationToken);
        var webhookAttempts24h = await db.WebhookDeliveryLogs
            .Where(x => x.CreatedUtc >= since24h)
            .SumAsync(x => (int?)x.AttemptCount, cancellationToken) ?? 0;
        var pendingOldestCreatedUtc = await db.WebhookDeliveryLogs
            .Where(x => x.Status == "pending")
            .OrderBy(x => x.CreatedUtc)
            .Select(x => (DateTime?)x.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var pendingOldestAgeMinutes = pendingOldestCreatedUtc.HasValue
            ? Math.Round((now - pendingOldestCreatedUtc.Value).TotalMinutes, 2)
            : 0;

        var totalTerminal24h = deliveredWebhooks24h + failedWebhooks24h;
        var webhookSuccessRate24h = totalTerminal24h == 0
            ? 1.0
            : Math.Round((double)deliveredWebhooks24h / totalTerminal24h, 4);

        var degraded = deadLetterWebhooks > 0 || pendingOldestAgeMinutes > 30 || webhookSuccessRate24h < 0.99;

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
                unreadNotifications,
                idempotencyReplayEvents,
                idempotencyReplayHits,
                deliveredWebhooks24h,
                webhookAttempts24h,
                pendingOldestAgeMinutes,
                webhookSuccessRate24h,
                degraded,
                thresholds = new
                {
                    pendingOldestAgeMinutesWarn = 30,
                    webhookSuccessRate24hWarnBelow = 0.99,
                    deadLetterWebhooksWarnAbove = 0
                }
            }
        });
    }
}
