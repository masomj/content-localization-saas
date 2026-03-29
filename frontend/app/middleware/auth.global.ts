import { waitForAuthReady } from '~/composables/useAuth'

const publicPaths = ['/', '/login', '/register', '/forgot-password', '/reset-password', '/auth/callback']

export default defineNuxtRouteMiddleware(async (to) => {
  const isPublicPath = publicPaths.includes(to.path)
  if (isPublicPath) return

  const isDocsPath = to.path.startsWith('/docs')
  if (isDocsPath) return

  const isOnboardingPath = to.path.startsWith('/onboarding')
  if (isOnboardingPath) return

  if (import.meta.server) {
    const cookieHeader = useRequestHeaders(['cookie']).cookie || ''
    const hasToken = cookieHeader.split('; ').some(c => {
      const [key, ...rest] = c.split('=')
      return key === 'InterCopy_auth_token' && rest.join('=').length > 0
    })
    if (!hasToken) return navigateTo('/login')
    return
  }

  const runtime = useRuntimeConfig().public
  await waitForAuthReady({
    keycloakUrl: runtime.keycloakUrl,
    keycloakRealm: runtime.keycloakRealm,
    keycloakClientId: runtime.keycloakClientId,
  })
  const auth = useAuth()

  if (!auth.isAuthenticated.value) {
    return navigateTo('/login')
  }

  if (!auth.hasOrganization.value) {
    return navigateTo('/onboarding/organisation')
  }
})
