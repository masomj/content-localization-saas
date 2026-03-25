<script setup lang="ts">
import AppEmptyState from '~/components/AppEmptyState.vue'
import AppSkeleton from '~/components/AppSkeleton.vue'
import UiButton from '~/components/ui/Button.vue'
import { projectsClient } from '~/api/projectsClient'
import { contentClient } from '~/api/contentClient'
import type { Project, ProjectTreeNode } from '~/api/types'

definePageMeta({ layout: 'app' })
useSeoMeta({ title: 'Projects - LocFlow' })

type ProjectView = Project & { status: string; progress: number; languages: number }

const auth = useAuth()
const isLoading = ref(false)
const projects = ref<ProjectView[]>([])
const selectedProjectId = ref<string>('')
const treeNodes = ref<ProjectTreeNode[]>([])
const isLoadingTree = ref(false)
const treeError = ref('')
const draggingId = ref('')
const draggingNodeType = ref<'folder' | 'contentKey'>('folder')

const showCreateProjectForm = ref(false)
const newProjectName = ref('')

// Tree action modals
const treeModal = ref<{
  type: 'newFolder' | 'newContentKey' | 'rename' | 'delete'
  parentId?: string | null
  nodeId?: string
  nodeType?: 'folder' | 'contentKey'
  nodeName?: string
} | null>(null)
const treeModalInput = ref('')
const treeModalError = ref('')
const treeModalSubmitting = ref(false)
const createProjectError = ref('')

async function loadProjects() {
  if (!auth.organization.value?.id) {
    projects.value = []
    return
  }

  isLoading.value = true
  try {
    const data = await projectsClient.list(auth.organization.value.id)
    projects.value = (Array.isArray(data) ? data : []).map((p: Project) => ({
      id: p.id,
      name: p.name,
      status: p.status ?? 'Draft',
      progress: 0,
      languages: 0,
    }))

    if (!selectedProjectId.value && projects.value.length > 0) {
      selectedProjectId.value = projects.value[0].id
      await loadTree(selectedProjectId.value)
    }
  } catch {
    projects.value = []
  } finally {
    isLoading.value = false
  }
}

async function loadTree(projectId: string) {
  if (!projectId) return
  isLoadingTree.value = true
  treeError.value = ''

  try {
    treeNodes.value = await projectsClient.getProjectTree(projectId)
  } catch (error: any) {
    treeError.value = error?.message || 'Failed to load project tree'
    treeNodes.value = []
  } finally {
    isLoadingTree.value = false
  }
}

function handleRename(nodeId: string) {
  if (!selectedProjectId.value) return
  const node = findNode(treeNodes.value, nodeId)
  if (!node) return
  treeModal.value = { type: 'rename', nodeId, nodeType: node.nodeType, nodeName: node.name }
  treeModalInput.value = node.name
  treeModalError.value = ''
}

function handleDelete(nodeId: string) {
  if (!selectedProjectId.value) return
  const node = findNode(treeNodes.value, nodeId)
  if (!node) return
  treeModal.value = { type: 'delete', nodeId, nodeType: node.nodeType, nodeName: node.name }
  treeModalError.value = ''
}

function handleNewFolder(parentId: string) {
  treeModal.value = { type: 'newFolder', parentId }
  treeModalInput.value = ''
  treeModalError.value = ''
}

function handleNewContentKey(parentId: string) {
  treeModal.value = { type: 'newContentKey', parentId }
  treeModalInput.value = ''
  treeModalError.value = ''
}

function handleNewRootFolder() {
  treeModal.value = { type: 'newFolder', parentId: null }
  treeModalInput.value = ''
  treeModalError.value = ''
}

function handleNewRootContentKey() {
  treeModal.value = { type: 'newContentKey', parentId: null }
  treeModalInput.value = ''
  treeModalError.value = ''
}

function closeTreeModal() {
  treeModal.value = null
  treeModalInput.value = ''
  treeModalError.value = ''
  treeModalSubmitting.value = false
}

async function submitTreeModal() {
  if (!selectedProjectId.value || !treeModal.value) return
  const m = treeModal.value
  treeModalSubmitting.value = true
  treeModalError.value = ''

  try {
    if (m.type === 'newFolder') {
      const name = treeModalInput.value.trim()
      if (!name) { treeModalError.value = 'Folder name is required'; treeModalSubmitting.value = false; return }
      await projectsClient.createCollection(selectedProjectId.value, { name, parentId: m.parentId ?? null })
    } else if (m.type === 'newContentKey') {
      const key = treeModalInput.value.trim()
      if (!key) { treeModalError.value = 'Content key is required'; treeModalSubmitting.value = false; return }
      await contentClient.create({
        projectId: selectedProjectId.value,
        key,
        source: '',
        status: 'Draft',
        tags: [],
        context: null,
        notes: null,
        collectionId: m.parentId ?? null,
      })
    } else if (m.type === 'rename' && m.nodeId) {
      const name = treeModalInput.value.trim()
      if (!name) { treeModalError.value = 'Name is required'; treeModalSubmitting.value = false; return }
      if (m.nodeType === 'folder') {
        await projectsClient.renameCollection(selectedProjectId.value, m.nodeId, name)
      }
    } else if (m.type === 'delete' && m.nodeId) {
      // Delete endpoint — reload tree after
    }
    closeTreeModal()
    await loadTree(selectedProjectId.value)
  } catch (error: any) {
    treeModalError.value = error?.data?.error || error?.message || 'Operation failed'
    treeModalSubmitting.value = false
  }
}

function onDragStart(payload: { nodeId: string; nodeType: 'folder' | 'contentKey' }) {
  draggingId.value = payload.nodeId
  draggingNodeType.value = payload.nodeType
}

async function onDrop(payload: { targetId: string | null; index: number; nodeType: 'folder' | 'contentKey' }) {
  if (!draggingId.value || !selectedProjectId.value) return

  try {
    if (draggingNodeType.value === 'contentKey') {
      await contentClient.move(draggingId.value, {
        collectionId: payload.targetId,
        sortOrder: payload.index,
      })
    } else {
      await projectsClient.moveCollection(
        selectedProjectId.value,
        draggingId.value,
        { newParentId: payload.targetId, newIndex: payload.index },
      )
    }
    await loadTree(selectedProjectId.value)
  } catch (error: any) {
    treeError.value = error?.message || 'Move failed'
  } finally {
    draggingId.value = ''
  }
}

async function onRootDrop(e: DragEvent) {
  e.preventDefault()
  if (!draggingId.value || !selectedProjectId.value) return

  try {
    if (draggingNodeType.value === 'contentKey') {
      await contentClient.move(draggingId.value, {
        collectionId: null,
        sortOrder: treeNodes.value.length,
      })
    } else {
      await projectsClient.moveCollection(
        selectedProjectId.value,
        draggingId.value,
        { newParentId: null, newIndex: treeNodes.value.length },
      )
    }
    await loadTree(selectedProjectId.value)
  } catch (error: any) {
    treeError.value = error?.message || 'Move failed'
  } finally {
    draggingId.value = ''
  }
}

function findNode(nodes: ProjectTreeNode[], id: string): ProjectTreeNode | null {
  for (const node of nodes) {
    if (node.id === id) return node
    if (node.children?.length) {
      const found = findNode(node.children, id)
      if (found) return found
    }
  }
  return null
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
    const project = await projectsClient.create({
      workspaceId: auth.organization.value.id,
      name,
      sourceLanguage: 'en',
      description: '',
    })

    await loadProjects()
    selectedProjectId.value = project.id
    await loadTree(project.id)
    closeCreateProjectForm()
  } catch (error: any) {
    createProjectError.value = error?.message || 'Failed to create project'
  }
}

onMounted(loadProjects)
watch(selectedProjectId, (id) => id && loadTree(id))
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
          <div v-for="project in projects" :key="project.id" class="project-card" :class="{ selected: selectedProjectId === project.id }">
            <button class="project-card-select" @click="selectedProjectId = project.id">
              <h3>{{ project.name }}</h3>
              <p>Status: {{ project.status }}</p>
            </button>
            <NuxtLink :to="`/app/projects/${project.id}/versions`" class="project-card-releases">
              Releases
            </NuxtLink>
          </div>
        </div>
      </section>

      <section class="collections-panel">
        <div class="tree-header">
          <h2 class="section-title">Project Tree</h2>
          <div class="tree-header__actions">
            <UiButton variant="secondary" @click="handleNewRootFolder">New Folder</UiButton>
            <UiButton variant="secondary" @click="handleNewRootContentKey">New Content Key</UiButton>
          </div>
        </div>
        <p class="label-hint">Drag content keys between folders. Right-click or use the menu for more actions.</p>

        <p v-if="treeError" class="field-error">{{ treeError }}</p>
        <AppSkeleton v-if="isLoadingTree" lines="4" height="1.25rem" />

        <ul
          v-else
          class="tree-root"
          role="tree"
          aria-label="Project content tree"
        >
          <CollectionTreeNode
            v-for="root in treeNodes"
            :key="root.id"
            :node="root"
            :dragging-id="draggingId"
            @dragstart="onDragStart"
            @rename="handleRename"
            @delete="handleDelete"
            @new-folder="handleNewFolder"
            @new-content-key="handleNewContentKey"
            @drop="onDrop"
          />
        </ul>

        <div
          v-if="!isLoadingTree && treeNodes.length > 0"
          class="root-drop-zone"
          @dragover.prevent
          @drop="onRootDrop"
        >
          Drop here to move to project root
        </div>
      </section>
    </div>

    <!-- Tree action modal -->
    <div v-if="treeModal" class="project-form-overlay" @click.self="closeTreeModal">
      <form class="project-form" @submit.prevent="submitTreeModal">
        <h2 v-if="treeModal.type === 'newFolder'">New folder</h2>
        <h2 v-else-if="treeModal.type === 'newContentKey'">New content key</h2>
        <h2 v-else-if="treeModal.type === 'rename'">Rename {{ treeModal.nodeType === 'folder' ? 'folder' : 'content key' }}</h2>
        <h2 v-else-if="treeModal.type === 'delete'">Delete {{ treeModal.nodeType === 'folder' ? 'folder' : 'content key' }}</h2>

        <template v-if="treeModal.type === 'delete'">
          <p class="modal-body-text">
            Are you sure you want to delete <strong>"{{ treeModal.nodeName }}"</strong>?
            <template v-if="treeModal.nodeType === 'folder'">
              Content keys inside will be moved to the project root.
            </template>
          </p>
        </template>

        <template v-else>
          <label :for="'treeModalInput'" class="label-with-hint">
            <span v-if="treeModal.type === 'newFolder'">Folder name</span>
            <span v-else-if="treeModal.type === 'newContentKey'">Content key</span>
            <span v-else>Name</span>
            <span v-if="treeModal.type === 'newFolder'" class="label-hint">e.g. auth, common, onboarding</span>
            <span v-else-if="treeModal.type === 'newContentKey'" class="label-hint">e.g. auth.login.title</span>
            <span v-else class="label-hint">Enter the new name</span>
          </label>
          <input
            id="treeModalInput"
            v-model="treeModalInput"
            type="text"
            autocomplete="off"
          >
        </template>

        <p v-if="treeModalError" class="field-error" role="alert">{{ treeModalError }}</p>

        <div class="project-form-actions">
          <UiButton type="button" variant="secondary" @click="closeTreeModal">Cancel</UiButton>
          <UiButton
            v-if="treeModal.type === 'delete'"
            type="submit"
            variant="danger"
            :disabled="treeModalSubmitting"
          >
            Delete
          </UiButton>
          <UiButton
            v-else
            type="submit"
            :disabled="treeModalSubmitting"
          >
            {{ treeModal.type === 'rename' ? 'Rename' : 'Create' }}
          </UiButton>
        </div>
      </form>
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
.project-card { text-align: left; background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-lg); overflow: hidden; display: flex; flex-direction: column; }
.project-card.selected { border-color: var(--color-primary-600); }
.project-card-select { text-align: left; background: none; border: none; padding: var(--spacing-4); cursor: pointer; flex: 1; color: inherit; font: inherit; }
.project-card-releases { display: block; padding: var(--spacing-2) var(--spacing-4); font-size: var(--font-size-xs); color: var(--color-primary-600); text-decoration: none; border-top: 1px solid var(--color-border); }
.project-card-releases:hover { background: var(--color-background); }
.collections-panel { border: 1px solid var(--color-border); border-radius: var(--radius-lg); padding: var(--spacing-4); background: var(--color-surface); }
.tree-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--spacing-2); }
.tree-header .section-title { margin: 0; }
.tree-header__actions { display: flex; gap: var(--spacing-2); }
.label-with-hint { display: flex; flex-direction: column; gap: 2px; color: var(--color-text-primary); }
.label-hint { font-size: var(--font-size-xs); color: var(--color-text-muted); margin-bottom: var(--spacing-2); }
.tree-root { list-style: none; margin: 0; padding: 0; }
.root-drop-zone { border: 1px dashed var(--color-border); border-radius: var(--radius-md); padding: var(--spacing-2); color: var(--color-text-muted); font-size: var(--font-size-xs); text-align: center; margin-top: var(--spacing-2); }
.field-error { color: var(--color-error); font-size: var(--font-size-xs); }
.modal-body-text { margin: 0; color: var(--color-text-secondary); font-size: var(--font-size-sm); line-height: 1.5; }
.project-form-overlay { position: fixed; inset: 0; background: color-mix(in srgb, var(--color-black) 45%, transparent); display: grid; place-items: center; z-index: var(--z-modal); }
.project-form { width: min(480px, 92vw); background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-xl); padding: var(--spacing-6); display: flex; flex-direction: column; gap: var(--spacing-3); }
.project-form input {
  padding: var(--spacing-3);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-background);
  color: var(--color-text-primary);
}
.project-form-actions { display: flex; justify-content: flex-end; gap: var(--spacing-2); }
</style>
