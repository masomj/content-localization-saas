using ContentLocalizationSaaS.Domain;

namespace ContentLocalizationSaaS.Application.Abstractions;

public interface IWorkspaceService
{
    Task<IReadOnlyList<Workspace>> GetAllAsync(CancellationToken cancellationToken);
    Task<Workspace> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Workspace> CreateAsync(CreateWorkspaceRequest request, CancellationToken cancellationToken);
    Task<Workspace> UpdateAsync(Guid id, UpdateWorkspaceRequest request, CancellationToken cancellationToken);
}
