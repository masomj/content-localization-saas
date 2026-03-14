using System.Text.RegularExpressions;
using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record SetNotificationPreferenceRequest(string UserEmail, bool InAppEnabled, bool EmailEnabled, bool SlackEnabled);
public sealed record MarkNotificationReadRequest(Guid NotificationId, bool IsRead);

[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string userEmail, [FromQuery] bool unreadOnly = false, CancellationToken cancellationToken = default)
    {
        var normalized = userEmail.Trim().ToLowerInvariant();
        var query = db.UserNotifications.Where(x => x.UserEmail == normalized);
        if (unreadOnly) query = query.Where(x => !x.IsRead);

        var rows = await query.OrderByDescending(x => x.CreatedUtc).ToListAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpPost("preferences")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> SetPreferences([FromBody] SetNotificationPreferenceRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserEmail)) return BadRequest(new { error = "userEmail_required" });

        var normalized = request.UserEmail.Trim().ToLowerInvariant();
        var pref = await db.NotificationPreferences.FirstOrDefaultAsync(x => x.UserEmail == normalized, cancellationToken);
        if (pref is null)
        {
            pref = new NotificationPreference { UserEmail = normalized };
            db.NotificationPreferences.Add(pref);
        }

        pref.InAppEnabled = request.InAppEnabled;
        pref.EmailEnabled = request.EmailEnabled;
        pref.SlackEnabled = request.SlackEnabled;
        pref.UpdatedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return Ok(pref);
    }

    [HttpPost("mark")]
    public async Task<IActionResult> MarkRead([FromBody] MarkNotificationReadRequest request, CancellationToken cancellationToken)
    {
        var row = await db.UserNotifications.FirstOrDefaultAsync(x => x.Id == request.NotificationId, cancellationToken);
        if (row is null) return NotFound();

        row.IsRead = request.IsRead;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(row);
    }

    public static IEnumerable<string> ExtractMentions(string body)
    {
        var regex = new Regex("@([A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,})", RegexOptions.Compiled);
        return regex.Matches(body).Select(m => m.Groups[1].Value.Trim().ToLowerInvariant()).Distinct();
    }

    public static async Task CreateMentionNotificationsAsync(AppDbContext db, string body, string authorEmail, CancellationToken cancellationToken)
    {
        var mentioned = ExtractMentions(body).Where(x => !string.Equals(x, authorEmail, StringComparison.OrdinalIgnoreCase)).ToList();
        if (mentioned.Count == 0) return;

        foreach (var email in mentioned)
        {
            var pref = await db.NotificationPreferences.FirstOrDefaultAsync(x => x.UserEmail == email, cancellationToken);
            var channel = pref is null || pref.InAppEnabled
                ? "in_app"
                : pref.EmailEnabled
                    ? "email"
                    : pref.SlackEnabled
                        ? "slack"
                        : "none";

            if (channel == "none") continue;

            db.UserNotifications.Add(new UserNotification
            {
                UserEmail = email,
                Type = "mention",
                Message = $"You were mentioned by {authorEmail}",
                ChannelUsed = channel,
                IsRead = false
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
