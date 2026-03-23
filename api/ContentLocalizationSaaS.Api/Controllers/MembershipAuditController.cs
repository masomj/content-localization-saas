using System.Security.Claims;
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
        var contextWorkspaceId = ResolveWorkspaceContext();
        if (contextWorkspaceId == Guid.Empty && workspaceId is null)
        {
            return BadRequest(new { error = "workspace_context_required" });
        }

        var effectiveWorkspaceId = workspaceId ?? contextWorkspaceId;
        var adminCheck = await EnsureAdminInWorkspace(effectiveWorkspaceId, cancellationToken);
        if (adminCheck is not null) return adminCheck;

        var query = db.MembershipAuditLogs.Where(x => x.WorkspaceId == effectiveWorkspaceId);

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

    private Guid ResolveWorkspaceContext()
    {
        var raw = HttpContext.Request.Headers["X-Workspace-Id"].ToString();
        return Guid.TryParse(raw, out var workspaceId) ? workspaceId : Guid.Empty;
    }

    private async Task<IActionResult?> EnsureAdminInWorkspace(Guid workspaceId, CancellationToken cancellationToken)
    {
        var actorEmail = (User.FindFirst(ClaimTypes.Email)?.Value
                          ?? User.FindFirst("email")?.Value
                          ?? HttpContext.Request.Headers["X-Actor-Email"].ToString())
            .Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(actorEmail))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails { Status = 403, Title = "Forbidden" });
        }

        var actorMembership = await db.WorkspaceMemberships
            .FirstOrDefaultAsync(x => x.WorkspaceId == workspaceId && x.Email == actorEmail && x.IsActive, cancellationToken);

        if (actorMembership is null || !string.Equals(actorMembership.Role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails { Status = 403, Title = "Forbidden" });
        }

        return null;
    }

    private static string Escape(string value)
    {
        var safe = value.Replace("\"", "\"\"");
        return $"\"{safe}\"";
    }
}
