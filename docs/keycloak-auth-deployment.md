# Keycloak OAuth/OIDC rollout strategy

## What changed
- InterCopy API now validates bearer JWTs issued by Keycloak (`Auth:Oidc:*` config).
- Aspire AppHost now runs Keycloak plus realm/theme import.
- Keycloak uses the same Aspire Postgres server as the app, but a separate database (`keycloak`).
- Frontend login now starts OIDC Authorization Code + PKCE flow and stores access/refresh tokens in cookies.
- Custom Keycloak login theme added at `keycloak/themes/InterCopy`.

## Local dev runbook (Aspire)
1. Set admin password secret (user-secrets or env):
   - `Parameters__keycloak-admin-password=<strong-dev-password>`
2. Start AppHost:
   - `dotnet run --project api/ContentLocalizationSaaS.AppHost`
3. Access:
   - Frontend: `http://localhost:3000`
   - Keycloak admin: `http://localhost:8080/admin`
4. Realm import:
   - `keycloak/realm/InterCopy-realm.json` is imported automatically on start.
5. Theme wiring:
   - Realm sets `loginTheme=InterCopy`.
   - Theme files are bind-mounted from `keycloak/themes`.

## Dev compatibility/migration notes
- Existing Identity users/passwords in app DB are **not** used for login anymore.
- Existing workspace data remains intact.
- Membership linking now uses token email -> `WorkspaceMembership.Email`.
- For local dev, easiest path is to recreate users directly in Keycloak realm.
- If old user emails must preserve workspace access, create Keycloak users with same email values.

## Production topology recommendation
- Run Keycloak as a dedicated service (container set or managed Kubernetes deployment).
- Use dedicated Postgres instance for Keycloak in prod (recommended), or strict schema/db isolation if shared.
- Place Keycloak behind TLS reverse proxy (`https://auth.<domain>`).
- API and frontend should trust only TLS issuer URL.

### Required prod env vars (examples)
API:
- `Auth__Oidc__Issuer=https://auth.example.com/realms/InterCopy`
- `Auth__Oidc__Audience=InterCopy-web`
- `Auth__Oidc__RequireHttpsMetadata=true`

Frontend:
- `NUXT_PUBLIC_KEYCLOAK_URL=https://auth.example.com`
- `NUXT_PUBLIC_KEYCLOAK_REALM=InterCopy`
- `NUXT_PUBLIC_KEYCLOAK_CLIENT_ID=InterCopy-web`

Keycloak:
- `KC_DB=postgres`
- `KC_DB_URL=jdbc:postgresql://<host>:5432/keycloak`
- `KC_DB_USERNAME=<user>`
- `KC_DB_PASSWORD=<secret>`
- `KEYCLOAK_ADMIN=<admin-user>`
- `KEYCLOAK_ADMIN_PASSWORD=<admin-password>`
- `KC_PROXY_HEADERS=xforwarded`
- `KC_HOSTNAME=auth.example.com`
- `KC_HTTP_ENABLED=false`

## CI/CD and rollback
- Keep realm config (`keycloak/realm/InterCopy-realm.json`) versioned.
- Validate on CI:
  1. backend build
  2. frontend unit tests + build
- Theme rollout: publish image/artifact including `keycloak/themes/InterCopy`.
- Rollback plan:
  1. redeploy previous API/frontend artifact,
  2. keep Keycloak realm backward-compatible (do not remove existing roles/clients abruptly),
  3. if needed restore realm export backup.

## Backups and scaling
- Back up Keycloak DB daily and before realm/client changes.
- Export realm JSON on each release (`kcadm`/admin API) and store in artifact retention.
- Scale Keycloak horizontally only with shared DB + sticky sessions or external Infinispan per official guidance.
- Monitor `/health` and token endpoint latency.
