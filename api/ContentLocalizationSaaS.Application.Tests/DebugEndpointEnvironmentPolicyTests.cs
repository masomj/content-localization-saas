using ContentLocalizationSaaS.Application;

namespace ContentLocalizationSaaS.Application.Tests;

public sealed class DebugEndpointEnvironmentPolicyTests
{
    [Theory]
    [InlineData("Development")]
    [InlineData("development")]
    [InlineData("Test")]
    [InlineData("test")]
    public void AllowsDebugUserDeletion_ReturnsTrue_ForDevelopmentOrTest(string environmentName)
    {
        var allowed = DebugEndpointEnvironmentPolicy.AllowsDebugUserDeletion(environmentName);

        Assert.True(allowed);
    }

    [Theory]
    [InlineData("Production", "disabled_in_production")]
    [InlineData("Staging", "disabled_outside_development_or_test")]
    [InlineData(null, "disabled_outside_development_or_test")]
    public void GetDebugUserDeletionDeniedReason_ReturnsExpectedReason(string? environmentName, string expectedReason)
    {
        var reason = DebugEndpointEnvironmentPolicy.GetDebugUserDeletionDeniedReason(environmentName);

        Assert.Equal(expectedReason, reason);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("production")]
    public void IsProduction_ReturnsTrue_ForProductionName(string environmentName)
    {
        var isProduction = DebugEndpointEnvironmentPolicy.IsProduction(environmentName);

        Assert.True(isProduction);
    }
}
