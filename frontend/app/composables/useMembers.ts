export interface Member {
  id: string
  workspaceId: string
  email: string
  role: string
  isActive: boolean
  createdUtc: string
}

export interface Invite {
  id: string
  workspaceId: string
  email: string
  role: string
  status: 'Pending' | 'Accepted' | 'Expired' | 'Revoked'
  expiresUtc: string
  createdUtc: string
}

export interface MemberWithInvite extends Member {
  pendingInvite?: Invite
}

const API_BASE = 'http://localhost:5000'

export function useMembers() {
  const config = useRuntimeConfig()
  const apiBase = config.public?.apiBase || API_BASE

  const members = ref<Member[]>([])
  const invites = ref<Invite[]>([])
  const isLoading = ref(false)
  const error = ref('')

  const roleOptions = [
    { value: 'Viewer', label: 'Viewer' },
    { value: 'Editor', label: 'Editor' },
    { value: 'Reviewer', label: 'Reviewer' },
    { value: 'Admin', label: 'Admin' },
  ]

  async function fetchMembers(workspaceId: string) {
    isLoading.value = true
    error.value = ''
    try {
      const data = await $fetch<Member[]>(`${apiBase}/api/admin/members`, {
        headers: { 'X-User-Role': 'Admin' },
      })
      members.value = data.filter(m => m.workspaceId === workspaceId)
    } catch (e) {
      error.value = 'Failed to load members'
      members.value = []
    } finally {
      isLoading.value = false
    }
  }

  async function fetchInvites() {
    try {
      const data = await $fetch<Invite[]>(`${apiBase}/api/admin/invites`, {
        headers: { 'X-User-Role': 'Admin' },
      })
      invites.value = data
    } catch (e) {
      error.value = 'Failed to load invites'
      invites.value = []
    }
  }

  async function inviteMember(workspaceId: string, email: string, role: string) {
    isLoading.value = true
    error.value = ''
    try {
      await $fetch(`${apiBase}/api/admin/invites`, {
        method: 'POST',
        headers: { 'X-User-Role': 'Admin' },
        body: { workspaceId, email, role },
      })
      await fetchInvites()
      return { success: true }
    } catch (e: any) {
      error.value = e.data?.error || 'Failed to send invite'
      return { success: false, error: error.value }
    } finally {
      isLoading.value = false
    }
  }

  async function resendInvite(workspaceId: string, email: string, role: string) {
    return inviteMember(workspaceId, email, role)
  }

  async function revokeInvite(workspaceId: string, email: string) {
    isLoading.value = true
    error.value = ''
    try {
      await $fetch(`${apiBase}/api/admin/invites/revoke`, {
        method: 'POST',
        headers: { 'X-User-Role': 'Admin' },
        body: { workspaceId, email },
      })
      await fetchInvites()
      return { success: true }
    } catch (e: any) {
      error.value = e.data?.error || 'Failed to revoke invite'
      return { success: false, error: error.value }
    } finally {
      isLoading.value = false
    }
  }

  async function changeRole(workspaceId: string, email: string, role: string) {
    isLoading.value = true
    error.value = ''
    try {
      await $fetch(`${apiBase}/api/admin/invites/change-role`, {
        method: 'POST',
        headers: { 'X-User-Role': 'Admin' },
        body: { workspaceId, email, role },
      })
      return { success: true }
    } catch (e: any) {
      error.value = e.data?.error || 'Failed to change role'
      return { success: false, error: error.value }
    } finally {
      isLoading.value = false
    }
  }

  async function removeMember(workspaceId: string, email: string) {
    isLoading.value = true
    error.value = ''
    try {
      await $fetch(`${apiBase}/api/admin/members`, {
        method: 'DELETE',
        headers: { 'X-User-Role': 'Admin' },
        body: { workspaceId, email },
      })
      return { success: true }
    } catch (e: any) {
      error.value = e.data?.error || 'Failed to remove member'
      return { success: false, error: error.value }
    } finally {
      isLoading.value = false
    }
  }

  return {
    members,
    invites,
    isLoading,
    error,
    roleOptions,
    fetchMembers,
    fetchInvites,
    inviteMember,
    resendInvite,
    revokeInvite,
    changeRole,
    removeMember,
  }
}
