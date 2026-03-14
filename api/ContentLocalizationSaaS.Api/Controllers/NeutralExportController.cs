using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/exports")]
public sealed class NeutralExportController(AppDbContext db) : ControllerBase
{
    [HttpGet("neutral")]
    public async Task<IActionResult> ExportNeutral([FromQuery] Guid projectId, CancellationToken cancellationToken)
    {
        if (projectId == Guid.Empty) return BadRequest(new { error = "projectId_required" });

        var items = await db.ContentItems.Where(x => x.ProjectId == projectId).OrderBy(x => x.Key).ToListAsync(cancellationToken);
        var languages = await db.ProjectLanguages.Where(x => x.ProjectId == projectId && x.IsActive).OrderBy(x => x.Bcp47Code).ToListAsync(cancellationToken);
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

    private static List<object> ValidateConflicts(IReadOnlyList<ContentLocalizationSaaS.Domain.ContentItem> items)
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

    private static Dictionary<string, Dictionary<string, string>> BuildLanguageMap(
        IReadOnlyList<ContentLocalizationSaaS.Domain.ContentItem> items,
        IReadOnlyList<ContentLocalizationSaaS.Domain.ContentItemLanguageTask> tasks,
        string? languageCode)
    {
        var file = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            var (ns, key) = SplitNamespaceKey(item.Key);
            if (!file.TryGetValue(ns, out var bucket))
            {
                bucket = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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
