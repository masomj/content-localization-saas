/**
 * Feature flags composable.
 * Flags are injected at build time via nuxt.config runtimeConfig.public.featureFlags.
 * To enable a flag locally, set the corresponding env var in .env or your shell.
 *
 *   NUXT_PUBLIC_FF_COMPONENT_LIBRARY=true  → enables /app/components + /app/library
 *   NUXT_PUBLIC_FF_INTEGRATIONS=true       → enables /app/integrations
 *   NUXT_PUBLIC_FF_SCREENSHOTS=true        → enables /app/screenshots + visual context
 *   NUXT_PUBLIC_FF_GOVERNANCE=true         → enables /app/governance
 */
export function useFeatureFlags() {
  const config = useRuntimeConfig()
  const flags = config.public.featureFlags as {
    componentLibrary: boolean
    integrations: boolean
    screenshots: boolean
    governance: boolean
  }
  return {
    componentLibrary: flags?.componentLibrary ?? false,
    integrations: flags?.integrations ?? false,
    screenshots: flags?.screenshots ?? false,
    governance: flags?.governance ?? false,
  }
}
