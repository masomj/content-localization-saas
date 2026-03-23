import { authClient } from '~/api/authClient'
import { getAuthToken, setAuthToken, attemptTokenRefresh, ApiError, setRefreshToken } from '~/api/client'
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
const OIDC_VERIFIER_KEY = 'locflow_oidc_verifier'
const OIDC_STATE_KEY = 'locflow_oidc_state'

const authState = reactive<AuthState>({
  user: null,
  organization: null,
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
  authState.user = user
  authState.organization = user.workspace ? { id: user.workspace.id, name: user.workspace.name } : null
  authState.isAuthenticated = true
  authState.isAdmin = user.role === 'Admin'
  setStoredUser(user)
  setStoredOrganization(authState.organization)
}

function clearSessionState() {
  setAuthToken(null)
  setRefreshToken(null)
  setStoredUser(null)
  setStoredOrganization(null)
  authState.user = null
  authState.organization = null
  authState.isAuthenticated = false
  authState.isAdmin = false
}

async function bootstrapSession(): Promise<void> {
  authState.isLoading = true
  try {
    let token = getAuthToken()
    if (!token) {
      token = await attemptTokenRefresh()
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
      clearSessionState()
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

async function sha256(input: string): Promise<string> {
  const data = new TextEncoder().encode(input)
  const digest = await crypto.subtle.digest('SHA-256', data)
  return btoa(String.fromCharCode(...new Uint8Array(digest))).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
}

function randomString(length = 64): string {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~'
  const bytes = new Uint8Array(length)
  crypto.getRandomValues(bytes)
  return Array.from(bytes).map(b => chars[b % chars.length]).join('')
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

if (typeof window !== 'undefined') {
  _bootstrapPromise = bootstrapSession()
}

export function waitForAuthReady(): Promise<void> {
  return _bootstrapPromise ?? Promise.resolve()
}

export function useAuth() {
  const router = useRouter()
  const config = useRuntimeConfig().public

  async function login(_email?: string, _password?: string, _rememberMe = true): Promise<{ success: boolean; error?: string }> {
    try {
      const verifier = randomString(64)
      const state = randomString(32)
      const challenge = await sha256(verifier)
      sessionStorage.setItem(OIDC_VERIFIER_KEY, verifier)
      sessionStorage.setItem(OIDC_STATE_KEY, state)

      const redirectUri = `${window.location.origin}/auth/callback`
      const authorizeUrl = new URL(`${config.keycloakUrl}/realms/${config.keycloakRealm}/protocol/openid-connect/auth`)
      authorizeUrl.searchParams.set('client_id', config.keycloakClientId)
      authorizeUrl.searchParams.set('response_type', 'code')
      authorizeUrl.searchParams.set('scope', 'openid profile email')
      authorizeUrl.searchParams.set('redirect_uri', redirectUri)
      authorizeUrl.searchParams.set('state', state)
      authorizeUrl.searchParams.set('code_challenge', challenge)
      authorizeUrl.searchParams.set('code_challenge_method', 'S256')
      authorizeUrl.searchParams.set('ui_theme', resolveUiTheme())

      window.location.href = authorizeUrl.toString()
      return { success: true }
    } catch (error) {
      return { success: false, error: error instanceof Error ? error.message : 'Unable to start SSO login' }
    }
  }

  async function handleCallback(code: string, state: string): Promise<{ success: boolean; error?: string }> {
    try {
      const expectedState = sessionStorage.getItem(OIDC_STATE_KEY)
      const verifier = sessionStorage.getItem(OIDC_VERIFIER_KEY)
      if (!expectedState || !verifier || expectedState !== state) {
        return { success: false, error: 'Invalid login state. Please try again.' }
      }

      const tokenEndpoint = `${config.keycloakUrl}/realms/${config.keycloakRealm}/protocol/openid-connect/token`
      const redirectUri = `${window.location.origin}/auth/callback`
      const body = new URLSearchParams({
        grant_type: 'authorization_code',
        code,
        client_id: config.keycloakClientId,
        code_verifier: verifier,
        redirect_uri: redirectUri,
      })

      const response = await fetch(tokenEndpoint, {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: body.toString(),
      })

      if (!response.ok) {
        return { success: false, error: 'Token exchange failed' }
      }

      const tokens = await response.json()
      if (!tokens.access_token) {
        return { success: false, error: 'No access token returned by identity provider' }
      }

      setAuthToken(tokens.access_token, true)
      setRefreshToken(tokens.refresh_token ?? null, true)
      sessionStorage.removeItem(OIDC_STATE_KEY)
      sessionStorage.removeItem(OIDC_VERIFIER_KEY)

      await refreshUser()
      return { success: true }
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
      clearSessionState()
      const logoutUrl = new URL(`${config.keycloakUrl}/realms/${config.keycloakRealm}/protocol/openid-connect/logout`)
      logoutUrl.searchParams.set('client_id', config.keycloakClientId)
      logoutUrl.searchParams.set('post_logout_redirect_uri', `${window.location.origin}/login`)
      window.location.href = logoutUrl.toString()
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
      const org = { id: `org_${Date.now()}`, name: name.trim() }
      setStoredOrganization(org)
      authState.organization = org
      return { success: true }
    } finally {
      authState.isLoading = false
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
      clearSessionState()
      await router.push('/login')
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
    handleCallback,
    clearError: () => {},
  }
}
