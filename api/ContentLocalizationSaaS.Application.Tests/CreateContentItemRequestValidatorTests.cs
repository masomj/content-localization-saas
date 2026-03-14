using ContentLocalizationSaaS.Application;

namespace ContentLocalizationSaaS.Application.Tests;

public class CreateContentItemRequestValidatorTests
{
    private readonly CreateContentItemRequestValidator _validator = new();

    [Fact]
    public void Should_Fail_When_Key_Format_Is_Invalid()
    {
        var req = new CreateContentItemRequest(Guid.NewGuid(), "Invalid Key", "Hello", "draft", ["tag"], "ctx", "notes");
        var result = _validator.Validate(req);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_Pass_When_Required_Fields_Are_Valid()
    {
        var req = new CreateContentItemRequest(Guid.NewGuid(), "auth.login.title", "Hello", "draft", ["auth", "login"], "screen", "notes");
        var result = _validator.Validate(req);
        Assert.True(result.IsValid);
    }
}
