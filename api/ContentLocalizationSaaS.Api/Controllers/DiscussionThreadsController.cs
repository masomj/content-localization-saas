using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record CreateThreadRequest(Guid ContentItemId, string Title, string Body);
public sealed record ReplyRequest(Guid ThreadId, Guid? ParentCommentId, string Body);

[ApiController]
[Route("api/discussions")]
public sealed class DiscussionThreadsController(AppDbContext db) : ControllerBase
{
    private string CurrentActor => HttpContext.Request.Headers["X-Actor-Email"].ToString() is { Length: > 0 } raw
        ? raw.Trim().ToLowerInvariant()
        : "member@example.com";

    [HttpGet("threads")]
    public async Task<IActionResult> GetThreads([FromQuery] Guid contentItemId, [FromQuery] bool includeResolved = false, CancellationToken cancellationToken = default)
    {
        var query = db.DiscussionThreads.Where(x => x.ContentItemId == contentItemId);
        if (!includeResolved) query = query.Where(x => !x.IsResolved);

        var threads = await query.OrderByDescending(x => x.CreatedUtc).ToListAsync(cancellationToken);
        return Ok(threads);
    }

    [HttpGet("threads/{threadId:guid}/comments")]
    public async Task<IActionResult> GetComments(Guid threadId, CancellationToken cancellationToken)
    {
        var comments = await db.DiscussionComments.Where(x => x.ThreadId == threadId).OrderBy(x => x.CreatedUtc).ToListAsync(cancellationToken);
        return Ok(comments);
    }

    [HttpPost("threads")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> CreateThread([FromBody] CreateThreadRequest request, CancellationToken cancellationToken)
    {
        if (request.ContentItemId == Guid.Empty || string.IsNullOrWhiteSpace(request.Body))
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = ["contentItemId and body are required"]
            }));
        }

        var itemExists = await db.ContentItems.AnyAsync(x => x.Id == request.ContentItemId, cancellationToken);
        if (!itemExists) return NotFound();

        var thread = new DiscussionThread
        {
            ContentItemId = request.ContentItemId,
            Title = request.Title?.Trim() ?? string.Empty,
            CreatedByEmail = CurrentActor,
            IsResolved = false
        };

        db.DiscussionThreads.Add(thread);
        await db.SaveChangesAsync(cancellationToken);

        var openingComment = new DiscussionComment
        {
            ThreadId = thread.Id,
            ParentCommentId = null,
            Body = request.Body.Trim(),
            AuthorEmail = CurrentActor
        };

        db.DiscussionComments.Add(openingComment);
        await db.SaveChangesAsync(cancellationToken);
        await NotificationsController.CreateMentionNotificationsAsync(db, openingComment.Body, CurrentActor, cancellationToken);
        return Ok(thread);
    }

    [HttpPost("replies")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Reply([FromBody] ReplyRequest request, CancellationToken cancellationToken)
    {
        if (request.ThreadId == Guid.Empty || string.IsNullOrWhiteSpace(request.Body)) return BadRequest(new { error = "threadId and body required" });

        var thread = await db.DiscussionThreads.FirstOrDefaultAsync(x => x.Id == request.ThreadId, cancellationToken);
        if (thread is null) return NotFound();

        var comment = new DiscussionComment
        {
            ThreadId = request.ThreadId,
            ParentCommentId = request.ParentCommentId,
            Body = request.Body.Trim(),
            AuthorEmail = CurrentActor
        };

        db.DiscussionComments.Add(comment);
        await db.SaveChangesAsync(cancellationToken);
        await NotificationsController.CreateMentionNotificationsAsync(db, comment.Body, CurrentActor, cancellationToken);
        return Ok(comment);
    }

    [HttpPost("threads/{threadId:guid}/resolve")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Resolve(Guid threadId, CancellationToken cancellationToken)
    {
        var thread = await db.DiscussionThreads.FirstOrDefaultAsync(x => x.Id == threadId, cancellationToken);
        if (thread is null) return NotFound();

        thread.IsResolved = true;
        thread.ResolvedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new { status = "resolved" });
    }
}
