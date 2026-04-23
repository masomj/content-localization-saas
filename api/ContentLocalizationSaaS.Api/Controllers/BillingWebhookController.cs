using ContentLocalizationSaaS.Application.Abstractions;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/billing")]
public class BillingWebhookController : ControllerBase
{
    private readonly IBillingProvider _billingProvider;
    private readonly IEntitlementService _entitlements;
    private readonly AppDbContext _db;

    public BillingWebhookController(
        IBillingProvider billingProvider,
        IEntitlementService entitlements,
        AppDbContext db)
    {
        _billingProvider = billingProvider;
        _entitlements = entitlements;
        _db = db;
    }

    [HttpPost("webhook/gocardless")]
    public async Task<IActionResult> HandleGoCardlessWebhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);
        var signature = Request.Headers["Webhook-Signature"].FirstOrDefault() ?? string.Empty;

        var validation = await _billingProvider.ValidateWebhookAsync(body, signature, ct);
        if (!validation.IsValid)
            return Unauthorized();

        // Find the subscription by parsing the event payload
        // For now, we need the subscription to be identifiable from the event
        // GoCardless events contain links to mandates/subscriptions/payments
        var billingEvent = new BillingEvent
        {
            ProviderEventId = validation.EventId,
            EventType = validation.EventType,
            PayloadJson = validation.PayloadJson,
            ReceivedUtc = DateTime.UtcNow
        };

        // Look up subscription by provider subscription ID from payload
        // TODO: Parse GoCardless event payload to extract subscription/mandate links
        var sub = await _db.WorkspaceSubscriptions
            .FirstOrDefaultAsync(s => s.ProviderSubscriptionId != string.Empty, ct);

        if (sub is not null)
        {
            billingEvent.WorkspaceSubscriptionId = sub.Id;
            await _entitlements.ProcessBillingEventAsync(billingEvent, ct);
        }

        return Ok();
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> CreateCheckout(
        [FromQuery] Guid workspaceId,
        [FromQuery] string redirectUrl,
        CancellationToken ct)
    {
        var result = await _billingProvider.CreateCheckoutAsync(workspaceId, redirectUrl, ct);
        return Ok(result);
    }
}
