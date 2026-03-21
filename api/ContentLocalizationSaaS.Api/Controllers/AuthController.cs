using System.Security.Claims;
using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

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
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userId) && string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized();
        }

        var displayName = User.FindFirst(ClaimTypes.Name)?.Value
            ?? User.FindFirst("name")?.Value
            ?? User.FindFirst("preferred_username")?.Value
            ?? email
            ?? userId;

        AppRoleResolver.TryResolveFromClaims(User, out var appRole);

        var membership = !string.IsNullOrWhiteSpace(email)
            ? await dbContext.WorkspaceMemberships.FirstOrDefaultAsync(m => m.Email == email, cancellationToken)
            : null;

        var workspace = membership != null
            ? await dbContext.Workspaces.FirstOrDefaultAsync(w => w.Id == membership.WorkspaceId, cancellationToken)
            : null;

        return Ok(new UserInfo
        {
            Id = string.IsNullOrWhiteSpace(userId) ? email : userId,
            Email = email,
            Name = displayName,
            Role = appRole.ToString(),
            Workspace = workspace != null
                ? new WorkspaceInfo
                {
                    Id = workspace.Id,
                    Name = workspace.Name,
                }
                : null,
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
}

public sealed class WorkspaceInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}
