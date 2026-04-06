using ContentLocalizationSaaS.Application.Abstractions;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record UpdateSeatCountRequest(int SeatCount);
public sealed record InitiateUpgradeRequest(string RedirectUrl);

[ApiController]
[Route("api/workspaces/{workspaceId:guid}/billing")]
[Authorize]
public sealed class SeatManagementController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IBillingProvider _billingProvider;
    private readonly IEntitlementService _entitlements;

    public SeatManagementController(AppDbContext db, IBillingProvider billingProvider, IEntitlementService entitlements)
    {
        _db = db;
        _billingProvider = billingProvider;
        _entitlements = entitlements;
    }

    /// <summary>
    /// Get current seat count and billing status for a workspace.
    /// </summary>
    [HttpGet("seats")]
    public async Task<IActionResult> GetSeats(Guid workspaceId, CancellationToken ct)
    {
        var sub = await _db.WorkspaceSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.WorkspaceId == workspaceId, ct);

        if (sub is null)
            return Ok(new { seatCount = 1, status = "free", canModify = false });

        var plan = await _db.PlanDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == sub.PlanDefinitionId, ct);

        return Ok(new
        {
            seatCount = sub.SeatCount,
            status = sub.Status.ToString(),
            plan = plan?.Name ?? "Unknown",
            tier = plan?.Tier.ToString() ?? "Free",
            canModify = sub.Status == SubscriptionStatus.Active,
            provider = sub.Provider.ToString(),
            currentPeriodEnd = sub.CurrentPeriodEndUtc
        });
    }

    /// <summary>
    /// Update seat count for a Pro workspace. Requires active subscription.
    /// </summary>
    [HttpPut("seats")]
    public async Task<IActionResult> UpdateSeats(Guid workspaceId, [FromBody] UpdateSeatCountRequest request, CancellationToken ct)
    {
        if (request.SeatCount < 1)
            return BadRequest(new { error = "Seat count must be at least 1" });

        var sub = await _db.WorkspaceSubscriptions
            .FirstOrDefaultAsync(s => s.WorkspaceId == workspaceId, ct);

        if (sub is null)
            return BadRequest(new { error = "no_subscription", message = "No active subscription found. Upgrade to Pro first." });

        if (sub.Status != SubscriptionStatus.Active)
            return BadRequest(new { error = "subscription_not_active", message = $"Subscription is {sub.Status}. Cannot modify seats." });

        // Check current active member count — can't reduce below it
        var activeMembers = await _db.WorkspaceMemberships
            .CountAsync(m => m.WorkspaceId == workspaceId && m.IsActive, ct);

        if (request.SeatCount < activeMembers)
            return BadRequest(new
            {
                error = "seat_count_below_usage",
                message = $"Cannot reduce to {request.SeatCount} seats — {activeMembers} members are currently active. Remove members first.",
                activeMembers,
                requestedSeats = request.SeatCount
            });

        var oldSeatCount = sub.SeatCount;
        sub.SeatCount = request.SeatCount;
        sub.UpdatedUtc = DateTime.UtcNow;

        // Log the seat change as a billing event
        _db.BillingEvents.Add(new BillingEvent
        {
            WorkspaceSubscriptionId = sub.Id,
            ProviderEventId = $"seat_change_{Guid.NewGuid():N}",
            EventType = "seat_count_updated",
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { oldSeatCount, newSeatCount = request.SeatCount }),
            Processed = true,
            ProcessedUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            seatCount = sub.SeatCount,
            previousSeatCount = oldSeatCount,
            status = sub.Status.ToString()
        });
    }

    /// <summary>
    /// Initiate upgrade from Free to Pro via GoCardless checkout.
    /// </summary>
    [HttpPost("upgrade")]
    public async Task<IActionResult> InitiateUpgrade(Guid workspaceId, [FromBody] InitiateUpgradeRequest request, CancellationToken ct)
    {
        var sub = await _db.WorkspaceSubscriptions
            .FirstOrDefaultAsync(s => s.WorkspaceId == workspaceId, ct);

        // If already on an active Pro subscription, no need to upgrade
        if (sub is not null && sub.Status == SubscriptionStatus.Active)
        {
            var existingPlan = await _db.PlanDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == sub.PlanDefinitionId, ct);

            if (existingPlan?.Tier == PlanTier.Pro)
                return BadRequest(new { error = "already_pro", message = "Workspace is already on the Pro plan." });
        }

        try
        {
            var result = await _billingProvider.CreateCheckoutAsync(workspaceId, request.RedirectUrl, ct);

            // Create or update subscription record as pending
            var proPlan = await _db.PlanDefinitions
                .FirstOrDefaultAsync(p => p.Tier == PlanTier.Pro && p.IsActive, ct);

            if (proPlan is null)
                return StatusCode(500, new { error = "no_pro_plan", message = "Pro plan is not configured." });

            if (sub is null)
            {
                sub = new WorkspaceSubscription
                {
                    WorkspaceId = workspaceId,
                    PlanDefinitionId = proPlan.Id,
                    Status = SubscriptionStatus.Pending,
                    Provider = BillingProvider.GoCardless,
                    SeatCount = 1
                };
                _db.WorkspaceSubscriptions.Add(sub);
            }
            else
            {
                sub.PlanDefinitionId = proPlan.Id;
                sub.Status = SubscriptionStatus.Pending;
                sub.Provider = BillingProvider.GoCardless;
                sub.UpdatedUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);

            return Ok(new
            {
                checkoutUrl = result.CheckoutUrl,
                status = "pending",
                message = "Complete the GoCardless setup to activate your Pro subscription."
            });
        }
        catch (Exception)
        {
            return StatusCode(502, new
            {
                error = "checkout_failed",
                message = "Failed to initiate GoCardless checkout. Please try again.",
                retryable = true,
                eventId = Guid.NewGuid().ToString("N")
            });
        }
    }
}
