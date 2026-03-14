export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  devtools: { enabled: true },
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
    },
  },
})
