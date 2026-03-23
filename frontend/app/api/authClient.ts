import { apiRequest } from '~/api/client'
import type { User, Workspace } from '~/api/types'

export const authClient = {
  me() {
    return apiRequest<User>('/auth/me')
  },
  logout() {
    return apiRequest<void>('/auth/logout', { method: 'POST' })
  },
  listMyWorkspaces() {
    return apiRequest<Workspace[]>('/workspaces/mine')
  },
  createWorkspace(name: string) {
    return apiRequest<{ workspace: Workspace; role: string }>('/workspaces/bootstrap', {
      method: 'POST',
      body: JSON.stringify({ name }),
    })
  },
  switchWorkspace(workspaceId: string) {
    return apiRequest<{ workspace: Workspace; role: string }>('/auth/switch-workspace', {
      method: 'POST',
      body: JSON.stringify({ workspaceId }),
    })
  },
}
