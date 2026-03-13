using ContentLocalizationSaaS.Application;
using ContentLocalizationSaaS.Application.Abstractions;
using ContentLocalizationSaaS.Application.Exceptions;
using ContentLocalizationSaaS.Domain;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Infrastructure.Services;

internal sealed class ProjectService(
    AppDbContext db,
    IValidator<CreateProjectRequest> createValidator,
    IValidator<UpdateProjectRequest> updateValidator) : IProjectService
{
    public async Task<IReadOnlyList<Project>> GetAllAsync(Guid? workspaceId, CancellationToken cancellationToken)
    {
        var query = db.Projects.AsQueryable();

        if (workspaceId.HasValue)
        {
            query = query.Where(x => x.WorkspaceId == workspaceId.Value);
        }

        return await query.OrderBy(x => x.CreatedUtc).ToListAsync(cancellationToken);
    }

    public async Task<Project> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await db.Projects.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
           ?? throw new ResourceNotFoundException(nameof(Project), id);

    public async Task<Project> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(createValidator, request, cancellationToken);

        var workspaceExists = await db.Workspaces.AnyAsync(x => x.Id == request.WorkspaceId, cancellationToken);
        if (!workspaceExists)
        {
            throw new ResourceNotFoundException(nameof(Workspace), request.WorkspaceId);
        }

        var project = new Project
        {
            WorkspaceId = request.WorkspaceId,
            Name = request.Name.Trim(),
            SourceLanguage = request.SourceLanguage.Trim()
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync(cancellationToken);
        return project;
    }

    public async Task<Project> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(updateValidator, request, cancellationToken);

        var project = await db.Projects.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException(nameof(Project), id);

        project.Name = request.Name.Trim();
        project.SourceLanguage = request.SourceLanguage.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return project;
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw RequestValidationException.FromFailures(validation.Errors);
        }
    }
}
