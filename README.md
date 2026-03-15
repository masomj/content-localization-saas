# Content Localization SaaS (Monorepo)

Initial monorepo scaffold for a language-agnostic UX content + localization platform.

## Stack

- **Backend:** ASP.NET Core 10, EF Core 10, PostgreSQL, ASP.NET Identity, FluentValidation
- **Frontend:** Nuxt 4 (TypeScript)
- **Testing:** xUnit (unit + integration), Playwright (frontend e2e), Vitest (unit)
- **Local orchestration:** .NET Aspire (AppHost + ServiceDefaults)

## Route Map

### Public Routes
| Path | Description |
|------|-------------|
| `/` | Landing page with features, benefits, pricing |
| `/login` | User login form |
| `/register` | User registration form |

### Auth Routes (Authenticated, no org)
| Path | Description |
|------|-------------|
| `/onboarding/organisation` | Organization creation (required before app access) |

### App Routes (Authenticated + org required)
| Path | Description |
|------|-------------|
| `/app` | Redirects to dashboard |
| `/app/dashboard` | Main dashboard with user/org info |
| `/app/projects` | Projects list |
| `/app/content` | Content management |
| `/app/review` | Translation review |
| `/app/integrations` | Integration settings |
| `/app/settings` | General settings |
| `/app/settings/members` | Team members management |

## User Journey

```
Landing (/): Features & CTA
    |
    v
Register (/register) --> Login (/login)
    |                       |
    v                       v
Onboarding (/onboarding/organisation)
    |
    v
App Dashboard (/app/dashboard)
    |
    +-- Projects (/app/projects)
    +-- Content (/app/content)
    +-- Review (/app/review)
    +-- Integrations (/app/integrations)
    +-- Settings (/app/settings)
          |
          +-- Members (/app/settings/members)
```

## Middleware Flow

1. **Public paths** (`/`, `/login`, `/register`): Allow access
2. **Onboarding paths** (`/onboarding/*`): Allow access (for users without org)
3. **App paths** (`/app/*`): Require authentication AND organization
4. If not authenticated: Redirect to `/login`
5. If authenticated but no org: Redirect to `/onboarding/organisation`

## Monorepo layout

```text
content-localization-saas/
  api/
    ContentLocalizationSaaS.slnx
    ContentLocalizationSaaS.Api/
    ContentLocalizationSaaS.Application/
    ContentLocalizationSaaS.Domain/
    ContentLocalizationSaaS.Infrastructure/
    ContentLocalizationSaaS.AppHost/
    ContentLocalizationSaaS.ServiceDefaults/
    ContentLocalizationSaaS.*.Tests/
  frontend/
    (Nuxt 4 app)
  docs/
    ARCHITECTURE.md
```

## Quick start

### Backend

```bash
cd api
# run API directly
 dotnet run --project ContentLocalizationSaaS.Api

# or run local stack (API + postgres + pgAdmin) via Aspire
 dotnet run --project ContentLocalizationSaaS.AppHost
```

### Frontend

```bash
cd frontend
npm install
npm run dev
```

### OpenTelemetry (debugging API issues)

- Service defaults already wire ASP.NET + HttpClient tracing/metrics/logging.
- Exceptions are now captured in telemetry spans for easier debugging.
- **Recommended local flow:** run via Aspire AppHost so telemetry is visible in the Aspire dashboard.

```bash
cd api
dotnet run --project ContentLocalizationSaaS.AppHost
```

- If running API standalone, set an OTLP exporter endpoint before start:

```bash
# PowerShell example
$env:OTEL_EXPORTER_OTLP_ENDPOINT="http://localhost:4317"
dotnet run --project ContentLocalizationSaaS.Api
```

## Current status

This is an initial architecture scaffold with:

- Clean architecture project split (Domain/Application/Infrastructure/API)
- Identity + EF Core + PostgreSQL wiring baseline
- FluentValidation baseline
- Aspire AppHost with Postgres
- starter xUnit tests (validator + API health integration)

See `docs/ARCHITECTURE.md` for the detailed draft architecture.

## API contracts

- Versioning policy: `docs/API_VERSIONING.md`
- Contract payload examples (success + error): `docs/API_CONTRACT_EXAMPLES.md`
- OpenAPI-focused canonical examples: `docs/OPENAPI_EXAMPLES.md`

## CLI

- Official CLI docs and CI usage: `docs/CLI.md`
- Integration contract/spec (Story 6.6): `docs/CLI_INTEGRATION.md`
- Machine-readable CLI spec endpoint: `GET /api/cli/spec`
- Build command:

```bash
cd api
 dotnet build ContentLocalizationSaaS.Cli/ContentLocalizationSaaS.Cli.csproj -c Release
```

## Integration test runtime note

Aspire integration tests require a healthy container runtime.

If integration tests fail with container-runtime health errors:

- verify Docker/Podman daemon is running and responsive
- run unit tests + Playwright as baseline while runtime issues are triaged
- use CI artifacts from the integration job for diagnostics (`integration-tests.log` + `.trx`)
