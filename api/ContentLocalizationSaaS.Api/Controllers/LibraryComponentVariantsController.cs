using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record CreateLibraryVariantRequest(
    string FigmaNodeId,
    string VariantName,
    string? VariantProperties);

[ApiController]
[Route("api/library-components/{componentId:guid}/variants")]
[Microsoft.AspNetCore.Cors.EnableCors("PluginCors")]
public sealed class LibraryComponentVariantsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid componentId, CancellationToken ct)
    {
        var component = await db.LibraryComponents.FirstOrDefaultAsync(c => c.Id == componentId, ct);
        if (component is null) return NotFound();

        var variants = await db.LibraryComponentVariants
            .Where(v => v.LibraryComponentId == componentId)
            .OrderBy(v => v.VariantName)
            .Select(v => new
            {
                v.Id,
                v.LibraryComponentId,
                v.FigmaNodeId,
                v.VariantName,
                v.VariantProperties,
                v.FrameWidth,
                v.FrameHeight,
                v.CreatedUtc,
                v.UpdatedUtc,
                TextFieldCount = db.LibraryComponentTextFields.Count(tf => tf.LibraryComponentVariantId == v.Id)
            })
            .ToListAsync(ct);

        return Ok(variants);
    }

    [HttpGet("{variantId:guid}")]
    public async Task<IActionResult> Get(Guid componentId, Guid variantId, CancellationToken ct)
    {
        var variant = await db.LibraryComponentVariants
            .FirstOrDefaultAsync(v => v.Id == variantId && v.LibraryComponentId == componentId, ct);

        if (variant is null) return NotFound();

        var textFields = await db.LibraryComponentTextFields
            .Where(tf => tf.LibraryComponentVariantId == variantId)
            .OrderBy(tf => tf.Y).ThenBy(tf => tf.X)
            .ToListAsync(ct);

        return Ok(new
        {
            variant.Id,
            variant.LibraryComponentId,
            variant.FigmaNodeId,
            variant.VariantName,
            variant.VariantProperties,
            variant.FrameWidth,
            variant.FrameHeight,
            variant.CreatedUtc,
            variant.UpdatedUtc,
            TextFields = textFields.Select(tf => new
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
        });
    }

    [HttpPost]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Create(Guid componentId, [FromBody] CreateLibraryVariantRequest request, CancellationToken ct)
    {
        var component = await db.LibraryComponents.FirstOrDefaultAsync(c => c.Id == componentId, ct);
        if (component is null) return NotFound();

        // Upsert: check if variant already exists by componentId + figmaNodeId
        var existing = await db.LibraryComponentVariants.FirstOrDefaultAsync(
            v => v.LibraryComponentId == componentId && v.FigmaNodeId == request.FigmaNodeId,
            ct);

        if (existing is not null)
        {
            existing.VariantName = request.VariantName;
            existing.VariantProperties = request.VariantProperties ?? existing.VariantProperties;
            existing.UpdatedUtc = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);
            return Ok(existing);
        }

        var variant = new LibraryComponentVariant
        {
            LibraryComponentId = componentId,
            FigmaNodeId = request.FigmaNodeId,
            VariantName = request.VariantName,
            VariantProperties = request.VariantProperties ?? string.Empty
        };

        db.LibraryComponentVariants.Add(variant);
        await db.SaveChangesAsync(ct);

        return Ok(variant);
    }

    [HttpDelete("{variantId:guid}")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Delete(Guid componentId, Guid variantId, CancellationToken ct)
    {
        var variant = await db.LibraryComponentVariants
            .FirstOrDefaultAsync(v => v.Id == variantId && v.LibraryComponentId == componentId, ct);

        if (variant is null) return NotFound();

        db.LibraryComponentVariants.Remove(variant);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
