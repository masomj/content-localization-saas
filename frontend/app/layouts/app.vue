<script setup lang="ts">
import AppBreadcrumbs from '~/components/AppBreadcrumbs.vue'

const sidebarCollapsed = ref(false)

function toggleSidebar() {
  sidebarCollapsed.value = !sidebarCollapsed.value
}

const auth = useAuth()
const route = useRoute()

const breadcrumbs = computed(() => {
  const pathSegments = route.path.split('/').filter(Boolean)
  const items = [{ label: 'Home', to: '/app/dashboard' }]
  
  let currentPath = ''
  for (let i = 0; i < pathSegments.length; i++) {
    const segment = pathSegments[i]
    currentPath += `/${segment}`
    
    if (segment === 'app') continue
    
    const label = segment.charAt(0).toUpperCase() + segment.slice(1).replace(/-/g, ' ')
    const isLast = i === pathSegments.length - 1
    
    items.push({
      label,
      to: isLast ? undefined : currentPath
    })
  }
  
  return items
})
</script>

<template>
  <div class="layout-app" :class="{ 'layout-app--collapsed': sidebarCollapsed }">
    <aside class="layout-app__sidebar">
      <div class="layout-app__sidebar-header">
        <NuxtLink to="/app/dashboard" class="layout-app__brand">
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
        <NuxtLink to="/app/dashboard" class="nav-link" :class="{ 'nav-link--collapsed': sidebarCollapsed }">
          <svg viewBox="0 0 20 20" fill="currentColor"><path d="M10.707 2.293a1 1 0 00-1.414 0l-7 7a1 1 0 001.414 1.414L4 10.414V17a1 1 0 001 1h2a1 1 0 001-1v-2a1 1 0 011-1h2a1 1 0 011 1v2a1 1 0 001 1h2a1 1 0 001-1v-6.586l.293.293a1 1 0 001.414-1.414l-7-7z" /></svg>
          <span v-if="!sidebarCollapsed">Dashboard</span>
        </NuxtLink>
        <NuxtLink to="/app/projects" class="nav-link" :class="{ 'nav-link--collapsed': sidebarCollapsed }">
          <svg viewBox="0 0 20 20" fill="currentColor"><path d="M2 6a2 2 0 012-2h5l2 2h5a2 2 0 012 2v6a2 2 0 01-2 2H4a2 2 0 01-2-2V6z" /></svg>
          <span v-if="!sidebarCollapsed">Projects</span>
        </NuxtLink>
        <NuxtLink to="/app/content" class="nav-link" :class="{ 'nav-link--collapsed': sidebarCollapsed }">
          <svg viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M4 4a2 2 0 012-2h4.586A2 2 0 0112 2.586L15.414 6A2 2 0 0116 7.414V16a2 2 0 01-2 2H6a2 2 0 01-2-2V4z" clip-rule="evenodd" /></svg>
          <span v-if="!sidebarCollapsed">Content</span>
        </NuxtLink>
        <NuxtLink to="/app/review" class="nav-link" :class="{ 'nav-link--collapsed': sidebarCollapsed }">
          <svg viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" /></svg>
          <span v-if="!sidebarCollapsed">Review</span>
        </NuxtLink>
        <NuxtLink to="/app/integrations" class="nav-link" :class="{ 'nav-link--collapsed': sidebarCollapsed }">
          <svg viewBox="0 0 20 20" fill="currentColor"><path d="M13 6a3 3 0 11-6 0 3 3 0 016 0zM18 8a2 2 0 11-4 0 2 2 0 014 0zM14 15a4 4 0 00-8 0v3h8v-3zM6 8a2 2 0 11-4 0 2 2 0 014 0zM16 18v-3a5.972 5.972 0 00-.75-2.906A3.005 3.005 0 0119 15v3h-3zM4.75 12.094A5.973 5.973 0 004 15v3H1v-3a3 3 0 013.75-2.906z" /></svg>
          <span v-if="!sidebarCollapsed">Integrations</span>
        </NuxtLink>
        <NuxtLink v-if="auth.isAdmin.value" to="/app/settings" class="nav-link" :class="{ 'nav-link--collapsed': sidebarCollapsed }">
          <svg viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M11.49 3.17c-.38-1.56-2.6-1.56-2.98 0a1.532 1.532 0 01-2.286.948c-1.372-.836-2.942.734-2.106 2.106.54.886.061 2.042-.947 2.287-1.561.379-1.561 2.6 0 2.978a1.532 1.532 0 01.947 2.287c-.836 1.372.734 2.942 2.106 2.106a1.532 1.532 0 012.287.947c.379 1.561 2.6 1.561 2.978 0a1.533 1.533 0 012.287-.947c1.372.836 2.942-.734 2.106-2.106a1.533 1.533 0 01.947-2.287c1.561-.379 1.561-2.6 0-2.978a1.532 1.532 0 01-.947-2.287c.836-1.372-.734-2.942-2.106-2.106a1.532 1.532 0 01-2.287-.947zM10 13a3 3 0 100-6 3 3 0 000 6z" clip-rule="evenodd" /></svg>
          <span v-if="!sidebarCollapsed">Settings</span>
        </NuxtLink>
        <NuxtLink v-if="auth.isAdmin.value" to="/app/settings/members" class="nav-link" :class="{ 'nav-link--collapsed': sidebarCollapsed }">
          <svg viewBox="0 0 20 20" fill="currentColor"><path d="M9 6a3 3 0 11-6 0 3 3 0 016 0zM17 6a3 3 0 11-6 0 3 3 0 016 0zM12.93 17c.046-.327.07-.66.07-1a6.97 6.97 0 00-1.5-4.33A5 5 0 0119 16v1h-6.07zM6 11a5 5 0 015 5v1H1v-1a5 5 0 015-5z" /></svg>
          <span v-if="!sidebarCollapsed">Team Members</span>
        </NuxtLink>
      </nav>
    </aside>
    <div class="layout-app__main">
      <header class="layout-app__header">
        <AppBreadcrumbs :items="breadcrumbs" class="layout-app__breadcrumbs" />
        <div class="layout-app__user">
          <UiThemeToggle />
          <span class="layout-app__user-name">{{ auth.user.value?.name }}</span>
          <button class="layout-app__logout" @click="auth.logout()" aria-label="Log out">
            <svg viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M3 3a1 1 0 00-1 1v12a1 1 0 102 0V4a1 1 0 00-1-1zm10.293 9.293a1 1 0 001.414 1.414l3-3a1 1 0 000-1.414l-3-3a1 1 0 10-1.414 1.414L14.586 9H7a1 1 0 100 2h7.586l-1.293 1.293z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>
      </header>
      <main id="main-content" class="layout-app__content" tabindex="-1">
        <slot />
      </main>
    </div>
  </div>
</template>

<style scoped>
.layout-app {
  display: flex;
  min-height: 100vh;
  background: var(--color-background);
}

.layout-app--collapsed .layout-app__sidebar {
  width: 64px;
}

.layout-app--collapsed .layout-app__main {
  margin-left: 64px;
}

.layout-app__sidebar {
  width: 260px;
  background: var(--color-surface);
  border-right: 1px solid var(--color-border);
  display: flex;
  flex-direction: column;
  transition: width var(--transition-normal);
  position: fixed;
  top: 0;
  left: 0;
  bottom: 0;
  z-index: var(--z-sticky);
}

.layout-app__sidebar-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--spacing-4);
  border-bottom: 1px solid var(--color-border);
}

.layout-app__brand {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
  text-decoration: none;
  color: var(--color-text-primary);
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
  color: var(--color-text-muted);
  border-radius: var(--radius-md);
  transition: all var(--transition-fast);
}

.layout-app__sidebar-toggle:hover {
  background: var(--color-gray-100);
  color: var(--color-gray-700);
}

.layout-app__sidebar-toggle:focus-visible {
  outline: 2px solid var(--color-primary-500);
  outline-offset: 2px;
}

.layout-app__sidebar-toggle svg {
  width: 1.25rem;
  height: 1.25rem;
}

.layout-app__nav {
  flex: 1;
  padding: var(--spacing-4);
  display: flex;
  flex-direction: column;
  gap: var(--spacing-1);
  overflow-y: auto;
}

.nav-link {
  display: flex;
  align-items: center;
  gap: var(--spacing-3);
  padding: var(--spacing-3);
  color: var(--color-text-secondary);
  text-decoration: none;
  border-radius: var(--radius-lg);
  transition: all var(--transition-fast);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
}

.nav-link:hover {
  background: var(--color-gray-100);
  color: var(--color-text-primary);
}

.nav-link:focus-visible {
  outline: 2px solid var(--color-primary-500);
  outline-offset: 2px;
}

.nav-link.router-link-active {
  background: var(--color-primary-50);
  color: var(--color-primary-700);
}

.nav-link svg {
  width: 1.25rem;
  height: 1.25rem;
  flex-shrink: 0;
}

.nav-link--collapsed {
  justify-content: center;
  padding: var(--spacing-3);
}

.layout-app__main {
  flex: 1;
  margin-left: 260px;
  display: flex;
  flex-direction: column;
  transition: margin-left var(--transition-normal);
}

.layout-app__header {
  height: 64px;
  background: var(--color-surface);
  border-bottom: 1px solid var(--color-border);
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 var(--spacing-6);
  position: sticky;
  top: 0;
  z-index: var(--z-sticky);
}

.layout-app__breadcrumbs {
  flex: 1;
  min-width: 0;
}

.layout-app__user {
  display: flex;
  align-items: center;
  gap: var(--spacing-3);
}

.layout-app__user-name {
  font-size: var(--font-size-sm);
  color: var(--color-text-primary);
  font-weight: var(--font-weight-medium);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.layout-app__logout {
  background: none;
  border: none;
  padding: var(--spacing-2);
  cursor: pointer;
  color: var(--color-text-muted);
  border-radius: var(--radius-md);
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all var(--transition-fast);
}

.layout-app__logout:hover {
  background: var(--color-gray-100);
  color: var(--color-gray-700);
}

.layout-app__logout:focus-visible {
  outline: 2px solid var(--color-primary-500);
  outline-offset: 2px;
}

.layout-app__logout svg {
  width: 1.25rem;
  height: 1.25rem;
}

.layout-app__content {
  flex: 1;
  padding: var(--spacing-6);
}

@media (max-width: 1024px) {
  .layout-app__sidebar {
    width: 64px;
    overflow: hidden;
  }

  .layout-app__sidebar:hover {
    width: 260px;
  }

  .layout-app__main {
    margin-left: 64px;
  }

  .nav-link--collapsed span {
    display: none;
  }

  .layout-app__sidebar:hover .nav-link--collapsed span {
    display: inline;
  }

  .layout-app__brand-text {
    display: none;
  }

  .layout-app__sidebar:hover .layout-app__brand-text {
    display: inline;
  }
}

@media (max-width: 768px) {
  .layout-app__sidebar {
    transform: translateX(-100%);
    width: 260px;
    transition: transform var(--transition-normal);
  }

  .layout-app__sidebar--open {
    transform: translateX(0);
  }

  .layout-app__main {
    margin-left: 0;
  }

  .layout-app__content {
    padding: var(--spacing-4);
  }

  .layout-app__header {
    padding: 0 var(--spacing-4);
  }

  .layout-app__user-name {
    display: none;
  }
}
</style>
