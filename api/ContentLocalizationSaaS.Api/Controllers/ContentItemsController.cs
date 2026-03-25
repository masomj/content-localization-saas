using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Application;
using ContentLocalizationSaaS.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/content-items")]
public sealed class ContentItemsController(IContentItemService contentItems) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? projectId, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        var rows = await contentItems.GetAllAsync(projectId, search, cancellationToken);
        return Ok(rows);
    }

    [HttpPost]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Create([FromBody] CreateContentItemRequest request, CancellationToken cancellationToken)
    {
        var item = await contentItems.CreateAsync(request, cancellationToken);
        return Ok(item);
    }

    [HttpPut("{id:guid}/move")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Move(Guid id, [FromBody] MoveContentItemRequest request, CancellationToken cancellationToken)
    {
        var item = await contentItems.MoveAsync(id, request, cancellationToken);
        return Ok(item);
    }
}
