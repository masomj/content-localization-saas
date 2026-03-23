using System.Security.Claims;
using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record RemoveMemberRequest(Guid WorkspaceId, string Email);

[ApiController]
[Route("api/admin/members")]
[RequireAppRole(AppRole.Admin)]
public sealed class WorkspaceMembersController(AppDbContext db) : ControllerBase
{
    private string ActorEmail => (User.FindFirst(ClaimTypes.Email)?.Value
                                  ?? User.FindFirst("email")?.Value
                                  ?? HttpContext.Request.Headers["X-Actor-Email"].ToString())
        .Trim().ToLowerInvariant() is { Length: > 0 } actor
            ? actor
            : "admin@system.local";

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var workspaceId = ResolveWorkspaceContext();
        if (workspaceId == Guid.Empty) return BadRequest(new { error = "workspace_context_required" });

        var adminCheck = await EnsureAdminInWorkspace(workspaceId, cancellationToken);
        if (adminCheck is not null) return adminCheck;

        var memberships = await db.WorkspaceMemberships
            .Where(x => x.WorkspaceId == workspaceId)
            .OrderByDescending(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);

        return Ok(memberships);
    }

    [HttpDelete]
    public async Task<IActionResult> Remove([FromBody] RemoveMemberRequest request, CancellationToken cancellationToken)
    {
        var adminCheck = await EnsureAdminInWorkspace(request.WorkspaceId, cancellationToken);
        if (adminCheck is not null) return adminCheck;

        var email = request.Email.Trim().ToLowerInvariant();
        var membership = await db.WorkspaceMemberships
            .FirstOrDefaultAsync(x => x.WorkspaceId == request.WorkspaceId && x.Email == email, cancellationToken);

        if (membership is null) return NotFound();

        var oldMembership = $"role={membership.Role};active={membership.IsActive}";

        db.WorkspaceMemberships.Remove(membership);

        var pendingInvites = await db.WorkspaceInvites
            .Where(x => x.WorkspaceId == request.WorkspaceId && x.Email == email && x.Status == InviteStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var invite in pendingInvites)
        {
            invite.Status = InviteStatus.Revoked;
        }

        db.MembershipAuditLogs.Add(new MembershipAuditLog
        {
            WorkspaceId = request.WorkspaceId,
            ActorEmail = ActorEmail,
            TargetEmail = email,
            Action = "member_removed",
            OldValue = oldMembership,
            NewValue = "removed"
        });

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { status = "removed" });
    }

    private Guid ResolveWorkspaceContext()
    {
        var raw = HttpContext.Request.Headers["X-Workspace-Id"].ToString();
        return Guid.TryParse(raw, out var workspaceId) ? workspaceId : Guid.Empty;
    }

    private async Task<IActionResult?> EnsureAdminInWorkspace(Guid workspaceId, CancellationToken cancellationToken)
    {
        var actorMembership = await db.WorkspaceMemberships
            .FirstOrDefaultAsync(x => x.WorkspaceId == workspaceId && x.Email == ActorEmail && x.IsActive, cancellationToken);

        if (actorMembership is null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails { Status = 403, Title = "Forbidden" });
        }

        if (!string.Equals(actorMembership.Role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails { Status = 403, Title = "Forbidden" });
        }

        return null;
    }
}
