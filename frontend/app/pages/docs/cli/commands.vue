<script setup lang="ts">
definePageMeta({ layout: 'docs' })

useSeoMeta({
  title: 'CLI Commands - InterCopy Docs',
  description: 'Available commands for the InterCopy CLI tool.',
})
</script>

<template>
  <div>
    <h1>Commands</h1>
    <p>
      The InterCopy CLI provides the following commands.
    </p>

    <h2>configure</h2>
    <p>
      Interactive setup wizard that creates or updates <code>.intercopyrc.json</code>.
    </p>
    <pre><code class="language-bash">intercopy configure</code></pre>

    <h3>Options</h3>
    <table>
      <thead>
        <tr>
          <th>Flag</th>
          <th>Description</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td><code>--api-url &lt;url&gt;</code></td>
          <td>Set the API base URL non-interactively</td>
        </tr>
        <tr>
          <td><code>--token &lt;token&gt;</code></td>
          <td>Set the API token non-interactively</td>
        </tr>
        <tr>
          <td><code>--project &lt;id&gt;</code></td>
          <td>Set the project ID</td>
        </tr>
        <tr>
          <td><code>--format &lt;format&gt;</code></td>
          <td>Export format: <code>i18next</code>, <code>vue-i18n</code>, <code>react-intl</code>, <code>flat</code></td>
        </tr>
        <tr>
          <td><code>--output &lt;dir&gt;</code></td>
          <td>Output directory for translation files</td>
        </tr>
      </tbody>
    </table>

    <h2>pull</h2>
    <p>
      Downloads translation bundles from InterCopy and writes them to the configured output directory.
    </p>
    <pre><code class="language-bash">intercopy pull</code></pre>

    <h3>Options</h3>
    <table>
      <thead>
        <tr>
          <th>Flag</th>
          <th>Description</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td><code>--version &lt;label&gt;</code></td>
          <td>Pull from a specific release instead of the working copy</td>
        </tr>
        <tr>
          <td><code>--languages &lt;codes&gt;</code></td>
          <td>Comma-separated list of language codes to include (default: all)</td>
        </tr>
        <tr>
          <td><code>--clean</code></td>
          <td>Remove existing files in the output directory before writing</td>
        </tr>
        <tr>
          <td><code>--dry-run</code></td>
          <td>Preview which files would be written without actually writing them</td>
        </tr>
      </tbody>
    </table>

    <h3>Example</h3>
    <pre><code class="language-bash"># Pull French and German from v2.1.0, clean output first
intercopy pull --version v2.1.0 --languages fr,de --clean</code></pre>

    <h3>CI/CD usage</h3>
    <p>
      In CI environments, pass the token via the <code>INTERCOPY_TOKEN</code> environment variable instead of
      storing it in the credentials file:
    </p>
    <pre><code class="language-bash">INTERCOPY_TOKEN=$&#123;&#123; secrets.INTERCOPY_TOKEN &#125;&#125; npx @intercopy/cli pull</code></pre>

    <div class="docs-next-page">
      <NuxtLink to="/docs/api">API Reference &rarr;</NuxtLink>
    </div>
  </div>
</template>
