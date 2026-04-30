<script setup lang="ts">
import UiButton from '~/components/ui/Button.vue'
import UiCard from '~/components/ui/Card.vue'
import UiInput from '~/components/ui/Input.vue'
import { projectsClient } from '~/api/projectsClient'
import type { Project } from '~/api/types'

definePageMeta({
  layout: 'app',
  middleware: ['admin'],
})

useSeoMeta({
  title: 'Settings - InterCopy',
})

const auth = useAuth()
const router = useRouter()

const orgName = ref('')
const isSaving = ref(false)

const projects = ref<Project[]>([])
const projectsLoading = ref(false)
const projectsError = ref('')
const copiedId = ref<string | null>(null)

async function loadProjects() {
  if (!auth.organization.value?.id) {
    projects.value = []
    return
  }
  projectsLoading.value = true
  projectsError.value = ''
  try {
    const data = await projectsClient.list(auth.organization.value.id)
    projects.value = Array.isArray(data) ? data : []
  } catch (err: any) {
    projectsError.value = err?.message || 'Failed to load projects'
    projects.value = []
  } finally {
    projectsLoading.value = false
  }
}

async function copyProjectId(id: string) {
  try {
    await navigator.clipboard.writeText(id)
    copiedId.value = id
    setTimeout(() => {
      if (copiedId.value === id) copiedId.value = null
    }, 1500)
  } catch {
    // ignore — browser clipboard not available
  }
}

onMounted(() => {
  if (auth.organization.value) {
    orgName.value = auth.organization.value.name
  }
  loadProjects()
})

async function handleSave() {
  isSaving.value = true
  await new Promise(resolve => setTimeout(resolve, 500))
  isSaving.value = false
}
</script>

<template>
  <div class="settings-page">
    <header class="page-header">
      <div>
        <h1>Settings</h1>
        <p class="page-subtitle">Manage your organization settings</p>
      </div>
    </header>

    <div class="settings-sections">
      <UiCard class="settings-section">
        <h2>Organization</h2>
        <p class="section-description">Basic information about your organization</p>
        
        <form class="settings-form" @submit.prevent="handleSave">
          <div class="form-group">
            <label for="orgName" class="label-with-hint">
              <span>Organization Name</span>
              <span class="label-hint">Your organization name</span>
            </label>
            <UiInput
              id="orgName"
              v-model="orgName"
              type="text"
            />
          </div>
          
          <div class="form-actions">
            <UiButton type="submit" :disabled="isSaving">
              {{ isSaving ? 'Saving...' : 'Save Changes' }}
            </UiButton>
          </div>
        </form>
      </UiCard>

      <UiCard class="settings-section">
        <h2>Integrations &amp; CI</h2>
        <p class="section-description">Manage machine access for export pulls and deployment pipelines</p>

        <div class="settings-link-card">
          <div>
            <h3>API Tokens</h3>
            <p>Create, rotate, extend, and revoke CI/CD tokens for export access.</p>
          </div>
          <NuxtLink to="/app/settings/api-tokens" class="settings-link-button">Manage tokens</NuxtLink>
        </div>

        <div class="ci-projects">
          <div class="ci-projects-header">
            <h3>Project IDs</h3>
            <span class="ci-required-badge">Required for CI/CD</span>
          </div>
          <p class="ci-projects-blurb">
            Your CI pipeline needs a project ID alongside an API token to call
            <code>GET /api/integration/exports/locales?projectId=&lt;id&gt;</code>.
            Copy the ID for the project you want to export and store it as a secret
            (e.g. <code>INTERCOPY_PROJECT_ID</code>) in your CI provider.
          </p>

          <div v-if="projectsLoading" class="ci-projects-state">Loading projects…</div>
          <div v-else-if="projectsError" class="ci-projects-state ci-projects-state--error">{{ projectsError }}</div>
          <p v-else-if="projects.length === 0" class="ci-projects-state">
            No projects yet — create one from
            <NuxtLink to="/app/projects">Projects</NuxtLink>.
          </p>
          <ul v-else class="ci-projects-list">
            <li v-for="project in projects" :key="project.id" class="ci-project-row">
              <div class="ci-project-name">{{ project.name }}</div>
              <code class="ci-project-id">{{ project.id }}</code>
              <button
                type="button"
                class="ci-copy-button"
                :class="{ 'ci-copy-button--copied': copiedId === project.id }"
                @click="copyProjectId(project.id)"
              >
                {{ copiedId === project.id ? 'Copied' : 'Copy ID' }}
              </button>
            </li>
          </ul>
        </div>
      </UiCard>

      <UiCard class="settings-section">
        <h2>Danger Zone</h2>
        <p class="section-description">Irreversible and destructive actions</p>
        
        <div class="danger-actions">
          <div class="danger-action">
            <div>
              <h3>Delete Organization</h3>
              <p>Permanently delete this organization and all its data</p>
            </div>
            <UiButton variant="danger">Delete</UiButton>
          </div>
        </div>
      </UiCard>
    </div>
  </div>
</template>

<style scoped>
.settings-page {
  max-width: 800px;
}

.page-header {
  margin-bottom: var(--spacing-6);
}

.page-header h1 {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  margin: 0 0 var(--spacing-1) 0;
}

.page-subtitle {
  color: var(--color-gray-500);
  margin: 0;
}

.settings-sections {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-6);
}

.settings-section {
  padding: var(--spacing-6);
}

.settings-section h2 {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  margin: 0 0 var(--spacing-1) 0;
}

.section-description {
  font-size: var(--font-size-sm);
  color: var(--color-gray-500);
  margin: 0 0 var(--spacing-5) 0;
}

.settings-form {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-4);
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-2);
}

.form-group label {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-primary);
}

.label-with-hint {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.label-hint {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-regular);
  color: var(--color-gray-500);
}

.form-actions {
  display: flex;
  justify-content: flex-start;
}

.settings-link-card {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--spacing-4);
  padding: var(--spacing-4);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  background: color-mix(in srgb, var(--color-primary-50) 25%, var(--color-surface));
}

.settings-link-card h3 {
  margin: 0 0 var(--spacing-1) 0;
  font-size: var(--font-size-base);
  color: var(--color-text-primary);
}

.settings-link-card p {
  margin: 0;
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
}

.settings-link-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-2) var(--spacing-4);
  border-radius: var(--radius-md);
  background: var(--color-primary-600);
  color: white;
  text-decoration: none;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
}

.settings-link-button:hover {
  background: var(--color-primary-700);
}

.ci-projects {
  margin-top: var(--spacing-6);
  padding-top: var(--spacing-5);
  border-top: 1px solid var(--color-border);
}

.ci-projects-header {
  display: flex;
  align-items: center;
  gap: var(--spacing-3);
  margin-bottom: var(--spacing-2);
}

.ci-projects-header h3 {
  margin: 0;
  font-size: var(--font-size-base);
  color: var(--color-text-primary);
}

.ci-required-badge {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  letter-spacing: 0.04em;
  text-transform: uppercase;
  padding: 2px var(--spacing-2);
  border-radius: var(--radius-md);
  background: color-mix(in srgb, var(--color-primary-600) 18%, var(--color-surface));
  color: var(--color-primary-700, var(--color-primary-600));
  border: 1px solid color-mix(in srgb, var(--color-primary-600) 35%, transparent);
}

.ci-projects-blurb {
  margin: 0 0 var(--spacing-4) 0;
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  line-height: 1.5;
}

.ci-projects-blurb code {
  font-family: var(--font-family-mono, ui-monospace, SFMono-Regular, Menlo, monospace);
  font-size: 0.85em;
  padding: 1px 6px;
  border-radius: var(--radius-sm);
  background: color-mix(in srgb, var(--color-surface) 60%, var(--color-border));
  color: var(--color-text-primary);
}

.ci-projects-state {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  padding: var(--spacing-3) 0;
}

.ci-projects-state--error {
  color: var(--color-error);
}

.ci-projects-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: var(--spacing-2);
}

.ci-project-row {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto auto;
  align-items: center;
  gap: var(--spacing-3);
  padding: var(--spacing-3) var(--spacing-4);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-surface);
}

.ci-project-name {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.ci-project-id {
  font-family: var(--font-family-mono, ui-monospace, SFMono-Regular, Menlo, monospace);
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
  padding: 4px 8px;
  border-radius: var(--radius-sm);
  background: color-mix(in srgb, var(--color-surface) 60%, var(--color-border));
  user-select: all;
}

.ci-copy-button {
  font-family: inherit;
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  padding: 6px var(--spacing-3);
  border-radius: var(--radius-md);
  border: 1px solid var(--color-border);
  background: var(--color-surface);
  color: var(--color-text-primary);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.ci-copy-button:hover {
  border-color: var(--color-primary-600);
  color: var(--color-primary-600);
}

.ci-copy-button--copied {
  background: var(--color-primary-600);
  color: var(--color-white, #fff);
  border-color: var(--color-primary-600);
}

.danger-actions {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-4);
}

.danger-action {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--spacing-4);
  background: color-mix(in srgb, var(--color-error) 12%, var(--color-surface));
  border: 1px solid color-mix(in srgb, var(--color-error) 45%, var(--color-border));
  border-radius: var(--radius-lg);
}

.danger-action h3 {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  margin: 0 0 var(--spacing-1) 0;
}

.danger-action p {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  margin: 0;
}
</style>
