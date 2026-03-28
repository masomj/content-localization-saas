namespace ContentLocalizationSaaS.Api.Authorization;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public bool LegacyHeaderAuthEnabled { get; set; } = true;

    public OidcOptions Oidc { get; set; } = new();
}

public sealed class OidcOptions
{
    public string Issuer { get; set; } = "http://localhost:8080/realms/InterCopy";
    public string Audience { get; set; } = "InterCopy-web";
    public bool RequireHttpsMetadata { get; set; } = false;
}
