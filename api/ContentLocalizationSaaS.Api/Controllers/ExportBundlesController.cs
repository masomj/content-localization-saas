using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/integration/exports")]
public sealed class ExportBundlesController(AppDbContext db) : ControllerBase
{
    private static readonly ConcurrentDictionary<string, Queue<DateTime>> RequestHistory = new();
    private const int MaxPerMinute = 30;

    [HttpGet("bundle")]
    public async Task<IActionResult> Bundle(
        [FromQuery] Guid projectId,
        [FromQuery] string? language,
        [FromQuery] string? @namespace,
        CancellationToken cancellationToken)
    {
        var auth = await ValidateTokenAsync(requiredScope: "exports:read", cancellationToken);
        if (auth.Result is not null) return auth.Result;

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

        if (projectId == Guid.Empty) return BadRequest(new { error = "projectId_required" });

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

        var payload = new Dictionary<string, Dictionary<string, string>>();
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

        return Ok(new { signedBundleUrl, payload = bundle });
    }

    private async Task<(IActionResult? Result, string? TokenHash)> ValidateTokenAsync(string requiredScope, CancellationToken cancellationToken)
    {
        var raw = Request.Headers["X-Api-Token"].ToString();
        if (string.IsNullOrWhiteSpace(raw)) return (StatusCode(StatusCodes.Status401Unauthorized, new { error = "api_token_required" }), null);

        var hash = IntegrationTokensController.Hash(raw.Trim());
        var token = await db.ApiTokens.FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (token is null || token.IsRevoked) return (StatusCode(StatusCodes.Status401Unauthorized, new { error = "invalid_token" }), null);
        if (token.ExpiresUtc <= DateTime.UtcNow) return (StatusCode(StatusCodes.Status401Unauthorized, new { error = "token_expired" }), null);

        token.LastUsedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var scopes = (token.Scope ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!scopes.Contains(requiredScope))
            return (StatusCode(StatusCodes.Status403Forbidden, new { error = "scope_denied", requiredScope }), null);

        return (null, hash);
    }
}
