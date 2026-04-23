using ContentLocalizationSaaS.Application;
using ContentLocalizationSaaS.Application.Abstractions;
using ContentLocalizationSaaS.Application.Exceptions;
using ContentLocalizationSaaS.Domain;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace ContentLocalizationSaaS.Infrastructure.Services;

internal sealed class ContentItemService(
    AppDbContext db,
    IValidator<CreateContentItemRequest> createValidator) : IContentItemService
{
    private static readonly Regex InvalidKeyCharsRegex = new("[^a-z0-9._-]", RegexOptions.Compiled);

    public async Task<ContentItem> CreateAsync(CreateContentItemRequest request, CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw RequestValidationException.FromFailures(validation.Errors);
        }

        var projectExists = await db.Projects.AnyAsync(x => x.Id == request.ProjectId, cancellationToken);
        if (!projectExists) throw new ResourceNotFoundException(nameof(Project), request.ProjectId);

        var normalizedKey = await BuildPrefixedKeyAsync(request.ProjectId, request.CollectionId, request.Key, cancellationToken);

        var item = new ContentItem
        {
            ProjectId = request.ProjectId,
            CollectionId = request.CollectionId,
            Key = normalizedKey,
            Source = request.Source.Trim(),
            Status = request.Status.Trim(),
            Tags = string.Join('|', (request.Tags ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().ToLowerInvariant())),
            Context = request.Context?.Trim() ?? string.Empty,
            Notes = request.Notes?.Trim() ?? string.Empty,
            Description = request.Description?.Trim() ?? string.Empty,
            MaxLength = request.MaxLength,
            ContentType = request.ContentType?.Trim() ?? string.Empty
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

    public async Task<ContentItem> UpdateAsync(Guid id, string source, string status, string actorEmail, CancellationToken cancellationToken, string? description = null, int? maxLength = null, string? contentType = null)
    {
        var item = await db.ContentItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new ResourceNotFoundException(nameof(ContentItem), id);

        var previousSource = item.Source;
        var previousStatus = item.Status;

        item.Source = source.Trim();
        item.Status = status.Trim();

        if (description is not null) item.Description = description.Trim();
        if (maxLength.HasValue) item.MaxLength = maxLength.Value > 0 ? maxLength.Value : null;
        if (contentType is not null) item.ContentType = contentType.Trim();

        if (!string.Equals(previousSource, item.Source, StringComparison.Ordinal))
        {
            var affectedTasks = await db.ContentItemLanguageTasks
                .Where(x => x.ContentItemId == item.Id)
                .ToListAsync(cancellationToken);

            foreach (var task in affectedTasks)
            {
                if (task.Status == "done" || task.Status == "approved")
                {
                    task.PreviousApprovedTranslation = task.TranslationText;
                }

                task.IsOutdated = true;
                task.Status = "outdated";
            }
        }

        db.ContentItemRevisions.Add(new ContentItemRevision
        {
            ContentItemId = item.Id,
            ActorEmail = actorEmail,
            PreviousSource = previousSource,
            NewSource = item.Source,
            PreviousStatus = previousStatus,
            NewStatus = item.Status,
            DiffSummary = BuildDiffSummary(previousSource, item.Source, previousStatus, item.Status),
            EventType = "edited"
        });

        await db.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<IReadOnlyList<ContentItemRevision>> GetRevisionsAsync(Guid contentItemId, CancellationToken cancellationToken)
    {
        return await db.ContentItemRevisions
            .Where(x => x.ContentItemId == contentItemId)
            .OrderByDescending(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<object> CompareRevisionsAsync(Guid contentItemId, Guid leftRevisionId, Guid rightRevisionId, CancellationToken cancellationToken)
    {
        var left = await db.ContentItemRevisions.FirstOrDefaultAsync(x => x.Id == leftRevisionId && x.ContentItemId == contentItemId, cancellationToken)
            ?? throw new ResourceNotFoundException(nameof(ContentItemRevision), leftRevisionId);
        var right = await db.ContentItemRevisions.FirstOrDefaultAsync(x => x.Id == rightRevisionId && x.ContentItemId == contentItemId, cancellationToken)
            ?? throw new ResourceNotFoundException(nameof(ContentItemRevision), rightRevisionId);

        return new
        {
            left = new { left.Id, left.NewSource, left.NewStatus, left.ActorEmail, left.CreatedUtc },
            right = new { right.Id, right.NewSource, right.NewStatus, right.ActorEmail, right.CreatedUtc },
            delta = new
            {
                sourceChanged = !string.Equals(left.NewSource, right.NewSource, StringComparison.Ordinal),
                statusChanged = !string.Equals(left.NewStatus, right.NewStatus, StringComparison.Ordinal),
                sourceDelta = $"{left.NewSource} -> {right.NewSource}",
                statusDelta = $"{left.NewStatus} -> {right.NewStatus}"
            }
        };
    }

    public async Task<ContentItem> RollbackAsync(Guid contentItemId, Guid revisionId, string actorEmail, CancellationToken cancellationToken)
    {
        var item = await db.ContentItems.FirstOrDefaultAsync(x => x.Id == contentItemId, cancellationToken)
            ?? throw new ResourceNotFoundException(nameof(ContentItem), contentItemId);

        var revision = await db.ContentItemRevisions.FirstOrDefaultAsync(x => x.Id == revisionId && x.ContentItemId == contentItemId, cancellationToken)
            ?? throw new ResourceNotFoundException(nameof(ContentItemRevision), revisionId);

        var oldSource = item.Source;
        var oldStatus = item.Status;

        item.Source = revision.NewSource;
        item.Status = revision.NewStatus;

        db.ContentItemRevisions.Add(new ContentItemRevision
        {
            ContentItemId = item.Id,
            ActorEmail = actorEmail,
            PreviousSource = oldSource,
            NewSource = item.Source,
            PreviousStatus = oldStatus,
            NewStatus = item.Status,
            DiffSummary = BuildDiffSummary(oldSource, item.Source, oldStatus, item.Status),
            EventType = "rollback"
        });

        await db.SaveChangesAsync(cancellationToken);
        return item;
    }

    private static string BuildDiffSummary(string previousSource, string newSource, string previousStatus, string newStatus)
    {
        var sourceChanged = !string.Equals(previousSource, newSource, StringComparison.Ordinal);
        var statusChanged = !string.Equals(previousStatus, newStatus, StringComparison.Ordinal);

        var parts = new List<string>();
        if (sourceChanged) parts.Add("source changed");
        if (statusChanged) parts.Add($"status {previousStatus}->{newStatus}");
        if (parts.Count == 0) parts.Add("no-op edit");
        return string.Join(", ", parts);
    }

    public async Task<ContentItem> MoveAsync(Guid contentItemId, MoveContentItemRequest request, CancellationToken cancellationToken)
    {
        var item = await db.ContentItems.FirstOrDefaultAsync(x => x.Id == contentItemId, cancellationToken)
            ?? throw new ResourceNotFoundException(nameof(ContentItem), contentItemId);

        // If a target collection is specified, validate it belongs to the same project
        if (request.CollectionId.HasValue)
        {
            var collection = await db.ProjectCollections
                .FirstOrDefaultAsync(x => x.Id == request.CollectionId.Value, cancellationToken)
                ?? throw new ResourceNotFoundException(nameof(ProjectCollection), request.CollectionId.Value);

            if (collection.ProjectId != item.ProjectId)
            {
                throw new RequestValidationException(new Dictionary<string, string[]>
                {
                    ["collectionId"] = ["Target collection must belong to the same project."]
                });
            }
        }

        item.CollectionId = request.CollectionId;
        item.SortOrder = request.SortOrder;

        // Recompute key prefix on folder move while preserving leaf segment.
        var currentLeaf = GetLeafSegment(item.Key);
        item.Key = await BuildPrefixedKeyAsync(item.ProjectId, item.CollectionId, currentLeaf, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        return item;
    }

    private async Task<string> BuildPrefixedKeyAsync(Guid projectId, Guid? collectionId, string rawKey, CancellationToken cancellationToken)
    {
        var leaf = GetLeafSegment(rawKey);
        if (string.IsNullOrWhiteSpace(leaf))
        {
            throw new RequestValidationException(new Dictionary<string, string[]>
            {
                ["key"] = ["Key is required."]
            });
        }

        if (!collectionId.HasValue)
        {
            return leaf;
        }

        var prefix = await BuildCollectionPrefixAsync(projectId, collectionId.Value, cancellationToken);
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return leaf;
        }

        // Avoid double-prefixing when client already sent full key.
        return leaf.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase) ? leaf : $"{prefix}.{leaf}";
    }

    private async Task<string> BuildCollectionPrefixAsync(Guid projectId, Guid collectionId, CancellationToken cancellationToken)
    {
        var nodes = await db.ProjectCollections
            .Where(x => x.ProjectId == projectId)
            .Select(x => new { x.Id, x.ParentId, x.Name })
            .ToListAsync(cancellationToken);

        var map = nodes.ToDictionary(x => x.Id, x => (x.ParentId, x.Name));
        if (!map.TryGetValue(collectionId, out _))
        {
            throw new RequestValidationException(new Dictionary<string, string[]>
            {
                ["collectionId"] = ["Target collection not found for project."]
            });
        }

        var segments = new List<string>();
        var seen = new HashSet<Guid>();
        var current = collectionId;

        while (map.TryGetValue(current, out var node))
        {
            if (!seen.Add(current)) break;
            segments.Add(SanitizeSegment(node.Name));
            if (!node.ParentId.HasValue) break;
            current = node.ParentId.Value;
        }

        segments.Reverse();
        return string.Join('.', segments.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string GetLeafSegment(string key)
    {
        var cleaned = (key ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(cleaned)) return string.Empty;

        var normalized = InvalidKeyCharsRegex.Replace(cleaned, "_");
        normalized = Regex.Replace(normalized, "[.]{2,}", ".");
        normalized = Regex.Replace(normalized, "_{2,}", "_");
        normalized = normalized.Trim('.', '_', '-');

        if (string.IsNullOrWhiteSpace(normalized)) return string.Empty;

        var idx = normalized.LastIndexOf('.');
        return idx >= 0 && idx < normalized.Length - 1
            ? normalized[(idx + 1)..]
            : normalized;
    }

    private static string SanitizeSegment(string value)
    {
        var cleaned = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(cleaned)) return string.Empty;
        cleaned = cleaned.Replace(' ', '.').Replace('/', '.').Replace('\\', '.');
        cleaned = InvalidKeyCharsRegex.Replace(cleaned, "_");
        cleaned = Regex.Replace(cleaned, "[.]{2,}", ".");
        cleaned = cleaned.Trim('.', '_', '-');
        return cleaned;
    }
}
