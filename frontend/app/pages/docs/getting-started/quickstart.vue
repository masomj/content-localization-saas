<script setup lang="ts">
definePageMeta({ layout: 'docs' })

useSeoMeta({
  title: 'Quick Start - InterCopy Docs',
  description: 'Set up your first InterCopy project in five minutes.',
})
</script>

<template>
  <div>
    <h1>Quick Start</h1>
    <p>
      Follow these five steps to go from a fresh account to your first translated content key.
    </p>

    <h2>1. Create an account</h2>
    <p>
      Visit <NuxtLink to="/register">intercopy.co.uk/register</NuxtLink> and sign up with your email or SSO provider.
      After confirming your email you will land on the onboarding screen.
    </p>

    <h2>2. Create an organisation</h2>
    <p>
      Every InterCopy workspace lives inside an <strong>organisation</strong>. Enter a name for your team or company
      and click <strong>Create Organisation</strong>. You can invite team members later.
    </p>

    <h2>3. Create a project</h2>
    <p>
      Navigate to <strong>Projects</strong> in the sidebar and click <strong>New Project</strong>. Give it a name
      (e.g. "Web App") and choose your source language (the language you write content in first).
    </p>

    <h2>4. Add content keys</h2>
    <p>
      Open the <strong>Content</strong> page. You will see a file-explorer-style tree on the left. Create a folder
      (e.g. <code>homepage</code>) and then add keys inside it:
    </p>
    <pre><code class="language-text">homepage/
  hero.title     → "Welcome to Acme"
  hero.subtitle  → "The best product ever"
  cta.button     → "Get Started"</code></pre>
    <p>
      Type the source text for each key in the editor panel on the right.
    </p>

    <h2>5. Add a language and translate</h2>
    <p>
      Go to <strong>Content &gt; Languages</strong> and add a target language (e.g. <code>fr</code> for French).
      Switch back to the content grid and you will see a new column. Click a cell to enter the translation.
    </p>

    <blockquote>
      <p>
        That's it! You now have a project with source content and at least one translation. From here you can
        <NuxtLink to="/docs/webapp/export">export bundles</NuxtLink>, set up the
        <NuxtLink to="/docs/figma-plugin">Figma plugin</NuxtLink>, or
        <NuxtLink to="/docs/cli">pull translations via the CLI</NuxtLink>.
      </p>
    </blockquote>

    <div class="docs-next-page">
      <NuxtLink to="/docs/getting-started/concepts">Core Concepts &rarr;</NuxtLink>
    </div>
  </div>
</template>
