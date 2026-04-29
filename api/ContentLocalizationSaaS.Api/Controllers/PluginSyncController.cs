using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record PluginPullRequest(Guid ProjectId, string DesignFileId, string? LanguageCode);
public sealed record PluginPushItem(string LayerId, string BaseSourceText, string NewText);
public sealed record PluginPushRequest(Guid ProjectId, string DesignFileId, PluginPushItem[] Items);

public sealed record PluginPushComponentTextField(
    string FigmaLayerId,
    string FigmaLayerName,
    string CurrentText,
    double X,
    double Y,
    double Width,
    double Height,
    string FontFamily,
    double FontSize,
    string FontWeight,
    string TextAlign,
    string Color);

public sealed record PluginPushComponentRequest(
    Guid ProjectId,
    string FigmaFileId,
    string FigmaFrameId,
    string FigmaFrameName,
    string ThumbnailUrl,
    int FrameWidth,
    int FrameHeight,
    PluginPushComponentTextField[] TextFields);

public sealed record PluginPushLibraryTextField(
    string FigmaLayerId,
    string FigmaLayerName,
    string CurrentText,
    double X,
    double Y,
    double Width,
    double Height,
    string FontFamily,
    double FontSize,
    string FontWeight,
    string TextAlign,
    string Color);

public sealed record PluginPushLibraryVariant(
    string FigmaNodeId,
    string VariantName,
    string VariantProperties,
    string BackgroundColor,
    string ThumbnailUrl,
    int FrameWidth,
    int FrameHeight,
    PluginPushLibraryTextField[] TextFields);

public sealed record PluginPushLibraryComponentRequest(
    Guid ProjectId,
    string FigmaFileId,
    string FigmaComponentKey,
    string FigmaComponentId,
    string FigmaComponentSetId,
    string ComponentName,
    int FrameWidth,
    int FrameHeight,
    PluginPushLibraryVariant[] Variants);

[ApiController]
[Route("api/plugin-sync")]
[Microsoft.AspNetCore.Cors.EnableCors("PluginCors")]
public sealed class PluginSyncController(AppDbContext db) : ControllerBase
{
    [HttpPost("pull")]
    public async Task<IActionResult> Pull([FromBody] PluginPullRequest request, CancellationToken cancellationToken)
    {
        var links = await db.DesignLayerLinks
            .Where(x => x.ProjectId == request.ProjectId && x.DesignFileId == request.DesignFileId)
            .ToListAsync(cancellationToken);

        var contentIds = links.Select(x => x.ContentItemId).Distinct().ToList();
        var items = await db.ContentItems.Where(x => contentIds.Contains(x.Id) && x.Status == "approved").ToListAsync(cancellationToken);

        var payload = new List<object>();
        foreach (var link in links)
        {
            var item = items.FirstOrDefault(x => x.Id == link.ContentItemId);
            if (item is null) continue;

            string text = item.Source;
            if (!string.IsNullOrWhiteSpace(request.LanguageCode))
            {
                var task = await db.ContentItemLanguageTasks.FirstOrDefaultAsync(
                    x => x.ContentItemId == item.Id && x.LanguageCode == request.LanguageCode && (x.Status == "approved" || x.Status == "done"),
                    cancellationToken);
                if (task is not null && !string.IsNullOrWhiteSpace(task.TranslationText))
                {
                    text = task.TranslationText;
                }
            }

            payload.Add(new
            {
                link.LayerId,
                contentItemId = item.Id,
                text,
                language = request.LanguageCode ?? "source"
            });
        }

        return Ok(new { updates = payload });
    }

    [HttpPost("push")]
    public async Task<IActionResult> Push([FromBody] PluginPushRequest request, CancellationToken cancellationToken)
    {
        var conflicts = new List<object>();
        var updates = new List<object>();

        foreach (var incoming in request.Items ?? [])
        {
            var link = await db.DesignLayerLinks.FirstOrDefaultAsync(
                x => x.ProjectId == request.ProjectId && x.DesignFileId == request.DesignFileId && x.LayerId == incoming.LayerId,
                cancellationToken);
            if (link is null) continue;

            var item = await db.ContentItems.FirstOrDefaultAsync(x => x.Id == link.ContentItemId, cancellationToken);
            if (item is null) continue;

            if (!string.Equals(item.Source, incoming.BaseSourceText, StringComparison.Ordinal))
            {
                var conflict = new PluginSyncConflict
                {
                    DesignLayerLinkId = link.Id,
                    CurrentText = item.Source,
                    ProposedText = incoming.NewText,
                    ResolutionState = "open"
                };
                db.PluginSyncConflicts.Add(conflict);
                await db.SaveChangesAsync(cancellationToken);

                conflicts.Add(new
                {
                    layerId = link.LayerId,
                    contentItemId = item.Id,
                    currentText = item.Source,
                    proposedText = incoming.NewText,
                    conflictId = conflict.Id
                });
                continue;
            }

            var previousStatus = item.Status;
            var previousSource = item.Source;
            item.Source = incoming.NewText;
            item.Status = "draft";

            db.ContentItemRevisions.Add(new ContentItemRevision
            {
                ContentItemId = item.Id,
                ActorEmail = "plugin@figma",
                PreviousSource = previousSource,
                NewSource = item.Source,
                PreviousStatus = previousStatus,
                NewStatus = item.Status,
                DiffSummary = "plugin push update",
                EventType = "plugin_push"
            });

            updates.Add(new { layerId = link.LayerId, contentItemId = item.Id, status = "updated" });
        }

        await db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            updates,
            conflicts,
            requiresConflictResolution = conflicts.Count > 0
        });
    }

    [HttpPost("push-component")]
    public async Task<IActionResult> PushComponent([FromBody] PluginPushComponentRequest request, CancellationToken cancellationToken)
    {
        // Check if component already exists (same fileId + frameId within project)
        var existing = await db.DesignComponents.FirstOrDefaultAsync(
            c => c.ProjectId == request.ProjectId
                && c.FigmaFileId == request.FigmaFileId
                && c.FigmaFrameId == request.FigmaFrameId,
            cancellationToken);

        if (existing is not null)
        {
            // Update metadata
            existing.FigmaFrameName = request.FigmaFrameName;
            existing.ThumbnailUrl = request.ThumbnailUrl;
            existing.FrameWidth = request.FrameWidth;
            existing.FrameHeight = request.FrameHeight;
            existing.UpdatedUtc = DateTime.UtcNow;

            // Update or create text fields
            var existingFields = await db.DesignComponentTextFields
                .Where(tf => tf.DesignComponentId == existing.Id)
                .ToListAsync(cancellationToken);

            foreach (var incoming in request.TextFields ?? [])
            {
                var field = existingFields.FirstOrDefault(f => f.FigmaLayerId == incoming.FigmaLayerId);
                if (field is not null)
                {
                    var previousText = field.CurrentText;
                    field.FigmaLayerName = incoming.FigmaLayerName;
                    field.CurrentText = incoming.CurrentText;
                    field.X = incoming.X;
                    field.Y = incoming.Y;
                    field.Width = incoming.Width;
                    field.Height = incoming.Height;
                    field.FontFamily = incoming.FontFamily;
                    field.FontSize = incoming.FontSize;
                    field.FontWeight = incoming.FontWeight;
                    field.TextAlign = incoming.TextAlign;
                    field.Color = incoming.Color;
                    field.UpdatedUtc = DateTime.UtcNow;

                    // If linked to content item and text changed, update linked item
                    if (field.ContentItemId.HasValue && !string.Equals(previousText, incoming.CurrentText, StringComparison.Ordinal))
                    {
                        var linkedItem = await db.ContentItems.FirstOrDefaultAsync(ci => ci.Id == field.ContentItemId.Value, cancellationToken);
                        if (linkedItem is not null)
                        {
                            var prevSource = linkedItem.Source;
                            var prevStatus = linkedItem.Status;
                            linkedItem.Source = incoming.CurrentText;
                            linkedItem.Status = "draft";

                            db.ContentItemRevisions.Add(new ContentItemRevision
                            {
                                ContentItemId = linkedItem.Id,
                                ActorEmail = "plugin@figma",
                                PreviousSource = prevSource,
                                NewSource = incoming.CurrentText,
                                PreviousStatus = prevStatus,
                                NewStatus = "draft",
                                DiffSummary = "plugin component push update",
                                EventType = "plugin_push"
                            });
                        }
                    }
                }
                else
                {
                    db.DesignComponentTextFields.Add(new DesignComponentTextField
                    {
                        DesignComponentId = existing.Id,
                        FigmaLayerId = incoming.FigmaLayerId,
                        FigmaLayerName = incoming.FigmaLayerName,
                        CurrentText = incoming.CurrentText,
                        X = incoming.X,
                        Y = incoming.Y,
                        Width = incoming.Width,
                        Height = incoming.Height,
                        FontFamily = incoming.FontFamily,
                        FontSize = incoming.FontSize,
                        FontWeight = incoming.FontWeight,
                        TextAlign = incoming.TextAlign,
                        Color = incoming.Color
                    });
                }
            }

            // Remove fields no longer present in the push
            var incomingLayerIds = (request.TextFields ?? []).Select(tf => tf.FigmaLayerId).ToHashSet();
            var removedFields = existingFields.Where(f => !incomingLayerIds.Contains(f.FigmaLayerId)).ToList();
            db.DesignComponentTextFields.RemoveRange(removedFields);

            await db.SaveChangesAsync(cancellationToken);

            var updatedFields = await db.DesignComponentTextFields
                .Where(tf => tf.DesignComponentId == existing.Id)
                .OrderBy(tf => tf.Y).ThenBy(tf => tf.X)
                .ToListAsync(cancellationToken);

            return Ok(new { component = existing, textFields = updatedFields });
        }

        // Create new component
        var component = new DesignComponent
        {
            ProjectId = request.ProjectId,
            FigmaFileId = request.FigmaFileId,
            FigmaFrameId = request.FigmaFrameId,
            FigmaFrameName = request.FigmaFrameName,
            ThumbnailUrl = request.ThumbnailUrl,
            FrameWidth = request.FrameWidth,
            FrameHeight = request.FrameHeight,
            CreatedByEmail = "plugin@figma",
            Status = "draft"
        };
        db.DesignComponents.Add(component);

        var newFields = new List<DesignComponentTextField>();
        foreach (var tf in request.TextFields ?? [])
        {
            var field = new DesignComponentTextField
            {
                DesignComponentId = component.Id,
                FigmaLayerId = tf.FigmaLayerId,
                FigmaLayerName = tf.FigmaLayerName,
                CurrentText = tf.CurrentText,
                X = tf.X,
                Y = tf.Y,
                Width = tf.Width,
                Height = tf.Height,
                FontFamily = tf.FontFamily,
                FontSize = tf.FontSize,
                FontWeight = tf.FontWeight,
                TextAlign = tf.TextAlign,
                Color = tf.Color
            };
            db.DesignComponentTextFields.Add(field);
            newFields.Add(field);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { component, textFields = newFields });
    }

    [HttpPost("pull-component/{componentId:guid}")]
    public async Task<IActionResult> PullComponent(Guid componentId, [FromQuery] string? language, CancellationToken cancellationToken)
    {
        var component = await db.DesignComponents.FirstOrDefaultAsync(c => c.Id == componentId, cancellationToken);
        if (component is null) return NotFound();

        var textFields = await db.DesignComponentTextFields
            .Where(tf => tf.DesignComponentId == componentId)
            .OrderBy(tf => tf.Y).ThenBy(tf => tf.X)
            .ToListAsync(cancellationToken);

        // If a language is requested, look up translations for linked content items
        var translationMap = new Dictionary<Guid, string>();
        if (!string.IsNullOrWhiteSpace(language))
        {
            var linkedContentIds = textFields
                .Where(tf => tf.ContentItemId.HasValue)
                .Select(tf => tf.ContentItemId!.Value)
                .Distinct()
                .ToList();

            if (linkedContentIds.Count > 0)
            {
                var translations = await db.ContentItemLanguageTasks
                    .Where(t => linkedContentIds.Contains(t.ContentItemId)
                        && t.LanguageCode == language
                        && !string.IsNullOrEmpty(t.TranslationText))
                    .ToListAsync(cancellationToken);

                foreach (var t in translations)
                {
                    translationMap[t.ContentItemId] = t.TranslationText;
                }
            }
        }

        var result = textFields.Select(tf =>
        {
            var text = tf.CurrentText;
            // If language requested and this field is linked with a translation, use it
            if (!string.IsNullOrWhiteSpace(language) && tf.ContentItemId.HasValue
                && translationMap.TryGetValue(tf.ContentItemId.Value, out var translated))
            {
                text = translated;
            }

            return new
            {
                tf.Id,
                tf.FigmaLayerId,
                tf.FigmaLayerName,
                CurrentText = text,
                tf.ContentItemId,
                tf.X,
                tf.Y,
                tf.Width,
                tf.Height,
                tf.FontFamily,
                tf.FontSize,
                tf.FontWeight,
                tf.TextAlign,
                tf.Color,
                tf.UpdatedUtc,
                Language = !string.IsNullOrWhiteSpace(language) ? language : null
            };
        }).ToList();

        // Also return available languages for the project
        var projectLanguages = await db.ProjectLanguages
            .Where(l => l.ProjectId == component.ProjectId && l.IsActive)
            .Select(l => new { l.Bcp47Code, l.IsSource })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            component.Id,
            component.FigmaFileId,
            component.FigmaFrameId,
            component.FigmaFrameName,
            component.FrameWidth,
            component.FrameHeight,
            component.Status,
            component.UpdatedUtc,
            textFields = result,
            languages = projectLanguages,
            requestedLanguage = language
        });
    }

    [HttpPost("push-library-component")]
    public async Task<IActionResult> PushLibraryComponent([FromBody] PluginPushLibraryComponentRequest request, CancellationToken cancellationToken)
    {
        // Upsert component by projectId + fileId + componentKey
        var existing = await db.LibraryComponents.FirstOrDefaultAsync(
            c => c.ProjectId == request.ProjectId
                && c.FigmaFileId == request.FigmaFileId
                && c.FigmaComponentKey == request.FigmaComponentKey,
            cancellationToken);

        LibraryComponent component;

        if (existing is not null)
        {
            existing.FigmaComponentId = request.FigmaComponentId;
            existing.FigmaComponentSetId = request.FigmaComponentSetId;
            existing.Name = request.ComponentName;
            existing.FrameWidth = request.FrameWidth;
            existing.FrameHeight = request.FrameHeight;
            existing.UpdatedUtc = DateTime.UtcNow;
            component = existing;
        }
        else
        {
            component = new LibraryComponent
            {
                ProjectId = request.ProjectId,
                FigmaFileId = request.FigmaFileId,
                FigmaComponentKey = request.FigmaComponentKey,
                FigmaComponentId = request.FigmaComponentId,
                FigmaComponentSetId = request.FigmaComponentSetId,
                Name = request.ComponentName,
                FrameWidth = request.FrameWidth,
                FrameHeight = request.FrameHeight
            };
            db.LibraryComponents.Add(component);
        }

        // Load existing variants for this component
        var existingVariants = await db.LibraryComponentVariants
            .Where(v => v.LibraryComponentId == component.Id)
            .ToListAsync(cancellationToken);

        var existingVariantIds = existingVariants.Select(v => v.Id).ToList();
        var existingTextFields = existingVariantIds.Count > 0
            ? await db.LibraryComponentTextFields
                .Where(tf => existingVariantIds.Contains(tf.LibraryComponentVariantId))
                .ToListAsync(cancellationToken)
            : new List<LibraryComponentTextField>();

        var processedVariantIds = new HashSet<Guid>();

        foreach (var incomingVariant in request.Variants ?? [])
        {
            var variant = existingVariants.FirstOrDefault(v => v.FigmaNodeId == incomingVariant.FigmaNodeId);
            if (variant is not null)
            {
                variant.VariantName = incomingVariant.VariantName;
                variant.VariantProperties = incomingVariant.VariantProperties;
                variant.BackgroundColor = incomingVariant.BackgroundColor ?? "#374151";
                variant.ThumbnailUrl = incomingVariant.ThumbnailUrl ?? "";
                variant.FrameWidth = incomingVariant.FrameWidth;
                variant.FrameHeight = incomingVariant.FrameHeight;
                variant.UpdatedUtc = DateTime.UtcNow;
            }
            else
            {
                variant = new LibraryComponentVariant
                {
                    LibraryComponentId = component.Id,
                    FigmaNodeId = incomingVariant.FigmaNodeId,
                    VariantName = incomingVariant.VariantName,
                    VariantProperties = incomingVariant.VariantProperties,
                    BackgroundColor = incomingVariant.BackgroundColor ?? "#374151",
                    ThumbnailUrl = incomingVariant.ThumbnailUrl ?? "",
                    FrameWidth = incomingVariant.FrameWidth,
                    FrameHeight = incomingVariant.FrameHeight
                };
                db.LibraryComponentVariants.Add(variant);
            }
            processedVariantIds.Add(variant.Id);

            // Upsert text fields for this variant
            var variantFields = existingTextFields.Where(tf => tf.LibraryComponentVariantId == variant.Id).ToList();
            var processedFieldIds = new HashSet<Guid>();

            foreach (var incomingField in incomingVariant.TextFields ?? [])
            {
                var field = variantFields.FirstOrDefault(f => f.FigmaLayerId == incomingField.FigmaLayerId);
                if (field is not null)
                {
                    field.FigmaLayerName = incomingField.FigmaLayerName;
                    field.CurrentText = incomingField.CurrentText;
                    field.X = incomingField.X;
                    field.Y = incomingField.Y;
                    field.Width = incomingField.Width;
                    field.Height = incomingField.Height;
                    field.FontFamily = incomingField.FontFamily;
                    field.FontSize = incomingField.FontSize;
                    field.FontWeight = incomingField.FontWeight;
                    field.TextAlign = incomingField.TextAlign;
                    field.Color = incomingField.Color;
                    field.UpdatedUtc = DateTime.UtcNow;
                }
                else
                {
                    field = new LibraryComponentTextField
                    {
                        LibraryComponentVariantId = variant.Id,
                        FigmaLayerId = incomingField.FigmaLayerId,
                        FigmaLayerName = incomingField.FigmaLayerName,
                        CurrentText = incomingField.CurrentText,
                        X = incomingField.X,
                        Y = incomingField.Y,
                        Width = incomingField.Width,
                        Height = incomingField.Height,
                        FontFamily = incomingField.FontFamily,
                        FontSize = incomingField.FontSize,
                        FontWeight = incomingField.FontWeight,
                        TextAlign = incomingField.TextAlign,
                        Color = incomingField.Color
                    };
                    db.LibraryComponentTextFields.Add(field);
                }
                processedFieldIds.Add(field.Id);
            }

            // Remove text fields no longer present
            var removedFields = variantFields.Where(f => !processedFieldIds.Contains(f.Id)).ToList();
            db.LibraryComponentTextFields.RemoveRange(removedFields);
        }

        // Remove variants no longer present
        var removedVariants = existingVariants.Where(v => !processedVariantIds.Contains(v.Id)).ToList();
        foreach (var removedVariant in removedVariants)
        {
            var orphanFields = existingTextFields.Where(tf => tf.LibraryComponentVariantId == removedVariant.Id).ToList();
            db.LibraryComponentTextFields.RemoveRange(orphanFields);
        }
        db.LibraryComponentVariants.RemoveRange(removedVariants);

        await db.SaveChangesAsync(cancellationToken);

        // Reload full result
        var resultVariants = await db.LibraryComponentVariants
            .Where(v => v.LibraryComponentId == component.Id)
            .OrderBy(v => v.VariantName)
            .ToListAsync(cancellationToken);

        var resultVariantIds = resultVariants.Select(v => v.Id).ToList();
        var resultFields = await db.LibraryComponentTextFields
            .Where(tf => resultVariantIds.Contains(tf.LibraryComponentVariantId))
            .OrderBy(tf => tf.Y).ThenBy(tf => tf.X)
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            component = new
            {
                component.Id,
                component.ProjectId,
                component.FigmaFileId,
                component.FigmaComponentKey,
                component.FigmaComponentId,
                component.FigmaComponentSetId,
                component.Name,
                component.FrameWidth,
                component.FrameHeight,
                component.CreatedUtc,
                component.UpdatedUtc
            },
            variants = resultVariants.Select(v => new
            {
                v.Id,
                v.LibraryComponentId,
                v.FigmaNodeId,
                v.VariantName,
                v.VariantProperties,
                v.BackgroundColor,
                v.ThumbnailUrl,
                v.FrameWidth,
                v.FrameHeight,
                v.CreatedUtc,
                v.UpdatedUtc,
                textFields = resultFields
                    .Where(tf => tf.LibraryComponentVariantId == v.Id)
                    .Select(tf => new
                    {
                        tf.Id,
                        tf.LibraryComponentVariantId,
                        tf.FigmaLayerId,
                        tf.FigmaLayerName,
                        tf.CurrentText,
                        tf.ContentItemId,
                        tf.X,
                        tf.Y,
                        tf.Width,
                        tf.Height,
                        tf.FontFamily,
                        tf.FontSize,
                        tf.FontWeight,
                        tf.TextAlign,
                        tf.Color,
                        tf.CreatedUtc,
                        tf.UpdatedUtc
                    })
            })
        });
    }
}
