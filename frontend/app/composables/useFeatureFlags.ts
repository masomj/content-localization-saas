/**
 * Feature flags composable.
 * Flags are injected at build time via nuxt.config runtimeConfig.public.featureFlags.
 * To enable a flag locally, set the corresponding env var in .env or your shell.
 *
 *   NUXT_PUBLIC_FF_COMPONENT_LIBRARY=true  → enables /app/components + /app/library
 */
export function useFeatureFlags() {
  const config = useRuntimeConfig()
  const flags = config.public.featureFlags as { componentLibrary: boolean }
  return {
    componentLibrary: flags?.componentLibrary ?? false,
  }
}
