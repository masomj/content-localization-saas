<script setup lang="ts">
import AppEmptyState from '~/components/AppEmptyState.vue'
import AppSkeleton from '~/components/AppSkeleton.vue'
import UiButton from '~/components/ui/Button.vue'

definePageMeta({
  layout: 'app',
})

useSeoMeta({
  title: 'Projects - LocFlow',
})

const isLoading = ref(false)
const projects = ref<Array<{ id: string; name: string; status: string; progress: number; languages: number }>>([])
const showCreateProjectForm = ref(false)
const newProjectName = ref('')
const createProjectError = ref('')

function openCreateProjectForm() {
  showCreateProjectForm.value = true
  createProjectError.value = ''
}

function closeCreateProjectForm() {
  showCreateProjectForm.value = false
  newProjectName.value = ''
  createProjectError.value = ''
}

function createProject() {
  const name = newProjectName.value.trim()
  if (!name) {
    createProjectError.value = 'Project name is required'
    return
  }

  projects.value.unshift({
    id: `project_${Date.now()}`,
    name,
    status: 'Draft',
    progress: 0,
    languages: 0,
  })

  closeCreateProjectForm()
}

onMounted(async () => {
  isLoading.value = true
  await new Promise(resolve => setTimeout(resolve, 500))
  projects.value = []
  isLoading.value = false
})
</script>

<template>
  <div class="projects-page">
    <header class="page-header">
      <div>
        <h1>Projects</h1>
        <p class="page-subtitle">Manage your translation projects</p>
      </div>
      <UiButton @click="openCreateProjectForm">
        <svg viewBox="0 0 20 20" fill="currentColor" class="btn-icon">
          <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
        </svg>
        New Project
      </UiButton>
    </header>

    <div v-if="isLoading" class="projects-grid">
      <div v-for="i in 3" :key="i" class="project-card">
        <AppSkeleton lines="3" height="1.5rem" />
      </div>
    </div>

    <AppEmptyState
      v-else-if="projects.length === 0"
      title="No projects yet"
      description="Create your first project to start managing translations"
    >
      <template #action>
        <UiButton @click="openCreateProjectForm">Create Project</UiButton>
      </template>
    </AppEmptyState>

    <div v-else class="projects-grid">
      <div v-for="project in projects" :key="project.id" class="project-card">
        <h3>{{ project.name }}</h3>
        <p>Status: {{ project.status }}</p>
      </div>
    </div>

    <div v-if="showCreateProjectForm" class="project-form-overlay" @click.self="closeCreateProjectForm">
      <form class="project-form" @submit.prevent="createProject">
        <h2>Create project</h2>
        <label for="projectName" class="label-with-hint">
          <span>Project name</span>
          <span class="label-hint">Choose a clear internal name</span>
        </label>
        <input id="projectName" v-model="newProjectName" type="text" autocomplete="off">
        <p v-if="createProjectError" class="field-error">{{ createProjectError }}</p>
        <div class="project-form-actions">
          <button type="button" class="btn-secondary" @click="closeCreateProjectForm">Cancel</button>
          <button type="submit" class="btn-primary">Create project</button>
        </div>
      </form>
    </div>
  </div>
</template>

<style scoped>
.projects-page {
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

.btn-icon {
  width: 1.25em;
  height: 1.25em;
  margin-right: var(--spacing-2);
}

.projects-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: var(--spacing-4);
}

.project-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-4);
}

.project-form-overlay {
  position: fixed;
  inset: 0;
  background: color-mix(in srgb, var(--color-black) 45%, transparent);
  display: grid;
  place-items: center;
  z-index: var(--z-modal);
}

.project-form {
  width: min(480px, 92vw);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-xl);
  padding: var(--spacing-6);
  display: flex;
  flex-direction: column;
  gap: var(--spacing-3);
}

.project-form h2 {
  margin: 0 0 var(--spacing-2);
  color: var(--color-text-primary);
}

.label-with-hint {
  display: flex;
  flex-direction: column;
  gap: 2px;
  color: var(--color-text-primary);
}

.label-hint {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

.project-form input {
  padding: var(--spacing-3) var(--spacing-4);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  background: var(--color-background);
  color: var(--color-text-primary);
}

.field-error {
  margin: 0;
  color: var(--color-error);
  font-size: var(--font-size-xs);
}

.project-form-actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-2);
}

.btn-secondary,
.btn-primary {
  border-radius: var(--radius-md);
  padding: var(--spacing-2) var(--spacing-3);
  border: 1px solid var(--color-border);
  cursor: pointer;
}

.btn-secondary {
  background: var(--color-surface);
  color: var(--color-text-secondary);
}

.btn-primary {
  background: var(--color-primary-600);
  color: var(--color-white);
  border-color: var(--color-primary-600);
}
</style>
