<script setup lang="ts">
definePageMeta({ layout: 'docs' })

useSeoMeta({
  title: 'Getting Started - InterCopy Docs',
  description: 'Get up and running with InterCopy in minutes.',
})
</script>

<template>
  <div>
    <h1>Getting Started</h1>
    <p>
      Welcome to InterCopy! This section will help you go from zero to managing multilingual content in just a few minutes.
    </p>

    <h2>Where to begin</h2>
    <p>
      If you are brand new to InterCopy, we recommend following the guides in order:
    </p>

    <ol>
      <li>
        <NuxtLink to="/docs/getting-started/quickstart"><strong>Quick Start</strong></NuxtLink> &mdash;
        Create an account, set up your first project, and add a translation in under five minutes.
      </li>
      <li>
        <NuxtLink to="/docs/getting-started/concepts"><strong>Core Concepts</strong></NuxtLink> &mdash;
        Understand the building blocks: projects, folders, content keys, languages, translations, versions, and components.
      </li>
    </ol>

    <h2>Already set up?</h2>
    <p>
      Jump straight to the section you need:
    </p>
    <ul>
      <li><NuxtLink to="/docs/webapp">Web App</NuxtLink> &mdash; Full reference for the dashboard.</li>
      <li><NuxtLink to="/docs/figma-plugin">Figma Plugin</NuxtLink> &mdash; Sync design text layers with InterCopy.</li>
      <li><NuxtLink to="/docs/cli">CLI</NuxtLink> &mdash; Pull translations into your codebase.</li>
      <li><NuxtLink to="/docs/api">API</NuxtLink> &mdash; Integrate programmatically.</li>
      <li><NuxtLink to="/docs/integrations">Integrations</NuxtLink> &mdash; Framework-specific setup guides.</li>
    </ul>

    <div class="docs-next-page">
      <NuxtLink to="/docs/getting-started/quickstart">Quick Start &rarr;</NuxtLink>
    </div>
  </div>
</template>
