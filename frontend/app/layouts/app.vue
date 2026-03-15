<script setup lang="ts">
const sidebarCollapsed = ref(false)

function toggleSidebar() {
  sidebarCollapsed.value = !sidebarCollapsed.value
}
</script>

<template>
  <div class="layout-app">
    <aside class="layout-app__sidebar" :class="{ 'layout-app__sidebar--collapsed': sidebarCollapsed }">
      <div class="layout-app__sidebar-header">
        <NuxtLink to="/" class="layout-app__brand">
          <span class="layout-app__brand-icon">◈</span>
          <span v-if="!sidebarCollapsed" class="layout-app__brand-text">LocFlow</span>
        </NuxtLink>
        <button class="layout-app__sidebar-toggle" @click="toggleSidebar" aria-label="Toggle sidebar">
          <svg viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M3 5a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zM3 10a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zM3 15a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1z" clip-rule="evenodd" />
          </svg>
        </button>
      </div>
      <nav class="layout-app__nav">
        <slot name="sidebar" />
      </nav>
    </aside>
    <div class="layout-app__main">
      <header class="layout-app__header">
        <slot name="header" />
      </header>
      <main class="layout-app__content">
        <slot />
      </main>
    </div>
  </div>
</template>

<style scoped>
.layout-app {
  display: flex;
  min-height: 100vh;
  background: var(--color-gray-50);
}

.layout-app__sidebar {
  width: 260px;
  background: var(--color-white);
  border-right: 1px solid var(--color-gray-200);
  display: flex;
  flex-direction: column;
  transition: width var(--transition-normal);
  position: fixed;
  top: 0;
  left: 0;
  bottom: 0;
  z-index: var(--z-sticky);
}

.layout-app__sidebar--collapsed {
  width: 64px;
}

.layout-app__sidebar-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--spacing-4);
  border-bottom: 1px solid var(--color-gray-200);
}

.layout-app__brand {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
  text-decoration: none;
  color: var(--color-gray-900);
  font-weight: var(--font-weight-bold);
  font-size: var(--font-size-lg);
}

.layout-app__brand-icon {
  color: var(--color-primary-600);
  font-size: var(--font-size-xl);
}

.layout-app__sidebar-toggle {
  background: none;
  border: none;
  padding: var(--spacing-2);
  cursor: pointer;
  color: var(--color-gray-500);
  border-radius: var(--radius-md);
}

.layout-app__sidebar-toggle:hover {
  background: var(--color-gray-100);
  color: var(--color-gray-700);
}

.layout-app__sidebar-toggle svg {
  width: 1.25rem;
  height: 1.25rem;
}

.layout-app__nav {
  flex: 1;
  padding: var(--spacing-4);
}

.layout-app__main {
  flex: 1;
  margin-left: 260px;
  display: flex;
  flex-direction: column;
  transition: margin-left var(--transition-normal);
}

.layout-app__sidebar--collapsed + .layout-app__main,
.layout-app__sidebar--collapsed ~ .layout-app__main {
  margin-left: 64px;
}

.layout-app__header {
  height: 64px;
  background: var(--color-white);
  border-bottom: 1px solid var(--color-gray-200);
  display: flex;
  align-items: center;
  padding: 0 var(--spacing-6);
  position: sticky;
  top: 0;
  z-index: var(--z-sticky);
}

.layout-app__content {
  flex: 1;
  padding: var(--spacing-6);
}
</style>
