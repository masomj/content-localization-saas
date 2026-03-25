# ORCHESTRA Story: Multi-Language Content Under Keys

## Summary

Build the frontend UI that lets users manage translations for content keys across multiple languages. The backend already supports this (`ProjectLanguage`, `ContentItemLanguageTask`, `TranslationMemoryEntry`, language tasks API, localization grid API, export bundles). This feature connects the frontend to that backend so that:

1. Users can add/manage target languages for a project
2. Users can view all keys × languages in a grid
3. Users can write/edit translations per key per language
4. Users can see translation status (todo/pending_review/approved/done/outdated)
5. Devs can export locale bundles (JSON) for consumption by i18n libs (react-intl, vue-i18n, next-intl, i18next, etc.)

## Current State

### Backend (already exists, no changes needed)
- `ProjectLanguage` entity: `Id`, `ProjectId`, `Bcp47Code`, `IsSource`, `IsActive`
- `ContentItemLanguageTask` entity: per-key per-language task with `LanguageCode`, `TranslationText`, `Status`, `AssigneeEmail`, `IsOutdated`, `DueUtc`
- `TranslationMemoryEntry` entity: approved translations for TM suggestions
- **API endpoints already working:**
  - `GET /api/project-languages?projectId=X` — list project languages
  - `POST /api/project-languages` — add language `{ projectId, bcp47Code, isSource }`
  - `PUT /api/project-languages/{id}/active` — toggle active `{ isActive }`
  - `POST /api/project-languages/source-language?projectId=X` — change source language
  - `GET /api/language-tasks?contentItemId=X` — get translation tasks for a key
  - `GET /api/language-tasks/suggestions?contentItemId=X&languageCode=Y` — TM suggestions
  - `POST /api/language-tasks` — upsert translation `{ contentItemId, languageCode, status, translationText, ... }`
  - `POST /api/language-tasks/apply-memory` — apply TM suggestion
  - `GET /api/localization-grid?projectId=X` — paginated key × language grid
  - `GET /api/exports/neutral?projectId=X` — full neutral export (all languages)
  - `GET /api/integration/exports/bundle?projectId=X&language=Y` — per-language bundle export

### Frontend (needs building)
- Content page (`pages/app/content/index.vue`) shows flat key list with source text only
- No language management UI
- No translation editing UI
- No grid view of keys × languages
- No export UI
- `types.ts` has no language/translation types
- No API client methods for language/translation endpoints

## Implementation Stories

### Story 1: Frontend Types & API Client

**Changes:**
1. Add types to `types.ts`:
   ```ts
   interface ProjectLanguage {
     id: string
     projectId: string
     bcp47Code: string
     isSource: boolean
     isActive: boolean
   }

   interface LanguageTask {
     id: string
     contentItemId: string
     languageCode: string
     assigneeEmail: string
     translationText: string
     previousApprovedTranslation: string
     isOutdated: boolean
     dueUtc: string | null
     status: string  // todo | pending_review | approved | done | outdated
   }

   interface TranslationSuggestion {
     hasSuggestion: boolean
     suggestion: { id: string; translationText: string; createdUtc: string } | null
   }

   interface LocalizationGridRow {
     itemId: string
     itemKey: string
     source: string
     sourceStatus: string
     targets: Array<{ language: string; status: string; assigneeEmail: string; dueUtc: string | null }>
     hasMissing: boolean
     hasOutdated: boolean
     hasReview: boolean
   }

   interface LocalizationGridResponse {
     total: number
     page: number
     pageSize: number
     rows: LocalizationGridRow[]
   }
   ```

2. New `languagesClient.ts`:
   - `list(projectId)` → `GET /api/project-languages?projectId=X`
   - `add(projectId, bcp47Code, isSource?)` → `POST /api/project-languages`
   - `toggleActive(id, isActive)` → `PUT /api/project-languages/{id}/active`
   - `changeSource(projectId, bcp47Code)` → `POST /api/project-languages/source-language?projectId=X`

3. New `translationClient.ts`:
   - `getTasks(contentItemId)` → `GET /api/language-tasks?contentItemId=X`
   - `getSuggestion(contentItemId, languageCode)` → `GET /api/language-tasks/suggestions?...`
   - `upsert(task)` → `POST /api/language-tasks`
   - `applyMemory(contentItemId, languageCode, acceptSuggestion, manualText?)` → `POST /api/language-tasks/apply-memory`
   - `getGrid(projectId, opts?)` → `GET /api/localization-grid?...`
   - `exportNeutral(projectId)` → `GET /api/exports/neutral?projectId=X`

**Acceptance:**
- Types compile
- `npm run build` passes

### Story 2: Language Management Panel

**Changes:**
1. New component `components/projects/LanguageManager.vue`:
   - Shows list of project languages with BCP-47 code, source badge, active toggle
   - "Add Language" button opens inline form with BCP-47 input
     - Label: "Language code" / Hint: "BCP-47 format, e.g. en-US, fr, de-DE, ja"
   - Each language row shows: flag emoji (derive from code), code, "Source" badge if isSource, active toggle
   - Context actions: Set as Source, Deactivate/Activate, Remove
   - Source language highlighted distinctly
   - Common presets dropdown: English (en), French (fr), German (de), Spanish (es), Japanese (ja), Chinese (zh), Korean (ko), Portuguese (pt), Italian (it), Arabic (ar), etc.

2. Integrate into project settings or content page sidebar

**Acceptance:**
- Can add/remove languages
- Can toggle active
- Can change source language
- Follows Mason's UX: helper text under labels, not placeholders
- `npm run build` passes

### Story 3: Localization Grid View

**Changes:**
1. New page or view component `components/projects/LocalizationGrid.vue`:
   - Table/grid: rows = content keys, columns = source + each active target language
   - Each cell shows translation status with color coding:
     - `todo` / missing → grey/empty indicator
     - `pending_review` → yellow/amber
     - `approved` / `done` → green
     - `outdated` → orange warning
   - Source column shows the source text
   - Target columns show translation text (truncated) or status indicator
   - Click on a target cell opens translation editor (Story 4)
   - Filters: status filter (missing/outdated/review), search by key
   - Pagination using grid API (page, pageSize)
   - Column headers show language code + completion percentage

2. Update content page to offer grid view toggle (list vs grid)

**Acceptance:**
- Grid renders keys × languages
- Status colors are correct
- Pagination works
- Filter by missing/outdated/review works
- `npm run build` passes

### Story 4: Translation Editor

**Changes:**
1. New component `components/projects/TranslationEditor.vue`:
   - Slide-out panel or modal when clicking a grid cell
   - Shows: key name, source text (read-only, for reference), target language
   - Translation textarea for writing/editing the translation
   - TM suggestion banner: if `hasSuggestion`, show "Translation memory suggestion available" with the suggested text and "Apply" button
   - Status selector: draft → pending_review → approved
   - Save button: calls `POST /api/language-tasks` (upsert)
   - Shows previous approved translation if exists (for comparison when source changed)
   - "Outdated" warning banner when `isOutdated` is true with the previous source text

2. Keyboard shortcut: Ctrl+Enter to save and move to next key

**Acceptance:**
- Can write and save translations
- TM suggestions display and can be applied
- Status updates correctly
- Outdated warning shows when relevant
- `npm run build` passes

### Story 5: Export UI

**Changes:**
1. New component `components/projects/ExportPanel.vue`:
   - "Export" button/section on content page
   - Export options:
     - Format: JSON (i18next style), Flat JSON, Nested JSON
     - Language: All / specific language
     - Namespace filter (optional)
   - Preview of export structure before download
   - Download button generates the file(s)
   - Shows how to consume in popular frameworks:
     ```
     // i18next
     import en from './locales/en.json'
     i18next.init({ resources: { en: { translation: en } } })

     // vue-i18n
     import { createI18n } from 'vue-i18n'
     import en from './locales/en.json'
     const i18n = createI18n({ locale: 'en', messages: { en } })

     // react-intl / next-intl
     import messages from './locales/en.json'
     ```
   - Copy API endpoint URL for CI/CD integration

**Acceptance:**
- Can export per-language JSON files
- Can export all languages at once
- Download works
- Integration snippets display correctly
- `npm run build` passes

### Story 6: A11y Review

- Grid keyboard navigation (arrow keys between cells)
- Translation editor focus trap when open
- Screen reader announces translation status per cell
- Language manager: ARIA for toggle switches
- Export panel: accessible download buttons
- Color coding has text/icon accompaniment (not color-only)
- Skip-link target maintained

### Story 7: QA Smoke Tests

- Add language to project → appears in grid columns
- Add translation for key+language → shows in grid cell
- TM suggestion appears for matching source text
- Export produces valid JSON with correct structure
- Grid pagination with >20 keys
- Filter by missing/outdated/review
- Source language change propagates correctly
- `npm run build` passes
- `dotnet build` + `dotnet test` pass (no backend changes expected, but verify)

## Delivery Rules

- Strict phase order: Types/Client → Language Manager → Grid → Translation Editor → Export → A11y → QA
- Small commits to `main`
- Report after each story: changed files, validations, commit hash
- Raise blockers immediately

## UX Defaults

- Helper text under labels (not placeholders) per Mason's preference
- Grid cells show truncated translation text + status icon (not just color)
- Language codes shown as BCP-47 with human-readable name where available
- Responsive: grid scrolls horizontally on narrow viewports
- Dark mode support via existing CSS variables
- No markdown tables in Discord — use bullet lists for status updates

## Integration Context (for devs consuming this)

The export format follows `neutral.v1` schema:
```json
{
  "schema": "neutral.v1",
  "projectId": "...",
  "language": "en",
  "files": {
    "common": { "welcome": "Welcome", "goodbye": "Goodbye" },
    "auth": { "login.title": "Sign In", "login.subtitle": "..." }
  }
}
```

Keys use dot-notation namespacing: `auth.login.title` → namespace `auth`, key `login.title`.
This maps directly to i18next namespace structure and can be flattened for flat-key libs.
