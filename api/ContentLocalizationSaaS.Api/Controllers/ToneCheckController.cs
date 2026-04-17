using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Application;
using ContentLocalizationSaaS.Application.Abstractions;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/tone-check")]
public sealed class ToneCheckController(AppDbContext db, IToneCheckService toneService) : ControllerBase
{
    [HttpPost]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Check([FromBody] ToneCheckRequest request, CancellationToken ct)
    {
        // Look up tone config for the project
        var config = await db.ProjectToneConfigs
            .Where(c => c.ProjectId == request.ProjectId && c.IsActive)
            .FirstOrDefaultAsync(ct);

        if (config is null)
            return Ok(new { hasMismatch = false, suggestion = "", confidence = 0.0, reasoning = "No tone config found for project." });

        var response = await toneService.CheckAsync(request.Text, config.ToneDescription, request.Language);

        // Persist result
        var result = new ToneCheckResult
        {
            ContentItemLanguageTaskId = request.ContentItemLanguageTaskId,
            OriginalText = request.Text,
            SuggestedText = response.Suggestion,
            ConfidenceScore = response.Confidence,
            Reasoning = response.Reasoning,
            Applied = false,
        };

        db.ToneCheckResults.Add(result);
        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            id = result.Id,
            hasMismatch = response.HasMismatch,
            suggestion = response.Suggestion,
            confidence = response.Confidence,
            reasoning = response.Reasoning,
        });
    }

    [HttpPost("{resultId:guid}/apply")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Apply(Guid resultId, CancellationToken ct)
    {
        var result = await db.ToneCheckResults.FirstOrDefaultAsync(r => r.Id == resultId, ct);
        if (result is null) return NotFound();

        // Apply the suggestion to the translation task
        var task = await db.ContentItemLanguageTasks.FirstOrDefaultAsync(t => t.Id == result.ContentItemLanguageTaskId, ct);
        if (task is null) return NotFound("Translation task not found.");

        task.TranslationText = result.SuggestedText;
        result.Applied = true;

        await db.SaveChangesAsync(ct);
        return Ok(new { translationText = task.TranslationText });
    }

    [HttpGet("results")]
    public async Task<IActionResult> GetResults([FromQuery] Guid contentItemLanguageTaskId, CancellationToken ct)
    {
        var results = await db.ToneCheckResults
            .Where(r => r.ContentItemLanguageTaskId == contentItemLanguageTaskId)
            .OrderByDescending(r => r.CreatedUtc)
            .ToListAsync(ct);

        return Ok(results);
    }
}

public sealed record ToneCheckRequest(Guid ContentItemLanguageTaskId, string Text, string Language, Guid ProjectId);
