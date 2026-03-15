export default defineNuxtRouteMiddleware((to) => {
  const auth = useAuth()

  if (!auth.isAdmin.value) {
    return navigateTo('/app/dashboard')
  }
})
