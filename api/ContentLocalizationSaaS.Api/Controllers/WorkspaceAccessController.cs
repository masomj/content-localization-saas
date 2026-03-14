using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/workspaces")]
public sealed class WorkspaceAccessController(AppDbContext db) : ControllerBase
{
    [HttpGet("{id:guid}/access-check")]
    public async Task<IActionResult> AccessCheck(Guid id, CancellationToken cancellationToken)
    {
        var email = HttpContext.Request.Headers["X-User-Email"].ToString().Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails { Status = 403, Title = "Forbidden" });
        }

        var membership = await db.WorkspaceMemberships
            .FirstOrDefaultAsync(x => x.WorkspaceId == id && x.Email == email, cancellationToken);

        if (membership is null || !membership.IsActive)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails { Status = 403, Title = "Forbidden" });
        }

        return Ok(new { workspaceId = id, email, role = membership.Role, active = membership.IsActive });
    }
}
