namespace ContentLocalizationSaaS.Api.IntegrationTests;

public sealed class ScalarOpenApiEndpointTests(AspireIntegrationFixture fixture) : IClassFixture<AspireIntegrationFixture>
{
    [Fact]
    public async Task ScalarReferenceEndpoint_IsAvailable_ForDebugging()
    {
        var response = await fixture.ApiClient.GetAsync("/scalar/v1");

        Assert.NotEqual(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}
