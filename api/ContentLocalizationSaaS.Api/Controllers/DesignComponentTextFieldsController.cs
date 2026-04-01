using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record UpdateTextFieldRequest(string CurrentText, Guid? ContentItemId);
public sealed record LinkContentKeyRequest(Guid ContentItemId);

[ApiController]
[Route("api/component-text-fields")]
[Microsoft.AspNetCore.Cors.EnableCors("PluginCors")]
public sealed class DesignComponentTextFieldsController(AppDbContext db) : ControllerBase
{
    private string CurrentActor => HttpContext.Request.Headers["X-Actor-Email"].ToString() is { Length: > 0 } raw
        ? raw.Trim().ToLowerInvariant()
        : "editor@system.local";

    [HttpPut("{id:guid}")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTextFieldRequest request, CancellationToken ct)
    {
        var field = await db.DesignComponentTextFields.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (field is null) return NotFound();

        var previousText = field.CurrentText;
        var textChanged = !string.Equals(previousText, request.CurrentText, StringComparison.Ordinal);

        field.CurrentText = request.CurrentText;
        field.UpdatedUtc = DateTime.UtcNow;

        // Update contentItemId link if provided
        if (request.ContentItemId.HasValue)
        {
            var contentItem = await db.ContentItems.FirstOrDefaultAsync(ci => ci.Id == request.ContentItemId.Value, ct);
            if (contentItem is null) return BadRequest(new { error = "ContentItem not found" });
            field.ContentItemId = request.ContentItemId.Value;
        }

        // If linked to a content item and text changed, update the linked ContentItem + create revision
        if (textChanged && field.ContentItemId.HasValue)
        {
            var linkedItem = await db.ContentItems.FirstOrDefaultAsync(ci => ci.Id == field.ContentItemId.Value, ct);
            if (linkedItem is not null)
            {
                var prevSource = linkedItem.Source;
                var prevStatus = linkedItem.Status;
                linkedItem.Source = request.CurrentText;
                linkedItem.Status = "draft";

                db.ContentItemRevisions.Add(new ContentItemRevision
                {
                    ContentItemId = linkedItem.Id,
                    ActorEmail = CurrentActor,
                    PreviousSource = prevSource,
                    NewSource = request.CurrentText,
                    PreviousStatus = prevStatus,
                    NewStatus = "draft",
                    DiffSummary = "design component text field update",
                    EventType = "component_edit"
                });
            }
        }

        await db.SaveChangesAsync(ct);
        return Ok(field);
    }

    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> GetHistory(Guid id, CancellationToken ct)
    {
        var field = await db.DesignComponentTextFields.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (field is null) return NotFound();

        if (!field.ContentItemId.HasValue)
            return Ok(Array.Empty<object>());

        var revisions = await db.ContentItemRevisions
            .Where(r => r.ContentItemId == field.ContentItemId.Value)
            .OrderByDescending(r => r.CreatedUtc)
            .Select(r => new
            {
                r.Id,
                r.ContentItemId,
                r.ActorEmail,
                r.PreviousSource,
                r.NewSource,
                r.PreviousStatus,
                r.NewStatus,
                r.DiffSummary,
                r.EventType,
                r.CreatedUtc
            })
            .ToListAsync(ct);

        return Ok(revisions);
    }

    [HttpPost("{id:guid}/link-content-key")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> LinkContentKey(Guid id, [FromBody] LinkContentKeyRequest request, CancellationToken ct)
    {
        var field = await db.DesignComponentTextFields.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (field is null) return NotFound();

        var contentItem = await db.ContentItems.FirstOrDefaultAsync(ci => ci.Id == request.ContentItemId, ct);
        if (contentItem is null) return BadRequest(new { error = "ContentItem not found" });

        field.ContentItemId = request.ContentItemId;
        field.UpdatedUtc = DateTime.UtcNow;

        // Sync text from content item to text field
        if (!string.IsNullOrEmpty(contentItem.Source))
        {
            field.CurrentText = contentItem.Source;
        }

        await db.SaveChangesAsync(ct);
        return Ok(new
        {
            field.Id,
            field.ContentItemId,
            field.CurrentText,
            ContentKey = contentItem.Key
        });
    }

    [HttpDelete("{id:guid}/link-content-key")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> UnlinkContentKey(Guid id, CancellationToken ct)
    {
        var field = await db.DesignComponentTextFields.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (field is null) return NotFound();

        field.ContentItemId = null;
        field.UpdatedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(new { field.Id, field.ContentItemId, field.CurrentText });
    }
}
