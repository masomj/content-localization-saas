using ContentLocalizationSaaS.Domain;

namespace ContentLocalizationSaaS.Application.Abstractions;

public interface IContentItemService
{
    Task<ContentItem> CreateAsync(CreateContentItemRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ContentItem>> GetAllAsync(Guid? projectId, string? search, CancellationToken cancellationToken);
}
