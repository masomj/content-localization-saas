<script setup lang="ts">
definePageMeta({ layout: 'docs' })

useSeoMeta({
  title: 'API Reference - InterCopy Docs',
  description: 'REST API overview and authentication for InterCopy.',
})
</script>

<template>
  <div>
    <h1>API Reference</h1>
    <p>
      The InterCopy REST API lets you manage content, languages, translations, and exports programmatically. All
      endpoints are available at <code>/api</code> under your InterCopy domain.
    </p>

    <h2>Base URL</h2>
    <pre><code class="language-text">https://intercopy.co.uk/api</code></pre>

    <h2>Authentication</h2>
    <p>
      The API uses <strong>Bearer token</strong> authentication. Include your token in the <code>Authorization</code>
      header of every request:
    </p>
    <pre><code class="language-bash">curl -H "Authorization: Bearer YOUR_API_TOKEN" \
  https://intercopy.co.uk/api/projects</code></pre>

    <h3>Obtaining a token</h3>
    <p>
      Generate a personal access token from the web app:
    </p>
    <ol>
      <li>Navigate to <strong>Settings</strong> in the sidebar.</li>
      <li>Scroll to <strong>API Tokens</strong>.</li>
      <li>Click <strong>Generate Token</strong>, give it a name, and copy the value.</li>
    </ol>
    <blockquote>
      <p>
        Store tokens securely. They grant the same permissions as your user account. You can revoke a token at any
        time from the Settings page.
      </p>
    </blockquote>

    <h2>Response format</h2>
    <p>
      All responses are JSON. Successful responses return the resource or an array of resources. Errors follow this
      shape:
    </p>
    <pre><code class="language-json">{
  "error": {
    "code": "NOT_FOUND",
    "message": "Project not found"
  }
}</code></pre>

    <h2>Common status codes</h2>
    <table>
      <thead>
        <tr>
          <th>Code</th>
          <th>Meaning</th>
        </tr>
      </thead>
      <tbody>
        <tr><td><code>200</code></td><td>Success</td></tr>
        <tr><td><code>201</code></td><td>Created</td></tr>
        <tr><td><code>400</code></td><td>Bad request (validation error)</td></tr>
        <tr><td><code>401</code></td><td>Unauthorised (missing or invalid token)</td></tr>
        <tr><td><code>403</code></td><td>Forbidden (insufficient permissions)</td></tr>
        <tr><td><code>404</code></td><td>Not found</td></tr>
        <tr><td><code>500</code></td><td>Internal server error</td></tr>
      </tbody>
    </table>

    <div class="docs-next-page">
      <NuxtLink to="/docs/api/endpoints">Endpoints &rarr;</NuxtLink>
    </div>
  </div>
</template>
