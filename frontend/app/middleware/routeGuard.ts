import { getAuthToken } from '~/api/client'
import type { Middleware } from '.'

export const publicRoutes: string[] = ['/', '/login', '/register']

export const protectedRoutes: string[] = ['/app']

export const routeGuardMiddleware: Middleware = (to) => {
  const auth = useAuth()
  
  const isPublicRoute = publicRoutes.some(route => {
    if (route === '/') return to.path === '/'
    return to.path.startsWith(route)
  })
  
  const isProtectedRoute = protectedRoutes.some(route => to.path.startsWith(route))
  
  if (isProtectedRoute) {
    const hasToken = typeof window !== 'undefined' && !!getAuthToken()

    if (auth.isLoading.value) {
      if (hasToken) return
      return navigateTo('/login')
    }

    if (!auth.isAuthenticated.value) {
      return navigateTo('/login')
    }
  }
  
  if (isPublicRoute && auth.isAuthenticated.value && to.path === '/') {
    return navigateTo('/dashboard')
  }
}
