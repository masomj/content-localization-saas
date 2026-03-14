using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace ContentLocalizationSaaS.Api.IntegrationTests;

public class MembershipAuditTests(AspireIntegrationFixture fixture) : IClassFixture<AspireIntegrationFixture>
{
    [Fact]
    public async Task NonAdmin_Cannot_View_MembershipAudit()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/admin/membership-audit");
        req.Headers.Add("X-User-Role", "Viewer");
        var response = await fixture.ApiClient.SendAsync(req);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MembershipAudit_Supports_Filtering_And_Csv_Export()
    {
        var wsResponse = await fixture.ApiClient.PostAsJsonAsync("/api/workspaces", new { name = "Audit Workspace" });
        wsResponse.EnsureSuccessStatusCode();
        var workspaceId = JsonNode.Parse(await wsResponse.Content.ReadAsStringAsync())?["id"]?.GetValue<string>();
        Assert.False(string.IsNullOrWhiteSpace(workspaceId));

        var createInviteReq = new HttpRequestMessage(HttpMethod.Post, "/api/admin/invites")
        {
            Content = JsonContent.Create(new { workspaceId, email = "audit.user@example.com", role = "Editor" })
        };
        createInviteReq.Headers.Add("X-User-Role", "Admin");
        createInviteReq.Headers.Add("X-Actor-Email", "owner@example.com");
        var inviteResponse = await fixture.ApiClient.SendAsync(createInviteReq);
        inviteResponse.EnsureSuccessStatusCode();

        var listReq = new HttpRequestMessage(HttpMethod.Get, $"/api/admin/membership-audit?targetEmail=audit.user@example.com&action=invite_created");
        listReq.Headers.Add("X-User-Role", "Admin");
        var listResponse = await fixture.ApiClient.SendAsync(listReq);
        listResponse.EnsureSuccessStatusCode();
        var payload = await listResponse.Content.ReadAsStringAsync();
        Assert.Contains("invite_created", payload);

        var csvReq = new HttpRequestMessage(HttpMethod.Get, "/api/admin/membership-audit?format=csv");
        csvReq.Headers.Add("X-User-Role", "Admin");
        var csvResponse = await fixture.ApiClient.SendAsync(csvReq);
        csvResponse.EnsureSuccessStatusCode();
        var csv = await csvResponse.Content.ReadAsStringAsync();
        Assert.Contains("timestampUtc,actorEmail,targetEmail,action,oldValue,newValue", csv);
    }
}
