namespace ContentLocalizationSaaS.Api.IntegrationTests;

public class HealthEndpointTests(AspireIntegrationFixture fixture) : IClassFixture<AspireIntegrationFixture>
{
    [Fact]
    public async Task Healthz_Returns_Success()
    {
        var response = await fixture.ApiClient.GetAsync("/healthz");
        Assert.True(response.IsSuccessStatusCode);
    }
}
