using ContentLocalizationSaaS.Application;

namespace ContentLocalizationSaaS.Application.Tests;

public sealed class ProjectCollectionRequestValidatorTests
{
    [Fact]
    public void CreateValidator_Fails_WhenNameMissing()
    {
        var validator = new CreateProjectCollectionRequestValidator();
        var result = validator.Validate(new CreateProjectCollectionRequest(string.Empty));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void RenameValidator_Fails_WhenNameTooLong()
    {
        var validator = new RenameProjectCollectionRequestValidator();
        var result = validator.Validate(new RenameProjectCollectionRequest(new string('a', 201)));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void MoveValidator_Fails_WhenIndexNegative()
    {
        var validator = new MoveProjectCollectionRequestValidator();
        var result = validator.Validate(new MoveProjectCollectionRequest(null, -1));
        Assert.False(result.IsValid);
    }
}
