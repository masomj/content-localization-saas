using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record ForbiddenCheckRequest(string Text, string LanguageCode, Guid WorkspaceId);
public sealed record ForbiddenMatch(string Term, string Replacement, int Position);

[ApiController]
[Route("api/forbidden-check")]
public sealed class ForbiddenCheckController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Check([FromBody] ForbiddenCheckRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return Ok(Array.Empty<ForbiddenMatch>());

        var glossaryIds = await db.Glossaries
            .Where(g => g.WorkspaceId == request.WorkspaceId)
            .Select(g => g.Id)
            .ToListAsync(ct);

        if (glossaryIds.Count == 0)
            return Ok(Array.Empty<ForbiddenMatch>());

        var forbiddenTerms = await db.GlossaryTerms
            .Where(t => glossaryIds.Contains(t.GlossaryId) && t.IsForbidden)
            .Select(t => new { t.SourceTerm, t.ForbiddenReplacement, t.CaseSensitive })
            .ToListAsync(ct);

        var matches = new List<ForbiddenMatch>();
        var textLower = request.Text.ToLowerInvariant();

        foreach (var term in forbiddenTerms)
        {
            var searchText = term.CaseSensitive ? request.Text : textLower;
            var searchTerm = term.CaseSensitive ? term.SourceTerm : term.SourceTerm.ToLowerInvariant();

            var index = 0;
            while ((index = searchText.IndexOf(searchTerm, index, term.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                matches.Add(new ForbiddenMatch(term.SourceTerm, term.ForbiddenReplacement, index));
                index += searchTerm.Length;
            }
        }

        return Ok(matches.OrderBy(m => m.Position));
    }
}
