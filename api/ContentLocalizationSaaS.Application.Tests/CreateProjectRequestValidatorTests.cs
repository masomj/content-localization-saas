using ContentLocalizationSaaS.Application;

namespace ContentLocalizationSaaS.Application.Tests;

public class CreateProjectRequestValidatorTests
{
    private readonly CreateProjectRequestValidator _validator = new();

    [Fact]
    public void Should_Fail_When_Name_Is_Empty()
    {
        var result = _validator.Validate(new CreateProjectRequest(Guid.NewGuid(), "", "en", ""));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_Pass_For_Valid_Request()
    {
        var result = _validator.Validate(new CreateProjectRequest(Guid.NewGuid(), "Core Product", "en", "Website content"));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Should_Fail_When_Description_Too_Long()
    {
        var tooLong = new string('a', 2001);
        var result = _validator.Validate(new CreateProjectRequest(Guid.NewGuid(), "Core Product", "en", tooLong));
        Assert.False(result.IsValid);
    }
}
