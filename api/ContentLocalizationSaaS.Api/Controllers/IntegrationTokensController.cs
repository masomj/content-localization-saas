using System.Security.Cryptography;
using System.Text;
using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record CreateApiTokenRequest(string Name, string Scope, DateTime? ExpiresUtc);
public sealed record RevokeApiTokenRequest(Guid TokenId);
public sealed record RotateApiTokenRequest(Guid TokenId, DateTime? NewExpiresUtc);
public sealed record ExtendApiTokenRequest(Guid TokenId, DateTime NewExpiresUtc);

[ApiController]
[Route("api/integration/tokens")]
public sealed class IntegrationTokensController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var rows = await db.ApiTokens.OrderByDescending(x => x.CreatedUtc).ToListAsync(cancellationToken);
        return Ok(rows.Select(x => new { x.Id, x.Name, x.Scope, x.IsRevoked, x.CreatedUtc, x.ExpiresUtc, x.LastUsedUtc, x.RevokedUtc }));
    }

    [HttpPost]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateApiTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Scope))
            return BadRequest(new { error = "name_and_scope_required" });

        var expiry = request.ExpiresUtc ?? DateTime.UtcNow.AddDays(90);
        if (expiry <= DateTime.UtcNow)
            return BadRequest(new { error = "expiry_must_be_in_future" });

        var rawToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", string.Empty).Replace("/", "_").Replace("+", "-");
        var hash = Hash(rawToken);

        var row = new ApiToken
        {
            Name = request.Name.Trim(),
            Scope = request.Scope.Trim(),
            TokenHash = hash,
            IsRevoked = false,
            ExpiresUtc = expiry
        };

        db.ApiTokens.Add(row);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new { token = rawToken, row.Id, row.Name, row.Scope, row.ExpiresUtc });
    }

    [HttpPost("revoke")]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> Revoke([FromBody] RevokeApiTokenRequest request, CancellationToken cancellationToken)
    {
        var row = await db.ApiTokens.FirstOrDefaultAsync(x => x.Id == request.TokenId, cancellationToken);
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
        var existing = await db.ApiTokens.FirstOrDefaultAsync(x => x.Id == request.TokenId, cancellationToken);
        if (existing is null) return NotFound();

        existing.IsRevoked = true;
        existing.RevokedUtc = DateTime.UtcNow;

        var expiry = request.NewExpiresUtc ?? existing.ExpiresUtc;
        if (expiry <= DateTime.UtcNow) return BadRequest(new { error = "expiry_must_be_in_future" });

        var rawToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", string.Empty).Replace("/", "_").Replace("+", "-");
        var replacement = new ApiToken
        {
            Name = existing.Name,
            Scope = existing.Scope,
            TokenHash = Hash(rawToken),
            IsRevoked = false,
            ExpiresUtc = expiry
        };

        db.ApiTokens.Add(replacement);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            status = "rotated",
            revokedTokenId = existing.Id,
            token = rawToken,
            replacement.Id,
            replacement.Name,
            replacement.Scope,
            replacement.ExpiresUtc
        });
    }

    [HttpPost("extend")]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> Extend([FromBody] ExtendApiTokenRequest request, CancellationToken cancellationToken)
    {
        var row = await db.ApiTokens.FirstOrDefaultAsync(x => x.Id == request.TokenId, cancellationToken);
        if (row is null) return NotFound();
        if (row.IsRevoked) return BadRequest(new { error = "token_revoked" });
        if (request.NewExpiresUtc <= DateTime.UtcNow) return BadRequest(new { error = "expiry_must_be_in_future" });
        if (request.NewExpiresUtc <= row.ExpiresUtc) return BadRequest(new { error = "new_expiry_must_be_later" });

        row.ExpiresUtc = request.NewExpiresUtc;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { status = "extended", row.Id, row.ExpiresUtc });
    }

    public static string Hash(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
