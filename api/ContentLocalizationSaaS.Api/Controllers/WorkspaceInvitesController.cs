using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record CreateInviteRequest(Guid WorkspaceId, string Email, string Role);
public sealed record AcceptInviteRequest(string Token, string Email);
public sealed record RevokeMembershipRequest(Guid WorkspaceId, string Email);

[ApiController]
[Route("api/admin/invites")]
[RequireAppRole(AppRole.Admin)]
public sealed class WorkspaceInvitesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var invites = await db.WorkspaceInvites
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

        invite.Status = InviteStatus.Accepted;

        var email = request.Email.Trim().ToLowerInvariant();
        var membership = await db.WorkspaceMemberships
            .FirstOrDefaultAsync(x => x.WorkspaceId == invite.WorkspaceId && x.Email == email, cancellationToken);

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
            membership.Role = invite.Role;
            membership.IsActive = true;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { status = "accepted", workspaceId = invite.WorkspaceId, role = invite.Role });
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RevokeMembershipRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var membership = await db.WorkspaceMemberships
            .FirstOrDefaultAsync(x => x.WorkspaceId == request.WorkspaceId && x.Email == email, cancellationToken);

        if (membership is null) return NotFound();

        membership.IsActive = false;

        var pendingInvites = await db.WorkspaceInvites
            .Where(x => x.WorkspaceId == request.WorkspaceId && x.Email == email && x.Status == InviteStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var invite in pendingInvites)
        {
            invite.Status = InviteStatus.Revoked;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { status = "revoked" });
    }
}
