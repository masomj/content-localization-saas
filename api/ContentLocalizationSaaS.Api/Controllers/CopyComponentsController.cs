using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record CreateCopyComponentRequest(Guid ProjectId, string Name, string Source);
public sealed record UpdateCopyComponentRequest(string Source);
public sealed record LinkContentItemRequest(Guid ContentItemId);

[ApiController]
[Route("api/copy-components")]
public sealed class CopyComponentsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? projectId, CancellationToken cancellationToken)
    {
        var query = db.CopyComponents.AsQueryable();
        if (projectId.HasValue) query = query.Where(x => x.ProjectId == projectId.Value);

        var rows = await query.OrderByDescending(x => x.UpdatedUtc).ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpPost]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Create([FromBody] CreateCopyComponentRequest request, CancellationToken cancellationToken)
    {
        if (request.ProjectId == Guid.Empty || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Source))
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = ["projectId, name and source are required"]
            }));
        }

        var component = new CopyComponent
        {
            ProjectId = request.ProjectId,
            Name = request.Name.Trim(),
            Source = request.Source.Trim()
        };

        db.CopyComponents.Add(component);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(component);
    }

    [HttpPost("{id:guid}/link")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Link(Guid id, [FromBody] LinkContentItemRequest request, CancellationToken cancellationToken)
    {
        var component = await db.CopyComponents.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (component is null) return NotFound();

        var item = await db.ContentItems.FirstOrDefaultAsync(x => x.Id == request.ContentItemId, cancellationToken);
        if (item is null) return NotFound();

        item.CopyComponentId = component.Id;
        item.Source = component.Source;
        item.Status = "updated";

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { status = "linked" });
    }

    [HttpPut("{id:guid}")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCopyComponentRequest request, CancellationToken cancellationToken)
    {
        var component = await db.CopyComponents.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (component is null) return NotFound();

        component.Source = request.Source.Trim();
        component.UpdatedUtc = DateTime.UtcNow;

        var linkedItems = await db.ContentItems.Where(x => x.CopyComponentId == id).ToListAsync(cancellationToken);
        foreach (var item in linkedItems)
        {
            item.Source = component.Source;
            item.Status = "pending_review";
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { status = "propagated", linkedCount = linkedItems.Count });
    }

    [HttpDelete("{id:guid}")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var component = await db.CopyComponents.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (component is null) return NotFound();

        var linkedCount = await db.ContentItems.CountAsync(x => x.CopyComponentId == id, cancellationToken);
        if (linkedCount > 0)
        {
            return Conflict(new { error = "component_has_active_links", linkedCount, message = "Migrate links before deletion." });
        }

        db.CopyComponents.Remove(component);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { status = "deleted" });
    }
}
