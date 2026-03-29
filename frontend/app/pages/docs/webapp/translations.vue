<script setup lang="ts">
definePageMeta({ layout: 'docs' })

useSeoMeta({
  title: 'Translations - InterCopy Docs',
  description: 'Add languages, translate strings, and manage the translation status workflow.',
})
</script>

<template>
  <div>
    <h1>Translations</h1>
    <p>
      InterCopy provides a grid-based translation editor that makes it easy to translate content keys into every
      target language your project supports.
    </p>

    <h2>Adding languages</h2>
    <p>
      Navigate to <strong>Content &gt; Languages</strong> to manage your project's language list. Click
      <strong>Add Language</strong> and select from the list of BCP 47 locale codes. The source language is set
      when the project is created and cannot be changed later.
    </p>

    <h2>The translation grid</h2>
    <p>
      The main content view shows a grid with one row per content key and one column per language. Click a cell to
      open the inline editor and type the translation.
    </p>
    <ul>
      <li><strong>Empty cells</strong> &mdash; No translation entered yet. Highlighted to draw attention.</li>
      <li><strong>Draft</strong> &mdash; Translation has been entered but not submitted for review.</li>
      <li><strong>In review</strong> &mdash; Translation has been submitted for review.</li>
      <li><strong>Approved</strong> &mdash; Translation passed review and is ready for export.</li>
    </ul>

    <h2>Status workflow</h2>
    <p>
      Each translation moves through a lifecycle:
    </p>
    <ol>
      <li><strong>Draft</strong> &mdash; Translator enters or updates the text.</li>
      <li><strong>Submitted</strong> &mdash; Translator submits the batch for review.</li>
      <li><strong>Approved / Rejected</strong> &mdash; Reviewer accepts or sends back with comments.</li>
    </ol>
    <p>
      See <NuxtLink to="/docs/webapp/review">Review</NuxtLink> for the full review workflow.
    </p>

    <h2>Bulk operations</h2>
    <p>
      Select multiple keys using the checkboxes and then use the toolbar to:
    </p>
    <ul>
      <li>Submit selected translations for review</li>
      <li>Mark selected as approved (reviewers only)</li>
      <li>Delete selected keys</li>
    </ul>

    <div class="docs-next-page">
      <NuxtLink to="/docs/webapp/review">Review &rarr;</NuxtLink>
    </div>
  </div>
</template>
