<script setup lang="ts">
definePageMeta({ layout: 'docs' })

useSeoMeta({
  title: 'Review - InterCopy Docs',
  description: 'PR-style review workflow for translations: queue, verdicts, and timeline.',
})
</script>

<template>
  <div>
    <h1>Review</h1>
    <p>
      InterCopy includes a <strong>PR-style review system</strong> that lets translators submit work for approval
      and reviewers provide feedback before translations go live.
    </p>

    <h2>The review queue</h2>
    <p>
      Navigate to <strong>Review</strong> in the sidebar to see all pending review requests. Each request shows:
    </p>
    <ul>
      <li>The submitter's name and submission date</li>
      <li>Number of translations included</li>
      <li>Target language(s)</li>
      <li>Current status (pending, approved, changes requested)</li>
    </ul>

    <h2>Reviewing translations</h2>
    <p>
      Click a review request to open the detail view. You will see a side-by-side comparison of the source text and
      the proposed translation for every key in the batch.
    </p>
    <p>
      For each translation you can:
    </p>
    <ul>
      <li><strong>Approve</strong> &mdash; The translation is correct and ready for export.</li>
      <li><strong>Request changes</strong> &mdash; Leave a comment explaining what needs to be fixed.</li>
      <li><strong>Skip</strong> &mdash; Move on without a verdict; another reviewer can handle it.</li>
    </ul>

    <h2>Verdicts</h2>
    <p>
      Once you have reviewed all translations in a batch, submit your overall verdict:
    </p>
    <ul>
      <li><strong>Approve all</strong> &mdash; Mark the entire batch as approved.</li>
      <li><strong>Request changes</strong> &mdash; Send the batch back to the translator with your comments.</li>
    </ul>

    <h2>Timeline</h2>
    <p>
      Every review request has a timeline showing all activity: submissions, comments, verdict changes, and
      re-submissions. This provides a full audit trail of every decision.
    </p>

    <div class="docs-next-page">
      <NuxtLink to="/docs/webapp/versions">Versions &rarr;</NuxtLink>
    </div>
  </div>
</template>
