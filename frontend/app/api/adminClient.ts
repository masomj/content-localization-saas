import { apiRequest } from '~/api/client'
import type { Invite, Member, MembershipAuditRow } from '~/api/types'

const adminHeaders = { 'X-User-Role': 'Admin' }

export const adminClient = {
  listMembers() {
    return apiRequest<Member[]>('/api/admin/members', { headers: adminHeaders })
  },
  listInvites() {
    return apiRequest<Invite[]>('/api/admin/invites', { headers: adminHeaders })
  },
  inviteMember(workspaceId: string, email: string, role: string) {
    return apiRequest<void>('/api/admin/invites', {
      method: 'POST',
      headers: adminHeaders,
      body: JSON.stringify({ workspaceId, email, role }),
    })
  },
  revokeInvite(workspaceId: string, email: string) {
    return apiRequest<void>('/api/admin/invites/revoke', {
      method: 'POST',
      headers: adminHeaders,
      body: JSON.stringify({ workspaceId, email }),
    })
  },
  changeRole(workspaceId: string, email: string, role: string) {
    return apiRequest<void>('/api/admin/invites/change-role', {
      method: 'POST',
      headers: adminHeaders,
      body: JSON.stringify({ workspaceId, email, role }),
    })
  },
  removeMember(workspaceId: string, email: string) {
    return apiRequest<void>('/api/admin/members', {
      method: 'DELETE',
      headers: adminHeaders,
      body: JSON.stringify({ workspaceId, email }),
    })
  },
  membershipAudit(query: { targetEmail?: string; action?: string; fromUtc?: string; toUtc?: string }) {
    const params = new URLSearchParams()
    if (query.targetEmail?.trim()) params.set('targetEmail', query.targetEmail.trim())
    if (query.action?.trim()) params.set('action', query.action.trim())
    if (query.fromUtc?.trim()) params.set('fromUtc', query.fromUtc.trim())
    if (query.toUtc?.trim()) params.set('toUtc', query.toUtc.trim())

    const qs = params.toString()
    return apiRequest<MembershipAuditRow[]>(`/api/admin/membership-audit${qs ? `?${qs}` : ''}`, { headers: adminHeaders })
  },
}
