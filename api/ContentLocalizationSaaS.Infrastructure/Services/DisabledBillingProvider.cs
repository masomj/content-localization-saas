using ContentLocalizationSaaS.Application.Abstractions;
using ContentLocalizationSaaS.Application.Exceptions;
using Microsoft.Extensions.Logging;

namespace ContentLocalizationSaaS.Infrastructure.Services;

public sealed class BillingOptions
{
    public const string SectionName = "Billing";

    /// <summary>
    /// When false, no checkout can be started and webhooks are rejected.
    /// Defaults to true so production behaviour is unchanged unless explicitly switched off.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Billing provider used when <see cref="BillingOptions.Enabled"/> is false.
/// Stops anyone starting a paid subscription in non-production environments.
/// </summary>
public sealed class DisabledBillingProvider(ILogger<DisabledBillingProvider> logger) : IBillingProvider
{
    public Task<CreateCheckoutResult> CreateCheckoutAsync(Guid workspaceId, string redirectUrl, CancellationToken ct = default)
    {
        logger.LogInformation("Checkout blocked for workspace {WorkspaceId}: billing is disabled", workspaceId);
        throw new BillingDisabledException();
    }

    public Task<WebhookValidationResult> ValidateWebhookAsync(string body, string signatureHeader, CancellationToken ct = default)
        => Task.FromResult(new WebhookValidationResult { IsValid = false });

    public Task CancelSubscriptionAsync(string subscriptionId, CancellationToken ct = default)
        => throw new BillingDisabledException();
}
