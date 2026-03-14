using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record PluginPullRequest(Guid ProjectId, string DesignFileId, string? LanguageCode);
public sealed record PluginPushItem(string LayerId, string BaseSourceText, string NewText);
public sealed record PluginPushRequest(Guid ProjectId, string DesignFileId, PluginPushItem[] Items);

[ApiController]
[Route("api/plugin-sync")]
public sealed class PluginSyncController(AppDbContext db) : ControllerBase
{
    [HttpPost("pull")]
    public async Task<IActionResult> Pull([FromBody] PluginPullRequest request, CancellationToken cancellationToken)
    {
        var links = await db.DesignLayerLinks
            .Where(x => x.ProjectId == request.ProjectId && x.DesignFileId == request.DesignFileId)
            .ToListAsync(cancellationToken);

        var contentIds = links.Select(x => x.ContentItemId).Distinct().ToList();
        var items = await db.ContentItems.Where(x => contentIds.Contains(x.Id) && x.Status == "approved").ToListAsync(cancellationToken);

        var payload = new List<object>();
        foreach (var link in links)
        {
            var item = items.FirstOrDefault(x => x.Id == link.ContentItemId);
            if (item is null) continue;

            string text = item.Source;
            if (!string.IsNullOrWhiteSpace(request.LanguageCode))
            {
                var task = await db.ContentItemLanguageTasks.FirstOrDefaultAsync(
                    x => x.ContentItemId == item.Id && x.LanguageCode == request.LanguageCode && (x.Status == "approved" || x.Status == "done"),
                    cancellationToken);
                if (task is not null && !string.IsNullOrWhiteSpace(task.TranslationText))
                {
                    text = task.TranslationText;
                }
            }

            payload.Add(new
            {
                link.LayerId,
                contentItemId = item.Id,
                text,
                language = request.LanguageCode ?? "source"
            });
        }

        return Ok(new { updates = payload });
    }

    [HttpPost("push")]
    public async Task<IActionResult> Push([FromBody] PluginPushRequest request, CancellationToken cancellationToken)
    {
        var conflicts = new List<object>();
        var updates = new List<object>();

        foreach (var incoming in request.Items ?? [])
        {
            var link = await db.DesignLayerLinks.FirstOrDefaultAsync(
                x => x.ProjectId == request.ProjectId && x.DesignFileId == request.DesignFileId && x.LayerId == incoming.LayerId,
                cancellationToken);
            if (link is null) continue;

            var item = await db.ContentItems.FirstOrDefaultAsync(x => x.Id == link.ContentItemId, cancellationToken);
            if (item is null) continue;

            if (!string.Equals(item.Source, incoming.BaseSourceText, StringComparison.Ordinal))
            {
                var conflict = new PluginSyncConflict
                {
                    DesignLayerLinkId = link.Id,
                    CurrentText = item.Source,
                    ProposedText = incoming.NewText,
                    ResolutionState = "open"
                };
                db.PluginSyncConflicts.Add(conflict);
                await db.SaveChangesAsync(cancellationToken);

                conflicts.Add(new
                {
                    layerId = link.LayerId,
                    contentItemId = item.Id,
                    currentText = item.Source,
                    proposedText = incoming.NewText,
                    conflictId = conflict.Id
                });
                continue;
            }

            var previousStatus = item.Status;
            var previousSource = item.Source;
            item.Source = incoming.NewText;
            item.Status = "draft";

            db.ContentItemRevisions.Add(new ContentItemRevision
            {
                ContentItemId = item.Id,
                ActorEmail = "plugin@figma",
                PreviousSource = previousSource,
                NewSource = item.Source,
                PreviousStatus = previousStatus,
                NewStatus = item.Status,
                DiffSummary = "plugin push update",
                EventType = "plugin_push"
            });

            updates.Add(new { layerId = link.LayerId, contentItemId = item.Id, status = "updated" });
        }

        await db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            updates,
            conflicts,
            requiresConflictResolution = conflicts.Count > 0
        });
    }
}
