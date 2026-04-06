using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/workspaces/{workspaceId:guid}/billing")]
[Authorize]
public sealed class BillingAuditController : ControllerBase
{
    private readonly AppDbContext _db;

    public BillingAuditController(AppDbContext db) => _db = db;

    /// <summary>
    /// EP11-S7: Billing & Entitlement Audit Trail.
    /// Returns immutable billing events with GoCardless event/mandate/payment IDs.
    /// </summary>
    [HttpGet("audit")]
    public async Task<IActionResult> GetAuditTrail(
        Guid workspaceId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var sub = await _db.WorkspaceSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.WorkspaceId == workspaceId, ct);

        if (sub is null)
            return Ok(new { events = Array.Empty<object>(), total = 0 });

        var query = _db.BillingEvents
            .AsNoTracking()
            .Where(e => e.WorkspaceSubscriptionId == sub.Id)
            .OrderByDescending(e => e.ReceivedUtc);

        var total = await query.CountAsync(ct);

        var events = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new
            {
                e.Id,
                e.ProviderEventId,
                e.EventType,
                e.PayloadJson,
                e.Processed,
                e.ReceivedUtc,
                e.ProcessedUtc
            })
            .ToListAsync(ct);

        return Ok(new
        {
            events,
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize)
        });
    }

    /// <summary>
    /// EP11-S7: Reconciliation export — billing events with GoCardless transaction references.
    /// Returns CSV-friendly data with workspace/org IDs + GoCardless references.
    /// </summary>
    [HttpGet("audit/export")]
    public async Task<IActionResult> ExportAuditTrail(
        Guid workspaceId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct = default)
    {
        var sub = await _db.WorkspaceSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.WorkspaceId == workspaceId, ct);

        if (sub is null)
            return Ok(Array.Empty<object>());

        var query = _db.BillingEvents
            .AsNoTracking()
            .Where(e => e.WorkspaceSubscriptionId == sub.Id);

        if (from.HasValue)
            query = query.Where(e => e.ReceivedUtc >= from.Value);
        if (to.HasValue)
            query = query.Where(e => e.ReceivedUtc <= to.Value);

        var events = await query
            .OrderBy(e => e.ReceivedUtc)
            .Select(e => new
            {
                workspaceId,
                subscriptionId = sub.Id,
                providerSubscriptionId = sub.ProviderSubscriptionId,
                providerMandateId = sub.ProviderMandateId,
                providerCustomerId = sub.ProviderCustomerId,
                e.ProviderEventId,
                e.EventType,
                e.Processed,
                e.ReceivedUtc,
                e.ProcessedUtc
            })
            .ToListAsync(ct);

        return Ok(events);
    }
}
