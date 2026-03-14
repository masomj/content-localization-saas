using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/localization-grid")]
public sealed class LocalizationGridController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] Guid? projectId,
        [FromQuery] string? stateFilter,
        [FromQuery] string? sortBy,
        [FromQuery] bool desc = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var itemsQuery = db.ContentItems.AsQueryable();
        if (projectId.HasValue) itemsQuery = itemsQuery.Where(x => x.ProjectId == projectId.Value);

        var tasksQuery = db.ContentItemLanguageTasks.AsQueryable();
        if (projectId.HasValue)
        {
            var itemIds = db.ContentItems.Where(x => x.ProjectId == projectId.Value).Select(x => x.Id);
            tasksQuery = tasksQuery.Where(x => itemIds.Contains(x.ContentItemId));
        }

        var items = await itemsQuery.ToListAsync(cancellationToken);
        var tasks = await tasksQuery.ToListAsync(cancellationToken);

        var rows = items.Select(item =>
        {
            var perLang = tasks.Where(t => t.ContentItemId == item.Id).ToList();
            var hasMissing = perLang.Count == 0 || perLang.Any(x => x.Status is "todo" or "missing");
            var hasOutdated = perLang.Any(x => x.Status == "outdated");
            var hasReview = perLang.Any(x => x.Status == "pending_review");

            return new
            {
                itemId = item.Id,
                itemKey = item.Key,
                source = item.Source,
                sourceStatus = item.Status,
                targets = perLang.Select(x => new { language = x.LanguageCode, x.Status, x.AssigneeEmail, x.DueUtc }),
                hasMissing,
                hasOutdated,
                hasReview
            };
        });

        if (!string.IsNullOrWhiteSpace(stateFilter))
        {
            rows = stateFilter.Trim().ToLowerInvariant() switch
            {
                "missing" => rows.Where(x => x.hasMissing),
                "outdated" => rows.Where(x => x.hasOutdated),
                "review" => rows.Where(x => x.hasReview),
                _ => rows
            };
        }

        rows = (sortBy?.Trim().ToLowerInvariant()) switch
        {
            "source" => desc ? rows.OrderByDescending(x => x.source) : rows.OrderBy(x => x.source),
            "sourcestatus" => desc ? rows.OrderByDescending(x => x.sourceStatus) : rows.OrderBy(x => x.sourceStatus),
            _ => desc ? rows.OrderByDescending(x => x.itemKey) : rows.OrderBy(x => x.itemKey)
        };

        var total = rows.Count();
        var paged = rows.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Ok(new { total, page, pageSize, rows = paged });
    }
}
