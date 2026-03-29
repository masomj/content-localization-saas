<script setup lang="ts">
definePageMeta({ layout: 'docs' })

useSeoMeta({
  title: 'Core Concepts - InterCopy Docs',
  description: 'Understand projects, folders, content keys, languages, translations, versions, and components.',
})
</script>

<template>
  <div>
    <h1>Core Concepts</h1>
    <p>
      Before diving deeper, it helps to understand the data model that powers InterCopy.
    </p>

    <h2>Projects</h2>
    <p>
      A <strong>project</strong> is the top-level container for all your localized content. Most teams create one
      project per product (e.g. "Marketing Site", "Mobile App"). Each project has its own set of languages, content
      keys, and version history.
    </p>

    <h2>Folders</h2>
    <p>
      Folders let you organise content keys into a tree structure, similar to a filesystem. Common patterns include
      grouping by page (<code>homepage/</code>, <code>settings/</code>) or by feature (<code>auth/</code>,
      <code>billing/</code>). Folders can be nested to any depth.
    </p>

    <h2>Content keys</h2>
    <p>
      A <strong>content key</strong> (sometimes called a "string" or "message") is a single piece of text that will
      be translated. Each key has:
    </p>
    <ul>
      <li>A unique identifier within its folder (e.g. <code>hero.title</code>)</li>
      <li>Source text in the project's source language</li>
      <li>Zero or more translations in target languages</li>
    </ul>

    <h2>Languages</h2>
    <p>
      Every project has one <strong>source language</strong> (the language you author content in) and zero or more
      <strong>target languages</strong>. Languages are identified by BCP 47 codes such as <code>en</code>,
      <code>fr</code>, <code>de</code>, or <code>zh-Hans</code>.
    </p>

    <h2>Translations</h2>
    <p>
      A <strong>translation</strong> is the localized version of a content key for a given language. Translations have
      a status workflow:
    </p>
    <ul>
      <li><strong>Draft</strong> &mdash; Initial entry, not yet reviewed.</li>
      <li><strong>Review</strong> &mdash; Submitted for review by a translator or manager.</li>
      <li><strong>Approved</strong> &mdash; Passed review and ready for export.</li>
      <li><strong>Rejected</strong> &mdash; Review flagged issues; needs revision.</li>
    </ul>

    <h2>Versions</h2>
    <p>
      InterCopy supports <strong>project versioning</strong> so you can snapshot your content at a point in time. Key concepts:
    </p>
    <ul>
      <li><strong>Working copy</strong> &mdash; The live, editable state of your content.</li>
      <li><strong>Release / Snapshot</strong> &mdash; An immutable copy of all content at a specific moment. Useful for tying translations to app releases.</li>
    </ul>

    <h2>Components</h2>
    <p>
      <strong>Design components</strong> bridge the gap between design tools and the translation pipeline. When you
      use the <NuxtLink to="/docs/figma-plugin">Figma Plugin</NuxtLink>, each Figma frame is mapped to a component
      in InterCopy. Components store a visual preview alongside the extracted text layers, so translators can see
      exactly where each string appears in the UI.
    </p>

    <div class="docs-next-page">
      <NuxtLink to="/docs/webapp">Web App Overview &rarr;</NuxtLink>
    </div>
  </div>
</template>
