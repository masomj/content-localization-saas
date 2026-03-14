using System.Text.RegularExpressions;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record SetKeyConventionRequest(Guid ProjectId, string Convention, string? Prefix);

[ApiController]
[Route("api/key-conventions")]
public sealed class KeyConventionsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid projectId, CancellationToken cancellationToken)
    {
        var row = await db.ProjectKeyConventions.FirstOrDefaultAsync(x => x.ProjectId == projectId, cancellationToken);
        if (row is null) return Ok(new { projectId, convention = "dot.case", prefix = "" });
        return Ok(row);
    }

    [HttpPost]
    public async Task<IActionResult> Set([FromBody] SetKeyConventionRequest request, CancellationToken cancellationToken)
    {
        if (request.ProjectId == Guid.Empty || string.IsNullOrWhiteSpace(request.Convention))
            return BadRequest(new { error = "projectId_and_convention_required" });

        var convention = request.Convention.Trim().ToLowerInvariant();
        if (convention is not ("dot.case" or "snake_case" or "kebab-case"))
            return BadRequest(new { error = "unsupported_convention" });

        var row = await db.ProjectKeyConventions.FirstOrDefaultAsync(x => x.ProjectId == request.ProjectId, cancellationToken);
        if (row is null)
        {
            row = new ProjectKeyConvention { ProjectId = request.ProjectId };
            db.ProjectKeyConventions.Add(row);
        }

        row.Convention = convention;
        row.Prefix = request.Prefix?.Trim() ?? string.Empty;
        row.UpdatedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return Ok(row);
    }

    [HttpGet("suggest")]
    public async Task<IActionResult> Suggest([FromQuery] Guid projectId, [FromQuery] string text, CancellationToken cancellationToken)
    {
        var row = await db.ProjectKeyConventions.FirstOrDefaultAsync(x => x.ProjectId == projectId, cancellationToken);
        var convention = row?.Convention ?? "dot.case";
        var prefix = row?.Prefix ?? string.Empty;

        var suggestion = BuildKey(text, convention, prefix);
        return Ok(new { suggestion, convention, prefix });
    }

    [HttpGet("validate")]
    public async Task<IActionResult> Validate([FromQuery] Guid projectId, [FromQuery] string key, CancellationToken cancellationToken)
    {
        var row = await db.ProjectKeyConventions.FirstOrDefaultAsync(x => x.ProjectId == projectId, cancellationToken);
        var convention = row?.Convention ?? "dot.case";
        var prefix = row?.Prefix ?? string.Empty;

        var errors = ValidateKeyAgainstConvention(key, convention, prefix);
        return Ok(new { valid = errors.Count == 0, errors, convention, prefix });
    }

    [HttpGet("migration-report")]
    public async Task<IActionResult> MigrationReport([FromQuery] Guid projectId, CancellationToken cancellationToken)
    {
        var row = await db.ProjectKeyConventions.FirstOrDefaultAsync(x => x.ProjectId == projectId, cancellationToken);
        var convention = row?.Convention ?? "dot.case";
        var prefix = row?.Prefix ?? string.Empty;

        var items = await db.ContentItems.Where(x => x.ProjectId == projectId).OrderBy(x => x.Key).ToListAsync(cancellationToken);
        var report = items.Select(item => new
        {
            currentKey = item.Key,
            suggestedKey = BuildKey(item.Key.Replace('.', ' '), convention, prefix),
            needsMigration = !string.Equals(item.Key, BuildKey(item.Key.Replace('.', ' '), convention, prefix), StringComparison.Ordinal)
        }).ToList();

        return Ok(new { projectId, convention, prefix, report });
    }

    private static List<string> ValidateKeyAgainstConvention(string key, string convention, string prefix)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(key))
        {
            errors.Add("Key cannot be empty.");
            return errors;
        }

        string pattern = convention switch
        {
            "dot.case" => "^[a-z0-9]+(\\.[a-z0-9]+)+$",
            "snake_case" => "^[a-z0-9]+(_[a-z0-9]+)+$",
            "kebab-case" => "^[a-z0-9]+(-[a-z0-9]+)+$",
            _ => "^[a-z0-9]+$"
        };

        if (!Regex.IsMatch(key, pattern))
        {
            errors.Add($"Key must follow {convention} format.");
        }

        if (!string.IsNullOrWhiteSpace(prefix) && !key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"Key must start with prefix '{prefix}'.");
        }

        return errors;
    }

    private static string BuildKey(string text, string convention, string prefix)
    {
        var cleaned = Regex.Replace(text.ToLowerInvariant(), "[^a-z0-9]+", " ").Trim();
        var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var joined = convention switch
        {
            "snake_case" => string.Join('_', parts),
            "kebab-case" => string.Join('-', parts),
            _ => string.Join('.', parts)
        };

        if (!string.IsNullOrWhiteSpace(prefix))
        {
            return $"{prefix}{joined}";
        }

        return joined;
    }
}
