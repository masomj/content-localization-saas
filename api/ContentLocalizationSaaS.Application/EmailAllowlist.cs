namespace ContentLocalizationSaaS.Application;

public sealed class AccessAllowlistOptions
{
    public const string SectionName = "AccessAllowlist";

    /// <summary>
    /// Comma, semicolon or whitespace separated list of allowed emails and/or domains.
    /// Entries starting with "@" (or "*@") allow a whole domain, e.g. "@vanguard.dev".
    /// Empty means the allowlist is switched off and everyone is allowed.
    /// </summary>
    public string Emails { get; set; } = string.Empty;
}

/// <summary>
/// Decides whether an authenticated user's email may use this environment.
/// Used to keep the shared dev environment invite-only.
/// </summary>
public sealed class EmailAllowlist
{
    private static readonly char[] Separators = [',', ';', ' ', '\n', '\r', '\t'];

    private readonly HashSet<string> _emails = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _domains = new(StringComparer.OrdinalIgnoreCase);

    public EmailAllowlist(string? rawEntries)
    {
        foreach (var raw in (rawEntries ?? string.Empty).Split(Separators, StringSplitOptions.RemoveEmptyEntries))
        {
            var entry = raw.Trim().ToLowerInvariant();
            if (entry.StartsWith("*@", StringComparison.Ordinal)) entry = entry[1..];

            if (entry.StartsWith('@'))
            {
                if (entry.Length > 1) _domains.Add(entry[1..]);
            }
            else if (entry.Contains('@'))
            {
                _emails.Add(entry);
            }
        }
    }

    /// <summary>True when at least one entry is configured.</summary>
    public bool IsEnabled => _emails.Count > 0 || _domains.Count > 0;

    public bool IsAllowed(string? email)
    {
        if (!IsEnabled) return true;
        if (string.IsNullOrWhiteSpace(email)) return false;

        var normalized = email.Trim().ToLowerInvariant();
        if (_emails.Contains(normalized)) return true;

        var at = normalized.LastIndexOf('@');
        return at > 0 && at < normalized.Length - 1 && _domains.Contains(normalized[(at + 1)..]);
    }
}
