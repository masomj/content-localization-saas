using ContentLocalizationSaaS.Application.Abstractions;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/workspaces/{workspaceId:guid}/admin")]
[Authorize]
public sealed class AdminUsageReportingController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IEntitlementService _entitlements;

    public AdminUsageReportingController(AppDbContext db, IEntitlementService entitlements)
    {
        _db = db;
        _entitlements = entitlements;
    }

    /// <summary>
    /// EP11-S10: Admin usage dashboard — entitlements, usage, billing health.
    /// </summary>
    [HttpGet("usage")]
    public async Task<IActionResult> GetUsageReport(Guid workspaceId, CancellationToken ct)
    {
        var snapshot = await _entitlements.GetEntitlementsAsync(workspaceId, ct);

        var sub = await _db.WorkspaceSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.WorkspaceId == workspaceId, ct);

        var plan = sub is not null
            ? await _db.PlanDefinitions.AsNoTracking().FirstOrDefaultAsync(p => p.Id == sub.PlanDefinitionId, ct)
            : await _db.PlanDefinitions.AsNoTracking().FirstOrDefaultAsync(p => p.IsDefault, ct);

        // Billing health status
        string billingHealth;
        string? billingAction = null;
        if (sub is null || sub.Status == SubscriptionStatus.None)
        {
            billingHealth = "free";
        }
        else
        {
            billingHealth = sub.Status switch
            {
                SubscriptionStatus.Active => "healthy",
                SubscriptionStatus.Pending => "pending_setup",
                SubscriptionStatus.PastDue => snapshot.IsGracePeriod ? "grace_period" : "lapsed",
                SubscriptionStatus.Cancelled => "cancelled",
                SubscriptionStatus.Expired => "expired",
                _ => "unknown"
            };

            billingAction = sub.Status switch
            {
                SubscriptionStatus.PastDue => "Update your payment method to avoid service interruption.",
                SubscriptionStatus.Cancelled => "Reactivate your subscription to restore full access.",
                SubscriptionStatus.Expired => "Your subscription has expired. Upgrade to continue using Pro features.",
                _ => null
            };
        }

        // Recent billing events for alerting
        var recentEvents = sub is not null
            ? await _db.BillingEvents
                .AsNoTracking()
                .Where(e => e.WorkspaceSubscriptionId == sub.Id)
                .OrderByDescending(e => e.ReceivedUtc)
                .Take(5)
                .Select(e => new
                {
                    e.EventType,
                    e.ProviderEventId,
                    e.ReceivedUtc,
                    e.Processed
                })
                .ToListAsync(ct)
            : [];

        return Ok(new
        {
            workspace = new { id = workspaceId },
            plan = new
            {
                name = plan?.Name ?? "Free",
                tier = plan?.Tier.ToString() ?? "Free",
                pricePerSeat = plan?.PricePerSeatMonthly ?? 0m
            },
            usage = new
            {
                users = new { used = snapshot.UsedUsers, max = snapshot.MaxUsers, unlimited = snapshot.MaxUsers == 0 },
                projects = new { used = snapshot.UsedProjects, max = snapshot.MaxProjects, unlimited = snapshot.MaxProjects == 0 },
                figmaBoards = new { used = snapshot.UsedFigmaBoards, max = snapshot.MaxFigmaBoards, unlimited = snapshot.MaxFigmaBoards == 0 },
                framesAndComponents = new { used = snapshot.UsedFramesAndComponents, max = snapshot.MaxFramesAndComponents, unlimited = snapshot.MaxFramesAndComponents == 0 }
            },
            billing = new
            {
                health = billingHealth,
                status = sub?.Status.ToString() ?? "None",
                provider = sub?.Provider.ToString() ?? "None",
                seatCount = sub?.SeatCount ?? 1,
                currentPeriodStart = sub?.CurrentPeriodStartUtc,
                currentPeriodEnd = sub?.CurrentPeriodEndUtc,
                graceExpires = sub?.GraceExpiresUtc,
                isGracePeriod = snapshot.IsGracePeriod,
                actionRequired = billingAction
            },
            recentBillingEvents = recentEvents
        });
    }

    /// <summary>
    /// EP11-S10: Monthly reconciliation report with GoCardless IDs.
    /// </summary>
    [HttpGet("usage/monthly")]
    public async Task<IActionResult> GetMonthlyReport(
        Guid workspaceId,
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken ct)
    {
        var reportYear = year ?? DateTime.UtcNow.Year;
        var reportMonth = month ?? DateTime.UtcNow.Month;
        var startUtc = new DateTime(reportYear, reportMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var endUtc = startUtc.AddMonths(1);

        var sub = await _db.WorkspaceSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.WorkspaceId == workspaceId, ct);

        if (sub is null)
            return Ok(new
            {
                period = new { year = reportYear, month = reportMonth },
                events = Array.Empty<object>(),
                summary = new { totalEvents = 0, paymentsConfirmed = 0, paymentsFailed = 0 }
            });

        var events = await _db.BillingEvents
            .AsNoTracking()
            .Where(e => e.WorkspaceSubscriptionId == sub.Id
                && e.ReceivedUtc >= startUtc
                && e.ReceivedUtc < endUtc)
            .OrderBy(e => e.ReceivedUtc)
            .Select(e => new
            {
                e.ProviderEventId,
                e.EventType,
                e.ReceivedUtc,
                e.ProcessedUtc,
                e.Processed,
                providerMandateId = sub.ProviderMandateId,
                providerSubscriptionId = sub.ProviderSubscriptionId,
                providerCustomerId = sub.ProviderCustomerId
            })
            .ToListAsync(ct);

        return Ok(new
        {
            period = new { year = reportYear, month = reportMonth },
            workspace = new { id = workspaceId },
            subscription = new
            {
                sub.Id,
                sub.ProviderMandateId,
                sub.ProviderSubscriptionId,
                sub.ProviderCustomerId
            },
            events,
            summary = new
            {
                totalEvents = events.Count,
                paymentsConfirmed = events.Count(e => e.EventType == "payment_confirmed"),
                paymentsFailed = events.Count(e => e.EventType == "payment_failed"),
                mandateEvents = events.Count(e => e.EventType.StartsWith("mandate_")),
                ciEvents = events.Count(e => e.EventType.StartsWith("ci_"))
            }
        });
    }
}
