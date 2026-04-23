using FluentValidation;

namespace ContentLocalizationSaaS.Application;

public sealed record CreateContentItemRequest(
    Guid ProjectId,
    string Key,
    string Source,
    string Status,
    string[]? Tags,
    string? Context,
    string? Notes,
    string? Description = null,
    int? MaxLength = null,
    string? ContentType = null,
    Guid? CollectionId = null);

public sealed record MoveContentItemRequest(Guid? CollectionId, int SortOrder);

public sealed record ProjectTreeNode
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string NodeType { get; init; } // "folder" or "contentKey"
    public Guid? ParentId { get; init; }
    public int SortOrder { get; init; }
    public int Depth { get; init; }
    public List<ProjectTreeNode> Children { get; init; } = [];
    // For contentKey nodes only:
    public string? Key { get; init; }
    public string? Status { get; init; }
}

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
            .WithMessage("Key must use lowercase letters/numbers and separators (., _, -).");
        RuleFor(x => x.Source).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Status).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Context).MaximumLength(1000);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.MaxLength).GreaterThan(0).When(x => x.MaxLength.HasValue);
        RuleFor(x => x.ContentType).MaximumLength(50)
            .Must(v => string.IsNullOrEmpty(v) || ValidContentTypes.Contains(v))
            .WithMessage("Invalid content type.");
        RuleForEach(x => x.Tags!).MaximumLength(64);
    }

    private static readonly HashSet<string> ValidContentTypes = new(StringComparer.Ordinal)
    {
        "", "button_label", "error_message", "heading", "body_text", "placeholder", "tooltip", "menu_item"
    };
}

public sealed class MoveContentItemRequestValidator : AbstractValidator<MoveContentItemRequest>
{
    public MoveContentItemRequestValidator()
    {
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}
