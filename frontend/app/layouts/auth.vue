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
    </header>

    <main class="auth-main">
      <slot />
    </main>
  </div>
</template>

<style scoped>
.auth-page {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background: var(--color-gray-50);
}

.auth-header {
  padding: var(--spacing-6);
  text-align: center;
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
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-4);
}

@media (max-width: 480px) {
  .auth-header {
    padding: var(--spacing-4);
  }
}
</style>
