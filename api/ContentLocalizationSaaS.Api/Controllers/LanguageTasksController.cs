using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record UpsertLanguageTaskRequest(Guid ContentItemId, string LanguageCode, string? AssigneeEmail, DateTime? DueUtc, string Status);

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

    [HttpPost]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Upsert([FromBody] UpsertLanguageTaskRequest request, CancellationToken cancellationToken)
    {
        if (request.ContentItemId == Guid.Empty || string.IsNullOrWhiteSpace(request.LanguageCode) || string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(new { error = "contentItemId, languageCode and status required" });

        var existing = await db.ContentItemLanguageTasks
            .FirstOrDefaultAsync(x => x.ContentItemId == request.ContentItemId && x.LanguageCode == request.LanguageCode, cancellationToken);

        if (existing is null)
        {
            existing = new ContentItemLanguageTask
            {
                ContentItemId = request.ContentItemId,
                LanguageCode = request.LanguageCode,
                AssigneeEmail = request.AssigneeEmail?.Trim().ToLowerInvariant() ?? string.Empty,
                DueUtc = request.DueUtc,
                Status = request.Status.Trim()
            };
            db.ContentItemLanguageTasks.Add(existing);
        }
        else
        {
            existing.AssigneeEmail = request.AssigneeEmail?.Trim().ToLowerInvariant() ?? string.Empty;
            existing.DueUtc = request.DueUtc;
            existing.Status = request.Status.Trim();
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(existing);
    }
}
