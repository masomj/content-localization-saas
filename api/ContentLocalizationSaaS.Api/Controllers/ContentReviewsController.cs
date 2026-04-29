using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record SubmitReviewRequest(Guid ContentItemId, string Verdict, string Body);

public sealed record ReviewQueueItem
{
    public Guid Id { get; init; }
    public string Key { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string ReviewAssigneeEmail { get; init; } = string.Empty;
    public Guid ProjectId { get; init; }
    public int CommentCount { get; init; }
    public int ReviewCount { get; init; }
    public string? LatestReviewVerdict { get; init; }
}

public sealed record TimelineEntry
{
    public string Type { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string ActorEmail { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public object? Details { get; init; }
}

[ApiController]
[Route("api/content-reviews")]
public sealed class ContentReviewsController(AppDbContext db) : ControllerBase
{
    private static readonly HashSet<string> ValidVerdicts = new(StringComparer.OrdinalIgnoreCase)
    {
        "approved",
        "changes_requested",
        "comment"
    };

    private string CurrentActor => HttpContext.Request.Headers["X-Actor-Email"].ToString() is { Length: > 0 } raw
        ? raw.Trim().ToLowerInvariant()
        : "reviewer@example.com";

    /// <summary>
    /// GET /api/content-reviews/queue — reviewer's queue: content items with status in_review
    /// </summary>
    [HttpGet("queue")]
    [RequireAppRole(AppRole.Reviewer)]
    public async Task<IActionResult> GetQueue([FromQuery] string? reviewerEmail, CancellationToken cancellationToken)
    {
        var pendingReviewTaskItemIds = await db.ContentItemLanguageTasks
            .Where(x => x.Status == "pending_review")
            .Select(x => x.ContentItemId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var query = db.ContentItems.Where(x => x.Status == "in_review" || pendingReviewTaskItemIds.Contains(x.Id));

        if (!string.IsNullOrWhiteSpace(reviewerEmail))
        {
            var normalized = reviewerEmail.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.ReviewAssigneeEmail == normalized || x.ReviewAssigneeEmail == string.Empty || x.ReviewAssigneeEmail == null);
        }

        var items = await query.OrderByDescending(x => x.CreatedUtc).ToListAsync(cancellationToken);
        var itemIds = items.Select(x => x.Id).ToList();

        var pendingReviewItems = await db.ContentItemLanguageTasks
            .Where(x => itemIds.Contains(x.ContentItemId) && x.Status == "pending_review")
            .Select(x => x.ContentItemId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var pendingReviewItemSet = pendingReviewItems.ToHashSet();

        // Get comment counts per content item (via threads)
        var threadIds = await db.DiscussionThreads
            .Where(x => itemIds.Contains(x.ContentItemId))
            .Select(x => new { x.Id, x.ContentItemId })
            .ToListAsync(cancellationToken);

        var threadIdList = threadIds.Select(x => x.Id).ToList();
        var commentCounts = await db.DiscussionComments
            .Where(x => threadIdList.Contains(x.ThreadId))
            .GroupBy(x => x.ThreadId)
            .Select(g => new { ThreadId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var commentCountByThread = commentCounts.ToDictionary(x => x.ThreadId, x => x.Count);
        var commentCountByItem = threadIds
            .GroupBy(x => x.ContentItemId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(t => commentCountByThread.GetValueOrDefault(t.Id, 0)));

        // Get review counts and latest verdict per content item
        var reviews = await db.ContentReviews
            .Where(x => itemIds.Contains(x.ContentItemId))
            .GroupBy(x => x.ContentItemId)
            .Select(g => new
            {
                ContentItemId = g.Key,
                Count = g.Count(),
                LatestVerdict = g.OrderByDescending(r => r.CreatedUtc).First().Verdict
            })
            .ToListAsync(cancellationToken);

        var reviewByItem = reviews.ToDictionary(x => x.ContentItemId);

        var result = items.Select(item =>
        {
            reviewByItem.TryGetValue(item.Id, out var reviewInfo);
            return new ReviewQueueItem
            {
                Id = item.Id,
                Key = item.Key,
                Source = item.Source,
                Status = pendingReviewItemSet.Contains(item.Id) ? "pending_review" : item.Status,
                ReviewAssigneeEmail = item.ReviewAssigneeEmail,
                ProjectId = item.ProjectId,
                CommentCount = commentCountByItem.GetValueOrDefault(item.Id, 0),
                ReviewCount = reviewInfo?.Count ?? 0,
                LatestReviewVerdict = reviewInfo?.LatestVerdict
            };
        });

        return Ok(result);
    }

    /// <summary>
    /// GET /api/content-reviews?contentItemId=X — list all reviews for a content item
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListReviews([FromQuery] Guid contentItemId, CancellationToken cancellationToken)
    {
        if (contentItemId == Guid.Empty)
            return BadRequest(new { error = "contentItemId_required" });

        var reviews = await db.ContentReviews
            .Where(x => x.ContentItemId == contentItemId)
            .OrderByDescending(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);

        return Ok(reviews);
    }

    /// <summary>
    /// POST /api/content-reviews — submit a formal review
    /// </summary>
    [HttpPost]
    [RequireAppRole(AppRole.Reviewer)]
    public async Task<IActionResult> SubmitReview([FromBody] SubmitReviewRequest request, CancellationToken cancellationToken)
    {
        if (request.ContentItemId == Guid.Empty)
            return BadRequest(new { error = "contentItemId_required" });

        var verdict = request.Verdict?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!ValidVerdicts.Contains(verdict))
            return BadRequest(new { error = "invalid_verdict", allowed = ValidVerdicts.OrderBy(x => x) });

        var item = await db.ContentItems.FirstOrDefaultAsync(x => x.Id == request.ContentItemId, cancellationToken);
        if (item is null)
            return NotFound();

        var pendingReviewTasks = await db.ContentItemLanguageTasks
            .Where(x => x.ContentItemId == item.Id && x.Status == "pending_review")
            .ToListAsync(cancellationToken);
        var hasPendingReviewTasks = pendingReviewTasks.Count > 0;

        // Create the review record
        var review = new ContentReview
        {
            ContentItemId = item.Id,
            ReviewerEmail = CurrentActor,
            Verdict = verdict,
            Body = request.Body?.Trim() ?? string.Empty
        };
        db.ContentReviews.Add(review);

        var previousStatus = item.Status;

        if (string.Equals(verdict, "approved", StringComparison.OrdinalIgnoreCase))
        {
            // Reuse approve logic from ReviewWorkflowController
            if (!string.Equals(item.Status, "in_review", StringComparison.OrdinalIgnoreCase) && !hasPendingReviewTasks)
            {
                return Conflict(new
                {
                    error = "invalid_transition",
                    from = item.Status,
                    to = "approved",
                    guidance = "Only In Review items can be approved."
                });
            }

            var approvedUtc = DateTime.UtcNow;

            var updated = await db.ContentItems
                .Where(x => x.Id == item.Id && x.Status == previousStatus)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, "approved")
                    .SetProperty(x => x.ApprovedUtc, approvedUtc)
                    .SetProperty(x => x.ApprovedByEmail, CurrentActor)
                    .SetProperty(x => x.RejectionReason, string.Empty), cancellationToken);

            if (updated == 0)
                return Conflict(new { error = "stale_write", guidance = "Content item changed since read; refresh and retry." });

            foreach (var task in pendingReviewTasks)
            {
                task.Status = "approved";
                task.IsOutdated = false;

                if (!string.IsNullOrWhiteSpace(task.TranslationText))
                {
                    db.TranslationMemoryEntries.Add(new TranslationMemoryEntry
                    {
                        ProjectId = item.ProjectId,
                        SourceText = item.Source,
                        LanguageCode = task.LanguageCode,
                        TranslationText = task.TranslationText,
                        IsApproved = true
                    });
                }
            }

            // Audit trail
            db.ContentItemRevisions.Add(new ContentItemRevision
            {
                ContentItemId = item.Id,
                ActorEmail = CurrentActor,
                PreviousSource = item.Source,
                NewSource = item.Source,
                PreviousStatus = previousStatus,
                NewStatus = "approved",
                DiffSummary = "approved via content review",
                EventType = "review_approved"
            });

            // Webhook dispatch (same logic as ReviewWorkflowController.Approve)
            var subscriptions = await db.WebhookSubscriptions
                .Where(x => x.ProjectId == item.ProjectId && x.IsActive)
                .ToListAsync(cancellationToken);

            if (subscriptions.Count > 0)
            {
                var languageTasks = await db.ContentItemLanguageTasks
                    .Where(x => x.ContentItemId == item.Id)
                    .ToListAsync(cancellationToken);
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
                        approvedUtc
                    };

                    var sourcePayloadJson = JsonSerializer.Serialize(sourcePayload);
                    var sourceIdempotencyKey = WebhooksController.ComputeIdempotencyKey(sub.Id, sourcePayloadJson);
                    var existingSource = await db.WebhookDeliveryLogs
                        .FirstOrDefaultAsync(x => x.IdempotencyKey == sourceIdempotencyKey, cancellationToken);

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
                            approvedUtc
                        };

                        var payloadJson = JsonSerializer.Serialize(payload);
                        var idempotencyKey = WebhooksController.ComputeIdempotencyKey(sub.Id, payloadJson);
                        var existing = await db.WebhookDeliveryLogs
                            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

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
        }
        else if (string.Equals(verdict, "changes_requested", StringComparison.OrdinalIgnoreCase))
        {
            // Reuse reject logic from ReviewWorkflowController
            if (!string.Equals(item.Status, "in_review", StringComparison.OrdinalIgnoreCase) && !hasPendingReviewTasks)
            {
                return Conflict(new
                {
                    error = "invalid_transition",
                    from = item.Status,
                    to = "draft",
                    guidance = "Only In Review items can have changes requested."
                });
            }

            var reason = request.Body?.Trim() ?? string.Empty;

            var updated = await db.ContentItems
                .Where(x => x.Id == item.Id && x.Status == previousStatus)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, "draft")
                    .SetProperty(x => x.RejectionReason, reason), cancellationToken);

            if (updated == 0)
                return Conflict(new { error = "stale_write", guidance = "Content item changed since read; refresh and retry." });

            foreach (var task in pendingReviewTasks)
            {
                task.Status = "draft";
            }

            // Audit trail
            db.ContentItemRevisions.Add(new ContentItemRevision
            {
                ContentItemId = item.Id,
                ActorEmail = CurrentActor,
                PreviousSource = item.Source,
                NewSource = item.Source,
                PreviousStatus = previousStatus,
                NewStatus = "draft",
                DiffSummary = $"changes requested: {reason}",
                EventType = "review_changes_requested"
            });
        }
        else
        {
            // comment — no status change, just record
            db.ContentItemRevisions.Add(new ContentItemRevision
            {
                ContentItemId = item.Id,
                ActorEmail = CurrentActor,
                PreviousSource = item.Source,
                NewSource = item.Source,
                PreviousStatus = previousStatus,
                NewStatus = previousStatus,
                DiffSummary = "review comment submitted",
                EventType = "review_comment"
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(review);
    }

    /// <summary>
    /// GET /api/content-reviews/{contentItemId}/timeline — unified chronological timeline
    /// </summary>
    [HttpGet("{contentItemId:guid}/timeline")]
    public async Task<IActionResult> GetTimeline(Guid contentItemId, CancellationToken cancellationToken)
    {
        var itemExists = await db.ContentItems.AnyAsync(x => x.Id == contentItemId, cancellationToken);
        if (!itemExists)
            return NotFound();

        var timeline = new List<TimelineEntry>();

        // Reviews
        var reviews = await db.ContentReviews
            .Where(x => x.ContentItemId == contentItemId)
            .OrderBy(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);

        foreach (var r in reviews)
        {
            timeline.Add(new TimelineEntry
            {
                Type = "review",
                Timestamp = r.CreatedUtc,
                ActorEmail = r.ReviewerEmail,
                Summary = r.Verdict switch
                {
                    "approved" => "approved this content item",
                    "changes_requested" => "requested changes",
                    _ => "left a review comment"
                },
                Details = new { r.Id, r.Verdict, r.Body }
            });
        }

        // Comments (via discussion threads)
        var threads = await db.DiscussionThreads
            .Where(x => x.ContentItemId == contentItemId)
            .ToListAsync(cancellationToken);

        var threadIds = threads.Select(x => x.Id).ToList();

        var comments = await db.DiscussionComments
            .Where(x => threadIds.Contains(x.ThreadId))
            .OrderBy(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);

        foreach (var c in comments)
        {
            timeline.Add(new TimelineEntry
            {
                Type = "comment",
                Timestamp = c.CreatedUtc,
                ActorEmail = c.AuthorEmail,
                Summary = "commented",
                Details = new { c.Id, c.ThreadId, c.Body, c.ReviewId }
            });
        }

        // Status changes / revisions
        var revisions = await db.ContentItemRevisions
            .Where(x => x.ContentItemId == contentItemId)
            .OrderBy(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);

        foreach (var rev in revisions)
        {
            timeline.Add(new TimelineEntry
            {
                Type = rev.EventType switch
                {
                    "edited" or "rollback" => "revision",
                    _ => "status_change"
                },
                Timestamp = rev.CreatedUtc,
                ActorEmail = rev.ActorEmail,
                Summary = rev.DiffSummary,
                Details = new { rev.Id, rev.EventType, rev.PreviousStatus, rev.NewStatus }
            });
        }

        // Sort chronologically
        var sorted = timeline.OrderBy(x => x.Timestamp).ToList();
        return Ok(sorted);
    }
}
