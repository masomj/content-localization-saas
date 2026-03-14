using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record SubmitForReviewRequest(Guid ContentItemId, string ReviewerEmail);
public sealed record ApproveContentItemRequest(Guid ContentItemId);
public sealed record RejectContentItemRequest(Guid ContentItemId, string Reason);

[ApiController]
[Route("api/review-workflow")]
public sealed class ReviewWorkflowController(AppDbContext db) : ControllerBase
{
    private static readonly HashSet<string> SubmitAllowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "draft",
        "outdated"
    };

    private string CurrentActor => HttpContext.Request.Headers["X-Actor-Email"].ToString() is { Length: > 0 } raw
        ? raw.Trim().ToLowerInvariant()
        : "reviewer@example.com";

    [HttpPost("submit")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Submit([FromBody] SubmitForReviewRequest request, CancellationToken cancellationToken)
    {
        if (request.ContentItemId == Guid.Empty || string.IsNullOrWhiteSpace(request.ReviewerEmail))
            return BadRequest(new { error = "contentItemId and reviewerEmail required" });

        var item = await db.ContentItems.FirstOrDefaultAsync(x => x.Id == request.ContentItemId, cancellationToken);
        if (item is null) return NotFound();

        if (!SubmitAllowed.Contains(item.Status))
        {
            return Conflict(new
            {
                error = "invalid_transition",
                from = item.Status,
                to = "in_review",
                guidance = "Only Draft/Outdated items can be submitted for review."
            });
        }

        var previousStatus = item.Status;
        var reviewer = request.ReviewerEmail.Trim().ToLowerInvariant();

        var updated = await db.ContentItems
            .Where(x => x.Id == item.Id && x.Status == previousStatus)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, "in_review")
                .SetProperty(x => x.ReviewAssigneeEmail, reviewer)
                .SetProperty(x => x.RejectionReason, string.Empty), cancellationToken);

        if (updated == 0)
        {
            return Conflict(new
            {
                error = "stale_write",
                guidance = "Content item changed since read; refresh and retry."
            });
        }

        db.ContentItemRevisions.Add(new ContentItemRevision
        {
            ContentItemId = item.Id,
            ActorEmail = CurrentActor,
            PreviousSource = item.Source,
            NewSource = item.Source,
            PreviousStatus = previousStatus,
            NewStatus = "in_review",
            DiffSummary = $"submitted for review to {reviewer}",
            EventType = "review_submit"
        });

        await db.SaveChangesAsync(cancellationToken);
        var updatedItem = await db.ContentItems.FirstAsync(x => x.Id == item.Id, cancellationToken);
        return Ok(updatedItem);
    }

    [HttpPost("approve")]
    [RequireAppRole(AppRole.Reviewer)]
    public async Task<IActionResult> Approve([FromBody] ApproveContentItemRequest request, CancellationToken cancellationToken)
    {
        var item = await db.ContentItems.FirstOrDefaultAsync(x => x.Id == request.ContentItemId, cancellationToken);
        if (item is null) return NotFound();

        if (!string.Equals(item.Status, "in_review", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new
            {
                error = "invalid_transition",
                from = item.Status,
                to = "approved",
                guidance = "Only In Review items can be approved."
            });
        }

        if (!string.IsNullOrWhiteSpace(item.ReviewAssigneeEmail) &&
            !string.Equals(item.ReviewAssigneeEmail, CurrentActor, StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = "reviewer_mismatch",
                assignedReviewer = item.ReviewAssigneeEmail,
                actor = CurrentActor
            });
        }

        var previousStatus = item.Status;
        var approvedUtc = DateTime.UtcNow;

        var updated = await db.ContentItems
            .Where(x => x.Id == item.Id
                        && x.Status == previousStatus
                        && (string.IsNullOrEmpty(x.ReviewAssigneeEmail) || x.ReviewAssigneeEmail == CurrentActor))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, "approved")
                .SetProperty(x => x.ApprovedUtc, approvedUtc)
                .SetProperty(x => x.ApprovedByEmail, CurrentActor)
                .SetProperty(x => x.RejectionReason, string.Empty), cancellationToken);

        if (updated == 0)
        {
            return Conflict(new
            {
                error = "stale_write",
                guidance = "Content item changed since read; refresh and retry."
            });
        }

        db.ContentItemRevisions.Add(new ContentItemRevision
        {
            ContentItemId = item.Id,
            ActorEmail = CurrentActor,
            PreviousSource = item.Source,
            NewSource = item.Source,
            PreviousStatus = previousStatus,
            NewStatus = "approved",
            DiffSummary = "approved",
            EventType = "review_approved"
        });

        var subscriptions = await db.WebhookSubscriptions.Where(x => x.ProjectId == item.ProjectId && x.IsActive).ToListAsync(cancellationToken);
        if (subscriptions.Count > 0)
        {
            var languageTasks = await db.ContentItemLanguageTasks.Where(x => x.ContentItemId == item.Id).ToListAsync(cancellationToken);
            var version = approvedUtc.Ticks;

            foreach (var sub in subscriptions)
            {
                var sourcePayload = new
                {
                    projectId = item.ProjectId,
                    itemKey = item.Key,
                    language = "source",
                    version,
                    status = "approved",
                    approvedBy = CurrentActor,
                    approvedUtc = approvedUtc
                };

                var sourcePayloadJson = System.Text.Json.JsonSerializer.Serialize(sourcePayload);
                var sourceIdempotencyKey = WebhooksController.ComputeIdempotencyKey(sub.Id, sourcePayloadJson);
                var existingSource = await db.WebhookDeliveryLogs.FirstOrDefaultAsync(x => x.IdempotencyKey == sourceIdempotencyKey, cancellationToken);
                if (existingSource is null)
                {
                    db.WebhookDeliveryLogs.Add(new WebhookDeliveryLog
                    {
                        SubscriptionId = sub.Id,
                        EventType = "content.approved",
                        PayloadJson = sourcePayloadJson,
                        IdempotencyKey = sourceIdempotencyKey,
                        AttemptCount = 0,
                        Status = "pending",
                        NextAttemptUtc = DateTime.UtcNow
                    });
                }

                foreach (var task in languageTasks)
                {
                    var payload = new
                    {
                        projectId = item.ProjectId,
                        itemKey = item.Key,
                        language = task.LanguageCode,
                        version,
                        status = task.Status,
                        approvedBy = CurrentActor,
                        approvedUtc = approvedUtc
                    };

                    var payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);
                    var idempotencyKey = WebhooksController.ComputeIdempotencyKey(sub.Id, payloadJson);
                    var existing = await db.WebhookDeliveryLogs.FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
                    if (existing is null)
                    {
                        db.WebhookDeliveryLogs.Add(new WebhookDeliveryLog
                        {
                            SubscriptionId = sub.Id,
                            EventType = "content.approved",
                            PayloadJson = payloadJson,
                            IdempotencyKey = idempotencyKey,
                            AttemptCount = 0,
                            Status = "pending",
                            NextAttemptUtc = DateTime.UtcNow
                        });
                    }
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        var updatedItem = await db.ContentItems.FirstAsync(x => x.Id == item.Id, cancellationToken);
        return Ok(updatedItem);
    }

    [HttpPost("reject")]
    [RequireAppRole(AppRole.Reviewer)]
    public async Task<IActionResult> Reject([FromBody] RejectContentItemRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(new { error = "rejection_reason_required" });

        var item = await db.ContentItems.FirstOrDefaultAsync(x => x.Id == request.ContentItemId, cancellationToken);
        if (item is null) return NotFound();

        if (!string.Equals(item.Status, "in_review", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new
            {
                error = "invalid_transition",
                from = item.Status,
                to = "draft",
                guidance = "Only In Review items can be rejected to editor."
            });
        }

        if (!string.IsNullOrWhiteSpace(item.ReviewAssigneeEmail) &&
            !string.Equals(item.ReviewAssigneeEmail, CurrentActor, StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = "reviewer_mismatch",
                assignedReviewer = item.ReviewAssigneeEmail,
                actor = CurrentActor
            });
        }

        var previousStatus = item.Status;
        var reason = request.Reason.Trim();

        var updated = await db.ContentItems
            .Where(x => x.Id == item.Id
                        && x.Status == previousStatus
                        && (string.IsNullOrEmpty(x.ReviewAssigneeEmail) || x.ReviewAssigneeEmail == CurrentActor))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, "draft")
                .SetProperty(x => x.RejectionReason, reason), cancellationToken);

        if (updated == 0)
        {
            return Conflict(new
            {
                error = "stale_write",
                guidance = "Content item changed since read; refresh and retry."
            });
        }

        db.ContentItemRevisions.Add(new ContentItemRevision
        {
            ContentItemId = item.Id,
            ActorEmail = CurrentActor,
            PreviousSource = item.Source,
            NewSource = item.Source,
            PreviousStatus = previousStatus,
            NewStatus = "draft",
            DiffSummary = $"rejected: {reason}",
            EventType = "review_rejected"
        });

        await db.SaveChangesAsync(cancellationToken);
        var updatedItem = await db.ContentItems.FirstAsync(x => x.Id == item.Id, cancellationToken);
        return Ok(updatedItem);
    }
}
