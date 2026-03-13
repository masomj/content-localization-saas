using ContentLocalizationSaaS.Application;
using ContentLocalizationSaaS.Application.Abstractions;
using ContentLocalizationSaaS.Application.Exceptions;
using ContentLocalizationSaaS.Domain;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Infrastructure.Services;

internal sealed class WorkspaceService(
    AppDbContext db,
    IValidator<CreateWorkspaceRequest> createValidator,
    IValidator<UpdateWorkspaceRequest> updateValidator) : IWorkspaceService
{
    public async Task<IReadOnlyList<Workspace>> GetAllAsync(CancellationToken cancellationToken)
        => await db.Workspaces.OrderBy(x => x.CreatedUtc).ToListAsync(cancellationToken);

    public async Task<Workspace> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await db.Workspaces.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
           ?? throw new ResourceNotFoundException(nameof(Workspace), id);

    public async Task<Workspace> CreateAsync(CreateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(createValidator, request, cancellationToken);

        var workspace = new Workspace
        {
            Name = request.Name.Trim()
        };

        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync(cancellationToken);
        return workspace;
    }

    public async Task<Workspace> UpdateAsync(Guid id, UpdateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(updateValidator, request, cancellationToken);

        var workspace = await db.Workspaces.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException(nameof(Workspace), id);

        workspace.Name = request.Name.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return workspace;
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
