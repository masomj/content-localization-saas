# Content Localization SaaS (Monorepo)

Initial monorepo scaffold for a language-agnostic UX content + localization platform.

## Stack

- **Backend:** ASP.NET Core 10, EF Core 10, PostgreSQL, ASP.NET Identity, FluentValidation
- **Frontend:** Nuxt 4 (TypeScript)
- **Testing:** xUnit (unit + integration), Playwright (frontend e2e planned)
- **Local orchestration:** .NET Aspire (AppHost + ServiceDefaults)

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
