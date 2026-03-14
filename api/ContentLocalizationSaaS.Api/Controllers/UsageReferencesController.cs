using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record CreateUsageReferenceRequest(Guid ContentItemId, Guid ProjectId, string? Screen, string? Component, string? ReferencePath);

[ApiController]
[Route("api/usage-references")]
public sealed class UsageReferencesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? contentItemId,
        [FromQuery] Guid? projectId,
        [FromQuery] string? screen,
        [FromQuery] string? component,
        CancellationToken cancellationToken)
    {
        var query = db.UsageReferences.AsQueryable();

        if (contentItemId.HasValue) query = query.Where(x => x.ContentItemId == contentItemId.Value);
        if (projectId.HasValue) query = query.Where(x => x.ProjectId == projectId.Value);
        if (!string.IsNullOrWhiteSpace(screen))
        {
            var s = screen.Trim().ToLowerInvariant();
            query = query.Where(x => x.Screen.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(component))
        {
            var c = component.Trim().ToLowerInvariant();
            query = query.Where(x => x.Component.ToLower().Contains(c));
        }

        var rows = await query.OrderBy(x => x.Screen).ThenBy(x => x.Component).ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpPost]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Create([FromBody] CreateUsageReferenceRequest request, CancellationToken cancellationToken)
    {
        if (request.ContentItemId == Guid.Empty || request.ProjectId == Guid.Empty)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = ["contentItemId and projectId are required"]
            }));
        }

        var itemExists = await db.ContentItems.AnyAsync(x => x.Id == request.ContentItemId, cancellationToken);
        if (!itemExists) return NotFound();

        var reference = new UsageReference
        {
            ContentItemId = request.ContentItemId,
            ProjectId = request.ProjectId,
            Screen = request.Screen?.Trim() ?? string.Empty,
            Component = request.Component?.Trim() ?? string.Empty,
            ReferencePath = request.ReferencePath?.Trim() ?? string.Empty,
        };

        db.UsageReferences.Add(reference);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(reference);
    }
}
