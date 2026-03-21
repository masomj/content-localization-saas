var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder
    .AddPostgres("postgres")
    .WithDataVolume("locflow-postgres-data")
    .WithPgAdmin();

var contentDb = postgres.AddDatabase("contentdb", "content_localization");
var keycloakDb = postgres.AddDatabase("keycloakdb", "keycloak");

var keycloakAdminUser = builder.AddParameter("keycloak-admin-user", "admin");
var keycloakAdminPassword = builder.AddParameter("keycloak-admin-password", secret: true);

var keycloak = builder
    .AddContainer("keycloak", "quay.io/keycloak/keycloak", "26.1")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
    .WithEnvironment("KEYCLOAK_ADMIN", keycloakAdminUser)
    .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", keycloakAdminPassword)
    .WithEnvironment("KC_DB", "postgres")
    .WithEnvironment("KC_DB_URL", keycloakDb)
    .WithEnvironment("KC_DB_USERNAME", "postgres")
    .WithEnvironment("KC_DB_PASSWORD", postgres.Resource.PasswordParameter)
    .WithEnvironment("KC_HEALTH_ENABLED", "true")
    .WithEnvironment("KC_METRICS_ENABLED", "true")
    .WithEnvironment("KC_HOSTNAME_STRICT", "false")
    .WithEnvironment("KC_PROXY_HEADERS", "xforwarded")
    .WithEnvironment("KC_HTTP_ENABLED", "true")
    .WithEnvironment("KC_SPI_THEME_STATIC_MAX_AGE", "-1")
    .WithEnvironment("KC_SPI_THEME_CACHE_THEMES", "false")
    .WithEnvironment("KC_SPI_THEME_CACHE_TEMPLATES", "false")
    .WithBindMount("..\\..\\keycloak\\themes", "/opt/keycloak/themes")
    .WithBindMount("..\\..\\keycloak\\realm", "/opt/keycloak/data/import")
    .WithArgs("start-dev", "--import-realm")
    .WaitFor(keycloakDb);

var api = builder
    .AddProject<Projects.ContentLocalizationSaaS_Api>("api")
    .WaitFor(contentDb)
    .WaitFor(keycloak)
    .WithReference(contentDb)
    .WithEnvironment("Auth__Oidc__Issuer", "http://localhost:8080/realms/locflow")
    .WithEnvironment("Auth__Oidc__Audience", "locflow-web")
    .WithEnvironment("Auth__Oidc__RequireHttpsMetadata", "false");

var frontend = builder.AddJavaScriptApp("frontend", "..\\..\\frontend", "dev")
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WaitFor(api)
    .WithReference(api)
    .WithEnvironment("NUXT_PUBLIC_KEYCLOAK_URL", "http://localhost:8080")
    .WithEnvironment("NUXT_PUBLIC_KEYCLOAK_REALM", "locflow")
    .WithEnvironment("NUXT_PUBLIC_KEYCLOAK_CLIENT_ID", "locflow-web");

builder.Build().Run();

public partial class Program;
