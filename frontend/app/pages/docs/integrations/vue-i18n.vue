<script setup lang="ts">
definePageMeta({ layout: 'docs' })

useSeoMeta({
  title: 'vue-i18n Integration - InterCopy Docs',
  description: 'Set up vue-i18n with InterCopy translation bundles.',
})
</script>

<template>
  <div>
    <h1>vue-i18n</h1>
    <p>
      <a href="https://vue-i18n.intlify.dev/" target="_blank" rel="noopener">vue-i18n</a> is the standard i18n
      solution for Vue.js and Nuxt applications. InterCopy exports vue-i18n-compatible JSON natively.
    </p>

    <h2>Step 1: Export translations</h2>
    <p>
      Export your translations in <strong>vue-i18n</strong> format and place the files in your project:
    </p>
    <pre><code class="language-text">src/locales/
  en.json
  fr.json
  de.json</code></pre>

    <h2>Step 2: Install vue-i18n</h2>
    <pre><code class="language-bash">npm install vue-i18n</code></pre>
    <p>For Nuxt, use the official module:</p>
    <pre><code class="language-bash">npm install @nuxtjs/i18n</code></pre>

    <h2>Step 3: Configure (Vue 3)</h2>
    <p>Create an <code>i18n.ts</code> plugin file:</p>
    <pre><code class="language-typescript">import { createI18n } from 'vue-i18n'
import en from './locales/en.json'
import fr from './locales/fr.json'

const i18n = createI18n({
  locale: 'en',
  fallbackLocale: 'en',
  messages: { en, fr },
})

export default i18n</code></pre>
    <p>Register the plugin in your app entry:</p>
    <pre><code class="language-typescript">import { createApp } from 'vue'
import App from './App.vue'
import i18n from './i18n'

createApp(App).use(i18n).mount('#app')</code></pre>

    <h2>Step 4: Use in templates</h2>
    <pre><code class="language-html">&lt;template&gt;
  &lt;h1&gt;{{ $t('homepage.hero.title') }}&lt;/h1&gt;
  &lt;p&gt;{{ $t('homepage.hero.subtitle') }}&lt;/p&gt;
&lt;/template&gt;</code></pre>

    <h2>Nuxt configuration</h2>
    <p>
      If you are using Nuxt with <code>@nuxtjs/i18n</code>, configure it in <code>nuxt.config.ts</code>:
    </p>
    <pre><code class="language-typescript">export default defineNuxtConfig({
  modules: ['@nuxtjs/i18n'],
  i18n: {
    locales: [
      { code: 'en', file: 'en.json' },
      { code: 'fr', file: 'fr.json' },
    ],
    defaultLocale: 'en',
    langDir: 'locales/',
  },
})</code></pre>

    <div class="docs-next-page">
      <NuxtLink to="/docs/integrations/react-intl">react-intl Setup &rarr;</NuxtLink>
    </div>
  </div>
</template>
