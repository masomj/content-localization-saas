using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Application;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/tone-config")]
public sealed class ProjectToneConfigController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid projectId, CancellationToken ct)
    {
        var config = await db.ProjectToneConfigs
            .Where(c => c.ProjectId == projectId && c.IsActive)
            .OrderByDescending(c => c.CreatedUtc)
            .FirstOrDefaultAsync(ct);

        if (config is null) return NotFound();
        return Ok(config);
    }

    [HttpPost]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Create(Guid projectId, [FromBody] UpsertToneConfigRequest request, CancellationToken ct)
    {
        // Deactivate any existing config
        var existing = await db.ProjectToneConfigs
            .Where(c => c.ProjectId == projectId && c.IsActive)
            .ToListAsync(ct);

        foreach (var e in existing)
            e.IsActive = false;

        var config = new ProjectToneConfig
        {
            ProjectId = projectId,
            ToneDescription = request.ToneDescription,
            IsActive = true,
        };

        db.ProjectToneConfigs.Add(config);
        await db.SaveChangesAsync(ct);
        return Ok(config);
    }

    [HttpPut]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Update(Guid projectId, [FromBody] UpsertToneConfigRequest request, CancellationToken ct)
    {
        var config = await db.ProjectToneConfigs
            .Where(c => c.ProjectId == projectId && c.IsActive)
            .FirstOrDefaultAsync(ct);

        if (config is null) return NotFound();

        config.ToneDescription = request.ToneDescription;
        await db.SaveChangesAsync(ct);
        return Ok(config);
    }

    [HttpDelete]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> Deactivate(Guid projectId, CancellationToken ct)
    {
        var configs = await db.ProjectToneConfigs
            .Where(c => c.ProjectId == projectId && c.IsActive)
            .ToListAsync(ct);

        foreach (var c in configs)
            c.IsActive = false;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public sealed record UpsertToneConfigRequest(string ToneDescription);
