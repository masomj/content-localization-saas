import { apiRequest } from '~/api/client'
import type { AuthResponse, User } from '~/api/types'

export const authClient = {
  login(email: string, password: string) {
    return apiRequest<AuthResponse>('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    })
  },
  register(data: { email: string; password: string; firstName: string; lastName: string; company?: string }) {
    return apiRequest<AuthResponse>('/auth/register', {
      method: 'POST',
      body: JSON.stringify(data),
    })
  },
  me() {
    return apiRequest<User>('/auth/me')
  },
  logout() {
    return apiRequest<void>('/auth/logout', { method: 'POST' })
  },
  forgotPassword(email: string) {
    return apiRequest<{ message: string; resetLink?: string }>('/auth/forgot-password', {
      method: 'POST',
      body: JSON.stringify({ email }),
    })
  },
  resetPassword(email: string, token: string, newPassword: string) {
    return apiRequest<{ message?: string }>('/auth/reset-password', {
      method: 'POST',
      body: JSON.stringify({ email, token, newPassword }),
    })
  },
}
