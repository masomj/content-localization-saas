namespace ContentLocalizationSaaS.Application;

public static class DebugEndpointEnvironmentPolicy
{
    public const string TestEnvironmentName = "Test";

    public static bool AllowsDebugUserDeletion(string? environmentName)
    {
        return IsDevelopment(environmentName) || IsTest(environmentName);
    }

    public static string GetDebugUserDeletionDeniedReason(string? environmentName)
    {
        if (AllowsDebugUserDeletion(environmentName))
        {
            return "allowed";
        }

        if (IsProduction(environmentName))
        {
            return "disabled_in_production";
        }

        return "disabled_outside_development_or_test";
    }

    public static bool IsProduction(string? environmentName)
    {
        return string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDevelopment(string? environmentName)
    {
        return string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTest(string? environmentName)
    {
        return string.Equals(environmentName, TestEnvironmentName, StringComparison.OrdinalIgnoreCase);
    }
}
