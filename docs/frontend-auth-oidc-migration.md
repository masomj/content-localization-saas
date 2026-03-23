# Frontend auth migration: custom OIDC -> `oidc-client-ts`

## Library choice

- **Selected:** [`oidc-client-ts`](https://github.com/authts/oidc-client-ts)
- **Why:**
  - widely used OIDC/OAuth 2.0 SPA client
  - actively maintained successor to `oidc-client`
  - works cleanly in Nuxt 4 client-side composables/plugins
  - manages redirect login/logout, callback validation, token lifecycle and storage primitives

## Flow mapping (old -> new)

- **PKCE + authorize URL assembly (manual in `useAuth.login`)**
  - replaced by `UserManager.signinRedirect()` via `startOidcLogin`
- **Callback `code/state` validation + token exchange (manual in `useAuth.handleCallback`)**
  - replaced by `UserManager.signinCallback()` via `completeOidcCallback`
- **Token refresh (manual Keycloak token endpoint/fallback refresh endpoint)**
  - replaced by `UserManager.signinSilent()` via `attemptTokenRefresh`
- **Logout redirect assembly (manual Keycloak logout URL)**
  - replaced by `UserManager.signoutRedirect()` via `startOidcLogout`

## Product behavior preserved

- login still redirects to Keycloak
- callback page still finalizes session then routes to dashboard
- logout still clears app session and redirects through IdP logout
- onboarding gate still enforced by `auth.global.ts` (`/onboarding/organisation`)
- org selection/switching remains app-side (`switchWorkspace` + persisted selected org)

## `ui_theme` handling

- preserved through `signinRedirect({ extraQueryParams: { ui_theme } })`
- theme source remains identical (`data-theme` -> `locflow-theme` -> system preference)

## Migration impacts

- frontend now depends on `oidc-client-ts`
- custom PKCE/state/token exchange code removed from app auth composable
- refresh token storage in app client removed from primary flow; access token cookie/local/session behavior preserved for API and SSR route protection

## Runtime config / env vars

No new env vars required. Existing values are reused:

- `NUXT_PUBLIC_KEYCLOAK_URL`
- `NUXT_PUBLIC_KEYCLOAK_REALM`
- `NUXT_PUBLIC_KEYCLOAK_CLIENT_ID`

