<script setup lang="ts">
import AppEmptyState from '~/components/AppEmptyState.vue'
import AppSkeleton from '~/components/AppSkeleton.vue'
import UiButton from '~/components/ui/Button.vue'
import { projectsClient } from '~/api/projectsClient'
import type { Project } from '~/api/types'

definePageMeta({ layout: 'app' })
useSeoMeta({ title: 'Projects - LocFlow' })

// Redirect non-admins away from projects page
const auth = useAuth()
if (import.meta.client && !auth.isLoading.value && !auth.isAdmin.value) {
  navigateTo('/app/content')
}

const isLoading = ref(false)
const projects = ref<Project[]>([])

const showCreateProjectForm = ref(false)
const newProjectName = ref('')
const newProjectDescription = ref('')
const createProjectError = ref('')

async function loadProjects() {
  if (!auth.organization.value?.id) {
    projects.value = []
    return
  }

  isLoading.value = true
  try {
    const data = await projectsClient.list(auth.organization.value.id)
    projects.value = Array.isArray(data) ? data : []
  } catch {
    projects.value = []
  } finally {
    isLoading.value = false
  }
}

function openCreateProjectForm() {
  showCreateProjectForm.value = true
  createProjectError.value = ''
}

function closeCreateProjectForm() {
  showCreateProjectForm.value = false
  newProjectName.value = ''
  newProjectDescription.value = ''
  createProjectError.value = ''
}

async function createProject() {
  const name = newProjectName.value.trim()
  if (!name) {
    createProjectError.value = 'Project name is required'
    return
  }

  if (!auth.organization.value?.id) {
    createProjectError.value = 'No workspace is available for this account'
    return
  }

  try {
    await projectsClient.create({
      workspaceId: auth.organization.value.id,
      name,
      sourceLanguage: 'en',
      description: newProjectDescription.value.trim(),
    })

    await loadProjects()
    closeCreateProjectForm()
  } catch (error: any) {
    createProjectError.value = error?.message || 'Failed to create project'
  }
}

function formatDate(dateStr?: string): string {
  if (!dateStr) return ''
  try {
    return new Date(dateStr).toLocaleDateString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    })
  } catch {
    return ''
  }
}

function truncateDescription(text?: string, max = 120): string {
  if (!text) return ''
  return text.length > max ? `${text.slice(0, max)}...` : text
}

onMounted(loadProjects)
</script>

<template>
  <div class="projects-page">
    <header class="page-header">
      <div>
        <h1>Projects</h1>
        <p class="page-subtitle">Manage your translation projects</p>
      </div>
      <div class="page-header-right">
        <UiButton v-if="auth.isAdmin.value" @click="openCreateProjectForm">New Project</UiButton>
        <p v-else class="admin-hint">Contact an admin to create projects</p>
      </div>
    </header>

    <div v-if="isLoading" class="projects-grid">
      <div v-for="i in 3" :key="i" class="project-card"><AppSkeleton lines="3" height="1.5rem" /></div>
    </div>

    <AppEmptyState
      v-else-if="projects.length === 0"
      title="No projects yet"
      description="Create your first project to start managing translations"
    >
      <template #action>
        <UiButton v-if="auth.isAdmin.value" @click="openCreateProjectForm">Create Project</UiButton>
        <p v-else class="admin-hint">Contact an admin to create projects</p>
      </template>
    </AppEmptyState>

    <div v-else class="projects-grid">
      <div v-for="project in projects" :key="project.id" class="project-card">
        <div class="project-card-body">
          <h3 class="project-card-name">{{ project.name }}</h3>
          <p v-if="project.description" class="project-card-description">
            {{ truncateDescription(project.description) }}
          </p>
          <p v-else class="project-card-description project-card-description--empty">
            No description
          </p>
          <div class="project-card-meta">
            <span v-if="project.sourceLanguage" class="source-lang-badge">
              {{ project.sourceLanguage }}
            </span>
            <span v-if="project.createdUtc" class="project-card-date">
              {{ formatDate(project.createdUtc) }}
            </span>
          </div>
        </div>
        <div class="project-card-actions">
          <NuxtLink :to="`/app/projects/${project.id}/versions`" class="project-card-link">
            Releases
          </NuxtLink>
          <span class="project-card-link project-card-link--disabled">
            Settings
          </span>
        </div>
      </div>
    </div>

    <!-- Create project modal -->
    <div v-if="showCreateProjectForm" class="project-form-overlay" @click.self="closeCreateProjectForm">
      <form class="project-form" @submit.prevent="createProject">
        <h2>Create project</h2>

        <label for="projectName" class="label-with-hint">
          <span>Project name</span>
          <span class="label-hint">Choose a clear internal name</span>
        </label>
        <input id="projectName" v-model="newProjectName" type="text" autocomplete="off">

        <label for="projectDescription" class="label-with-hint">
          <span>Description</span>
          <span class="label-hint">Describe what this project is for</span>
        </label>
        <textarea
          id="projectDescription"
          v-model="newProjectDescription"
          rows="3"
          autocomplete="off"
        />

        <p v-if="createProjectError" class="field-error">{{ createProjectError }}</p>

        <div class="project-form-actions">
          <UiButton type="button" variant="secondary" @click="closeCreateProjectForm">Cancel</UiButton>
          <UiButton type="submit">Create project</UiButton>
        </div>
      </form>
    </div>
  </div>
</template>

<style scoped>
.projects-page { max-width: 1200px; }
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--spacing-6); }
.page-header-right { display: flex; align-items: center; }
.admin-hint { margin: 0; font-size: var(--font-size-sm); color: var(--color-text-muted); font-style: italic; }
.page-subtitle { color: var(--color-text-muted); margin: 0; }

.projects-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: var(--spacing-4);
}

.project-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  transition: border-color var(--transition-fast);
}
.project-card:hover {
  border-color: var(--color-primary-600);
}

.project-card-body {
  padding: var(--spacing-5);
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: var(--spacing-2);
}

.project-card-name {
  margin: 0;
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
}

.project-card-description {
  margin: 0;
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
  line-height: 1.5;
  display: -webkit-box;
  -webkit-line-clamp: 3;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.project-card-description--empty {
  font-style: italic;
  opacity: 0.6;
}

.project-card-meta {
  display: flex;
  align-items: center;
  gap: var(--spacing-3);
  margin-top: var(--spacing-2);
}

.source-lang-badge {
  display: inline-flex;
  align-items: center;
  padding: 2px var(--spacing-2);
  border-radius: var(--radius-md);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  text-transform: uppercase;
  background: color-mix(in srgb, var(--color-primary-600) 12%, transparent);
  color: var(--color-primary-600);
}

.project-card-date {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

.project-card-actions {
  display: flex;
  border-top: 1px solid var(--color-border);
}

.project-card-link {
  flex: 1;
  display: block;
  padding: var(--spacing-2) var(--spacing-4);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  color: var(--color-primary-600);
  text-decoration: none;
  text-align: center;
  transition: background var(--transition-fast);
}
.project-card-link:hover {
  background: color-mix(in srgb, var(--color-primary-600) 5%, transparent);
}
.project-card-link + .project-card-link {
  border-left: 1px solid var(--color-border);
}
.project-card-link--disabled {
  color: var(--color-text-muted);
  cursor: default;
  opacity: 0.5;
}
.project-card-link--disabled:hover {
  background: none;
}

/* Modal */
.label-with-hint { display: flex; flex-direction: column; gap: 2px; color: var(--color-text-primary); }
.label-hint { font-size: var(--font-size-xs); color: var(--color-text-muted); margin-bottom: var(--spacing-2); }
.field-error { color: var(--color-error); font-size: var(--font-size-xs); margin: 0; }
.project-form-overlay { position: fixed; inset: 0; background: color-mix(in srgb, var(--color-black) 45%, transparent); display: grid; place-items: center; z-index: var(--z-modal); }
.project-form { width: min(480px, 92vw); background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-xl); padding: var(--spacing-6); display: flex; flex-direction: column; gap: var(--spacing-3); }
.project-form h2 { margin: 0 0 var(--spacing-2); color: var(--color-text-primary); }
.project-form input,
.project-form textarea {
  padding: var(--spacing-3);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-background);
  color: var(--color-text-primary);
  font: inherit;
  resize: vertical;
}
.project-form-actions { display: flex; justify-content: flex-end; gap: var(--spacing-2); }
</style>
