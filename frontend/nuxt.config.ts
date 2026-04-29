// Resolve the backend URL from Aspire service-discovery env vars, falling
// back to the default standalone dev port.
const apiTarget =
  process.env.services__api__https__0 ||
  process.env.services__api__http__0 ||
  'http://localhost:5135'

export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  devtools: { enabled: true },
  css: ['~/assets/css/tokens.css'],
  runtimeConfig: {
    public: {
      apiBase: '/api',
      keycloakUrl: process.env.NUXT_PUBLIC_KEYCLOAK_URL || 'http://localhost:8080',
      keycloakRealm: process.env.NUXT_PUBLIC_KEYCLOAK_REALM || 'intercopy',
      keycloakClientId: process.env.NUXT_PUBLIC_KEYCLOAK_CLIENT_ID || 'intercopy-web',
      featureFlags: {
        // Set NUXT_PUBLIC_FF_COMPONENT_LIBRARY=true to enable the component library feature
        componentLibrary: process.env.NUXT_PUBLIC_FF_COMPONENT_LIBRARY === 'true',
        // Set NUXT_PUBLIC_FF_INTEGRATIONS=true to enable integrations pages/navigation
        integrations: process.env.NUXT_PUBLIC_FF_INTEGRATIONS === 'true',
        // Set NUXT_PUBLIC_FF_SCREENSHOTS=true to enable screenshot management and visual context
        screenshots: process.env.NUXT_PUBLIC_FF_SCREENSHOTS === 'true',
        // Set NUXT_PUBLIC_FF_GOVERNANCE=true to enable governance pages/navigation
        governance: process.env.NUXT_PUBLIC_FF_GOVERNANCE === 'true',
      },
    },
  },
  nitro: {
    devProxy: {
      '/api': {
        target: `${apiTarget}/api`,
        changeOrigin: true,
      },
    },
  },
  app: {
    head: {
      htmlAttrs: { lang: 'en' },
      title: 'InterCopy - Translate Your Content. Scale Globally.',
      charset: 'utf-8',
      viewport: 'width=device-width, initial-scale=1',
      meta: [
        { name: 'description', content: 'The all-in-one localization platform that helps teams manage translations, collaborate with reviewers, and deliver localized content to every market.' },
        { name: 'theme-color', content: '#4f46e5' },
        { property: 'og:title', content: 'InterCopy - Translate Your Content. Scale Globally.' },
        { property: 'og:description', content: 'The all-in-one localization platform that helps teams manage translations, collaborate with reviewers, and deliver localized content to every market.' },
        { property: 'og:type', content: 'website' },
        { name: 'twitter:card', content: 'summary_large_image' },
      ],
      link: [
        { rel: 'icon', type: 'image/x-icon', href: '/favicon.ico' },
      ],
      script: [
        {
          id: 'theme-bootstrap',
          innerHTML: "(function(){try{var key='intercopy-theme';var stored=localStorage.getItem(key);var pref=(stored==='light'||stored==='dark'||stored==='system')?stored:'system';var dark=window.matchMedia&&window.matchMedia('(prefers-color-scheme: dark)').matches;var theme=pref==='system'?(dark?'dark':'light'):pref;document.documentElement.setAttribute('data-theme',theme);document.documentElement.style.colorScheme=theme;}catch(e){document.documentElement.setAttribute('data-theme','light');document.documentElement.style.colorScheme='light';}})();",
        },
      ],
    },
  },
})
