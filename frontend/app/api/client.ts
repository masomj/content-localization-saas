const AUTH_STORAGE_KEY = 'locflow_auth_token'

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

function decodeJwtExp(token: string | null): number {
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

  if (localToken && sessionToken) {
    if (localExp === sessionExp) return localToken
    return localExp > sessionExp ? localToken : sessionToken
  }

  return localToken || sessionToken
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

export async function apiRequest<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers: Record<string, string> = {
    ...(options.body ? { 'Content-Type': 'application/json' } : {}),
    ...(options.headers as Record<string, string> || {}),
  }

  const token = getAuthToken()
  if (token && !headers.Authorization) {
    headers.Authorization = `Bearer ${token}`
  }

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
