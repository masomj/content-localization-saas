using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/screenshot-regions")]
public sealed class ScreenshotRegionsController(AppDbContext db) : ControllerBase
{
    public sealed record LinkRequest(Guid ContentItemId);

    [HttpPut("{regionId:guid}/link")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Link(Guid regionId, [FromBody] LinkRequest request, CancellationToken ct)
    {
        var region = await db.ScreenshotRegions.FirstOrDefaultAsync(r => r.Id == regionId, ct);
        if (region is null) return NotFound();

        var contentItemExists = await db.ContentItems.AnyAsync(ci => ci.Id == request.ContentItemId, ct);
        if (!contentItemExists) return BadRequest(new { error = "Content item not found." });

        region.ContentItemId = request.ContentItemId;
        region.IsManualLink = true;
        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            region.Id,
            region.ScreenshotId,
            region.ContentItemId,
            region.DetectedText,
            region.X,
            region.Y,
            region.Width,
            region.Height,
            region.Confidence,
            region.IsManualLink,
            region.CreatedUtc
        });
    }

    [HttpPut("{regionId:guid}/unlink")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Unlink(Guid regionId, CancellationToken ct)
    {
        var region = await db.ScreenshotRegions.FirstOrDefaultAsync(r => r.Id == regionId, ct);
        if (region is null) return NotFound();

        region.ContentItemId = null;
        region.IsManualLink = false;
        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            region.Id,
            region.ScreenshotId,
            region.ContentItemId,
            region.DetectedText,
            region.X,
            region.Y,
            region.Width,
            region.Height,
            region.Confidence,
            region.IsManualLink,
            region.CreatedUtc
        });
    }
}
