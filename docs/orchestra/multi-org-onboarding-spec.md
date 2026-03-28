# Multi-Org Onboarding + Membership (ORCHESTRA BA/Solution Summary)

## Problem Statement
After OIDC sign-in, users must establish an org context before entering the app. A user can belong to multiple organizations, and invites must support adding an existing account/email into additional organizations without breaking existing memberships.

## Implemented Decisions

1. **Org context is claimless and client-selected**
   - Active org is stored client-side (`InterCopy_organization`), sent on API calls as `X-Workspace-Id`.
   - Backend validates membership against this context for admin membership endpoints.

2. **First org bootstrap grants Admin**
   - New endpoint creates workspace + creator membership (`Role=Admin`) in one flow.

3. **Single account can belong to many orgs**
   - Existing `WorkspaceMembership` model (`WorkspaceId + Email` unique) already supports many memberships per email.

4. **Invite flow remains upsert-per-workspace**
   - Accepting invite upserts membership only for the invite’s workspace, preserving other workspace memberships.

## Backend API Changes

- `GET /api/auth/me`
  - Now returns:
    - `workspace` (active/selected context)
    - `workspaces[]` (all memberships for current user, each with org role)
    - `role` now resolves to active org role when available.
- `POST /api/auth/switch-workspace`
  - Validates caller is active member of requested workspace.
- `GET /api/workspaces/mine`
  - Lists organizations where current user has active membership.
- `POST /api/workspaces/bootstrap`
  - Creates workspace and bootstraps creator as `Admin` membership.

## Authorization / Context Enforcement

Scoped admin endpoints now enforce active workspace context + org-admin membership:
- `GET /api/admin/invites`
- `POST /api/admin/invites`
- `POST /api/admin/invites/revoke`
- `POST /api/admin/invites/change-role`
- `GET /api/admin/members`
- `DELETE /api/admin/members`
- `GET /api/admin/membership-audit`

Enforcement behavior:
- Requires `X-Workspace-Id` context (or explicit workspace query where supported).
- Actor must be active member in that workspace with `Role=Admin`.

## Frontend Changes

- `useAuth` now tracks `organizations[]` and adds `switchOrganization()`.
- Onboarding (`/onboarding/organisation`) now supports:
  - selecting an existing organization if memberships already exist,
  - creating a new organization via backend bootstrap endpoint.
- App layout adds organization selector when user has multiple orgs.
- API client auto-injects `X-Workspace-Id` from selected org.
- Admin members list now relies on server-side org scoping (no client-side post-filtering).

## Notes / Assumptions

- Existing OIDC flow is preserved.
- No DB schema migration required.
- Workspace naming in backend remains `Workspace` while UX text uses “Organization” for product language consistency.
