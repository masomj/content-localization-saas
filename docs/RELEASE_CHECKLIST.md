# Release Readiness Checklist

## Pre-Release Validation

### Frontend
- [ ] `npm run test:unit` passes (Vitest unit tests)
- [ ] `npm run test:e2e` passes (Playwright e2e tests)
- [ ] `npm run build` succeeds without errors
- [ ] No TypeScript errors (`npx nuxt typecheck` if available)
- [ ] No lint errors (if lint script available)

### Backend
- [ ] `dotnet build` succeeds for all projects
- [ ] Unit tests pass (`dotnet test`)
- [ ] Integration tests pass (if environment available)

### E2E Test Coverage
- [ ] Landing page renders correctly
- [ ] Login form validation works
- [ ] Register form validation works
- [ ] Protected routes redirect unauthenticated users
- [ ] Full user journey works: register → onboarding → dashboard → members
- [ ] App navigation (sidebar) works
- [ ] App 404 page renders for unknown routes

### Security
- [ ] No secrets/credentials in code
- [ ] Environment variables properly documented
- [ ] Auth token storage uses secure practices

### Documentation
- [ ] Route map is up-to-date
- [ ] User journey documented
- [ ] README reflects current state
- [ ] API contracts documented

## Known Limitations

- **SSR Auth State**: Auth state from localStorage is not available during SSR. Tests use `addInitScript` to set auth state before page interaction.
- **Demo Mode**: Frontend currently runs in demo/fallback mode with mock authentication.
- **Backend Integration**: Members page API calls require backend to be running.

## Version Info

- Frontend: Nuxt 4.x
- Backend: ASP.NET Core 10
- Tests: Playwright (e2e), Vitest (unit), xUnit (backend)
