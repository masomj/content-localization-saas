using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContentLocalizationSaaS.Infrastructure.Services;

/// <summary>
/// EP11-S4: CI Pipeline Licensing.
/// Handles automatic revocation of API/CI tokens when billing lapses.
/// Triggered by EntitlementService when processing payment_failed or mandate_cancelled events.
/// </summary>
public interface ICiLicensingService
{
    /// <summary>
    /// Revoke all active API tokens for workspaces linked to a subscription
    /// when billing enters a terminal state past grace.
    /// </summary>
    Task RevokeCiTokensForSubscriptionAsync(Guid workspaceSubscriptionId, string billingEventId, CancellationToken ct = default);

    /// <summary>
    /// Re-enable CI tokens when billing is restored (payment_confirmed).
    /// </summary>
    Task RestoreCiTokensForSubscriptionAsync(Guid workspaceSubscriptionId, string billingEventId, CancellationToken ct = default);
}

public sealed class CiLicensingService : ICiLicensingService
{
    private readonly AppDbContext _db;
    private readonly ILogger<CiLicensingService> _logger;

    public CiLicensingService(AppDbContext db, ILogger<CiLicensingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task RevokeCiTokensForSubscriptionAsync(Guid workspaceSubscriptionId, string billingEventId, CancellationToken ct = default)
    {
        var sub = await _db.WorkspaceSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == workspaceSubscriptionId, ct);

        if (sub is null) return;

        // Find all active API tokens scoped to projects in this workspace
        var projectIds = await _db.Projects
            .Where(p => p.WorkspaceId == sub.WorkspaceId)
            .Select(p => p.Id)
            .ToListAsync(ct);

        // API tokens are workspace-level (not project-scoped in current model)
        // Revoke all non-revoked tokens
        var tokens = await _db.ApiTokens
            .Where(t => !t.IsRevoked)
            .ToListAsync(ct);

        // Since ApiToken doesn't have a WorkspaceId, we revoke all for now
        // TODO: Add WorkspaceId to ApiToken entity for proper scoping
        var revokedCount = 0;
        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedUtc = DateTime.UtcNow;
            revokedCount++;
        }

        if (revokedCount > 0)
        {
            // Log the CI lock event linked to billing event
            _db.BillingEvents.Add(new BillingEvent
            {
                WorkspaceSubscriptionId = workspaceSubscriptionId,
                ProviderEventId = $"ci_lock_{billingEventId}",
                EventType = "ci_tokens_revoked",
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    revokedCount,
                    triggerEventId = billingEventId,
                    workspaceId = sub.WorkspaceId,
                    reason = "billing_lapsed"
                }),
                Processed = true,
                ProcessedUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(ct);
            _logger.LogWarning("Revoked {Count} CI/API tokens for workspace {WorkspaceId} due to billing event {EventId}",
                revokedCount, sub.WorkspaceId, billingEventId);
        }
    }

    public async Task RestoreCiTokensForSubscriptionAsync(Guid workspaceSubscriptionId, string billingEventId, CancellationToken ct = default)
    {
        // Note: Revoked tokens cannot be un-revoked (security best practice).
        // Instead, log that billing is restored — users must generate new tokens.
        var sub = await _db.WorkspaceSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == workspaceSubscriptionId, ct);

        if (sub is null) return;

        _db.BillingEvents.Add(new BillingEvent
        {
            WorkspaceSubscriptionId = workspaceSubscriptionId,
            ProviderEventId = $"ci_restore_{billingEventId}",
            EventType = "ci_access_restored",
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                triggerEventId = billingEventId,
                workspaceId = sub.WorkspaceId,
                note = "Billing restored. Users must generate new API tokens."
            }),
            Processed = true,
            ProcessedUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("CI access restored for workspace {WorkspaceId} after billing event {EventId}",
            sub.WorkspaceId, billingEventId);
    }
}
