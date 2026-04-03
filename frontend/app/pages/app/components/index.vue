<script setup lang="ts">
import AppEmptyState from '~/components/AppEmptyState.vue'
import AppSkeleton from '~/components/AppSkeleton.vue'
import UiButton from '~/components/ui/Button.vue'
import UiSelect from '~/components/ui/Select.vue'
import { componentsClient } from '~/api/componentsClient'
import { projectsClient } from '~/api/projectsClient'
import type { DesignComponent, Project } from '~/api/types'

definePageMeta({ layout: 'app' })
useSeoMeta({ title: 'Components - InterCopy' })

const auth = useAuth()
const router = useRouter()

const isLoading = ref(false)
const projects = ref<Array<{ id: string; name: string }>>([])
const selectedProjectId = ref('')
const components = ref<DesignComponent[]>([])

function statusBadgeClass(status: string): string {
  switch (status) {
    case 'approved':
      return 'badge--approved'
    case 'in_review':
      return 'badge--review'
    case 'draft':
    default:
      return 'badge--draft'
  }
}

function formatDate(dateStr: string): string {
  if (!dateStr) return ''
  const d = new Date(dateStr)
  return d.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })
}

function truncate(text: string, max: number): string {
  if (!text) return ''
  return text.length > max ? `${text.slice(0, max)}...` : text
}

function openComponent(comp: DesignComponent) {
  router.push(`/app/components/${comp.id}?projectId=${encodeURIComponent(selectedProjectId.value)}`)
}

// Delete
const deleteTarget = ref<DesignComponent | null>(null)
const showDeleteConfirm = ref(false)
const deleteError = ref('')

function openDeleteConfirm(comp: DesignComponent, event: Event) {
  event.stopPropagation()
  deleteTarget.value = comp
  deleteError.value = ''
  showDeleteConfirm.value = true
}

async function confirmDelete() {
  if (!deleteTarget.value || !selectedProjectId.value) return
  deleteError.value = ''
  try {
    await componentsClient.delete(selectedProjectId.value, deleteTarget.value.id)
    showDeleteConfirm.value = false
    deleteTarget.value = null
    await loadComponents()
  } catch (err: any) {
    deleteError.value = err?.message ?? 'Failed to delete component'
  }
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
    const data = await componentsClient.list(selectedProjectId.value)
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
  <div class="components-page">
    <header class="page-header">
      <div>
        <h1>Components</h1>
        <p class="page-subtitle">Design components synced from Figma</p>
      </div>
    </header>

    <div class="project-picker">
      <UiSelect
        id="componentProjectSelect"
        v-model="selectedProjectId"
        label="Project"
        :options="[
          { value: '', label: 'Select project' },
          ...projects.map(project => ({ value: project.id, label: project.name }))
        ]"
      />
      <p class="label-hint">Select a project to view its design components</p>
    </div>

    <AppEmptyState
      v-if="projects.length === 0 && !isLoading"
      title="No projects available"
      description="Create a project first. Components are associated with projects."
    />

    <template v-else-if="selectedProjectId">
      <div v-if="isLoading" class="component-grid">
        <div v-for="i in 6" :key="i" class="component-card component-card--skeleton">
          <AppSkeleton :lines="1" height="8rem" />
          <div class="component-card__body">
            <AppSkeleton :lines="2" height="1rem" />
          </div>
        </div>
      </div>

      <AppEmptyState
        v-else-if="components.length === 0"
        title="No components yet"
        description="Design components will appear here once they are pushed from the Figma plugin."
      />

      <div v-else class="component-grid" role="list">
        <button
          v-for="comp in components"
          :key="comp.id"
          class="component-card"
          role="listitem"
          @click="openComponent(comp)"
        >
          <div class="component-card__thumbnail">
            <img
              v-if="comp.thumbnailUrl"
              :src="comp.thumbnailUrl"
              :alt="comp.figmaFrameName"
              class="component-card__image"
            >
            <div v-else class="component-card__placeholder">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                <rect x="3" y="3" width="18" height="18" rx="2" />
                <path d="M3 16l5-5 4 4 4-6 5 7" />
              </svg>
            </div>
          </div>
          <div class="component-card__body">
            <div class="component-card__header">
              <span class="component-card__name">{{ truncate(comp.figmaFrameName, 40) }}</span>
              <span class="component-card__badge" :class="statusBadgeClass(comp.status)">
                {{ comp.status }}
              </span>
            </div>
            <div class="component-card__meta">
              <span v-if="comp.textFieldCount != null" class="component-card__fields">
                {{ comp.textFieldCount }} text field{{ comp.textFieldCount !== 1 ? 's' : '' }}
              </span>
              <span class="component-card__date">{{ formatDate(comp.updatedUtc) }}</span>
            </div>
            <button class="component-card__delete" title="Delete component" @click="openDeleteConfirm(comp, $event)">
              <svg viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd" /></svg>
            </button>
          </div>
        </button>
      </div>
    </template>

    <!-- Delete confirmation modal -->
    <div v-if="showDeleteConfirm" class="delete-overlay" @click.self="showDeleteConfirm = false">
      <div class="delete-modal">
        <h2>Delete component</h2>
        <p class="delete-text">Delete <strong>{{ deleteTarget?.figmaFrameName }}</strong>? This will remove the component and all its text fields from InterCopy. This cannot be undone.</p>
        <p v-if="deleteError" class="delete-error">{{ deleteError }}</p>
        <div class="delete-actions">
          <UiButton variant="secondary" size="sm" @click="showDeleteConfirm = false">Cancel</UiButton>
          <UiButton variant="danger" size="sm" @click="confirmDelete">Delete</UiButton>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.components-page {
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

/* Component grid */
.component-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: var(--spacing-4);
}

/* Component card */
.component-card {
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

.component-card:hover {
  box-shadow: var(--shadow-md);
  border-color: var(--color-primary-300);
}

.component-card:focus-visible {
  outline: 2px solid var(--color-primary-500);
  outline-offset: 2px;
}

.component-card--skeleton {
  cursor: default;
  pointer-events: none;
}

/* Thumbnail area */
.component-card__thumbnail {
  width: 100%;
  height: 160px;
  background: color-mix(in srgb, var(--color-border) 30%, var(--color-background));
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

.component-card__image {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.component-card__placeholder {
  color: var(--color-text-muted);
  opacity: 0.4;
}

.component-card__placeholder svg {
  width: 48px;
  height: 48px;
}

/* Card body */
.component-card__body {
  padding: var(--spacing-4);
  display: flex;
  flex-direction: column;
  gap: var(--spacing-2);
}

.component-card__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--spacing-2);
}

.component-card__name {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  min-width: 0;
}

.component-card__badge {
  flex-shrink: 0;
  padding: 2px var(--spacing-2);
  border-radius: var(--radius-md);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  text-transform: capitalize;
}

.badge--approved {
  background: color-mix(in srgb, #22c55e 15%, transparent);
  color: #16a34a;
}

.badge--review {
  background: color-mix(in srgb, #eab308 15%, transparent);
  color: #a16207;
}

.badge--draft {
  background: color-mix(in srgb, #6366f1 12%, transparent);
  color: #6366f1;
}

.component-card__meta {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--spacing-2);
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

.component-card__fields {
  color: var(--color-text-secondary);
}

@media (max-width: 640px) {
  .component-grid {
    grid-template-columns: 1fr;
  }
}

/* Delete button on card */
.component-card__delete {
  position: absolute;
  top: var(--spacing-2);
  right: var(--spacing-2);
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  padding: 0;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-surface);
  color: var(--color-text-muted);
  cursor: pointer;
  opacity: 0;
  transition: opacity var(--transition-fast), background var(--transition-fast), color var(--transition-fast), border-color var(--transition-fast);
  z-index: 1;
}
.component-card:hover .component-card__delete { opacity: 1; }
.component-card__delete:hover {
  background: color-mix(in srgb, #ef4444 12%, var(--color-surface));
  color: #ef4444;
  border-color: #ef4444;
}
.component-card__delete svg { width: 14px; height: 14px; }
.component-card { position: relative; }

/* Delete confirmation modal */
.delete-overlay { position: fixed; inset: 0; background: color-mix(in srgb, var(--color-black) 45%, transparent); display: grid; place-items: center; z-index: var(--z-modal); }
.delete-modal { width: min(480px, 92vw); background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-xl); padding: var(--spacing-6); display: flex; flex-direction: column; gap: var(--spacing-3); }
.delete-modal h2 { margin: 0; color: var(--color-text-primary); font-size: var(--font-size-lg); }
.delete-text { margin: 0; font-size: var(--font-size-sm); color: var(--color-text-secondary); line-height: 1.6; }
.delete-text strong { color: var(--color-text-primary); }
.delete-error { margin: 0; color: #ef4444; font-size: var(--font-size-xs); }
.delete-actions { display: flex; justify-content: flex-end; gap: var(--spacing-2); }
</style>
