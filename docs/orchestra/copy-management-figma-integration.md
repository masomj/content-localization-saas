# ORCHESTRA Spec: Copy Management + Figma Integration (Frontitude-Style)

## Vision

Combine i18n content management with UX copy editing. Designers create components in Figma, the plugin pushes frame structures to the webapp, UX writers edit copy in context, and devs consume finalized copy via the existing i18n export pipeline.

## Architecture Overview

```
Figma Plugin ──push──→ API ──→ Component (frame + text layers)
     ↑                              ↓
     └──pull── API ←── Text edits ← Webapp Component View
                                     ↓
                          Link to ContentItem (i18n key)
                                     ↓
                          Translations + Export pipeline
```

## Existing Infrastructure (already built)

### Backend
- `DesignLayerLink` — maps Figma fileId + layerId to ContentItemId
- `PluginAuthController` — `POST /api/plugin/login`, `GET /api/plugin/projects`, `POST /api/plugin/switch-workspace`
- `PluginLayerLinksController` — `POST link-layer`, `GET search-items`, `GET layer`, `GET details`, `POST duplicate-layer`
- `PluginSyncController` — `POST pull` (get latest text from webapp), `POST push` (send Figma text to webapp)
- `PluginSyncConflict` — conflict detection for push/pull
- `CopyComponent` — reusable copy blocks with name + source text
- `ContentItemRevision` — full edit history with actor, before/after text, diff summary

### Frontend
- Content page with file explorer, folders, breadcrumbs
- Side panel for editing source text + status + folder
- Delete, drag-drop move between folders
- Language management, translation editor
- Export panel with version-aware exports

---

## Phase 1: Domain — DesignComponent Entity

### What's needed
The existing `DesignLayerLink` stores individual layer→content-item mappings. But we need a parent entity that represents a Figma frame/component as a visual unit containing multiple text layers.

### New entity: `DesignComponent`
```csharp
public sealed class DesignComponent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string FigmaFileId { get; set; } = string.Empty;
    public string FigmaFrameId { get; set; } = string.Empty;
    public string FigmaFrameName { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;      // snapshot from Figma
    public int FrameWidth { get; set; }
    public int FrameHeight { get; set; }
    public string Status { get; set; } = "draft";                   // draft | in_review | approved
    public string CreatedByEmail { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
```

### New entity: `DesignComponentTextField`
Each text layer within a frame, with its position for visual rendering.
```csharp
public sealed class DesignComponentTextField
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DesignComponentId { get; set; }
    public string FigmaLayerId { get; set; } = string.Empty;
    public string FigmaLayerName { get; set; } = string.Empty;
    public string CurrentText { get; set; } = string.Empty;
    public Guid? ContentItemId { get; set; }                        // linked i18n key (nullable)
    public double X { get; set; }                                    // position within frame
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string FontFamily { get; set; } = string.Empty;
    public double FontSize { get; set; }
    public string FontWeight { get; set; } = string.Empty;
    public string TextAlign { get; set; } = "left";
    public string Color { get; set; } = string.Empty;               // hex color
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
```

### Migration
- Tables: `design_components`, `design_component_text_fields`
- FKs: DesignComponent.ProjectId → Project (Cascade), DesignComponentTextField.DesignComponentId → DesignComponent (Cascade), DesignComponentTextField.ContentItemId → ContentItem (SetNull)
- Indexes: (ProjectId, FigmaFileId, FigmaFrameId) unique on DesignComponent, (DesignComponentId) on TextField

---

## Phase 2: Backend — Component CRUD + Sync API

### New `DesignComponentsController`
- `GET /api/projects/{projectId}/components` — list all components for a project
- `GET /api/projects/{projectId}/components/{id}` — get component with all text fields
- `POST /api/projects/{projectId}/components` — create component (from Figma plugin push)
  - Accepts: figmaFileId, figmaFrameId, figmaFrameName, thumbnailUrl, width, height, textFields[]
  - Each textField: figmaLayerId, figmaLayerName, currentText, x, y, width, height, fontFamily, fontSize, fontWeight, textAlign, color
- `PUT /api/projects/{projectId}/components/{id}` — update component metadata
- `DELETE /api/projects/{projectId}/components/{id}` — delete component (Admin only)

### New `DesignComponentTextFieldsController`
- `PUT /api/component-text-fields/{id}` — update text field content
  - Body: { currentText, contentItemId? }
  - Creates a `ContentItemRevision` if text changed
  - If contentItemId is set, also updates the linked ContentItem's source text
- `GET /api/component-text-fields/{id}/history` — get revision history for this text field
- `POST /api/component-text-fields/{id}/link-content-key` — link to an existing content key
  - Body: { contentItemId }
- `DELETE /api/component-text-fields/{id}/link-content-key` — unlink from content key

### Plugin sync updates
- `POST /api/plugin/push-component` — Figma plugin pushes a frame structure
  - If component exists (same fileId + frameId), update text fields + positions
  - If new, create the component
  - Returns the component with all text fields
- `POST /api/plugin/pull-component/{componentId}` — pull latest text from webapp
  - Returns all text fields with current text (for Figma to update)

---

## Phase 3: Frontend — Components Section + Layer Navigator

### New sidebar nav item: "Components"
- Add to app layout sidebar between Content and Review
- Route: `/app/components`

### Components list page (`/app/components`)
- Project selector (like content page)
- Card grid showing each component:
  - Thumbnail image (from Figma)
  - Frame name
  - Text field count
  - Status badge
  - Last updated
- Click a card → navigates to component detail

### Component detail page (`/app/components/[id]`)
- **Left sidebar: Layer navigator** (Figma-style)
  - Tree of text fields within the component
  - Click a text field → highlights it on the canvas + opens editor
  - Shows linked content key badge per field

- **Center: Visual canvas**
  - Renders the frame thumbnail as background
  - Overlays text fields as positioned rectangles
  - Each text field shows its current text
  - Click any text field → selects it + opens right sidebar editor
  - Selected field has a highlighted border

- **Right sidebar: Text field editor** (slide-out)
  - Field name (from Figma layer name)
  - Current text (editable textarea)
  - Link to content key: dropdown to search/select existing content keys or create new
  - If linked: shows the content key, source language text, translation status summary
  - Version history: timeline of edits with before/after text, actor, timestamp
  - Save button

### UX flow
1. Designer pushes frame from Figma plugin
2. Component appears in webapp Components section
3. UX writer opens component, sees the visual layout
4. Clicks a text field, edits copy in the right sidebar
5. Optionally links the text field to an i18n content key
6. Saves → text is stored, revision created
7. If linked to content key, translations pipeline is triggered
8. Designer pulls latest in Figma → text layers update

---

## Phase 4: Figma Plugin

### Technology
- TypeScript + Figma Plugin API
- Located in new `figma-plugin/` directory at repo root
- Uses Figma Plugin API for frame scanning + text layer extraction
- Communicates with backend via REST API (plugin auth flow)

### Plugin features
1. **Auth**: Login via InterCopy credentials (uses existing `/api/plugin/login`)
2. **Push frame**: Select a frame → extract all text layers with positions → push to API
3. **Pull latest**: Select a component → pull latest text from webapp → update Figma text layers
4. **Link indicator**: Show which text layers are synced (badge/icon)
5. **Conflict resolution**: If text changed in both Figma and webapp since last sync, show conflict UI

### Plugin structure
```
figma-plugin/
  manifest.json
  src/
    main.ts          // Plugin main thread (Figma API access)
    ui.html          // Plugin UI (iframe)
    ui.ts            // UI logic
    api.ts           // API client for InterCopy backend
    types.ts         // Shared types
  package.json
  tsconfig.json
```

---

## Phase 5: i18n Bridge

### When text field is linked to a content key
- Editing text in component view updates both the text field AND the linked ContentItem.Source
- Changing ContentItem.Source (from content page) updates the linked text field's CurrentText
- Translation status shows in the component text field editor sidebar

### When text field is NOT linked
- Text is stored only in DesignComponentTextField.CurrentText
- User can link it later to a content key

---

## Delivery Order

1. **Phase 1**: Domain entities + migration (backend only, ~15 min)
2. **Phase 2**: Backend API endpoints (backend only, ~30 min)
3. **Phase 3**: Frontend components section + canvas + editor (~45 min)
4. **Phase 4**: Figma plugin scaffold + core features (~30 min)
5. **Phase 5**: i18n bridge wiring (~15 min)

## Acceptance Criteria

- Figma plugin can push a frame with text layers to the webapp
- Webapp shows the component with positioned text fields
- Click text field → edit text → save → revision created
- Link text field to content key → i18n translations available
- Figma plugin can pull latest text back
- `dotnet build` + `npm run build` pass
- No browser alerts/prompts — styled modals only
