<script setup lang="ts">
definePageMeta({ layout: 'docs' })

useSeoMeta({
  title: 'Export - InterCopy Docs',
  description: 'Export translation bundles in various formats for your framework of choice.',
})
</script>

<template>
  <div>
    <h1>Export</h1>
    <p>
      InterCopy can export your translations in several industry-standard formats so they integrate directly with
      your framework's i18n library.
    </p>

    <h2>Supported formats</h2>
    <table>
      <thead>
        <tr>
          <th>Format</th>
          <th>File extension</th>
          <th>Typical use</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td><strong>i18next JSON</strong></td>
          <td><code>.json</code></td>
          <td>React, Next.js, Node.js apps using i18next</td>
        </tr>
        <tr>
          <td><strong>vue-i18n</strong></td>
          <td><code>.json</code></td>
          <td>Vue / Nuxt apps using vue-i18n</td>
        </tr>
        <tr>
          <td><strong>react-intl</strong></td>
          <td><code>.json</code></td>
          <td>React apps using react-intl or FormatJS</td>
        </tr>
        <tr>
          <td><strong>Flat JSON</strong></td>
          <td><code>.json</code></td>
          <td>Simple key-value pairs for any framework</td>
        </tr>
      </tbody>
    </table>

    <h2>Exporting from the dashboard</h2>
    <ol>
      <li>Navigate to the <strong>Export</strong> section (or open the export panel from any content page).</li>
      <li>Select the <strong>version</strong> to export (working copy or a specific release).</li>
      <li>Choose the <strong>format</strong> from the dropdown.</li>
      <li>Select the <strong>languages</strong> to include.</li>
      <li>Click <strong>Download</strong> to get a ZIP archive with one file per language.</li>
    </ol>

    <h2>Example output</h2>
    <p>For an i18next export with English and French, you would receive:</p>
    <pre><code class="language-text">export/
  en/
    translation.json
  fr/
    translation.json</code></pre>

    <p>Each file contains nested JSON matching your folder structure:</p>
    <pre><code class="language-json">{
  "homepage": {
    "hero": {
      "title": "Welcome to Acme",
      "subtitle": "The best product ever"
    },
    "cta": {
      "button": "Get Started"
    }
  }
}</code></pre>

    <h2>Programmatic export</h2>
    <p>
      You can also export via the <NuxtLink to="/docs/api/endpoints">REST API</NuxtLink> or the
      <NuxtLink to="/docs/cli">CLI tool</NuxtLink> for CI/CD integration.
    </p>

    <div class="docs-next-page">
      <NuxtLink to="/docs/webapp/admin">Admin &rarr;</NuxtLink>
    </div>
  </div>
</template>
