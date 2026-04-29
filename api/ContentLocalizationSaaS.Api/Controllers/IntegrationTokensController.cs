using System.Security.Cryptography;
using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record CreateApiTokenRequest(string Name, string Scope, DateTime? ExpiresUtc, List<Guid>? ProjectIds);
public sealed record RevokeApiTokenRequest(Guid TokenId);
public sealed record RotateApiTokenRequest(Guid TokenId, DateTime? NewExpiresUtc);
public sealed record ExtendApiTokenRequest(Guid TokenId, DateTime NewExpiresUtc);
public sealed record ApiTokenProjectScopeDto(Guid ProjectId, string Name);

[ApiController]
[Route("api/integration/tokens")]
public sealed class IntegrationTokensController(AppDbContext db) : ControllerBase
{
    private static readonly HashSet<string> AllowedScopes = new(StringComparer.OrdinalIgnoreCase)
    {
        "exports:read"
    };

    [HttpGet]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var workspaceId = ResolveWorkspaceId();
        if (workspaceId == Guid.Empty)
            return BadRequest(new { error = "workspace_id_required", message = "X-Workspace-Id header is required." });

        var projectNames = await db.Projects
            .AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId)
            .Select(x => new { x.Id, x.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var scopes = await db.ApiTokenProjectScopes
            .AsNoTracking()
            .Where(x => db.ApiTokens.Where(t => t.WorkspaceId == workspaceId).Select(t => t.Id).Contains(x.ApiTokenId))
            .ToListAsync(cancellationToken);

        var scopeMap = scopes
            .GroupBy(x => x.ApiTokenId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => new ApiTokenProjectScopeDto(
                    x.ProjectId,
                    projectNames.TryGetValue(x.ProjectId, out var name) ? name : "Unknown project"
                )).OrderBy(x => x.Name).ToArray());

        var rows = await db.ApiTokens
            .AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId)
            .OrderByDescending(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);

        return Ok(rows.Select(x => new
        {
            x.Id,
            x.Name,
            x.Scope,
            x.IsRevoked,
            x.CreatedUtc,
            x.ExpiresUtc,
            x.LastUsedUtc,
            x.RevokedUtc,
            isProjectRestricted = scopeMap.ContainsKey(x.Id),
            projectScopes = scopeMap.TryGetValue(x.Id, out var projectScopes) ? projectScopes : Array.Empty<ApiTokenProjectScopeDto>()
        }));
    }

    [HttpPost]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateApiTokenRequest request, CancellationToken cancellationToken)
    {
        var workspaceId = ResolveWorkspaceId();
        if (workspaceId == Guid.Empty)
            return BadRequest(new { error = "workspace_id_required", message = "X-Workspace-Id header is required." });

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Scope))
            return BadRequest(new { error = "name_and_scope_required" });

        var scopes = NormalizeScopes(request.Scope);
        if (scopes.Count == 0)
            return BadRequest(new { error = "scope_required" });

        var unsupportedScopes = scopes.Where(x => !AllowedScopes.Contains(x)).OrderBy(x => x).ToArray();
        if (unsupportedScopes.Length > 0)
            return BadRequest(new { error = "unsupported_scope", unsupportedScopes, allowedScopes = AllowedScopes.OrderBy(x => x) });

        var expiryResult = ValidateExpiry(request.ExpiresUtc ?? DateTime.UtcNow.AddDays(90));
        if (expiryResult.Result is not null) return expiryResult.Result;

        var scopedProjects = await ResolveScopedProjectsAsync(workspaceId, request.ProjectIds, cancellationToken);
        if (scopedProjects.Result is not null) return scopedProjects.Result;

        var rawToken = GenerateToken();
        var hash = Hash(rawToken);
        var normalizedScope = string.Join(",", scopes);

        var row = new ApiToken
        {
            WorkspaceId = workspaceId,
            Name = request.Name.Trim(),
            Scope = normalizedScope,
            TokenHash = hash,
            IsRevoked = false,
            ExpiresUtc = expiryResult.ExpiresUtc!.Value
        };

        db.ApiTokens.Add(row);
        foreach (var projectId in scopedProjects.ProjectIds!)
        {
            db.ApiTokenProjectScopes.Add(new ApiTokenProjectScope
            {
                ApiTokenId = row.Id,
                ProjectId = projectId
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            token = rawToken,
            row.Id,
            row.Name,
            row.Scope,
            row.ExpiresUtc,
            workspaceId = row.WorkspaceId,
            projectIds = scopedProjects.ProjectIds
        });
    }

    [HttpPost("revoke")]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> Revoke([FromBody] RevokeApiTokenRequest request, CancellationToken cancellationToken)
    {
        var workspaceId = ResolveWorkspaceId();
        if (workspaceId == Guid.Empty)
            return BadRequest(new { error = "workspace_id_required", message = "X-Workspace-Id header is required." });

        var row = await db.ApiTokens.FirstOrDefaultAsync(x => x.Id == request.TokenId && x.WorkspaceId == workspaceId, cancellationToken);
        if (row is null) return NotFound();

        row.IsRevoked = true;
        row.RevokedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { status = "revoked" });
    }

    [HttpPost("rotate")]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> Rotate([FromBody] RotateApiTokenRequest request, CancellationToken cancellationToken)
    {
        var workspaceId = ResolveWorkspaceId();
        if (workspaceId == Guid.Empty)
            return BadRequest(new { error = "workspace_id_required", message = "X-Workspace-Id header is required." });

        var existing = await db.ApiTokens.FirstOrDefaultAsync(x => x.Id == request.TokenId && x.WorkspaceId == workspaceId, cancellationToken);
        if (existing is null) return NotFound();

        var expiry = request.NewExpiresUtc ?? existing.ExpiresUtc;
        var expiryResult = ValidateExpiry(expiry);
        if (expiryResult.Result is not null) return expiryResult.Result;

        existing.IsRevoked = true;
        existing.RevokedUtc = DateTime.UtcNow;

        var rawToken = GenerateToken();
        var replacement = new ApiToken
        {
            WorkspaceId = existing.WorkspaceId,
            Name = existing.Name,
            Scope = existing.Scope,
            TokenHash = Hash(rawToken),
            IsRevoked = false,
            ExpiresUtc = expiryResult.ExpiresUtc!.Value
        };

        var projectIds = await db.ApiTokenProjectScopes
            .AsNoTracking()
            .Where(x => x.ApiTokenId == existing.Id)
            .Select(x => x.ProjectId)
            .ToListAsync(cancellationToken);

        db.ApiTokens.Add(replacement);
        foreach (var projectId in projectIds)
        {
            db.ApiTokenProjectScopes.Add(new ApiTokenProjectScope
            {
                ApiTokenId = replacement.Id,
                ProjectId = projectId
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            status = "rotated",
            revokedTokenId = existing.Id,
            token = rawToken,
            replacement.Id,
            replacement.Name,
            replacement.Scope,
            replacement.ExpiresUtc,
            workspaceId = replacement.WorkspaceId,
            projectIds
        });
    }

    [HttpPost("extend")]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> Extend([FromBody] ExtendApiTokenRequest request, CancellationToken cancellationToken)
    {
        var workspaceId = ResolveWorkspaceId();
        if (workspaceId == Guid.Empty)
            return BadRequest(new { error = "workspace_id_required", message = "X-Workspace-Id header is required." });

        var row = await db.ApiTokens.FirstOrDefaultAsync(x => x.Id == request.TokenId && x.WorkspaceId == workspaceId, cancellationToken);
        if (row is null) return NotFound();
        if (row.IsRevoked) return BadRequest(new { error = "token_revoked" });

        var expiryResult = ValidateExpiry(request.NewExpiresUtc);
        if (expiryResult.Result is not null) return expiryResult.Result;
        if (expiryResult.ExpiresUtc <= row.ExpiresUtc) return BadRequest(new { error = "new_expiry_must_be_later" });

        row.ExpiresUtc = expiryResult.ExpiresUtc!.Value;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { status = "extended", row.Id, row.ExpiresUtc });
    }

    private Guid ResolveWorkspaceId()
    {
        var raw = Request.Headers["X-Workspace-Id"].ToString();
        return Guid.TryParse(raw, out var workspaceId) ? workspaceId : Guid.Empty;
    }

    private static List<string> NormalizeScopes(string rawScope)
    {
        return rawScope
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static (IActionResult? Result, DateTime? ExpiresUtc) ValidateExpiry(DateTime requestedExpiryUtc)
    {
        var now = DateTime.UtcNow;
        if (requestedExpiryUtc <= now)
            return (new BadRequestObjectResult(new { error = "expiry_must_be_in_future" }), null);

        var maxExpiry = now.AddYears(3);
        if (requestedExpiryUtc > maxExpiry)
            return (new BadRequestObjectResult(new { error = "expiry_exceeds_maximum", maximumExpiryUtc = maxExpiry }), null);

        return (null, requestedExpiryUtc);
    }

    private async Task<(IActionResult? Result, List<Guid>? ProjectIds)> ResolveScopedProjectsAsync(Guid workspaceId, List<Guid>? requestedProjectIds, CancellationToken cancellationToken)
    {
        var projectIds = (requestedProjectIds ?? new List<Guid>())
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        if (projectIds.Count == 0) return (null, projectIds);

        var validProjectIds = await db.Projects
            .AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId && projectIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var missingProjectIds = projectIds.Except(validProjectIds).OrderBy(x => x).ToArray();
        if (missingProjectIds.Length > 0)
            return (new BadRequestObjectResult(new { error = "invalid_project_scope", projectIds = missingProjectIds }), null);

        return (null, validProjectIds.OrderBy(x => x).ToList());
    }

    private static string GenerateToken()
    {
        Span<byte> buffer = stackalloc byte[32];
        RandomNumberGenerator.Fill(buffer);
        return $"icp_{Base64UrlEncode(buffer)}";
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static string Hash(string raw)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
