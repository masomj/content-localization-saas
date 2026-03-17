<script setup lang="ts">
import AppEmptyState from '~/components/AppEmptyState.vue'
import AppSkeleton from '~/components/AppSkeleton.vue'
import UiButton from '~/components/ui/Button.vue'
import UiSelect from '~/components/ui/Select.vue'

definePageMeta({ layout: 'app' })
useSeoMeta({ title: 'Projects - LocFlow' })

type Project = { id: string; name: string; status: string; progress: number; languages: number }
type Collection = { id: string; projectId: string; parentId: string | null; name: string; isRoot: boolean; depth: number; sortOrder: number }

type TreeNode = Collection & { children: TreeNode[] }

const auth = useAuth()
const isLoading = ref(false)
const projects = ref<Project[]>([])
const selectedProjectId = ref<string>('')
const collections = ref<Collection[]>([])
const isLoadingCollections = ref(false)
const newCollectionName = ref('')
const newCollectionParentId = ref<string>('')
const collectionError = ref('')
const draggingId = ref('')

const showCreateProjectForm = ref(false)
const newProjectName = ref('')
const createProjectError = ref('')

function getApiBaseUrl() {
  if (typeof window === 'undefined') return '/api'
  return (window as any).__NUXT__?.config?.public?.apiBase || '/api'
}

function decodeJwtExp(token: string | null): number {
  if (!token) return 0
  try {
    const payload = JSON.parse(atob(token.split('.')[1] || ''))
    return Number(payload?.exp || 0)
  } catch {
    return 0
  }
}

function getAuthToken() {
  if (typeof window === 'undefined') return null

  const localToken = localStorage.getItem('locflow_auth_token')
  const sessionToken = sessionStorage.getItem('locflow_auth_token')

  const localExp = decodeJwtExp(localToken)
  const sessionExp = decodeJwtExp(sessionToken)
  const now = Math.floor(Date.now() / 1000)

  const localValid = localToken && localExp > now
  const sessionValid = sessionToken && sessionExp > now

  if (localValid && sessionValid) {
    return localExp >= sessionExp ? localToken : sessionToken
  }

  if (sessionValid) return sessionToken
  if (localValid) return localToken

  return sessionToken || localToken
}

function authHeaders() {
  const token = getAuthToken()
  return token
    ? { Authorization: `Bearer ${token}`, 'X-Debug-Auth-Present': '1' }
    : { 'X-Debug-Auth-Present': '0' }
}

const collectionTree = computed<TreeNode[]>(() => {
  const byParent = new Map<string | null, Collection[]>()
  for (const item of collections.value) {
    const key = item.parentId ?? null
    const arr = byParent.get(key) || []
    arr.push(item)
    byParent.set(key, arr)
  }

  const build = (parentId: string | null): TreeNode[] => {
    const rows = (byParent.get(parentId) || []).sort((a, b) => a.sortOrder - b.sortOrder)
    return rows.map(row => ({ ...row, children: build(row.id) }))
  }

  return build(null)
})

const rootCollection = computed(() => collections.value.find(x => x.isRoot) || null)
const selectableParents = computed(() => collections.value.filter(x => x.depth < 2))

async function loadProjects() {
  if (!auth.organization.value?.id) {
    projects.value = []
    return
  }

  isLoading.value = true
  try {
    const apiBase = getApiBaseUrl()
    const res = await fetch(`${apiBase}/projects?workspaceId=${encodeURIComponent(auth.organization.value.id)}`, {
      headers: authHeaders(),
    })

    if (!res.ok) throw new Error('Failed to load projects')

    const data = await res.json()
    projects.value = (Array.isArray(data) ? data : []).map((p: any) => ({
      id: p.id,
      name: p.name,
      status: p.status ?? 'Draft',
      progress: 0,
      languages: 0,
    }))

    if (!selectedProjectId.value && projects.value.length > 0) {
      selectedProjectId.value = projects.value[0].id
      await loadCollections(selectedProjectId.value)
    }
  } catch {
    projects.value = []
  } finally {
    isLoading.value = false
  }
}

async function loadCollections(projectId: string) {
  if (!projectId) return
  isLoadingCollections.value = true
  collectionError.value = ''

  try {
    const apiBase = getApiBaseUrl()
    const res = await fetch(`${apiBase}/projects/${projectId}/collections`, { headers: authHeaders() })
    if (!res.ok) throw new Error('Failed to load collections')

    const data = await res.json()
    collections.value = Array.isArray(data) ? data : []
    newCollectionParentId.value = rootCollection.value?.id || ''
  } catch (error: any) {
    collectionError.value = error?.message || 'Failed to load collections'
    collections.value = []
  } finally {
    isLoadingCollections.value = false
  }
}

async function createCollection() {
  const name = newCollectionName.value.trim()
  if (!name || !selectedProjectId.value) return

  try {
    const apiBase = getApiBaseUrl()
    const res = await fetch(`${apiBase}/projects/${selectedProjectId.value}/collections`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', ...authHeaders() },
      body: JSON.stringify({ name, parentId: newCollectionParentId.value || null }),
    })
    const body = await res.json().catch(() => ({}))
    if (!res.ok) throw new Error(body.error || body.errors?.name?.[0] || 'Failed to create collection')

    newCollectionName.value = ''
    await loadCollections(selectedProjectId.value)
  } catch (error: any) {
    collectionError.value = error?.message || 'Failed to create collection'
  }
}

async function renameCollection(item: Collection) {
  if (!selectedProjectId.value || item.isRoot) return
  const nextName = window.prompt('Rename folder', item.name)?.trim()
  if (!nextName || nextName === item.name) return

  try {
    const apiBase = getApiBaseUrl()
    const res = await fetch(`${apiBase}/projects/${selectedProjectId.value}/collections/${item.id}/rename`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json', ...authHeaders() },
      body: JSON.stringify({ name: nextName }),
    })
    const body = await res.json().catch(() => ({}))
    if (!res.ok) throw new Error(body.error || body.errors?.name?.[0] || 'Rename failed')
    await loadCollections(selectedProjectId.value)
  } catch (error: any) {
    collectionError.value = error?.message || 'Rename failed'
  }
}

async function moveCollection(collectionId: string, newParentId: string | null, newIndex: number) {
  if (!selectedProjectId.value) return

  const apiBase = getApiBaseUrl()
  const res = await fetch(`${apiBase}/projects/${selectedProjectId.value}/collections/${collectionId}/move`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: JSON.stringify({ newParentId, newIndex }),
  })
  const body = await res.json().catch(() => ({}))
  if (!res.ok) throw new Error(body.error || body.errors?.newParentId?.[0] || 'Move failed')

  collections.value = Array.isArray(body) ? body : collections.value
}

function onDragStart(item: Collection) {
  draggingId.value = item.id
}

async function onDrop(parentId: string | null, index: number) {
  if (!draggingId.value) return
  try {
    await moveCollection(draggingId.value, parentId, index)
  } catch (error: any) {
    collectionError.value = error?.message || 'Move failed'
  } finally {
    draggingId.value = ''
  }
}

function openCreateProjectForm() {
  showCreateProjectForm.value = true
  createProjectError.value = ''
}

function closeCreateProjectForm() {
  showCreateProjectForm.value = false
  newProjectName.value = ''
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
    const apiBase = getApiBaseUrl()
    const res = await fetch(`${apiBase}/projects`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...authHeaders(),
      },
      body: JSON.stringify({
        workspaceId: auth.organization.value.id,
        name,
        sourceLanguage: 'en',
        description: '',
      }),
    })

    const body = await res.json().catch(() => ({}))
    if (!res.ok) {
      throw new Error(body.error || body.errors?.join(', ') || 'Failed to create project')
    }

    await loadProjects()
    selectedProjectId.value = body.id
    await loadCollections(body.id)
    closeCreateProjectForm()
  } catch (error: any) {
    createProjectError.value = error?.message || 'Failed to create project'
  }
}

onMounted(loadProjects)
watch(selectedProjectId, (id) => id && loadCollections(id))
</script>

<template>
  <div class="projects-page">
    <header class="page-header">
      <div>
        <h1>Projects</h1>
        <p class="page-subtitle">Manage your translation projects</p>
      </div>
      <UiButton @click="openCreateProjectForm">New Project</UiButton>
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
        <UiButton @click="openCreateProjectForm">Create Project</UiButton>
      </template>
    </AppEmptyState>

    <div v-else class="projects-layout">
      <section>
        <h2 class="section-title">Your projects</h2>
        <div class="projects-grid">
          <button v-for="project in projects" :key="project.id" class="project-card" :class="{ selected: selectedProjectId === project.id }" @click="selectedProjectId = project.id">
            <h3>{{ project.name }}</h3>
            <p>Status: {{ project.status }}</p>
          </button>
        </div>
      </section>

      <section class="collections-panel">
        <h2 class="section-title">Collections</h2>
        <p class="label-hint">Top-level folder is fixed as “Collections”. Drag and drop folders to reorder or move.</p>

        <div class="collection-form">
          <label class="label-with-hint" for="collectionName">
            <span>Folder name</span>
            <span class="label-hint">Names must be unique within the same parent folder</span>
          </label>
          <input id="collectionName" v-model="newCollectionName" type="text" autocomplete="off">

          <UiSelect
            id="collectionParent"
            v-model="newCollectionParentId"
            label="Parent folder"
            :options="selectableParents.map(option => ({ value: option.id, label: option.name }))"
          />
          <p class="label-hint">Nested folders are supported to a maximum depth of 3</p>
          <UiButton @click="createCollection">Add folder</UiButton>
        </div>

        <p v-if="collectionError" class="field-error">{{ collectionError }}</p>
        <AppSkeleton v-if="isLoadingCollections" lines="4" height="1.25rem" />

        <ul v-else class="tree-root">
          <CollectionTreeNode
            v-for="root in collectionTree"
            :key="root.id"
            :node="root"
            :dragging-id="draggingId"
            @dragstart="onDragStart(collections.find(c => c.id === $event)!)"
            @rename="renameCollection(collections.find(c => c.id === $event)!)"
            @drop="onDrop($event.parentId, $event.index)"
          />
        </ul>
      </section>
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
.page-subtitle { color: var(--color-text-muted); margin: 0; }
.projects-layout { display: grid; gap: var(--spacing-6); }
.section-title { margin: 0 0 var(--spacing-3); }
.projects-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: var(--spacing-4); }
.project-card { text-align: left; background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-lg); padding: var(--spacing-4); cursor: pointer; }
.project-card.selected { border-color: var(--color-primary-600); }
.collections-panel { border: 1px solid var(--color-border); border-radius: var(--radius-lg); padding: var(--spacing-4); background: var(--color-surface); }
.collection-form { display: grid; gap: var(--spacing-2); margin-bottom: var(--spacing-4); }
.label-with-hint { display: flex; flex-direction: column; gap: 2px; color: var(--color-text-primary); }
.label-hint { font-size: var(--font-size-xs); color: var(--color-text-muted); }
.collection-form input,
.project-form input {
  padding: var(--spacing-3);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-background);
  color: var(--color-text-primary);
}
.tree-root, .tree-root ul { list-style: none; margin: 0; padding-left: var(--spacing-4); }
.tree-node { display: flex; align-items: center; justify-content: space-between; border: 1px solid var(--color-border); border-radius: var(--radius-md); padding: var(--spacing-2) var(--spacing-3); margin-bottom: var(--spacing-2); }
.root-node { font-weight: 600; background: var(--color-primary-50); }
.drop-zone { border: 1px dashed var(--color-border); border-radius: var(--radius-md); padding: var(--spacing-2); color: var(--color-text-muted); margin-bottom: var(--spacing-2); }
.inline-action { border: 0; background: transparent; color: var(--color-primary-700); cursor: pointer; }
.field-error { color: var(--color-error); font-size: var(--font-size-xs); }
.project-form-overlay { position: fixed; inset: 0; background: color-mix(in srgb, var(--color-black) 45%, transparent); display: grid; place-items: center; z-index: var(--z-modal); }
.project-form { width: min(480px, 92vw); background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-xl); padding: var(--spacing-6); display: flex; flex-direction: column; gap: var(--spacing-3); }
.project-form-actions { display: flex; justify-content: flex-end; gap: var(--spacing-2); }
</style>
