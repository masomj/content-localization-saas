<script setup lang="ts">
import AppEmptyState from '~/components/AppEmptyState.vue'
import AppSkeleton from '~/components/AppSkeleton.vue'
import UiButton from '~/components/ui/Button.vue'
import UiSelect from '~/components/ui/Select.vue'

definePageMeta({ layout: 'app' })
useSeoMeta({ title: 'Content - LocFlow' })

const auth = useAuth()
const isLoading = ref(false)
const projects = ref<Array<{ id: string; name: string }>>([])
const selectedProjectId = ref('')
const contents = ref<Array<{ id: string; key: string; source: string; status: string }>>([])

const showAddContentForm = ref(false)
const newContentKey = ref('')
const newContentSource = ref('')
const addContentError = ref('')

function getApiBaseUrl() {
  if (typeof window === 'undefined') return '/api'
  return (window as any).__NUXT__?.config?.public?.apiBase || '/api'
}

async function loadProjects() {
  if (!auth.organization.value?.id) return
  const token = localStorage.getItem('locflow_auth_token')
  const apiBase = getApiBaseUrl()
  const res = await fetch(`${apiBase}/projects?workspaceId=${encodeURIComponent(auth.organization.value.id)}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  })
  if (!res.ok) return

  const data = await res.json()
  projects.value = (Array.isArray(data) ? data : []).map((p: any) => ({ id: p.id, name: p.name }))

  if (!selectedProjectId.value && projects.value.length > 0) {
    selectedProjectId.value = projects.value[0]!.id
  }
}

async function loadContent() {
  if (!selectedProjectId.value) {
    contents.value = []
    return
  }

  isLoading.value = true
  try {
    const token = localStorage.getItem('locflow_auth_token')
    const apiBase = getApiBaseUrl()
    const res = await fetch(`${apiBase}/content-items?projectId=${encodeURIComponent(selectedProjectId.value)}`, {
      headers: token ? { Authorization: `Bearer ${token}` } : {},
    })
    if (!res.ok) throw new Error('Failed to load content')
    const data = await res.json()
    contents.value = (Array.isArray(data) ? data : []).map((c: any) => ({
      id: c.id,
      key: c.key,
      source: c.source,
      status: c.status,
    }))
  } catch {
    contents.value = []
  } finally {
    isLoading.value = false
  }
}

function openAddContentForm() {
  addContentError.value = ''
  showAddContentForm.value = true
}

function closeAddContentForm() {
  showAddContentForm.value = false
  newContentKey.value = ''
  newContentSource.value = ''
  addContentError.value = ''
}

async function addContent() {
  const key = newContentKey.value.trim()
  const source = newContentSource.value.trim()

  if (!selectedProjectId.value) {
    addContentError.value = 'Please select a project first'
    return
  }

  if (!key || !source) {
    addContentError.value = 'Key and source text are required'
    return
  }

  try {
    const token = localStorage.getItem('locflow_auth_token')
    const apiBase = getApiBaseUrl()
    const res = await fetch(`${apiBase}/content-items`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: JSON.stringify({
        projectId: selectedProjectId.value,
        key,
        source,
        status: 'draft',
        tags: [],
        context: null,
        notes: null,
      }),
    })

    const body = await res.json().catch(() => ({}))
    if (!res.ok) throw new Error(body.error || body.errors?.join(', ') || 'Failed to add content')

    await loadContent()
    closeAddContentForm()
  } catch (error: any) {
    addContentError.value = error?.message || 'Failed to add content'
  }
}

onMounted(async () => {
  await loadProjects()
  await loadContent()
})

watch(selectedProjectId, async () => {
  await loadContent()
})
</script>

<template>
  <div class="content-page">
    <header class="page-header">
      <div>
        <h1>Content</h1>
        <p class="page-subtitle">Content is attached to a project</p>
      </div>
      <UiButton :disabled="!selectedProjectId" @click="openAddContentForm">
        <svg viewBox="0 0 20 20" fill="currentColor" class="btn-icon">
          <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
        </svg>
        Add Content
      </UiButton>
    </header>

    <div class="project-picker">
      <UiSelect
        id="projectSelect"
        v-model="selectedProjectId"
        label="Project"
        :options="[
          { value: '', label: 'Select project' },
          ...projects.map(project => ({ value: project.id, label: project.name }))
        ]"
      />
      <p class="label-hint">Select a project to view/manage content</p>
    </div>

    <AppEmptyState
      v-if="projects.length === 0"
      title="No projects available"
      description="Create a project first. Content cannot exist without a project."
    />

    <template v-else>
      <div v-if="isLoading" class="content-list">
        <div v-for="i in 3" :key="i" class="content-item"><AppSkeleton lines="2" height="1rem" /></div>
      </div>

      <AppEmptyState
        v-else-if="contents.length === 0"
        title="No content in this project"
        description="Add content items linked to this project"
      >
        <template #action>
          <UiButton @click="openAddContentForm">Add Content</UiButton>
        </template>
      </AppEmptyState>

      <div v-else class="content-list">
        <div v-for="item in contents" :key="item.id" class="content-item">
          <h3>{{ item.key }}</h3>
          <p>{{ item.source }}</p>
          <small>Status: {{ item.status }}</small>
        </div>
      </div>
    </template>

    <div v-if="showAddContentForm" class="content-form-overlay" @click.self="closeAddContentForm">
      <form class="content-form" @submit.prevent="addContent">
        <h2>Add content item</h2>

        <label for="contentKey" class="label-with-hint">
          <span>Key</span>
          <span class="label-hint">example: auth.login.title</span>
        </label>
        <input id="contentKey" v-model="newContentKey" type="text" autocomplete="off">

        <label for="contentSource" class="label-with-hint">
          <span>Source text</span>
          <span class="label-hint">The default text in source language</span>
        </label>
        <textarea id="contentSource" v-model="newContentSource" rows="4" />

        <p v-if="addContentError" class="field-error">{{ addContentError }}</p>

        <div class="content-form-actions">
          <UiButton type="button" variant="secondary" @click="closeAddContentForm">Cancel</UiButton>
          <UiButton type="submit">Add content</UiButton>
        </div>
      </form>
    </div>
  </div>
</template>

<style scoped>
.content-page { max-width: 1200px; }
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--spacing-6); }
.page-header h1 { font-size: var(--font-size-2xl); font-weight: var(--font-weight-semibold); color: var(--color-text-primary); margin: 0 0 var(--spacing-1) 0; }
.page-subtitle { color: var(--color-text-muted); margin: 0; }
.btn-icon { width: 1.25em; height: 1.25em; margin-right: var(--spacing-2); }
.project-picker { margin-bottom: var(--spacing-5); display: flex; flex-direction: column; gap: var(--spacing-2); max-width: 420px; }
.label-with-hint { display: flex; flex-direction: column; gap: 2px; color: var(--color-text-primary); }
.label-hint { font-size: var(--font-size-xs); color: var(--color-text-muted); }
.content-list { display: flex; flex-direction: column; gap: var(--spacing-3); }
.content-item { background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-lg); padding: var(--spacing-4); }
.content-item h3 { margin: 0 0 var(--spacing-1); }
.content-item p { margin: 0 0 var(--spacing-2); }
.content-form-overlay { position: fixed; inset: 0; background: color-mix(in srgb, var(--color-black) 45%, transparent); display: grid; place-items: center; z-index: var(--z-modal); }
.content-form { width: min(560px, 92vw); background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-xl); padding: var(--spacing-6); display: flex; flex-direction: column; gap: var(--spacing-3); }
.content-form h2 { margin: 0 0 var(--spacing-2); color: var(--color-text-primary); }
.content-form input,.content-form textarea { padding: var(--spacing-3) var(--spacing-4); border: 1px solid var(--color-border); border-radius: var(--radius-lg); background: var(--color-background); color: var(--color-text-primary); }
.field-error { margin: 0; color: var(--color-error); font-size: var(--font-size-xs); }
.content-form-actions { display: flex; justify-content: flex-end; gap: var(--spacing-2); }
</style>
