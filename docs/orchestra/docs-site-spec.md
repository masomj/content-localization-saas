# ORCHESTRA Spec: Documentation Site (aspire.dev-style)

## Summary

Build a comprehensive documentation site for InterCopy, similar to aspire.dev. Lives under the main domain as subpages (`intercopy.co.uk/docs/*`) for SEO benefits and simpler DNS management.

## Architecture

The docs will be Nuxt Content pages within the existing frontend app — no separate site needed. This gives us:
- Same domain (intercopy.co.uk/docs/*)
- Same design system / dark mode
- SSR for SEO
- Markdown-based content (easy to maintain)

## Structure

```
/docs                          → Overview / Getting Started
/docs/getting-started          → Quick start guide
/docs/getting-started/quickstart → 5-min setup
/docs/getting-started/concepts  → Core concepts (projects, keys, translations)

/docs/webapp                   → Web Application
/docs/webapp/content           → Managing content (folders, keys, source text)
/docs/webapp/translations      → Languages, translation editor, grid
/docs/webapp/review            → PR-style review workflow
/docs/webapp/versions          → Project versioning (releases)
/docs/webapp/components        → Design components (Figma integration)
/docs/webapp/export            → Export formats (i18next, vue-i18n, react-intl)
/docs/webapp/admin             → Admin features (projects, members, RBAC)

/docs/figma-plugin             → Figma Plugin
/docs/figma-plugin/install     → Installation guide
/docs/figma-plugin/auth        → Device auth flow
/docs/figma-plugin/sync        → Push/pull frames
/docs/figma-plugin/edit        → Editing text in context

/docs/cli                      → CLI Tool
/docs/cli/install              → Installation
/docs/cli/configure            → Configuration (base URL, API token)
/docs/cli/pull                 → Pulling content bundles

/docs/api                      → API Reference
/docs/api/authentication       → Auth (OIDC, Bearer tokens)
/docs/api/content-items        → Content CRUD endpoints
/docs/api/languages            → Language management
/docs/api/export               → Export endpoints
/docs/api/components           → Design components API
/docs/api/webhooks             → Webhook subscriptions

/docs/integrations             → Framework Integrations
/docs/integrations/i18next     → i18next setup
/docs/integrations/vue-i18n    → vue-i18n setup
/docs/integrations/react-intl  → react-intl / next-intl setup
```

## Implementation

### Phase 1: Docs layout + navigation

1. Create a docs layout (`layouts/docs.vue`) with:
   - Left sidebar: navigation tree (collapsible sections)
   - Center: content area (markdown rendered)
   - Right sidebar: table of contents (auto-generated from headings)
   - Top: breadcrumbs
   - Responsive: sidebar collapses on mobile

2. Add "Docs" link to the main site navbar

### Phase 2: Core content pages

Create markdown/vue pages for each doc section. Content should be:
- Clear, concise, practical
- Code examples where relevant
- Screenshots/diagrams where helpful
- "Next steps" links at bottom of each page

### Phase 3: API reference

Auto-generate or manually document all API endpoints with:
- Method + URL
- Request/response examples
- Authentication requirements
- Error codes

## Style

- Match the existing InterCopy design system
- Dark mode support
- Code blocks with syntax highlighting
- Collapsible sections for long content
- Search (future enhancement)

## Delivery

Phase 1: Layout + navigation + Getting Started pages
Phase 2: Webapp docs (all sections)
Phase 3: Plugin + CLI + API docs
