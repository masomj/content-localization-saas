using ContentLocalizationSaaS.Application;
using ContentLocalizationSaaS.Application.Abstractions;
using ContentLocalizationSaaS.Application.Exceptions;
using ContentLocalizationSaaS.Domain;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Infrastructure.Services;

internal sealed class ContentItemService(
    AppDbContext db,
    IValidator<CreateContentItemRequest> createValidator) : IContentItemService
{
    public async Task<ContentItem> CreateAsync(CreateContentItemRequest request, CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw RequestValidationException.FromFailures(validation.Errors);
        }

        var projectExists = await db.Projects.AnyAsync(x => x.Id == request.ProjectId, cancellationToken);
        if (!projectExists) throw new ResourceNotFoundException(nameof(Project), request.ProjectId);

        var item = new ContentItem
        {
            ProjectId = request.ProjectId,
            Key = request.Key.Trim(),
            Source = request.Source.Trim(),
            Status = request.Status.Trim(),
            Tags = string.Join('|', (request.Tags ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().ToLowerInvariant())),
            Context = request.Context?.Trim() ?? string.Empty,
            Notes = request.Notes?.Trim() ?? string.Empty
        };

        db.ContentItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<IReadOnlyList<ContentItem>> GetAllAsync(Guid? projectId, string? search, CancellationToken cancellationToken)
    {
        var query = db.ContentItems.AsQueryable();

        if (projectId.HasValue)
        {
            query = query.Where(x => x.ProjectId == projectId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Key.ToLower().Contains(s) ||
                x.Tags.ToLower().Contains(s) ||
                x.Context.ToLower().Contains(s) ||
                x.Notes.ToLower().Contains(s));
        }

        return await query.OrderByDescending(x => x.CreatedUtc).ToListAsync(cancellationToken);
    }
}
