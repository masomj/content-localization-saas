using ContentLocalizationSaaS.Application;

namespace ContentLocalizationSaaS.Application.Tests;

public sealed class CopyEnvironmentsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("prod")]
    [InlineData("Production")]
    [InlineData("LIVE")]
    public void NormalizeForExport_ReturnsNull_ForProductionOrMissing(string? environment)
    {
        Assert.Null(CopyEnvironments.NormalizeForExport(environment));
    }

    [Fact]
    public void NormalizeForExport_LowercasesAndTrims()
    {
        Assert.Equal("dev", CopyEnvironments.NormalizeForExport("  Dev "));
    }

    [Theory]
    [InlineData("dev", "dev")]
    [InlineData("Staging", "staging")]
    [InlineData("feature-123", "feature-123")]
    public void TryNormalizeForVariant_AcceptsSlugs(string input, string expected)
    {
        Assert.True(CopyEnvironments.TryNormalizeForVariant(input, out var normalized, out var error));
        Assert.Equal(expected, normalized);
        Assert.Null(error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("prod")]
    [InlineData("production")]
    [InlineData("-dev")]
    [InlineData("dev env")]
    [InlineData("dev_1")]
    [InlineData("this-environment-name-is-far-too-long-to-accept")]
    public void TryNormalizeForVariant_RejectsInvalidOrProduction(string? input)
    {
        Assert.False(CopyEnvironments.TryNormalizeForVariant(input, out _, out var error));
        Assert.False(string.IsNullOrWhiteSpace(error));
    }
}

public sealed class EnvironmentVariantIndexTests
{
    private static readonly Guid ItemA = Guid.NewGuid();
    private static readonly Guid ItemB = Guid.NewGuid();

    [Fact]
    public void Empty_ReturnsProductionCopyUnchanged()
    {
        var index = EnvironmentVariantIndex.Empty;

        Assert.Equal("Sign in", index.ResolveSource(ItemA, "Sign in"));
        Assert.Equal("Connexion", index.ResolveTranslation(ItemA, "fr", "Connexion", "Sign in"));
        Assert.Equal("Sign in", index.ResolveTranslation(ItemA, "fr", null, "Sign in"));
    }

    [Fact]
    public void SourceVariant_ReplacesSource_OnlyForThatItem()
    {
        var index = new EnvironmentVariantIndex([(ItemA, "", "Log in (test)")]);

        Assert.Equal("Log in (test)", index.ResolveSource(ItemA, "Sign in"));
        Assert.Equal("Continue", index.ResolveSource(ItemB, "Continue"));
    }

    [Fact]
    public void LanguageVariant_WinsOverProductionTranslation()
    {
        var index = new EnvironmentVariantIndex([(ItemA, "fr", "Se connecter (test)")]);

        Assert.Equal("Se connecter (test)", index.ResolveTranslation(ItemA, "FR", "Connexion", "Sign in"));
    }

    [Fact]
    public void ProductionTranslation_WinsOverSourceVariant()
    {
        var index = new EnvironmentVariantIndex([(ItemA, "", "Log in (test)")]);

        Assert.Equal("Connexion", index.ResolveTranslation(ItemA, "fr", "Connexion", "Sign in"));
    }

    [Fact]
    public void UntranslatedLanguage_FallsBackToSourceVariant()
    {
        var index = new EnvironmentVariantIndex([(ItemA, "", "Log in (test)")]);

        Assert.Equal("Log in (test)", index.ResolveTranslation(ItemA, "de", "  ", "Sign in"));
    }

    [Fact]
    public void HasVariant_IsLanguageSpecific()
    {
        var index = new EnvironmentVariantIndex([(ItemA, "fr", "x")]);

        Assert.True(index.HasVariant(ItemA, "fr"));
        Assert.False(index.HasVariant(ItemA, null));
        Assert.False(index.HasVariant(ItemB, "fr"));
    }
}
