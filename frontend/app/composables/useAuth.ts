interface User {
  id: string
  email: string
  name: string
}

interface AuthState {
  user: User | null
  isAuthenticated: boolean
  isLoading: boolean
}

const authState = reactive<AuthState>({
  user: null,
  isAuthenticated: false,
  isLoading: false,
})

export function useAuth() {
  const router = useRouter()

  async function login(email: string, password: string) {
    authState.isLoading = true
    try {
      await new Promise(resolve => setTimeout(resolve, 500))
      authState.user = {
        id: '1',
        email,
        name: email.split('@')[0],
      }
      authState.isAuthenticated = true
      return { success: true }
    } catch (error) {
      return { success: false, error: 'Invalid credentials' }
    } finally {
      authState.isLoading = false
    }
  }

  async function logout() {
    authState.isLoading = true
    try {
      await new Promise(resolve => setTimeout(resolve, 300))
      authState.user = null
      authState.isAuthenticated = false
      router.push('/')
    } finally {
      authState.isLoading = false
    }
  }

  async function register(data: { email: string; password: string; firstName: string; lastName: string; company?: string }) {
    authState.isLoading = true
    try {
      await new Promise(resolve => setTimeout(resolve, 500))
      authState.user = {
        id: '1',
        email: data.email,
        name: `${data.firstName} ${data.lastName}`,
      }
      authState.isAuthenticated = true
      return { success: true }
    } catch (error) {
      return { success: false, error: 'Registration failed' }
    } finally {
      authState.isLoading = false
    }
  }

  return {
    user: computed(() => authState.user),
    isAuthenticated: computed(() => authState.isAuthenticated),
    isLoading: computed(() => authState.isLoading),
    login,
    logout,
    register,
  }
}
