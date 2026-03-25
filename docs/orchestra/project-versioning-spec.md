# ORCHESTRA Story: Project Versioning (GitHub Releases-Style)

## Summary

Add versioning to projects so users can work on content changes in the app without affecting what's deployed/consumed by the CLI. Users can create versions (like GitHub releases), and mark one as the **current live/release version**. The CLI and export endpoints serve content from the live version only.

## Concept Model

```
Project
  ├── Working Copy (always editable, what users see in the app)
  ├── v1.0.0 (release) ← LIVE
  ├── v0.2.0 (release)
  └── v0.1.0 (release)
```

- **Working copy**: the current state of content items, always editable
- **Release/version**: a snapshot of all content items + translations at a point in time
- **Live version**: the one release marked as current — CLI and export endpoints serve from this
- When no release exists or none is marked live, exports serve from the working copy (backward compatible)

## Current State

### What exists
- `Project` entity with basic metadata
- `ContentItem` entities (flat + collection-scoped)
- `ContentItemLanguageTask` for translations
- Export endpoints: `GET /api/exports/neutral`, `GET /api/integration/exports/bundle`
- CLI spec: `pull` command exports approved content

### What's needed
- `ProjectVersion` entity: version metadata (tag, title, notes, isLive, createdUtc)
- `ProjectVersionSnapshot` entity: frozen copy of content items + translations at release time
- API: CRUD for versions, promote/demote live, compare versions
- Frontend: version management UI
- Export endpoints updated to serve from live version when one exists

## Implementation Stories

### Story 1: Domain — ProjectVersion + ProjectVersionSnapshot Entities

**Changes to `Entities.cs`:**
```csharp
public sealed class ProjectVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string Tag { get; set; } = string.Empty;        // e.g. "v1.0.0", "2024-03-25-release"
    public string Title { get; set; } = string.Empty;       // human-readable title
    public string Notes { get; set; } = string.Empty;       // release notes (markdown)
    public bool IsLive { get; set; }                         // only one per project can be live
    public string CreatedByEmail { get; set; } = string.Empty;
    public int ContentItemCount { get; set; }                // cached count at snapshot time
    public int TranslationCount { get; set; }                // cached count at snapshot time
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ProjectVersionSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VersionId { get; set; }
    public Guid OriginalContentItemId { get; set; }          // reference to source content item
    public string Key { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public Guid? CollectionId { get; set; }
    public int SortOrder { get; set; }
    public string TranslationsJson { get; set; } = string.Empty; // JSON blob: { "fr": "Bonjour", "de": "Hallo" }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
```

**AppDbContext:**
- `DbSet<ProjectVersion>`, `DbSet<ProjectVersionSnapshot>`
- Table `project_versions`: indexes on `(ProjectId, CreatedUtc)`, `(ProjectId, Tag)` unique, `(ProjectId, IsLive)` filtered unique where `IsLive = true`
- Table `project_version_snapshots`: indexes on `(VersionId)`, `(VersionId, Key)`
- FK: `ProjectVersion.ProjectId → Project.Id` Cascade
- FK: `ProjectVersionSnapshot.VersionId → ProjectVersion.Id` Cascade

**Migration:** `AddProjectVersioning`

**Acceptance:**
- Migration applies cleanly, `dotnet build` passes

### Story 2: Backend — Version CRUD + Snapshot + Promote API

**New `ProjectVersionsController`:**

1. `GET /api/projects/{projectId}/versions` — list all versions for a project (ordered by createdUtc desc)
   - Returns: id, tag, title, notes (truncated), isLive, contentItemCount, translationCount, createdByEmail, createdUtc

2. `GET /api/projects/{projectId}/versions/{versionId}` — get single version with full notes

3. `POST /api/projects/{projectId}/versions` — create a new version (cut a release)
   - Request: `{ tag, title, notes }`
   - `[RequireAppRole(AppRole.Editor)]`
   - Validates tag is unique within project
   - **Snapshots all current content items + their approved translations** into `ProjectVersionSnapshot` rows
   - Sets `ContentItemCount` and `TranslationCount`
   - Does NOT auto-promote to live (user must explicitly promote)

4. `PUT /api/projects/{projectId}/versions/{versionId}` — update version metadata
   - Request: `{ title, notes }` (tag is immutable after creation)
   - `[RequireAppRole(AppRole.Editor)]`

5. `DELETE /api/projects/{projectId}/versions/{versionId}` — delete a version
   - `[RequireAppRole(AppRole.Admin)]`
   - Cannot delete the live version (must demote first)
   - Cascades to snapshots

6. `POST /api/projects/{projectId}/versions/{versionId}/promote` — mark as live
   - `[RequireAppRole(AppRole.Editor)]`
   - Demotes any existing live version for the same project
   - Sets `IsLive = true` on this version

7. `POST /api/projects/{projectId}/versions/{versionId}/demote` — remove live status
   - `[RequireAppRole(AppRole.Editor)]`
   - Sets `IsLive = false` (project falls back to working copy for exports)

8. `GET /api/projects/{projectId}/versions/{versionId}/content` — get snapshot content for a version
   - Returns the frozen content items with translations
   - Used for preview/comparison

9. `GET /api/projects/{projectId}/versions/compare?from={versionId}&to={versionId}` — diff two versions
   - Returns: added keys, removed keys, changed keys (with before/after source text)

**Update export endpoints:**
- `ExportBundlesController` and `NeutralExportController`: when a project has a live version, serve from `ProjectVersionSnapshot` instead of working copy `ContentItem`
- Add `?version=latest|live|{versionId}` query param (default: `live` if one exists, otherwise working copy)

**Acceptance:**
- All endpoints work, `dotnet build` + `dotnet test` pass
- Creating a version snapshots current content correctly
- Promote/demote toggles which version exports serve from
- Export endpoints serve from live version when one exists

### Story 3: Frontend — Types & API Client

**Types in `types.ts`:**
```ts
interface ProjectVersion {
  id: string
  projectId: string
  tag: string
  title: string
  notes: string
  isLive: boolean
  createdByEmail: string
  contentItemCount: number
  translationCount: number
  createdUtc: string
}

interface VersionSnapshotItem {
  id: string
  versionId: string
  originalContentItemId: string
  key: string
  source: string
  status: string
  tags: string
  translationsJson: string
}

interface VersionDiff {
  added: Array<{ key: string; source: string }>
  removed: Array<{ key: string; source: string }>
  changed: Array<{ key: string; oldSource: string; newSource: string }>
}
```

**New `versionsClient.ts`:**
- `list(projectId)` → GET /api/projects/{projectId}/versions
- `get(projectId, versionId)` → single version
- `create(projectId, tag, title, notes)` → POST
- `update(projectId, versionId, title, notes)` → PUT
- `remove(projectId, versionId)` → DELETE
- `promote(projectId, versionId)` → POST .../promote
- `demote(projectId, versionId)` → POST .../demote
- `getContent(projectId, versionId)` → GET .../content
- `compare(projectId, fromId, toId)` → GET .../compare

**Acceptance:** `npm run build` passes

### Story 4: Frontend — Version Management Page

**New page `pages/app/projects/[projectId]/versions.vue`:**

GitHub Releases-inspired layout:
- **Header:** "Releases" with "Create Release" button
- **Live version banner** at top if one exists: tag, title, "LIVE" badge, promote date
- **Version list:** cards for each version, newest first
  - Tag (semver or custom, monospace)
  - Title
  - "LIVE" badge (green) if isLive
  - Release notes (markdown rendered or plain text, collapsible)
  - Stats: X content items, Y translations
  - Created by + timestamp
  - Actions: Promote to Live / Demote, Edit, Delete (Admin only)
- **Create Release modal:**
  - Tag input (label: "Version tag" / hint: "e.g. v1.0.0, 2024-Q1-release")
  - Title input (label: "Release title" / hint: "Human-readable name for this release")
  - Notes textarea (label: "Release notes" / hint: "Describe what changed in this version")
  - "Create Release" button — snapshots current working copy
  - Shows content item count + translation count that will be snapshotted
- **Version detail view** (expandable or separate): shows snapshot content, compare with another version

**Navigation:** Add "Releases" link in project context (sidebar or breadcrumb)

**Acceptance:**
- Can create, edit, delete versions
- Can promote/demote live
- Version list shows correctly with live badge
- `npm run build` passes

### Story 5: Frontend — Version Comparison + Content Preview

**Enhancements:**
1. Compare view: select two versions, see diff (added/removed/changed keys)
2. Version content preview: browse the frozen snapshot content
3. Working copy indicator: show "Working Copy (unreleased changes)" badge on content page when working copy differs from live version
4. Export panel: add version selector dropdown (Live / Working Copy / specific version)

**Acceptance:**
- Compare shows meaningful diffs
- Export respects version selection
- `npm run build` passes

### Story 6: A11y Review

- Version cards: keyboard navigable, focus management
- Live badge: not color-only (text + icon)
- Promote/demote: confirmation dialog
- Compare view: accessible diff rendering
- Skip-link maintained

### Story 7: QA Smoke Tests

- Create project → create content → create version → content is snapshotted
- Promote version → export serves from snapshot
- Edit working copy → export still serves old live version
- Create new version → promote → export serves new content
- Demote → export falls back to working copy
- Delete non-live version → succeeds
- Delete live version → blocked
- Compare two versions → correct diff
- `npm run build` + `dotnet build` + `dotnet test` pass

## Delivery Rules

- Strict phase order: Domain → Backend → Frontend Types → Version Management → Compare/Preview → A11y → QA
- Small commits to `main`
- Report after each story
- Raise blockers immediately

## UX Defaults

- Helper text under labels, not placeholders
- Tags shown in monospace
- "LIVE" badge in green with icon
- Confirm dialog before promote/demote/delete
- Release notes support plain text (markdown rendering is stretch goal)
- Responsive: cards stack on mobile

## Export Behavior Summary

| Scenario | Export serves from |
|---|---|
| No versions exist | Working copy |
| Versions exist, none is live | Working copy |
| One version is live | Live version snapshot |
| `?version={id}` specified | That specific version's snapshot |
| `?version=working` specified | Working copy (explicit override) |
