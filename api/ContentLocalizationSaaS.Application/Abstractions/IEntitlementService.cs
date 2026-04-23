using ContentLocalizationSaaS.Domain;

namespace ContentLocalizationSaaS.Application.Abstractions;

public sealed class EntitlementSnapshot
{
    public PlanTier CurrentTier { get; set; }
    public SubscriptionStatus BillingStatus { get; set; }
    public int UsedUsers { get; set; }
    public int MaxUsers { get; set; }
    public int UsedProjects { get; set; }
    public int MaxProjects { get; set; }
    public int UsedFigmaBoards { get; set; }
    public int MaxFigmaBoards { get; set; }
    public int UsedFramesAndComponents { get; set; }
    public int MaxFramesAndComponents { get; set; }
    public bool IsGracePeriod { get; set; }
    public DateTime? GraceExpiresUtc { get; set; }
}

public interface IEntitlementService
{
    Task<EntitlementSnapshot> GetEntitlementsAsync(Guid workspaceId, CancellationToken ct = default);
    Task<bool> CanAddUserAsync(Guid workspaceId, CancellationToken ct = default);
    Task<bool> CanCreateProjectAsync(Guid workspaceId, CancellationToken ct = default);
    Task<bool> CanAddFigmaBoardAsync(Guid workspaceId, CancellationToken ct = default);
    Task<bool> CanAddFrameOrComponentAsync(Guid workspaceId, CancellationToken ct = default);
    Task ProcessBillingEventAsync(BillingEvent billingEvent, CancellationToken ct = default);
}
