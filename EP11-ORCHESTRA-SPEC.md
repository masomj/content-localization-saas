# EP11: Pricing, Packaging & Entitlements — ORCHESTRA BA Spec

## Payment Provider: GoCardless
- Payment link: https://pay.gocardless.com/BRT0004YGMH47WN
- Direct debit mandate lifecycle
- Webhook-driven entitlement updates
- Idempotent event handling mandatory

## Tech Stack
- Backend: .NET 10 API (Aspire), EF Core, PostgreSQL
- Frontend: Nuxt 4 + Vue 3
- Auth: Keycloak OIDC (realm: `intercopy`)
- All UI must support dark mode via CSS variables
- No browser native alerts — styled modals only
- Helper text under labels, not placeholders

## Pricing Model
- **Free**: 1 user, 1 project, 1 Figma board, 75 frames+components cap
- **Pro**: Per-seat, GoCardless direct debit billing

## Implementation Batch 1: Foundation (S1 + S8)

### S1: Plan Definitions (Free + Pro)

**New Domain Entities** (add to `Entities.cs`):

```csharp
public enum PlanTier { Free = 0, Pro = 1 }
public enum BillingProvider { None = 0, GoCardless = 1 }
public enum SubscriptionStatus { None = 0, Pending = 1, Active = 2, PastDue = 3, Cancelled = 4, Expired = 5 }

public sealed class PlanDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }           // "Free", "Pro"
    public PlanTier Tier { get; set; }
    public int MaxUsers { get; set; }                    // 1 for Free, 0 = unlimited for Pro
    public int MaxProjects { get; set; }                 // 1 for Free, 0 = unlimited
    public int MaxFigmaBoards { get; set; }              // 1 for Free, 0 = unlimited
    public int MaxFramesAndComponents { get; set; }      // 75 for Free, 0 = unlimited
    public decimal PricePerSeatMonthly { get; set; }     // 0 for Free
    public bool IsDefault { get; set; }                  // true for Free
    public bool IsActive { get; set; } = true;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class WorkspaceSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; set; }
    public Guid PlanDefinitionId { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.None;
    public BillingProvider Provider { get; set; } = BillingProvider.None;
    public string ProviderCustomerId { get; set; } = string.Empty;    // GoCardless customer ID
    public string ProviderMandateId { get; set; } = string.Empty;     // GoCardless mandate ID
    public string ProviderSubscriptionId { get; set; } = string.Empty; // GoCardless subscription ID
    public int SeatCount { get; set; } = 1;
    public DateTime? CurrentPeriodStartUtc { get; set; }
    public DateTime? CurrentPeriodEndUtc { get; set; }
    public DateTime? GraceExpiresUtc { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class BillingEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceSubscriptionId { get; set; }
    public string ProviderEventId { get; set; } = string.Empty;       // GoCardless event ID (idempotency key)
    public string EventType { get; set; } = string.Empty;             // mandate_created, payment_confirmed, payment_failed, etc.
    public string PayloadJson { get; set; } = string.Empty;
    public bool Processed { get; set; }
    public DateTime ReceivedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedUtc { get; set; }
}
```

**Backend Tasks:**
1. Add entities to `Entities.cs`
2. Add DbSets to `AppDbContext`
3. Create EF migration
4. Create `IBillingProvider` abstraction interface in Application layer
5. Create `GoCardlessProvider` implementation in Infrastructure
6. Create `GoCardlessOptions` config class (sandbox/prod URLs, access token)
7. Add GoCardless config to `appsettings.json` / `appsettings.Development.json`
8. Seed Free + Pro plan definitions in migration or startup
9. Create `PlanDefinitionsController` — GET /api/plans (public, list active plans)
10. Create `SubscriptionsController` — GET /api/workspaces/{id}/subscription

**Acceptance Criteria:**
- PlanDefinition seeded with Free (limits: 1/1/1/75) and Pro (unlimited, price TBD)
- GoCardless config abstracted behind `IBillingProvider`
- WorkspaceSubscription defaults to Free plan on workspace creation
- All new entities have proper indexes (WorkspaceId, ProviderEventId unique)
- Unit tests for plan definition validation

### S8: Central Entitlements API

**New Service:**

```csharp
public interface IEntitlementService
{
    Task<EntitlementSnapshot> GetEntitlementsAsync(Guid workspaceId);
    Task<bool> CanAddUserAsync(Guid workspaceId);
    Task<bool> CanCreateProjectAsync(Guid workspaceId);
    Task<bool> CanAddFigmaBoardAsync(Guid workspaceId);
    Task<bool> CanAddFrameOrComponentAsync(Guid workspaceId);
    Task ProcessBillingEventAsync(BillingEvent billingEvent);
}

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
```

**Backend Tasks:**
1. Create `IEntitlementService` interface in Application/Abstractions
2. Create `EntitlementService` implementation in Infrastructure/Services
3. Wire up DI registration
4. Create `EntitlementsController` — GET /api/workspaces/{id}/entitlements
5. Entitlement checks query current subscription + plan limits + actual usage counts
6. `ProcessBillingEventAsync` — idempotent handler (checks ProviderEventId, updates subscription status atomically)
7. Provider outage fallback: if GoCardless unreachable, retain last known paid state for configurable TTL (default 72h)
8. Unit tests for entitlement checks (free limits, pro unlimited, grace period)

**Acceptance Criteria:**
- Entitlements service consumes billing status from provider adapter only (no UI trust)
- Webhook processor updates entitlement state atomically with idempotent event handling
- Duplicate events (same ProviderEventId) are silently skipped
- Provider outage fallback retains last known state with warning
- GET /api/workspaces/{id}/entitlements returns full snapshot

**No frontend work in Batch 1** — this is pure backend foundation.

## DoD (all batches)
- All code compiles (`dotnet build`)
- Unit tests pass
- Dark mode compatible (when UI is involved)
- No browser native alerts
- Idempotent where applicable
- EF migrations created
- Committed to feature branch, not main
