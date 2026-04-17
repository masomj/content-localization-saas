using System.Text.Json;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/exports")]
public sealed class NeutralExportController(AppDbContext db) : ControllerBase
{
    [HttpGet("neutral")]
    public async Task<IActionResult> ExportNeutral(
        [FromQuery] Guid projectId,
        [FromQuery] string? version,
        CancellationToken cancellationToken)
    {
        if (projectId == Guid.Empty) return BadRequest(new { error = "projectId_required" });

        // Resolve which version to serve from
        var useSnapshot = false;
        Guid? resolvedVersionId = null;
        var normalizedVersion = version?.Trim().ToLowerInvariant();

        if (normalizedVersion == "working")
        {
            useSnapshot = false;
        }
        else if (!string.IsNullOrWhiteSpace(normalizedVersion) && normalizedVersion != "live" && Guid.TryParse(normalizedVersion, out var specificVersionId))
        {
            var versionExists = await db.ProjectVersions.AnyAsync(x => x.Id == specificVersionId && x.ProjectId == projectId, cancellationToken);
            if (!versionExists) return NotFound(new { error = "version_not_found" });
            resolvedVersionId = specificVersionId;
            useSnapshot = true;
        }
        else
        {
            var liveVersion = await db.ProjectVersions.FirstOrDefaultAsync(x => x.ProjectId == projectId && x.IsLive, cancellationToken);
            if (liveVersion is not null)
            {
                resolvedVersionId = liveVersion.Id;
                useSnapshot = true;
            }
        }

        var languages = await db.ProjectLanguages.Where(x => x.ProjectId == projectId && x.IsActive).OrderBy(x => x.Bcp47Code).ToListAsync(cancellationToken);

        if (useSnapshot && resolvedVersionId.HasValue)
        {
            return await ExportFromSnapshot(projectId, resolvedVersionId.Value, languages, cancellationToken);
        }

        return await ExportFromWorkingCopy(projectId, languages, cancellationToken);
    }

    private async Task<IActionResult> ExportFromSnapshot(Guid projectId, Guid versionId, List<ProjectLanguage> languages, CancellationToken cancellationToken)
    {
        var snapshots = await db.ProjectVersionSnapshots
            .Where(x => x.VersionId == versionId)
            .OrderBy(x => x.Key)
            .ToListAsync(cancellationToken);

        var conflictErrors = ValidateSnapshotConflicts(snapshots);
        if (conflictErrors.Count > 0)
        {
            return BadRequest(new { error = "key_conflicts", conflicts = conflictErrors });
        }

        var result = new Dictionary<string, object>();

        // source export
        var sourceMap = BuildSnapshotLanguageMap(snapshots, languageCode: null);
        result["source"] = sourceMap;

        // per-target export
        foreach (var language in languages.Where(x => !x.IsSource))
        {
            var map = BuildSnapshotLanguageMap(snapshots, language.Bcp47Code);
            result[language.Bcp47Code] = map;
        }

        return Ok(new
        {
            schema = "neutral.v1",
            projectId,
            versionId,
            files = result
        });
    }

    private async Task<IActionResult> ExportFromWorkingCopy(Guid projectId, List<ProjectLanguage> languages, CancellationToken cancellationToken)
    {
        var items = await db.ContentItems.Where(x => x.ProjectId == projectId).OrderBy(x => x.Key).ToListAsync(cancellationToken);
        var tasks = await db.ContentItemLanguageTasks
            .Where(x => items.Select(i => i.Id).Contains(x.ContentItemId))
            .ToListAsync(cancellationToken);

        var conflictErrors = ValidateConflicts(items);
        if (conflictErrors.Count > 0)
        {
            return BadRequest(new { error = "key_conflicts", conflicts = conflictErrors });
        }

        var result = new Dictionary<string, object>();

        // source export
        var sourceMap = BuildLanguageMap(items, tasks, languageCode: null);
        result["source"] = sourceMap;

        // per-target export
        foreach (var language in languages.Where(x => !x.IsSource))
        {
            var map = BuildLanguageMap(items, tasks, language.Bcp47Code);
            result[language.Bcp47Code] = map;
        }

        return Ok(new
        {
            schema = "neutral.v1",
            projectId,
            files = result
        });
    }

    private static List<object> ValidateConflicts(IReadOnlyList<ContentItem> items)
    {
        var conflicts = new List<object>();
        var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            var (ns, key) = SplitNamespaceKey(item.Key);
            var normalized = $"{ns.ToLowerInvariant()}::{key.ToLowerInvariant()}";

            if (seen.TryGetValue(normalized, out var existingKey))
            {
                conflicts.Add(new
                {
                    namespaceName = ns,
                    key,
                    conflictingKeys = new[] { existingKey, item.Key },
                    message = "Duplicate namespace/key detected. Rename one of the keys."
                });
            }
            else
            {
                seen[normalized] = item.Key;
            }
        }

        return conflicts;
    }

    private static List<object> ValidateSnapshotConflicts(IReadOnlyList<ProjectVersionSnapshot> snapshots)
    {
        var conflicts = new List<object>();
        var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var snap in snapshots)
        {
            var (ns, key) = SplitNamespaceKey(snap.Key);
            var normalized = $"{ns.ToLowerInvariant()}::{key.ToLowerInvariant()}";

            if (seen.TryGetValue(normalized, out var existingKey))
            {
                conflicts.Add(new
                {
                    namespaceName = ns,
                    key,
                    conflictingKeys = new[] { existingKey, snap.Key },
                    message = "Duplicate namespace/key detected. Rename one of the keys."
                });
            }
            else
            {
                seen[normalized] = snap.Key;
            }
        }

        return conflicts;
    }

    private static Dictionary<string, Dictionary<string, object>> BuildLanguageMap(
        IReadOnlyList<ContentItem> items,
        IReadOnlyList<ContentItemLanguageTask> tasks,
        string? languageCode)
    {
        var file = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            var (ns, key) = SplitNamespaceKey(item.Key);
            if (!file.TryGetValue(ns, out var bucket))
            {
                bucket = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                file[ns] = bucket;
            }

            string value;
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                value = item.Source;
            }
            else
            {
                var task = tasks.FirstOrDefault(x => x.ContentItemId == item.Id && x.LanguageCode == languageCode);
                value = task is not null && !string.IsNullOrWhiteSpace(task.TranslationText)
                    ? task.TranslationText
                    : item.Source;
            }

            var entry = new Dictionary<string, object?> { ["value"] = value };
            if (!string.IsNullOrEmpty(item.Description)) entry["description"] = item.Description;
            if (item.MaxLength.HasValue) entry["maxLength"] = item.MaxLength.Value;
            if (!string.IsNullOrEmpty(item.ContentType)) entry["contentType"] = item.ContentType;
            bucket[key] = entry;
        }

        return file;
    }

    private static Dictionary<string, Dictionary<string, string>> BuildSnapshotLanguageMap(
        IReadOnlyList<ProjectVersionSnapshot> snapshots,
        string? languageCode)
    {
        var file = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var snap in snapshots)
        {
            var (ns, key) = SplitNamespaceKey(snap.Key);
            if (!file.TryGetValue(ns, out var bucket))
            {
                bucket = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                file[ns] = bucket;
            }

            string value;
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                value = snap.Source;
            }
            else
            {
                value = snap.Source; // default to source
                if (!string.IsNullOrWhiteSpace(snap.TranslationsJson) && snap.TranslationsJson != "{}")
                {
                    try
                    {
                        var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(snap.TranslationsJson);
                        if (translations is not null && translations.TryGetValue(languageCode, out var translatedText) && !string.IsNullOrWhiteSpace(translatedText))
                        {
                            value = translatedText;
                        }
                    }
                    catch (JsonException)
                    {
                        // Ignore malformed JSON, fall back to source
                    }
                }
            }

            bucket[key] = value;
        }

        return file;
    }

    private static (string Namespace, string Key) SplitNamespaceKey(string fullKey)
    {
        var key = fullKey?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(key)) return ("common", "unnamed_key");

        var idx = key.IndexOf('.');
        if (idx <= 0 || idx == key.Length - 1) return ("common", key);

        var ns = key[..idx];
        var leaf = key[(idx + 1)..];
        return (ns, leaf);
    }
}
