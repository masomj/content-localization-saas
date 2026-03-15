const publicPaths = ['/', '/login', '/register']

export default defineNuxtRouteMiddleware((to) => {
  const isPublicPath = publicPaths.includes(to.path)
  
  if (isPublicPath) {
    return
  }

  const auth = useAuth()
  
  if (!auth.isAuthenticated.value) {
    return navigateTo('/login')
  }
})
