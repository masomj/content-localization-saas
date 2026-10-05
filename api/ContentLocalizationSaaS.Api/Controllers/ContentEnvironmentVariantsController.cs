using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Application;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record UpsertEnvironmentVariantRequest(string Environment, string? LanguageCode, string Value);

public sealed record EnvironmentVariantDto(
    Guid Id,
    Guid ContentItemId,
    string Environment,
    string LanguageCode,
    string Value,
    string UpdatedByEmail,
    DateTime UpdatedUtc);

public sealed record ProjectEnvironmentDto(string Name, int VariantCount);

/// <summary>
/// #57: environment-tagged copy. Variants replace production copy only in exports that
/// ask for their environment (?environment=dev), so production copy is never changed.
/// </summary>
[ApiController]
[Authorize]
public sealed class ContentEnvironmentVariantsController(AppDbContext db) : ControllerBase
{
    private const int MaxValueLength = 4000;

    [HttpGet("api/projects/{projectId:guid}/environments")]
    public async Task<IActionResult> ListEnvironments(Guid projectId, CancellationToken ct)
    {
        if (!await CanAccessProjectAsync(projectId, ct)) return NotFound();

        var rows = await db.ContentEnvironmentOverrides
            .AsNoTracking()
            .Where(x => x.ProjectId == projectId)
            .GroupBy(x => x.Environment)
            .Select(g => new ProjectEnvironmentDto(g.Key, g.Count()))
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

        return Ok(rows);
    }

    [HttpGet("api/content-items/{contentItemId:guid}/environment-variants")]
    public async Task<IActionResult> List(Guid contentItemId, CancellationToken ct)
    {
        var item = await FindAccessibleItemAsync(contentItemId, ct);
        if (item is null) return NotFound();

        var rows = await db.ContentEnvironmentOverrides
            .AsNoTracking()
            .Where(x => x.ContentItemId == contentItemId)
            .OrderBy(x => x.Environment).ThenBy(x => x.LanguageCode)
            .Select(x => ToDto(x))
            .ToListAsync(ct);

        return Ok(rows);
    }

    [HttpPut("api/content-items/{contentItemId:guid}/environment-variants")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Upsert(Guid contentItemId, [FromBody] UpsertEnvironmentVariantRequest request, CancellationToken ct)
    {
        var item = await FindAccessibleItemAsync(contentItemId, ct);
        if (item is null) return NotFound();

        if (!CopyEnvironments.TryNormalizeForVariant(request.Environment, out var environment, out var envError))
            return ValidationError(nameof(request.Environment), envError!);

        if (string.IsNullOrWhiteSpace(request.Value))
            return ValidationError(nameof(request.Value), "Value is required. Delete the variant to fall back to production copy.");
        if (request.Value.Length > MaxValueLength)
            return ValidationError(nameof(request.Value), $"Value must be {MaxValueLength} characters or fewer.");

        var languageResult = await ResolveLanguageCodeAsync(item.ProjectId, request.LanguageCode, ct);
        if (languageResult.Error is not null)
            return ValidationError(nameof(request.LanguageCode), languageResult.Error);
        var languageCode = languageResult.Code;

        var existing = await db.ContentEnvironmentOverrides.FirstOrDefaultAsync(
            x => x.ContentItemId == contentItemId && x.Environment == environment && x.LanguageCode == languageCode, ct);

        var actor = CurrentEmail();
        if (existing is null)
        {
            existing = new ContentEnvironmentOverride
            {
                ProjectId = item.ProjectId,
                ContentItemId = contentItemId,
                Environment = environment,
                LanguageCode = languageCode,
                Value = request.Value,
                UpdatedByEmail = actor
            };
            db.ContentEnvironmentOverrides.Add(existing);
        }
        else
        {
            existing.Value = request.Value;
            existing.UpdatedByEmail = actor;
            existing.UpdatedUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return Ok(ToDto(existing));
    }

    [HttpDelete("api/content-items/{contentItemId:guid}/environment-variants/{variantId:guid}")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Delete(Guid contentItemId, Guid variantId, CancellationToken ct)
    {
        var item = await FindAccessibleItemAsync(contentItemId, ct);
        if (item is null) return NotFound();

        var variant = await db.ContentEnvironmentOverrides
            .FirstOrDefaultAsync(x => x.Id == variantId && x.ContentItemId == contentItemId, ct);
        if (variant is null) return NotFound();

        db.ContentEnvironmentOverrides.Remove(variant);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static EnvironmentVariantDto ToDto(ContentEnvironmentOverride x)
        => new(x.Id, x.ContentItemId, x.Environment, x.LanguageCode, x.Value, x.UpdatedByEmail, x.UpdatedUtc);

    private BadRequestObjectResult ValidationError(string field, string message)
        => BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { [field] = [message] })
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed"
        });

    /// <summary>
    /// Empty/null or the project's source language → source variant (""). Otherwise it must be an
    /// active target language on the project.
    /// </summary>
    private async Task<(string Code, string? Error)> ResolveLanguageCodeAsync(Guid projectId, string? requested, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(requested)) return (string.Empty, null);

        var languages = await db.ProjectLanguages
            .AsNoTracking()
            .Where(x => x.ProjectId == projectId && x.IsActive)
            .ToListAsync(ct);

        var match = languages.FirstOrDefault(x => string.Equals(x.Bcp47Code, requested.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match is null) return (string.Empty, $"Language '{requested}' is not active on this project.");

        return match.IsSource ? (string.Empty, null) : (match.Bcp47Code, null);
    }

    private async Task<ContentItem?> FindAccessibleItemAsync(Guid contentItemId, CancellationToken ct)
    {
        var item = await db.ContentItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == contentItemId, ct);
        if (item is null) return null;
        return await CanAccessProjectAsync(item.ProjectId, ct) ? item : null;
    }

    /// <summary>The caller must be an active member of the workspace that owns the project.</summary>
    private async Task<bool> CanAccessProjectAsync(Guid projectId, CancellationToken ct)
    {
        var email = CurrentEmail();
        if (string.IsNullOrWhiteSpace(email)) return false;

        var workspaceId = await db.Projects
            .AsNoTracking()
            .Where(x => x.Id == projectId)
            .Select(x => (Guid?)x.WorkspaceId)
            .FirstOrDefaultAsync(ct);
        if (workspaceId is null) return false;

        return await db.WorkspaceMemberships
            .AsNoTracking()
            .AnyAsync(m => m.WorkspaceId == workspaceId.Value && m.Email == email && m.IsActive, ct);
    }

    private string CurrentEmail()
        => (User.FindFirst("email")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty)
            .Trim()
            .ToLowerInvariant();
}
