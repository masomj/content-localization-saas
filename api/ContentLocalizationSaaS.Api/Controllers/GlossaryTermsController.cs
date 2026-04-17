using System.Globalization;
using System.Text;
using System.Xml.Linq;
using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Application;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

[ApiController]
[Route("api/glossaries/{glossaryId:guid}/terms")]
public sealed class GlossaryTermsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid glossaryId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string? q = null, CancellationToken ct = default)
    {
        var query = db.GlossaryTerms.Where(t => t.GlossaryId == glossaryId);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(t => EF.Functions.ILike(t.SourceTerm, $"%{q}%"));

        var total = await query.CountAsync(ct);

        var terms = await query
            .OrderBy(t => t.SourceTerm)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var termIds = terms.Select(t => t.Id).ToList();
        var translations = await db.GlossaryTermTranslations
            .Where(tt => termIds.Contains(tt.GlossaryTermId))
            .ToListAsync(ct);

        var translationsByTerm = translations.GroupBy(tt => tt.GlossaryTermId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var items = terms.Select(t => new
        {
            t.Id,
            t.GlossaryId,
            t.SourceTerm,
            t.Definition,
            t.IsForbidden,
            t.ForbiddenReplacement,
            t.CaseSensitive,
            Translations = translationsByTerm.GetValueOrDefault(t.Id, []).Select(tt => new
            {
                tt.Id,
                tt.LanguageCode,
                tt.TranslatedTerm,
            }),
            t.CreatedUtc,
            t.UpdatedUtc,
        });

        return Ok(new { items, total });
    }

    [HttpPost]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Create(Guid glossaryId, [FromBody] UpsertTermRequest request, CancellationToken ct)
    {
        var term = new GlossaryTerm
        {
            GlossaryId = glossaryId,
            SourceTerm = request.SourceTerm,
            Definition = request.Definition ?? string.Empty,
            CaseSensitive = request.CaseSensitive,
            IsForbidden = request.IsForbidden,
            ForbiddenReplacement = request.ForbiddenReplacement ?? string.Empty,
        };

        db.GlossaryTerms.Add(term);

        if (request.Translations is { Count: > 0 })
        {
            foreach (var tr in request.Translations)
            {
                db.GlossaryTermTranslations.Add(new GlossaryTermTranslation
                {
                    GlossaryTermId = term.Id,
                    LanguageCode = tr.LanguageCode,
                    TranslatedTerm = tr.TranslatedTerm,
                });
            }
        }

        await db.SaveChangesAsync(ct);

        var translations = await db.GlossaryTermTranslations
            .Where(tt => tt.GlossaryTermId == term.Id)
            .ToListAsync(ct);

        return Ok(new
        {
            term.Id,
            term.GlossaryId,
            term.SourceTerm,
            term.Definition,
            term.IsForbidden,
            term.ForbiddenReplacement,
            term.CaseSensitive,
            Translations = translations.Select(tt => new { tt.Id, tt.LanguageCode, tt.TranslatedTerm }),
            term.CreatedUtc,
            term.UpdatedUtc,
        });
    }

    [HttpPut("{termId:guid}")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Update(Guid glossaryId, Guid termId, [FromBody] UpsertTermRequest request, CancellationToken ct)
    {
        var term = await db.GlossaryTerms.FirstOrDefaultAsync(t => t.Id == termId && t.GlossaryId == glossaryId, ct);
        if (term is null) return NotFound();

        term.SourceTerm = request.SourceTerm;
        term.Definition = request.Definition ?? string.Empty;
        term.CaseSensitive = request.CaseSensitive;
        term.IsForbidden = request.IsForbidden;
        term.ForbiddenReplacement = request.ForbiddenReplacement ?? string.Empty;
        term.UpdatedUtc = DateTime.UtcNow;

        // Upsert translations
        if (request.Translations is not null)
        {
            var existing = await db.GlossaryTermTranslations
                .Where(tt => tt.GlossaryTermId == termId)
                .ToListAsync(ct);

            var incoming = request.Translations.ToDictionary(t => t.LanguageCode);
            var toRemove = existing.Where(e => !incoming.ContainsKey(e.LanguageCode)).ToList();
            db.GlossaryTermTranslations.RemoveRange(toRemove);

            foreach (var tr in request.Translations)
            {
                var ex = existing.FirstOrDefault(e => e.LanguageCode == tr.LanguageCode);
                if (ex is not null)
                {
                    ex.TranslatedTerm = tr.TranslatedTerm;
                }
                else
                {
                    db.GlossaryTermTranslations.Add(new GlossaryTermTranslation
                    {
                        GlossaryTermId = termId,
                        LanguageCode = tr.LanguageCode,
                        TranslatedTerm = tr.TranslatedTerm,
                    });
                }
            }
        }

        await db.SaveChangesAsync(ct);

        var translations = await db.GlossaryTermTranslations
            .Where(tt => tt.GlossaryTermId == term.Id)
            .ToListAsync(ct);

        return Ok(new
        {
            term.Id,
            term.GlossaryId,
            term.SourceTerm,
            term.Definition,
            term.IsForbidden,
            term.ForbiddenReplacement,
            term.CaseSensitive,
            Translations = translations.Select(tt => new { tt.Id, tt.LanguageCode, tt.TranslatedTerm }),
            term.CreatedUtc,
            term.UpdatedUtc,
        });
    }

    [HttpDelete("{termId:guid}")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Delete(Guid glossaryId, Guid termId, CancellationToken ct)
    {
        var term = await db.GlossaryTerms.FirstOrDefaultAsync(t => t.Id == termId && t.GlossaryId == glossaryId, ct);
        if (term is null) return NotFound();

        await db.GlossaryTermTranslations.Where(tt => tt.GlossaryTermId == termId).ExecuteDeleteAsync(ct);
        db.GlossaryTerms.Remove(term);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("import/csv")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> ImportCsv(Guid glossaryId, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("File is required.");

        using var reader = new StreamReader(file.OpenReadStream());
        var headerLine = await reader.ReadLineAsync(ct);
        if (headerLine is null) return BadRequest("Empty CSV file.");

        var headers = headerLine.Split(',').Select(h => h.Trim().Trim('"')).ToList();
        // Expected: sourceTerm, definition, lang1, lang2, ...
        var langCodes = headers.Skip(2).ToList();

        var imported = 0;
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            var cols = ParseCsvLine(line);
            if (cols.Count < 1) continue;

            var sourceTerm = cols[0];
            if (string.IsNullOrWhiteSpace(sourceTerm)) continue;

            var definition = cols.Count > 1 ? cols[1] : string.Empty;

            // Upsert: find existing term by sourceTerm in this glossary
            var existing = await db.GlossaryTerms
                .FirstOrDefaultAsync(t => t.GlossaryId == glossaryId && t.SourceTerm == sourceTerm, ct);

            if (existing is null)
            {
                existing = new GlossaryTerm
                {
                    GlossaryId = glossaryId,
                    SourceTerm = sourceTerm,
                    Definition = definition,
                };
                db.GlossaryTerms.Add(existing);
                await db.SaveChangesAsync(ct);
            }
            else
            {
                existing.Definition = definition;
                existing.UpdatedUtc = DateTime.UtcNow;
            }

            // Upsert translations
            for (var i = 0; i < langCodes.Count && i + 2 < cols.Count; i++)
            {
                var langCode = langCodes[i];
                var translated = cols[i + 2];
                if (string.IsNullOrWhiteSpace(translated)) continue;

                var exTrans = await db.GlossaryTermTranslations
                    .FirstOrDefaultAsync(tt => tt.GlossaryTermId == existing.Id && tt.LanguageCode == langCode, ct);

                if (exTrans is null)
                {
                    db.GlossaryTermTranslations.Add(new GlossaryTermTranslation
                    {
                        GlossaryTermId = existing.Id,
                        LanguageCode = langCode,
                        TranslatedTerm = translated,
                    });
                }
                else
                {
                    exTrans.TranslatedTerm = translated;
                }
            }

            imported++;
        }

        await db.SaveChangesAsync(ct);
        return Ok(new { imported });
    }

    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv(Guid glossaryId, CancellationToken ct)
    {
        var terms = await db.GlossaryTerms
            .Where(t => t.GlossaryId == glossaryId)
            .OrderBy(t => t.SourceTerm)
            .ToListAsync(ct);

        var termIds = terms.Select(t => t.Id).ToList();
        var translations = await db.GlossaryTermTranslations
            .Where(tt => termIds.Contains(tt.GlossaryTermId))
            .ToListAsync(ct);

        var allLangs = translations.Select(tt => tt.LanguageCode).Distinct().OrderBy(l => l).ToList();
        var translationMap = translations.GroupBy(tt => tt.GlossaryTermId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(tt => tt.LanguageCode, tt => tt.TranslatedTerm));

        var sb = new StringBuilder();
        sb.AppendLine($"sourceTerm,definition,{string.Join(",", allLangs)}");

        foreach (var term in terms)
        {
            var langValues = allLangs.Select(l =>
                translationMap.GetValueOrDefault(term.Id)?.GetValueOrDefault(l, "") ?? "");
            sb.AppendLine($"{CsvEscape(term.SourceTerm)},{CsvEscape(term.Definition)},{string.Join(",", langValues.Select(CsvEscape))}");
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "glossary.csv");
    }

    [HttpPost("import/tbx")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> ImportTbx(Guid glossaryId, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("File is required.");

        XDocument doc;
        using (var stream = file.OpenReadStream())
        {
            doc = await XDocument.LoadAsync(stream, LoadOptions.None, ct);
        }

        var imported = 0;
        var entries = doc.Descendants("termEntry").Concat(doc.Descendants("conceptEntry"));

        foreach (var entry in entries)
        {
            string? sourceTerm = null;
            var langTerms = new Dictionary<string, string>();

            foreach (var langSet in entry.Descendants("langSet"))
            {
                var lang = langSet.Attribute(XNamespace.Xml + "lang")?.Value ?? langSet.Attribute("lang")?.Value;
                var termText = langSet.Descendants("term").FirstOrDefault()?.Value;
                if (lang is null || termText is null) continue;

                if (sourceTerm is null)
                    sourceTerm = termText;
                else
                    langTerms[lang] = termText;
            }

            if (sourceTerm is null) continue;

            var existing = await db.GlossaryTerms
                .FirstOrDefaultAsync(t => t.GlossaryId == glossaryId && t.SourceTerm == sourceTerm, ct);

            if (existing is null)
            {
                existing = new GlossaryTerm
                {
                    GlossaryId = glossaryId,
                    SourceTerm = sourceTerm,
                };
                db.GlossaryTerms.Add(existing);
                await db.SaveChangesAsync(ct);
            }

            foreach (var (lang, translated) in langTerms)
            {
                var exTrans = await db.GlossaryTermTranslations
                    .FirstOrDefaultAsync(tt => tt.GlossaryTermId == existing.Id && tt.LanguageCode == lang, ct);

                if (exTrans is null)
                {
                    db.GlossaryTermTranslations.Add(new GlossaryTermTranslation
                    {
                        GlossaryTermId = existing.Id,
                        LanguageCode = lang,
                        TranslatedTerm = translated,
                    });
                }
                else
                {
                    exTrans.TranslatedTerm = translated;
                }
            }

            imported++;
        }

        await db.SaveChangesAsync(ct);
        return Ok(new { imported });
    }

    [HttpGet("export/tbx")]
    public async Task<IActionResult> ExportTbx(Guid glossaryId, CancellationToken ct)
    {
        var glossary = await db.Glossaries.FirstOrDefaultAsync(g => g.Id == glossaryId, ct);
        if (glossary is null) return NotFound();

        var terms = await db.GlossaryTerms
            .Where(t => t.GlossaryId == glossaryId)
            .OrderBy(t => t.SourceTerm)
            .ToListAsync(ct);

        var termIds = terms.Select(t => t.Id).ToList();
        var translations = await db.GlossaryTermTranslations
            .Where(tt => termIds.Contains(tt.GlossaryTermId))
            .ToListAsync(ct);

        var translationMap = translations.GroupBy(tt => tt.GlossaryTermId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var tbx = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("martif",
                new XAttribute("type", "TBX"),
                new XElement("martifHeader",
                    new XElement("fileDesc",
                        new XElement("titleStmt",
                            new XElement("title", glossary.Name)))),
                new XElement("text",
                    new XElement("body",
                        terms.Select(term =>
                        {
                            var langSets = new List<XElement>
                            {
                                new("langSet",
                                    new XAttribute(XNamespace.Xml + "lang", "en"),
                                    new XElement("tig",
                                        new XElement("term", term.SourceTerm)))
                            };

                            if (translationMap.TryGetValue(term.Id, out var trs))
                            {
                                langSets.AddRange(trs.Select(tr =>
                                    new XElement("langSet",
                                        new XAttribute(XNamespace.Xml + "lang", tr.LanguageCode),
                                        new XElement("tig",
                                            new XElement("term", tr.TranslatedTerm)))));
                            }

                            return new XElement("termEntry",
                                new XAttribute("id", term.Id.ToString()), langSets);
                        })))));

        using var ms = new MemoryStream();
        await tbx.SaveAsync(ms, SaveOptions.None, ct);
        return File(ms.ToArray(), "application/xml", "glossary.tbx");
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var inQuotes = false;
        var current = new StringBuilder();

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"') inQuotes = true;
                else if (c == ',')
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                }
                else current.Append(c);
            }
        }

        result.Add(current.ToString().Trim());
        return result;
    }
}

public sealed record UpsertTermRequest(
    string SourceTerm,
    string? Definition,
    bool CaseSensitive,
    bool IsForbidden,
    string? ForbiddenReplacement,
    List<TranslationInput>? Translations);

public sealed record TranslationInput(string LanguageCode, string TranslatedTerm);
