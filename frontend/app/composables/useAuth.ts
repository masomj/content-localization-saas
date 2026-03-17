interface User {
  id: string
  email: string
  name: string
  role?: string
  workspace?: {
    id: string
    name: string
  }
}

interface Organization {
  id: string
  name: string
}

interface AuthState {
  user: User | null
  organization: Organization | null
  isAuthenticated: boolean
  isLoading: boolean
  isAdmin: boolean
}

const AUTH_STORAGE_KEY = 'locflow_auth_token'
const USER_STORAGE_KEY = 'locflow_user'
const ORG_STORAGE_KEY = 'locflow_organization'
const REMEMBER_ME_KEY = 'locflow_remember_me'

function getApiBaseUrl(): string {
  if (typeof window === 'undefined') {
    return '/api'
  }

  const fromEnv = (window as any).__ENV__?.API_URL
  const fromNuxtRuntime = (window as any).__NUXT__?.config?.public?.apiBase

  return fromEnv || fromNuxtRuntime || '/api'
}

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

function getStoredToken(): string | null {
  if (typeof window === 'undefined') return null
  return localStorage.getItem(AUTH_STORAGE_KEY) || sessionStorage.getItem(AUTH_STORAGE_KEY)
}

function setStoredToken(token: string | null, rememberMe = true): void {
  if (typeof window === 'undefined') return
  const target = rememberMe ? localStorage : sessionStorage
  const other = rememberMe ? sessionStorage : localStorage

  if (token) {
    target.setItem(AUTH_STORAGE_KEY, token)
    other.removeItem(AUTH_STORAGE_KEY)
  } else {
    localStorage.removeItem(AUTH_STORAGE_KEY)
    sessionStorage.removeItem(AUTH_STORAGE_KEY)
  }
}

function getStoredUser(): User | null {
  if (typeof window === 'undefined') return null
  const stored = localStorage.getItem(USER_STORAGE_KEY)
  if (stored) {
    try {
      return JSON.parse(stored)
    } catch {
      return null
    }
  }
  return null
}

function setStoredUser(user: User | null): void {
  if (typeof window === 'undefined') return
  if (user) {
    localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(user))
  } else {
    localStorage.removeItem(USER_STORAGE_KEY)
  }
}

function getStoredOrganization(): Organization | null {
  if (typeof window === 'undefined') return null
  const stored = localStorage.getItem(ORG_STORAGE_KEY)
  if (stored) {
    try {
      return JSON.parse(stored)
    } catch {
      return null
    }
  }
  return null
}

function setStoredOrganization(org: Organization | null): void {
  if (typeof window === 'undefined') return
  if (org) {
    localStorage.setItem(ORG_STORAGE_KEY, JSON.stringify(org))
  } else {
    localStorage.removeItem(ORG_STORAGE_KEY)
  }
}

async function fetchApi<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
  const token = getStoredToken()
  
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string> || {}),
  }
  
  if (token) {
    headers['Authorization'] = `Bearer ${token}`
  }

  const apiBaseUrl = getApiBaseUrl()
  const response = await fetch(`${apiBaseUrl}${endpoint}`, {
    ...options,
    headers,
  })

  const contentType = response.headers.get('content-type') || ''
  const isJson = contentType.includes('json')

  if (!response.ok) {
    if (isJson) {
      const error = await response.json().catch(() => ({ error: 'Request failed' })) as any
      const problemErrors = error?.errors && typeof error.errors === 'object' && !Array.isArray(error.errors)
        ? Object.values(error.errors).flat().join(', ')
        : undefined
      throw new Error(
        error?.error ||
        (Array.isArray(error?.errors) ? error.errors.join(', ') : undefined) ||
        problemErrors ||
        error?.detail ||
        error?.title ||
        'Request failed'
      )
    }

    const text = await response.text().catch(() => '')
    if (text.trim().startsWith('<!DOCTYPE') || text.trim().startsWith('<html')) {
      throw new Error('API endpoint returned HTML instead of JSON. Check frontend API base/proxy configuration.')
    }

    throw new Error('Request failed')
  }

  if (!isJson) {
    const text = await response.text().catch(() => '')
    if (text.trim().startsWith('<!DOCTYPE') || text.trim().startsWith('<html')) {
      throw new Error('API endpoint returned HTML instead of JSON. Check frontend API base/proxy configuration.')
    }

    throw new Error('Unexpected non-JSON API response')
  }

  return response.json()
}

async function bootstrapSession(): Promise<void> {
  authState.isLoading = true
  try {
    const token = getStoredToken()
    const storedUser = getStoredUser()
    const storedOrg = getStoredOrganization()

    if (token && storedUser) {
      try {
        const currentUser = await fetchApi<User>('/auth/me')
        
        authState.user = currentUser
        authState.organization = currentUser.workspace ? {
          id: currentUser.workspace.id,
          name: currentUser.workspace.name,
        } : null
        authState.isAuthenticated = true
        authState.isAdmin = currentUser.role === 'Admin' || currentUser.role === 'Admin'
        
        setStoredUser(currentUser)
        if (currentUser.workspace) {
          setStoredOrganization({
            id: currentUser.workspace.id,
            name: currentUser.workspace.name,
          })
        }
      } catch {
        setStoredToken(null)
        setStoredUser(null)
        setStoredOrganization(null)
        authState.user = null
        authState.organization = null
        authState.isAuthenticated = false
        authState.isAdmin = false
      }
    } else {
      authState.isAuthenticated = false
      authState.isAdmin = false
    }
  } catch {
    authState.isAuthenticated = false
    authState.isAdmin = false
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
      if (!email || !password) {
        return { success: false, error: 'Email and password are required' }
      }

      const response = await fetchApi<{
        token: string
        user: User
        workspace?: { id: string; name: string }
      }>('/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email, password }),
      })

      if (typeof window !== 'undefined') { localStorage.setItem(REMEMBER_ME_KEY, rememberMe ? 'true' : 'false') }
      setStoredToken(response.token, rememberMe)
      setStoredUser(response.user, rememberMe)
      
      authState.user = response.user
      authState.isAuthenticated = true
      authState.isAdmin = response.user.role === 'Admin'

      if (response.workspace) {
        const org = {
          id: response.workspace.id,
          name: response.workspace.name,
        }
        setStoredOrganization(org, rememberMe)
        authState.organization = org
      }

      return { success: true }
    } catch (error) {
      return { success: false, error: error instanceof Error ? error.message : 'Invalid credentials' }
    } finally {
      authState.isLoading = false
    }
  }

  async function logout(): Promise<void> {
    authState.isLoading = true
    try {
      try {
        await fetchApi('/auth/logout', { method: 'POST' })
      } catch {
      }
      
      setStoredToken(null)
      setStoredUser(null)
      setStoredOrganization(null)
      if (typeof window !== 'undefined') {
        localStorage.removeItem(REMEMBER_ME_KEY)
      }
      authState.user = null
      authState.organization = null
      authState.isAuthenticated = false
      authState.isAdmin = false
    } finally {
      authState.isLoading = false
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

      if (data.password.length < 8) {
        return { success: false, error: 'Password must be at least 8 characters' }
      }

      const response = await fetchApi<{
        token: string
        user: User
        workspace?: { id: string; name: string }
      }>('/auth/register', {
        method: 'POST',
        body: JSON.stringify(data),
      })

      if (typeof window !== 'undefined') { localStorage.setItem(REMEMBER_ME_KEY, rememberMe ? 'true' : 'false') }
      setStoredToken(response.token, rememberMe)
      setStoredUser(response.user, rememberMe)
      
      authState.user = response.user
      authState.isAuthenticated = true
      authState.isAdmin = response.user.role === 'Admin'

      if (response.workspace) {
        const org = {
          id: response.workspace.id,
          name: response.workspace.name,
        }
        setStoredOrganization(org, rememberMe)
        authState.organization = org
      }

      return { success: true }
    } catch (error) {
      return { success: false, error: error instanceof Error ? error.message : 'Registration failed' }
    } finally {
      authState.isLoading = false
    }
  }

  async function createOrganization(name: string): Promise<{ success: boolean; error?: string }> {
    const rememberMe = shouldPersistSession()
    authState.isLoading = true
    try {
      if (!name.trim()) {
        return { success: false, error: 'Organization name is required' }
      }

      const org: Organization = {
        id: `org_${Date.now()}`,
        name: name.trim(),
      }

      setStoredOrganization(org, rememberMe)
      authState.organization = org

      return { success: true }
    } catch (error) {
      return { success: false, error: 'Failed to create organization' }
    } finally {
      authState.isLoading = false
    }
  }

  async function requestPasswordReset(email: string): Promise<{ success: boolean; error?: string; resetLink?: string }> {
    authState.isLoading = true
    try {
      if (!email.trim()) {
        return { success: false, error: 'Email is required' }
      }

      const response = await fetchApi<{ message: string; resetLink?: string }>('/auth/forgot-password', {
        method: 'POST',
        body: JSON.stringify({ email }),
      })

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
      const currentUser = await fetchApi<User>('/auth/me')
      authState.user = currentUser
      authState.isAdmin = currentUser.role === 'Admin'
      setStoredUser(currentUser)
    } catch {
      await logout()
    }
  }

  function clearError(): void {
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
    clearError,
  }
}


