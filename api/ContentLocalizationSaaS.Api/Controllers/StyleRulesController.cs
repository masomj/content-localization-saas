using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Application;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record CreateStyleRuleRequest(string Name, string RuleType, string? Pattern, string? Scope, string? Message, bool IsActive = true);
public sealed record UpdateStyleRuleRequest(string Name, string RuleType, string? Pattern, string? Scope, string? Message, bool IsActive = true);

[ApiController]
[Route("api/projects/{projectId:guid}/style-rules")]
public sealed class StyleRulesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid projectId, CancellationToken ct)
    {
        var rules = await db.StyleRules
            .Where(r => r.ProjectId == projectId)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

        return Ok(rules);
    }

    [HttpPost]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> Create(Guid projectId, [FromBody] CreateStyleRuleRequest request, CancellationToken ct)
    {
        var rule = new StyleRule
        {
            ProjectId = projectId,
            Name = request.Name,
            RuleType = request.RuleType,
            Pattern = request.Pattern ?? string.Empty,
            Scope = request.Scope ?? string.Empty,
            Message = request.Message ?? string.Empty,
            IsActive = request.IsActive,
        };

        db.StyleRules.Add(rule);
        await db.SaveChangesAsync(ct);
        return Ok(rule);
    }

    [HttpPut("{ruleId:guid}")]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> Update(Guid projectId, Guid ruleId, [FromBody] UpdateStyleRuleRequest request, CancellationToken ct)
    {
        var rule = await db.StyleRules.FirstOrDefaultAsync(r => r.Id == ruleId && r.ProjectId == projectId, ct);
        if (rule is null) return NotFound();

        rule.Name = request.Name;
        rule.RuleType = request.RuleType;
        rule.Pattern = request.Pattern ?? string.Empty;
        rule.Scope = request.Scope ?? string.Empty;
        rule.Message = request.Message ?? string.Empty;
        rule.IsActive = request.IsActive;

        await db.SaveChangesAsync(ct);
        return Ok(rule);
    }

    [HttpDelete("{ruleId:guid}")]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> Delete(Guid projectId, Guid ruleId, CancellationToken ct)
    {
        var rule = await db.StyleRules.FirstOrDefaultAsync(r => r.Id == ruleId && r.ProjectId == projectId, ct);
        if (rule is null) return NotFound();

        db.StyleRules.Remove(rule);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
