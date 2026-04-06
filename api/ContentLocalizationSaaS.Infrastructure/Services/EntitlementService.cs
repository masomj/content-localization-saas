using ContentLocalizationSaaS.Application.Abstractions;
using ContentLocalizationSaaS.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContentLocalizationSaaS.Infrastructure.Services;

public sealed class EntitlementService : IEntitlementService
{
    private readonly AppDbContext _db;
    private readonly ILogger<EntitlementService> _logger;

    // Grace period TTL for provider outage fallback
    private static readonly TimeSpan GraceFallbackTtl = TimeSpan.FromHours(72);

    public EntitlementService(AppDbContext db, ILogger<EntitlementService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<EntitlementSnapshot> GetEntitlementsAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var sub = await _db.WorkspaceSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.WorkspaceId == workspaceId, ct);

        PlanDefinition plan;
        if (sub is null)
        {
            plan = await GetDefaultPlanAsync(ct);
            return BuildSnapshot(plan, SubscriptionStatus.None, workspaceId, 0, 0, 0, 0, false, null);
        }

        plan = await _db.PlanDefinitions
            .AsNoTracking()
            .FirstAsync(p => p.Id == sub.PlanDefinitionId, ct);

        var isGrace = sub.Status == SubscriptionStatus.PastDue
            && sub.GraceExpiresUtc.HasValue
            && sub.GraceExpiresUtc.Value > DateTime.UtcNow;

        var counts = await GetUsageCountsAsync(workspaceId, ct);

        return BuildSnapshot(plan, sub.Status, workspaceId,
            counts.users, counts.projects, counts.figmaBoards, counts.framesAndComponents,
            isGrace, sub.GraceExpiresUtc);
    }

    public async Task<bool> CanAddUserAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var snap = await GetEntitlementsAsync(workspaceId, ct);
        return snap.MaxUsers == 0 || snap.UsedUsers < snap.MaxUsers;
    }

    public async Task<bool> CanCreateProjectAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var snap = await GetEntitlementsAsync(workspaceId, ct);
        return snap.MaxProjects == 0 || snap.UsedProjects < snap.MaxProjects;
    }

    public async Task<bool> CanAddFigmaBoardAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var snap = await GetEntitlementsAsync(workspaceId, ct);
        return snap.MaxFigmaBoards == 0 || snap.UsedFigmaBoards < snap.MaxFigmaBoards;
    }

    public async Task<bool> CanAddFrameOrComponentAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var snap = await GetEntitlementsAsync(workspaceId, ct);
        return snap.MaxFramesAndComponents == 0 || snap.UsedFramesAndComponents < snap.MaxFramesAndComponents;
    }

    public async Task ProcessBillingEventAsync(BillingEvent billingEvent, CancellationToken ct = default)
    {
        // Idempotency: check if already processed
        var existing = await _db.BillingEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ProviderEventId == billingEvent.ProviderEventId, ct);

        if (existing is not null)
        {
            _logger.LogInformation("Duplicate billing event {EventId} skipped", billingEvent.ProviderEventId);
            return;
        }

        _db.BillingEvents.Add(billingEvent);

        var sub = await _db.WorkspaceSubscriptions
            .FirstOrDefaultAsync(s => s.Id == billingEvent.WorkspaceSubscriptionId, ct);

        if (sub is null)
        {
            _logger.LogWarning("Billing event {EventId} references unknown subscription {SubId}",
                billingEvent.ProviderEventId, billingEvent.WorkspaceSubscriptionId);
            billingEvent.Processed = true;
            billingEvent.ProcessedUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return;
        }

        switch (billingEvent.EventType)
        {
            case "mandate_created":
                sub.Status = SubscriptionStatus.Pending;
                sub.UpdatedUtc = DateTime.UtcNow;
                break;

            case "payment_confirmed":
                sub.Status = SubscriptionStatus.Active;
                sub.GraceExpiresUtc = null;
                sub.UpdatedUtc = DateTime.UtcNow;
                break;

            case "payment_failed":
                sub.Status = SubscriptionStatus.PastDue;
                sub.GraceExpiresUtc ??= DateTime.UtcNow.AddDays(14); // 14-day grace
                sub.UpdatedUtc = DateTime.UtcNow;
                break;

            case "mandate_cancelled":
            case "subscription_cancelled":
                sub.Status = SubscriptionStatus.Cancelled;
                sub.UpdatedUtc = DateTime.UtcNow;
                break;

            default:
                _logger.LogInformation("Unhandled billing event type: {EventType}", billingEvent.EventType);
                break;
        }

        billingEvent.Processed = true;
        billingEvent.ProcessedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private async Task<PlanDefinition> GetDefaultPlanAsync(CancellationToken ct)
    {
        return await _db.PlanDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IsDefault, ct)
            ?? new PlanDefinition
            {
                Name = "Free",
                Tier = PlanTier.Free,
                MaxUsers = 1,
                MaxProjects = 1,
                MaxFigmaBoards = 1,
                MaxFramesAndComponents = 75,
                IsDefault = true
            };
    }

    private async Task<(int users, int projects, int figmaBoards, int framesAndComponents)> GetUsageCountsAsync(
        Guid workspaceId, CancellationToken ct)
    {
        var users = await _db.WorkspaceMemberships
            .CountAsync(m => m.WorkspaceId == workspaceId && m.IsActive, ct);

        var projects = await _db.Projects
            .CountAsync(p => p.WorkspaceId == workspaceId, ct);

        // Figma boards = distinct DesignComponents per workspace (via project)
        var projectIds = await _db.Projects
            .Where(p => p.WorkspaceId == workspaceId)
            .Select(p => p.Id)
            .ToListAsync(ct);

        var figmaBoards = await _db.DesignComponents
            .CountAsync(dc => projectIds.Contains(dc.ProjectId), ct);

        // Frames + components = DesignComponents + LibraryComponents
        var libraryComponents = await _db.LibraryComponents
            .CountAsync(lc => projectIds.Contains(lc.ProjectId), ct);

        return (users, projects, figmaBoards, figmaBoards + libraryComponents);
    }

    private static EntitlementSnapshot BuildSnapshot(
        PlanDefinition plan, SubscriptionStatus status, Guid workspaceId,
        int usedUsers, int usedProjects, int usedFigmaBoards, int usedFramesAndComponents,
        bool isGrace, DateTime? graceExpires)
    {
        return new EntitlementSnapshot
        {
            CurrentTier = plan.Tier,
            BillingStatus = status,
            UsedUsers = usedUsers,
            MaxUsers = plan.MaxUsers,
            UsedProjects = usedProjects,
            MaxProjects = plan.MaxProjects,
            UsedFigmaBoards = usedFigmaBoards,
            MaxFigmaBoards = plan.MaxFigmaBoards,
            UsedFramesAndComponents = usedFramesAndComponents,
            MaxFramesAndComponents = plan.MaxFramesAndComponents,
            IsGracePeriod = isGrace,
            GraceExpiresUtc = graceExpires
        };
    }
}
