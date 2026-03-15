export default defineNuxtRouteMiddleware(() => {
  const auth = useAuth()

  if (!auth.isAdmin.value) {
    return navigateTo('/app/dashboard')
  }
})
