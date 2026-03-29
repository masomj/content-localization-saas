<script setup lang="ts">
definePageMeta({ layout: 'docs' })

useSeoMeta({
  title: 'react-intl Integration - InterCopy Docs',
  description: 'Set up react-intl or next-intl with InterCopy translation bundles.',
})
</script>

<template>
  <div>
    <h1>react-intl / next-intl</h1>
    <p>
      <a href="https://formatjs.io/docs/react-intl/" target="_blank" rel="noopener">react-intl</a> (part of FormatJS)
      and <a href="https://next-intl-docs.vercel.app/" target="_blank" rel="noopener">next-intl</a> are popular
      choices for React and Next.js internationalisation. InterCopy exports compatible JSON for both.
    </p>

    <h2>Step 1: Export translations</h2>
    <p>
      Export in <strong>react-intl</strong> format. The output is a flat key-value JSON file per language:
    </p>
    <pre><code class="language-text">src/locales/
  en.json
  fr.json</code></pre>
    <p>Each file looks like:</p>
    <pre><code class="language-json">{
  "homepage.hero.title": "Welcome to Acme",
  "homepage.hero.subtitle": "The best product ever",
  "homepage.cta.button": "Get Started"
}</code></pre>

    <h2>react-intl setup</h2>
    <h3>Install</h3>
    <pre><code class="language-bash">npm install react-intl</code></pre>

    <h3>Configure</h3>
    <pre><code class="language-typescript">import { IntlProvider } from 'react-intl'
import messages_en from './locales/en.json'
import messages_fr from './locales/fr.json'

const messages: Record&lt;string, Record&lt;string, string&gt;&gt; = {
  en: messages_en,
  fr: messages_fr,
}

function App() {
  const locale = 'en' // or from user preference

  return (
    &lt;IntlProvider locale={locale} messages={messages[locale]}&gt;
      &lt;YourApp /&gt;
    &lt;/IntlProvider&gt;
  )
}</code></pre>

    <h3>Use in components</h3>
    <pre><code class="language-typescript">import { FormattedMessage } from 'react-intl'

function HeroSection() {
  return (
    &lt;h1&gt;
      &lt;FormattedMessage id="homepage.hero.title" /&gt;
    &lt;/h1&gt;
  )
}</code></pre>

    <hr>

    <h2>next-intl setup</h2>
    <h3>Install</h3>
    <pre><code class="language-bash">npm install next-intl</code></pre>

    <h3>Configure</h3>
    <p>Place your locale files in <code>messages/</code>:</p>
    <pre><code class="language-text">messages/
  en.json
  fr.json</code></pre>
    <p>Create <code>i18n.ts</code>:</p>
    <pre><code class="language-typescript">import { getRequestConfig } from 'next-intl/server'

export default getRequestConfig(async ({ locale }) =&gt; ({
  messages: (await import(`./messages/${locale}.json`)).default,
}))</code></pre>

    <h3>Use in components</h3>
    <pre><code class="language-typescript">import { useTranslations } from 'next-intl'

function HeroSection() {
  const t = useTranslations('homepage.hero')
  return &lt;h1&gt;{t('title')}&lt;/h1&gt;
}</code></pre>

    <div class="docs-next-page">
      <NuxtLink to="/docs">Back to Docs Home &rarr;</NuxtLink>
    </div>
  </div>
</template>
