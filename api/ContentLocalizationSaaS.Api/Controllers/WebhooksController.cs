using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record CreateWebhookSubscriptionRequest(Guid ProjectId, string EndpointUrl, string Secret);
public sealed record RequeueWebhookDeliveryRequest(Guid DeliveryId);

[ApiController]
[Route("api/webhooks")]
public sealed class WebhooksController(AppDbContext db) : ControllerBase
{
    private const int MaxAttempts = 5;

    [HttpGet("subscriptions")]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> Subscriptions([FromQuery] Guid projectId, CancellationToken cancellationToken)
    {
        var rows = await db.WebhookSubscriptions.Where(x => x.ProjectId == projectId).OrderByDescending(x => x.CreatedUtc).ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpPost("subscriptions")]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateWebhookSubscriptionRequest request, CancellationToken cancellationToken)
    {
        if (request.ProjectId == Guid.Empty || string.IsNullOrWhiteSpace(request.EndpointUrl) || string.IsNullOrWhiteSpace(request.Secret))
            return BadRequest(new { error = "projectId_endpoint_secret_required" });

        var row = new WebhookSubscription
        {
            ProjectId = request.ProjectId,
            EndpointUrl = request.EndpointUrl.Trim(),
            Secret = request.Secret.Trim(),
            IsActive = true
        };

        db.WebhookSubscriptions.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(row);
    }

    [HttpGet("deliveries")]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> Deliveries([FromQuery] Guid projectId, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var subIds = await db.WebhookSubscriptions.Where(x => x.ProjectId == projectId).Select(x => x.Id).ToListAsync(cancellationToken);
        var query = db.WebhookDeliveryLogs.Where(x => subIds.Contains(x.SubscriptionId));
        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.Trim().ToLowerInvariant();
            query = query.Where(x => x.Status == s);
        }

        var logs = await query.OrderByDescending(x => x.CreatedUtc).ToListAsync(cancellationToken);
        return Ok(logs);
    }

    [HttpGet("dead-letters")]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> DeadLetters([FromQuery] Guid projectId, CancellationToken cancellationToken)
    {
        var subIds = await db.WebhookSubscriptions.Where(x => x.ProjectId == projectId).Select(x => x.Id).ToListAsync(cancellationToken);
        var logs = await db.WebhookDeliveryLogs
            .Where(x => subIds.Contains(x.SubscriptionId) && x.Status == "dead_letter")
            .OrderByDescending(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);

        return Ok(logs);
    }

    [HttpPost("requeue")]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> Requeue([FromBody] RequeueWebhookDeliveryRequest request, CancellationToken cancellationToken)
    {
        var row = await db.WebhookDeliveryLogs.FirstOrDefaultAsync(x => x.Id == request.DeliveryId, cancellationToken);
        if (row is null) return NotFound();

        row.Status = "pending";
        row.NextAttemptUtc = DateTime.UtcNow;
        row.LastError = string.Empty;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new { status = "requeued", row.Id });
    }

    [HttpPost("process-pending")]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> ProcessPending(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var pending = await db.WebhookDeliveryLogs
            .Where(x => x.Status == "pending" && (!x.NextAttemptUtc.HasValue || x.NextAttemptUtc <= now))
            .OrderBy(x => x.CreatedUtc)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var log in pending)
        {
            var sub = await db.WebhookSubscriptions.FirstOrDefaultAsync(x => x.Id == log.SubscriptionId, cancellationToken);
            if (sub is null || !sub.IsActive)
            {
                log.Status = "failed";
                log.LastError = "subscription_inactive";
                continue;
            }

            var signature = ComputeSignature(log.PayloadJson, sub.Secret);
            var simulatedFailure = sub.EndpointUrl.Contains("fail", StringComparison.OrdinalIgnoreCase);

            log.AttemptCount++;

            if (simulatedFailure)
            {
                if (log.AttemptCount >= MaxAttempts)
                {
                    log.Status = "dead_letter";
                    log.LastError = "max_retries_exceeded";
                    log.NextAttemptUtc = null;
                }
                else
                {
                    log.Status = "pending";
                    log.LastError = "delivery_failed";
                    var delayMinutes = Math.Min(60, Math.Pow(2, log.AttemptCount - 1)); // 1,2,4,8... capped
                    log.NextAttemptUtc = DateTime.UtcNow.AddMinutes(delayMinutes);
                }
            }
            else
            {
                log.Status = "delivered";
                log.DeliveredUtc = DateTime.UtcNow;
                log.LastError = string.Empty;
                log.NextAttemptUtc = null;
                // signature is included to demonstrate verifiable payload contracts
                log.PayloadJson = JsonSerializer.Serialize(new
                {
                    payload = JsonSerializer.Deserialize<object>(log.PayloadJson),
                    signature
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var delivered = pending.Count(x => x.Status == "delivered");
        var retried = pending.Count(x => x.Status == "pending");
        var deadLettered = pending.Count(x => x.Status == "dead_letter");

        return Ok(new { processed = pending.Count, delivered, retried, deadLettered, maxAttempts = MaxAttempts });
    }

    public static string ComputeSignature(string payloadJson, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadJson));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string ComputeIdempotencyKey(Guid subscriptionId, string payloadJson)
    {
        var raw = $"{subscriptionId:N}:{payloadJson}";
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
