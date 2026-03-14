using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record CreateSavedFilterPresetRequest(Guid ProjectId, string Name, string? Query, string? Tags, string? Status);

[ApiController]
[Route("api/content-items/filter-presets")]
public sealed class SavedFilterPresetsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? projectId, CancellationToken cancellationToken)
    {
        var query = db.SavedFilterPresets.AsQueryable();
        if (projectId.HasValue) query = query.Where(x => x.ProjectId == projectId.Value);

        var rows = await query.OrderByDescending(x => x.CreatedUtc).ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpPost]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Create([FromBody] CreateSavedFilterPresetRequest request, CancellationToken cancellationToken)
    {
        if (request.ProjectId == Guid.Empty || string.IsNullOrWhiteSpace(request.Name))
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = ["projectId and name are required"]
            }));
        }

        var preset = new SavedFilterPreset
        {
            ProjectId = request.ProjectId,
            Name = request.Name.Trim(),
            Query = request.Query?.Trim() ?? string.Empty,
            Tags = request.Tags?.Trim() ?? string.Empty,
            Status = request.Status?.Trim() ?? string.Empty,
        };

        db.SavedFilterPresets.Add(preset);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(preset);
    }
}
