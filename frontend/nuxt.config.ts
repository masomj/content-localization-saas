export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  devtools: { enabled: true },
  css: ['~/assets/css/tokens.css'],
  app: {
    head: {
      htmlAttrs: { lang: 'en' },
      title: 'LocFlow - Translate Your Content. Scale Globally.',
      charset: 'utf-8',
      viewport: 'width=device-width, initial-scale=1',
      meta: [
        { name: 'description', content: 'The all-in-one localization platform that helps teams manage translations, collaborate with reviewers, and deliver localized content to every market.' },
        { name: 'theme-color', content: '#4f46e5' },
        { property: 'og:title', content: 'LocFlow - Translate Your Content. Scale Globally.' },
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
          innerHTML: "(function(){try{var key='locflow-theme';var stored=localStorage.getItem(key);var pref=(stored==='light'||stored==='dark'||stored==='system')?stored:'system';var dark=window.matchMedia&&window.matchMedia('(prefers-color-scheme: dark)').matches;var theme=pref==='system'?(dark?'dark':'light'):pref;document.documentElement.setAttribute('data-theme',theme);document.documentElement.style.colorScheme=theme;}catch(e){document.documentElement.setAttribute('data-theme','light');document.documentElement.style.colorScheme='light';}})();",
        },
      ],
    },
  },
})
