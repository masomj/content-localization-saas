<script setup lang="ts">
import UiButton from '~/components/ui/Button.vue'

definePageMeta({
  layout: 'app',
})

useSeoMeta({
  title: 'Page Not Found - LocFlow',
})
</script>

<template>
  <div class="not-found">
    <div class="not-found__icon">🔍</div>
    <h1>Page Not Found</h1>
    <p>Sorry, we couldn't find the page you're looking for.</p>
    <div class="not-found__actions">
      <NuxtLink to="/app/dashboard">
        <UiButton>Go to Dashboard</UiButton>
      </NuxtLink>
      <NuxtLink to="/app/projects">
        <UiButton variant="secondary">View Projects</UiButton>
      </NuxtLink>
    </div>
  </div>
</template>

<style scoped>
.not-found {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 60vh;
  text-align: center;
  padding: var(--spacing-6);
}

.not-found__icon {
  font-size: 4rem;
  margin-bottom: var(--spacing-4);
}

.not-found h1 {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  color: var(--color-gray-900);
  margin: 0 0 var(--spacing-2) 0;
}

.not-found p {
  font-size: var(--font-size-base);
  color: var(--color-gray-500);
  margin: 0 0 var(--spacing-6) 0;
  max-width: 400px;
}

.not-found__actions {
  display: flex;
  gap: var(--spacing-3);
}
</style>
