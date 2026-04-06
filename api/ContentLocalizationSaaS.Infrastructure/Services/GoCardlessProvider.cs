using ContentLocalizationSaaS.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContentLocalizationSaaS.Infrastructure.Services;

public sealed class GoCardlessOptions
{
    public const string SectionName = "GoCardless";
    public string Environment { get; set; } = "sandbox"; // sandbox | live
    public string AccessToken { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api-sandbox.gocardless.com";
}

public sealed class GoCardlessProvider : IBillingProvider
{
    private readonly GoCardlessOptions _options;
    private readonly ILogger<GoCardlessProvider> _logger;
    private readonly HttpClient _httpClient;

    public GoCardlessProvider(
        IOptions<GoCardlessOptions> options,
        ILogger<GoCardlessProvider> logger,
        HttpClient httpClient)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.AccessToken}");
        _httpClient.DefaultRequestHeaders.Add("GoCardless-Version", "2015-07-06");
    }

    public Task<CreateCheckoutResult> CreateCheckoutAsync(Guid workspaceId, string redirectUrl, CancellationToken ct = default)
    {
        // TODO: Implement GoCardless Billing Request creation
        // POST /billing_requests with mandate_request + customer metadata
        // Then create a billing request flow with redirect_uri
        _logger.LogInformation("CreateCheckout called for workspace {WorkspaceId}", workspaceId);

        return Task.FromResult(new CreateCheckoutResult
        {
            CheckoutUrl = $"{_options.BaseUrl}/billing-request-flows/placeholder",
            CustomerId = string.Empty,
            BillingRequestId = string.Empty
        });
    }

    public Task<WebhookValidationResult> ValidateWebhookAsync(string body, string signatureHeader, CancellationToken ct = default)
    {
        // TODO: Implement HMAC-SHA256 webhook signature validation
        // Compare computed HMAC of body using WebhookSecret against signatureHeader
        _logger.LogInformation("ValidateWebhook called");

        return Task.FromResult(new WebhookValidationResult
        {
            IsValid = false,
            EventId = string.Empty,
            EventType = string.Empty,
            PayloadJson = body
        });
    }

    public Task CancelSubscriptionAsync(string subscriptionId, CancellationToken ct = default)
    {
        // TODO: Implement GoCardless subscription cancellation
        // PUT /subscriptions/{id}/actions/cancel
        _logger.LogInformation("CancelSubscription called for {SubscriptionId}", subscriptionId);
        return Task.CompletedTask;
    }
}
