using ContentLocalizationSaaS.Application;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Infrastructure.Services;

public static class EnvironmentVariantQueries
{
    /// <summary>
    /// Loads the variant index for an export. Returns an empty index when no environment
    /// (or a production alias) is requested, so callers can always use it unconditionally.
    /// </summary>
    public static async Task<EnvironmentVariantIndex> LoadEnvironmentVariantsAsync(
        this AppDbContext db,
        Guid projectId,
        string? environment,
        CancellationToken cancellationToken)
    {
        var normalized = CopyEnvironments.NormalizeForExport(environment);
        if (normalized is null) return EnvironmentVariantIndex.Empty;

        var rows = await db.ContentEnvironmentOverrides
            .AsNoTracking()
            .Where(x => x.ProjectId == projectId && x.Environment == normalized)
            .Select(x => new { x.ContentItemId, x.LanguageCode, x.Value })
            .ToListAsync(cancellationToken);

        return new EnvironmentVariantIndex(rows.Select(r => (r.ContentItemId, r.LanguageCode, r.Value)));
    }
}
