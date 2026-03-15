# Dark Mode BA Requirements (Orchestra BA)

Use this template language in stories to prevent dark-mode regressions.

## Mandatory Requirements Block
1. Support **Light / Dark / System** behavior.
2. All new/changed UI must use semantic tokens (no hardcoded neutral hex values for core surfaces/text).
3. Story scope must explicitly include:
   - Layouts touched
   - Pages touched
   - Shared components touched
   - UI states (hover/focus/disabled/error)
4. Accessibility requirement:
   - focus-visible indicators preserved
   - keyboard navigation and skip-link behavior preserved
5. Definition of Done:
   - dark-mode parity verified by QA checklist and automation lane

## Non-Functional Acceptance Criteria
- Visual consistency across full app route map
- Readable contrast for text, controls, borders, and status states
- No theme flash/regression between route transitions

## Hand-off to QA
Every BA story must include a direct link/reference to:
- `docs/DARK_MODE_QA_CHECKLIST.md`
