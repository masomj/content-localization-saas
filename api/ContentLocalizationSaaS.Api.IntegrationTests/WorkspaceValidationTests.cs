using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
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

    [Fact]
    public async Task UpdatingProjectMetadata_CreatesAuditLogEntry()
    {
        var workspaceResponse = await fixture.ApiClient.PostAsJsonAsync("/api/workspaces", new { name = "Story 1.1 Workspace" });
        workspaceResponse.EnsureSuccessStatusCode();
        var workspaceJson = JsonNode.Parse(await workspaceResponse.Content.ReadAsStringAsync());
        var workspaceId = workspaceJson?["id"]?.GetValue<string>();
        Assert.False(string.IsNullOrWhiteSpace(workspaceId));

        var projectResponse = await fixture.ApiClient.PostAsJsonAsync("/api/projects", new
        {
            workspaceId,
            name = "Story 1.1 Project",
            sourceLanguage = "en",
            description = "Initial description"
        });
        projectResponse.EnsureSuccessStatusCode();

        var projectJson = JsonNode.Parse(await projectResponse.Content.ReadAsStringAsync());
        var projectId = projectJson?["id"]?.GetValue<string>();
        Assert.False(string.IsNullOrWhiteSpace(projectId));

        var updateResponse = await fixture.ApiClient.PutAsJsonAsync($"/api/projects/{projectId}", new
        {
            name = "Story 1.1 Project Updated",
            sourceLanguage = "en-GB",
            description = "Updated description"
        });
        updateResponse.EnsureSuccessStatusCode();

        var logsResponse = await fixture.ApiClient.GetAsync($"/api/projects/{projectId}/audit-logs");
        logsResponse.EnsureSuccessStatusCode();
        var logsJson = JsonNode.Parse(await logsResponse.Content.ReadAsStringAsync())?.AsArray();
        Assert.NotNull(logsJson);
        Assert.Contains(logsJson!, x => x?["action"]?.GetValue<string>() == "project_metadata_updated");
    }

    [Fact]
    public async Task ViewerRole_CannotAccess_AuditLogs_Endpoint()
    {
        var workspaceResponse = await fixture.ApiClient.PostAsJsonAsync("/api/workspaces", new { name = "Role Test Workspace" });
        workspaceResponse.EnsureSuccessStatusCode();
        var workspaceId = JsonNode.Parse(await workspaceResponse.Content.ReadAsStringAsync())?["id"]?.GetValue<string>();

        var projectResponse = await fixture.ApiClient.PostAsJsonAsync("/api/projects", new
        {
            workspaceId,
            name = "Role Test Project",
            sourceLanguage = "en",
            description = "desc"
        });
        projectResponse.EnsureSuccessStatusCode();
        var projectId = JsonNode.Parse(await projectResponse.Content.ReadAsStringAsync())?["id"]?.GetValue<string>();

        var req = new HttpRequestMessage(HttpMethod.Get, $"/api/projects/{projectId}/audit-logs");
        req.Headers.Add("X-User-Role", "Viewer");
        var response = await fixture.ApiClient.SendAsync(req);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
