<script setup lang="ts">
const route = useRoute()
const sidebarOpen = ref(false)

function toggleSidebar() {
  sidebarOpen.value = !sidebarOpen.value
}

function closeSidebar() {
  sidebarOpen.value = false
}

interface NavItem {
  label: string
  to: string
}

interface NavSection {
  label: string
  children: NavItem[]
}

const sections: NavSection[] = [
  {
    label: 'Getting Started',
    children: [
      { label: 'Overview', to: '/docs/getting-started' },
      { label: 'Quick Start', to: '/docs/getting-started/quickstart' },
      { label: 'Core Concepts', to: '/docs/getting-started/concepts' },
    ],
  },
  {
    label: 'Web App',
    children: [
      { label: 'Overview', to: '/docs/webapp' },
      { label: 'Managing Content', to: '/docs/webapp/content' },
      { label: 'Translations', to: '/docs/webapp/translations' },
      { label: 'Review', to: '/docs/webapp/review' },
      { label: 'Versions', to: '/docs/webapp/versions' },
      { label: 'Components', to: '/docs/webapp/components' },
      { label: 'Export', to: '/docs/webapp/export' },
      { label: 'Admin', to: '/docs/webapp/admin' },
    ],
  },
  {
    label: 'Figma Plugin',
    children: [
      { label: 'Overview', to: '/docs/figma-plugin' },
      { label: 'Installation', to: '/docs/figma-plugin/install' },
      { label: 'Authentication', to: '/docs/figma-plugin/auth' },
      { label: 'Push / Pull Sync', to: '/docs/figma-plugin/sync' },
    ],
  },
  {
    label: 'CLI',
    children: [
      { label: 'Overview', to: '/docs/cli' },
      { label: 'Commands', to: '/docs/cli/commands' },
    ],
  },
  {
    label: 'API Reference',
    children: [
      { label: 'Overview', to: '/docs/api' },
      { label: 'Endpoints', to: '/docs/api/endpoints' },
    ],
  },
  {
    label: 'Integrations',
    children: [
      { label: 'Overview', to: '/docs/integrations' },
      { label: 'i18next', to: '/docs/integrations/i18next' },
      { label: 'vue-i18n', to: '/docs/integrations/vue-i18n' },
      { label: 'react-intl', to: '/docs/integrations/react-intl' },
    ],
  },
]

const expandedSections = ref<Record<string, boolean>>({})

function isSectionActive(section: NavSection): boolean {
  return section.children.some((c) => route.path === c.to)
}

function isSectionExpanded(section: NavSection): boolean {
  if (expandedSections.value[section.label] !== undefined) {
    return expandedSections.value[section.label]
  }
  return isSectionActive(section)
}

function toggleSection(section: NavSection) {
  expandedSections.value[section.label] = !isSectionExpanded(section)
}

// Auto-expand the active section on route change
watch(
  () => route.path,
  () => {
    for (const section of sections) {
      if (isSectionActive(section)) {
        expandedSections.value[section.label] = true
      }
    }
    closeSidebar()
  },
  { immediate: true }
)

// On-page TOC from headings
interface TocItem {
  id: string
  text: string
  level: number
}

const tocItems = ref<TocItem[]>([])

function buildToc() {
  if (typeof document === 'undefined') return
  const content = document.querySelector('.docs-content')
  if (!content) return
  const headings = content.querySelectorAll('h2, h3')
  const items: TocItem[] = []
  headings.forEach((el) => {
    const heading = el as HTMLElement
    if (!heading.id) {
      heading.id = heading.textContent?.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, '') || ''
    }
    items.push({
      id: heading.id,
      text: heading.textContent || '',
      level: parseInt(heading.tagName[1]),
    })
  })
  tocItems.value = items
}

onMounted(() => {
  nextTick(() => buildToc())
})

watch(
  () => route.path,
  () => {
    nextTick(() => {
      setTimeout(buildToc, 100)
    })
  }
)
</script>

<template>
  <div class="docs-layout">
    <!-- Top bar -->
    <header class="docs-topbar">
      <div class="docs-topbar__left">
        <button class="docs-topbar__hamburger" aria-label="Toggle navigation" @click="toggleSidebar">
          <svg viewBox="0 0 20 20" fill="currentColor" width="20" height="20">
            <path fill-rule="evenodd" d="M3 5a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zM3 10a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zM3 15a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1z" clip-rule="evenodd" />
          </svg>
        </button>
        <NuxtLink to="/" class="docs-topbar__brand" aria-label="InterCopy home">
          <span class="docs-topbar__brand-icon" aria-hidden="true">&#9672;</span>
          <span class="docs-topbar__brand-text">InterCopy</span>
        </NuxtLink>
        <span class="docs-topbar__separator" aria-hidden="true">/</span>
        <NuxtLink to="/docs" class="docs-topbar__docs-link">Docs</NuxtLink>
      </div>
      <div class="docs-topbar__right">
        <ClientOnly><UiThemeToggle /></ClientOnly>
      </div>
    </header>

    <!-- Mobile sidebar overlay -->
    <div v-if="sidebarOpen" class="docs-overlay" @click="closeSidebar" />

    <!-- Left sidebar - navigation -->
    <aside class="docs-sidebar" :class="{ 'docs-sidebar--open': sidebarOpen }">
      <nav class="docs-sidebar__nav" aria-label="Documentation navigation">
        <div v-for="section in sections" :key="section.label" class="docs-sidebar__section">
          <button
            class="docs-sidebar__section-toggle"
            :aria-expanded="isSectionExpanded(section)"
            @click="toggleSection(section)"
          >
            <svg
              class="docs-sidebar__chevron"
              :class="{ 'docs-sidebar__chevron--open': isSectionExpanded(section) }"
              viewBox="0 0 20 20"
              fill="currentColor"
              width="16"
              height="16"
            >
              <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd" />
            </svg>
            {{ section.label }}
          </button>
          <ul v-show="isSectionExpanded(section)" class="docs-sidebar__links">
            <li v-for="item in section.children" :key="item.to">
              <NuxtLink
                :to="item.to"
                class="docs-sidebar__link"
                :class="{ 'docs-sidebar__link--active': route.path === item.to }"
                @click="closeSidebar"
              >
                {{ item.label }}
              </NuxtLink>
            </li>
          </ul>
        </div>
      </nav>
    </aside>

    <!-- Main content area -->
    <div class="docs-main">
      <main id="main-content" class="docs-content" tabindex="-1">
        <slot />
      </main>

      <!-- Right sidebar - on-page TOC -->
      <aside v-if="tocItems.length > 0" class="docs-toc">
        <p class="docs-toc__title">On this page</p>
        <ul class="docs-toc__list">
          <li v-for="item in tocItems" :key="item.id" :class="{ 'docs-toc__item--nested': item.level === 3 }">
            <a :href="'#' + item.id" class="docs-toc__link">{{ item.text }}</a>
          </li>
        </ul>
      </aside>
    </div>
  </div>
</template>

<style scoped>
.docs-layout {
  min-height: 100vh;
  background: var(--color-background);
  display: flex;
  flex-direction: column;
}

/* ---------- Top bar ---------- */
.docs-topbar {
  height: 56px;
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

.docs-topbar__left {
  display: flex;
  align-items: center;
  gap: var(--spacing-3);
}

.docs-topbar__hamburger {
  display: none;
  background: none;
  border: none;
  padding: var(--spacing-2);
  cursor: pointer;
  color: var(--color-text-muted);
  border-radius: var(--radius-md);
}

.docs-topbar__hamburger:hover {
  background: var(--color-gray-100);
  color: var(--color-text-primary);
}

.docs-topbar__brand {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
  text-decoration: none;
  color: var(--color-text-primary);
  font-weight: var(--font-weight-bold);
  font-size: var(--font-size-lg);
}

.docs-topbar__brand-icon {
  color: var(--color-primary-600);
  font-size: var(--font-size-xl);
}

.docs-topbar__separator {
  color: var(--color-text-muted);
  font-size: var(--font-size-lg);
}

.docs-topbar__docs-link {
  color: var(--color-text-secondary);
  text-decoration: none;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
}

.docs-topbar__docs-link:hover {
  color: var(--color-primary-600);
}

.docs-topbar__right {
  display: flex;
  align-items: center;
  gap: var(--spacing-3);
}

/* ---------- Sidebar ---------- */
.docs-sidebar {
  position: fixed;
  top: 56px;
  left: 0;
  bottom: 0;
  width: 260px;
  background: var(--color-surface);
  border-right: 1px solid var(--color-border);
  overflow-y: auto;
  z-index: var(--z-sticky);
  padding: var(--spacing-4) 0;
}

.docs-sidebar__nav {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-1);
}

.docs-sidebar__section {
  padding: 0 var(--spacing-3);
}

.docs-sidebar__section-toggle {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
  width: 100%;
  background: none;
  border: none;
  padding: var(--spacing-2) var(--spacing-2);
  cursor: pointer;
  color: var(--color-text-primary);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  border-radius: var(--radius-md);
  text-align: left;
  transition: background var(--transition-fast);
}

.docs-sidebar__section-toggle:hover {
  background: var(--color-gray-100);
}

.docs-sidebar__chevron {
  flex-shrink: 0;
  transition: transform var(--transition-fast);
}

.docs-sidebar__chevron--open {
  transform: rotate(90deg);
}

.docs-sidebar__links {
  list-style: none;
  margin: 0;
  padding: 0 0 var(--spacing-2) var(--spacing-6);
}

.docs-sidebar__link {
  display: block;
  padding: var(--spacing-1) var(--spacing-3);
  color: var(--color-text-secondary);
  text-decoration: none;
  font-size: var(--font-size-sm);
  border-radius: var(--radius-md);
  transition: all var(--transition-fast);
  border-left: 2px solid transparent;
}

.docs-sidebar__link:hover {
  color: var(--color-text-primary);
  background: var(--color-gray-100);
}

.docs-sidebar__link--active {
  color: var(--color-primary-700);
  background: var(--color-primary-50);
  border-left-color: var(--color-primary-600);
  font-weight: var(--font-weight-medium);
}

/* ---------- Mobile overlay ---------- */
.docs-overlay {
  display: none;
}

/* ---------- Main content ---------- */
.docs-main {
  margin-left: 260px;
  margin-top: 56px;
  display: flex;
  min-height: calc(100vh - 56px);
}

.docs-content {
  flex: 1;
  max-width: 780px;
  padding: var(--spacing-8) var(--spacing-8) var(--spacing-16);
  min-width: 0;
}

/* ---------- Right TOC sidebar ---------- */
.docs-toc {
  width: 220px;
  flex-shrink: 0;
  padding: var(--spacing-8) var(--spacing-4) var(--spacing-8) 0;
  position: sticky;
  top: 56px;
  max-height: calc(100vh - 56px);
  overflow-y: auto;
  align-self: flex-start;
}

.docs-toc__title {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-muted);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  margin: 0 0 var(--spacing-3);
}

.docs-toc__list {
  list-style: none;
  margin: 0;
  padding: 0;
}

.docs-toc__list li {
  margin-bottom: var(--spacing-1);
}

.docs-toc__item--nested {
  padding-left: var(--spacing-3);
}

.docs-toc__link {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  text-decoration: none;
  line-height: var(--line-height-relaxed);
  transition: color var(--transition-fast);
}

.docs-toc__link:hover {
  color: var(--color-primary-600);
}

/* ---------- Responsive ---------- */
@media (max-width: 1280px) {
  .docs-toc {
    display: none;
  }
}

@media (max-width: 768px) {
  .docs-topbar__hamburger {
    display: flex;
    align-items: center;
  }

  .docs-sidebar {
    transform: translateX(-100%);
    transition: transform var(--transition-normal);
    z-index: var(--z-modal);
  }

  .docs-sidebar--open {
    transform: translateX(0);
  }

  .docs-overlay {
    display: block;
    position: fixed;
    inset: 0;
    background: rgba(0, 0, 0, 0.4);
    z-index: var(--z-modal-backdrop);
  }

  .docs-main {
    margin-left: 0;
  }

  .docs-content {
    padding: var(--spacing-6) var(--spacing-4) var(--spacing-12);
  }
}
</style>

<!-- Unscoped styles for content within docs pages -->
<style>
.docs-content h1 {
  font-size: var(--font-size-3xl);
  font-weight: var(--font-weight-bold);
  color: var(--color-text-primary);
  margin: 0 0 var(--spacing-4);
  line-height: var(--line-height-tight);
}

.docs-content h2 {
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  margin: var(--spacing-10) 0 var(--spacing-4);
  padding-bottom: var(--spacing-2);
  border-bottom: 1px solid var(--color-border);
}

.docs-content h3 {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  margin: var(--spacing-8) 0 var(--spacing-3);
}

.docs-content p {
  color: var(--color-text-secondary);
  line-height: var(--line-height-relaxed);
  margin: 0 0 var(--spacing-4);
}

.docs-content ul,
.docs-content ol {
  color: var(--color-text-secondary);
  line-height: var(--line-height-relaxed);
  margin: 0 0 var(--spacing-4);
  padding-left: var(--spacing-6);
}

.docs-content li {
  margin-bottom: var(--spacing-2);
}

.docs-content a {
  color: var(--color-primary-600);
  text-decoration: none;
}

.docs-content a:hover {
  text-decoration: underline;
}

.docs-content code {
  font-family: var(--font-family-mono);
  font-size: var(--font-size-sm);
  background: var(--color-gray-100);
  color: var(--color-primary-700);
  padding: 0.15em 0.4em;
  border-radius: var(--radius-sm);
}

.docs-content pre {
  background: var(--color-gray-100);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-4);
  overflow-x: auto;
  margin: 0 0 var(--spacing-6);
}

.docs-content pre code {
  background: none;
  padding: 0;
  color: var(--color-text-primary);
  font-size: var(--font-size-sm);
  line-height: var(--line-height-relaxed);
}

.docs-content table {
  width: 100%;
  border-collapse: collapse;
  margin: 0 0 var(--spacing-6);
  font-size: var(--font-size-sm);
}

.docs-content th,
.docs-content td {
  text-align: left;
  padding: var(--spacing-3) var(--spacing-4);
  border-bottom: 1px solid var(--color-border);
}

.docs-content th {
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  background: var(--color-surface);
}

.docs-content td {
  color: var(--color-text-secondary);
}

.docs-content blockquote {
  border-left: 3px solid var(--color-primary-400);
  padding: var(--spacing-3) var(--spacing-4);
  margin: 0 0 var(--spacing-4);
  background: var(--color-primary-50);
  border-radius: 0 var(--radius-md) var(--radius-md) 0;
  color: var(--color-text-secondary);
}

.docs-content blockquote p {
  margin: 0;
}

.docs-content hr {
  border: none;
  border-top: 1px solid var(--color-border);
  margin: var(--spacing-8) 0;
}

/* Next page navigation */
.docs-next-page {
  margin-top: var(--spacing-12);
  padding-top: var(--spacing-6);
  border-top: 1px solid var(--color-border);
  display: flex;
  justify-content: flex-end;
}

.docs-next-page a {
  display: inline-flex;
  align-items: center;
  gap: var(--spacing-2);
  color: var(--color-primary-600);
  text-decoration: none;
  font-weight: var(--font-weight-medium);
  font-size: var(--font-size-sm);
  padding: var(--spacing-3) var(--spacing-4);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  transition: all var(--transition-fast);
}

.docs-next-page a:hover {
  border-color: var(--color-primary-400);
  background: var(--color-primary-50);
}
</style>
