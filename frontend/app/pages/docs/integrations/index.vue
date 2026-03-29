<script setup lang="ts">
definePageMeta({ layout: 'docs' })

useSeoMeta({
  title: 'Integrations - InterCopy Docs',
  description: 'Framework integration guides for i18next, vue-i18n, react-intl, and more.',
})
</script>

<template>
  <div>
    <h1>Integrations</h1>
    <p>
      InterCopy exports translation bundles in formats that plug directly into popular i18n frameworks. Choose
      your stack below for a step-by-step setup guide.
    </p>

    <h2>Available guides</h2>
    <ul>
      <li>
        <NuxtLink to="/docs/integrations/i18next"><strong>i18next</strong></NuxtLink> &mdash;
        The most popular JavaScript i18n framework. Works with React, Next.js, Node.js, and more.
      </li>
      <li>
        <NuxtLink to="/docs/integrations/vue-i18n"><strong>vue-i18n</strong></NuxtLink> &mdash;
        The standard i18n solution for Vue.js and Nuxt applications.
      </li>
      <li>
        <NuxtLink to="/docs/integrations/react-intl"><strong>react-intl / next-intl</strong></NuxtLink> &mdash;
        FormatJS-based i18n for React and Next.js.
      </li>
    </ul>

    <h2>General workflow</h2>
    <p>
      Regardless of which framework you use, the integration workflow is the same:
    </p>
    <ol>
      <li>Manage your content in InterCopy (web app, Figma plugin, or API).</li>
      <li>Export or pull translations in your framework's expected format.</li>
      <li>Load the translation files in your app using the framework's loader.</li>
    </ol>
    <p>
      You can automate step 2 with the <NuxtLink to="/docs/cli">CLI tool</NuxtLink> in your build pipeline.
    </p>

    <div class="docs-next-page">
      <NuxtLink to="/docs/integrations/i18next">i18next Setup &rarr;</NuxtLink>
    </div>
  </div>
</template>
