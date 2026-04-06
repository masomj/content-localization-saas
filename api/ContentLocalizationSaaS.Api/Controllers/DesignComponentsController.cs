using ContentLocalizationSaaS.Api.Authorization;
using ContentLocalizationSaaS.Application.Abstractions;
using ContentLocalizationSaaS.Domain;
using ContentLocalizationSaaS.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ContentLocalizationSaaS.Api.Controllers;

public sealed record CreateTextFieldDto(
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

public sealed record CreateDesignComponentRequest(
    string FigmaFileId,
    string FigmaFrameId,
    string FigmaFrameName,
    string ThumbnailUrl,
    int FrameWidth,
    int FrameHeight,
    CreateTextFieldDto[] TextFields);

public sealed record UpdateDesignComponentRequest(
    string? FigmaFrameName,
    string? ThumbnailUrl,
    int? FrameWidth,
    int? FrameHeight,
    string? Status);

[ApiController]
[Route("api/projects/{projectId:guid}/components")]
[Microsoft.AspNetCore.Cors.EnableCors("PluginCors")]
public sealed class DesignComponentsController(AppDbContext db, IEntitlementService entitlements) : ControllerBase
{
    private string CurrentActor => HttpContext.Request.Headers["X-Actor-Email"].ToString() is { Length: > 0 } raw
        ? raw.Trim().ToLowerInvariant()
        : "editor@system.local";

    [HttpGet]
    public async Task<IActionResult> List(Guid projectId, CancellationToken ct)
    {
        var components = await db.DesignComponents
            .Where(c => c.ProjectId == projectId)
            .OrderByDescending(c => c.UpdatedUtc)
            .Select(c => new
            {
                c.Id,
                c.ProjectId,
                c.FigmaFileId,
                c.FigmaFrameId,
                c.FigmaFrameName,
                c.ThumbnailUrl,
                c.FrameWidth,
                c.FrameHeight,
                c.Status,
                c.CreatedByEmail,
                c.CreatedUtc,
                c.UpdatedUtc,
                TextFieldCount = db.DesignComponentTextFields.Count(tf => tf.DesignComponentId == c.Id)
            })
            .ToListAsync(ct);

        return Ok(components);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid projectId, Guid id, CancellationToken ct)
    {
        var component = await db.DesignComponents
            .FirstOrDefaultAsync(c => c.Id == id && c.ProjectId == projectId, ct);

        if (component is null) return NotFound();

        var textFields = await db.DesignComponentTextFields
            .Where(tf => tf.DesignComponentId == id)
            .OrderBy(tf => tf.Y).ThenBy(tf => tf.X)
            .ToListAsync(ct);

        return Ok(new
        {
            component.Id,
            component.ProjectId,
            component.FigmaFileId,
            component.FigmaFrameId,
            component.FigmaFrameName,
            component.ThumbnailUrl,
            component.FrameWidth,
            component.FrameHeight,
            component.Status,
            component.CreatedByEmail,
            component.CreatedUtc,
            component.UpdatedUtc,
            TextFields = textFields.Select(tf => new
            {
                tf.Id,
                tf.DesignComponentId,
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
        });
    }

    [HttpPost]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Create(Guid projectId, [FromBody] CreateDesignComponentRequest request, CancellationToken ct)
    {
        // EP11-S9: Figma board limit enforcement
        var project = await db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (project is not null)
        {
            if (!await entitlements.CanAddFigmaBoardAsync(project.WorkspaceId, ct))
            {
                var snap = await entitlements.GetEntitlementsAsync(project.WorkspaceId, ct);
                return StatusCode(403, new
                {
                    error = "plan_limit_reached",
                    message = $"Your {snap.CurrentTier} plan allows a maximum of {snap.MaxFigmaBoards} Figma board(s). Upgrade to Pro to add more.",
                    currentTier = snap.CurrentTier.ToString(),
                    upgradeRequired = true,
                    used = snap.UsedFigmaBoards,
                    max = snap.MaxFigmaBoards
                });
            }
        }

        var component = new DesignComponent
        {
            ProjectId = projectId,
            FigmaFileId = request.FigmaFileId,
            FigmaFrameId = request.FigmaFrameId,
            FigmaFrameName = request.FigmaFrameName,
            ThumbnailUrl = request.ThumbnailUrl,
            FrameWidth = request.FrameWidth,
            FrameHeight = request.FrameHeight,
            CreatedByEmail = CurrentActor,
            Status = "draft"
        };

        db.DesignComponents.Add(component);

        var textFields = new List<DesignComponentTextField>();
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
            textFields.Add(field);
        }

        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            component.Id,
            component.ProjectId,
            component.FigmaFileId,
            component.FigmaFrameId,
            component.FigmaFrameName,
            component.ThumbnailUrl,
            component.FrameWidth,
            component.FrameHeight,
            component.Status,
            component.CreatedByEmail,
            component.CreatedUtc,
            component.UpdatedUtc,
            TextFields = textFields.Select(tf => new
            {
                tf.Id,
                tf.DesignComponentId,
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
        });
    }

    [HttpPut("{id:guid}")]
    [RequireAppRole(AppRole.Editor)]
    public async Task<IActionResult> Update(Guid projectId, Guid id, [FromBody] UpdateDesignComponentRequest request, CancellationToken ct)
    {
        var component = await db.DesignComponents
            .FirstOrDefaultAsync(c => c.Id == id && c.ProjectId == projectId, ct);

        if (component is null) return NotFound();

        if (request.FigmaFrameName is not null) component.FigmaFrameName = request.FigmaFrameName;
        if (request.ThumbnailUrl is not null) component.ThumbnailUrl = request.ThumbnailUrl;
        if (request.FrameWidth.HasValue) component.FrameWidth = request.FrameWidth.Value;
        if (request.FrameHeight.HasValue) component.FrameHeight = request.FrameHeight.Value;
        if (request.Status is not null) component.Status = request.Status;
        component.UpdatedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(component);
    }

    [HttpDelete("{id:guid}")]
    [RequireAppRole(AppRole.Admin)]
    public async Task<IActionResult> Delete(Guid projectId, Guid id, CancellationToken ct)
    {
        var component = await db.DesignComponents
            .FirstOrDefaultAsync(c => c.Id == id && c.ProjectId == projectId, ct);

        if (component is null) return NotFound();

        db.DesignComponents.Remove(component);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
