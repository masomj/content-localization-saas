<script setup lang="ts">
definePageMeta({ layout: 'docs' })

useSeoMeta({
  title: 'CI/CD Integration - InterCopy Docs',
  description: 'Pull live translations from InterCopy in your CI pipeline using API tokens and project IDs.',
})
</script>

<template>
  <div>
    <h1>CI/CD Integration</h1>
    <p>
      Wire your build pipeline to InterCopy so every deploy ships with the latest translations.
      The flow is the same on any CI provider: get an <strong>API token</strong>, get the
      <strong>project ID</strong>, then call the integration export endpoint and split the
      response into per-language files before your build step.
    </p>

    <h2>What you need</h2>
    <ul>
      <li><strong>API token</strong> &mdash; created from <em>Settings &rarr; API Tokens</em>. Scope: <code>exports:read</code>.</li>
      <li><strong>Project ID</strong> &mdash; the UUID of the project you want to export.</li>
      <li><strong>Two CI secrets</strong> &mdash; we use <code>INTERCOPY_API_TOKEN</code> and <code>INTERCOPY_PROJECT_ID</code> in the examples below.</li>
    </ul>

    <h2>Step 1: Create an API token</h2>
    <ol>
      <li>Sign in as an admin and go to <em>Settings &rarr; API Tokens</em>.</li>
      <li>Click <strong>Create token</strong>, give it a name (e.g. <code>sample-app-ci</code>) and expiry.</li>
      <li>Copy the token value <strong>once</strong> &mdash; it is shown only at creation time.</li>
      <li>Store it as a secret in your CI provider (GitHub Actions, GitLab CI, etc).</li>
    </ol>

    <h2>Step 2: Find your project ID</h2>
    <p>
      Open <em>Settings</em> in the web app. Under <strong>Integrations &amp; CI</strong> there is a
      <strong>Project IDs</strong> list with a copy button next to each project. The project ID is the
      UUID required by every CI export call.
    </p>
    <p>
      Add it as a second CI secret (e.g. <code>INTERCOPY_PROJECT_ID</code>). One token can read multiple
      projects, but each pipeline only needs the ID of the project it deploys.
    </p>

    <h2>Step 3: Call the export endpoint</h2>
    <p>
      Use the authed integration endpoint. It returns one nested JSON object per language, ready to
      drop straight into vue-i18n, i18next, or any framework that loads JSON locale files.
    </p>
    <pre><code class="language-bash">curl -sS \
  -H "X-Api-Token: $INTERCOPY_API_TOKEN" \
  -H "Accept: application/json" \
  "https://api.intercopy.co.uk/api/integration/exports/locales?projectId=$INTERCOPY_PROJECT_ID&amp;version=live"</code></pre>

    <p>Response shape (abbreviated):</p>
    <pre><code class="language-json">{
  "source": { "nav": { "title": "TaskFlow" } },
  "en":     { "nav": { "title": "TaskFlow" } },
  "fr":     { "nav": { "title": "TaskFlow" } },
  "de":     { "nav": { "title": "TaskFlow" } }
}</code></pre>

    <p>
      The <code>source</code> key is always an alias for the project's source language so generic
      pipelines can grab it without knowing the BCP-47 code.
      Pass <code>?version=live</code> to serve the promoted version, <code>?version=working</code> for
      the in-progress copy, or a specific version UUID.
    </p>

    <h2>Step 4: Split into per-language files</h2>
    <pre><code class="language-bash">mkdir -p src/locales
EXPORT=/tmp/intercopy-export.json

curl -sS -o "$EXPORT" \
  -H "X-Api-Token: $INTERCOPY_API_TOKEN" \
  "https://api.intercopy.co.uk/api/integration/exports/locales?projectId=$INTERCOPY_PROJECT_ID&amp;version=live"

# Each language key in the response -> src/locales/&lt;lang&gt;.json
# Skip the duplicate "source" alias; we get the real BCP-47 code too.
for lang in $(jq -r 'keys[] | select(. != "source")' "$EXPORT"); do
  jq --arg lang "$lang" '.[$lang]' "$EXPORT" &gt; "src/locales/${lang}.json"
done</code></pre>

    <h2>Full example: GitHub Actions</h2>
    <p>
      Drop this into <code>.github/workflows/deploy.yml</code> before your build step. It falls back to
      the locale files committed in the repo when the API is unreachable, so a cold-start build still
      ships something usable.
    </p>
    <pre><code class="language-yaml">jobs:
  fetch-locales:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Fetch locales from InterCopy
        env:
          INTERCOPY_API_TOKEN: ${{ secrets.INTERCOPY_API_TOKEN }}
          INTERCOPY_PROJECT_ID: ${{ secrets.INTERCOPY_PROJECT_ID }}
          INTERCOPY_API_URL: https://api.intercopy.co.uk
        run: |
          mkdir -p src/locales

          if [ -z "$INTERCOPY_PROJECT_ID" ] || [ -z "$INTERCOPY_API_TOKEN" ]; then
            echo "Missing CI secrets - using bundled locales"
            exit 0
          fi

          STATUS=$(curl -sS -o /tmp/export.json -w "%{http_code}" \
            -H "X-Api-Token: $INTERCOPY_API_TOKEN" \
            -H "Accept: application/json" \
            "$INTERCOPY_API_URL/api/integration/exports/locales?projectId=$INTERCOPY_PROJECT_ID&amp;version=live")

          if [ "$STATUS" = "200" ] &amp;&amp; jq -e '.source' /tmp/export.json &gt; /dev/null; then
            for lang in $(jq -r 'keys[] | select(. != "source")' /tmp/export.json); do
              jq --arg lang "$lang" '.[$lang]' /tmp/export.json &gt; "src/locales/${lang}.json"
            done
          else
            echo "InterCopy returned $STATUS - keeping bundled locales"
          fi

      - uses: actions/upload-artifact@v4
        with:
          name: locales
          path: src/locales/</code></pre>

    <h2>Other CI providers</h2>
    <p>
      The same <code>curl + jq</code> snippet works on any runner with internet access. For
      GitLab CI, drop it into a <code>before_script</code>; for CircleCI, use a <code>run</code> step;
      for Vercel, run it as a build command. The only requirements are <code>curl</code>,
      <code>jq</code>, and your two secrets.
    </p>

    <h2>Operational tips</h2>
    <ul>
      <li>
        <strong>Rotate tokens regularly.</strong> The Settings &rarr; API Tokens page has rotate, extend,
        and revoke actions; revoking takes effect immediately and breaks any pipeline still using the
        old value.
      </li>
      <li>
        <strong>Pin to <code>?version=live</code></strong> in production pipelines so unfinished edits
        on the working copy never reach prod. Use <code>?version=working</code> only for staging or
        preview builds.
      </li>
      <li>
        <strong>Cache the response</strong> if you build the same commit many times &mdash; the export
        is idempotent for a given version.
      </li>
      <li>
        <strong>Always provide a fallback.</strong> Commit the last known good locale files alongside
        the workflow so deploys don't go dark if the API is unavailable.
      </li>
    </ul>

    <div class="docs-next-page">
      <NuxtLink to="/docs/cli">Try the CLI &rarr;</NuxtLink>
    </div>
  </div>
</template>
