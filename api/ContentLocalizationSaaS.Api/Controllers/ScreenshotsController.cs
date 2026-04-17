using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Application.Abstractions;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/screenshots")]
public sealed class ScreenshotsController(AppDbContext db, IOcrService ocr, IWebHostEnvironment env, IServiceScopeFactory scopeFactory) : ControllerBase
{
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/webp"
    };

    private static readonly Dictionary<string, string> MimeToExt = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/png"] = ".png",
        ["image/jpeg"] = ".jpg",
        ["image/webp"] = ".webp"
    };

    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

    [HttpPost]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Upload(Guid projectId, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        if (file.Length > MaxFileSize)
            return BadRequest(new { error = "File exceeds 10MB limit." });

        var contentType = file.ContentType;
        if (!AllowedMimeTypes.Contains(contentType))
            return BadRequest(new { error = "Only PNG, JPG, and WebP files are accepted." });

        // Verify project exists
        var projectExists = await db.Projects.AnyAsync(p => p.Id == projectId, ct);
        if (!projectExists) return NotFound(new { error = "Project not found." });

        var screenshotId = Guid.NewGuid();
        var ext = MimeToExt.GetValueOrDefault(contentType, ".png");
        var relativePath = $"screenshots/{projectId}/{screenshotId}{ext}";
        var fullPath = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream, ct);
        }

        var screenshot = new Screenshot
        {
            Id = screenshotId,
            ProjectId = projectId,
            FileName = file.FileName,
            StoragePath = relativePath,
            MimeType = contentType,
            FileSizeBytes = file.Length,
            OcrStatus = "pending"
        };

        db.Screenshots.Add(screenshot);
        await db.SaveChangesAsync(ct);

        // Fire-and-forget OCR processing
        _ = Task.Run(async () =>
        {
            try
            {
                var results = await ocr.ProcessAsync(fullPath);

                // Fuzzy-match detected text against project content items
                using var scope = scopeFactory.CreateScope();
                var scopedDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var contentItems = await scopedDb.ContentItems
                    .Where(ci => ci.ProjectId == projectId)
                    .Select(ci => new { ci.Id, ci.Key, ci.Source })
                    .ToListAsync();

                foreach (var result in results)
                {
                    Guid? matchedContentItemId = null;
                    bool isManualLink = false;

                    // Auto-link: fuzzy match against content items
                    if (result.Confidence > 0.85)
                    {
                        var match = contentItems.FirstOrDefault(ci =>
                            ci.Source.Contains(result.Text, StringComparison.OrdinalIgnoreCase) ||
                            ci.Key.Contains(result.Text, StringComparison.OrdinalIgnoreCase));
                        if (match is not null)
                            matchedContentItemId = match.Id;
                    }

                    var region = new ScreenshotRegion
                    {
                        ScreenshotId = screenshotId,
                        ContentItemId = matchedContentItemId,
                        DetectedText = result.Text,
                        X = result.X,
                        Y = result.Y,
                        Width = result.Width,
                        Height = result.Height,
                        Confidence = result.Confidence,
                        IsManualLink = isManualLink
                    };
                    scopedDb.ScreenshotRegions.Add(region);
                }

                var entity = await scopedDb.Screenshots.FindAsync(screenshotId);
                if (entity is not null)
                    entity.OcrStatus = "completed";

                await scopedDb.SaveChangesAsync();
            }
            catch
            {
                // Best-effort: mark as failed
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var scopedDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var entity = await scopedDb.Screenshots.FindAsync(screenshotId);
                    if (entity is not null)
                    {
                        entity.OcrStatus = "failed";
                        await scopedDb.SaveChangesAsync();
                    }
                }
                catch { /* swallow */ }
            }
        });

        return Ok(new
        {
            screenshot.Id,
            screenshot.ProjectId,
            screenshot.FileName,
            screenshot.StoragePath,
            screenshot.MimeType,
            screenshot.FileSizeBytes,
            screenshot.Width,
            screenshot.Height,
            screenshot.OcrStatus,
            screenshot.CreatedUtc
        });
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid projectId, CancellationToken ct)
    {
        var screenshots = await db.Screenshots
            .Where(s => s.ProjectId == projectId)
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
                RegionCount = db.ScreenshotRegions.Count(r => r.ScreenshotId == s.Id)
            })
            .ToListAsync(ct);

        return Ok(screenshots);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid projectId, Guid id, CancellationToken ct)
    {
        var screenshot = await db.Screenshots
            .FirstOrDefaultAsync(s => s.Id == id && s.ProjectId == projectId, ct);

        if (screenshot is null) return NotFound();

        var regions = await db.ScreenshotRegions
            .Where(r => r.ScreenshotId == id)
            .OrderBy(r => r.Y).ThenBy(r => r.X)
            .Select(r => new
            {
                r.Id,
                r.ScreenshotId,
                r.ContentItemId,
                r.DetectedText,
                r.X,
                r.Y,
                r.Width,
                r.Height,
                r.Confidence,
                r.IsManualLink,
                r.CreatedUtc,
                ContentItemKey = r.ContentItemId != null
                    ? db.ContentItems.Where(ci => ci.Id == r.ContentItemId).Select(ci => ci.Key).FirstOrDefault()
                    : null
            })
            .ToListAsync(ct);

        return Ok(new
        {
            screenshot.Id,
            screenshot.ProjectId,
            screenshot.FileName,
            screenshot.StoragePath,
            screenshot.MimeType,
            screenshot.FileSizeBytes,
            screenshot.Width,
            screenshot.Height,
            screenshot.OcrStatus,
            screenshot.CreatedUtc,
            Regions = regions
        });
    }

    [HttpDelete("{id:guid}")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Delete(Guid projectId, Guid id, CancellationToken ct)
    {
        var screenshot = await db.Screenshots
            .FirstOrDefaultAsync(s => s.Id == id && s.ProjectId == projectId, ct);

        if (screenshot is null) return NotFound();

        // Delete file from disk
        var fullPath = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), screenshot.StoragePath);
        if (System.IO.File.Exists(fullPath))
            System.IO.File.Delete(fullPath);

        db.Screenshots.Remove(screenshot);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
