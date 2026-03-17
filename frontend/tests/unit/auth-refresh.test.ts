import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'

// --- JWT helpers (mirror the production logic) ---

function makeJwt(exp: number): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }))
  const payload = btoa(JSON.stringify({ exp, sub: 'user-1' }))
  return `${header}.${payload}.fake-signature`
}

function nowEpoch(): number {
  return Math.floor(Date.now() / 1000)
}

// --- Storage mocks ---
const AUTH_STORAGE_KEY = 'locflow_auth_token'

let localStore: Record<string, string>
let sessionStore: Record<string, string>

function createStorageMock(store: Record<string, string>) {
  return {
    getItem: (key: string) => store[key] ?? null,
    setItem: (key: string, value: string) => { store[key] = value },
    removeItem: (key: string) => { delete store[key] },
    clear: () => { Object.keys(store).forEach(k => delete store[k]) },
    get length() { return Object.keys(store).length },
    key: (index: number) => Object.keys(store)[index] ?? null,
  }
}

// ============================================================
// Tests for client.ts functions (imported dynamically to get fresh module state)
// ============================================================

describe('Auth Token Refresh – client.ts', () => {
  beforeEach(() => {
    localStore = {}
    sessionStore = {}
    vi.stubGlobal('localStorage', createStorageMock(localStore))
    vi.stubGlobal('sessionStorage', createStorageMock(sessionStore))
    vi.stubGlobal('window', globalThis)
  })

  afterEach(() => {
    vi.restoreAllMocks()
    vi.resetModules()
  })

  describe('getAuthToken', () => {
    it('returns null when no tokens are stored', async () => {
      const { getAuthToken } = await import('~/api/client')
      expect(getAuthToken()).toBeNull()
    })

    it('returns a valid token from localStorage', async () => {
      const validToken = makeJwt(nowEpoch() + 3600)
      localStore[AUTH_STORAGE_KEY] = validToken
      const { getAuthToken } = await import('~/api/client')
      expect(getAuthToken()).toBe(validToken)
    })

    it('returns a valid token from sessionStorage', async () => {
      const validToken = makeJwt(nowEpoch() + 3600)
      sessionStore[AUTH_STORAGE_KEY] = validToken
      const { getAuthToken } = await import('~/api/client')
      expect(getAuthToken()).toBe(validToken)
    })

    it('returns null when all stored tokens are expired (bug fix)', async () => {
      const expiredToken = makeJwt(nowEpoch() - 100)
      localStore[AUTH_STORAGE_KEY] = expiredToken
      sessionStore[AUTH_STORAGE_KEY] = expiredToken
      const { getAuthToken } = await import('~/api/client')
      expect(getAuthToken()).toBeNull()
    })

    it('returns the valid token when one is expired and the other is valid', async () => {
      const expiredToken = makeJwt(nowEpoch() - 100)
      const validToken = makeJwt(nowEpoch() + 3600)
      localStore[AUTH_STORAGE_KEY] = expiredToken
      sessionStore[AUTH_STORAGE_KEY] = validToken
      const { getAuthToken } = await import('~/api/client')
      expect(getAuthToken()).toBe(validToken)
    })

    it('returns the token with the latest expiry when both are valid', async () => {
      const shortToken = makeJwt(nowEpoch() + 100)
      const longToken = makeJwt(nowEpoch() + 7200)
      localStore[AUTH_STORAGE_KEY] = shortToken
      sessionStore[AUTH_STORAGE_KEY] = longToken
      const { getAuthToken } = await import('~/api/client')
      expect(getAuthToken()).toBe(longToken)
    })
  })

  describe('getStoredTokenForRefresh', () => {
    it('returns expired token from localStorage (for refresh endpoint use)', async () => {
      const expiredToken = makeJwt(nowEpoch() - 100)
      localStore[AUTH_STORAGE_KEY] = expiredToken
      const { getStoredTokenForRefresh } = await import('~/api/client')
      expect(getStoredTokenForRefresh()).toBe(expiredToken)
    })

    it('falls back to sessionStorage when localStorage is empty', async () => {
      const expiredToken = makeJwt(nowEpoch() - 100)
      sessionStore[AUTH_STORAGE_KEY] = expiredToken
      const { getStoredTokenForRefresh } = await import('~/api/client')
      expect(getStoredTokenForRefresh()).toBe(expiredToken)
    })

    it('returns null when no token is stored anywhere', async () => {
      const { getStoredTokenForRefresh } = await import('~/api/client')
      expect(getStoredTokenForRefresh()).toBeNull()
    })
  })

  describe('isTokenExpiringSoon', () => {
    it('returns false for a token with plenty of time remaining', async () => {
      const token = makeJwt(nowEpoch() + 3600)
      const { isTokenExpiringSoon } = await import('~/api/client')
      expect(isTokenExpiringSoon(token)).toBe(false)
    })

    it('returns true for a token expiring within the buffer window', async () => {
      const token = makeJwt(nowEpoch() + 30) // 30s remaining, buffer is 60s
      const { isTokenExpiringSoon } = await import('~/api/client')
      expect(isTokenExpiringSoon(token)).toBe(true)
    })

    it('returns false for an already-expired token', async () => {
      const token = makeJwt(nowEpoch() - 10)
      const { isTokenExpiringSoon } = await import('~/api/client')
      expect(isTokenExpiringSoon(token)).toBe(false)
    })

    it('returns false for null', async () => {
      const { isTokenExpiringSoon } = await import('~/api/client')
      expect(isTokenExpiringSoon(null)).toBe(false)
    })
  })

  describe('decodeJwtExp', () => {
    it('extracts exp from a valid JWT', async () => {
      const exp = nowEpoch() + 3600
      const token = makeJwt(exp)
      const { decodeJwtExp } = await import('~/api/client')
      expect(decodeJwtExp(token)).toBe(exp)
    })

    it('returns 0 for null', async () => {
      const { decodeJwtExp } = await import('~/api/client')
      expect(decodeJwtExp(null)).toBe(0)
    })

    it('returns 0 for malformed tokens', async () => {
      const { decodeJwtExp } = await import('~/api/client')
      expect(decodeJwtExp('not.a.jwt')).toBe(0)
    })
  })

  describe('attemptTokenRefresh', () => {
    it('returns new token on successful refresh', async () => {
      const expiredToken = makeJwt(nowEpoch() - 100)
      const newToken = makeJwt(nowEpoch() + 3600)
      localStore[AUTH_STORAGE_KEY] = expiredToken

      vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
        ok: true,
        json: () => Promise.resolve({ token: newToken }),
      }))

      const { attemptTokenRefresh } = await import('~/api/client')
      const result = await attemptTokenRefresh()
      expect(result).toBe(newToken)
      expect(localStore[AUTH_STORAGE_KEY]).toBe(newToken)
    })

    it('returns null when refresh endpoint returns non-ok', async () => {
      const expiredToken = makeJwt(nowEpoch() - 100)
      localStore[AUTH_STORAGE_KEY] = expiredToken

      vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
        ok: false,
        status: 401,
      }))

      const { attemptTokenRefresh } = await import('~/api/client')
      const result = await attemptTokenRefresh()
      expect(result).toBeNull()
    })

    it('returns null when no stored token exists', async () => {
      vi.stubGlobal('fetch', vi.fn())

      const { attemptTokenRefresh } = await import('~/api/client')
      const result = await attemptTokenRefresh()
      expect(result).toBeNull()
      expect(fetch).not.toHaveBeenCalled()
    })

    it('deduplicates concurrent refresh calls (mutex)', async () => {
      const expiredToken = makeJwt(nowEpoch() - 100)
      const newToken = makeJwt(nowEpoch() + 3600)
      localStore[AUTH_STORAGE_KEY] = expiredToken

      let resolveRefresh!: (value: any) => void
      vi.stubGlobal('fetch', vi.fn().mockReturnValue(
        new Promise(resolve => { resolveRefresh = resolve })
      ))

      const { attemptTokenRefresh } = await import('~/api/client')

      // Fire three concurrent refresh attempts
      const p1 = attemptTokenRefresh()
      const p2 = attemptTokenRefresh()
      const p3 = attemptTokenRefresh()

      // Only one fetch call should have been made
      expect(fetch).toHaveBeenCalledTimes(1)

      // Resolve the single in-flight request
      resolveRefresh({
        ok: true,
        json: () => Promise.resolve({ token: newToken }),
      })

      const [r1, r2, r3] = await Promise.all([p1, p2, p3])
      expect(r1).toBe(newToken)
      expect(r2).toBe(newToken)
      expect(r3).toBe(newToken)
    })

    it('returns null on network error', async () => {
      const expiredToken = makeJwt(nowEpoch() - 100)
      localStore[AUTH_STORAGE_KEY] = expiredToken

      vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('Network error')))

      const { attemptTokenRefresh } = await import('~/api/client')
      const result = await attemptTokenRefresh()
      expect(result).toBeNull()
    })
  })

  describe('apiRequest – 401 retry with refresh', () => {
    it('retries once on 401 and succeeds with refreshed token', async () => {
      const expiredToken = makeJwt(nowEpoch() + 300) // technically valid but server rejects
      const newToken = makeJwt(nowEpoch() + 3600)
      localStore[AUTH_STORAGE_KEY] = expiredToken

      const fetchMock = vi.fn()
        // First call: the original request → 401
        .mockResolvedValueOnce({
          ok: false,
          status: 401,
          headers: new Headers({ 'content-type': 'application/json' }),
          json: () => Promise.resolve({ error: 'Unauthorized' }),
        })
        // Second call: refresh endpoint → success
        .mockResolvedValueOnce({
          ok: true,
          json: () => Promise.resolve({ token: newToken }),
        })
        // Third call: retried original request → success
        .mockResolvedValueOnce({
          ok: true,
          status: 200,
          headers: new Headers({ 'content-type': 'application/json' }),
          json: () => Promise.resolve({ data: 'success' }),
        })

      vi.stubGlobal('fetch', fetchMock)

      const { apiRequest } = await import('~/api/client')
      const result = await apiRequest<{ data: string }>('/some/endpoint')
      expect(result).toEqual({ data: 'success' })
      expect(fetchMock).toHaveBeenCalledTimes(3)
    })

    it('does not retry on 401 for auth paths', async () => {
      const validToken = makeJwt(nowEpoch() + 300)
      localStore[AUTH_STORAGE_KEY] = validToken

      const fetchMock = vi.fn().mockResolvedValueOnce({
        ok: false,
        status: 401,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: () => Promise.resolve({ error: 'Bad credentials' }),
      })

      vi.stubGlobal('fetch', fetchMock)

      const { apiRequest, ApiError } = await import('~/api/client')
      await expect(apiRequest('/auth/login', { method: 'POST', body: '{}' }))
        .rejects.toThrow(ApiError)
      // Only the original call, no refresh or retry
      expect(fetchMock).toHaveBeenCalledTimes(1)
    })

    it('throws after failed refresh on 401', async () => {
      const expiredToken = makeJwt(nowEpoch() + 300)
      localStore[AUTH_STORAGE_KEY] = expiredToken

      const fetchMock = vi.fn()
        // Original request → 401
        .mockResolvedValueOnce({
          ok: false,
          status: 401,
          headers: new Headers({ 'content-type': 'application/json' }),
          json: () => Promise.resolve({ error: 'Unauthorized' }),
        })
        // Refresh → also fails
        .mockResolvedValueOnce({
          ok: false,
          status: 401,
        })

      vi.stubGlobal('fetch', fetchMock)

      const { apiRequest, ApiError } = await import('~/api/client')
      await expect(apiRequest('/some/endpoint')).rejects.toThrow(ApiError)
      expect(fetchMock).toHaveBeenCalledTimes(2) // original + refresh attempt
    })

    it('does not retry on non-401 errors', async () => {
      const validToken = makeJwt(nowEpoch() + 3600)
      localStore[AUTH_STORAGE_KEY] = validToken

      const fetchMock = vi.fn().mockResolvedValueOnce({
        ok: false,
        status: 500,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: () => Promise.resolve({ error: 'Server error' }),
      })

      vi.stubGlobal('fetch', fetchMock)

      const { apiRequest, ApiError } = await import('~/api/client')
      await expect(apiRequest('/some/endpoint')).rejects.toThrow(ApiError)
      expect(fetchMock).toHaveBeenCalledTimes(1)
    })
  })

  describe('setAuthToken', () => {
    it('stores token in localStorage when rememberMe is true', async () => {
      const token = makeJwt(nowEpoch() + 3600)
      const { setAuthToken } = await import('~/api/client')
      setAuthToken(token, true)
      expect(localStore[AUTH_STORAGE_KEY]).toBe(token)
      expect(sessionStore[AUTH_STORAGE_KEY]).toBeUndefined()
    })

    it('stores token in sessionStorage when rememberMe is false', async () => {
      const token = makeJwt(nowEpoch() + 3600)
      const { setAuthToken } = await import('~/api/client')
      setAuthToken(token, false)
      expect(sessionStore[AUTH_STORAGE_KEY]).toBe(token)
      expect(localStore[AUTH_STORAGE_KEY]).toBeUndefined()
    })

    it('clears both stores when token is null', async () => {
      localStore[AUTH_STORAGE_KEY] = 'old-token'
      sessionStore[AUTH_STORAGE_KEY] = 'old-token'
      const { setAuthToken } = await import('~/api/client')
      setAuthToken(null)
      expect(localStore[AUTH_STORAGE_KEY]).toBeUndefined()
      expect(sessionStore[AUTH_STORAGE_KEY]).toBeUndefined()
    })
  })
})
