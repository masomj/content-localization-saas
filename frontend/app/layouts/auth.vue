<script setup lang="ts">
definePageMeta({
  layout: 'auth',
})
</script>

<template>
  <div class="auth-page">
    <header class="auth-header">
      <NuxtLink to="/" class="brand" aria-label="LocFlow Home">
        <span class="brand-icon" aria-hidden="true">◈</span>
        <span class="brand-text">LocFlow</span>
      </NuxtLink>
      <ThemeToggle />
    </header>

    <main id="main-content" class="auth-main" tabindex="-1">
      <slot />
    </main>
  </div>
</template>

<style scoped>
.auth-page {
  position: relative;
  min-height: 100vh;
  background: var(--color-gray-50);
}

.auth-header {
  position: absolute;
  top: var(--spacing-6);
  left: var(--spacing-6);
  right: var(--spacing-6);
  display: flex;
  align-items: center;
  justify-content: space-between;
  z-index: 1;
}

.brand {
  display: inline-flex;
  align-items: center;
  gap: var(--spacing-2);
  text-decoration: none;
  color: var(--color-gray-900);
  font-weight: var(--font-weight-bold);
  font-size: var(--font-size-xl);
}

.brand-icon {
  font-size: var(--font-size-2xl);
  color: var(--color-primary-600);
}

.auth-main {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: calc(var(--spacing-16) + var(--spacing-4)) var(--spacing-4) var(--spacing-8);
}

@media (max-width: 480px) {
  .auth-header {
    top: var(--spacing-4);
    left: var(--spacing-4);
    right: var(--spacing-4);
  }

  .auth-main {
    padding-top: calc(var(--spacing-12) + var(--spacing-4));
  }
}
</style>
