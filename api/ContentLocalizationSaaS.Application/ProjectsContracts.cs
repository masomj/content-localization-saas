using FluentValidation;

namespace ContentLocalizationSaaS.Application;

public sealed record CreateProjectRequest(Guid WorkspaceId, string Name, string SourceLanguage, string Description);
public sealed record UpdateProjectRequest(string Name, string SourceLanguage, string Description);

public sealed class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SourceLanguage).NotEmpty().MaximumLength(16);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public sealed class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SourceLanguage).NotEmpty().MaximumLength(16);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
