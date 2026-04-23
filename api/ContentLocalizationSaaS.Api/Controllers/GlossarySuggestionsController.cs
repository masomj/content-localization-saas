using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/glossary-suggestions")]
public sealed class GlossarySuggestionsController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Suggest([FromBody] SuggestRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SourceText))
            return Ok(Array.Empty<object>());

        var workspaceId = request.WorkspaceId
            ?? (Guid.TryParse(Request.Headers["X-Workspace-Id"].FirstOrDefault(), out var wsId) ? wsId : (Guid?)null);

        if (workspaceId is null)
            return BadRequest("WorkspaceId is required.");

        // Get all glossary IDs for this workspace
        var glossaryIds = await db.Glossaries
            .Where(g => g.WorkspaceId == workspaceId.Value)
            .Select(g => new { g.Id, g.Name })
            .ToListAsync(ct);

        if (glossaryIds.Count == 0)
            return Ok(Array.Empty<object>());

        var gIds = glossaryIds.Select(g => g.Id).ToList();
        var glossaryNames = glossaryIds.ToDictionary(g => g.Id, g => g.Name);

        // Find terms whose SourceTerm appears in the sourceText (case-insensitive)
        var sourceTextLower = request.SourceText.ToLowerInvariant();

        var matchingTerms = await db.GlossaryTerms
            .Where(t => gIds.Contains(t.GlossaryId))
            .Where(t => EF.Functions.ILike(request.SourceText, "%" + t.SourceTerm + "%"))
            .ToListAsync(ct);

        if (matchingTerms.Count == 0)
            return Ok(Array.Empty<object>());

        var termIds = matchingTerms.Select(t => t.Id).ToList();
        var translations = await db.GlossaryTermTranslations
            .Where(tt => termIds.Contains(tt.GlossaryTermId) && tt.LanguageCode == request.LanguageCode)
            .ToListAsync(ct);

        var translationMap = translations.ToDictionary(tt => tt.GlossaryTermId, tt => tt.TranslatedTerm);

        var results = matchingTerms.Select(t => new
        {
            Term = t.SourceTerm,
            t.Definition,
            TranslatedTerm = translationMap.GetValueOrDefault(t.Id, ""),
            GlossaryName = glossaryNames.GetValueOrDefault(t.GlossaryId, ""),
        });

        return Ok(results);
    }
}

public sealed record SuggestRequest(string SourceText, string LanguageCode, Guid? WorkspaceId);
