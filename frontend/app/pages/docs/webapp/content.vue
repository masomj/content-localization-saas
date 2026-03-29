<script setup lang="ts">
definePageMeta({ layout: 'docs' })

useSeoMeta({
  title: 'Managing Content - InterCopy Docs',
  description: 'Learn how to organise folders, create content keys, and manage source text in InterCopy.',
})
</script>

<template>
  <div>
    <h1>Managing Content</h1>
    <p>
      The <strong>Content</strong> page is the heart of InterCopy. It provides a file-explorer-style interface for
      organising your translation keys into a hierarchical folder structure.
    </p>

    <h2>The file explorer</h2>
    <p>
      The left panel displays your content tree. Each node is either a <strong>folder</strong> or a
      <strong>content key</strong>. Click a folder to expand it and reveal its children.
    </p>
    <ul>
      <li>Right-click (or use the context menu button) to rename, move, or delete items.</li>
      <li>Drag and drop keys or folders to reorganise the tree.</li>
      <li>Use the search bar at the top to filter by key name.</li>
    </ul>

    <h2>Creating folders</h2>
    <p>
      Click <strong>New Folder</strong> in the toolbar above the tree. Enter a name and press Enter. Folders can
      be nested to any depth.
    </p>
    <pre><code class="language-text">common/
  buttons/
    save   → "Save"
    cancel → "Cancel"
  labels/
    email  → "Email address"</code></pre>

    <h2>Creating content keys</h2>
    <p>
      Select a folder, then click <strong>New Key</strong>. Give the key a descriptive identifier (e.g.
      <code>hero.title</code>). The right panel will open the source text editor where you type the default
      language value.
    </p>

    <h2>Editing source text</h2>
    <p>
      Click any key in the tree to open the detail panel. The source text field supports:
    </p>
    <ul>
      <li>Plain text strings</li>
      <li>ICU MessageFormat placeholders (e.g. <code>{"{"}name{"}"}</code>)</li>
      <li>HTML snippets (if your export format supports them)</li>
    </ul>

    <h2>Drag and drop</h2>
    <p>
      Restructuring is as simple as dragging a key or folder onto a new parent. InterCopy will update all internal
      references automatically.
    </p>

    <div class="docs-next-page">
      <NuxtLink to="/docs/webapp/translations">Translations &rarr;</NuxtLink>
    </div>
  </div>
</template>
