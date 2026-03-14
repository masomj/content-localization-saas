using System.Text;
using System.Text.Json;

namespace ContentLocalizationSaaS.Cli;

internal static class Program
{
    private const int ExitOk = 0;
    private const int ExitUsage = 2;
    private const int ExitUnauthorized = 10;
    private const int ExitForbidden = 11;
    private const int ExitNotFound = 12;
    private const int ExitRateLimited = 13;
    private const int ExitServerError = 20;
    private const int ExitNetworkError = 21;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintUsage();
            return ExitUsage;
        }

        try
        {
            return args[0].ToLowerInvariant() switch
            {
                "configure" => await Configure(args.Skip(1).ToArray()),
                "pull" => await Pull(args.Skip(1).ToArray()),
                _ => ExitUsage
            };
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine($"Network error: {ex.Message}");
            return ExitNetworkError;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unexpected error: {ex.Message}");
            return ExitServerError;
        }
    }

    private static bool IsHelp(string arg) => arg is "-h" or "--help" or "help";

    private static void PrintUsage()
    {
        Console.WriteLine("content-localization-cli");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  configure --base-url <url> --api-token <token>");
        Console.WriteLine("  pull --project-id <guid> [--language <code>] [--namespace <ns>] [--out <file>]");
        Console.WriteLine();
        Console.WriteLine("Environment fallback:");
        Console.WriteLine("  CLSAAS_BASE_URL, CLSAAS_API_TOKEN");
    }

    private static async Task<int> Configure(string[] args)
    {
        var options = ParseArgs(args);
        if (!options.TryGetValue("base-url", out var baseUrl) || !options.TryGetValue("api-token", out var apiToken))
        {
            Console.Error.WriteLine("configure requires --base-url and --api-token");
            return ExitUsage;
        }

        var cfg = new CliConfig(baseUrl.TrimEnd('/'), apiToken);
        var path = GetConfigPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(cfg, JsonOptions));
        Console.WriteLine($"Saved config: {path}");
        return ExitOk;
    }

    private static async Task<int> Pull(string[] args)
    {
        var options = ParseArgs(args);
        if (!options.TryGetValue("project-id", out var projectIdRaw) || !Guid.TryParse(projectIdRaw, out var projectId))
        {
            Console.Error.WriteLine("pull requires valid --project-id <guid>");
            return ExitUsage;
        }

        var cfg = await LoadConfig();
        if (cfg is null)
        {
            Console.Error.WriteLine("No config found. Run configure first or set CLSAAS_BASE_URL + CLSAAS_API_TOKEN.");
            return ExitUsage;
        }

        var language = options.GetValueOrDefault("language");
        var ns = options.GetValueOrDefault("namespace");
        var outFile = options.GetValueOrDefault("out") ?? BuildDefaultOutputFile(projectId, language);

        var query = new List<string> { $"projectId={projectId}" };
        if (!string.IsNullOrWhiteSpace(language)) query.Add($"language={Uri.EscapeDataString(language)}");
        if (!string.IsNullOrWhiteSpace(ns)) query.Add($"namespace={Uri.EscapeDataString(ns)}");

        var url = $"{cfg.BaseUrl}/api/integration/exports/bundle?{string.Join("&", query)}";

        using var http = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Api-Token", cfg.ApiToken);
        request.Headers.Add("X-Request-Id", Guid.NewGuid().ToString("N"));

        using var response = await http.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Console.Error.WriteLine(body);
            return response.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized => ExitUnauthorized,
                System.Net.HttpStatusCode.Forbidden => ExitForbidden,
                System.Net.HttpStatusCode.NotFound => ExitNotFound,
                (System.Net.HttpStatusCode)429 => ExitRateLimited,
                _ => ExitServerError
            };
        }

        var exportResponse = JsonSerializer.Deserialize<ExportResponse>(body);
        if (exportResponse is null)
        {
            Console.Error.WriteLine("Could not parse export response.");
            return ExitServerError;
        }

        var json = DecodeSignedBundle(exportResponse.SignedBundleUrl);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outFile))!);
        await File.WriteAllTextAsync(outFile, json, Encoding.UTF8);

        Console.WriteLine($"Export written: {outFile}");
        return ExitOk;
    }

    private static string DecodeSignedBundle(string signedBundleUrl)
    {
        const string prefix = "data:application/json;base64,";
        if (!signedBundleUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Unexpected signedBundleUrl format.");

        var base64 = signedBundleUrl[prefix.Length..];
        var bytes = Convert.FromBase64String(base64);
        return Encoding.UTF8.GetString(bytes);
    }

    private static string BuildDefaultOutputFile(Guid projectId, string? language)
    {
        var lang = string.IsNullOrWhiteSpace(language) ? "source" : language;
        return Path.Combine("exports", $"{projectId}_{lang}.json");
    }

    private static async Task<CliConfig?> LoadConfig()
    {
        var baseUrl = Environment.GetEnvironmentVariable("CLSAAS_BASE_URL");
        var apiToken = Environment.GetEnvironmentVariable("CLSAAS_API_TOKEN");
        if (!string.IsNullOrWhiteSpace(baseUrl) && !string.IsNullOrWhiteSpace(apiToken))
            return new CliConfig(baseUrl.TrimEnd('/'), apiToken);

        var path = GetConfigPath();
        if (!File.Exists(path)) return null;

        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<CliConfig>(json);
    }

    private static string GetConfigPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "content-localization-cli", "config.json");
    }

    private static Dictionary<string, string> ParseArgs(string[] args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            var current = args[i];
            if (!current.StartsWith("--", StringComparison.Ordinal)) continue;

            var key = current[2..];
            var value = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal)
                ? args[++i]
                : "true";
            result[key] = value;
        }

        return result;
    }

    private sealed record CliConfig(string BaseUrl, string ApiToken);

    private sealed record ExportResponse(string SignedBundleUrl);
}
