import { refreshOidcUser } from '~/lib/oidc'

const AUTH_STORAGE_KEY = 'InterCopy_auth_token'
const ORG_STORAGE_KEY = 'InterCopy_organization'
const TOKEN_EXPIRY_BUFFER_SECONDS = 60

function parseCookie(name: string): string | null {
  if (typeof document === 'undefined') return null
  const cookies = document.cookie.split('; ')
  for (const c of cookies) {
    const [key, ...rest] = c.split('=')
    if (key === name) return decodeURIComponent(rest.join('='))
  }
  return null
}

function writeCookie(name: string, value: string | null, persistent = true): void {
  if (typeof document === 'undefined') return
  if (value === null) {
    document.cookie = `${name}=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT; SameSite=Lax`
    return
  }
  let cookie = `${name}=${encodeURIComponent(value)}; path=/; SameSite=Lax`
  if (persistent) {
    const expires = new Date(Date.now() + 30 * 864e5).toUTCString()
    cookie += `; expires=${expires}`
  }
  if (typeof location !== 'undefined' && location.protocol === 'https:') cookie += '; Secure'
  document.cookie = cookie
}

function readStorage(key: string): string | null {
  if (typeof localStorage === 'undefined' || typeof sessionStorage === 'undefined') return parseCookie(key)
  const local = localStorage.getItem(key)
  const session = sessionStorage.getItem(key)
  if (!local && !session) return parseCookie(key)
  return local ?? session
}

function writeStorage(key: string, value: string | null, rememberMe = true): void {
  if (typeof localStorage !== 'undefined' && typeof sessionStorage !== 'undefined') {
    if (value === null) {
      localStorage.removeItem(key)
      sessionStorage.removeItem(key)
    } else if (rememberMe) {
      localStorage.setItem(key, value)
      sessionStorage.removeItem(key)
    } else {
      sessionStorage.setItem(key, value)
      localStorage.removeItem(key)
    }
  }
  writeCookie(key, value, rememberMe)
}

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

function getApiBaseUrl(): string { return '/api' }

function getStoredWorkspaceId(): string | null {
  if (typeof localStorage === 'undefined') return null
  try {
    const raw = localStorage.getItem(ORG_STORAGE_KEY)
    if (!raw) return null
    const parsed = JSON.parse(raw)
    return typeof parsed?.id === 'string' ? parsed.id : null
  } catch {
    return null
  }
}

export function decodeJwtExp(token: string | null): number {
  if (!token) return 0
  try { return Number(JSON.parse(atob(token.split('.')[1] || '')).exp || 0) } catch { return 0 }
}

function isNotExpired(exp: number): boolean { return exp > Math.floor(Date.now() / 1000) }

export function isTokenExpiringSoon(token: string | null): boolean {
  const exp = decodeJwtExp(token)
  if (exp === 0) return false
  const now = Math.floor(Date.now() / 1000)
  return exp > now && exp - now <= TOKEN_EXPIRY_BUFFER_SECONDS
}

export function getAuthToken(): string | null {
  const local = typeof localStorage !== 'undefined' ? localStorage.getItem(AUTH_STORAGE_KEY) : null
  const session = typeof sessionStorage !== 'undefined' ? sessionStorage.getItem(AUTH_STORAGE_KEY) : null
  const cookie = parseCookie(AUTH_STORAGE_KEY)

  const candidates = [local, session, cookie].filter(Boolean) as string[]
  let best: string | null = null
  let bestExp = -1
  for (const token of candidates) {
    const exp = decodeJwtExp(token)
    if (!isNotExpired(exp)) continue
    if (exp > bestExp) {
      best = token
      bestExp = exp
    }
  }
  return best
}

export function getStoredTokenForRefresh(): string | null {
  return readStorage(AUTH_STORAGE_KEY)
}

export function setAuthToken(token: string | null, rememberMe = true): void {
  writeStorage(AUTH_STORAGE_KEY, token, rememberMe)
}

function readErrorMessage(payload: any): string {
  const problemErrors = payload?.errors && typeof payload.errors === 'object' && !Array.isArray(payload.errors)
    ? Object.values(payload.errors).flat().join(', ')
    : undefined
  return payload?.error || (Array.isArray(payload?.errors) ? payload.errors.join(', ') : undefined) || problemErrors || payload?.detail || payload?.title || 'Request failed'
}

let _refreshPromise: Promise<string | null> | null = null

export async function attemptTokenRefresh(): Promise<string | null> {
  if (_refreshPromise) return _refreshPromise

  _refreshPromise = (async () => {
    try {
      if (typeof window === 'undefined') return null

      const config = useRuntimeConfig().public
      const user = await refreshOidcUser({
        authority: config.keycloakUrl,
        realm: config.keycloakRealm,
        clientId: config.keycloakClientId,
      })

      const token = user?.access_token ?? null
      setAuthToken(token, true)
      return token
    } catch {
      return null
    } finally {
      _refreshPromise = null
    }
  })()

  return _refreshPromise
}

const AUTH_PATHS = ['/auth/login', '/auth/register', '/auth/refresh', '/auth/forgot-password', '/auth/reset-password']
const isAuthPath = (path: string) => AUTH_PATHS.some(p => path === p || path.endsWith(p))

async function executeRequest<T>(path: string, options: RequestInit, headers: Record<string, string>): Promise<T> {
  const response = await fetch(`${getApiBaseUrl()}${path}`, { ...options, headers })
  const contentType = response.headers?.get?.('content-type') || ''
  const isJsonLike = contentType.includes('json') || contentType.includes('problem+json')
  if (!response.ok) {
    if (isJsonLike) {
      const payload = await response.json().catch(() => null)
      throw new ApiError(readErrorMessage(payload), response.status, payload?.detail, payload?.errors)
    }
    throw new ApiError(`Request failed (${response.status})`, response.status)
  }
  if (response.status === 204) return undefined as T
  if (isJsonLike || typeof (response as any).json === 'function') return response.json() as Promise<T>
  return undefined as T
}

export async function apiRequest<T>(path: string, options: RequestInit = {}): Promise<T> {
  const isFormData = typeof FormData !== 'undefined' && options.body instanceof FormData
  const headers: Record<string, string> = { ...(options.body && !isFormData ? { 'Content-Type': 'application/json' } : {}), ...(options.headers as Record<string, string> || {}) }
  const workspaceId = getStoredWorkspaceId()
  if (workspaceId && !headers['X-Workspace-Id']) {
    headers['X-Workspace-Id'] = workspaceId
  }

  const token = getAuthToken()
  if (token && !headers.Authorization) {
    if (!isAuthPath(path) && isTokenExpiringSoon(token)) {
      const refreshed = await attemptTokenRefresh()
      headers.Authorization = `Bearer ${refreshed || token}`
    } else headers.Authorization = `Bearer ${token}`
  }
  try {
    return await executeRequest<T>(path, options, headers)
  } catch (error) {
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
