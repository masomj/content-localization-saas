# Initial Architecture Draft

## 1) High-level architecture

- **Frontend:** Nuxt 4 app (`frontend/`)
- **Backend API:** ASP.NET Core 10 REST API (`api/ContentLocalizationSaaS.Api`)
- **Application layer:** use-cases, validation, contracts (`api/ContentLocalizationSaaS.Application`)
- **Domain layer:** core entities/value rules (`api/ContentLocalizationSaaS.Domain`)
- **Infrastructure layer:** EF Core, Identity, PostgreSQL adapters (`api/ContentLocalizationSaaS.Infrastructure`)
- **Local orchestration:** Aspire AppHost (`api/ContentLocalizationSaaS.AppHost`)

## 2) Monorepo structure

```text
api/
  ContentLocalizationSaaS.Api              # HTTP API (controllers/endpoints)
  ContentLocalizationSaaS.Application      # Commands/queries/validators
  ContentLocalizationSaaS.Domain           # Entities + domain primitives
  ContentLocalizationSaaS.Infrastructure   # DbContext, Identity, persistence adapters
  ContentLocalizationSaaS.ServiceDefaults  # Aspire defaults (observability, resilience)
  ContentLocalizationSaaS.AppHost          # Aspire host (postgres + api)
  ContentLocalizationSaaS.*.Tests          # xUnit unit/integration tests
frontend/
  Nuxt 4 app
docs/
  Architecture and product engineering docs
```

## 3) Primary design choices

1. **Language-agnostic localization model**
   - no hard-coded locale assumptions in backend model
   - language codes should support BCP-47

2. **Neutral i18n export format by default**
   - canonical JSON shape intended for easy adaptation to any i18n runtime
   - future: adapters for vue-i18n, i18next, FormatJS, etc.

3. **Validation on both client + API**
   - FluentValidation on backend request DTOs
   - mirrored frontend validation (Nuxt forms)

4. **Auth baseline with ASP.NET Identity**
   - roles aligned to PRD: Owner/Admin/Editor/Translator/Reviewer/Viewer

5. **Testing strategy**
   - unit tests for domain/application logic
   - integration tests for API behavior and auth/role boundaries
   - planned Playwright e2e for critical journeys

## 4) Initial data model (baseline)

- `Workspace`
- `Project`
- Identity tables (`AspNetUsers`, roles, claims, logins, tokens)

Planned next model additions:

- `ContentItem`
- `CopyComponent`
- `Translation`
- `ItemRevision`
- `CommentThread` / `Comment`
- `WebhookSubscription`

## 5) API boundaries (first increment)

- `/healthz`
- `/api/projects` (validated create baseline)

Planned next endpoints:

- workspaces CRUD + membership
- content items/components
- localization grid and translation updates
- export endpoints + webhook delivery

## 6) Local development

Aspire AppHost currently provisions:

- PostgreSQL container
- pgAdmin
- API project with DB reference

Future AppHost additions:

- frontend process orchestration
- optional Redis/queue for webhook retries and background jobs

## 7) Quality gates (DoD alignment)

Every story should satisfy:

- semantic HTML + keyboard accessibility + skip-link behavior
- responsive behavior across breakpoints
- client + API validation
- unit + integration tests; e2e where flow is user-critical
