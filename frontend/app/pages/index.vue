<script setup lang="ts">
useSeoMeta({
  title: 'LocFlow - Translate Your Content. Scale Globally.',
  description: 'The all-in-one localization platform that helps teams manage translations, collaborate with reviewers, and deliver localized content to every market.',
  ogTitle: 'LocFlow - Translate Your Content. Scale Globally.',
  ogDescription: 'The all-in-one localization platform that helps teams manage translations, collaborate with reviewers, and deliver localized content to every market.',
  ogImage: '/og-image.png',
  twitterCard: 'summary_large_image',
})
</script>

<template>
  <div class="landing-page">
    <header class="landing-page__toolbar">
      <UiThemeToggle />
    </header>

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

.landing-page__toolbar {
  position: absolute;
  top: var(--spacing-4);
  right: var(--spacing-4);
  z-index: 20;
}

.landing-page main {
  flex: 1;
}

</style>
