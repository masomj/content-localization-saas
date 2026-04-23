using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record UpdateContentItemRequest(string Source, string Status, string? Description = null, int? MaxLength = null, string? ContentType = null);

[ApiController]
[Route("api/content-items")]
public sealed class ContentItemHistoryController(IContentItemService contentItems) : ControllerBase
{
    [HttpPut("{id:guid}")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateContentItemRequest request, CancellationToken cancellationToken)
    {
        var actor = HttpContext.Request.Headers["X-Actor-Email"].ToString();
        if (string.IsNullOrWhiteSpace(actor)) actor = "editor@system.local";

        var item = await contentItems.UpdateAsync(id, request.Source, request.Status, actor, cancellationToken, request.Description, request.MaxLength, request.ContentType);
        return Ok(item);
    }

    [HttpGet("{id:guid}/revisions")]
    public async Task<IActionResult> GetRevisions(Guid id, CancellationToken cancellationToken)
    {
        var rows = await contentItems.GetRevisionsAsync(id, cancellationToken);
        return Ok(rows);
    }

    [HttpGet("{id:guid}/revisions/compare")]
    public async Task<IActionResult> Compare(Guid id, [FromQuery] Guid left, [FromQuery] Guid right, CancellationToken cancellationToken)
    {
        var diff = await contentItems.CompareRevisionsAsync(id, left, right, cancellationToken);
        return Ok(diff);
    }

    [HttpPost("{id:guid}/revisions/{revisionId:guid}/rollback")]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> Rollback(Guid id, Guid revisionId, CancellationToken cancellationToken)
    {
        var actor = HttpContext.Request.Headers["X-Actor-Email"].ToString();
        if (string.IsNullOrWhiteSpace(actor)) actor = "admin@system.local";

        var item = await contentItems.RollbackAsync(id, revisionId, actor, cancellationToken);
        return Ok(item);
    }
}
