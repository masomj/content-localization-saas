using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Application;
using ContentLocalizationSaaS.Application.Abstractions;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/content-items")]
public sealed class ContentItemsController(IContentItemService contentItems, AppDbContext db) : ControllerBase
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

    [HttpDelete("{id:guid}")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var item = await db.ContentItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return NotFound();

        db.ContentItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
