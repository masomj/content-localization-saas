using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record PluginScanIssuesRequest(Guid ProjectId, string DesignFileId, string[] MonitoredLayerIds);

[ApiController]
[Route("api/plugin-diagnostics")]
public sealed class PluginDiagnosticsController(AppDbContext db) : ControllerBase
{
    [HttpPost("scan")]
    public async Task<IActionResult> Scan([FromBody] PluginScanIssuesRequest request, CancellationToken cancellationToken)
    {
        var links = await db.DesignLayerLinks
            .Where(x => x.ProjectId == request.ProjectId && x.DesignFileId == request.DesignFileId)
            .ToListAsync(cancellationToken);

        var contentMap = await db.ContentItems
            .Where(x => links.Select(l => l.ContentItemId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var issues = new List<object>();

        foreach (var link in links)
        {
            var broken = !contentMap.TryGetValue(link.ContentItemId, out var item) || string.IsNullOrWhiteSpace(item.Key);
            if (broken)
            {
                issues.Add(new
                {
                    issueType = "broken_link",
                    designFileId = link.DesignFileId,
                    layerId = link.LayerId,
                    focus = new { link.DesignFileId, link.LayerId },
                    message = "Layer link is broken or item key invalid."
                });
            }
        }

        var linkedLayerIds = links.Select(x => x.LayerId).ToHashSet(StringComparer.Ordinal);
        foreach (var layerId in request.MonitoredLayerIds ?? Array.Empty<string>())
        {
            if (linkedLayerIds.Contains(layerId)) continue;
            issues.Add(new
            {
                issueType = "unlinked_candidate",
                designFileId = request.DesignFileId,
                layerId,
                focus = new { designFileId = request.DesignFileId, layerId },
                message = "Layer appears unlinked and can be linked to content."
            });
        }

        return Ok(new { issues });
    }
}
