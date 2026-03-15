const publicPaths = ['/', '/login', '/register']

export default defineNuxtRouteMiddleware((to) => {
  if (publicPaths.includes(to.path)) {
    return
  }
  
  const auth = useAuth()
  
  if (!auth.isAuthenticated.value) {
    return navigateTo('/login')
  }
})
