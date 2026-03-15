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
1. In dark mode, primary app shell surfaces and text are legible.
2. Components using `--color-gray-*` variables render with dark-appropriate values.
3. Borders and interactive states remain visible.
4. Build succeeds.

## Orchestra Breakdown
- **BA:** Confirm root cause as token mismatch (semantic vs direct gray token usage).
- **Dev:** Introduce comprehensive dark token remapping for gray + semantic values.
- **UI:** Validate contrast consistency in shell/header/sidebar/form controls.
- **A11y:** Ensure focus outlines and readable contrast are preserved.
- **QA:** `npm run build` (and optional e2e lane as follow-up).
- **Conductor:** Report what changed + validation + commit hash.
