using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record CreateLibraryComponentRequest(
    Guid ProjectId,
    string FigmaFileId,
    string FigmaComponentKey,
    string FigmaComponentId,
    string FigmaComponentSetId,
    string Name,
    string? Description,
    string? ThumbnailUrl,
    int FrameWidth,
    int FrameHeight);

public sealed record UpdateLibraryComponentRequest(
    string? Name,
    string? Description,
    string? ThumbnailUrl,
    int? FrameWidth,
    int? FrameHeight);

[ApiController]
[Route("api/library-components")]
[Microsoft.AspNetCore.Cors.EnableCors("PluginCors")]
public sealed class LibraryComponentsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid projectId, CancellationToken ct)
    {
        var components = await db.LibraryComponents
            .Where(c => c.ProjectId == projectId)
            .OrderByDescending(c => c.UpdatedUtc)
            .Select(c => new
            {
                c.Id,
                c.ProjectId,
                c.FigmaFileId,
                c.FigmaComponentKey,
                c.FigmaComponentId,
                c.FigmaComponentSetId,
                c.Name,
                c.Description,
                c.ThumbnailUrl,
                c.FrameWidth,
                c.FrameHeight,
                c.CreatedUtc,
                c.UpdatedUtc,
                VariantCount = db.LibraryComponentVariants.Count(v => v.LibraryComponentId == c.Id),
                TextFieldCount = db.LibraryComponentTextFields.Count(tf =>
                    db.LibraryComponentVariants.Where(v => v.LibraryComponentId == c.Id).Select(v => v.Id).Contains(tf.LibraryComponentVariantId))
            })
            .ToListAsync(ct);

        return Ok(components);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var component = await db.LibraryComponents
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (component is null) return NotFound();

        var variants = await db.LibraryComponentVariants
            .Where(v => v.LibraryComponentId == id)
            .OrderBy(v => v.VariantName)
            .ToListAsync(ct);

        var variantIds = variants.Select(v => v.Id).ToList();

        var textFields = await db.LibraryComponentTextFields
            .Where(tf => variantIds.Contains(tf.LibraryComponentVariantId))
            .OrderBy(tf => tf.Y).ThenBy(tf => tf.X)
            .ToListAsync(ct);

        return Ok(new
        {
            component.Id,
            component.ProjectId,
            component.FigmaFileId,
            component.FigmaComponentKey,
            component.FigmaComponentId,
            component.FigmaComponentSetId,
            component.Name,
            component.Description,
            component.ThumbnailUrl,
            component.FrameWidth,
            component.FrameHeight,
            component.CreatedUtc,
            component.UpdatedUtc,
            Variants = variants.Select(v => new
            {
                v.Id,
                v.LibraryComponentId,
                v.FigmaNodeId,
                v.VariantName,
                v.VariantProperties,
                v.BackgroundColor,
                v.FrameWidth,
                v.FrameHeight,
                v.CreatedUtc,
                v.UpdatedUtc,
                TextFields = textFields
                    .Where(tf => tf.LibraryComponentVariantId == v.Id)
                    .Select(tf => new
                    {
                        tf.Id,
                        tf.LibraryComponentVariantId,
                        tf.FigmaLayerId,
                        tf.FigmaLayerName,
                        tf.CurrentText,
                        tf.ContentItemId,
                        tf.X,
                        tf.Y,
                        tf.Width,
                        tf.Height,
                        tf.FontFamily,
                        tf.FontSize,
                        tf.FontWeight,
                        tf.TextAlign,
                        tf.Color,
                        tf.CreatedUtc,
                        tf.UpdatedUtc
                    })
            })
        });
    }

    [HttpPost]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Create([FromBody] CreateLibraryComponentRequest request, CancellationToken ct)
    {
        // Upsert: check if component already exists by projectId + fileId + componentKey
        var existing = await db.LibraryComponents.FirstOrDefaultAsync(
            c => c.ProjectId == request.ProjectId
                && c.FigmaFileId == request.FigmaFileId
                && c.FigmaComponentKey == request.FigmaComponentKey,
            ct);

        if (existing is not null)
        {
            existing.FigmaComponentId = request.FigmaComponentId;
            existing.FigmaComponentSetId = request.FigmaComponentSetId;
            existing.Name = request.Name;
            existing.Description = request.Description ?? existing.Description;
            existing.ThumbnailUrl = request.ThumbnailUrl ?? existing.ThumbnailUrl;
            existing.FrameWidth = request.FrameWidth;
            existing.FrameHeight = request.FrameHeight;
            existing.UpdatedUtc = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);
            return Ok(existing);
        }

        var component = new LibraryComponent
        {
            ProjectId = request.ProjectId,
            FigmaFileId = request.FigmaFileId,
            FigmaComponentKey = request.FigmaComponentKey,
            FigmaComponentId = request.FigmaComponentId,
            FigmaComponentSetId = request.FigmaComponentSetId,
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            ThumbnailUrl = request.ThumbnailUrl ?? string.Empty,
            FrameWidth = request.FrameWidth,
            FrameHeight = request.FrameHeight
        };

        db.LibraryComponents.Add(component);
        await db.SaveChangesAsync(ct);

        return Ok(component);
    }

    [HttpPut("{id:guid}")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLibraryComponentRequest request, CancellationToken ct)
    {
        var component = await db.LibraryComponents
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (component is null) return NotFound();

        if (request.Name is not null) component.Name = request.Name;
        if (request.Description is not null) component.Description = request.Description;
        if (request.ThumbnailUrl is not null) component.ThumbnailUrl = request.ThumbnailUrl;
        if (request.FrameWidth.HasValue) component.FrameWidth = request.FrameWidth.Value;
        if (request.FrameHeight.HasValue) component.FrameHeight = request.FrameHeight.Value;
        component.UpdatedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(component);
    }

    [HttpDelete("{id:guid}")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var component = await db.LibraryComponents
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (component is null) return NotFound();

        db.LibraryComponents.Remove(component);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
