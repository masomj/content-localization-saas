# ORCHESTRA Story: Content UX Fixes + Admin Project Management

## Summary

Four improvements:
1. List view: clicking a content item opens the translation editor aside
2. Grid: fix "--missing" showing when content exists (the grid status should reflect source text presence, not just translation status)
3. Content tab: add ability to create folders (not just on projects page)
4. Projects CRUD: move to admin-only permissions

## Stories

### Story 1: List View — Clickable Content Items Open Translation Editor

**Problem:** Content rows in list view are non-interactive `<div>` elements. Users need to click a content key to open the TranslationEditor aside and fill in translations.

**Changes in `frontend/app/pages/app/content/index.vue`:**
1. Make content rows clickable (change `<div>` to `<button>` or add `@click` + cursor:pointer)
2. On click, load the languages for the project, then open the TranslationEditor aside with the first target language selected (or show a language picker if multiple target languages exist)
3. If only one target language, open editor directly for that language
4. If multiple target languages, show a small popover/dropdown to pick which language to edit, then open editor
5. The existing `editingCell` ref and `TranslationEditor` component already support this — just need to wire up the click handler

**Alternative simpler approach:** Open a content detail panel (slide-out) that shows the source text and all language translations at once, allowing inline editing. This is more useful than picking one language at a time.

Actually, the simplest valuable approach: when clicking a content item in list view, open the TranslationEditor with the item's details and the first available target language. The user can close and click again for another language. Use the existing `openEditor` function pattern from the grid view.

### Story 2: Grid — Fix "--missing" Status Display

**Problem:** Grid cells show "--missing" even when the content item has source text. The localization grid's `statusLabel` function shows "Missing" as the default case for any status that isn't explicitly handled (approved/done/pending_review/outdated/todo).

**Root cause:** The grid API (`/api/localization-grid`) returns `targets` per row. If a language has no `ContentItemLanguageTask` entry at all, it won't appear in `targets` array. The `getTarget()` function returns `undefined`, and the template shows "Missing" for `undefined`.

**This is actually correct behavior** — "Missing" means no translation exists for that language+key combination. The issue Mason is describing is likely that he entered source text but expected the grid to show "done" — but source text ≠ translation. Source text is the original language; translations are per-target-language.

**Fix:** Make the "Missing" status clearer in the UI. Instead of `--missing`, show a meaningful indicator:
- If the content item has source text but no translation for this language: show "Untranslated" with a neutral indicator
- If no source text at all: show "No source" in red
- Make the cell clickable to add the translation (already works via edit-cell emit)

**Changes in `frontend/app/components/projects/LocalizationGrid.vue`:**
1. Update `statusLabel()` default case from "Missing" to "Untranslated"
2. Update `statusClass()` default to use a more neutral style (not error-red)
3. In the template, when `getTarget()` returns undefined, show "Untranslated" with a "click to add" hint

### Story 3: Content Tab — Add Folder Creation

**Problem:** Users can only create folders on the projects page, not in the content tab's file explorer.

**Changes in `frontend/app/pages/app/content/index.vue`:**
1. Add a "New Folder" button next to the "Add Content" button in the header actions
2. On click, open a styled modal (matching the existing add-content modal pattern) with:
   - Label: "Folder name" / Hint: "e.g. auth, common, onboarding"
   - Creates folder in the CURRENT folder (uses `currentFolderId` as parentId)
3. After creation, reload the tree to show the new folder
4. Do NOT use window.prompt() — use the modal overlay pattern

### Story 4: Projects — Admin-Only CRUD

**Problem:** Any authenticated user can create/update/delete projects. Mason wants this restricted to admins.

**Backend changes in `api/ContentLocalizationSaaS.Api/Controllers/ProjectsController.cs`:**
1. Add `[RequireAppRole(AppRole.Admin)]` to Create, Update endpoints (already on audit logs)
2. Add a Delete endpoint with `[RequireAppRole(AppRole.Admin)]` if one doesn't exist
3. Keep GetAll and GetById accessible to all roles (Viewer+)

**Frontend changes:**
1. In `frontend/app/pages/app/projects/index.vue`: hide "Create Project" button unless user is admin
2. Use the existing `useAuth().isAdmin` computed ref
3. Add appropriate messaging for non-admin users ("Contact an admin to create projects")

## Delivery Rules

- Strict order: Story 1 → 2 → 3 → 4
- Small commits to `main`
- `npm run build` after frontend changes
- `dotnet build` after backend changes
- Do NOT use window.prompt/alert/confirm — use styled modals
- Report after each story

## UX Defaults

- Helper text under labels, not placeholders
- Dark mode via CSS variables
- Modals use `.project-form-overlay` / `.content-form-overlay` pattern
