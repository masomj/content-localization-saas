/**
 * Route middleware: blocks flagged app routes when their feature flag is off.
 */
export default defineNuxtRouteMiddleware((to) => {
  const { componentLibrary, integrations, screenshots, governance } = useFeatureFlags()

  const gatedRoutes = [
    { enabled: componentLibrary, prefixes: ['/app/components', '/app/library'] },
    { enabled: integrations, prefixes: ['/app/integrations'] },
    { enabled: screenshots, prefixes: ['/app/screenshots'] },
    { enabled: governance, prefixes: ['/app/governance'] },
  ]

  const blocked = gatedRoutes.some(({ enabled, prefixes }) => {
    if (enabled) return false
    return prefixes.some((prefix) => to.path === prefix || to.path.startsWith(prefix + '/'))
  })

  if (blocked) {
    return navigateTo('/app/dashboard')
  }
})
