using ContentLocalizationSaaS.Application;

namespace ContentLocalizationSaaS.Application.Tests;

public sealed class EmailAllowlistTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyConfig_IsDisabled_AndAllowsEveryone(string? raw)
    {
        var allowlist = new EmailAllowlist(raw);

        Assert.False(allowlist.IsEnabled);
        Assert.True(allowlist.IsAllowed("anyone@example.com"));
        Assert.True(allowlist.IsAllowed(null));
    }

    [Theory]
    [InlineData("mason@example.com")]
    [InlineData("MASON@Example.com")]
    [InlineData("  mason@example.com ")]
    public void ExactEmail_IsAllowed_CaseInsensitive(string email)
    {
        var allowlist = new EmailAllowlist("mason@example.com");

        Assert.True(allowlist.IsAllowed(email));
    }

    [Theory]
    [InlineData("@team.dev")]
    [InlineData("*@team.dev")]
    public void DomainEntry_AllowsAnyAddressOnThatDomain(string entry)
    {
        var allowlist = new EmailAllowlist(entry);

        Assert.True(allowlist.IsAllowed("someone@team.dev"));
        Assert.False(allowlist.IsAllowed("someone@notteam.dev"));
        Assert.False(allowlist.IsAllowed("someone@sub.team.dev"));
    }

    [Fact]
    public void MixedSeparators_AreAllParsed()
    {
        var allowlist = new EmailAllowlist("a@x.com, b@y.com;@z.com\nc@w.com");

        Assert.True(allowlist.IsAllowed("a@x.com"));
        Assert.True(allowlist.IsAllowed("b@y.com"));
        Assert.True(allowlist.IsAllowed("anyone@z.com"));
        Assert.True(allowlist.IsAllowed("c@w.com"));
    }

    [Theory]
    [InlineData("stranger@example.com")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void WhenEnabled_UnknownOrMissingEmail_IsBlocked(string? email)
    {
        var allowlist = new EmailAllowlist("mason@example.com");

        Assert.False(allowlist.IsAllowed(email));
    }
}
