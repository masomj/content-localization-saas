using System.Text.RegularExpressions;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record StyleCheckRequest(string Text, Guid ProjectId, string? ContentType);
public sealed record StyleViolation(Guid RuleId, string RuleName, string Message, string RuleType);
public sealed record CreateStyleOverrideRequest(Guid ContentItemLanguageTaskId, Guid StyleRuleId, string OverriddenByEmail);

[ApiController]
[Route("api/style-check")]
public sealed class StyleCheckController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Check([FromBody] StyleCheckRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return Ok(Array.Empty<StyleViolation>());

        var rules = await db.StyleRules
            .Where(r => r.ProjectId == request.ProjectId && r.IsActive)
            .ToListAsync(ct);

        var contentType = request.ContentType ?? string.Empty;
        var violations = new List<StyleViolation>();

        foreach (var rule in rules)
        {
            // Scope filter: empty scope = applies to all
            if (!string.IsNullOrEmpty(rule.Scope) &&
                !string.Equals(rule.Scope, contentType, StringComparison.OrdinalIgnoreCase))
                continue;

            var isViolation = rule.RuleType switch
            {
                "case_check" => HasTitleCaseWords(request.Text),
                "regex" => !string.IsNullOrEmpty(rule.Pattern) && Regex.IsMatch(request.Text, rule.Pattern, RegexOptions.None, TimeSpan.FromSeconds(1)),
                "no_trailing_punctuation" => request.Text.Length > 0 && (request.Text[^1] == '.' || request.Text[^1] == '!'),
                "max_words" => int.TryParse(rule.Pattern, out var max) && request.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > max,
                _ => false,
            };

            if (isViolation)
            {
                violations.Add(new StyleViolation(rule.Id, rule.Name, rule.Message, rule.RuleType));
            }
        }

        return Ok(violations);
    }

    [HttpPost("override")]
    public async Task<IActionResult> Override([FromBody] CreateStyleOverrideRequest request, CancellationToken ct)
    {
        var existing = await db.StyleOverrides
            .FirstOrDefaultAsync(o => o.ContentItemLanguageTaskId == request.ContentItemLanguageTaskId && o.StyleRuleId == request.StyleRuleId, ct);

        if (existing is not null)
            return Ok(existing);

        var entry = new StyleOverride
        {
            ContentItemLanguageTaskId = request.ContentItemLanguageTaskId,
            StyleRuleId = request.StyleRuleId,
            OverriddenByEmail = request.OverriddenByEmail,
        };

        db.StyleOverrides.Add(entry);
        await db.SaveChangesAsync(ct);
        return Ok(entry);
    }

    private static bool HasTitleCaseWords(string text)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= 1) return false;

        // Check if multiple words (not just first) start with uppercase
        var titleCaseCount = 0;
        for (var i = 1; i < words.Length; i++)
        {
            if (words[i].Length > 0 && char.IsUpper(words[i][0]) && !IsCommonAcronym(words[i]))
                titleCaseCount++;
        }

        return titleCaseCount >= 2; // At least 2 non-first words are title-cased
    }

    private static bool IsCommonAcronym(string word)
    {
        return word.Length <= 4 && word == word.ToUpperInvariant();
    }
}
