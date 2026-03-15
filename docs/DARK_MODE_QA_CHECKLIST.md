# Dark Mode QA Checklist (Orchestra QA Engineer)

## Required Route Coverage
- `/`
- `/login`
- `/register`
- `/onboarding/organisation`
- `/app/dashboard`
- `/app/projects`
- `/app/content`
- `/app/review`
- `/app/integrations`
- `/app/settings`
- `/app/settings/members`

## Required Component Coverage
- App shell (sidebar, header, breadcrumbs)
- Landing sections (hero, features, benefits, CTA band, footer)
- Form controls (input/select/button/checkbox)
- Data display (tables/cards/empty states/skeletons)
- Modal/dialog surfaces
- Alerts/errors/notices

## State Coverage
- default
- hover
- focus-visible
- active
- disabled
- error

## Pass/Fail Rules
- No hardcoded light-only neutrals for core UI surfaces/text.
- No unreadable text-background combinations.
- Borders must remain perceivable on dark surfaces.
- Theme toggle must be visible and usable.
- Skip-link + `#main-content` flow must remain valid.

## Automated Validation
- `npm run build`
- `npx playwright test tests/e2e/dark-mode.spec.ts`
- Add/maintain additional dark-mode assertions as routes/components evolve.
