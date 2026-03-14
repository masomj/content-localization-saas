using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace ContentLocalizationSaaS.Api.IntegrationTests;

public class InviteFlowTests(AspireIntegrationFixture fixture) : IClassFixture<AspireIntegrationFixture>
{
    [Fact]
    public async Task Invite_Accept_Then_Revoke_DeniesAccessCheck()
    {
        var workspaceResponse = await fixture.ApiClient.PostAsJsonAsync("/api/workspaces", new { name = "Invite Flow Workspace" });
        workspaceResponse.EnsureSuccessStatusCode();
        var workspaceId = JsonNode.Parse(await workspaceResponse.Content.ReadAsStringAsync())?["id"]?.GetValue<string>();
        Assert.False(string.IsNullOrWhiteSpace(workspaceId));

        var createInvite = new HttpRequestMessage(HttpMethod.Post, "/api/admin/invites")
        {
            Content = JsonContent.Create(new { workspaceId, email = "user@example.com", role = "Editor" })
        };
        createInvite.Headers.Add("X-User-Role", "Admin");
        var inviteResponse = await fixture.ApiClient.SendAsync(createInvite);
        inviteResponse.EnsureSuccessStatusCode();

        var token = JsonNode.Parse(await inviteResponse.Content.ReadAsStringAsync())?["token"]?.GetValue<string>();
        Assert.False(string.IsNullOrWhiteSpace(token));

        var acceptResponse = await fixture.ApiClient.PostAsJsonAsync("/api/admin/invites/accept", new { token, email = "user@example.com" });
        acceptResponse.EnsureSuccessStatusCode();

        var accessReq = new HttpRequestMessage(HttpMethod.Get, $"/api/workspaces/{workspaceId}/access-check");
        accessReq.Headers.Add("X-User-Email", "user@example.com");
        var accessBeforeRevoke = await fixture.ApiClient.SendAsync(accessReq);
        accessBeforeRevoke.EnsureSuccessStatusCode();

        var revokeReq = new HttpRequestMessage(HttpMethod.Post, "/api/admin/invites/revoke")
        {
            Content = JsonContent.Create(new { workspaceId, email = "user@example.com" })
        };
        revokeReq.Headers.Add("X-User-Role", "Admin");
        var revokeResponse = await fixture.ApiClient.SendAsync(revokeReq);
        revokeResponse.EnsureSuccessStatusCode();

        var accessReqAfter = new HttpRequestMessage(HttpMethod.Get, $"/api/workspaces/{workspaceId}/access-check");
        accessReqAfter.Headers.Add("X-User-Email", "user@example.com");
        var accessAfterRevoke = await fixture.ApiClient.SendAsync(accessReqAfter);

        Assert.Equal(HttpStatusCode.Forbidden, accessAfterRevoke.StatusCode);
    }
}
