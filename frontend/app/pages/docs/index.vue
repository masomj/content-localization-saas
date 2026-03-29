<script setup lang="ts">
definePageMeta({ layout: 'docs' })

useSeoMeta({
  title: 'InterCopy Documentation',
  description: 'Learn how to use InterCopy to manage translations, collaborate with reviewers, and deliver localized content.',
})

const sections = [
  {
    title: 'Getting Started',
    description: 'Set up your account, create your first project, and learn the core concepts behind InterCopy.',
    to: '/docs/getting-started',
    icon: '&#9889;',
  },
  {
    title: 'Web App',
    description: 'Manage content, translate strings, review changes, and export localized bundles from the dashboard.',
    to: '/docs/webapp',
    icon: '&#9881;',
  },
  {
    title: 'Figma Plugin',
    description: 'Push and pull text layers between Figma and InterCopy for seamless design-to-dev localization.',
    to: '/docs/figma-plugin',
    icon: '&#9998;',
  },
  {
    title: 'CLI',
    description: 'Pull translation bundles into your codebase with a single command.',
    to: '/docs/cli',
    icon: '&#62;_',
  },
  {
    title: 'API Reference',
    description: 'Authenticate, manage content, and export translations programmatically via REST.',
    to: '/docs/api',
    icon: '&#123;&#125;',
  },
  {
    title: 'Integrations',
    description: 'Drop-in guides for i18next, vue-i18n, react-intl, and more.',
    to: '/docs/integrations',
    icon: '&#128268;',
  },
]
</script>

<template>
  <div>
    <div class="docs-hero">
      <h1>InterCopy Documentation</h1>
      <p class="docs-hero__subtitle">
        Everything you need to manage multilingual content, collaborate on translations, and ship localized apps.
      </p>
    </div>

    <div class="docs-cards">
      <NuxtLink
        v-for="section in sections"
        :key="section.to"
        :to="section.to"
        class="docs-card"
      >
        <span class="docs-card__icon" v-html="section.icon" />
        <h2 class="docs-card__title">{{ section.title }}</h2>
        <p class="docs-card__description">{{ section.description }}</p>
      </NuxtLink>
    </div>

    <hr>

    <h2>What is InterCopy?</h2>
    <p>
      InterCopy is an all-in-one localization platform for software teams. It provides a central hub where developers,
      translators, and designers collaborate to manage multilingual content across web apps, mobile apps, and design tools.
    </p>

    <h3>Key capabilities</h3>
    <ul>
      <li><strong>Content management</strong> &mdash; Organize translation keys in folders, edit source text, and keep everything in sync.</li>
      <li><strong>Translation workflow</strong> &mdash; Add languages, translate strings in a grid editor, and track completion status.</li>
      <li><strong>PR-style review</strong> &mdash; Submit translation batches for review, leave comments, approve or request changes.</li>
      <li><strong>Project versioning</strong> &mdash; Create releases, snapshot content, and manage live vs. working copies.</li>
      <li><strong>Design integration</strong> &mdash; Sync text layers from Figma directly into your translation pipeline.</li>
      <li><strong>Developer export</strong> &mdash; Export bundles for i18next, vue-i18n, react-intl, and flat JSON.</li>
    </ul>

    <div class="docs-next-page">
      <NuxtLink to="/docs/getting-started">Get started &rarr;</NuxtLink>
    </div>
  </div>
</template>

<style scoped>
.docs-hero {
  margin-bottom: var(--spacing-10);
}

.docs-hero h1 {
  font-size: var(--font-size-4xl);
  margin-bottom: var(--spacing-3);
}

.docs-hero__subtitle {
  font-size: var(--font-size-lg);
  color: var(--color-text-secondary);
  max-width: 600px;
  line-height: var(--line-height-relaxed);
}

.docs-cards {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  gap: var(--spacing-4);
  margin-bottom: var(--spacing-10);
}

.docs-card {
  display: flex;
  flex-direction: column;
  padding: var(--spacing-5);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-xl);
  text-decoration: none;
  transition: all var(--transition-fast);
  background: var(--color-surface);
}

.docs-card:hover {
  border-color: var(--color-primary-400);
  box-shadow: var(--shadow-md);
}

.docs-card__icon {
  font-size: var(--font-size-2xl);
  margin-bottom: var(--spacing-2);
}

.docs-card__title {
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  margin: 0 0 var(--spacing-2);
}

.docs-card__description {
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
  margin: 0;
  line-height: var(--line-height-relaxed);
  flex: 1;
}
</style>
