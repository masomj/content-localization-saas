import { UserManager, WebStorageStateStore, type User, type UserManagerSettings } from 'oidc-client-ts'

let manager: UserManager | null = null

function getOrigin(): string {
  if (typeof window === 'undefined') {
    throw new Error('OIDC manager is only available in browser context')
  }
  return window.location.origin
}

function getRedirectUri(path: string): string {
  return `${getOrigin()}${path}`
}

export interface OidcConfig {
  authority: string
  realm: string
  clientId: string
}

function buildSettings(config: OidcConfig): UserManagerSettings {
  return {
    authority: `${config.authority}/realms/${config.realm}`,
    client_id: config.clientId,
    redirect_uri: getRedirectUri('/auth/callback'),
    post_logout_redirect_uri: getRedirectUri('/login'),
    response_type: 'code',
    scope: 'openid profile email offline_access',
    userStore: new WebStorageStateStore({
      store: window.localStorage,
      prefix: 'intercopy.oidc.',
    }),
    automaticSilentRenew: false,
    monitorSession: false,
    loadUserInfo: true,
  }
}

export function getOidcManager(config: OidcConfig): UserManager {
  if (typeof window === 'undefined') {
    throw new Error('OIDC manager is only available in browser context')
  }

  if (!manager) {
    manager = new UserManager(buildSettings(config))
  }

  return manager
}

export async function getOidcUser(config: OidcConfig): Promise<User | null> {
  if (typeof window === 'undefined') return null

  const user = await getOidcManager(config).getUser()
  if (!user) return null

  if (!user.expired) return user

  try {
    return await getOidcManager(config).signinSilent()
  } catch {
    return null
  }
}

export async function refreshOidcUser(config: OidcConfig): Promise<User | null> {
  if (typeof window === 'undefined') return null

  try {
    return await getOidcManager(config).signinSilent()
  } catch {
    return null
  }
}

export async function completeOidcCallback(config: OidcConfig): Promise<User> {
  return getOidcManager(config).signinCallback()
}

export async function startOidcLogin(config: OidcConfig, uiTheme: 'light' | 'dark'): Promise<void> {
  await getOidcManager(config).signinRedirect({
    extraQueryParams: {
      ui_theme: uiTheme,
    },
  })
}

export async function startOidcLogout(config: OidcConfig): Promise<void> {
  await getOidcManager(config).signoutRedirect()
}

export async function clearOidcSession(config: OidcConfig): Promise<void> {
  try {
    await getOidcManager(config).removeUser()
  } catch {
    // ignore best-effort clear
  }
}
