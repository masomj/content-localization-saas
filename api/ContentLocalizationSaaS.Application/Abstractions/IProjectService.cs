using ContentLocalizationSaaS.Domain;

namespace ContentLocalizationSaaS.Application.Abstractions;

public interface IProjectService
{
    Task<IReadOnlyList<Project>> GetAllAsync(Guid? workspaceId, CancellationToken cancellationToken);
    Task<Project> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Project> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken);
    Task<Project> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProjectAuditLog>> GetAuditLogsAsync(Guid projectId, CancellationToken cancellationToken);
}
