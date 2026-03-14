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

        item.Status = "in_review";
        item.ReviewAssigneeEmail = request.ReviewerEmail.Trim().ToLowerInvariant();
        item.RejectionReason = string.Empty;

        db.ContentItemRevisions.Add(new ContentItemRevision
        {
            ContentItemId = item.Id,
            ActorEmail = CurrentActor,
            PreviousSource = item.Source,
            NewSource = item.Source,
            PreviousStatus = "draft",
            NewStatus = item.Status,
            DiffSummary = $"submitted for review to {item.ReviewAssigneeEmail}",
            EventType = "review_submit"
        });

        await db.SaveChangesAsync(cancellationToken);
        return Ok(item);
    }

    [HttpPost("approve")]
    [RequireAppRole(AppRole.Reviewer)]
    public async Task<IActionResult> Approve([FromBody] ApproveContentItemRequest request, CancellationToken cancellationToken)
    {
        var item = await db.ContentItems.FirstOrDefaultAsync(x => x.Id == request.ContentItemId, cancellationToken);
        if (item is null) return NotFound();

        var previousStatus = item.Status;
        item.Status = "approved";
        item.ApprovedUtc = DateTime.UtcNow;
        item.ApprovedByEmail = CurrentActor;
        item.RejectionReason = string.Empty;

        db.ContentItemRevisions.Add(new ContentItemRevision
        {
            ContentItemId = item.Id,
            ActorEmail = CurrentActor,
            PreviousSource = item.Source,
            NewSource = item.Source,
            PreviousStatus = previousStatus,
            NewStatus = item.Status,
            DiffSummary = "approved",
            EventType = "review_approved"
        });

        var subscriptions = await db.WebhookSubscriptions.Where(x => x.ProjectId == item.ProjectId && x.IsActive).ToListAsync(cancellationToken);
        if (subscriptions.Count > 0)
        {
            var languageTasks = await db.ContentItemLanguageTasks.Where(x => x.ContentItemId == item.Id).ToListAsync(cancellationToken);
            var version = item.ApprovedUtc?.Ticks ?? DateTime.UtcNow.Ticks;

            foreach (var sub in subscriptions)
            {
                var sourcePayload = new
                {
                    projectId = item.ProjectId,
                    itemKey = item.Key,
                    language = "source",
                    version,
                    status = item.Status,
                    approvedBy = item.ApprovedByEmail,
                    approvedUtc = item.ApprovedUtc
                };

                db.WebhookDeliveryLogs.Add(new WebhookDeliveryLog
                {
                    SubscriptionId = sub.Id,
                    EventType = "content.approved",
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(sourcePayload),
                    AttemptCount = 0,
                    Status = "pending",
                    NextAttemptUtc = DateTime.UtcNow
                });

                foreach (var task in languageTasks)
                {
                    var payload = new
                    {
                        projectId = item.ProjectId,
                        itemKey = item.Key,
                        language = task.LanguageCode,
                        version,
                        status = task.Status,
                        approvedBy = item.ApprovedByEmail,
                        approvedUtc = item.ApprovedUtc
                    };

                    db.WebhookDeliveryLogs.Add(new WebhookDeliveryLog
                    {
                        SubscriptionId = sub.Id,
                        EventType = "content.approved",
                        PayloadJson = System.Text.Json.JsonSerializer.Serialize(payload),
                        AttemptCount = 0,
                        Status = "pending",
                        NextAttemptUtc = DateTime.UtcNow
                    });
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(item);
    }

    [HttpPost("reject")]
    [RequireAppRole(AppRole.Reviewer)]
    public async Task<IActionResult> Reject([FromBody] RejectContentItemRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(new { error = "rejection_reason_required" });

        var item = await db.ContentItems.FirstOrDefaultAsync(x => x.Id == request.ContentItemId, cancellationToken);
        if (item is null) return NotFound();

        var previousStatus = item.Status;
        item.Status = "draft";
        item.RejectionReason = request.Reason.Trim();

        db.ContentItemRevisions.Add(new ContentItemRevision
        {
            ContentItemId = item.Id,
            ActorEmail = CurrentActor,
            PreviousSource = item.Source,
            NewSource = item.Source,
            PreviousStatus = previousStatus,
            NewStatus = item.Status,
            DiffSummary = $"rejected: {item.RejectionReason}",
            EventType = "review_rejected"
        });

        await db.SaveChangesAsync(cancellationToken);
        return Ok(item);
    }
}
