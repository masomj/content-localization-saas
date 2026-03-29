<script setup lang="ts">
definePageMeta({ layout: 'docs' })

useSeoMeta({
  title: 'Versions - InterCopy Docs',
  description: 'Project versioning: releases, snapshots, and live vs. working copies.',
})
</script>

<template>
  <div>
    <h1>Versions</h1>
    <p>
      InterCopy supports <strong>project versioning</strong> so you can tie your translations to specific app
      releases and maintain multiple versions of content simultaneously.
    </p>

    <h2>Working copy vs. releases</h2>
    <p>
      Every project has a <strong>working copy</strong> &mdash; the live, editable version of your content. When
      you are ready to ship, you create a <strong>release</strong> that snapshots the working copy at that point
      in time.
    </p>
    <ul>
      <li><strong>Working copy</strong> &mdash; Always editable. This is where day-to-day translation work happens.</li>
      <li><strong>Release</strong> &mdash; Immutable snapshot. Once created, a release cannot be modified.</li>
    </ul>

    <h2>Creating a release</h2>
    <p>
      Navigate to <strong>Projects &gt; [Your Project] &gt; Versions</strong> and click <strong>New Release</strong>.
      Enter a version label (e.g. <code>v2.1.0</code>) and an optional description. InterCopy will snapshot all
      content keys and translations as they currently exist.
    </p>

    <h2>Comparing versions</h2>
    <p>
      Select two versions to see a diff of what changed: added keys, removed keys, and modified translations. This
      is useful for release notes and QA checks.
    </p>

    <h2>Exporting a specific version</h2>
    <p>
      When you <NuxtLink to="/docs/webapp/export">export translations</NuxtLink>, you can choose which version to
      export from. This ensures your app always ships with the exact translations that were approved for that release.
    </p>

    <div class="docs-next-page">
      <NuxtLink to="/docs/webapp/components">Components &rarr;</NuxtLink>
    </div>
  </div>
</template>
