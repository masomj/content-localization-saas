using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record LocaleImportFileMapping(string FileName, string LanguageCode, string? NamespacePrefix = null);

[ApiController]
[Route("api/locale-imports")]
public sealed class LocaleImportsController(AppDbContext db) : ControllerBase
{
    private static readonly Regex Bcp47Regex = new("^[A-Za-z]{2,3}(-[A-Za-z0-9]{2,8})*$", RegexOptions.Compiled);
    private static readonly Regex InvalidKeyCharsRegex = new("[^a-z0-9._-]", RegexOptions.Compiled);

    [HttpPost]
    [RequireAppRole(AppRole.Editor)]
    [RequestFormLimits(MultipartBodyLengthLimit = 25_000_000)]
    public async Task<IActionResult> Import(
        [FromQuery] Guid projectId,
        [FromForm] List<IFormFile> files,
        [FromForm] string sourceLanguageCode,
        [FromForm] string? mappingsJson,
        [FromForm] bool createMissingLanguages = true,
        CancellationToken cancellationToken = default)
    {
        if (projectId == Guid.Empty)
        {
            return BadRequest(new { error = "project_required" });
        }

        if (files is null || files.Count == 0)
        {
            return BadRequest(new { error = "files_required" });
        }

        var normalizedSourceLanguage = NormalizeLanguageCode(sourceLanguageCode);
        if (!IsValidLanguageCode(normalizedSourceLanguage))
        {
            return BadRequest(new { error = "valid_source_language_required" });
        }

        var project = await db.Projects.FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken);
        if (project is null)
        {
            return NotFound(new { error = "project_not_found" });
        }

        var fileMappings = ParseMappings(files, mappingsJson);
        if (fileMappings.Count == 0)
        {
            return BadRequest(new { error = "valid_file_mappings_required" });
        }

        var mappingErrors = ValidateMappings(files, fileMappings);
        if (mappingErrors.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(mappingErrors));
        }

        var warnings = new List<string>();
        var importByLanguage = new Dictionary<string, Dictionary<string, ImportedLeaf>>(StringComparer.OrdinalIgnoreCase);
        var fileNameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            if (!fileNameSet.Add(file.FileName))
            {
                warnings.Add($"Duplicate uploaded file name '{file.FileName}' detected. Using the last matching mapping.");
            }

            var mapping = fileMappings.First(x => string.Equals(x.FileName, file.FileName, StringComparison.OrdinalIgnoreCase));
            var languageCode = NormalizeLanguageCode(mapping.LanguageCode);
            var namespacePrefix = SanitizeNamespace(mapping.NamespacePrefix);

            if (!importByLanguage.TryGetValue(languageCode, out var entries))
            {
                entries = new Dictionary<string, ImportedLeaf>(StringComparer.OrdinalIgnoreCase);
                importByLanguage[languageCode] = entries;
            }

            await using var stream = file.OpenReadStream();

            JsonDocument document;
            try
            {
                document = await JsonDocument.ParseAsync(stream, new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                }, cancellationToken);
            }
            catch (JsonException ex)
            {
                return BadRequest(new { error = "invalid_json", file = file.FileName, detail = ex.Message });
            }

            using (document)
            {
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return BadRequest(new { error = "root_object_required", file = file.FileName });
                }

                CollectEntries(document.RootElement, namespacePrefix, entries, warnings, file.FileName);
            }
        }

        if (!importByLanguage.TryGetValue(normalizedSourceLanguage, out var sourceEntries) || sourceEntries.Count == 0)
        {
            return BadRequest(new { error = "source_language_file_missing", sourceLanguageCode = normalizedSourceLanguage });
        }

        var importedLanguageCodes = importByLanguage.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        var projectLanguages = await db.ProjectLanguages.Where(x => x.ProjectId == projectId).ToListAsync(cancellationToken);
        var existingItems = await db.ContentItems.Where(x => x.ProjectId == projectId).ToListAsync(cancellationToken);
        var existingTasks = await db.ContentItemLanguageTasks
            .Where(x => existingItems.Select(i => i.Id).Contains(x.ContentItemId))
            .ToListAsync(cancellationToken);

        var languageByCode = projectLanguages.ToDictionary(x => NormalizeLanguageCode(x.Bcp47Code), x => x, StringComparer.OrdinalIgnoreCase);
        var createdLanguages = new List<string>();

        if (createMissingLanguages)
        {
            foreach (var languageCode in importedLanguageCodes)
            {
                if (languageByCode.ContainsKey(languageCode)) continue;

                var newLanguage = new ProjectLanguage
                {
                    ProjectId = projectId,
                    Bcp47Code = languageCode,
                    IsSource = false,
                    IsActive = true
                };

                db.ProjectLanguages.Add(newLanguage);
                projectLanguages.Add(newLanguage);
                languageByCode[languageCode] = newLanguage;
                createdLanguages.Add(languageCode);
            }
        }

        if (!languageByCode.TryGetValue(normalizedSourceLanguage, out var sourceLanguageRow))
        {
            return BadRequest(new { error = "source_language_missing_from_project", languageCode = normalizedSourceLanguage });
        }

        foreach (var language in projectLanguages.Where(x => x.IsSource))
        {
            language.IsSource = false;
        }

        sourceLanguageRow.IsSource = true;
        sourceLanguageRow.IsActive = true;
        project.SourceLanguage = normalizedSourceLanguage;

        foreach (var languageCode in importedLanguageCodes)
        {
            if (languageByCode.TryGetValue(languageCode, out var language))
            {
                language.IsActive = true;
            }
        }

        var contentByKey = existingItems.ToDictionary(x => x.Key, x => x, StringComparer.OrdinalIgnoreCase);
        var taskByCompositeKey = existingTasks.ToDictionary(
            x => $"{x.ContentItemId:N}::{NormalizeLanguageCode(x.LanguageCode)}",
            x => x,
            StringComparer.OrdinalIgnoreCase);

        var sourceChangedItemIds = new HashSet<Guid>();
        var createdContentItems = 0;
        var updatedSourceItems = 0;
        var createdTranslationTasks = 0;
        var updatedTranslationTasks = 0;
        var skippedEntries = 0;
        var actorEmail = GetActorEmail();

        foreach (var (fullKey, sourceLeaf) in sourceEntries.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (!contentByKey.TryGetValue(fullKey, out var contentItem))
            {
                contentItem = new ContentItem
                {
                    ProjectId = projectId,
                    CollectionId = null,
                    Key = fullKey,
                    Source = sourceLeaf.Value,
                    Status = "draft",
                    Description = sourceLeaf.Description ?? string.Empty,
                    MaxLength = sourceLeaf.MaxLength,
                    ContentType = sourceLeaf.ContentType ?? string.Empty
                };

                db.ContentItems.Add(contentItem);
                existingItems.Add(contentItem);
                contentByKey[contentItem.Key] = contentItem;
                createdContentItems++;
                continue;
            }

            var sourceChanged = !string.Equals(contentItem.Source, sourceLeaf.Value, StringComparison.Ordinal);
            if (sourceChanged)
            {
                var previousSource = contentItem.Source;
                var previousStatus = contentItem.Status;
                contentItem.Source = sourceLeaf.Value;
                updatedSourceItems++;
                sourceChangedItemIds.Add(contentItem.Id);

                var affectedTasks = existingTasks.Where(x => x.ContentItemId == contentItem.Id).ToList();
                foreach (var task in affectedTasks)
                {
                    if (task.Status == "done" || task.Status == "approved")
                    {
                        task.PreviousApprovedTranslation = task.TranslationText;
                    }

                    task.IsOutdated = true;
                    task.Status = "outdated";
                }

                db.ContentItemRevisions.Add(new ContentItemRevision
                {
                    ContentItemId = contentItem.Id,
                    ActorEmail = actorEmail,
                    PreviousSource = previousSource,
                    NewSource = contentItem.Source,
                    PreviousStatus = previousStatus,
                    NewStatus = contentItem.Status,
                    DiffSummary = "source changed via locale import",
                    EventType = "edited"
                });
            }

            if (!string.IsNullOrWhiteSpace(sourceLeaf.Description))
            {
                contentItem.Description = sourceLeaf.Description!;
            }

            if (sourceLeaf.MaxLength.HasValue)
            {
                contentItem.MaxLength = sourceLeaf.MaxLength;
            }

            if (!string.IsNullOrWhiteSpace(sourceLeaf.ContentType))
            {
                contentItem.ContentType = sourceLeaf.ContentType!;
            }
        }

        foreach (var (languageCode, entries) in importByLanguage.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (string.Equals(languageCode, normalizedSourceLanguage, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!languageByCode.TryGetValue(languageCode, out var language))
            {
                warnings.Add($"Skipped language '{languageCode}' because it is not configured on the project.");
                skippedEntries += entries.Count;
                continue;
            }

            language.IsActive = true;

            foreach (var (fullKey, importedLeaf) in entries.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (!contentByKey.TryGetValue(fullKey, out var contentItem))
                {
                    warnings.Add($"Skipped translation key '{fullKey}' for '{languageCode}' because no source entry exists.");
                    skippedEntries++;
                    continue;
                }

                var compositeKey = $"{contentItem.Id:N}::{languageCode}";
                if (!taskByCompositeKey.TryGetValue(compositeKey, out var existingTask))
                {
                    existingTask = new ContentItemLanguageTask
                    {
                        ContentItemId = contentItem.Id,
                        LanguageCode = languageCode,
                        TranslationText = importedLeaf.Value,
                        Status = "draft",
                        IsOutdated = false,
                        RequiresReview = false
                    };

                    db.ContentItemLanguageTasks.Add(existingTask);
                    existingTasks.Add(existingTask);
                    taskByCompositeKey[compositeKey] = existingTask;
                    createdTranslationTasks++;
                }
                else
                {
                    var previousTranslation = existingTask.TranslationText;
                    var translationChanged = !string.Equals(previousTranslation, importedLeaf.Value, StringComparison.Ordinal);
                    var sourceChanged = sourceChangedItemIds.Contains(contentItem.Id);

                    if (translationChanged)
                    {
                        if (existingTask.Status == "done" || existingTask.Status == "approved")
                        {
                            existingTask.PreviousApprovedTranslation = previousTranslation;
                        }

                        existingTask.TranslationText = importedLeaf.Value;
                        updatedTranslationTasks++;
                    }

                    existingTask.IsOutdated = false;
                    existingTask.RequiresReview = false;

                    if (translationChanged || sourceChanged)
                    {
                        existingTask.Status = "draft";
                    }
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            projectId,
            sourceLanguageCode = normalizedSourceLanguage,
            importedLanguages = importedLanguageCodes,
            createdLanguages,
            createdContentItems,
            updatedSourceItems,
            createdTranslationTasks,
            updatedTranslationTasks,
            skippedEntries,
            warnings = warnings.Distinct(StringComparer.OrdinalIgnoreCase).Take(100).ToArray()
        });
    }

    private static List<LocaleImportFileMapping> ParseMappings(IReadOnlyList<IFormFile> files, string? mappingsJson)
    {
        if (!string.IsNullOrWhiteSpace(mappingsJson))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<LocaleImportFileMapping>>(mappingsJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (parsed is { Count: > 0 })
                {
                    return parsed;
                }
            }
            catch (JsonException)
            {
                return [];
            }
        }

        return files.Select(file => InferMappingFromFileName(file.FileName)).ToList();
    }

    private static Dictionary<string, string[]> ValidateMappings(IReadOnlyList<IFormFile> files, IReadOnlyList<LocaleImportFileMapping> mappings)
    {
        var errors = new Dictionary<string, string[]>();
        var fileNames = files.Select(x => x.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in mappings)
        {
            if (string.IsNullOrWhiteSpace(mapping.FileName) || !fileNames.Contains(mapping.FileName))
            {
                errors[$"mapping:{mapping.FileName}"] = ["Every mapping must reference an uploaded file."];
                continue;
            }

            var languageCode = NormalizeLanguageCode(mapping.LanguageCode);
            if (!IsValidLanguageCode(languageCode))
            {
                errors[$"mapping:{mapping.FileName}:languageCode"] = ["A valid BCP-47 language code is required for every uploaded file."];
            }
        }

        foreach (var file in files)
        {
            if (!mappings.Any(x => string.Equals(x.FileName, file.FileName, StringComparison.OrdinalIgnoreCase)))
            {
                errors[$"mapping:{file.FileName}"] = ["Every uploaded file must have a mapping."];
            }
        }

        return errors;
    }

    private static LocaleImportFileMapping InferMappingFromFileName(string fileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName) ?? fileName;
        var parts = baseName.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return new LocaleImportFileMapping(fileName, string.Empty, null);
        }

        var languageIndex = Array.FindLastIndex(parts, part => IsValidLanguageCode(NormalizeLanguageCode(part)));
        if (languageIndex < 0)
        {
            return new LocaleImportFileMapping(fileName, string.Empty, null);
        }

        var languageCode = NormalizeLanguageCode(parts[languageIndex]);
        var namespacePrefix = languageIndex > 0
            ? SanitizeNamespace(string.Join('.', parts.Take(languageIndex)))
            : null;

        return new LocaleImportFileMapping(fileName, languageCode, namespacePrefix);
    }

    private static void CollectEntries(
        JsonElement element,
        string? namespacePrefix,
        Dictionary<string, ImportedLeaf> entries,
        List<string> warnings,
        string fileName)
    {
        foreach (var property in element.EnumerateObject())
        {
            var keySegment = SanitizeKeySegment(property.Name);
            if (string.IsNullOrWhiteSpace(keySegment))
            {
                warnings.Add($"Skipped invalid key segment '{property.Name}' in '{fileName}'.");
                continue;
            }

            var fullKey = string.IsNullOrWhiteSpace(namespacePrefix)
                ? keySegment
                : $"{namespacePrefix}.{keySegment}";

            CollectValue(property.Value, fullKey, entries, warnings, fileName);
        }
    }

    private static void CollectValue(
        JsonElement element,
        string currentPath,
        Dictionary<string, ImportedLeaf> entries,
        List<string> warnings,
        string fileName)
    {
        if (TryReadLeafObject(element, out var leaf))
        {
            AddOrReplaceEntry(entries, currentPath, leaf, warnings, fileName);
            return;
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var keySegment = SanitizeKeySegment(property.Name);
                    if (string.IsNullOrWhiteSpace(keySegment))
                    {
                        warnings.Add($"Skipped invalid key segment '{property.Name}' in '{fileName}'.");
                        continue;
                    }

                    CollectValue(property.Value, $"{currentPath}.{keySegment}", entries, warnings, fileName);
                }
                break;

            case JsonValueKind.String:
                AddOrReplaceEntry(entries, currentPath, new ImportedLeaf(element.GetString() ?? string.Empty), warnings, fileName);
                break;

            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                AddOrReplaceEntry(entries, currentPath, new ImportedLeaf(ReadPrimitiveAsString(element)), warnings, fileName);
                warnings.Add($"Imported primitive value for '{currentPath}' from '{fileName}' as text.");
                break;

            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                warnings.Add($"Skipped null value for '{currentPath}' in '{fileName}'.");
                break;

            case JsonValueKind.Array:
                warnings.Add($"Skipped array value for '{currentPath}' in '{fileName}' because InterCopy imports string keys only.");
                break;
        }
    }

    private static bool TryReadLeafObject(JsonElement element, out ImportedLeaf leaf)
    {
        leaf = new ImportedLeaf(string.Empty);
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty("value", out var valueProperty))
        {
            return false;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (property.NameEquals("value") || property.NameEquals("description") || property.NameEquals("maxLength") || property.NameEquals("contentType"))
            {
                continue;
            }

            return false;
        }

        var value = valueProperty.ValueKind switch
        {
            JsonValueKind.String => valueProperty.GetString() ?? string.Empty,
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => ReadPrimitiveAsString(valueProperty),
            _ => string.Empty
        };

        if (valueProperty.ValueKind is not (JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False))
        {
            return false;
        }

        string? description = null;
        if (element.TryGetProperty("description", out var descriptionProperty) && descriptionProperty.ValueKind == JsonValueKind.String)
        {
            description = descriptionProperty.GetString()?.Trim();
        }

        int? maxLength = null;
        if (element.TryGetProperty("maxLength", out var maxLengthProperty) && maxLengthProperty.ValueKind == JsonValueKind.Number && maxLengthProperty.TryGetInt32(out var parsedMaxLength) && parsedMaxLength > 0)
        {
            maxLength = parsedMaxLength;
        }

        string? contentType = null;
        if (element.TryGetProperty("contentType", out var contentTypeProperty) && contentTypeProperty.ValueKind == JsonValueKind.String)
        {
            contentType = contentTypeProperty.GetString()?.Trim();
        }

        leaf = new ImportedLeaf(value, description, maxLength, contentType);
        return true;
    }

    private static void AddOrReplaceEntry(
        Dictionary<string, ImportedLeaf> entries,
        string key,
        ImportedLeaf leaf,
        List<string> warnings,
        string fileName)
    {
        if (entries.ContainsKey(key))
        {
            warnings.Add($"Duplicate imported key '{key}' encountered in '{fileName}'. The last value was used.");
        }

        entries[key] = leaf with { Value = leaf.Value.Trim() };
    }

    private static string ReadPrimitiveAsString(JsonElement element)
        => element.ValueKind switch
        {
            JsonValueKind.Number => element.ToString(),
            JsonValueKind.True => bool.TrueString.ToLowerInvariant(),
            JsonValueKind.False => bool.FalseString.ToLowerInvariant(),
            _ => string.Empty
        };

    private static string NormalizeLanguageCode(string value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return string.Empty;

        var parts = trimmed.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = i switch
            {
                0 => parts[i].ToLowerInvariant(),
                _ when parts[i].Length is 2 or 3 => parts[i].ToUpperInvariant(),
                _ => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(parts[i].ToLowerInvariant())
            };
        }

        return string.Join('-', parts);
    }

    private static bool IsValidLanguageCode(string value)
        => !string.IsNullOrWhiteSpace(value) && Bcp47Regex.IsMatch(value);

    private static string? SanitizeNamespace(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var segments = value
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(SanitizeKeySegment)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        return segments.Length == 0 ? null : string.Join('.', segments);
    }

    private static string SanitizeKeySegment(string value)
    {
        var cleaned = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(cleaned)) return string.Empty;

        cleaned = cleaned.Replace(' ', '_').Replace('/', '.').Replace('\\', '.');
        cleaned = InvalidKeyCharsRegex.Replace(cleaned, "_");
        cleaned = Regex.Replace(cleaned, "[.]{2,}", ".");
        cleaned = Regex.Replace(cleaned, "_{2,}", "_");
        return cleaned.Trim('.', '_', '-');
    }

    private string GetActorEmail()
        => User.Claims.FirstOrDefault(x =>
                x.Type == ClaimTypes.Email ||
                x.Type == "email" ||
                x.Type == "preferred_username" ||
                x.Type == ClaimTypes.Name)
            ?.Value
            ?? "locale-import";

    private sealed record ImportedLeaf(string Value, string? Description = null, int? MaxLength = null, string? ContentType = null);
}
