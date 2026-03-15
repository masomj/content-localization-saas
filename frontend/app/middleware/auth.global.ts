const publicPaths = ['/', '/login', '/register']

export default defineNuxtRouteMiddleware((to) => {
  const isPublicPath = publicPaths.includes(to.path)
  
  if (isPublicPath) {
    return
  }

  const isOnboardingPath = to.path.startsWith('/onboarding')
  if (isOnboardingPath) {
    return
  }

  const auth = useAuth()
  
  if (!auth.isAuthenticated.value) {
    return navigateTo('/login')
  }

  if (!auth.hasOrganization.value) {
    return navigateTo('/onboarding/organisation')
  }
})
