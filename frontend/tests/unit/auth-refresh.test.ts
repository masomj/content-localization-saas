import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'

vi.mock('~/lib/oidc', () => ({
  refreshOidcUser: vi.fn(),
}))

function makeJwt(exp: number): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }))
  const payload = btoa(JSON.stringify({ exp, sub: 'user-1' }))
  return `${header}.${payload}.fake-signature`
}

function nowEpoch(): number {
  return Math.floor(Date.now() / 1000)
}

const AUTH_STORAGE_KEY = 'locflow_auth_token'

let localStore: Record<string, string>
let sessionStore: Record<string, string>

function createStorageMock(store: Record<string, string>) {
  return {
    getItem: (key: string) => store[key] ?? null,
    setItem: (key: string, value: string) => { store[key] = value },
    removeItem: (key: string) => { delete store[key] },
    clear: () => { Object.keys(store).forEach(k => delete store[k]) },
  }
}

describe('Auth Token Refresh – client.ts', () => {
  beforeEach(() => {
    localStore = {}
    sessionStore = {}

    vi.stubGlobal('localStorage', createStorageMock(localStore))
    vi.stubGlobal('sessionStorage', createStorageMock(sessionStore))
    vi.stubGlobal('window', globalThis)
    vi.stubGlobal('useRuntimeConfig', () => ({
      public: {
        keycloakUrl: 'http://localhost:8080',
        keycloakRealm: 'locflow',
        keycloakClientId: 'locflow-web',
      },
    }))
  })

  afterEach(() => {
    vi.restoreAllMocks()
    vi.resetModules()
  })

  it('getAuthToken returns the latest non-expired token', async () => {
    const shortToken = makeJwt(nowEpoch() + 100)
    const longToken = makeJwt(nowEpoch() + 7200)
    localStore[AUTH_STORAGE_KEY] = shortToken
    sessionStore[AUTH_STORAGE_KEY] = longToken

    const { getAuthToken } = await import('~/api/client')
    expect(getAuthToken()).toBe(longToken)
  })

  it('attemptTokenRefresh uses OIDC silent refresh and stores token', async () => {
    const newToken = makeJwt(nowEpoch() + 3600)
    const oidc = await import('~/lib/oidc')
    vi.mocked(oidc.refreshOidcUser).mockResolvedValue({ access_token: newToken } as any)

    const { attemptTokenRefresh } = await import('~/api/client')
    const result = await attemptTokenRefresh()

    expect(result).toBe(newToken)
    expect(localStore[AUTH_STORAGE_KEY]).toBe(newToken)
  })

  it('attemptTokenRefresh deduplicates concurrent refresh calls', async () => {
    const newToken = makeJwt(nowEpoch() + 3600)
    let resolveRefresh!: (value: any) => void

    const oidc = await import('~/lib/oidc')
    vi.mocked(oidc.refreshOidcUser).mockClear()
    vi.mocked(oidc.refreshOidcUser).mockReturnValue(
      new Promise((resolve) => {
        resolveRefresh = resolve
      }) as any,
    )

    const { attemptTokenRefresh } = await import('~/api/client')

    const p1 = attemptTokenRefresh()
    const p2 = attemptTokenRefresh()
    const p3 = attemptTokenRefresh()

    expect(oidc.refreshOidcUser).toHaveBeenCalledTimes(1)

    resolveRefresh({ access_token: newToken })
    const [r1, r2, r3] = await Promise.all([p1, p2, p3])

    expect(r1).toBe(newToken)
    expect(r2).toBe(newToken)
    expect(r3).toBe(newToken)
  })

  it('apiRequest retries once after 401 when refresh succeeds', async () => {
    const staleToken = makeJwt(nowEpoch() + 300)
    const newToken = makeJwt(nowEpoch() + 3600)
    localStore[AUTH_STORAGE_KEY] = staleToken

    const oidc = await import('~/lib/oidc')
    vi.mocked(oidc.refreshOidcUser).mockResolvedValue({ access_token: newToken } as any)

    const fetchMock = vi.fn()
      .mockResolvedValueOnce({
        ok: false,
        status: 401,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: () => Promise.resolve({ error: 'Unauthorized' }),
      })
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
    expect(fetchMock).toHaveBeenCalledTimes(2)
  })
})
