const AUTH_STORAGE_KEY = 'locflow_auth_token'

/** Buffer in seconds before actual JWT expiry to trigger proactive refresh */
const TOKEN_EXPIRY_BUFFER_SECONDS = 60

export class ApiError extends Error {
  status: number
  detail?: string
  errors?: unknown

  constructor(message: string, status: number, detail?: string, errors?: unknown) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.detail = detail
    this.errors = errors
  }
}

function getApiBaseUrl(): string {
  if (typeof window === 'undefined') return '/api'

  const fromEnv = (window as any).__ENV__?.API_URL
  const fromNuxtRuntime = (window as any).__NUXT__?.config?.public?.apiBase
  return fromEnv || fromNuxtRuntime || '/api'
}

export function decodeJwtExp(token: string | null): number {
  if (!token) return 0
  try {
    const payload = JSON.parse(atob(token.split('.')[1] || ''))
    return Number(payload?.exp || 0)
  } catch {
    return 0
  }
}

function isNotExpired(exp: number): boolean {
  const now = Math.floor(Date.now() / 1000)
  return exp > now
}

/**
 * Returns true if the token will expire within the buffer period.
 * Used to trigger proactive refresh before actual expiry.
 */
export function isTokenExpiringSoon(token: string | null): boolean {
  const exp = decodeJwtExp(token)
  if (exp === 0) return false
  const now = Math.floor(Date.now() / 1000)
  return exp > now && exp - now <= TOKEN_EXPIRY_BUFFER_SECONDS
}

export function getAuthToken(): string | null {
  if (typeof window === 'undefined') return null

  const localToken = localStorage.getItem(AUTH_STORAGE_KEY)
  const sessionToken = sessionStorage.getItem(AUTH_STORAGE_KEY)

  if (!localToken && !sessionToken) return null

  const localExp = decodeJwtExp(localToken)
  const sessionExp = decodeJwtExp(sessionToken)

  const localValid = localToken && isNotExpired(localExp)
  const sessionValid = sessionToken && isNotExpired(sessionExp)

  if (localValid && sessionValid) {
    if (localExp === sessionExp) return localToken
    return localExp > sessionExp ? localToken : sessionToken
  }

  if (localValid) return localToken
  if (sessionValid) return sessionToken

  // Both tokens are expired — return null instead of a stale token
  return null
}

/**
 * Returns a raw stored token even if expired, for use during token refresh.
 * The refresh endpoint accepts an expired-but-valid-signature JWT.
 */
export function getStoredTokenForRefresh(): string | null {
  if (typeof window === 'undefined') return null
  return localStorage.getItem(AUTH_STORAGE_KEY) || sessionStorage.getItem(AUTH_STORAGE_KEY)
}

export function setAuthToken(token: string | null, rememberMe = true): void {
  if (typeof window === 'undefined') return

  const target = rememberMe ? localStorage : sessionStorage
  const other = rememberMe ? sessionStorage : localStorage

  if (token) {
    target.setItem(AUTH_STORAGE_KEY, token)
    other.removeItem(AUTH_STORAGE_KEY)
    return
  }

  localStorage.removeItem(AUTH_STORAGE_KEY)
  sessionStorage.removeItem(AUTH_STORAGE_KEY)
}

function readErrorMessage(payload: any): string {
  const problemErrors = payload?.errors && typeof payload.errors === 'object' && !Array.isArray(payload.errors)
    ? Object.values(payload.errors).flat().join(', ')
    : undefined

  return payload?.error
    || (Array.isArray(payload?.errors) ? payload.errors.join(', ') : undefined)
    || problemErrors
    || payload?.detail
    || payload?.title
    || 'Request failed'
}

// --- Token refresh mutex ---
// Prevents concurrent refresh attempts from racing each other.
let _refreshPromise: Promise<string | null> | null = null

/**
 * Attempts to refresh the JWT using the backend refresh endpoint.
 * Uses a mutex so concurrent callers share the same in-flight request.
 * Returns the new token on success, or null on failure.
 */
export async function attemptTokenRefresh(): Promise<string | null> {
  if (_refreshPromise) return _refreshPromise

  _refreshPromise = (async () => {
    try {
      const staleToken = getStoredTokenForRefresh()
      if (!staleToken) return null

      const response = await fetch(`${getApiBaseUrl()}/auth/refresh`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${staleToken}`,
        },
      })

      if (!response.ok) return null

      const data = await response.json()
      const newToken = data?.token
      if (!newToken || typeof newToken !== 'string') return null

      // Persist with same storage preference
      const rememberMe = typeof window !== 'undefined' && localStorage.getItem(AUTH_STORAGE_KEY) !== null
      setAuthToken(newToken, rememberMe)
      return newToken
    } catch {
      return null
    } finally {
      _refreshPromise = null
    }
  })()

  return _refreshPromise
}

/** Auth endpoints that should never trigger automatic token refresh */
const AUTH_PATHS = ['/auth/login', '/auth/register', '/auth/refresh', '/auth/forgot-password', '/auth/reset-password']

function isAuthPath(path: string): boolean {
  return AUTH_PATHS.some(p => path === p || path.endsWith(p))
}

async function executeRequest<T>(path: string, options: RequestInit, headers: Record<string, string>): Promise<T> {
  const response = await fetch(`${getApiBaseUrl()}${path}`, {
    ...options,
    headers,
  })

  const contentType = response.headers.get('content-type') || ''
  const isJsonLike = contentType.includes('json') || contentType.includes('problem+json')

  if (!response.ok) {
    if (isJsonLike) {
      const payload = await response.json().catch(() => null)
      throw new ApiError(readErrorMessage(payload), response.status, payload?.detail, payload?.errors)
    }

    const text = await response.text().catch(() => '')
    const message = text.trim().startsWith('<!DOCTYPE') || text.trim().startsWith('<html')
      ? 'API endpoint returned HTML instead of JSON. Check frontend API base/proxy configuration.'
      : `Request failed (${response.status})`

    throw new ApiError(message, response.status)
  }

  if (response.status === 204) return undefined as T

  if (!isJsonLike) {
    const text = await response.text().catch(() => '')
    if (!text) return undefined as T
    throw new ApiError('Unexpected non-JSON API response', response.status)
  }

  return response.json() as Promise<T>
}

export async function apiRequest<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers: Record<string, string> = {
    ...(options.body ? { 'Content-Type': 'application/json' } : {}),
    ...(options.headers as Record<string, string> || {}),
  }

  const token = getAuthToken()
  if (token && !headers.Authorization) {
    // Proactively refresh if the token is about to expire
    if (!isAuthPath(path) && isTokenExpiringSoon(token)) {
      const refreshed = await attemptTokenRefresh()
      headers.Authorization = `Bearer ${refreshed || token}`
    } else {
      headers.Authorization = `Bearer ${token}`
    }
  }

  try {
    return await executeRequest<T>(path, options, headers)
  } catch (error) {
    // On 401 for non-auth paths, attempt one token refresh and retry
    if (error instanceof ApiError && error.status === 401 && !isAuthPath(path)) {
      const newToken = await attemptTokenRefresh()
      if (newToken) {
        headers.Authorization = `Bearer ${newToken}`
        return await executeRequest<T>(path, options, headers)
      }
    }
    throw error
  }
}
