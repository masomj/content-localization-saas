using System.Net;
using System.Net.Http.Json;
using ContentLocalizationSaaS.Domain;

namespace ContentLocalizationSaaS.Api.IntegrationTests;

public class WorkspaceValidationTests(AspireIntegrationFixture fixture) : IClassFixture<AspireIntegrationFixture>
{
    [Fact]
    public async Task CreateWorkspace_WithEmptyName_ReturnsBadRequest()
    {
        var response = await fixture.ApiClient.PostAsJsonAsync("/api/workspaces", new { name = "" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CanSeedWorkspaceThroughFixtureDbContext()
    {
        await fixture.SeedAsync(db =>
        {
            db.Workspaces.Add(new Workspace { Name = "Seeded Workspace" });
            return Task.CompletedTask;
        });

        var response = await fixture.ApiClient.GetAsync("/api/workspaces");
        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("Seeded Workspace", payload);
    }
}
