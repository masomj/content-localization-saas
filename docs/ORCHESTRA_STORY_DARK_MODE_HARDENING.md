# Orchestra Story: Dark Mode Visual Regression Hardening

## Context
Dark mode was implemented quickly across layouts. Subsequent feedback indicates the app view appears visually broken in dark mode.

## Problem Statement
Existing component styles still rely heavily on `--color-gray-*` tokens. Dark mode currently overrides only semantic tokens (`--color-background`, `--color-surface`, etc.), causing mixed contrast and inconsistent rendering across the app shell and pages.

## Goal
Stabilize dark mode so the app is consistently readable and visually coherent without introducing per-component one-off fixes.

## Scope
- Update dark-theme token overrides in `frontend/app/assets/css/tokens.css`
- Preserve existing structure and a11y patterns
- Avoid risky layout rewrites in this pass

## Acceptance Criteria
1. In dark mode, all top-level layouts (`app`, `public`, `auth`) remain readable and visually coherent.
2. Landing, onboarding, auth, and in-app pages avoid light-only hardcoded colors for text/background/borders.
3. Shared components (UI + landing + app shell) use semantic/tokenized colors that adapt to theme.
4. Focus indicators and keyboard navigation remain visible in dark mode.
5. Build succeeds.

## BA Requirement Translation Update (mandatory for future stories)
For any new UI story, BA must include:
- **Theme behavior matrix:** Light, Dark, System modes.
- **Tokenization requirement:** No hardcoded neutral hex colors (`#111827`, `#f9fafb`, `#ffffff`, etc.) for general UI surfaces/text.
- **Scope matrix:** Layouts, pages, shared components, and state variants (hover/focus/disabled/error).
- **DoD clause:** Dark mode parity required before story can close.

## QA Engineer Update (mandatory checks)
QA must run this dark-mode checklist on every UI story:
- **Layouts:** `/`, `/login`, `/register`, `/onboarding/organisation`, `/app/*`
- **Components:** nav, cards, tables, forms, modals, alerts, buttons, inputs, selects, empty/skeleton states
- **States:** default, hover, focus-visible, active, disabled, error
- **Assertions:**
  - no unreadable text/background combos
  - no washed-out borders on dark surfaces
  - no white flash on route/theme switch
  - skip-link and `#main-content` behavior remains valid
- **Automation lane:** keep `npm run build` mandatory; add/maintain dark-mode e2e checks for key routes

## Orchestra Breakdown
- **BA:** enforce above requirement translation in stories.
- **Dev:** remove light-only hardcoded neutrals and use semantic/tokenized colors.
- **UI:** verify visual consistency across pages/components/layouts.
- **A11y:** verify contrast/focus visibility and keyboard flow.
- **QA:** execute checklist + validation lane.
- **Conductor:** report what changed + validation + commit hash.
