<script setup lang="ts">
definePageMeta({ layout: 'docs' })

useSeoMeta({
  title: 'Figma Plugin - InterCopy Docs',
  description: 'Overview of the InterCopy Figma plugin for design-to-dev localization.',
})
</script>

<template>
  <div>
    <h1>Figma Plugin</h1>
    <p>
      The InterCopy Figma plugin lets designers push text layers from Figma frames directly into InterCopy, and pull
      translated text back into their designs. This creates a seamless bridge between design and localization.
    </p>

    <h2>What it does</h2>
    <ul>
      <li><strong>Push frames</strong> &mdash; Extract text layers from selected Figma frames and create corresponding content keys in InterCopy.</li>
      <li><strong>Pull translations</strong> &mdash; Replace text in Figma with translated strings from InterCopy.</li>
      <li><strong>Visual preview</strong> &mdash; Upload a rendered snapshot of each frame so translators see context.</li>
    </ul>

    <h2>Getting started</h2>
    <ol>
      <li><NuxtLink to="/docs/figma-plugin/install">Install the plugin</NuxtLink> from the Figma Community.</li>
      <li><NuxtLink to="/docs/figma-plugin/auth">Authenticate</NuxtLink> using the device auth flow.</li>
      <li><NuxtLink to="/docs/figma-plugin/sync">Push and pull</NuxtLink> frames to keep designs and translations in sync.</li>
    </ol>

    <div class="docs-next-page">
      <NuxtLink to="/docs/figma-plugin/install">Installation &rarr;</NuxtLink>
    </div>
  </div>
</template>
