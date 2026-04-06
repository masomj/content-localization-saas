using ContentLocalizationSaaS.Application.Abstractions;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/workspaces/{workspaceId:guid}")]
[Authorize]
public class EntitlementsController : ControllerBase
{
    private readonly IEntitlementService _entitlements;
    private readonly AppDbContext _db;

    public EntitlementsController(IEntitlementService entitlements, AppDbContext db)
    {
        _entitlements = entitlements;
        _db = db;
    }

    [HttpGet("entitlements")]
    public async Task<IActionResult> GetEntitlements(Guid workspaceId, CancellationToken ct)
    {
        var snapshot = await _entitlements.GetEntitlementsAsync(workspaceId, ct);
        return Ok(snapshot);
    }

    [HttpGet("subscription")]
    public async Task<IActionResult> GetSubscription(Guid workspaceId, CancellationToken ct)
    {
        var sub = await _db.WorkspaceSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.WorkspaceId == workspaceId, ct);

        if (sub is null)
            return Ok(new { status = "free", plan = "Free" });

        var plan = await _db.PlanDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == sub.PlanDefinitionId, ct);

        return Ok(new
        {
            status = sub.Status.ToString(),
            plan = plan?.Name ?? "Unknown",
            tier = plan?.Tier.ToString() ?? "Free",
            seatCount = sub.SeatCount,
            provider = sub.Provider.ToString(),
            currentPeriodStart = sub.CurrentPeriodStartUtc,
            currentPeriodEnd = sub.CurrentPeriodEndUtc,
            graceExpires = sub.GraceExpiresUtc
        });
    }
}
