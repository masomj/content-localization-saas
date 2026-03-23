import { authClient } from '~/api/authClient'
import { getAuthToken, setAuthToken, attemptTokenRefresh, ApiError } from '~/api/client'
import {
  clearOidcSession,
  completeOidcCallback,
  getOidcUser,
  startOidcLogin,
  startOidcLogout,
} from '~/lib/oidc'
import type { User, Workspace, WorkspaceMembership } from '~/api/types'

interface AuthState {
  user: User | null
  organization: Workspace | null
  organizations: WorkspaceMembership[]
  isAuthenticated: boolean
  isLoading: boolean
  isAdmin: boolean
}

const USER_STORAGE_KEY = 'locflow_user'
const ORG_STORAGE_KEY = 'locflow_organization'

const authState = reactive<AuthState>({
  user: null,
  organization: null,
  organizations: [],
  isAuthenticated: false,
  isLoading: true,
  isAdmin: false,
})

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
  const memberships = user.workspaces || []
  const storedOrg = getStoredOrganization()
  const selected = storedOrg
    ? memberships.find(x => x.id === storedOrg.id)
    : undefined

  const active = selected || (user.workspace ? memberships.find(x => x.id === user.workspace?.id) : undefined) || memberships[0]

  authState.user = user
  authState.organizations = memberships
  authState.organization = active ? { id: active.id, name: active.name } : null
  authState.isAuthenticated = true
  authState.isAdmin = !!active && active.role === 'Admin'

  setStoredUser(user)
  setStoredOrganization(authState.organization)
}

async function clearSessionState(config?: { keycloakUrl: string; keycloakRealm: string; keycloakClientId: string }) {
  setAuthToken(null)
  setStoredUser(null)
  setStoredOrganization(null)

  if (config) {
    await clearOidcSession({
      authority: config.keycloakUrl,
      realm: config.keycloakRealm,
      clientId: config.keycloakClientId,
    })
  }

  authState.user = null
  authState.organization = null
  authState.organizations = []
  authState.isAuthenticated = false
  authState.isAdmin = false
}

async function bootstrapSession(config: { keycloakUrl: string; keycloakRealm: string; keycloakClientId: string }): Promise<void> {
  authState.isLoading = true
  try {
    let token = getAuthToken()

    if (!token) {
      const oidcUser = await getOidcUser({
        authority: config.keycloakUrl,
        realm: config.keycloakRealm,
        clientId: config.keycloakClientId,
      })
      token = oidcUser?.access_token ?? null
      if (token) setAuthToken(token, true)
    }

    if (!token) {
      authState.isAuthenticated = false
      authState.isAdmin = false
      authState.user = null
      authState.organization = getStoredOrganization()
      return
    }

    const currentUser = await authClient.me()
    applyAuthenticatedUser(currentUser)
  } catch (error: unknown) {
    if (error instanceof ApiError && error.status === 401) {
      const refreshed = await attemptTokenRefresh()
      if (refreshed) {
        const currentUser = await authClient.me()
        applyAuthenticatedUser(currentUser)
      } else {
        await clearSessionState(config)
      }
    } else {
      const cached = getStoredUser()
      if (cached) {
        applyAuthenticatedUser(cached)
      } else {
        authState.isAuthenticated = false
        authState.isAdmin = false
        authState.user = null
        authState.organization = getStoredOrganization()
      }
    }
  } finally {
    authState.isLoading = false
  }
}

function resolveUiTheme(): 'light' | 'dark' {
  if (typeof window === 'undefined') return 'light'

  const fromDom = document.documentElement.getAttribute('data-theme')
  if (fromDom === 'light' || fromDom === 'dark') return fromDom

  const pref = window.localStorage.getItem('locflow-theme')
  if (pref === 'light' || pref === 'dark') return pref

  const systemDark = typeof window.matchMedia === 'function'
    && window.matchMedia('(prefers-color-scheme: dark)').matches

  return systemDark ? 'dark' : 'light'
}

let _bootstrapPromise: Promise<void> | null = null

function ensureBootstrap(config: { keycloakUrl: string; keycloakRealm: string; keycloakClientId: string }): Promise<void> {
  if (typeof window === 'undefined') return Promise.resolve()
  if (!_bootstrapPromise) {
    _bootstrapPromise = bootstrapSession(config)
  }
  return _bootstrapPromise
}

export function waitForAuthReady(config?: { keycloakUrl: string; keycloakRealm: string; keycloakClientId: string }): Promise<void> {
  if (!_bootstrapPromise && config) {
    return ensureBootstrap(config)
  }
  return _bootstrapPromise ?? Promise.resolve()
}

export function useAuth() {
  const router = useRouter()
  const config = useRuntimeConfig().public

  ensureBootstrap({
    keycloakUrl: config.keycloakUrl,
    keycloakRealm: config.keycloakRealm,
    keycloakClientId: config.keycloakClientId,
  })

  async function login(_email?: string, _password?: string, _rememberMe = true): Promise<{ success: boolean; error?: string }> {
    try {
      await startOidcLogin({
        authority: config.keycloakUrl,
        realm: config.keycloakRealm,
        clientId: config.keycloakClientId,
      }, resolveUiTheme())
      return { success: true }
    } catch (error) {
      return { success: false, error: error instanceof Error ? error.message : 'Unable to start SSO login' }
    }
  }

  async function handleCallback(): Promise<{ success: boolean; error?: string }> {
    try {
      const oidcUser = await completeOidcCallback({
        authority: config.keycloakUrl,
        realm: config.keycloakRealm,
        clientId: config.keycloakClientId,
      })

      if (!oidcUser?.access_token) {
        return { success: false, error: 'No access token returned by identity provider' }
      }

      setAuthToken(oidcUser.access_token, true)

      try {
        const currentUser = await authClient.me()
        applyAuthenticatedUser(currentUser)
        return { success: true }
      } catch (error: any) {
        if (error?.status === 401) {
          const newToken = await attemptTokenRefresh()
          if (newToken) {
            const currentUser = await authClient.me()
            applyAuthenticatedUser(currentUser)
            return { success: true }
          }
        }
        await clearSessionState(config)
        return { success: false, error: 'Could not load user profile after sign in.' }
      }
    } catch (error) {
      return { success: false, error: error instanceof Error ? error.message : 'Login callback failed' }
    }
  }

  let _logoutInProgress = false

  async function logout(): Promise<void> {
    if (_logoutInProgress) return
    _logoutInProgress = true
    authState.isLoading = true
    try {
      try { await authClient.logout() } catch {}
      await clearSessionState(config)
      await startOidcLogout({
        authority: config.keycloakUrl,
        realm: config.keycloakRealm,
        clientId: config.keycloakClientId,
      })
    } finally {
      authState.isLoading = false
      _logoutInProgress = false
    }
  }

  async function register(_data?: {
    email: string
    password: string
    firstName: string
    lastName: string
    company?: string
  }, _rememberMe = true): Promise<{ success: boolean; error?: string }> {
    await login()
    return { success: true }
  }

  async function requestPasswordReset(_email: string): Promise<{ success: boolean; error?: string; resetLink?: string }> {
    return {
      success: false,
      error: 'Password reset is managed by Keycloak. Use the "Forgot Password" link on the SSO sign-in page.',
    }
  }

  async function createOrganization(name: string): Promise<{ success: boolean; error?: string }> {
    authState.isLoading = true
    try {
      if (!name.trim()) return { success: false, error: 'Organization name is required' }
      await authClient.createWorkspace(name.trim())
      await refreshUser()
      return { success: true }
    } catch (error: any) {
      return { success: false, error: error?.message || 'Failed to create organization' }
    } finally {
      authState.isLoading = false
    }
  }

  async function switchOrganization(workspaceId: string): Promise<{ success: boolean; error?: string }> {
    try {
      const target = authState.organizations.find(x => x.id === workspaceId)
      if (!target) return { success: false, error: 'Organization not available for this user' }

      await authClient.switchWorkspace(workspaceId)
      authState.organization = { id: target.id, name: target.name }
      authState.isAdmin = target.role === 'Admin'
      setStoredOrganization(authState.organization)
      await refreshUser()
      return { success: true }
    } catch (error: any) {
      return { success: false, error: error?.message || 'Failed to switch organization' }
    }
  }

  async function refreshUser(): Promise<void> {
    try {
      const currentUser = await authClient.me()
      applyAuthenticatedUser(currentUser)
    } catch (error: any) {
      if (error?.status === 401) {
        const newToken = await attemptTokenRefresh()
        if (newToken) {
          const currentUser = await authClient.me()
          applyAuthenticatedUser(currentUser)
          return
        }
      }
      await clearSessionState(config)
      throw new Error('Unable to refresh authenticated user')
    }
  }

  return {
    user: computed(() => authState.user),
    organization: computed(() => authState.organization),
    organizations: computed(() => authState.organizations),
    hasOrganization: computed(() => !!authState.organization),
    isAuthenticated: computed(() => authState.isAuthenticated),
    isLoading: computed(() => authState.isLoading),
    isAdmin: computed(() => authState.isAdmin),
    login,
    logout,
    register,
    createOrganization,
    switchOrganization,
    requestPasswordReset,
    refreshUser,
    handleCallback,
    clearError: () => {},
  }
}
