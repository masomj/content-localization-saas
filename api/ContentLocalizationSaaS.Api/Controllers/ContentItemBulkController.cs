using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record BulkUpdateStatusRequest(Guid[] ItemIds, string Status);

[ApiController]
[Route("api/content-items/bulk")]
public sealed class ContentItemBulkController(AppDbContext db) : ControllerBase
{
    [HttpPost("status")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> BulkUpdateStatus([FromBody] BulkUpdateStatusRequest request, CancellationToken cancellationToken)
    {
        if (request.ItemIds is null || request.ItemIds.Length == 0 || string.IsNullOrWhiteSpace(request.Status))
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["request"] = ["itemIds and status are required"]
            }));
        }

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var items = await db.ContentItems.Where(x => request.ItemIds.Contains(x.Id)).ToListAsync(cancellationToken);
        if (items.Count != request.ItemIds.Length)
        {
            await tx.RollbackAsync(cancellationToken);
            var found = items.Select(x => x.Id).ToHashSet();
            var missing = request.ItemIds.Where(x => !found.Contains(x)).ToArray();
            return BadRequest(new { error = "missing_items", missing });
        }

        foreach (var item in items)
        {
            item.Status = request.Status.Trim();
        }

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return Ok(new { status = "updated", count = items.Count });
    }
}
