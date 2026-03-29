<script setup lang="ts">
definePageMeta({ layout: 'docs' })

useSeoMeta({
  title: 'Install Figma Plugin - InterCopy Docs',
  description: 'How to install the InterCopy Figma plugin.',
})
</script>

<template>
  <div>
    <h1>Installation</h1>
    <p>
      The InterCopy plugin is available on the Figma Community. Installation takes under a minute.
    </p>

    <h2>Step 1: Find the plugin</h2>
    <p>
      Open Figma and go to <strong>Plugins &gt; Browse plugins in Community</strong>. Search for
      <strong>"InterCopy"</strong>.
    </p>

    <h2>Step 2: Install</h2>
    <p>
      Click <strong>Install</strong> on the InterCopy plugin page. The plugin will appear in your
      <strong>Plugins</strong> menu.
    </p>

    <h2>Step 3: Run the plugin</h2>
    <p>
      Open a Figma file, right-click the canvas, and select <strong>Plugins &gt; InterCopy</strong>. The plugin
      panel will open on the right side.
    </p>

    <blockquote>
      <p>
        The first time you run the plugin you will need to authenticate. See the
        <NuxtLink to="/docs/figma-plugin/auth">Authentication</NuxtLink> guide.
      </p>
    </blockquote>

    <h2>Requirements</h2>
    <ul>
      <li>Figma desktop app or Figma in a Chromium-based browser</li>
      <li>An InterCopy account with at least one project</li>
      <li>Edit access to the Figma file</li>
    </ul>

    <div class="docs-next-page">
      <NuxtLink to="/docs/figma-plugin/auth">Authentication &rarr;</NuxtLink>
    </div>
  </div>
</template>
