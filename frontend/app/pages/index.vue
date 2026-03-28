<script setup lang="ts">
useSeoMeta({
  title: 'InterCopy - Translate Your Content. Scale Globally.',
  description: 'The all-in-one localization platform that helps teams manage translations, collaborate with reviewers, and deliver localized content to every market.',
  ogTitle: 'InterCopy - Translate Your Content. Scale Globally.',
  ogDescription: 'The all-in-one localization platform that helps teams manage translations, collaborate with reviewers, and deliver localized content to every market.',
  ogImage: '/og-image.png',
  twitterCard: 'summary_large_image',
})
</script>

<template>
  <div class="landing-page">
    <LandingNavBar />

    <main id="main-content" tabindex="-1">
      <LandingHeroSection />
      <LandingFeaturesSection />
      <LandingBenefitsSection />
      <LandingCtaBand />
    </main>

    <LandingFooterSection />
  </div>
</template>

<style>
* {
  box-sizing: border-box;
}

body {
  margin: 0;
  font-family: var(--font-family-sans);
}

.landing-page {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  position: relative;
}

.landing-page main {
  flex: 1;
}

</style>
