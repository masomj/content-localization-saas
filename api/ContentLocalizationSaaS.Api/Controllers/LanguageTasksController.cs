using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record UpsertLanguageTaskRequest(Guid ContentItemId, string LanguageCode, string? AssigneeEmail, DateTime? DueUtc, string Status, string? TranslationText);
public sealed record ApplyTranslationMemoryRequest(Guid ContentItemId, string LanguageCode, bool AcceptSuggestion, string? ManualTranslationText);

[ApiController]
[Route("api/language-tasks")]
public sealed class LanguageTasksController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? contentItemId, [FromQuery] bool overdueOnly = false, CancellationToken cancellationToken = default)
    {
        var query = db.ContentItemLanguageTasks.AsQueryable();
        if (contentItemId.HasValue) query = query.Where(x => x.ContentItemId == contentItemId.Value);
        if (overdueOnly) query = query.Where(x => x.DueUtc.HasValue && x.DueUtc < DateTime.UtcNow && x.Status != "done");

        var rows = await query.OrderBy(x => x.LanguageCode).ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> Suggestions([FromQuery] Guid contentItemId, [FromQuery] string languageCode, CancellationToken cancellationToken)
    {
        var item = await db.ContentItems.FirstOrDefaultAsync(x => x.Id == contentItemId, cancellationToken);
        if (item is null) return NotFound();

        var suggestion = await db.TranslationMemoryEntries
            .Where(x => x.ProjectId == item.ProjectId && x.LanguageCode == languageCode && x.SourceText == item.Source && x.IsApproved)
            .OrderByDescending(x => x.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new
        {
            hasSuggestion = suggestion is not null,
            suggestion = suggestion is null ? null : new { suggestion.Id, suggestion.TranslationText, suggestion.CreatedUtc }
        });
    }

    [HttpPost]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Upsert([FromBody] UpsertLanguageTaskRequest request, CancellationToken cancellationToken)
    {
        if (request.ContentItemId == Guid.Empty || string.IsNullOrWhiteSpace(request.LanguageCode) || string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(new { error = "contentItemId, languageCode and status required" });

        var normalizedStatus = request.Status.Trim().ToLowerInvariant();

        var existing = await db.ContentItemLanguageTasks
            .FirstOrDefaultAsync(x => x.ContentItemId == request.ContentItemId && x.LanguageCode == request.LanguageCode, cancellationToken);

        if (existing is null)
        {
            existing = new ContentItemLanguageTask
            {
                ContentItemId = request.ContentItemId,
                LanguageCode = request.LanguageCode,
                AssigneeEmail = request.AssigneeEmail?.Trim().ToLowerInvariant() ?? string.Empty,
                TranslationText = request.TranslationText?.Trim() ?? string.Empty,
                DueUtc = request.DueUtc,
                Status = normalizedStatus
            };
            db.ContentItemLanguageTasks.Add(existing);
        }
        else
        {
            existing.AssigneeEmail = request.AssigneeEmail?.Trim().ToLowerInvariant() ?? string.Empty;
            existing.TranslationText = request.TranslationText?.Trim() ?? existing.TranslationText;
            existing.DueUtc = request.DueUtc;
            existing.Status = normalizedStatus;
        }

        var item = await db.ContentItems.FirstOrDefaultAsync(x => x.Id == existing.ContentItemId, cancellationToken);

        if (string.Equals(existing.Status, "pending_review", StringComparison.OrdinalIgnoreCase) && item is not null)
        {
            item.Status = "in_review";
            item.ApprovedUtc = null;
            item.ApprovedByEmail = string.Empty;
            item.RejectionReason = string.Empty;
        }

        if (string.Equals(existing.Status, "approved", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(existing.Status, "done", StringComparison.OrdinalIgnoreCase))
        {
            existing.IsOutdated = false;

            if (item is not null && !string.IsNullOrWhiteSpace(existing.TranslationText))
            {
                db.TranslationMemoryEntries.Add(new TranslationMemoryEntry
                {
                    ProjectId = item.ProjectId,
                    SourceText = item.Source,
                    LanguageCode = existing.LanguageCode,
                    TranslationText = existing.TranslationText,
                    IsApproved = true
                });
            }
        }

        // Check for forbidden terms and set RequiresReview flag
        if (!string.IsNullOrWhiteSpace(existing.TranslationText) && item is not null)
        {
            var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == item.ProjectId, cancellationToken);
            if (project is not null)
            {
                var glossaryIds = await db.Glossaries
                    .Where(g => g.WorkspaceId == project.WorkspaceId)
                    .Select(g => g.Id)
                    .ToListAsync(cancellationToken);

                if (glossaryIds.Count > 0)
                {
                    var hasForbidden = await db.GlossaryTerms
                        .Where(t => glossaryIds.Contains(t.GlossaryId) && t.IsForbidden)
                        .AnyAsync(t => EF.Functions.ILike(existing.TranslationText, "%" + t.SourceTerm + "%"), cancellationToken);

                    existing.RequiresReview = hasForbidden;
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(existing);
    }

    [HttpPost("apply-memory")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> ApplyTranslationMemory([FromBody] ApplyTranslationMemoryRequest request, CancellationToken cancellationToken)
    {
        var item = await db.ContentItems.FirstOrDefaultAsync(x => x.Id == request.ContentItemId, cancellationToken);
        if (item is null) return NotFound();

        var task = await db.ContentItemLanguageTasks
            .FirstOrDefaultAsync(x => x.ContentItemId == request.ContentItemId && x.LanguageCode == request.LanguageCode, cancellationToken);

        if (task is null)
        {
            task = new ContentItemLanguageTask
            {
                ContentItemId = request.ContentItemId,
                LanguageCode = request.LanguageCode,
                Status = "todo"
            };
            db.ContentItemLanguageTasks.Add(task);
        }

        if (request.AcceptSuggestion)
        {
            var memory = await db.TranslationMemoryEntries
                .Where(x => x.ProjectId == item.ProjectId && x.LanguageCode == request.LanguageCode && x.SourceText == item.Source && x.IsApproved)
                .OrderByDescending(x => x.CreatedUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (memory is null) return BadRequest(new { error = "no_memory_suggestion" });

            task.TranslationText = memory.TranslationText;
            task.Status = "pending_review";
            item.Status = "in_review";
            item.ApprovedUtc = null;
            item.ApprovedByEmail = string.Empty;
            item.RejectionReason = string.Empty;
            await db.SaveChangesAsync(cancellationToken);
            return Ok(new { status = "suggestion_applied", task.TranslationText, task.Status });
        }

        if (string.IsNullOrWhiteSpace(request.ManualTranslationText))
        {
            return BadRequest(new { error = "manual_translation_required" });
        }

        task.TranslationText = request.ManualTranslationText.Trim();
        task.Status = "pending_review";
        item.Status = "in_review";
        item.ApprovedUtc = null;
        item.ApprovedByEmail = string.Empty;
        item.RejectionReason = string.Empty;

        db.TranslationMemoryEntries.Add(new TranslationMemoryEntry
        {
            ProjectId = item.ProjectId,
            SourceText = item.Source,
            LanguageCode = request.LanguageCode,
            TranslationText = task.TranslationText,
            IsApproved = false
        });

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { status = "manual_translation_recorded", task.TranslationText, task.Status, memoryCandidate = true });
    }
}
