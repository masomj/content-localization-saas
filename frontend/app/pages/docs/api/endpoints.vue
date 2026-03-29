<script setup lang="ts">
definePageMeta({ layout: 'docs' })

useSeoMeta({
  title: 'API Endpoints - InterCopy Docs',
  description: 'Key REST API endpoints for InterCopy.',
})
</script>

<template>
  <div>
    <h1>Endpoints</h1>
    <p>
      Below is a summary of the key API endpoints. All paths are relative to the base URL
      (<code>https://intercopy.co.uk/api</code>).
    </p>

    <h2>Projects</h2>
    <table>
      <thead>
        <tr>
          <th>Method</th>
          <th>Path</th>
          <th>Description</th>
        </tr>
      </thead>
      <tbody>
        <tr><td><code>GET</code></td><td><code>/projects</code></td><td>List all projects</td></tr>
        <tr><td><code>POST</code></td><td><code>/projects</code></td><td>Create a new project</td></tr>
        <tr><td><code>GET</code></td><td><code>/projects/:id</code></td><td>Get project details</td></tr>
        <tr><td><code>PUT</code></td><td><code>/projects/:id</code></td><td>Update project</td></tr>
        <tr><td><code>DELETE</code></td><td><code>/projects/:id</code></td><td>Delete project</td></tr>
      </tbody>
    </table>

    <h2>Content</h2>
    <table>
      <thead>
        <tr>
          <th>Method</th>
          <th>Path</th>
          <th>Description</th>
        </tr>
      </thead>
      <tbody>
        <tr><td><code>GET</code></td><td><code>/projects/:id/collections</code></td><td>List folders (collections)</td></tr>
        <tr><td><code>POST</code></td><td><code>/projects/:id/collections</code></td><td>Create a folder</td></tr>
        <tr><td><code>GET</code></td><td><code>/projects/:id/content-items</code></td><td>List content keys</td></tr>
        <tr><td><code>POST</code></td><td><code>/projects/:id/content-items</code></td><td>Create a content key</td></tr>
        <tr><td><code>PUT</code></td><td><code>/content-items/:id</code></td><td>Update a content key</td></tr>
        <tr><td><code>DELETE</code></td><td><code>/content-items/:id</code></td><td>Delete a content key</td></tr>
      </tbody>
    </table>

    <h2>Languages</h2>
    <table>
      <thead>
        <tr>
          <th>Method</th>
          <th>Path</th>
          <th>Description</th>
        </tr>
      </thead>
      <tbody>
        <tr><td><code>GET</code></td><td><code>/projects/:id/languages</code></td><td>List project languages</td></tr>
        <tr><td><code>POST</code></td><td><code>/projects/:id/languages</code></td><td>Add a language</td></tr>
        <tr><td><code>DELETE</code></td><td><code>/projects/:id/languages/:code</code></td><td>Remove a language</td></tr>
      </tbody>
    </table>

    <h2>Translations</h2>
    <table>
      <thead>
        <tr>
          <th>Method</th>
          <th>Path</th>
          <th>Description</th>
        </tr>
      </thead>
      <tbody>
        <tr><td><code>GET</code></td><td><code>/content-items/:id/translations</code></td><td>List translations for a key</td></tr>
        <tr><td><code>PUT</code></td><td><code>/content-items/:id/translations/:lang</code></td><td>Set translation for a language</td></tr>
      </tbody>
    </table>

    <h2>Export</h2>
    <table>
      <thead>
        <tr>
          <th>Method</th>
          <th>Path</th>
          <th>Description</th>
        </tr>
      </thead>
      <tbody>
        <tr><td><code>GET</code></td><td><code>/projects/:id/export</code></td><td>Export translations as a ZIP bundle</td></tr>
      </tbody>
    </table>
    <p>
      Query parameters for the export endpoint:
    </p>
    <ul>
      <li><code>format</code> &mdash; <code>i18next</code>, <code>vue-i18n</code>, <code>react-intl</code>, or <code>flat</code></li>
      <li><code>version</code> &mdash; Release label (omit for working copy)</li>
      <li><code>languages</code> &mdash; Comma-separated language codes (omit for all)</li>
    </ul>

    <h3>Example</h3>
    <pre><code class="language-bash">curl -H "Authorization: Bearer YOUR_TOKEN" \
  "https://intercopy.co.uk/api/projects/abc123/export?format=i18next&languages=en,fr" \
  -o translations.zip</code></pre>

    <h2>Components</h2>
    <table>
      <thead>
        <tr>
          <th>Method</th>
          <th>Path</th>
          <th>Description</th>
        </tr>
      </thead>
      <tbody>
        <tr><td><code>GET</code></td><td><code>/projects/:id/components</code></td><td>List design components</td></tr>
        <tr><td><code>GET</code></td><td><code>/components/:id</code></td><td>Get component details</td></tr>
        <tr><td><code>POST</code></td><td><code>/projects/:id/components</code></td><td>Create/update a component (used by Figma plugin)</td></tr>
      </tbody>
    </table>

    <h2>Versions</h2>
    <table>
      <thead>
        <tr>
          <th>Method</th>
          <th>Path</th>
          <th>Description</th>
        </tr>
      </thead>
      <tbody>
        <tr><td><code>GET</code></td><td><code>/projects/:id/versions</code></td><td>List releases</td></tr>
        <tr><td><code>POST</code></td><td><code>/projects/:id/versions</code></td><td>Create a release snapshot</td></tr>
      </tbody>
    </table>

    <div class="docs-next-page">
      <NuxtLink to="/docs/integrations">Integrations &rarr;</NuxtLink>
    </div>
  </div>
</template>
