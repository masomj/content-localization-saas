<script setup lang="ts">
definePageMeta({ layout: 'docs' })

useSeoMeta({
  title: 'Push / Pull Sync - InterCopy Docs',
  description: 'Push Figma frames to InterCopy and pull translated text back into designs.',
})
</script>

<template>
  <div>
    <h1>Push / Pull Sync</h1>
    <p>
      The core workflow of the Figma plugin is a two-way sync: <strong>push</strong> text from Figma into InterCopy,
      and <strong>pull</strong> translations back into your designs.
    </p>

    <h2>Pushing frames</h2>
    <ol>
      <li>Select one or more frames in Figma.</li>
      <li>Open the InterCopy plugin and click <strong>Push</strong>.</li>
      <li>Choose the target project and folder in InterCopy.</li>
      <li>Review the extracted text layers and click <strong>Confirm</strong>.</li>
    </ol>
    <p>
      InterCopy will create a <NuxtLink to="/docs/webapp/components">component</NuxtLink> for each frame, along
      with content keys for every text layer. A visual snapshot of the frame is uploaded for translator context.
    </p>

    <h3>What gets extracted</h3>
    <ul>
      <li>All visible text layers within the selected frames</li>
      <li>Layer names are used as key identifiers (you can rename before confirming)</li>
      <li>Nested frames and auto-layout groups are flattened</li>
    </ul>

    <h2>Pulling translations</h2>
    <ol>
      <li>Select the frames you previously pushed.</li>
      <li>Open the plugin and click <strong>Pull</strong>.</li>
      <li>Choose the target language (e.g. French).</li>
      <li>Click <strong>Apply</strong>.</li>
    </ol>
    <p>
      The plugin replaces each text layer's content with the corresponding translation. The original source text
      is preserved in InterCopy &mdash; only the Figma layer content changes.
    </p>

    <h2>Conflict resolution</h2>
    <p>
      If a text layer has been modified in both Figma and InterCopy since the last sync, the plugin will flag the
      conflict and let you choose which version to keep.
    </p>

    <h2>Best practices</h2>
    <ul>
      <li>Push frames after finalising the layout, not during active design iteration.</li>
      <li>Use descriptive layer names &mdash; they become your content key identifiers.</li>
      <li>Create a dedicated Figma page for "translation preview" where you pull translations to verify layout.</li>
    </ul>

    <div class="docs-next-page">
      <NuxtLink to="/docs/cli">CLI &rarr;</NuxtLink>
    </div>
  </div>
</template>
