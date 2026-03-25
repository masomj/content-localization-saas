using ContentLocalizationSaaS.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/tree")]
public sealed class ProjectTreeController(IProjectCollectionService collections) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetTree(Guid projectId, CancellationToken cancellationToken)
    {
        var tree = await collections.GetTreeAsync(projectId, cancellationToken);
        return Ok(tree);
    }
}
