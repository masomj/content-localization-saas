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

[ApiController]
[Route("api/webhooks")]
public sealed class WebhooksController(AppDbContext db) : ControllerBase
{
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
    public async Task<IActionResult> Deliveries([FromQuery] Guid projectId, CancellationToken cancellationToken)
    {
        var subIds = await db.WebhookSubscriptions.Where(x => x.ProjectId == projectId).Select(x => x.Id).ToListAsync(cancellationToken);
        var logs = await db.WebhookDeliveryLogs.Where(x => subIds.Contains(x.SubscriptionId)).OrderByDescending(x => x.CreatedUtc).ToListAsync(cancellationToken);
        return Ok(logs);
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
                if (log.AttemptCount >= 5)
                {
                    log.Status = "failed";
                    log.LastError = "max_retries_exceeded";
                    log.NextAttemptUtc = null;
                }
                else
                {
                    log.Status = "pending";
                    log.LastError = "delivery_failed";
                    var delayMinutes = Math.Pow(2, log.AttemptCount - 1); // 1,2,4,8...
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
        return Ok(new { processed = pending.Count });
    }

    public static string ComputeSignature(string payloadJson, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadJson));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
