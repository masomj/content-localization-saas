<script setup lang="ts">
definePageMeta({ layout: 'docs' })

useSeoMeta({
  title: 'CLI - InterCopy Docs',
  description: 'Overview and installation of the InterCopy CLI tool.',
})
</script>

<template>
  <div>
    <h1>CLI</h1>
    <p>
      The InterCopy CLI lets you pull translation bundles directly into your codebase from the command line. It is
      ideal for CI/CD pipelines and local development workflows.
    </p>

    <h2>Installation</h2>
    <p>Install globally via npm:</p>
    <pre><code class="language-bash">npm install -g @intercopy/cli</code></pre>
    <p>Or use npx without installing:</p>
    <pre><code class="language-bash">npx @intercopy/cli pull</code></pre>

    <h2>Configuration</h2>
    <p>
      Run the <code>configure</code> command to set up your project:
    </p>
    <pre><code class="language-bash">intercopy configure</code></pre>
    <p>
      This will prompt you for:
    </p>
    <ul>
      <li><strong>API base URL</strong> &mdash; The InterCopy server (default: <code>https://intercopy.co.uk/api</code>)</li>
      <li><strong>API token</strong> &mdash; A personal access token generated in <strong>Settings</strong> on the web app</li>
      <li><strong>Project ID</strong> &mdash; The project to pull from</li>
      <li><strong>Output directory</strong> &mdash; Where translation files are written (e.g. <code>./src/locales</code>)</li>
      <li><strong>Format</strong> &mdash; Export format (<code>i18next</code>, <code>vue-i18n</code>, <code>react-intl</code>, <code>flat</code>)</li>
    </ul>
    <p>
      Settings are saved to <code>.intercopyrc.json</code> in your project root. Commit this file (but not the API
      token, which is stored in <code>~/.intercopy/credentials</code>).
    </p>

    <h2>Quick reference</h2>
    <p>
      See <NuxtLink to="/docs/cli/commands">Commands</NuxtLink> for a full reference of all available commands.
    </p>

    <div class="docs-next-page">
      <NuxtLink to="/docs/cli/commands">Commands &rarr;</NuxtLink>
    </div>
  </div>
</template>
