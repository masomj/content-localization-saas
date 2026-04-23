using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/plans")]
public class PlanDefinitionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PlanDefinitionsController(AppDbContext db) => _db = db;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetPlans(CancellationToken ct)
    {
        var plans = await _db.PlanDefinitions
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Tier)
            .Select(p => new
            {
                p.Id,
                p.Name,
                Tier = p.Tier.ToString(),
                p.MaxUsers,
                p.MaxProjects,
                p.MaxFigmaBoards,
                p.MaxFramesAndComponents,
                p.PricePerSeatMonthly,
                p.IsDefault
            })
            .ToListAsync(ct);

        return Ok(plans);
    }
}
