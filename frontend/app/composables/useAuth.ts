interface User {
  id: string
  email: string
  name: string
}

interface AuthState {
  user: User | null
  isAuthenticated: boolean
  isLoading: boolean
  isFallbackMode: boolean
}

const AUTH_STORAGE_KEY = 'locflow_auth_token'
const USER_STORAGE_KEY = 'locflow_user'

const authState = reactive<AuthState>({
  user: null,
  isAuthenticated: false,
  isLoading: true,
  isFallbackMode: false,
})

function getStoredToken(): string | null {
  if (typeof window === 'undefined') return null
  return localStorage.getItem(AUTH_STORAGE_KEY)
}

function setStoredToken(token: string | null): void {
  if (typeof window === 'undefined') return
  if (token) {
    localStorage.setItem(AUTH_STORAGE_KEY, token)
  } else {
    localStorage.removeItem(AUTH_STORAGE_KEY)
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

function bootstrapSession(): void {
  authState.isLoading = true
  try {
    const token = getStoredToken()
    const user = getStoredUser()

    if (token && user) {
      authState.user = user
      authState.isAuthenticated = true
      authState.isFallbackMode = false
    } else {
      authState.isFallbackMode = true
    }
  } catch {
    authState.isFallbackMode = true
  } finally {
    authState.isLoading = false
  }
}

if (typeof window !== 'undefined') {
  bootstrapSession()
}

export function useAuth() {
  const router = useRouter()

  async function login(email: string, password: string): Promise<{ success: boolean; error?: string; isFallback?: boolean }> {
    authState.isLoading = true
    try {
      if (!email || !password) {
        return { success: false, error: 'Email and password are required', isFallback: authState.isFallbackMode }
      }

      const token = `mock_token_${Date.now()}`
      const user: User = {
        id: '1',
        email,
        name: email.split('@')[0],
      }

      setStoredToken(token)
      setStoredUser(user)
      authState.user = user
      authState.isAuthenticated = true
      authState.isFallbackMode = false

      return { success: true, isFallback: false }
    } catch (error) {
      return { success: false, error: 'Invalid credentials', isFallback: authState.isFallbackMode }
    } finally {
      authState.isLoading = false
    }
  }

  async function logout(): Promise<void> {
    authState.isLoading = true
    try {
      setStoredToken(null)
      setStoredUser(null)
      authState.user = null
      authState.isAuthenticated = false
      authState.isFallbackMode = true
      router.push('/login')
    } finally {
      authState.isLoading = false
    }
  }

  async function register(data: {
    email: string
    password: string
    firstName: string
    lastName: string
    company?: string
  }): Promise<{ success: boolean; error?: string; isFallback?: boolean }> {
    authState.isLoading = true
    try {
      if (!data.email || !data.password || !data.firstName || !data.lastName) {
        return { success: false, error: 'All required fields must be filled', isFallback: authState.isFallbackMode }
      }

      if (data.password.length < 8) {
        return { success: false, error: 'Password must be at least 8 characters', isFallback: authState.isFallbackMode }
      }

      const token = `mock_token_${Date.now()}`
      const user: User = {
        id: '1',
        email: data.email,
        name: `${data.firstName} ${data.lastName}`,
      }

      setStoredToken(token)
      setStoredUser(user)
      authState.user = user
      authState.isAuthenticated = true
      authState.isFallbackMode = false

      return { success: true, isFallback: false }
    } catch (error) {
      return { success: false, error: 'Registration failed', isFallback: authState.isFallbackMode }
    } finally {
      authState.isLoading = false
    }
  }

  function clearError(): void {
  }

  return {
    user: computed(() => authState.user),
    isAuthenticated: computed(() => authState.isAuthenticated),
    isLoading: computed(() => authState.isLoading),
    isFallbackMode: computed(() => authState.isFallbackMode),
    login,
    logout,
    register,
    clearError,
  }
}
