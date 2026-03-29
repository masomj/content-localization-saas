<script setup lang="ts">
definePageMeta({ layout: 'docs' })

useSeoMeta({
  title: 'i18next Integration - InterCopy Docs',
  description: 'Set up i18next with InterCopy translation bundles.',
})
</script>

<template>
  <div>
    <h1>i18next</h1>
    <p>
      <a href="https://www.i18next.com/" target="_blank" rel="noopener">i18next</a> is the most widely used
      JavaScript internationalization framework. InterCopy exports i18next-compatible JSON out of the box.
    </p>

    <h2>Step 1: Export translations</h2>
    <p>
      Use the <NuxtLink to="/docs/cli">CLI</NuxtLink> or the <NuxtLink to="/docs/webapp/export">web app</NuxtLink>
      to export your translations in <strong>i18next</strong> format. Place the files in your project:
    </p>
    <pre><code class="language-text">src/locales/
  en/
    translation.json
  fr/
    translation.json
  de/
    translation.json</code></pre>

    <h2>Step 2: Install i18next</h2>
    <pre><code class="language-bash">npm install i18next</code></pre>
    <p>For React, also install the React binding:</p>
    <pre><code class="language-bash">npm install react-i18next</code></pre>

    <h2>Step 3: Initialise</h2>
    <p>Create an <code>i18n.ts</code> file:</p>
    <pre><code class="language-typescript">import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'

import en from './locales/en/translation.json'
import fr from './locales/fr/translation.json'

i18n
  .use(initReactI18next) // omit if not using React
  .init({
    resources: {
      en: { translation: en },
      fr: { translation: fr },
    },
    lng: 'en',
    fallbackLng: 'en',
    interpolation: {
      escapeValue: false,
    },
  })

export default i18n</code></pre>

    <h2>Step 4: Use in components</h2>
    <pre><code class="language-typescript">import { useTranslation } from 'react-i18next'

function HeroSection() {
  const { t } = useTranslation()
  return &lt;h1&gt;{t('homepage.hero.title')}&lt;/h1&gt;
}</code></pre>

    <h2>Loading translations dynamically</h2>
    <p>
      For large apps, you can use <code>i18next-http-backend</code> to load translations on demand instead of
      bundling them:
    </p>
    <pre><code class="language-bash">npm install i18next-http-backend</code></pre>
    <pre><code class="language-typescript">import Backend from 'i18next-http-backend'

i18n
  .use(Backend)
  .use(initReactI18next)
  .init({
    backend: {
      loadPath: '/locales/{{lng}}/{{ns}}.json',
    },
    lng: 'en',
    fallbackLng: 'en',
  })</code></pre>

    <div class="docs-next-page">
      <NuxtLink to="/docs/integrations/vue-i18n">vue-i18n Setup &rarr;</NuxtLink>
    </div>
  </div>
</template>
