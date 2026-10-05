using System.Text.RegularExpressions;

namespace ContentLocalizationSaaS.Application;

/// <summary>
/// Rules for environment-tagged copy (#57). Production copy is the content item itself;
/// any other environment name can carry variant values that only appear in its exports.
/// </summary>
public static partial class CopyEnvironments
{
    private static readonly HashSet<string> ProductionAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        "prod", "production", "live"
    };

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,31}$")]
    private static partial Regex SlugPattern();

    /// <summary>
    /// Normalises an environment name from a query string or request body.
    /// Returns null for "no environment" or a production alias, meaning: serve production copy.
    /// </summary>
    public static string? NormalizeForExport(string? environment)
    {
        if (string.IsNullOrWhiteSpace(environment)) return null;
        var normalized = environment.Trim().ToLowerInvariant();
        return ProductionAliases.Contains(normalized) ? null : normalized;
    }

    /// <summary>
    /// Validates an environment name for storing a variant. Production aliases are rejected
    /// because production copy is edited on the content item directly.
    /// </summary>
    public static bool TryNormalizeForVariant(string? environment, out string normalized, out string? error)
    {
        normalized = (environment ?? string.Empty).Trim().ToLowerInvariant();
        error = null;

        if (normalized.Length == 0)
        {
            error = "Environment is required.";
            return false;
        }

        if (ProductionAliases.Contains(normalized))
        {
            error = "Production copy is the content item's own text. Edit it directly rather than adding a production variant.";
            return false;
        }

        if (!SlugPattern().IsMatch(normalized))
        {
            error = "Environment must be 1-32 characters: lowercase letters, numbers and hyphens, starting with a letter or number.";
            return false;
        }

        return true;
    }
}

/// <summary>
/// Looks up variant values for one environment. Keyed by content item ID and language
/// (empty language = source text).
/// </summary>
public sealed class EnvironmentVariantIndex
{
    public static readonly EnvironmentVariantIndex Empty = new([]);

    private readonly Dictionary<(Guid ItemId, string Language), string> _values;

    public EnvironmentVariantIndex(IEnumerable<(Guid ContentItemId, string LanguageCode, string Value)> variants)
    {
        _values = new Dictionary<(Guid, string), string>();
        foreach (var (itemId, language, value) in variants)
        {
            _values[(itemId, (language ?? string.Empty).ToLowerInvariant())] = value;
        }
    }

    public bool IsEmpty => _values.Count == 0;

    /// <summary>Source text for the environment: the variant if one exists, otherwise production source.</summary>
    public string ResolveSource(Guid contentItemId, string productionSource)
        => _values.TryGetValue((contentItemId, string.Empty), out var v) ? v : productionSource;

    /// <summary>
    /// Target-language text for the environment. Order: language variant, production translation,
    /// then the environment's source (so a source-only variant still shows in untranslated languages).
    /// </summary>
    public string ResolveTranslation(Guid contentItemId, string languageCode, string? productionTranslation, string productionSource)
    {
        if (_values.TryGetValue((contentItemId, languageCode.ToLowerInvariant()), out var variant)) return variant;
        if (!string.IsNullOrWhiteSpace(productionTranslation)) return productionTranslation;
        return ResolveSource(contentItemId, productionSource);
    }

    /// <summary>True if a variant exists for this exact language (empty = source).</summary>
    public bool HasVariant(Guid contentItemId, string? languageCode)
        => _values.ContainsKey((contentItemId, (languageCode ?? string.Empty).ToLowerInvariant()));
}
