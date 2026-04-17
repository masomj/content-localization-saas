using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

/// <summary>
/// EP4-S3: Returns screenshots linked to a content item via screenshot regions.
/// </summary>
[ApiController]
[Route("api/content-items/{contentItemId:guid}/screenshots")]
public sealed class ContentItemScreenshotsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid contentItemId, CancellationToken ct)
    {
        var contentItemExists = await db.ContentItems.AnyAsync(ci => ci.Id == contentItemId, ct);
        if (!contentItemExists) return NotFound(new { error = "Content item not found." });

        // Find all screenshot IDs that have regions linked to this content item
        var linkedRegions = await db.ScreenshotRegions
            .Where(r => r.ContentItemId == contentItemId)
            .ToListAsync(ct);

        var screenshotIds = linkedRegions.Select(r => r.ScreenshotId).Distinct().ToList();

        var screenshots = await db.Screenshots
            .Where(s => screenshotIds.Contains(s.Id))
            .OrderByDescending(s => s.CreatedUtc)
            .Select(s => new
            {
                s.Id,
                s.ProjectId,
                s.FileName,
                s.StoragePath,
                s.MimeType,
                s.FileSizeBytes,
                s.Width,
                s.Height,
                s.OcrStatus,
                s.CreatedUtc,
                LinkedRegions = db.ScreenshotRegions
                    .Where(r => r.ScreenshotId == s.Id && r.ContentItemId == contentItemId)
                    .Select(r => new
                    {
                        r.Id,
                        r.X,
                        r.Y,
                        r.Width,
                        r.Height,
                        r.DetectedText,
                        r.Confidence
                    })
                    .ToList()
            })
            .ToListAsync(ct);

        return Ok(screenshots);
    }
}
