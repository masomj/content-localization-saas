import type { Middleware } from '.'

export const publicRoutes: string[] = ['/', '/login', '/register']

export const protectedRoutes: string[] = ['/dashboard', '/projects', '/settings', '/team']

export const routeGuardMiddleware: Middleware = (to) => {
  const auth = useAuth()
  
  const isPublicRoute = publicRoutes.some(route => {
    if (route === '/') return to.path === '/'
    return to.path.startsWith(route)
  })
  
  const isProtectedRoute = protectedRoutes.some(route => to.path.startsWith(route))
  
  if (isProtectedRoute && !auth.isAuthenticated.value) {
    return navigateTo('/login')
  }
  
  if (isPublicRoute && auth.isAuthenticated.value && to.path === '/') {
    return navigateTo('/dashboard')
  }
}
