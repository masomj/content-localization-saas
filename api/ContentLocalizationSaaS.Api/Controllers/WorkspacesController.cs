using System.Security.Claims;
using ContentLocalizationSaaS.Application;
using ContentLocalizationSaaS.Application.Abstractions;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/workspaces")]
public sealed class WorkspacesController(IWorkspaceService workspaces, AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var items = await workspaces.GetAllAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var email = ResolveUserEmail();
        if (string.IsNullOrWhiteSpace(email)) return Unauthorized();

        var workspaceIds = await db.WorkspaceMemberships
            .Where(x => x.Email == email && x.IsActive)
            .Select(x => x.WorkspaceId)
            .ToListAsync(cancellationToken);

        var items = await db.Workspaces
            .Where(x => workspaceIds.Contains(x.Id))
            .OrderBy(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var workspace = await workspaces.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = workspace.Id }, workspace);
    }

    [HttpPost("bootstrap")]
    public async Task<IActionResult> Bootstrap([FromBody] CreateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var email = ResolveUserEmail();
        if (string.IsNullOrWhiteSpace(email)) return Unauthorized();

        var workspace = await workspaces.CreateAsync(request, cancellationToken);

        var existingMembership = await db.WorkspaceMemberships
            .FirstOrDefaultAsync(x => x.WorkspaceId == workspace.Id && x.Email == email, cancellationToken);

        if (existingMembership is null)
        {
            db.WorkspaceMemberships.Add(new WorkspaceMembership
            {
                WorkspaceId = workspace.Id,
                Email = email,
                Role = "Admin",
                IsActive = true,
            });

            db.MembershipAuditLogs.Add(new MembershipAuditLog
            {
                WorkspaceId = workspace.Id,
                ActorEmail = email,
                TargetEmail = email,
                Action = "membership_bootstrapped",
                OldValue = "none",
                NewValue = "role=Admin;active=True"
            });

            await db.SaveChangesAsync(cancellationToken);
        }

        return CreatedAtAction(nameof(GetById), new { id = workspace.Id }, new
        {
            workspace,
            role = "Admin"
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var workspace = await workspaces.GetByIdAsync(id, cancellationToken);
        return Ok(workspace);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var workspace = await workspaces.UpdateAsync(id, request, cancellationToken);
        return Ok(workspace);
    }

    private string ResolveUserEmail()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("email")?.Value
            ?? HttpContext.Request.Headers["X-User-Email"].ToString();

        return (email ?? string.Empty).Trim().ToLowerInvariant();
    }
}
