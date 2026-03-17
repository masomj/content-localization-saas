import { authClient } from '~/api/authClient'
import { getAuthToken, setAuthToken, attemptTokenRefresh } from '~/api/client'
import type { User, Workspace } from '~/api/types'

interface AuthState {
  user: User | null
  organization: Workspace | null
  isAuthenticated: boolean
  isLoading: boolean
  isAdmin: boolean
}

const USER_STORAGE_KEY = 'locflow_user'
const ORG_STORAGE_KEY = 'locflow_organization'
const REMEMBER_ME_KEY = 'locflow_remember_me'

const authState = reactive<AuthState>({
  user: null,
  organization: null,
  isAuthenticated: false,
  isLoading: true,
  isAdmin: false,
})

function shouldPersistSession(): boolean {
  if (typeof window === 'undefined') return true
  return localStorage.getItem(REMEMBER_ME_KEY) === 'true'
}

function getStoredUser(): User | null {
  if (typeof window === 'undefined') return null
  const stored = localStorage.getItem(USER_STORAGE_KEY)
  if (!stored) return null
  try { return JSON.parse(stored) } catch { return null }
}

function setStoredUser(user: User | null): void {
  if (typeof window === 'undefined') return
  if (user) localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(user))
  else localStorage.removeItem(USER_STORAGE_KEY)
}

function getStoredOrganization(): Workspace | null {
  if (typeof window === 'undefined') return null
  const stored = localStorage.getItem(ORG_STORAGE_KEY)
  if (!stored) return null
  try { return JSON.parse(stored) } catch { return null }
}

function setStoredOrganization(org: Workspace | null): void {
  if (typeof window === 'undefined') return
  if (org) localStorage.setItem(ORG_STORAGE_KEY, JSON.stringify(org))
  else localStorage.removeItem(ORG_STORAGE_KEY)
}

function applyAuthenticatedUser(user: User) {
  authState.user = user
  authState.organization = user.workspace ? { id: user.workspace.id, name: user.workspace.name } : null
  authState.isAuthenticated = true
  authState.isAdmin = user.role === 'Admin'
  setStoredUser(user)
  setStoredOrganization(authState.organization)
}

function clearSessionState() {
  setAuthToken(null)
  setStoredUser(null)
  setStoredOrganization(null)
  if (typeof window !== 'undefined') localStorage.removeItem(REMEMBER_ME_KEY)
  authState.user = null
  authState.organization = null
  authState.isAuthenticated = false
  authState.isAdmin = false
}

async function bootstrapSession(): Promise<void> {
  authState.isLoading = true
  try {
    const token = getAuthToken()
    const storedUser = getStoredUser()

    if (!token || !storedUser) {
      authState.isAuthenticated = false
      authState.isAdmin = false
      authState.user = null
      authState.organization = getStoredOrganization()
      return
    }

    const currentUser = await authClient.me()
    applyAuthenticatedUser(currentUser)
  } catch {
    clearSessionState()
  } finally {
    authState.isLoading = false
  }
}

if (typeof window !== 'undefined') {
  bootstrapSession()
}

export function useAuth() {
  const router = useRouter()

  async function login(email: string, password: string, rememberMe = true): Promise<{ success: boolean; error?: string }> {
    authState.isLoading = true
    try {
      if (!email || !password) return { success: false, error: 'Email and password are required' }

      const response = await authClient.login(email, password)
      if (typeof window !== 'undefined') localStorage.setItem(REMEMBER_ME_KEY, rememberMe ? 'true' : 'false')
      setAuthToken(response.token, rememberMe)
      applyAuthenticatedUser(response.user)
      if (response.workspace) {
        authState.organization = response.workspace
        setStoredOrganization(response.workspace)
      }
      return { success: true }
    } catch (error) {
      return { success: false, error: error instanceof Error ? error.message : 'Invalid credentials' }
    } finally {
      authState.isLoading = false
    }
  }

  let _logoutInProgress = false

  async function logout(): Promise<void> {
    // Guard against concurrent logout calls (e.g., multiple 401s firing at once)
    if (_logoutInProgress) return
    _logoutInProgress = true
    authState.isLoading = true
    try {
      try { await authClient.logout() } catch {}
      clearSessionState()
    } finally {
      authState.isLoading = false
      _logoutInProgress = false
      router.push('/login')
    }
  }

  async function register(data: {
    email: string
    password: string
    firstName: string
    lastName: string
    company?: string
  }, rememberMe = true): Promise<{ success: boolean; error?: string }> {
    authState.isLoading = true
    try {
      if (!data.email || !data.password || !data.firstName || !data.lastName) {
        return { success: false, error: 'All required fields must be filled' }
      }
      if (data.password.length < 8) return { success: false, error: 'Password must be at least 8 characters' }

      const response = await authClient.register(data)
      const persist = shouldPersistSession() || rememberMe
      if (typeof window !== 'undefined') localStorage.setItem(REMEMBER_ME_KEY, persist ? 'true' : 'false')
      setAuthToken(response.token, persist)
      applyAuthenticatedUser(response.user)
      if (response.workspace) {
        authState.organization = response.workspace
        setStoredOrganization(response.workspace)
      }
      return { success: true }
    } catch (error) {
      return { success: false, error: error instanceof Error ? error.message : 'Registration failed' }
    } finally {
      authState.isLoading = false
    }
  }

  async function createOrganization(name: string): Promise<{ success: boolean; error?: string }> {
    authState.isLoading = true
    try {
      if (!name.trim()) return { success: false, error: 'Organization name is required' }
      const org = { id: `org_${Date.now()}`, name: name.trim() }
      setStoredOrganization(org)
      authState.organization = org
      return { success: true }
    } catch {
      return { success: false, error: 'Failed to create organization' }
    } finally {
      authState.isLoading = false
    }
  }

  async function requestPasswordReset(email: string): Promise<{ success: boolean; error?: string; resetLink?: string }> {
    authState.isLoading = true
    try {
      if (!email.trim()) return { success: false, error: 'Email is required' }
      const response = await authClient.forgotPassword(email)
      return { success: true, resetLink: response.resetLink }
    } catch (error) {
      return { success: false, error: error instanceof Error ? error.message : 'Failed to request reset' }
    } finally {
      authState.isLoading = false
    }
  }

  async function refreshUser(): Promise<void> {
    if (!authState.isAuthenticated) return
    try {
      const currentUser = await authClient.me()
      applyAuthenticatedUser(currentUser)
    } catch (error: any) {
      // On 401, attempt a token refresh before giving up
      if (error?.status === 401) {
        const newToken = await attemptTokenRefresh()
        if (newToken) {
          try {
            const currentUser = await authClient.me()
            applyAuthenticatedUser(currentUser)
            return
          } catch {
            // Refresh succeeded but /me still failed — logout
          }
        }
      }
      await logout()
    }
  }

  return {
    user: computed(() => authState.user),
    organization: computed(() => authState.organization),
    hasOrganization: computed(() => !!authState.organization),
    isAuthenticated: computed(() => authState.isAuthenticated),
    isLoading: computed(() => authState.isLoading),
    isAdmin: computed(() => authState.isAdmin),
    login,
    logout,
    register,
    createOrganization,
    requestPasswordReset,
    refreshUser,
    clearError: () => {},
  }
}
