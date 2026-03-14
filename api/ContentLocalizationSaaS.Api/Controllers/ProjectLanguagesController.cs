using System.Text.RegularExpressions;
using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record AddProjectLanguageRequest(Guid ProjectId, string Bcp47Code, bool IsSource = false);
public sealed record ToggleProjectLanguageRequest(bool IsActive);
public sealed record ChangeSourceLanguageRequest(string Bcp47Code);

[ApiController]
[Route("api/project-languages")]
public sealed class ProjectLanguagesController(AppDbContext db) : ControllerBase
{
    private static readonly Regex Bcp47Regex = new("^[A-Za-z]{2,3}(-[A-Za-z0-9]{2,8})*$", RegexOptions.Compiled);

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid projectId, CancellationToken cancellationToken)
    {
        var rows = await db.ProjectLanguages.Where(x => x.ProjectId == projectId).OrderBy(x => x.Bcp47Code).ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpPost]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Add([FromBody] AddProjectLanguageRequest request, CancellationToken cancellationToken)
    {
        if (request.ProjectId == Guid.Empty || string.IsNullOrWhiteSpace(request.Bcp47Code) || !Bcp47Regex.IsMatch(request.Bcp47Code))
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["bcp47Code"] = ["Valid BCP-47 code is required."]
            }));
        }

        var normalized = request.Bcp47Code.Trim();
        var exists = await db.ProjectLanguages.AnyAsync(x => x.ProjectId == request.ProjectId && x.Bcp47Code == normalized, cancellationToken);
        if (exists) return Conflict(new { error = "language_exists" });

        if (request.IsSource)
        {
            var existingSource = await db.ProjectLanguages.Where(x => x.ProjectId == request.ProjectId && x.IsSource).ToListAsync(cancellationToken);
            foreach (var src in existingSource) src.IsSource = false;
        }

        var language = new ProjectLanguage
        {
            ProjectId = request.ProjectId,
            Bcp47Code = normalized,
            IsSource = request.IsSource,
            IsActive = true
        };

        db.ProjectLanguages.Add(language);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(language);
    }

    [HttpPut("{id:guid}/active")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> ToggleActive(Guid id, [FromBody] ToggleProjectLanguageRequest request, CancellationToken cancellationToken)
    {
        var row = await db.ProjectLanguages.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (row is null) return NotFound();

        row.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(row);
    }

    [HttpPost("source-language")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> ChangeSource([FromBody] ChangeSourceLanguageRequest request, [FromQuery] Guid projectId, CancellationToken cancellationToken)
    {
        if (projectId == Guid.Empty || string.IsNullOrWhiteSpace(request.Bcp47Code))
            return BadRequest(new { error = "project_and_language_required" });

        var source = await db.ProjectLanguages.FirstOrDefaultAsync(x => x.ProjectId == projectId && x.Bcp47Code == request.Bcp47Code, cancellationToken);
        if (source is null) return NotFound(new { error = "source_language_not_found" });

        var others = await db.ProjectLanguages.Where(x => x.ProjectId == projectId && x.IsSource).ToListAsync(cancellationToken);
        foreach (var l in others) l.IsSource = false;
        source.IsSource = true;

        await db.SaveChangesAsync(cancellationToken);

        return Ok(new { status = "source_language_changed", warning = "Ensure translation tasks are reviewed after source language change." });
    }
}
