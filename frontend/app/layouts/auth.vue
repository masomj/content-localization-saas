<template>
  <div class="auth-page">
    <header class="auth-header">
      <NuxtLink to="/" class="brand" aria-label="InterCopy Home">
        <span class="brand-icon" aria-hidden="true">◈</span>
        <span class="brand-text">InterCopy</span>
      </NuxtLink>
      <ClientOnly><UiThemeToggle /></ClientOnly>
    </header>

    <main id="main-content" class="auth-main" tabindex="-1">
      <slot />
    </main>
  </div>
</template>

<style scoped>
.auth-page {
  position: relative;
  min-height: 100dvh;
  background: var(--color-background);
}

.auth-header {
  position: absolute;
  top: var(--spacing-6);
  left: var(--spacing-6);
  right: var(--spacing-6);
  display: flex;
  align-items: center;
  justify-content: space-between;
  z-index: 10;
}

.brand {
  display: inline-flex;
  align-items: center;
  gap: var(--spacing-2);
  text-decoration: none;
  color: var(--color-text-primary);
  font-weight: var(--font-weight-bold);
  font-size: var(--font-size-xl);
}

.brand-icon {
  font-size: var(--font-size-2xl);
  color: var(--color-primary-600);
}

.auth-main {
  display: grid;
  place-items: center;
  min-height: 100dvh;
  padding: var(--spacing-16) var(--spacing-4) var(--spacing-8);
}

@media (max-width: 480px) {
  .auth-header {
    top: var(--spacing-4);
    left: var(--spacing-4);
    right: var(--spacing-4);
  }

  .auth-main {
    padding: var(--spacing-8) var(--spacing-4);
  }
}
</style>
