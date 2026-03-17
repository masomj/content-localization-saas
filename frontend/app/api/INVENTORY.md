# Frontend Backend API Inventory

## Auth
- `POST /auth/login` (`useAuth.login`)
- `POST /auth/register` (`useAuth.register`)
- `GET /auth/me` (`useAuth.bootstrapSession`, `useAuth.refreshUser`)
- `POST /auth/logout` (`useAuth.logout`)
- `POST /auth/forgot-password` (`useAuth.requestPasswordReset`)
- `POST /auth/reset-password` (`reset-password.vue`)

## Projects
- `GET /projects?workspaceId=...` (`projectsClient.list`, projects/content pages)
- `POST /projects` (`projectsClient.create`, projects page)

## Collections (under projects)
- `GET /projects/{projectId}/collections` (`projectsClient.listCollections`)
- `POST /projects/{projectId}/collections` (`projectsClient.createCollection`)
- `PUT /projects/{projectId}/collections/{collectionId}/rename` (`projectsClient.renameCollection`)
- `PUT /projects/{projectId}/collections/{collectionId}/move` (`projectsClient.moveCollection`)

## Content Items
- `GET /content-items?projectId=...` (`contentClient.list`, content page)
- `POST /content-items` (`contentClient.create`, content page)

## Admin / Members & Invites
- `GET /api/admin/members` (`adminClient.listMembers`, `useMembers.fetchMembers`)
- `DELETE /api/admin/members` (`adminClient.removeMember`, `useMembers.removeMember`)
- `GET /api/admin/invites` (`adminClient.listInvites`, `useMembers.fetchInvites`)
- `POST /api/admin/invites` (`adminClient.inviteMember`, `useMembers.inviteMember/resendInvite`)
- `POST /api/admin/invites/revoke` (`adminClient.revokeInvite`, `useMembers.revokeInvite`)
- `POST /api/admin/invites/change-role` (`adminClient.changeRole`, `useMembers.changeRole`)
- `GET /api/admin/membership-audit` (`adminClient.membershipAudit`, MembershipAuditPanel)
