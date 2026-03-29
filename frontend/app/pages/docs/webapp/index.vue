<script setup lang="ts">
definePageMeta({ layout: 'docs' })

useSeoMeta({
  title: 'Web App - InterCopy Docs',
  description: 'Overview of the InterCopy web application dashboard.',
})
</script>

<template>
  <div>
    <h1>Web App</h1>
    <p>
      The InterCopy web application is the primary interface for managing your localization workflow. It gives your
      entire team &mdash; developers, translators, reviewers, and project managers &mdash; a shared workspace to
      collaborate on multilingual content.
    </p>

    <h2>Dashboard sections</h2>
    <ul>
      <li>
        <NuxtLink to="/docs/webapp/content"><strong>Managing Content</strong></NuxtLink> &mdash;
        Organise folders, create keys, edit source text, and drag-and-drop to restructure.
      </li>
      <li>
        <NuxtLink to="/docs/webapp/translations"><strong>Translations</strong></NuxtLink> &mdash;
        Add languages, translate strings in the grid editor, and track status.
      </li>
      <li>
        <NuxtLink to="/docs/webapp/review"><strong>Review</strong></NuxtLink> &mdash;
        PR-style review queue with verdicts, comments, and a change timeline.
      </li>
      <li>
        <NuxtLink to="/docs/webapp/versions"><strong>Versions</strong></NuxtLink> &mdash;
        Create releases, take snapshots, and manage live vs. working copies.
      </li>
      <li>
        <NuxtLink to="/docs/webapp/components"><strong>Components</strong></NuxtLink> &mdash;
        Visual canvas for Figma-synced design components with inline text editing.
      </li>
      <li>
        <NuxtLink to="/docs/webapp/export"><strong>Export</strong></NuxtLink> &mdash;
        Download translation bundles in i18next, vue-i18n, react-intl, or flat JSON.
      </li>
      <li>
        <NuxtLink to="/docs/webapp/admin"><strong>Admin</strong></NuxtLink> &mdash;
        Manage projects, organisation settings, and team member roles.
      </li>
    </ul>

    <h2>Navigation</h2>
    <p>
      The app uses a fixed sidebar for primary navigation. Every section listed above is accessible from the sidebar.
      A breadcrumb bar at the top of the page shows your current location and lets you jump back to parent sections.
    </p>

    <div class="docs-next-page">
      <NuxtLink to="/docs/webapp/content">Managing Content &rarr;</NuxtLink>
    </div>
  </div>
</template>
