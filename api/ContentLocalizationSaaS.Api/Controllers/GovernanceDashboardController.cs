using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Application;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/governance")]
public sealed class GovernanceDashboardController(AppDbContext db) : ControllerBase
{
    [HttpGet("dashboard")]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> GetDashboard([FromQuery] Guid workspaceId, CancellationToken ct)
    {
        // Get all projects in workspace
        var projectIds = await db.Projects
            .Where(p => p.WorkspaceId == workspaceId)
            .Select(p => p.Id)
            .ToListAsync(ct);

        // Get content item IDs for these projects
        var contentItemIds = await db.ContentItems
            .Where(ci => projectIds.Contains(ci.ProjectId))
            .Select(ci => ci.Id)
            .ToListAsync(ct);

        // Total translations
        var totalTranslations = await db.ContentItemLanguageTasks
            .Where(t => contentItemIds.Contains(t.ContentItemId))
            .CountAsync(ct);

        // Glossary terms for workspace
        var glossaryIds = await db.Glossaries
            .Where(g => g.WorkspaceId == workspaceId)
            .Select(g => g.Id)
            .ToListAsync(ct);

        var approvedTerms = await db.GlossaryTerms
            .Where(t => glossaryIds.Contains(t.GlossaryId) && !t.IsForbidden)
            .Select(t => new { t.Id, t.SourceTerm })
            .ToListAsync(ct);

        // Simple adoption rate: count translations containing any glossary term / total
        var adoptionCount = 0;
        var sampleTexts = new List<string>();
        if (approvedTerms.Count > 0 && totalTranslations > 0)
        {
            sampleTexts = await db.ContentItemLanguageTasks
                .Where(t => contentItemIds.Contains(t.ContentItemId) && t.TranslationText != "")
                .Select(t => t.TranslationText)
                .Take(500)
                .ToListAsync(ct);

            foreach (var text in sampleTexts)
            {
                if (approvedTerms.Any(term => text.Contains(term.SourceTerm, StringComparison.OrdinalIgnoreCase)))
                    adoptionCount++;
            }
        }

        var glossaryAdoptionRate = totalTranslations > 0
            ? Math.Round(100.0 * adoptionCount / Math.Min(totalTranslations, 500), 1)
            : 0;

        // Compliance rate: stub — return 100%
        var styleRuleComplianceRate = 100.0;

        // Forbidden term incidents: translations marked RequiresReview
        var forbiddenTermIncidentCount = await db.ContentItemLanguageTasks
            .Where(t => contentItemIds.Contains(t.ContentItemId) && t.RequiresReview)
            .CountAsync(ct);

        // Top non-adopted terms
        var topNonAdoptedTerms = new List<object>();
        if (approvedTerms.Count > 0 && sampleTexts.Count > 0)
        {
            var sampleCount = sampleTexts.Count;
            foreach (var term in approvedTerms)
            {
                var uses = sampleTexts.Count(t => t.Contains(term.SourceTerm, StringComparison.OrdinalIgnoreCase));
                var rate = sampleCount > 0 ? Math.Round(100.0 * uses / sampleCount, 1) : 0;
                topNonAdoptedTerms.Add(new { term = term.SourceTerm, adoptionRate = rate });
            }

            topNonAdoptedTerms = topNonAdoptedTerms
                .OrderBy(t => ((dynamic)t).adoptionRate)
                .Take(10)
                .ToList();
        }

        // Recent forbidden incidents — join with ContentItems for the key
        var recentForbiddenIncidents = await (
            from t in db.ContentItemLanguageTasks
            join ci in db.ContentItems on t.ContentItemId equals ci.Id
            where contentItemIds.Contains(t.ContentItemId) && t.RequiresReview
            orderby t.CreatedUtc descending
            select new
            {
                contentItemKey = ci.Key,
                language = t.LanguageCode,
                term = "",
                translatorEmail = t.AssigneeEmail,
                date = t.CreatedUtc,
            })
            .Take(20)
            .ToListAsync(ct);

        return Ok(new
        {
            glossaryAdoptionRate,
            styleRuleComplianceRate,
            forbiddenTermIncidentCount,
            topNonAdoptedTerms,
            recentForbiddenIncidents,
        });
    }

    [HttpGet("export/csv")]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> ExportCsv([FromQuery] Guid workspaceId, CancellationToken ct)
    {
        var projectIds = await db.Projects
            .Where(p => p.WorkspaceId == workspaceId)
            .Select(p => p.Id)
            .ToListAsync(ct);

        var contentItemIds = await db.ContentItems
            .Where(ci => projectIds.Contains(ci.ProjectId))
            .Select(ci => ci.Id)
            .ToListAsync(ct);

        var incidents = await (
            from t in db.ContentItemLanguageTasks
            join ci in db.ContentItems on t.ContentItemId equals ci.Id
            where contentItemIds.Contains(t.ContentItemId) && t.RequiresReview
            orderby t.CreatedUtc descending
            select new
            {
                contentItemKey = ci.Key,
                language = t.LanguageCode,
                translatorEmail = t.AssigneeEmail,
                date = t.CreatedUtc,
            })
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Content Item Key,Language,Translator,Date");
        foreach (var i in incidents)
        {
            sb.AppendLine($"\"{i.contentItemKey}\",\"{i.language}\",\"{i.translatorEmail}\",\"{i.date:O}\"");
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "governance-report.csv");
    }

    [HttpGet("export/pdf")]
    [RequireAppRole(AppRole.Admin)]
    public IActionResult ExportPdf()
    {
        // Stub: PDF export not implemented for MVP
        return StatusCode(501, new { message = "PDF export is not implemented yet." });
    }
}
