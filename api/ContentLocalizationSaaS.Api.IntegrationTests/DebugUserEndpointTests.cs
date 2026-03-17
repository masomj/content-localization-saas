using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ContentLocalizationSaaS.Api.IntegrationTests;

public sealed class DebugUserEndpointTests(AspireIntegrationFixture fixture) : IClassFixture<AspireIntegrationFixture>
{
    [Fact]
    public async Task DeleteDebugUser_ByEmail_DeletesUserAndReturnsOutcomeJson()
    {
        var email = $"debug-delete-{Guid.NewGuid():N}@example.com";
        const string password = "DebugPass123!";

        var registerResponse = await fixture.ApiClient.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            firstName = "Debug",
            lastName = "Delete",
            company = "Test Co"
        });
        registerResponse.EnsureSuccessStatusCode();

        var deleteResponse = await fixture.ApiClient.DeleteAsync($"/debug/users?email={Uri.EscapeDataString(email)}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        using var deletePayload = JsonDocument.Parse(await deleteResponse.Content.ReadAsStringAsync());
        Assert.True(deletePayload.RootElement.GetProperty("deleted").GetBoolean());
        Assert.Equal("deleted", deletePayload.RootElement.GetProperty("reason").GetString());
        Assert.Equal(email, deletePayload.RootElement.GetProperty("email").GetString());

        var loginResponse = await fixture.ApiClient.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password
        });

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteDebugUser_WithoutEmailOrId_ReturnsBadRequest()
    {
        var response = await fixture.ApiClient.DeleteAsync("/debug/users");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(payload.RootElement.GetProperty("deleted").GetBoolean());
        Assert.Equal("email_or_id_required", payload.RootElement.GetProperty("reason").GetString());
    }
}
