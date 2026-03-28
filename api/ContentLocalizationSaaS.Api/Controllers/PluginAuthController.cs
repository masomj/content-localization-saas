// DEPRECATED: ASP.NET Identity auth. All new auth uses Keycloak OIDC.
// See DeviceAuthController for the replacement (OAuth 2.0 Device Authorization Grant).
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record PluginLoginRequest(string UserEmail, Guid WorkspaceId);
public sealed record PluginSwitchWorkspaceRequest(string Token, Guid WorkspaceId);

[ApiController]
[Route("api/plugin-auth")]
public sealed class PluginAuthController(AppDbContext db) : ControllerBase
{
    [Obsolete("Use DeviceAuthController (POST /api/device-auth/start) instead. This endpoint will be removed in a future release.")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] PluginLoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserEmail) || request.WorkspaceId == Guid.Empty)
            return BadRequest(new { error = "userEmail_and_workspaceId_required" });

        var email = request.UserEmail.Trim().ToLowerInvariant();
        var member = await db.WorkspaceMemberships.FirstOrDefaultAsync(
            x => x.WorkspaceId == request.WorkspaceId && x.Email == email && x.IsActive,
            cancellationToken);

        if (member is null)
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "not_authorized_for_workspace" });

        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", string.Empty).Replace("/", "_").Replace("+", "-");
        var session = new PluginSession
        {
            Token = token,
            UserEmail = email,
            WorkspaceId = request.WorkspaceId,
            ExpiresUtc = DateTime.UtcNow.AddHours(8)
        };

        db.PluginSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new { session.Token, session.ExpiresUtc, session.WorkspaceId });
    }

    [HttpGet("projects")]
    public async Task<IActionResult> Projects([FromQuery] string token, CancellationToken cancellationToken)
    {
        var session = await db.PluginSessions.FirstOrDefaultAsync(x => x.Token == token, cancellationToken);
        if (session is null) return NotFound(new { error = "session_not_found" });

        if (session.ExpiresUtc <= DateTime.UtcNow)
            return StatusCode(StatusCodes.Status401Unauthorized, new { error = "token_expired", prompt = "reauth_required" });

        var projects = await db.Projects
            .Where(x => x.WorkspaceId == session.WorkspaceId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            workspaceId = session.WorkspaceId,
            projects
        });
    }

    [HttpPost("switch-workspace")]
    public async Task<IActionResult> SwitchWorkspace([FromBody] PluginSwitchWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var session = await db.PluginSessions.FirstOrDefaultAsync(x => x.Token == request.Token, cancellationToken);
        if (session is null) return NotFound(new { error = "session_not_found" });
        if (session.ExpiresUtc <= DateTime.UtcNow)
            return StatusCode(StatusCodes.Status401Unauthorized, new { error = "token_expired", prompt = "reauth_required" });

        var member = await db.WorkspaceMemberships.FirstOrDefaultAsync(
            x => x.WorkspaceId == request.WorkspaceId && x.Email == session.UserEmail && x.IsActive,
            cancellationToken);
        if (member is null)
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "not_authorized_for_workspace" });

        session.WorkspaceId = request.WorkspaceId;
        await db.SaveChangesAsync(cancellationToken);

        var projects = await db.Projects.Where(x => x.WorkspaceId == session.WorkspaceId).OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return Ok(new { workspaceId = session.WorkspaceId, projects });
    }
}
