# ORCHESTRA Spec: Device Authorization Flow for Plugin Auth (RFC 8628)

## Summary

Implement OAuth 2.0 Device Authorization Grant (RFC 8628) for Figma plugin authentication. Users authenticate via their browser (Keycloak login) and the plugin receives a full OIDC access token — same identity, same permissions, same workspace access as the webapp.

## How It Works

```
Plugin                          API                         Keycloak                    Browser
  |                              |                            |                          |
  |-- POST /device-auth/start -->|                            |                          |
  |                              |-- POST /device/authorize ->|                          |
  |                              |<-- device_code + user_code |                          |
  |<-- user_code + verify_url ---|                            |                          |
  |                              |                            |                          |
  | (shows: "Enter code ABCD    |                            |                          |
  |  at InterCopy.app/device")     |                            |                          |
  |                              |                            |   User opens browser      |
  |                              |                            |<--- enters code + login --|
  |                              |                            |--- approves + returns --->|
  |                              |                            |                          |
  | (polls every 5s)             |                            |                          |
  |-- POST /device-auth/poll --->|                            |                          |
  |                              |-- POST /token (device) --->|                          |
  |                              |<-- access_token + refresh  |                          |
  |<-- access_token + user info -|                            |                          |
  |                              |                            |                          |
  | (uses access_token as        |                            |                          |
  |  Authorization: Bearer)      |                            |                          |
```

## Phase 1: Keycloak — Device Auth Client

### New Keycloak client: `InterCopy-device`
Add to `keycloak/realm/InterCopy-realm.json`:
- clientId: `InterCopy-device`
- publicClient: true
- oauth2DeviceAuthorizationGrantEnabled: true
- standardFlowEnabled: false
- directAccessGrantsEnabled: false
- Set `oauth2.device.authorization.grant.enabled` to true in client attributes

### Verification URI
Keycloak provides the device verification endpoint at:
`{keycloak-url}/realms/InterCopy/device`

## Phase 2: Backend — Device Auth Proxy Endpoints

### Why a proxy?
The plugin can't call Keycloak directly (CORS restrictions in Figma's iframe sandbox). The API acts as a proxy.

### New `DeviceAuthController`

**`POST /api/device-auth/start`**
- No auth required (public endpoint)
- Body: `{ }`
- Server calls Keycloak: `POST {keycloak}/realms/InterCopy/protocol/openid-connect/auth/device`
  - Form params: `client_id=InterCopy-device`
- Returns to plugin: `{ userCode, verificationUri, verificationUriComplete, deviceCode, expiresIn, interval }`
- Stores `deviceCode` in a short-lived cache (memory or DB) mapped to a session ID

**`POST /api/device-auth/poll`**
- No auth required
- Body: `{ deviceCode }`
- Server calls Keycloak: `POST {keycloak}/realms/InterCopy/protocol/openid-connect/token`
  - Form params: `client_id=InterCopy-device`, `grant_type=urn:ietf:params:oauth:grant-type:device_code`, `device_code={deviceCode}`
- If pending: returns `{ status: "pending" }`
- If expired: returns `{ status: "expired" }`
- If success: Keycloak returns access_token + refresh_token
  - Decode the JWT to extract user info (email, name, sub)
  - Return: `{ status: "complete", accessToken, refreshToken, expiresIn, user: { email, name, sub } }`

**`POST /api/device-auth/refresh`**
- No auth required
- Body: `{ refreshToken }`
- Server calls Keycloak token endpoint with `grant_type=refresh_token`
- Returns new access_token + refresh_token

### Auth on other endpoints
- All existing API endpoints that use `RequireAppRole` already check JWT Bearer tokens
- The device auth flow returns a real Keycloak access token
- Plugin sends `Authorization: Bearer {accessToken}` on all requests
- The existing OIDC middleware validates it — **same identity as the webapp**

## Phase 3: Plugin — Device Auth UI

### Login flow
1. Plugin shows "Connect to InterCopy" button + API base URL input
2. User clicks "Connect"
3. Plugin calls `POST /api/device-auth/start`
4. Plugin shows: "Enter code **ABCD-EFGH** at **InterCopy.app/device**" with a "Copy code" button and "Open browser" link
5. User opens browser, logs into Keycloak, enters the code
6. Plugin polls `POST /api/device-auth/poll` every 5 seconds
7. When complete: stores access_token + refresh_token in localStorage
8. Shows the logged-in state with user name/email

### Token refresh
- Before each API call, check if token is expired (decode JWT exp claim)
- If expired, call `POST /api/device-auth/refresh` with the refresh token
- If refresh fails, show "Session expired, please reconnect"

### Logout
- Clear tokens from localStorage
- Show login screen

## Phase 4: Remove Deprecated Auth

### PluginAuthController
- Mark `POST /api/plugin-auth/login` as `[Obsolete]` with deprecation notice
- Remove `SignInManager` usage from this endpoint
- Keep the endpoint temporarily for backward compatibility but have it return 410 Gone

### Notes
- Do NOT remove ASP.NET Identity entirely yet (it's used for Keycloak's Entity Framework store)
- Mark all `SignInManager` / direct password validation as deprecated
- Future: migrate all auth to pure OIDC token validation

## Delivery Order

1. Phase 1: Keycloak realm JSON update (add InterCopy-device client)
2. Phase 2: Backend DeviceAuthController (3 endpoints)
3. Phase 3: Plugin UI rewrite for device auth flow
4. Phase 4: Deprecate old plugin auth

## Acceptance Criteria

- Plugin shows "Enter code XXXX" screen
- User enters code in browser → Keycloak login → approves
- Plugin receives access token → shows logged-in state
- Plugin can list projects, push/pull components using the token
- Same user identity as webapp (same email, same workspace access)
- `dotnet build` passes
- Plugin builds with esbuild
