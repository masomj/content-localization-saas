using ContentLocalizationSaaS.Api.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/permissions")]
public sealed class PermissionsController : ControllerBase
{
    [HttpGet("me")]
    public IActionResult GetMyPermissions()
    {
        var role = AppRoleResolver.Resolve(HttpContext);
        var matrix = RolePermissions.For(role);
        return Ok(new
        {
            role = role.ToString(),
            canRead = matrix.CanRead,
            canWrite = matrix.CanWrite,
            canReview = matrix.CanReview,
            canAdmin = matrix.CanAdmin
        });
    }
}
