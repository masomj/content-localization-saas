type ThemePreference = 'light' | 'dark' | 'system'
type ResolvedTheme = 'light' | 'dark'

const STORAGE_KEY = 'locflow-theme'

const preference = ref<ThemePreference>('system')
const resolved = ref<ResolvedTheme>('light')
const isInitialized = ref(false)

function getSystemTheme(): ResolvedTheme {
  if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') {
    return 'light'
  }

  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

function applyThemeDocument(theme: ResolvedTheme) {
  if (typeof document === 'undefined') {
    return
  }

  const root = document.documentElement
  root.setAttribute('data-theme', theme)
  root.style.colorScheme = theme
}

function resolveTheme(pref: ThemePreference): ResolvedTheme {
  if (pref === 'system') {
    return getSystemTheme()
  }

  return pref
}

function persistPreference(pref: ThemePreference) {
  if (typeof window === 'undefined') {
    return
  }

  window.localStorage.setItem(STORAGE_KEY, pref)
}

function loadPreference(): ThemePreference {
  if (typeof window === 'undefined') {
    return 'system'
  }

  const stored = window.localStorage.getItem(STORAGE_KEY)
  if (stored === 'light' || stored === 'dark' || stored === 'system') {
    return stored
  }

  return 'system'
}

function syncTheme(pref: ThemePreference) {
  const nextResolved = resolveTheme(pref)
  preference.value = pref
  resolved.value = nextResolved
  applyThemeDocument(nextResolved)
}

function initializeTheme() {
  if (isInitialized.value) {
    return
  }

  const loaded = loadPreference()
  syncTheme(loaded)

  if (typeof window !== 'undefined' && typeof window.matchMedia === 'function') {
    const media = window.matchMedia('(prefers-color-scheme: dark)')
    const onChange = () => {
      if (preference.value === 'system') {
        syncTheme('system')
      }
    }

    if (typeof media.addEventListener === 'function') {
      media.addEventListener('change', onChange)
    } else {
      media.addListener(onChange)
    }
  }

  isInitialized.value = true
}

function setThemePreference(next: ThemePreference) {
  syncTheme(next)
  persistPreference(next)
}

function cycleThemePreference() {
  const order: ThemePreference[] = ['light', 'dark', 'system']
  const currentIndex = order.indexOf(preference.value)
  const nextIndex = (currentIndex + 1) % order.length
  setThemePreference(order[nextIndex] ?? 'system')
}

export function useTheme() {
  return {
    preference: readonly(preference),
    resolved: readonly(resolved),
    initializeTheme,
    setThemePreference,
    cycleThemePreference,
  }
}
