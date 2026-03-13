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
