using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record CreateExternalReviewLinkRequest(Guid ContentItemId, bool CommentEnabled, DateTime ExpiresUtc);
public sealed record ExternalCommentRequest(string Token, string Body, string AuthorEmail);

[ApiController]
[Route("api/external-review")]
public sealed class ExternalReviewLinksController(AppDbContext db) : ControllerBase
{
    [HttpPost("links")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> CreateLink([FromBody] CreateExternalReviewLinkRequest request, CancellationToken cancellationToken)
    {
        if (request.ContentItemId == Guid.Empty || request.ExpiresUtc <= DateTime.UtcNow)
            return BadRequest(new { error = "valid_contentItemId_and_future_expiry_required" });

        var item = await db.ContentItems.FirstOrDefaultAsync(x => x.Id == request.ContentItemId, cancellationToken);
        if (item is null) return NotFound();

        var link = new ExternalReviewLink
        {
            ContentItemId = request.ContentItemId,
            CommentEnabled = request.CommentEnabled,
            ExpiresUtc = request.ExpiresUtc,
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", string.Empty).Replace("/", "_").Replace("+", "-")
        };

        db.ExternalReviewLinks.Add(link);
        db.ActivityFeedEvents.Add(new ActivityFeedEvent
        {
            ProjectId = item.ProjectId,
            EventType = "external_link_created",
            ActorEmail = "editor@example.com",
            Message = $"External review link created for item {item.Key}"
        });

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { link.Id, link.Token, link.ExpiresUtc, link.CommentEnabled });
    }

    [HttpGet("links/{token}")]
    public async Task<IActionResult> Open(string token, CancellationToken cancellationToken)
    {
        var link = await db.ExternalReviewLinks.FirstOrDefaultAsync(x => x.Token == token, cancellationToken);
        if (link is null) return NotFound();
        if (link.ExpiresUtc <= DateTime.UtcNow) return StatusCode(StatusCodes.Status410Gone, new { error = "link_expired" });

        var item = await db.ContentItems.FirstOrDefaultAsync(x => x.Id == link.ContentItemId, cancellationToken);
        if (item is null) return NotFound();

        return Ok(new
        {
            item.Id,
            item.Key,
            item.Source,
            link.CommentEnabled,
            link.ExpiresUtc
        });
    }

    [HttpPost("comments")]
    public async Task<IActionResult> ExternalComment([FromBody] ExternalCommentRequest request, CancellationToken cancellationToken)
    {
        var link = await db.ExternalReviewLinks.FirstOrDefaultAsync(x => x.Token == request.Token, cancellationToken);
        if (link is null) return NotFound();
        if (link.ExpiresUtc <= DateTime.UtcNow) return StatusCode(StatusCodes.Status410Gone, new { error = "link_expired" });
        if (!link.CommentEnabled) return StatusCode(StatusCodes.Status403Forbidden, new { error = "comments_disabled" });

        var thread = await db.DiscussionThreads
            .FirstOrDefaultAsync(x => x.ContentItemId == link.ContentItemId && !x.IsResolved, cancellationToken);

        if (thread is null)
        {
            thread = new DiscussionThread
            {
                ContentItemId = link.ContentItemId,
                Title = "External review comments",
                CreatedByEmail = "external@review.link",
                IsResolved = false
            };
            db.DiscussionThreads.Add(thread);
            await db.SaveChangesAsync(cancellationToken);
        }

        db.DiscussionComments.Add(new DiscussionComment
        {
            ThreadId = thread.Id,
            ParentCommentId = null,
            Body = request.Body.Trim(),
            AuthorEmail = $"external:{request.AuthorEmail.Trim().ToLowerInvariant()}"
        });

        var item = await db.ContentItems.FirstOrDefaultAsync(x => x.Id == link.ContentItemId, cancellationToken);
        if (item is not null)
        {
            db.ActivityFeedEvents.Add(new ActivityFeedEvent
            {
                ProjectId = item.ProjectId,
                EventType = "external_comment",
                ActorEmail = $"external:{request.AuthorEmail.Trim().ToLowerInvariant()}",
                Message = $"External comment added to item {item.Key}"
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { status = "comment_saved" });
    }
}
