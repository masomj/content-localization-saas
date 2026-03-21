namespace ContentLocalizationSaaS.Api.Authorization;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public bool LegacyHeaderAuthEnabled { get; set; } = true;

    public OidcOptions Oidc { get; set; } = new();
}

public sealed class OidcOptions
{
    public string Issuer { get; set; } = "http://localhost:8080/realms/locflow";
    public string Audience { get; set; } = "locflow-web";
    public bool RequireHttpsMetadata { get; set; } = false;
}
