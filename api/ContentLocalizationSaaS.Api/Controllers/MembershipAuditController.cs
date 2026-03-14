using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/admin/membership-audit")]
[RequireAppRole(AppRole.Admin)]
public sealed class MembershipAuditController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] Guid? workspaceId,
        [FromQuery] string? targetEmail,
        [FromQuery] string? action,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken cancellationToken)
    {
        var query = db.MembershipAuditLogs.AsQueryable();

        if (workspaceId.HasValue) query = query.Where(x => x.WorkspaceId == workspaceId.Value);
        if (!string.IsNullOrWhiteSpace(targetEmail))
        {
            var email = targetEmail.Trim().ToLowerInvariant();
            query = query.Where(x => x.TargetEmail == email);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            var actionValue = action.Trim();
            query = query.Where(x => x.Action == actionValue);
        }

        if (fromUtc.HasValue) query = query.Where(x => x.CreatedUtc >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(x => x.CreatedUtc <= toUtc.Value);

        var rows = await query.OrderByDescending(x => x.CreatedUtc).ToListAsync(cancellationToken);

        if (HttpContext.Request.Query.TryGetValue("format", out var formatValue) &&
            string.Equals(formatValue.ToString(), "csv", StringComparison.OrdinalIgnoreCase))
        {
            var csvRows = rows.Select(x =>
                $"{x.CreatedUtc:O},{Escape(x.ActorEmail)},{Escape(x.TargetEmail)},{Escape(x.Action)},{Escape(x.OldValue)},{Escape(x.NewValue)}");
            var csv = "timestampUtc,actorEmail,targetEmail,action,oldValue,newValue\n" + string.Join("\n", csvRows);
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "membership-audit.csv");
        }

        return Ok(rows);
    }

    private static string Escape(string value)
    {
        var safe = value.Replace("\"", "\"\"");
        return $"\"{safe}\"";
    }
}
