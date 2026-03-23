using System.Security.Claims;
using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record SwitchWorkspaceRequest(Guid WorkspaceId);

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? string.Empty;

        var email = User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("email")?.Value
            ?? HttpContext.Request.Headers["X-User-Email"].ToString()
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userId) && string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized();
        }

        email = email.Trim().ToLowerInvariant();

        var displayName = User.FindFirst(ClaimTypes.Name)?.Value
            ?? User.FindFirst("name")?.Value
            ?? User.FindFirst("preferred_username")?.Value
            ?? email
            ?? userId;

        AppRoleResolver.TryResolveFromClaims(User, out var appRole);

        var memberships = string.IsNullOrWhiteSpace(email)
            ? []
            : await dbContext.WorkspaceMemberships
                .Where(m => m.Email == email && m.IsActive)
                .OrderBy(m => m.CreatedUtc)
                .ToListAsync(cancellationToken);

        var workspaceIds = memberships.Select(x => x.WorkspaceId).Distinct().ToArray();
        var workspaceMap = await dbContext.Workspaces
            .Where(w => workspaceIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, cancellationToken);

        var workspaces = memberships
            .Where(m => workspaceMap.ContainsKey(m.WorkspaceId))
            .Select(m => new WorkspaceMembershipInfo
            {
                Id = m.WorkspaceId,
                Name = workspaceMap[m.WorkspaceId].Name,
                Role = m.Role,
            })
            .ToList();

        var requestedWorkspaceIdRaw = HttpContext.Request.Headers["X-Workspace-Id"].ToString();
        var hasRequestedWorkspace = Guid.TryParse(requestedWorkspaceIdRaw, out var requestedWorkspaceId);

        var activeWorkspace = hasRequestedWorkspace
            ? workspaces.FirstOrDefault(x => x.Id == requestedWorkspaceId)
            : workspaces.FirstOrDefault();

        return Ok(new UserInfo
        {
            Id = string.IsNullOrWhiteSpace(userId) ? email : userId,
            Email = email,
            Name = displayName,
            Role = activeWorkspace?.Role ?? appRole.ToString(),
            Workspace = activeWorkspace != null
                ? new WorkspaceInfo
                {
                    Id = activeWorkspace.Id,
                    Name = activeWorkspace.Name,
                }
                : null,
            Workspaces = workspaces,
        });
    }

    [HttpPost("switch-workspace")]
    [Authorize]
    public async Task<IActionResult> SwitchWorkspace([FromBody] SwitchWorkspaceRequest request, CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty) return BadRequest(new { error = "workspace_id_required" });

        var email = User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("email")?.Value
            ?? HttpContext.Request.Headers["X-User-Email"].ToString();

        email = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email)) return Unauthorized();

        var membership = await dbContext.WorkspaceMemberships
            .FirstOrDefaultAsync(x => x.WorkspaceId == request.WorkspaceId && x.Email == email && x.IsActive, cancellationToken);

        if (membership is null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails { Status = 403, Title = "Forbidden" });
        }

        var workspace = await dbContext.Workspaces.FirstOrDefaultAsync(x => x.Id == request.WorkspaceId, cancellationToken);
        if (workspace is null) return NotFound();

        return Ok(new
        {
            workspace = new WorkspaceInfo { Id = workspace.Id, Name = workspace.Name },
            role = membership.Role,
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        return Ok(new { message = "Client logout acknowledged" });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public IActionResult RegisterDisabled()
        => StatusCode(StatusCodes.Status410Gone, new { error = "registration_managed_by_identity_provider" });

    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult LoginDisabled()
        => StatusCode(StatusCodes.Status410Gone, new { error = "login_managed_by_identity_provider" });

    [HttpPost("refresh")]
    [AllowAnonymous]
    public IActionResult RefreshDisabled()
        => StatusCode(StatusCodes.Status410Gone, new { error = "refresh_managed_by_identity_provider" });

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public IActionResult ForgotPasswordDisabled()
        => StatusCode(StatusCodes.Status410Gone, new { error = "password_reset_managed_by_identity_provider" });

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public IActionResult ResetPasswordDisabled()
        => StatusCode(StatusCodes.Status410Gone, new { error = "password_reset_managed_by_identity_provider" });
}

public sealed class UserInfo
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public WorkspaceInfo? Workspace { get; set; }
    public List<WorkspaceMembershipInfo> Workspaces { get; set; } = [];
}

public class WorkspaceInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}

public sealed class WorkspaceMembershipInfo : WorkspaceInfo
{
    public string Role { get; set; } = "Viewer";
}
