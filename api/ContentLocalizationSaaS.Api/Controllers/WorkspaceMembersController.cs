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
    private string ActorEmail => HttpContext.Request.Headers["X-Actor-Email"].ToString().Trim().ToLowerInvariant() is { Length: > 0 } actor
        ? actor
        : "admin@system.local";

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var memberships = await db.WorkspaceMemberships
            .OrderByDescending(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);

        return Ok(memberships);
    }

    [HttpDelete]
    public async Task<IActionResult> Remove([FromBody] RemoveMemberRequest request, CancellationToken cancellationToken)
    {
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
}
