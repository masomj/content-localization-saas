# ORCHESTRA Story: Hierarchical Content Tree (Mixed Folders + Content Keys)

## Summary

Enable a true hierarchical content model where projects contain mixed children — both **folders** (`ProjectCollection`) and **content keys** (`ContentItem`) — at any level. Currently content keys are flat (scoped only to `ProjectId`); this work adds `CollectionId` to `ContentItem` so keys can live inside folders.

## Current State (Phase 0 findings)

### Domain (`Entities.cs`)
- `ProjectCollection` has: `Id`, `ProjectId`, `ParentId` (nullable), `Name`, `IsRoot`, `Depth`, `SortOrder`
- `ContentItem` has: `Id`, `ProjectId`, `Key`, `Source`, `Status` … but **no folder/collection reference**
- Collections have self-referencing FK (`ParentId` → `ProjectCollection`)
- ContentItems are flat — only linked to project, not to a folder

### API
- `ProjectCollectionsController`: List, Create, Rename, Move — folder-only operations
- `ContentItemsController`: GetAll (flat list with optional `projectId`/`search` filter), Create
- No unified tree endpoint exists

### Frontend
- `CollectionTreeNode.vue`: recursive tree rendering folders only
- `types.ts` has `Collection` and `ContentItem` types — separate, not unified
- No UI for creating a content key inside a specific folder

## Implementation Stories

### Story 1: Domain — Add `CollectionId` to `ContentItem`

**Changes:**
1. `ContentItem` entity: add `Guid? CollectionId` property (nullable — `null` = project root)
2. `AppDbContext.OnModelCreating`: add FK from `ContentItem.CollectionId` → `ProjectCollection.Id` with `DeleteBehavior.SetNull` (if folder deleted, keys float to project root)
3. Add index on `(ProjectId, CollectionId)` for efficient tree queries
4. EF migration: `AddContentItemCollectionId`

**Acceptance:**
- Migration applies cleanly
- Existing content items get `CollectionId = null` (project root) — no data loss
- `dotnet build` passes

### Story 2: Application — Update Contracts & Validation

**Changes:**
1. `CreateContentItemRequest`: add `Guid? CollectionId` parameter
2. `CreateContentItemRequestValidator`: if `CollectionId` provided, no extra validation needed (FK handles integrity)
3. New contract: `MoveContentItemRequest(Guid? CollectionId, int SortOrder)`
4. New contract: `ProjectTreeNode` DTO for unified tree response:
   ```
   ProjectTreeNode {
     Id: Guid
     Name: string
     NodeType: "folder" | "contentKey"
     ParentId: Guid?  // CollectionId for keys, ParentId for folders
     SortOrder: int
     Depth: int
     Children: ProjectTreeNode[]
     // For contentKey nodes only:
     Key: string?
     Status: string?
   }
   ```
5. `IContentItemService`: add `MoveAsync(Guid contentItemId, MoveContentItemRequest)`
6. `IProjectCollectionService`: add `GetTreeAsync(Guid projectId)` → returns `List<ProjectTreeNode>`

**Acceptance:**
- Contracts compile
- Validators pass for valid/invalid inputs

### Story 3: Infrastructure — Service Implementations

**Changes:**
1. `ContentItemService.CreateAsync`: set `CollectionId` from request
2. `ContentItemService.MoveAsync`: update `CollectionId` + `SortOrder` (validate collection belongs to same project)
3. `ProjectCollectionService.GetTreeAsync`:
   - Query all collections + content items for project
   - Build unified tree (folders and keys as children, sorted by `SortOrder`)
   - Return `List<ProjectTreeNode>` (root-level nodes where `ParentId`/`CollectionId` is null)
4. Add `SortOrder` column to `ContentItem` if not present (for ordering within a folder)

**Acceptance:**
- `dotnet build` + `dotnet test` pass
- Tree endpoint returns mixed folder + key nodes

### Story 4: API — New Endpoints

**Changes:**
1. `GET /api/projects/{projectId}/tree` → returns unified `ProjectTreeNode[]`
2. `PUT /api/content-items/{id}/move` → moves a content key to a different folder
3. Update `POST /api/content-items` to accept `collectionId` in request body

**Acceptance:**
- API compiles and endpoints respond correctly
- `dotnet build` passes

### Story 5: Frontend — Unified Tree Explorer

**Changes:**
1. New `ProjectTreeNode` type in `types.ts`
2. New API client method: `getProjectTree(projectId)`, `moveContentItem(id, collectionId, sortOrder)`
3. Update `CollectionTreeNode.vue` → render both folder and contentKey node types:
   - Folder nodes: folder icon, expand/collapse, context menu (New Folder, New Content Key, Rename, Delete)
   - ContentKey nodes: key icon, leaf node, context menu (Rename, Move, Delete)
   - Project root level: "New Folder" + "New Content Key" buttons
4. Drag/drop: allow dragging content keys between folders and to project root
5. Content keys are **leaf nodes only** — cannot contain children
6. New key creation: "New Content Key" button/menu item passes current folder's `collectionId`

**Acceptance:**
- Tree renders mixed folder + content key children
- Can create a content key inside a subfolder
- Can drag a content key between folders
- `npm run build` passes
- Context menus work correctly per node type

### Story 6: UX/A11y Review

- Keyboard navigation through mixed tree
- Focus management on create/move/delete
- ARIA tree role attributes on mixed nodes
- Skip-link target maintained
- Screen reader announces node type (folder vs key)

### Story 7: QA Smoke Tests

- Create project → create folder → create content key inside folder
- Move content key from root to folder and back
- Delete folder → content keys float to project root
- Tree API returns correct nested structure
- Drag/drop reorder within folder
- `dotnet build` + `dotnet test` + `npm run build` all pass

## Delivery Rules

- Strict phase order: Domain → Application → Infrastructure → API → Frontend → A11y → QA
- Small commits to `main`
- Report after each story: changed files, validations, commit hash
- Raise blockers immediately

## UX Defaults

- Content keys are leaf nodes (no children)
- Folders are the only containers
- Context menu on folders: New Folder, New Content Key, Rename, Delete
- Context menu on keys: Rename, Move, Delete
- Deleting a folder sets orphaned keys' `CollectionId` to null (project root)
