using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/integration/exports")]
public sealed class ExportBundlesController(AppDbContext db) : ControllerBase
{
    private static readonly HashSet<string> AllowedIdempotencyOperations = new(StringComparer.OrdinalIgnoreCase)
    {
        "export_bundle"
    };

    [HttpGet("idempotency-audit")]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> IdempotencyAudit(
        [FromQuery] string operation = "export_bundle",
        [FromQuery] int limit = 100,
        [FromQuery] int minHitCount = 1,
        [FromQuery] DateTime? sinceUtc = null,
        CancellationToken cancellationToken = default)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Generated-At-Utc"] = DateTime.UtcNow.ToString("O");

        var normalizedOperation = operation.Trim().ToLowerInvariant();
        if (!AllowedIdempotencyOperations.Contains(normalizedOperation))
            return BadRequest(new { error = "unsupported_operation", allowed = AllowedIdempotencyOperations.OrderBy(x => x) });

        var clampedLimit = Math.Clamp(limit, 1, 500);
        var clampedMinHits = Math.Max(1, minHitCount);

        var query = db.IdempotencyRecords
            .Where(x => x.Operation == normalizedOperation && x.HitCount >= clampedMinHits);

        if (sinceUtc.HasValue && sinceUtc.Value > DateTime.UtcNow.AddMinutes(5))
            return BadRequest(new { error = "invalid_sinceUtc", guidance = "sinceUtc cannot be in the far future" });

        if (sinceUtc.HasValue)
        {
            query = query.Where(x => x.LastSeenUtc >= sinceUtc.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(x => x.LastSeenUtc)
            .Take(clampedLimit)
            .Select(x => new
            {
                x.Operation,
                x.Key,
                x.HitCount,
                x.FirstSeenUtc,
                x.LastSeenUtc
            })
            .ToListAsync(cancellationToken);

        Response.Headers["X-Total-Count"] = total.ToString();
        Response.Headers["X-Result-Limit"] = clampedLimit.ToString();

        return Ok(new
        {
            generatedAtUtc = DateTime.UtcNow,
            count = rows.Count,
            total,
            truncated = total > rows.Count,
            filters = new { operation = normalizedOperation, minHitCount = clampedMinHits, sinceUtc, limit = clampedLimit },
            rows
        });
    }

    private static readonly ConcurrentDictionary<string, Queue<DateTime>> RequestHistory = new();
    private const int MaxPerMinute = 30;

    [HttpGet("bundle")]
    public async Task<IActionResult> Bundle(
        [FromQuery] Guid projectId,
        [FromQuery] string? language,
        [FromQuery] string? @namespace,
        [FromQuery] string? version,
        CancellationToken cancellationToken)
    {
        if (projectId == Guid.Empty) return BadRequest(new { error = "projectId_required" });

        var auth = await ValidateTokenAsync(requiredScope: "exports:read", projectId, cancellationToken);
        if (auth.Result is not null) return auth.Result;

        var requestId = Request.Headers["X-Request-Id"].ToString().Trim();
        var idempotencyKey = string.IsNullOrWhiteSpace(requestId) ? null : $"{auth.TokenHash}:{requestId}";
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await db.IdempotencyRecords.FirstOrDefaultAsync(x => x.Operation == "export_bundle" && x.Key == idempotencyKey, cancellationToken);
            if (existing is not null)
            {
                existing.HitCount += 1;
                existing.LastSeenUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);

                Response.Headers["X-Idempotency-Replay"] = "true";
                Response.Headers["X-Idempotency-HitCount"] = existing.HitCount.ToString();

                var parsed = JsonSerializer.Deserialize<object>(existing.ResponseJson);
                return Ok(parsed);
            }
        }

        var tokenKey = auth.TokenHash!;
        var now = DateTime.UtcNow;
        var queue = RequestHistory.GetOrAdd(tokenKey, _ => new Queue<DateTime>());
        lock (queue)
        {
            while (queue.Count > 0 && (now - queue.Peek()).TotalMinutes >= 1) queue.Dequeue();
            if (queue.Count >= MaxPerMinute)
            {
                Response.Headers["X-RateLimit-Limit"] = MaxPerMinute.ToString();
                Response.Headers["X-RateLimit-Remaining"] = "0";
                Response.Headers["Retry-After"] = "60";
                Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds().ToString();
                return StatusCode(StatusCodes.Status429TooManyRequests, new { error = "rate_limit_exceeded" });
            }
            queue.Enqueue(now);
            Response.Headers["X-RateLimit-Limit"] = MaxPerMinute.ToString();
            Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, MaxPerMinute - queue.Count).ToString();
            Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds().ToString();
        }

        // Resolve which version to serve from
        var useSnapshot = false;
        Guid? resolvedVersionId = null;
        var normalizedVersion = version?.Trim().ToLowerInvariant();

        if (normalizedVersion == "working")
        {
            // Explicit working copy override — use content items
            useSnapshot = false;
        }
        else if (!string.IsNullOrWhiteSpace(normalizedVersion) && normalizedVersion != "live" && Guid.TryParse(normalizedVersion, out var specificVersionId))
        {
            // Specific version ID
            var versionExists = await db.ProjectVersions.AnyAsync(x => x.Id == specificVersionId && x.ProjectId == projectId, cancellationToken);
            if (!versionExists) return NotFound(new { error = "version_not_found" });
            resolvedVersionId = specificVersionId;
            useSnapshot = true;
        }
        else
        {
            // Default: use live version if one exists, otherwise working copy
            var liveVersion = await db.ProjectVersions.FirstOrDefaultAsync(x => x.ProjectId == projectId && x.IsLive, cancellationToken);
            if (liveVersion is not null)
            {
                resolvedVersionId = liveVersion.Id;
                useSnapshot = true;
            }
        }

        var payload = new Dictionary<string, Dictionary<string, string>>();

        if (useSnapshot && resolvedVersionId.HasValue)
        {
            // Serve from version snapshot
            var snapshotsQuery = db.ProjectVersionSnapshots.Where(x => x.VersionId == resolvedVersionId.Value);
            if (!string.IsNullOrWhiteSpace(@namespace))
            {
                var prefix = @namespace.Trim() + ".";
                snapshotsQuery = snapshotsQuery.Where(x => x.Key.StartsWith(prefix));
            }

            var snapshots = await snapshotsQuery.OrderBy(x => x.Key).ToListAsync(cancellationToken);

            if (snapshots.Count == 0)
            {
                return Ok(new { empty = true, files = new Dictionary<string, object>() });
            }

            foreach (var snap in snapshots)
            {
                var key = snap.Key;
                var value = snap.Source;

                if (!string.IsNullOrWhiteSpace(language) && !string.IsNullOrWhiteSpace(snap.TranslationsJson) && snap.TranslationsJson != "{}")
                {
                    try
                    {
                        var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(snap.TranslationsJson);
                        if (translations is not null && translations.TryGetValue(language, out var translatedText) && !string.IsNullOrWhiteSpace(translatedText))
                        {
                            value = translatedText;
                        }
                    }
                    catch (JsonException)
                    {
                        // Ignore malformed JSON, fall back to source
                    }
                }

                var ns = "common";
                var leaf = key;
                var idx = key.IndexOf('.');
                if (idx > 0 && idx < key.Length - 1)
                {
                    ns = key[..idx];
                    leaf = key[(idx + 1)..];
                }

                if (!payload.TryGetValue(ns, out var bucket))
                {
                    bucket = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    payload[ns] = bucket;
                }

                bucket[leaf] = value;
            }
        }
        else
        {
            // Serve from working copy (original behavior)
            var itemsQuery = db.ContentItems.Where(x => x.ProjectId == projectId && x.Status == "approved");
            if (!string.IsNullOrWhiteSpace(@namespace))
            {
                var prefix = @namespace.Trim() + ".";
                itemsQuery = itemsQuery.Where(x => x.Key.StartsWith(prefix));
            }

            var items = await itemsQuery.OrderBy(x => x.Key).ToListAsync(cancellationToken);

            if (items.Count == 0)
            {
                return Ok(new { empty = true, files = new Dictionary<string, object>() });
            }

            var contentIds = items.Select(x => x.Id).ToList();
            var tasks = await db.ContentItemLanguageTasks.Where(x => contentIds.Contains(x.ContentItemId)).ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                var key = item.Key;
                var value = item.Source;

                if (!string.IsNullOrWhiteSpace(language))
                {
                    var t = tasks.FirstOrDefault(x => x.ContentItemId == item.Id && x.LanguageCode == language && (x.Status == "approved" || x.Status == "done"));
                    if (t is not null && !string.IsNullOrWhiteSpace(t.TranslationText)) value = t.TranslationText;
                }

                var ns = "common";
                var leaf = key;
                var idx = key.IndexOf('.');
                if (idx > 0 && idx < key.Length - 1)
                {
                    ns = key[..idx];
                    leaf = key[(idx + 1)..];
                }

                if (!payload.TryGetValue(ns, out var bucket))
                {
                    bucket = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    payload[ns] = bucket;
                }

                bucket[leaf] = value;
            }
        }

        var bundle = new
        {
            schema = "neutral.v1",
            projectId,
            language = language ?? "source",
            files = payload
        };

        var json = JsonSerializer.Serialize(bundle);
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        var signedBundleUrl = $"data:application/json;base64,{base64}";

        var responseObj = new { signedBundleUrl, payload = bundle };

        if (!string.IsNullOrWhiteSpace(requestId))
        {
            db.IdempotencyRecords.Add(new IdempotencyRecord
            {
                Operation = "export_bundle",
                Key = idempotencyKey!,
                ResponseJson = JsonSerializer.Serialize(responseObj),
                HitCount = 1,
                FirstSeenUtc = DateTime.UtcNow,
                LastSeenUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(responseObj);
    }

    [HttpGet("locales")]
    public async Task<IActionResult> Locales(
        [FromQuery] Guid projectId,
        [FromQuery] string? version,
        CancellationToken cancellationToken)
    {
        if (projectId == Guid.Empty) return BadRequest(new { error = "projectId_required" });

        var auth = await ValidateTokenAsync(requiredScope: "exports:read", projectId, cancellationToken);
        if (auth.Result is not null) return auth.Result;

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

        var languages = await db.ProjectLanguages
            .AsNoTracking()
            .Where(x => x.ProjectId == projectId && x.IsActive)
            .OrderBy(x => x.Bcp47Code)
            .ToListAsync(cancellationToken);

        // (key, languageCode-or-null) -> value
        var sourceByKey = new Dictionary<string, string>(StringComparer.Ordinal);
        var translationsByKey = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

        if (useSnapshot && resolvedVersionId.HasValue)
        {
            var snapshots = await db.ProjectVersionSnapshots
                .AsNoTracking()
                .Where(x => x.VersionId == resolvedVersionId.Value)
                .OrderBy(x => x.Key)
                .ToListAsync(cancellationToken);

            foreach (var snap in snapshots)
            {
                sourceByKey[snap.Key] = snap.Source ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(snap.TranslationsJson) && snap.TranslationsJson != "{}")
                {
                    try
                    {
                        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(snap.TranslationsJson);
                        if (dict is not null) translationsByKey[snap.Key] = dict;
                    }
                    catch (JsonException) { }
                }
            }
        }
        else
        {
            var items = await db.ContentItems
                .AsNoTracking()
                .Where(x => x.ProjectId == projectId)
                .OrderBy(x => x.Key)
                .ToListAsync(cancellationToken);
            var itemIds = items.Select(x => x.Id).ToList();
            var tasks = await db.ContentItemLanguageTasks
                .AsNoTracking()
                .Where(x => itemIds.Contains(x.ContentItemId))
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                sourceByKey[item.Key] = item.Source ?? string.Empty;
                var perLang = tasks
                    .Where(t => t.ContentItemId == item.Id && !string.IsNullOrWhiteSpace(t.TranslationText))
                    .ToDictionary(t => t.LanguageCode, t => t.TranslationText!, StringComparer.Ordinal);
                if (perLang.Count > 0) translationsByKey[item.Key] = perLang;
            }
        }

        var sourceCode = languages.FirstOrDefault(x => x.IsSource)?.Bcp47Code ?? "en";
        var result = new Dictionary<string, object>(StringComparer.Ordinal);

        // source -> nested
        var sourceNested = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var kvp in sourceByKey) SetNested(sourceNested, kvp.Key, kvp.Value);
        result["source"] = sourceNested;
        result[sourceCode] = sourceNested;

        foreach (var lang in languages.Where(x => !x.IsSource))
        {
            var nested = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var kvp in sourceByKey)
            {
                var value = kvp.Value;
                if (translationsByKey.TryGetValue(kvp.Key, out var perLang)
                    && perLang.TryGetValue(lang.Bcp47Code, out var translated)
                    && !string.IsNullOrWhiteSpace(translated))
                {
                    value = translated;
                }
                SetNested(nested, kvp.Key, value);
            }
            result[lang.Bcp47Code] = nested;
        }

        return Ok(result);
    }

    private static void SetNested(Dictionary<string, object?> root, string dottedKey, string value)
    {
        if (string.IsNullOrWhiteSpace(dottedKey)) return;
        var parts = dottedKey.Split('.');
        var current = root;
        for (var i = 0; i < parts.Length - 1; i++)
        {
            var part = parts[i];
            if (!current.TryGetValue(part, out var next) || next is not Dictionary<string, object?> nextDict)
            {
                nextDict = new Dictionary<string, object?>(StringComparer.Ordinal);
                current[part] = nextDict;
            }
            current = nextDict;
        }
        current[parts[^1]] = value;
    }

    private async Task<(IActionResult? Result, string? TokenHash)> ValidateTokenAsync(string requiredScope, Guid projectId, CancellationToken cancellationToken)
    {
        var raw = Request.Headers["X-Api-Token"].ToString();
        if (string.IsNullOrWhiteSpace(raw)) return (StatusCode(StatusCodes.Status401Unauthorized, new { error = "api_token_required" }), null);

        var hash = IntegrationTokensController.Hash(raw.Trim());
        var token = await db.ApiTokens.FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (token is null || token.IsRevoked) return (StatusCode(StatusCodes.Status401Unauthorized, new { error = "invalid_token" }), null);
        if (token.ExpiresUtc <= DateTime.UtcNow) return (StatusCode(StatusCodes.Status401Unauthorized, new { error = "token_expired" }), null);

        var scopes = (token.Scope ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!scopes.Contains(requiredScope))
            return (StatusCode(StatusCodes.Status403Forbidden, new { error = "scope_denied", requiredScope }), null);

        var project = await db.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken);
        if (project is null)
            return (NotFound(new { error = "project_not_found" }), null);

        if (project.WorkspaceId != token.WorkspaceId)
            return (StatusCode(StatusCodes.Status403Forbidden, new { error = "project_out_of_scope" }), null);

        var scopedProjectIds = await db.ApiTokenProjectScopes
            .AsNoTracking()
            .Where(x => x.ApiTokenId == token.Id)
            .Select(x => x.ProjectId)
            .ToListAsync(cancellationToken);

        if (scopedProjectIds.Count > 0 && !scopedProjectIds.Contains(projectId))
            return (StatusCode(StatusCodes.Status403Forbidden, new { error = "project_out_of_scope" }), null);

        token.LastUsedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return (null, hash);
    }
}
