using FluentValidation;

namespace ContentLocalizationSaaS.Application;

public sealed record CreateProjectCollectionRequest(string Name, Guid? ParentId = null);
public sealed record RenameProjectCollectionRequest(string Name);
public sealed record MoveProjectCollectionRequest(Guid? NewParentId, int NewIndex);

public sealed class CreateProjectCollectionRequestValidator : AbstractValidator<CreateProjectCollectionRequest>
{
    public CreateProjectCollectionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class RenameProjectCollectionRequestValidator : AbstractValidator<RenameProjectCollectionRequest>
{
    public RenameProjectCollectionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public sealed class MoveProjectCollectionRequestValidator : AbstractValidator<MoveProjectCollectionRequest>
{
    public MoveProjectCollectionRequestValidator()
    {
        RuleFor(x => x.NewIndex).GreaterThanOrEqualTo(0);
    }
}
