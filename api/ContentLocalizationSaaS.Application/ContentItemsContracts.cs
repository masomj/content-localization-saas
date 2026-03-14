using FluentValidation;

namespace ContentLocalizationSaaS.Application;

public sealed record CreateContentItemRequest(
    Guid ProjectId,
    string Key,
    string Source,
    string Status,
    string[]? Tags,
    string? Context,
    string? Notes);

public sealed class CreateContentItemRequestValidator : AbstractValidator<CreateContentItemRequest>
{
    private const string KeyPattern = "^[a-z0-9]+([._-][a-z0-9]+)*$";

    public CreateContentItemRequestValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Key)
            .NotEmpty()
            .MaximumLength(200)
            .Matches(KeyPattern)
            .WithMessage("Key must use lowercase letters/numbers and separators (., _, -).") ;
        RuleFor(x => x.Source).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Status).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Context).MaximumLength(1000);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleForEach(x => x.Tags!).MaximumLength(64);
    }
}
