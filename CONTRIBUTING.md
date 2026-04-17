# CONTRIBUTING.md — InterCopy Codebase Conventions

> **⚠️ READ THIS FIRST** before writing any code. If you add a new component, composable, API client, or pattern — update this document before committing.

---

## Project Structure

```
frontend/
├── app/
│   ├── api/              # API client modules + shared types
│   ├── assets/css/       # Design tokens (tokens.css)
│   ├── components/       # Reusable Vue components
│   │   ├── ui/           # Generic UI primitives (Button, Input, Select, Card, etc.)
│   │   ├── landing/      # Marketing/landing page components
│   │   └── projects/     # Project-specific domain components
│   ├── composables/      # Shared reactive state (useAuth, useMembers, useTheme)
│   ├── layouts/          # Nuxt layouts (app, canvas, auth, docs, public)
│   ├── lib/              # Pure utility modules (oidc.ts)
│   ├── middleware/        # Route middleware (auth, admin, routeGuard)
│   ├── pages/            # File-based routing
│   └── plugins/          # Nuxt plugins (theme.client.ts)
api/
├── ContentLocalizationSaaS.Api/         # ASP.NET API (controllers, auth)
├── ContentLocalizationSaaS.Domain/      # Entities, enums
├── ContentLocalizationSaaS.Infrastructure/ # EF Core, services
└── ContentLocalizationSaaS.AppHost/     # Aspire orchestration
figma-plugin/                            # Figma plugin (TypeScript + esbuild)
keycloak/                                # Realm config + themes
```

---

## Frontend Conventions (Nuxt 4 + Vue 3)

### 1. UI Components — Use What Exists

Before creating ANY new component, check `components/ui/` first:

| Component | Location | Props | Use For |
|-----------|----------|-------|---------|
| `Button` | `ui/Button.vue` | `variant` (primary/secondary/ghost/danger), `size` (sm/md/lg), `loading`, `block`, `disabled` | All clickable actions |
| `Input` | `ui/Input.vue` | Standard input wrapper | Text inputs |
| `Select` | `ui/Select.vue` | Dropdown select wrapper | Dropdowns |
| `Card` | `ui/Card.vue` | Content container | Card-style containers |
| `FormError` | `ui/FormError.vue` | Error display | Form validation errors |
| `SectionContainer` | `ui/SectionContainer.vue` | Page section wrapper | Section layout |
| `ThemeToggle` | `ui/ThemeToggle.vue` | Theme switcher | Dark/light mode toggle |
| `AppBreadcrumbs` | `AppBreadcrumbs.vue` | Breadcrumb navigation | Page breadcrumbs |
| `AppEmptyState` | `AppEmptyState.vue` | Empty state display | "No items" states |
| `AppSkeleton` | `AppSkeleton.vue` | Loading skeleton | Loading placeholders |
| `InviteMemberModal` | `InviteMemberModal.vue` | Member invite modal | Invite flows |

**Rules:**
- **Never duplicate** a component that already exists — extend the existing one
- New generic UI components go in `components/ui/`
- Domain-specific components go in `components/{domain}/` (e.g. `components/projects/`)
- All components use `<script setup lang="ts">` with typed props via `defineProps<>()`

### 2. API Client Pattern

All API calls go through typed client modules in `api/`. **Never use raw `fetch()` or `$fetch()` in pages/components.**

**Base client:** `api/client.ts` provides:
- `apiRequest<T>(path, options)` — authenticated fetch with auto token refresh
- `ApiError` class for typed error handling
- Auto workspace ID injection via `X-Workspace-Id` header

**Adding a new API domain:**

```typescript
// api/myFeatureClient.ts
import { apiRequest } from '~/api/client'
import type { MyType } from '~/api/types'

export const myFeatureClient = {
  list(projectId: string) {
    return apiRequest<MyType[]>(`/my-feature?projectId=${encodeURIComponent(projectId)}`)
  },
  create(payload: CreateMyTypeRequest) {
    return apiRequest<MyType>('/my-feature', {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },
  // ... etc
}
```

**Existing API clients:**
- `adminClient` — admin operations
- `authClient` — authentication
- `componentsClient` — design components
- `contentClient` — content items (CRUD + move)
- `languagesClient` — project languages
- `libraryClient` — library components + variants
- `projectsClient` — projects
- `reviewClient` — content reviews
- `translationClient` — translations
- `versionsClient` — project versions
- `glossaryClient` — glossary/termbase CRUD, import/export, suggestions
- `styleRulesClient` — style rules CRUD, style checking

**All types live in `api/types.ts`** — add new interfaces/types there, not in client files or pages.

### 3. Composables

Shared reactive state uses composables in `composables/`:

- `useAuth()` — auth state, login/logout, workspace switching, role checks
- `useMembers()` — workspace member management
- `useTheme()` — dark/light mode toggle

**When to create a new composable:** When multiple pages/components need the same reactive state or logic. Single-use logic stays in the page.

### 4. Layouts

| Layout | File | Use For |
|--------|------|---------|
| `app` | `layouts/app.vue` | Standard app pages — sidebar nav + top header (breadcrumbs, user, theme) |
| `canvas` | `layouts/canvas.vue` | Full-viewport pages — sidebar nav only, no top header (e.g. component detail) |
| `auth` | `layouts/auth.vue` | Login/register/password pages |
| `docs` | `layouts/docs.vue` | Documentation pages |
| `public` | `layouts/public.vue` | Marketing/landing pages |

Set layout in page with:
```typescript
definePageMeta({ layout: 'app' }) // or 'canvas', 'auth', etc.
```

### 5. CSS & Styling

**Design tokens live in `assets/css/tokens.css`.** Use CSS variables everywhere:

```css
/* ✅ Correct */
color: var(--color-text-primary);
background: var(--color-surface);
padding: var(--spacing-4);
border-radius: var(--radius-md);
font-size: var(--font-size-sm);

/* ❌ Never */
color: #333;
background: white;
padding: 16px;
border-radius: 6px;
```

**Available token families:**
- `--color-primary-{50-900}` — brand indigo
- `--color-gray-{50-950}` — neutrals (auto-remap in dark mode)
- `--color-{background,surface,text-primary,text-secondary,text-muted,border}` — semantic
- `--color-{success,warning,error,info}` — status
- `--spacing-{0,1,2,3,4,5,6,8,10,12,16,20,24}` — spacing scale
- `--radius-{sm,md,lg,xl,2xl,full}` — border radius
- `--font-size-{xs,sm,base,lg,xl,2xl,3xl,4xl,5xl}` — typography
- `--font-weight-{normal,medium,semibold,bold}` — weights
- `--shadow-{sm,md,lg,xl}` — box shadows
- `--transition-{fast,normal,slow}` — transitions
- `--z-{dropdown,sticky,fixed,modal-backdrop,modal,popover,tooltip}` — z-index

**Dark mode:** Handled automatically via `html[data-theme='dark']` in `tokens.css`. The gray scale and semantic colours remap. Never add separate dark mode overrides in component styles unless truly necessary — if you need a dark variant, check if the token already handles it.

**Scoped styles:** Use `<style scoped>` in components. Page-level styles can be unscoped if needed for deep child targeting, but prefer scoped.

### 6. Forms & UX

- **Helper text goes under labels**, not in placeholders
  ```html
  <label for="myField" class="label-with-hint">
    <span>Field Name</span>
    <span class="label-hint">Helpful description here</span>
  </label>
  <input id="myField" v-model="value" type="text">
  ```
- **Never use `window.alert()`, `window.confirm()`, `window.prompt()`** — use styled modal components with dark mode support
- All modals use the `.modal-overlay` pattern with `color-mix` backdrop
- Prefer `<UiButton>` over raw `<button>` elements

### 7. Routing & Middleware

- Auth middleware (`auth.global.ts`) runs on every route — public paths are allowlisted
- Admin middleware (`admin.ts`) gates admin-only pages
- Route guards in `routeGuard.ts` for specific flows

**Public paths** (no auth required): `/`, `/login`, `/register`, `/forgot-password`, `/reset-password`, `/auth/callback`, `/docs/*`, `/onboarding/*`

### 8. File Naming

- **Pages:** kebab-case matching URL (`content/index.vue`, `forgot-password.vue`)
- **Components:** PascalCase (`AppBreadcrumbs.vue`, `TranslationEditor.vue`)
- **API clients:** camelCase with `Client` suffix (`contentClient.ts`)
- **Composables:** camelCase with `use` prefix (`useAuth.ts`)
- **Types:** PascalCase interfaces in `api/types.ts`

---

## Backend Conventions (.NET 10)

### Controller Pattern

Each domain gets its own controller in `Api/Controllers/`:
- Use `[Authorize]` attribute for protected endpoints
- Use `[RequireAppRole(AppRole.Admin)]` for admin-only endpoints
- Return typed DTOs, not entities
- Use `X-Workspace-Id` header for workspace scoping

### Entity Pattern

Entities in `Domain/` — EF Core configurations in `Infrastructure/`.

### Auth

- **Keycloak OIDC only** — ASP.NET Identity is DEPRECATED
- Do NOT use `SignInManager`, `UserManager`, or `IdentityUser`
- JWT validation against Keycloak issuer
- Workspace membership checked before JWT claims

---

## Figma Plugin Conventions

- Build: `cd figma-plugin && npm install && npm run build`
- esbuild target: **es2017** (Figma sandbox compatibility)
- Must use `catch(_)` NOT `catch {}` (Figma parser limitation)
- `networkAccess`: `"*"` in manifest.json
- localStorage is blocked — use `figma.clientStorage` bridge via main.ts↔ui.ts messaging
- Auth: Device Authorization Grant (RFC 8628) via Keycloak

---

## Commit Conventions

Format: `type(scope): description`

Types: `feat`, `fix`, `refactor`, `docs`, `style`, `test`, `chore`

Scopes: `components`, `content`, `auth`, `api`, `plugin`, `docs`, `ci`

Examples:
```
feat(components): add drag-to-pan on canvas
fix(auth): handle token refresh race condition
refactor(content): extract folder tree into composable
```

---

## ⚠️ Keeping This Document Current

**If you add, rename, or remove any of the following — update this file before committing:**

- A shared component in `components/ui/` or `components/`
- An API client in `api/`
- A composable in `composables/`
- A layout in `layouts/`
- A design token in `tokens.css`
- A middleware in `middleware/`
- A major pattern change

**This document is the single source of truth for codebase conventions.** If the code disagrees with this doc, update whichever is wrong.
