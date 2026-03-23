import { adminClient } from '~/api/adminClient'
import type { Invite, Member } from '~/api/types'

export type { Invite, Member }

export interface MemberWithInvite extends Member {
  pendingInvite?: Invite
}

export function useMembers() {
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

  async function fetchMembers() {
    isLoading.value = true
    error.value = ''
    try {
      const data = await adminClient.listMembers()
      members.value = data
    } catch {
      error.value = 'Failed to load members'
      members.value = []
    } finally {
      isLoading.value = false
    }
  }

  async function fetchInvites() {
    try {
      invites.value = await adminClient.listInvites()
    } catch {
      error.value = 'Failed to load invites'
      invites.value = []
    }
  }

  async function inviteMember(workspaceId: string, email: string, role: string) {
    isLoading.value = true
    error.value = ''
    try {
      await adminClient.inviteMember(workspaceId, email, role)
      await fetchInvites()
      return { success: true }
    } catch (e: any) {
      error.value = e?.message || 'Failed to send invite'
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
      await adminClient.revokeInvite(workspaceId, email)
      await fetchInvites()
      return { success: true }
    } catch (e: any) {
      error.value = e?.message || 'Failed to revoke invite'
      return { success: false, error: error.value }
    } finally {
      isLoading.value = false
    }
  }

  async function changeRole(workspaceId: string, email: string, role: string) {
    isLoading.value = true
    error.value = ''
    try {
      await adminClient.changeRole(workspaceId, email, role)
      return { success: true }
    } catch (e: any) {
      error.value = e?.message || 'Failed to change role'
      return { success: false, error: error.value }
    } finally {
      isLoading.value = false
    }
  }

  async function removeMember(workspaceId: string, email: string) {
    isLoading.value = true
    error.value = ''
    try {
      await adminClient.removeMember(workspaceId, email)
      return { success: true }
    } catch (e: any) {
      error.value = e?.message || 'Failed to remove member'
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
