using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Application;
using ContentLocalizationSaaS.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/collections")]
public sealed class ProjectCollectionsController(IProjectCollectionService collections) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid projectId, CancellationToken cancellationToken)
    {
        var items = await collections.ListAsync(projectId, cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Create(Guid projectId, [FromBody] CreateProjectCollectionRequest request, CancellationToken cancellationToken)
    {
        var item = await collections.CreateAsync(projectId, request, cancellationToken);
        return Ok(item);
    }

    [HttpPut("{collectionId:guid}/rename")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Rename(Guid projectId, Guid collectionId, [FromBody] RenameProjectCollectionRequest request, CancellationToken cancellationToken)
    {
        var item = await collections.RenameAsync(projectId, collectionId, request, cancellationToken);
        return Ok(item);
    }

    [HttpPut("{collectionId:guid}/move")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Move(Guid projectId, Guid collectionId, [FromBody] MoveProjectCollectionRequest request, CancellationToken cancellationToken)
    {
        var items = await collections.MoveAsync(projectId, collectionId, request, cancellationToken);
        return Ok(items);
    }
}
