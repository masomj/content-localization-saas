<template>
  <div>
    <a href="#main-content" class="skip-link">Skip to main content</a>
    <NuxtPage />
  </div>
</template>

<style>
.skip-link {
  position: absolute;
  left: -9999px;
  top: auto;
  width: 1px;
  height: 1px;
  overflow: hidden;
  z-index: 9999;
}

.skip-link:focus,
.skip-link:focus-visible {
  position: fixed;
  left: var(--spacing-4, 1rem);
  top: var(--spacing-4, 1rem);
  width: auto;
  height: auto;
  padding: var(--spacing-2, 0.5rem) var(--spacing-3, 0.75rem);
  background: var(--color-primary-700, #4338ca);
  color: var(--color-white, #fff);
  border-radius: var(--radius-md, 0.375rem);
  text-decoration: none;
  outline: 2px solid var(--color-white, #fff);
  outline-offset: 2px;
}

body { 
  font-family: Inter, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; 
  margin: 0; 
  background: var(--color-background);
  color: var(--color-text-primary);
}
*, *::before, *::after {
  box-sizing: border-box;
}
a {
  color: inherit;
  text-decoration: none;
}
</style>
