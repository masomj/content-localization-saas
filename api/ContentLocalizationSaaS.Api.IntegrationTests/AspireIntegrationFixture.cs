using Aspire.Hosting;
using Aspire.Hosting.Testing;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.IntegrationTests;

public sealed class AspireIntegrationFixture : IAsyncLifetime
{
    private DistributedApplication? _app;

    public HttpClient ApiClient { get; private set; } = null!;
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<global::Program>();
        _app = await builder.BuildAsync();
        await _app.StartAsync();

        var apiEndpoint = _app.GetEndpoint("api", "http");
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        ApiClient = new HttpClient(handler) { BaseAddress = apiEndpoint };

        ConnectionString = await _app.GetConnectionStringAsync("contentdb")
            ?? throw new InvalidOperationException("contentdb connection string not found from Aspire app host.");

        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    public async Task SeedAsync(Func<AppDbContext, Task> seed)
    {
        await using var db = CreateDbContext();
        await seed(db);
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }

        ApiClient.Dispose();
    }
}
