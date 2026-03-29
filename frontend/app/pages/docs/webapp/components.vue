<script setup lang="ts">
definePageMeta({ layout: 'docs' })

useSeoMeta({
  title: 'Components - InterCopy Docs',
  description: 'Design components: Figma frames, visual canvas, and text field editing.',
})
</script>

<template>
  <div>
    <h1>Components</h1>
    <p>
      <strong>Design components</strong> bridge the gap between your design tool (Figma) and the translation
      pipeline. They provide visual context so translators can see exactly where each string appears in the UI.
    </p>

    <h2>How components work</h2>
    <p>
      When you push frames from the <NuxtLink to="/docs/figma-plugin">Figma Plugin</NuxtLink>, each frame becomes
      a component in InterCopy. A component contains:
    </p>
    <ul>
      <li>A <strong>visual preview</strong> (rendered image of the Figma frame)</li>
      <li>A list of <strong>text fields</strong> extracted from the frame's text layers</li>
      <li>Positional data linking each text field to its location on the canvas</li>
    </ul>

    <h2>The visual canvas</h2>
    <p>
      Navigate to <strong>Components</strong> in the sidebar to see all synced frames. Click a component to open
      the visual canvas view. The frame preview is displayed with interactive overlays on each text region.
    </p>
    <p>
      Click an overlay to:
    </p>
    <ul>
      <li>View the current source text and translations</li>
      <li>Edit translations in-context</li>
      <li>See the content key this text field is linked to</li>
    </ul>

    <h2>Text field editing</h2>
    <p>
      Each text field on a component is linked to a content key. You can edit translations directly from the canvas
      view or jump to the full <NuxtLink to="/docs/webapp/translations">translation grid</NuxtLink> for more options.
    </p>

    <h2>Syncing updates</h2>
    <p>
      When a designer updates the Figma file and pushes again, InterCopy detects what changed: new text layers, removed
      layers, or modified text. The component view shows a diff so you can review changes before accepting them.
    </p>

    <div class="docs-next-page">
      <NuxtLink to="/docs/webapp/export">Export &rarr;</NuxtLink>
    </div>
  </div>
</template>
