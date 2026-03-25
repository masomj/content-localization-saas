using System.Text.Json;
using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record CreateVersionRequest(string Tag, string Title, string Notes);
public sealed record UpdateVersionRequest(string Title, string Notes);

[ApiController]
[Route("api/projects/{projectId:guid}/versions")]
public sealed class ProjectVersionsController(AppDbContext db) : ControllerBase
{
    private string CurrentActor => HttpContext.Request.Headers["X-Actor-Email"].ToString() is { Length: > 0 } raw
        ? raw.Trim().ToLowerInvariant()
        : "member@example.com";

    // 1. GET /api/projects/{projectId}/versions — list all versions
    [HttpGet]
    public async Task<IActionResult> List(Guid projectId, CancellationToken cancellationToken)
    {
        var versions = await db.ProjectVersions
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.CreatedUtc)
            .Select(x => new
            {
                x.Id,
                x.Tag,
                x.Title,
                notes = x.Notes.Length > 200 ? x.Notes.Substring(0, 200) + "..." : x.Notes,
                x.IsLive,
                x.ContentItemCount,
                x.TranslationCount,
                x.CreatedByEmail,
                x.CreatedUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(versions);
    }

    // 2. GET /api/projects/{projectId}/versions/{versionId} — single version with full notes
    [HttpGet("{versionId:guid}")]
    public async Task<IActionResult> Get(Guid projectId, Guid versionId, CancellationToken cancellationToken)
    {
        var version = await db.ProjectVersions
            .FirstOrDefaultAsync(x => x.Id == versionId && x.ProjectId == projectId, cancellationToken);

        if (version is null) return NotFound();

        return Ok(new
        {
            version.Id,
            version.Tag,
            version.Title,
            version.Notes,
            version.IsLive,
            version.ContentItemCount,
            version.TranslationCount,
            version.CreatedByEmail,
            version.CreatedUtc
        });
    }

    // 3. POST /api/projects/{projectId}/versions — create a new version (cut a release)
    [HttpPost]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Create(Guid projectId, [FromBody] CreateVersionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Tag))
            return BadRequest(new { error = "tag_required" });

        var tagExists = await db.ProjectVersions
            .AnyAsync(x => x.ProjectId == projectId && x.Tag == request.Tag.Trim(), cancellationToken);
        if (tagExists)
            return Conflict(new { error = "tag_already_exists", tag = request.Tag.Trim() });

        // Load all content items for the project
        var contentItems = await db.ContentItems
            .Where(x => x.ProjectId == projectId)
            .OrderBy(x => x.Key)
            .ToListAsync(cancellationToken);

        var contentItemIds = contentItems.Select(x => x.Id).ToList();

        // Load approved translations for these content items
        var approvedTasks = await db.ContentItemLanguageTasks
            .Where(x => contentItemIds.Contains(x.ContentItemId) && (x.Status == "approved" || x.Status == "done"))
            .ToListAsync(cancellationToken);

        // Group translations by content item
        var translationsByItem = approvedTasks
            .GroupBy(x => x.ContentItemId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var totalTranslations = approvedTasks.Count;

        // Create the version
        var version = new ProjectVersion
        {
            ProjectId = projectId,
            Tag = request.Tag.Trim(),
            Title = request.Title?.Trim() ?? string.Empty,
            Notes = request.Notes?.Trim() ?? string.Empty,
            IsLive = false,
            CreatedByEmail = CurrentActor,
            ContentItemCount = contentItems.Count,
            TranslationCount = totalTranslations,
            CreatedUtc = DateTime.UtcNow
        };

        db.ProjectVersions.Add(version);

        // Snapshot all content items with their approved translations
        foreach (var item in contentItems)
        {
            var translationsJson = "{}";
            if (translationsByItem.TryGetValue(item.Id, out var translations) && translations.Count > 0)
            {
                var dict = translations.ToDictionary(t => t.LanguageCode, t => t.TranslationText);
                translationsJson = JsonSerializer.Serialize(dict);
            }

            db.ProjectVersionSnapshots.Add(new ProjectVersionSnapshot
            {
                VersionId = version.Id,
                OriginalContentItemId = item.Id,
                Key = item.Key,
                Source = item.Source,
                Status = item.Status,
                Tags = item.Tags,
                CollectionId = item.CollectionId,
                SortOrder = item.SortOrder,
                TranslationsJson = translationsJson,
                CreatedUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { projectId, versionId = version.Id }, new
        {
            version.Id,
            version.Tag,
            version.Title,
            version.Notes,
            version.IsLive,
            version.ContentItemCount,
            version.TranslationCount,
            version.CreatedByEmail,
            version.CreatedUtc
        });
    }

    // 4. PUT /api/projects/{projectId}/versions/{versionId} — update version metadata
    [HttpPut("{versionId:guid}")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Update(Guid projectId, Guid versionId, [FromBody] UpdateVersionRequest request, CancellationToken cancellationToken)
    {
        var version = await db.ProjectVersions
            .FirstOrDefaultAsync(x => x.Id == versionId && x.ProjectId == projectId, cancellationToken);

        if (version is null) return NotFound();

        version.Title = request.Title?.Trim() ?? version.Title;
        version.Notes = request.Notes?.Trim() ?? version.Notes;

        await db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            version.Id,
            version.Tag,
            version.Title,
            version.Notes,
            version.IsLive,
            version.ContentItemCount,
            version.TranslationCount,
            version.CreatedByEmail,
            version.CreatedUtc
        });
    }

    // 5. DELETE /api/projects/{projectId}/versions/{versionId} — delete a version
    [HttpDelete("{versionId:guid}")]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> Delete(Guid projectId, Guid versionId, CancellationToken cancellationToken)
    {
        var version = await db.ProjectVersions
            .FirstOrDefaultAsync(x => x.Id == versionId && x.ProjectId == projectId, cancellationToken);

        if (version is null) return NotFound();

        if (version.IsLive)
            return BadRequest(new { error = "cannot_delete_live_version", guidance = "Demote the version before deleting it." });

        db.ProjectVersions.Remove(version);
        await db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // 6. POST /api/projects/{projectId}/versions/{versionId}/promote — mark as live
    [HttpPost("{versionId:guid}/promote")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Promote(Guid projectId, Guid versionId, CancellationToken cancellationToken)
    {
        var version = await db.ProjectVersions
            .FirstOrDefaultAsync(x => x.Id == versionId && x.ProjectId == projectId, cancellationToken);

        if (version is null) return NotFound();

        if (version.IsLive)
            return Ok(new { message = "already_live", version.Id, version.Tag });

        // Demote any existing live version for this project
        var currentLive = await db.ProjectVersions
            .FirstOrDefaultAsync(x => x.ProjectId == projectId && x.IsLive, cancellationToken);
        if (currentLive is not null)
        {
            currentLive.IsLive = false;
        }

        version.IsLive = true;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "promoted",
            version.Id,
            version.Tag,
            version.IsLive,
            demotedVersionId = currentLive?.Id
        });
    }

    // 7. POST /api/projects/{projectId}/versions/{versionId}/demote — remove live status
    [HttpPost("{versionId:guid}/demote")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Demote(Guid projectId, Guid versionId, CancellationToken cancellationToken)
    {
        var version = await db.ProjectVersions
            .FirstOrDefaultAsync(x => x.Id == versionId && x.ProjectId == projectId, cancellationToken);

        if (version is null) return NotFound();

        if (!version.IsLive)
            return Ok(new { message = "already_not_live", version.Id, version.Tag });

        version.IsLive = false;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            message = "demoted",
            version.Id,
            version.Tag,
            version.IsLive
        });
    }

    // 8. GET /api/projects/{projectId}/versions/{versionId}/content — snapshot content
    [HttpGet("{versionId:guid}/content")]
    public async Task<IActionResult> GetContent(Guid projectId, Guid versionId, CancellationToken cancellationToken)
    {
        var versionExists = await db.ProjectVersions
            .AnyAsync(x => x.Id == versionId && x.ProjectId == projectId, cancellationToken);

        if (!versionExists) return NotFound();

        var snapshots = await db.ProjectVersionSnapshots
            .Where(x => x.VersionId == versionId)
            .OrderBy(x => x.Key)
            .Select(x => new
            {
                x.Id,
                x.VersionId,
                x.OriginalContentItemId,
                x.Key,
                x.Source,
                x.Status,
                x.Tags,
                x.CollectionId,
                x.SortOrder,
                x.TranslationsJson
            })
            .ToListAsync(cancellationToken);

        return Ok(snapshots);
    }

    // 9. GET /api/projects/{projectId}/versions/compare?from={versionId}&to={versionId}
    [HttpGet("compare")]
    public async Task<IActionResult> Compare(Guid projectId, [FromQuery] Guid from, [FromQuery] Guid to, CancellationToken cancellationToken)
    {
        var fromVersion = await db.ProjectVersions
            .FirstOrDefaultAsync(x => x.Id == from && x.ProjectId == projectId, cancellationToken);
        var toVersion = await db.ProjectVersions
            .FirstOrDefaultAsync(x => x.Id == to && x.ProjectId == projectId, cancellationToken);

        if (fromVersion is null || toVersion is null)
            return NotFound(new { error = "version_not_found" });

        var fromSnapshots = await db.ProjectVersionSnapshots
            .Where(x => x.VersionId == from)
            .ToListAsync(cancellationToken);

        var toSnapshots = await db.ProjectVersionSnapshots
            .Where(x => x.VersionId == to)
            .ToListAsync(cancellationToken);

        var fromByKey = fromSnapshots.ToDictionary(x => x.Key);
        var toByKey = toSnapshots.ToDictionary(x => x.Key);

        var added = new List<object>();
        var removed = new List<object>();
        var changed = new List<object>();

        // Keys in "to" but not in "from" => added
        foreach (var kvp in toByKey)
        {
            if (!fromByKey.ContainsKey(kvp.Key))
            {
                added.Add(new { key = kvp.Key, source = kvp.Value.Source });
            }
        }

        // Keys in "from" but not in "to" => removed
        foreach (var kvp in fromByKey)
        {
            if (!toByKey.ContainsKey(kvp.Key))
            {
                removed.Add(new { key = kvp.Key, source = kvp.Value.Source });
            }
        }

        // Keys in both but with different source => changed
        foreach (var kvp in fromByKey)
        {
            if (toByKey.TryGetValue(kvp.Key, out var toSnapshot) && kvp.Value.Source != toSnapshot.Source)
            {
                changed.Add(new { key = kvp.Key, oldSource = kvp.Value.Source, newSource = toSnapshot.Source });
            }
        }

        return Ok(new { added, removed, changed });
    }
}
