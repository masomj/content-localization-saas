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
    <a href="#main-content" class="skip-link">Skip to main content</a>
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

.skip-link {
  position: absolute;
  left: var(--spacing-4);
  top: -100px;
  background: var(--color-gray-900);
  color: var(--color-white);
  padding: var(--spacing-2) var(--spacing-3);
  border-radius: var(--radius-md);
  z-index: 9999;
  transition: top var(--transition-fast);
  text-decoration: none;
  font-weight: var(--font-weight-medium);
  font-size: var(--font-size-sm);
}

.skip-link:focus {
  top: var(--spacing-4);
}

.skip-link:focus-visible {
  outline: 2px solid var(--color-primary-500);
  outline-offset: 2px;
}
</style>
