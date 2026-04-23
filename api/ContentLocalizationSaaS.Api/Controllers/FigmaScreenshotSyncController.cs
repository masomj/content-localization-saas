using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

/// <summary>
/// EP4-S4: Figma Screenshot Sync — manage connections and trigger syncs.
/// </summary>
[ApiController]
[Route("api/projects/{projectId:guid}/figma-sync")]
public sealed class FigmaScreenshotSyncController(AppDbContext db) : ControllerBase
{
    public sealed record ConnectRequest(string FigmaFileKey);

    [HttpPost("connect")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Connect(Guid projectId, [FromBody] ConnectRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FigmaFileKey))
            return BadRequest(new { error = "FigmaFileKey is required." });

        var projectExists = await db.Projects.AnyAsync(p => p.Id == projectId, ct);
        if (!projectExists) return NotFound(new { error = "Project not found." });

        var sync = new FigmaScreenshotSync
        {
            ProjectId = projectId,
            FigmaFileKey = request.FigmaFileKey.Trim(),
            FigmaFileName = request.FigmaFileKey.Trim(), // Placeholder — real Figma API would resolve name
            SyncStatus = "idle"
        };

        db.FigmaScreenshotSyncs.Add(sync);
        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            sync.Id,
            sync.ProjectId,
            sync.FigmaFileKey,
            sync.FigmaFileName,
            sync.LastSyncUtc,
            sync.SyncStatus,
            sync.FrameCount,
            sync.CreatedUtc
        });
    }

    [HttpPost("{syncId:guid}/sync")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Sync(Guid projectId, Guid syncId, CancellationToken ct)
    {
        var sync = await db.FigmaScreenshotSyncs
            .FirstOrDefaultAsync(s => s.Id == syncId && s.ProjectId == projectId, ct);

        if (sync is null) return NotFound();

        // TODO: MVP stub — in production this would call Figma REST API to enumerate frames,
        // download rendered images, and create Screenshot entities for each frame.
        // For now we just update the sync metadata.
        sync.SyncStatus = "completed";
        sync.LastSyncUtc = DateTime.UtcNow;
        sync.FrameCount = 0; // Stub: no frames fetched

        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            sync.Id,
            sync.ProjectId,
            sync.FigmaFileKey,
            sync.FigmaFileName,
            sync.LastSyncUtc,
            sync.SyncStatus,
            sync.FrameCount,
            sync.CreatedUtc
        });
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid projectId, CancellationToken ct)
    {
        var syncs = await db.FigmaScreenshotSyncs
            .Where(s => s.ProjectId == projectId)
            .OrderByDescending(s => s.CreatedUtc)
            .Select(s => new
            {
                s.Id,
                s.ProjectId,
                s.FigmaFileKey,
                s.FigmaFileName,
                s.LastSyncUtc,
                s.SyncStatus,
                s.FrameCount,
                s.CreatedUtc
            })
            .ToListAsync(ct);

        return Ok(syncs);
    }

    [HttpDelete("{syncId:guid}")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Delete(Guid projectId, Guid syncId, CancellationToken ct)
    {
        var sync = await db.FigmaScreenshotSyncs
            .FirstOrDefaultAsync(s => s.Id == syncId && s.ProjectId == projectId, ct);

        if (sync is null) return NotFound();

        db.FigmaScreenshotSyncs.Remove(sync);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
