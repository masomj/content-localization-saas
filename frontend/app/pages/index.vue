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
  font-family: Inter, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
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
  left: 0.5rem;
  top: -100px;
  background: #111827;
  color: white;
  padding: 0.5rem 0.75rem;
  border-radius: 0.375rem;
  z-index: 9999;
  transition: top 0.2s;
}

.skip-link:focus {
  top: 0.5rem;
}
</style>
