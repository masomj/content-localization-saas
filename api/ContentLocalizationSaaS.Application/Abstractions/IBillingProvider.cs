namespace ContentLocalizationSaaS.Application.Abstractions;

public sealed class CreateCheckoutResult
{
    public required string CheckoutUrl { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string BillingRequestId { get; set; } = string.Empty;
}

public sealed class WebhookValidationResult
{
    public bool IsValid { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
}

public interface IBillingProvider
{
    Task<CreateCheckoutResult> CreateCheckoutAsync(Guid workspaceId, string redirectUrl, CancellationToken ct = default);
    Task<WebhookValidationResult> ValidateWebhookAsync(string body, string signatureHeader, CancellationToken ct = default);
    Task CancelSubscriptionAsync(string subscriptionId, CancellationToken ct = default);
}
