using ContentLocalizationSaaS.Domain;

namespace ContentLocalizationSaaS.Application.Abstractions;

public interface IContentItemService
{
    Task<ContentItem> CreateAsync(CreateContentItemRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ContentItem>> GetAllAsync(Guid? projectId, string? search, CancellationToken cancellationToken);
    Task<ContentItem> UpdateAsync(Guid id, string source, string status, string actorEmail, CancellationToken cancellationToken);
    Task<IReadOnlyList<ContentItemRevision>> GetRevisionsAsync(Guid contentItemId, CancellationToken cancellationToken);
    Task<object> CompareRevisionsAsync(Guid contentItemId, Guid leftRevisionId, Guid rightRevisionId, CancellationToken cancellationToken);
    Task<ContentItem> RollbackAsync(Guid contentItemId, Guid revisionId, string actorEmail, CancellationToken cancellationToken);
}
