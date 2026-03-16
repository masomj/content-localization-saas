using ContentLocalizationSaaS.Domain;

namespace ContentLocalizationSaaS.Application.Abstractions;

public interface IProjectCollectionService
{
    Task<IReadOnlyList<ProjectCollection>> ListAsync(Guid projectId, CancellationToken cancellationToken);
    Task<ProjectCollection> CreateAsync(Guid projectId, CreateProjectCollectionRequest request, CancellationToken cancellationToken);
    Task<ProjectCollection> RenameAsync(Guid projectId, Guid collectionId, RenameProjectCollectionRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProjectCollection>> MoveAsync(Guid projectId, Guid collectionId, MoveProjectCollectionRequest request, CancellationToken cancellationToken);
}
