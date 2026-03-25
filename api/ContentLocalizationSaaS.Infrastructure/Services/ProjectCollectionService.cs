using ContentLocalizationSaaS.Application;
using ContentLocalizationSaaS.Application.Abstractions;
using ContentLocalizationSaaS.Application.Exceptions;
using ContentLocalizationSaaS.Domain;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Infrastructure.Services;

internal sealed class ProjectCollectionService(
    AppDbContext db,
    IValidator<CreateProjectCollectionRequest> createValidator,
    IValidator<RenameProjectCollectionRequest> renameValidator,
    IValidator<MoveProjectCollectionRequest> moveValidator) : IProjectCollectionService
{
    private const int MaxDepth = 3;

    public async Task<IReadOnlyList<ProjectCollection>> ListAsync(Guid projectId, CancellationToken cancellationToken)
    {
        await EnsureProjectExists(projectId, cancellationToken);

        return await db.ProjectCollections
            .Where(x => x.ProjectId == projectId)
            .OrderBy(x => x.Depth)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProjectCollection> CreateAsync(Guid projectId, CreateProjectCollectionRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(createValidator, request, cancellationToken);
        await EnsureProjectExists(projectId, cancellationToken);

        var name = request.Name.Trim();
        var parent = await ResolveParent(projectId, request.ParentId, cancellationToken);
        var depth = parent.Depth + 1;
        if (depth > MaxDepth)
        {
            throw new RequestValidationException(new Dictionary<string, string[]> { ["parentId"] = ["Maximum collection depth is 3."] });
        }

        await EnsureSiblingNameUnique(projectId, parent.Id, null, name, cancellationToken);

        var nextSortOrder = await db.ProjectCollections
            .Where(x => x.ProjectId == projectId && x.ParentId == parent.Id)
            .Select(x => (int?)x.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var collection = new ProjectCollection
        {
            ProjectId = projectId,
            ParentId = parent.Id,
            Name = name,
            IsRoot = false,
            Depth = depth,
            SortOrder = nextSortOrder + 1
        };

        db.ProjectCollections.Add(collection);
        await db.SaveChangesAsync(cancellationToken);
        return collection;
    }

    public async Task<ProjectCollection> RenameAsync(Guid projectId, Guid collectionId, RenameProjectCollectionRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(renameValidator, request, cancellationToken);

        var collection = await GetCollection(projectId, collectionId, cancellationToken);
        if (collection.IsRoot)
        {
            throw new RequestValidationException(new Dictionary<string, string[]> { ["name"] = ["Root collection cannot be renamed."] });
        }

        var name = request.Name.Trim();
        await EnsureSiblingNameUnique(projectId, collection.ParentId, collection.Id, name, cancellationToken);

        collection.Name = name;
        await db.SaveChangesAsync(cancellationToken);
        return collection;
    }

    public async Task<IReadOnlyList<ProjectCollection>> MoveAsync(Guid projectId, Guid collectionId, MoveProjectCollectionRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(moveValidator, request, cancellationToken);

        var collection = await GetCollection(projectId, collectionId, cancellationToken);
        if (collection.IsRoot)
        {
            throw new RequestValidationException(new Dictionary<string, string[]> { ["collectionId"] = ["Root collection cannot be moved."] });
        }

        var newParent = await ResolveParent(projectId, request.NewParentId, cancellationToken);
        if (newParent.Id == collection.Id)
        {
            throw new RequestValidationException(new Dictionary<string, string[]> { ["newParentId"] = ["Collection cannot be moved under itself."] });
        }

        await EnsureNotDescendant(projectId, collection.Id, newParent.Id, cancellationToken);

        var depthDelta = (newParent.Depth + 1) - collection.Depth;
        if (depthDelta > 0)
        {
            var maxDescendantDepth = await GetMaxDescendantDepth(projectId, collection.Id, cancellationToken);
            if (maxDescendantDepth + depthDelta > MaxDepth)
            {
                throw new RequestValidationException(new Dictionary<string, string[]> { ["newParentId"] = ["Move would exceed maximum depth of 3."] });
            }
        }

        await EnsureSiblingNameUnique(projectId, newParent.Id, collection.Id, collection.Name, cancellationToken);

        var oldSiblings = await db.ProjectCollections
            .Where(x => x.ProjectId == projectId && x.ParentId == collection.ParentId && x.Id != collection.Id)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        for (var i = 0; i < oldSiblings.Count; i++)
        {
            oldSiblings[i].SortOrder = i;
        }

        var newSiblings = await db.ProjectCollections
            .Where(x => x.ProjectId == projectId && x.ParentId == newParent.Id && x.Id != collection.Id)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        var newIndex = Math.Clamp(request.NewIndex, 0, newSiblings.Count);
        newSiblings.Insert(newIndex, collection);

        collection.ParentId = newParent.Id;
        collection.Depth = newParent.Depth + 1;

        for (var i = 0; i < newSiblings.Count; i++)
        {
            newSiblings[i].SortOrder = i;
        }

        await UpdateDescendantDepths(projectId, collection.Id, depthDelta, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return await ListAsync(projectId, cancellationToken);
    }

    private async Task<ProjectCollection> ResolveParent(Guid projectId, Guid? parentId, CancellationToken cancellationToken)
    {
        if (parentId is null)
        {
            return await db.ProjectCollections.FirstAsync(x => x.ProjectId == projectId && x.IsRoot, cancellationToken);
        }

        var parent = await db.ProjectCollections.FirstOrDefaultAsync(x => x.ProjectId == projectId && x.Id == parentId.Value, cancellationToken)
            ?? throw new ResourceNotFoundException(nameof(ProjectCollection), parentId.Value);

        return parent;
    }

    private async Task<ProjectCollection> GetCollection(Guid projectId, Guid collectionId, CancellationToken cancellationToken)
        => await db.ProjectCollections.FirstOrDefaultAsync(x => x.ProjectId == projectId && x.Id == collectionId, cancellationToken)
            ?? throw new ResourceNotFoundException(nameof(ProjectCollection), collectionId);

    private async Task EnsureProjectExists(Guid projectId, CancellationToken cancellationToken)
    {
        if (!await db.Projects.AnyAsync(x => x.Id == projectId, cancellationToken))
        {
            throw new ResourceNotFoundException(nameof(Project), projectId);
        }
    }

    private async Task EnsureSiblingNameUnique(Guid projectId, Guid? parentId, Guid? excludedId, string name, CancellationToken cancellationToken)
    {
        var normalized = name.Trim().ToLowerInvariant();
        var exists = await db.ProjectCollections
            .Where(x => x.ProjectId == projectId && x.ParentId == parentId)
            .Where(x => excludedId == null || x.Id != excludedId.Value)
            .AnyAsync(x => x.Name.ToLower() == normalized, cancellationToken);

        if (exists)
        {
            throw new RequestValidationException(new Dictionary<string, string[]> { ["name"] = ["A sibling collection with that name already exists."] });
        }
    }

    private async Task EnsureNotDescendant(Guid projectId, Guid sourceId, Guid candidateParentId, CancellationToken cancellationToken)
    {
        var all = await db.ProjectCollections.Where(x => x.ProjectId == projectId).ToListAsync(cancellationToken);
        var lookup = all.ToDictionary(x => x.Id, x => x.ParentId);

        var cursor = candidateParentId;
        while (lookup.TryGetValue(cursor, out var parentId) && parentId is Guid parent)
        {
            if (parent == sourceId || cursor == sourceId)
            {
                throw new RequestValidationException(new Dictionary<string, string[]> { ["newParentId"] = ["Collection cannot be moved into its descendant."] });
            }

            cursor = parent;
        }
    }

    private async Task<int> GetMaxDescendantDepth(Guid projectId, Guid rootId, CancellationToken cancellationToken)
    {
        var all = await db.ProjectCollections.Where(x => x.ProjectId == projectId).ToListAsync(cancellationToken);
        var map = all.ToDictionary(x => x.Id);

        var maxDepth = map[rootId].Depth;
        var stack = new Stack<Guid>();
        stack.Push(rootId);

        while (stack.Count > 0)
        {
            var id = stack.Pop();
            var childIds = all.Where(x => x.ParentId == id).Select(x => x.Id).ToList();

            foreach (var childId in childIds)
            {
                var depth = map[childId].Depth;
                if (depth > maxDepth) maxDepth = depth;
                stack.Push(childId);
            }
        }

        return maxDepth;
    }

    private async Task UpdateDescendantDepths(Guid projectId, Guid rootId, int depthDelta, CancellationToken cancellationToken)
    {
        if (depthDelta == 0) return;

        var all = await db.ProjectCollections.Where(x => x.ProjectId == projectId).ToListAsync(cancellationToken);

        var stack = new Stack<Guid>();
        stack.Push(rootId);

        while (stack.Count > 0)
        {
            var id = stack.Pop();
            var childRows = all.Where(x => x.ParentId == id).ToList();

            foreach (var child in childRows)
            {
                child.Depth += depthDelta;
                stack.Push(child.Id);
            }
        }
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw RequestValidationException.FromFailures(validation.Errors);
        }
    }

    public Task<List<ProjectTreeNode>> GetTreeAsync(Guid projectId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
