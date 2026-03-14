namespace ContentLocalizationSaaS.Api.Authorization;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    // Staged rollout flag for Story 7.3.
    // true: allow legacy X-User-Role fallback when no claims identity is present.
    // false: claims-only authorization.
    public bool LegacyHeaderAuthEnabled { get; set; } = true;

    public JwtOptions Jwt { get; set; } = new();
}

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "content-localization-api";
    public string Audience { get; set; } = "content-localization-clients";
    public string SigningKey { get; set; } = "dev-only-change-me-to-32-plus-chars";
}
