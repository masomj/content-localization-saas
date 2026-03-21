import { apiRequest } from '~/api/client'
import type { User } from '~/api/types'

export const authClient = {
  me() {
    return apiRequest<User>('/auth/me')
  },
  logout() {
    return apiRequest<void>('/auth/logout', { method: 'POST' })
  },
}
