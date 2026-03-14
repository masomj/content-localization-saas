using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record LinkLayerRequest(Guid ProjectId, string DesignFileId, string LayerId, Guid? ContentItemId, string? CreateNewKey, string? SourceText, string DuplicateLinkRule = "preserve");
public sealed record DuplicateLayerRequest(Guid ProjectId, string DesignFileId, string SourceLayerId, string NewLayerId);

[ApiController]
[Route("api/plugin-links")]
public sealed class PluginLayerLinksController(AppDbContext db) : ControllerBase
{
    [HttpGet("search-items")]
    public async Task<IActionResult> SearchItems([FromQuery] Guid projectId, [FromQuery] string q, CancellationToken cancellationToken)
    {
        var query = db.ContentItems.Where(x => x.ProjectId == projectId);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim().ToLowerInvariant();
            query = query.Where(x => x.Key.ToLower().Contains(s) || x.Source.ToLower().Contains(s));
        }

        var rows = await query.OrderBy(x => x.Key).Take(50).ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpPost("link-layer")]
    public async Task<IActionResult> LinkLayer([FromBody] LinkLayerRequest request, CancellationToken cancellationToken)
    {
        if (request.ProjectId == Guid.Empty || string.IsNullOrWhiteSpace(request.DesignFileId) || string.IsNullOrWhiteSpace(request.LayerId))
            return BadRequest(new { error = "projectId/designFileId/layerId required" });

        Guid contentItemId;

        if (request.ContentItemId.HasValue)
        {
            var existing = await db.ContentItems.FirstOrDefaultAsync(x => x.Id == request.ContentItemId.Value && x.ProjectId == request.ProjectId, cancellationToken);
            if (existing is null) return NotFound(new { error = "content_item_not_found" });
            contentItemId = existing.Id;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.CreateNewKey) || string.IsNullOrWhiteSpace(request.SourceText))
                return BadRequest(new { error = "either contentItemId or createNewKey+sourceText required" });

            var item = new ContentItem
            {
                ProjectId = request.ProjectId,
                Key = request.CreateNewKey.Trim(),
                Source = request.SourceText.Trim(),
                Status = "draft"
            };
            db.ContentItems.Add(item);
            await db.SaveChangesAsync(cancellationToken);
            contentItemId = item.Id;
        }

        var link = await db.DesignLayerLinks.FirstOrDefaultAsync(
            x => x.ProjectId == request.ProjectId && x.DesignFileId == request.DesignFileId && x.LayerId == request.LayerId,
            cancellationToken);

        if (link is null)
        {
            link = new DesignLayerLink
            {
                ProjectId = request.ProjectId,
                DesignFileId = request.DesignFileId.Trim(),
                LayerId = request.LayerId.Trim(),
                ContentItemId = contentItemId,
                DuplicateLinkRule = string.IsNullOrWhiteSpace(request.DuplicateLinkRule) ? "preserve" : request.DuplicateLinkRule.Trim().ToLowerInvariant()
            };
            db.DesignLayerLinks.Add(link);
        }
        else
        {
            link.ContentItemId = contentItemId;
            link.DuplicateLinkRule = string.IsNullOrWhiteSpace(request.DuplicateLinkRule) ? link.DuplicateLinkRule : request.DuplicateLinkRule.Trim().ToLowerInvariant();
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(link);
    }

    [HttpGet("layer")]
    public async Task<IActionResult> GetLayerLink([FromQuery] Guid projectId, [FromQuery] string designFileId, [FromQuery] string layerId, CancellationToken cancellationToken)
    {
        var link = await db.DesignLayerLinks
            .FirstOrDefaultAsync(x => x.ProjectId == projectId && x.DesignFileId == designFileId && x.LayerId == layerId, cancellationToken);

        if (link is null) return NotFound();

        var item = await db.ContentItems.FirstOrDefaultAsync(x => x.Id == link.ContentItemId, cancellationToken);
        return Ok(new { link, contentItem = item });
    }

    [HttpPost("duplicate-layer")]
    public async Task<IActionResult> DuplicateLayer([FromBody] DuplicateLayerRequest request, CancellationToken cancellationToken)
    {
        var source = await db.DesignLayerLinks.FirstOrDefaultAsync(
            x => x.ProjectId == request.ProjectId && x.DesignFileId == request.DesignFileId && x.LayerId == request.SourceLayerId,
            cancellationToken);

        if (source is null) return NotFound(new { error = "source_layer_link_not_found" });

        if (source.DuplicateLinkRule == "clear")
        {
            return Ok(new { status = "duplicated_unlinked", rule = source.DuplicateLinkRule });
        }

        var existing = await db.DesignLayerLinks.FirstOrDefaultAsync(
            x => x.ProjectId == request.ProjectId && x.DesignFileId == request.DesignFileId && x.LayerId == request.NewLayerId,
            cancellationToken);

        if (existing is null)
        {
            db.DesignLayerLinks.Add(new DesignLayerLink
            {
                ProjectId = source.ProjectId,
                DesignFileId = source.DesignFileId,
                LayerId = request.NewLayerId,
                ContentItemId = source.ContentItemId,
                DuplicateLinkRule = source.DuplicateLinkRule
            });
            await db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            existing.ContentItemId = source.ContentItemId;
            existing.DuplicateLinkRule = source.DuplicateLinkRule;
            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(new { status = "duplicated_link_preserved", rule = source.DuplicateLinkRule });
    }
}
