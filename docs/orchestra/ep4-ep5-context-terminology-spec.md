# EP4 + EP5 Implementation Spec — BA Phase

## Implementation Order (dependency-driven)

### Batch 1 — Foundations (no dependencies)
1. **EP4-S5: Context Metadata Fields** (S) — add description, maxLength, contentType to ContentItem
2. **EP5-S1: Glossary / Termbase** (L) — new entities, CRUD, term suggestions, TBX/CSV import/export

### Batch 2 — Core Features
3. **EP5-S3: Forbidden Terms** (S) — shares glossary storage, adds forbidden flag + replacement + severity
4. **EP5-S2: Style Rules Engine** (M) — rule definitions, regex matching, override flags
5. **EP4-S1: Screenshot Upload + OCR Tagging** (L) — screenshot storage, OCR via Tesseract.js, bounding boxes, auto-link

### Batch 3 — Dependent Features
6. **EP4-S2: In-Context Editor** (L) — visual overlay editor on screenshots
7. **EP4-S3: Visual Context in Review** (S) — screenshot panel in review mode
8. **EP4-S4: Figma Screenshot Sync** (L) — Figma REST API screenshot capture + text layer linking

### Batch 4 — Analytics & AI
9. **EP5-S4: AI-Assisted Tone Check** (L) — LLM integration, async tone analysis
10. **EP5-S5: Governance Dashboard** (M) — metrics, drill-downs, CSV/PDF export

---

## EP4-S5: Context Metadata Fields

### Backend Changes

**Entity changes** — add to `ContentItem`:
```csharp
public string Description { get; set; } = string.Empty;       // up to 500 chars
public int? MaxLength { get; set; }                            // character limit (warning, not block)
public string ContentType { get; set; } = string.Empty;        // "button_label", "error_message", "heading", "body_text", "placeholder", "tooltip"
```

**Migration**: `AddContextMetadataToContentItem`

**API changes** — update `ContentItemsController`:
- PUT `/api/content-items/{id}` — accept description, maxLength, contentType in body
- GET responses include new fields
- POST create also accepts new fields

**Export/Import** — update neutral export to include context fields in payload:
```json
{
  "key": "auth.login.title",
  "source": "Sign In",
  "context": {
    "description": "Main login page heading",
    "maxLength": 30,
    "contentType": "heading"
  }
}
```

### Frontend Changes

**Content side panel** — add context metadata section below source text:
- Description textarea (500 char limit)
- Max length number input
- Content type dropdown (predefined values)
- Show character count warning in translation editor when exceeding maxLength

**Translation editor** — show max length warning:
- Yellow border + "X/Y characters" when approaching limit
- Red border + warning text when exceeding

### Tests
- Unit: entity validation, export includes context
- Integration: CRUD with context fields
- Frontend: max length warning display

---

## EP5-S1: Glossary / Termbase

### Backend — New Entities

```csharp
public sealed class Glossary
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class GlossaryTerm
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GlossaryId { get; set; }
    public required string SourceTerm { get; set; }          // e.g. "Dashboard"
    public string Definition { get; set; } = string.Empty;
    public bool IsForbidden { get; set; }                     // shared with EP5-S3
    public string ForbiddenReplacement { get; set; } = string.Empty;
    public bool CaseSensitive { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class GlossaryTermTranslation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GlossaryTermId { get; set; }
    public required string LanguageCode { get; set; }
    public required string TranslatedTerm { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
```

### Backend — Controllers

**GlossariesController** (`/api/glossaries`):
- GET `/api/glossaries` — list for workspace
- POST `/api/glossaries` — create
- PUT `/api/glossaries/{id}` — update name/description
- DELETE `/api/glossaries/{id}` — delete (cascade terms)

**GlossaryTermsController** (`/api/glossaries/{glossaryId}/terms`):
- GET — list terms (paginated, searchable)
- POST — create term + translations
- PUT `/{termId}` — update
- DELETE `/{termId}` — delete
- POST `/import/csv` — CSV import
- POST `/import/tbx` — TBX import
- GET `/export/csv` — CSV export
- GET `/export/tbx` — TBX export

**GlossarySuggestionsController** (`/api/glossary-suggestions`):
- POST — given source text + language, return matching terms with translations (for editor integration)
- Must respond in ≤500ms for 100K terms (use indexed full-text search or pre-computed trie)

### Frontend

**New page**: `/app/settings/glossary` (or `/app/glossary`)
- List glossaries for workspace
- CRUD glossary
- Term list with search, pagination
- Add/edit term modal (source term, definition, translations per language, case-sensitive toggle)
- Import CSV/TBX buttons
- Export CSV/TBX buttons

**Translation editor integration**:
- When translator focuses a translation field, fetch suggestions for source text
- Show matched glossary terms as chips/badges below the editor
- Click to insert approved translation

### Tests
- Unit: term matching algorithm, TBX parsing, CSV parsing
- Integration: CRUD, import, export round-trip
- Performance: 100K terms suggestion ≤500ms

---

## EP5-S3: Forbidden Terms

Extends glossary infrastructure (GlossaryTerm.IsForbidden).

### Backend
- Filter endpoint: GET `/api/glossaries/{id}/terms?forbidden=true`
- Check endpoint: POST `/api/forbidden-check` — given text + language, return forbidden matches
- When saving translation, if forbidden term detected → set `RequiresReview = true` on language task

### Frontend
- Forbidden terms tab in glossary UI (filter view)
- Add term with "Forbidden" toggle + replacement suggestion
- In translation editor: real-time check on input, show error-level warning with replacement
- CSV import for bulk forbidden terms

---

## EP5-S2: Style Rules Engine

### Backend — New Entities

```csharp
public sealed class StyleRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public required string Name { get; set; }              // "Sentence case for buttons"
    public required string RuleType { get; set; }          // "case_check", "regex", "no_trailing_punctuation", "max_words"
    public string Pattern { get; set; } = string.Empty;    // regex pattern (for regex type)
    public string Scope { get; set; } = string.Empty;      // content type filter: "button_label", or empty = all
    public string Message { get; set; } = string.Empty;    // warning message to show
    public bool IsActive { get; set; } = true;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class StyleOverride
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContentItemLanguageTaskId { get; set; }
    public Guid StyleRuleId { get; set; }
    public required string OverriddenByEmail { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
```

### Backend — Controller

**StyleRulesController** (`/api/projects/{projectId}/style-rules`):
- CRUD for rules
- POST `/api/style-check` — given text + project + content type, return violations

### Frontend
- Style rules page under project settings
- Rule builder: name, type dropdown, pattern (for regex), scope, message
- In translation editor: evaluate on save, show warnings
- Override button: saves with style override flag
- In review mode: show override badges

---

## EP4-S1: Screenshot Upload + OCR Tagging

### Backend — New Entities

```csharp
public sealed class Screenshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public required string FileName { get; set; }
    public required string StoragePath { get; set; }       // local file path or blob URL
    public string MimeType { get; set; } = "image/png";
    public long FileSizeBytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string OcrStatus { get; set; } = "pending";     // pending, processing, completed, failed
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ScreenshotRegion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ScreenshotId { get; set; }
    public Guid? ContentItemId { get; set; }               // linked key (null if unmatched)
    public required string DetectedText { get; set; }
    public double X { get; set; }                          // bounding box
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double Confidence { get; set; }                 // OCR confidence 0-1
    public bool IsManualLink { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
```

### Backend — Controllers

**ScreenshotsController** (`/api/projects/{projectId}/screenshots`):
- POST — upload (multipart form, max 10MB, PNG/JPG/WebP)
- GET — list screenshots for project
- GET `/{id}` — get screenshot with regions
- DELETE `/{id}` — delete screenshot + regions
- POST `/{id}/process` — trigger OCR (or auto-trigger on upload)

**ScreenshotRegionsController**:
- PUT `/{regionId}/link` — manually link region to content item
- PUT `/{regionId}/unlink` — remove link

### OCR Strategy
- **MVP**: Use Tesseract via `Tesseract.NET` NuGet package (server-side, no external API dependency)
- Process in background (fire-and-forget or queue)
- After OCR: fuzzy-match detected text against project's content item source texts
- Auto-link when confidence > 0.8 AND text match > 0.85

### Storage
- MVP: local disk storage under `wwwroot/screenshots/` (or configurable path)
- Future: Azure Blob / S3

### Frontend
- Screenshots tab on project page
- Upload dropzone (drag & drop, max 10MB)
- Screenshot viewer with bounding box overlays
- Click unmatched region → search modal to link to key
- Visual indicators: green = auto-linked, yellow = unmatched, blue = manually linked

---

## Remaining stories (EP4-S2, S3, S4, EP5-S4, S5) follow similar pattern — specs will be written when Batch 2 completes.
