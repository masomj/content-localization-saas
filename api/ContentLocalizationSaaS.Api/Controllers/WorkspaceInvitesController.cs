using System.Security.Claims;
using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record CreateInviteRequest(Guid WorkspaceId, string Email, string Role);
public sealed record AcceptInviteRequest(string Token, string Email);
public sealed record RevokeMembershipRequest(Guid WorkspaceId, string Email);
public sealed record ChangeMembershipRoleRequest(Guid WorkspaceId, string Email, string Role);

[ApiController]
[Route("api/admin/invites")]
[RequireAppRole(AppRole.Admin)]
public sealed class WorkspaceInvitesController(AppDbContext db) : ControllerBase
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

        var invites = await db.WorkspaceInvites
            .Where(x => x.WorkspaceId == workspaceId)
            .OrderByDescending(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);

        return Ok(invites);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInviteRequest request, CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Role))
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = ["workspaceId, email and role are required"]
            }));
        }

        var adminCheck = await EnsureAdminInWorkspace(request.WorkspaceId, cancellationToken);
        if (adminCheck is not null) return adminCheck;

        var workspaceExists = await db.Workspaces.AnyAsync(x => x.Id == request.WorkspaceId, cancellationToken);
        if (!workspaceExists) return BadRequest(new { error = "workspace_not_found" });

        var invite = new WorkspaceInvite
        {
            WorkspaceId = request.WorkspaceId,
            Email = request.Email.Trim().ToLowerInvariant(),
            Role = request.Role.Trim(),
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", string.Empty).Replace("/", "_").Replace("+", "-")
        };

        db.WorkspaceInvites.Add(invite);
        db.MembershipAuditLogs.Add(new MembershipAuditLog
        {
            WorkspaceId = request.WorkspaceId,
            ActorEmail = ActorEmail,
            TargetEmail = invite.Email,
            Action = "invite_created",
            OldValue = string.Empty,
            NewValue = $"role={invite.Role};status={invite.Status}"
        });

        await db.SaveChangesAsync(cancellationToken);
        return Ok(invite);
    }

    [HttpPost("accept")]
    public async Task<IActionResult> Accept([FromBody] AcceptInviteRequest request, CancellationToken cancellationToken)
    {
        var invite = await db.WorkspaceInvites.FirstOrDefaultAsync(x => x.Token == request.Token, cancellationToken);
        if (invite is null) return NotFound();

        if (invite.Status is InviteStatus.Revoked or InviteStatus.Expired)
        {
            return BadRequest(new { error = "invite_not_usable" });
        }

        if (invite.ExpiresUtc < DateTime.UtcNow)
        {
            invite.Status = InviteStatus.Expired;
            await db.SaveChangesAsync(cancellationToken);
            return BadRequest(new { error = "invite_expired" });
        }

        var oldStatus = invite.Status;
        invite.Status = InviteStatus.Accepted;

        var email = request.Email.Trim().ToLowerInvariant();
        var membership = await db.WorkspaceMemberships
            .FirstOrDefaultAsync(x => x.WorkspaceId == invite.WorkspaceId && x.Email == email, cancellationToken);

        string membershipOld = "none";
        if (membership is null)
        {
            membership = new WorkspaceMembership
            {
                WorkspaceId = invite.WorkspaceId,
                Email = email,
                Role = invite.Role,
                IsActive = true
            };
            db.WorkspaceMemberships.Add(membership);
        }
        else
        {
            membershipOld = $"role={membership.Role};active={membership.IsActive}";
            membership.Role = invite.Role;
            membership.IsActive = true;
        }

        db.MembershipAuditLogs.AddRange(
            new MembershipAuditLog
            {
                WorkspaceId = invite.WorkspaceId,
                ActorEmail = ActorEmail,
                TargetEmail = email,
                Action = "invite_accepted",
                OldValue = oldStatus.ToString(),
                NewValue = invite.Status.ToString()
            },
            new MembershipAuditLog
            {
                WorkspaceId = invite.WorkspaceId,
                ActorEmail = ActorEmail,
                TargetEmail = email,
                Action = "membership_upserted",
                OldValue = membershipOld,
                NewValue = $"role={membership.Role};active={membership.IsActive}"
            });

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { status = "accepted", workspaceId = invite.WorkspaceId, role = invite.Role });
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RevokeMembershipRequest request, CancellationToken cancellationToken)
    {
        var adminCheck = await EnsureAdminInWorkspace(request.WorkspaceId, cancellationToken);
        if (adminCheck is not null) return adminCheck;

        var email = request.Email.Trim().ToLowerInvariant();
        var membership = await db.WorkspaceMemberships
            .FirstOrDefaultAsync(x => x.WorkspaceId == request.WorkspaceId && x.Email == email, cancellationToken);

        if (membership is null) return NotFound();

        var oldMembership = $"role={membership.Role};active={membership.IsActive}";
        membership.IsActive = false;

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
            Action = "membership_revoked",
            OldValue = oldMembership,
            NewValue = $"role={membership.Role};active={membership.IsActive}"
        });

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { status = "revoked" });
    }

    [HttpPost("change-role")]
    public async Task<IActionResult> ChangeRole([FromBody] ChangeMembershipRoleRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Role) || request.WorkspaceId == Guid.Empty)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = ["workspaceId, email and role are required"]
            }));
        }

        var adminCheck = await EnsureAdminInWorkspace(request.WorkspaceId, cancellationToken);
        if (adminCheck is not null) return adminCheck;

        var email = request.Email.Trim().ToLowerInvariant();
        var membership = await db.WorkspaceMemberships
            .FirstOrDefaultAsync(x => x.WorkspaceId == request.WorkspaceId && x.Email == email, cancellationToken);

        if (membership is null) return NotFound();

        var oldRole = membership.Role;
        membership.Role = request.Role.Trim();

        db.MembershipAuditLogs.Add(new MembershipAuditLog
        {
            WorkspaceId = request.WorkspaceId,
            ActorEmail = ActorEmail,
            TargetEmail = email,
            Action = "membership_role_changed",
            OldValue = oldRole,
            NewValue = membership.Role
        });

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { status = "updated", role = membership.Role });
    }

    private Guid ResolveWorkspaceContext()
    {
        var raw = HttpContext.Request.Headers["X-Workspace-Id"].ToString();
        return Guid.TryParse(raw, out var workspaceId) ? workspaceId : Guid.Empty;
    }

    private async Task<IActionResult?> EnsureAdminInWorkspace(Guid workspaceId, CancellationToken cancellationToken)
    {
        var actor = ActorEmail;
        var actorMembership = await db.WorkspaceMemberships
            .FirstOrDefaultAsync(x => x.WorkspaceId == workspaceId && x.Email == actor && x.IsActive, cancellationToken);

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
