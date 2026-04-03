<script setup lang="ts">
import AppEmptyState from '~/components/AppEmptyState.vue'
import AppSkeleton from '~/components/AppSkeleton.vue'
import UiSelect from '~/components/ui/Select.vue'
import { libraryClient } from '~/api/libraryClient'
import { projectsClient } from '~/api/projectsClient'
import type { LibraryComponent, Project } from '~/api/types'

definePageMeta({ layout: 'app' })
useSeoMeta({ title: 'Library - InterCopy' })

const auth = useAuth()
const router = useRouter()

const isLoading = ref(false)
const projects = ref<Array<{ id: string; name: string }>>([])
const selectedProjectId = ref('')
const components = ref<LibraryComponent[]>([])

function formatDate(dateStr: string): string {
  if (!dateStr) return ''
  const d = new Date(dateStr)
  return d.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })
}

function truncate(text: string, max: number): string {
  if (!text) return ''
  return text.length > max ? `${text.slice(0, max)}...` : text
}

function openComponent(comp: LibraryComponent) {
  router.push(`/app/library/${comp.id}`)
}

async function loadProjects() {
  if (!auth.organization.value?.id) return
  const data = await projectsClient.list(auth.organization.value.id)
  projects.value = (Array.isArray(data) ? data : []).map((p: Project) => ({ id: p.id, name: p.name }))

  if (!selectedProjectId.value && projects.value.length > 0) {
    selectedProjectId.value = projects.value[0]!.id
  }
}

async function loadComponents() {
  if (!selectedProjectId.value) {
    components.value = []
    return
  }

  isLoading.value = true
  try {
    const data = await libraryClient.list(selectedProjectId.value)
    components.value = Array.isArray(data) ? data : []
  } catch {
    components.value = []
  } finally {
    isLoading.value = false
  }
}

onMounted(async () => {
  await loadProjects()
  await loadComponents()
})

watch(selectedProjectId, async () => {
  await loadComponents()
})
</script>

<template>
  <div class="library-page">
    <header class="page-header">
      <div>
        <h1>Library</h1>
        <p class="page-subtitle">Reusable components synced from Figma with variant support</p>
      </div>
    </header>

    <div class="project-picker">
      <UiSelect
        id="libraryProjectSelect"
        v-model="selectedProjectId"
        label="Project"
        :options="[
          { value: '', label: 'Select project' },
          ...projects.map(project => ({ value: project.id, label: project.name }))
        ]"
      />
      <p class="label-hint">Select a project to view its library components</p>
    </div>

    <AppEmptyState
      v-if="projects.length === 0 && !isLoading"
      title="No projects available"
      description="Create a project first. Library components are associated with projects."
    />

    <template v-else-if="selectedProjectId">
      <div v-if="isLoading" class="library-grid">
        <div v-for="i in 6" :key="i" class="library-card library-card--skeleton">
          <AppSkeleton :lines="1" height="8rem" />
          <div class="library-card__body">
            <AppSkeleton :lines="2" height="1rem" />
          </div>
        </div>
      </div>

      <AppEmptyState
        v-else-if="components.length === 0"
        title="No library components yet"
        description="Library components will appear here once they are pushed from the Figma plugin."
      />

      <div v-else class="library-grid" role="list">
        <button
          v-for="comp in components"
          :key="comp.id"
          class="library-card"
          role="listitem"
          @click="openComponent(comp)"
        >
          <div class="library-card__thumbnail">
            <img
              v-if="comp.thumbnailUrl"
              :src="comp.thumbnailUrl"
              :alt="comp.name"
              class="library-card__image"
            >
            <div v-else class="library-card__placeholder">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                <rect x="3" y="3" width="18" height="18" rx="2" />
                <path d="M3 9h18M9 3v18" />
              </svg>
            </div>
          </div>
          <div class="library-card__body">
            <span class="library-card__name">{{ truncate(comp.name, 40) }}</span>
            <div class="library-card__meta">
              <span v-if="comp.variantCount != null" class="library-card__stat">
                {{ comp.variantCount }} variant{{ comp.variantCount !== 1 ? 's' : '' }}
              </span>
              <span v-if="comp.textFieldCount != null" class="library-card__stat">
                {{ comp.textFieldCount }} text field{{ comp.textFieldCount !== 1 ? 's' : '' }}
              </span>
            </div>
            <span class="library-card__date">{{ formatDate(comp.updatedUtc) }}</span>
          </div>
        </button>
      </div>
    </template>
  </div>
</template>

<style scoped>
.library-page {
  max-width: 1200px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-6);
}

.page-header h1 {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  margin: 0 0 var(--spacing-1) 0;
}

.page-subtitle {
  color: var(--color-text-muted);
  margin: 0;
}

.project-picker {
  margin-bottom: var(--spacing-5);
  display: flex;
  flex-direction: column;
  gap: var(--spacing-2);
  max-width: 420px;
}

.label-hint {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

/* Library grid */
.library-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: var(--spacing-4);
}

/* Library card */
.library-card {
  display: flex;
  flex-direction: column;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-xl);
  overflow: hidden;
  cursor: pointer;
  transition: box-shadow var(--transition-fast), border-color var(--transition-fast);
  text-align: left;
  font: inherit;
  color: inherit;
  padding: 0;
  width: 100%;
}

.library-card:hover {
  box-shadow: var(--shadow-md);
  border-color: var(--color-primary-300);
}

.library-card:focus-visible {
  outline: 2px solid var(--color-primary-500);
  outline-offset: 2px;
}

.library-card--skeleton {
  cursor: default;
  pointer-events: none;
}

/* Thumbnail area */
.library-card__thumbnail {
  width: 100%;
  height: 160px;
  background: color-mix(in srgb, var(--color-border) 30%, var(--color-background));
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

.library-card__image {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.library-card__placeholder {
  color: var(--color-text-muted);
  opacity: 0.4;
}

.library-card__placeholder svg {
  width: 48px;
  height: 48px;
}

/* Card body */
.library-card__body {
  padding: var(--spacing-4);
  display: flex;
  flex-direction: column;
  gap: var(--spacing-2);
}

.library-card__name {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  min-width: 0;
}

.library-card__meta {
  display: flex;
  align-items: center;
  gap: var(--spacing-3);
  font-size: var(--font-size-xs);
}

.library-card__stat {
  color: var(--color-text-secondary);
}

.library-card__date {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

@media (max-width: 640px) {
  .library-grid {
    grid-template-columns: 1fr;
  }
}
</style>
