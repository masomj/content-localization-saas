using System.Text;
using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/activity-feed")]
public sealed class ActivityFeedController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] Guid projectId,
        [FromQuery] string? eventType,
        [FromQuery] string? actor,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 25;

        var query = db.ActivityFeedEvents.Where(x => x.ProjectId == projectId);

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            var et = eventType.Trim();
            query = query.Where(x => x.EventType == et);
        }

        if (!string.IsNullOrWhiteSpace(actor))
        {
            var a = actor.Trim().ToLowerInvariant();
            query = query.Where(x => x.ActorEmail == a);
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(x => x.CreatedUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Ok(new { total, page, pageSize, rows });
    }

    [HttpGet("export")]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> Export([FromQuery] Guid projectId, CancellationToken cancellationToken)
    {
        var rows = await db.ActivityFeedEvents
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);

        var lines = rows.Select(x => $"{x.CreatedUtc:O},\"{x.EventType}\",\"{x.ActorEmail}\",\"{x.Message.Replace("\"", "\"\"")}\"");
        var csv = "timestampUtc,eventType,actorEmail,message\n" + string.Join("\n", lines);
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", "activity-feed.csv");
    }
}
