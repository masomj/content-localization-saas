/**
 * Route middleware: blocks component library pages when the feature flag is off.
 * Pages: /app/components, /app/components/:id, /app/library, /app/library/:id
 */
export default defineNuxtRouteMiddleware((to) => {
  const { componentLibrary } = useFeatureFlags()
  if (!componentLibrary) {
    const blocked = ['/app/components', '/app/library']
    if (blocked.some((prefix) => to.path === prefix || to.path.startsWith(prefix + '/'))) {
      return navigateTo('/app/dashboard')
    }
  }
})
