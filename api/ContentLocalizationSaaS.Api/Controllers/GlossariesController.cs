using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Application;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/glossaries")]
public sealed class GlossariesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var workspaceId = Request.Headers["X-Workspace-Id"].FirstOrDefault();
        if (string.IsNullOrEmpty(workspaceId) || !Guid.TryParse(workspaceId, out var wsId))
            return BadRequest("X-Workspace-Id header is required.");

        var glossaries = await db.Glossaries
            .Where(g => g.WorkspaceId == wsId)
            .OrderByDescending(g => g.UpdatedUtc)
            .ToListAsync(ct);

        return Ok(glossaries);
    }

    [HttpPost]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Create([FromBody] CreateGlossaryRequest request, CancellationToken ct)
    {
        var workspaceId = Request.Headers["X-Workspace-Id"].FirstOrDefault();
        if (string.IsNullOrEmpty(workspaceId) || !Guid.TryParse(workspaceId, out var wsId))
            return BadRequest("X-Workspace-Id header is required.");

        var glossary = new Glossary
        {
            WorkspaceId = wsId,
            Name = request.Name,
            Description = request.Description ?? string.Empty,
        };

        db.Glossaries.Add(glossary);
        await db.SaveChangesAsync(ct);
        return Ok(glossary);
    }

    [HttpPut("{id:guid}")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateGlossaryRequest request, CancellationToken ct)
    {
        var glossary = await db.Glossaries.FirstOrDefaultAsync(g => g.Id == id, ct);
        if (glossary is null) return NotFound();

        glossary.Name = request.Name;
        glossary.Description = request.Description ?? string.Empty;
        glossary.UpdatedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(glossary);
    }

    [HttpDelete("{id:guid}")]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var glossary = await db.Glossaries.FirstOrDefaultAsync(g => g.Id == id, ct);
        if (glossary is null) return NotFound();

        // Cascade: delete terms and their translations
        var termIds = await db.GlossaryTerms.Where(t => t.GlossaryId == id).Select(t => t.Id).ToListAsync(ct);
        if (termIds.Count > 0)
        {
            await db.GlossaryTermTranslations.Where(tt => termIds.Contains(tt.GlossaryTermId)).ExecuteDeleteAsync(ct);
            await db.GlossaryTerms.Where(t => t.GlossaryId == id).ExecuteDeleteAsync(ct);
        }

        db.Glossaries.Remove(glossary);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public sealed record CreateGlossaryRequest(string Name, string? Description);
