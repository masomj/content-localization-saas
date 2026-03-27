<script setup lang="ts">
import AppEmptyState from '~/components/AppEmptyState.vue'
import AppSkeleton from '~/components/AppSkeleton.vue'
import ExportPanel from '~/components/projects/ExportPanel.vue'
import UiButton from '~/components/ui/Button.vue'
import UiSelect from '~/components/ui/Select.vue'
import { contentClient } from '~/api/contentClient'
import { projectsClient } from '~/api/projectsClient'
import type { ContentItem, Project, ProjectTreeNode } from '~/api/types'

definePageMeta({ layout: 'app' })
useSeoMeta({ title: 'Content - LocFlow' })

const auth = useAuth()
const isLoading = ref(false)
const projects = ref<Array<{ id: string; name: string; description?: string }>>([])
const selectedProjectId = ref('')
const contents = ref<Array<Pick<ContentItem, 'id' | 'key' | 'source' | 'status' | 'collectionId'>>>([])

const showExport = ref(false)
const showAddContentForm = ref(false)
const newContentKey = ref('')
const newContentSource = ref('')
const addContentError = ref('')

const showNewFolderForm = ref(false)
const newFolderName = ref('')
const newFolderError = ref('')

// Delete confirmation modal
const showDeleteConfirm = ref(false)
const deleteTarget = ref<Pick<ContentItem, 'id' | 'key'> | null>(null)
const deleteError = ref('')

// Side panel
const selectedItem = ref<Pick<ContentItem, 'id' | 'key' | 'source' | 'status' | 'collectionId'> | null>(null)
const panelSource = ref('')
const panelStatus = ref('')
const panelCollectionId = ref<string | null>(null)
const panelError = ref('')

const selectedProject = computed(() =>
  projects.value.find(p => p.id === selectedProjectId.value) ?? null,
)

// ---------------------------------------------------------------------------
// Drag-and-drop state
// ---------------------------------------------------------------------------
const draggingItemId = ref<string | null>(null)
const dragOverFolderId = ref<string | null | '__parent__'>(null)

function onDragStart(itemId: string) {
  draggingItemId.value = itemId
}

function onDragEnd() {
  draggingItemId.value = null
  dragOverFolderId.value = null
}

function onFolderDragEnter(folderId: string | '__parent__') {
  dragOverFolderId.value = folderId
}

function onFolderDragLeave(folderId: string | '__parent__', event: DragEvent) {
  const related = event.relatedTarget as HTMLElement | null
  const target = event.currentTarget as HTMLElement
  if (related && target.contains(related)) return
  if (dragOverFolderId.value === folderId) {
    dragOverFolderId.value = null
  }
}

async function onDropOnFolder(collectionId: string | null) {
  if (!draggingItemId.value) return
  try {
    await contentClient.move(draggingItemId.value, { collectionId, sortOrder: 0 })
    await loadContent()
  } catch (error: any) {
    // silently fail — user can retry
  } finally {
    draggingItemId.value = null
    dragOverFolderId.value = null
  }
}

// ---------------------------------------------------------------------------
// Folder navigation state
// ---------------------------------------------------------------------------
const treeNodes = ref<ProjectTreeNode[]>([])
const currentFolderId = ref<string | null>(null)

interface BreadcrumbItem {
  id: string | null
  name: string
}

/** Build breadcrumb trail from root to current folder */
const breadcrumbs = computed<BreadcrumbItem[]>(() => {
  const trail: BreadcrumbItem[] = [{ id: null, name: 'Project Root' }]
  if (currentFolderId.value === null) return trail

  // Walk the tree to find the path to currentFolderId
  function findPath(nodes: ProjectTreeNode[], target: string): ProjectTreeNode[] | null {
    for (const node of nodes) {
      if (node.id === target) return [node]
      if (node.nodeType === 'folder' && node.children.length > 0) {
        const sub = findPath(node.children, target)
        if (sub) return [node, ...sub]
      }
    }
    return null
  }

  const path = findPath(treeNodes.value, currentFolderId.value)
  if (path) {
    for (const node of path) {
      trail.push({ id: node.id, name: node.name })
    }
  }
  return trail
})

/** Children of the current folder (folders first, then content keys) */
const currentChildren = computed(() => {
  if (currentFolderId.value === null) {
    return treeNodes.value
  }
  function findNode(nodes: ProjectTreeNode[], id: string): ProjectTreeNode | null {
    for (const n of nodes) {
      if (n.id === id) return n
      if (n.children.length > 0) {
        const found = findNode(n.children, id)
        if (found) return found
      }
    }
    return null
  }
  const folder = findNode(treeNodes.value, currentFolderId.value)
  return folder?.children ?? []
})

const currentFolders = computed(() =>
  currentChildren.value.filter(n => n.nodeType === 'folder'),
)

/** Content items in the current folder (matched from the flat contents list) */
const folderContentItems = computed(() => {
  // Match content items whose collectionId equals currentFolderId
  return contents.value.filter(c => (c.collectionId ?? null) === currentFolderId.value)
})

/** Flatten all folders in the tree for the side panel folder dropdown */
const folderOptions = computed(() => {
  const options: Array<{ value: string; label: string }> = [{ value: '__root__', label: 'Project Root' }]
  function walk(nodes: ProjectTreeNode[], prefix: string) {
    for (const node of nodes) {
      if (node.nodeType === 'folder') {
        options.push({ value: node.id, label: prefix + node.name })
        walk(node.children, prefix + node.name + ' / ')
      }
    }
  }
  walk(treeNodes.value, '')
  return options
})

// ---------------------------------------------------------------------------
// List-view pagination
// ---------------------------------------------------------------------------
const listPage = ref(1)
const listPageSize = ref(20)

const listTotalPages = computed(() =>
  Math.max(1, Math.ceil(folderContentItems.value.length / listPageSize.value)),
)

const paginatedContentItems = computed(() => {
  const start = (listPage.value - 1) * listPageSize.value
  return folderContentItems.value.slice(start, start + listPageSize.value)
})

// ---------------------------------------------------------------------------
// Folder child count (for display)
// ---------------------------------------------------------------------------
function folderChildCount(node: ProjectTreeNode): number {
  let count = 0
  function walk(n: ProjectTreeNode) {
    if (n.nodeType === 'contentKey') count++
    for (const child of n.children) walk(child)
  }
  for (const child of node.children) walk(child)
  return count
}

// ---------------------------------------------------------------------------
// Navigation
// ---------------------------------------------------------------------------
function navigateToFolder(folderId: string | null) {
  currentFolderId.value = folderId
  listPage.value = 1
}

/** Navigate to parent folder. Uses breadcrumbs — the parent is the second-to-last breadcrumb. */
function navigateUp() {
  const bc = breadcrumbs.value
  if (bc.length >= 2) {
    navigateToFolder(bc[bc.length - 2]!.id)
  }
}

// ---------------------------------------------------------------------------
// Side panel (replaces TranslationEditor row click)
// ---------------------------------------------------------------------------
function openSidePanel(item: Pick<ContentItem, 'id' | 'key' | 'source' | 'status' | 'collectionId'>) {
  selectedItem.value = item
  panelSource.value = item.source
  panelStatus.value = item.status
  panelCollectionId.value = item.collectionId ?? null
  panelError.value = ''
}

function closeSidePanel() {
  selectedItem.value = null
  panelSource.value = ''
  panelStatus.value = ''
  panelCollectionId.value = null
  panelError.value = ''
}

async function saveSidePanel() {
  if (!selectedItem.value) return
  const source = panelSource.value.trim()
  if (!source) {
    panelError.value = 'Source text is required'
    return
  }
  try {
    await contentClient.update(selectedItem.value.id, source, panelStatus.value)

    // Move if folder changed
    const originalCollectionId = selectedItem.value.collectionId ?? null
    const newCollectionId = panelCollectionId.value === '__root__' ? null : panelCollectionId.value
    if (newCollectionId !== originalCollectionId) {
      await contentClient.move(selectedItem.value.id, { collectionId: newCollectionId, sortOrder: 0 })
    }

    await loadContent()
    closeSidePanel()
  } catch (error: any) {
    panelError.value = error?.message || 'Failed to save changes'
  }
}

async function deleteSidePanelItem() {
  if (!selectedItem.value) return
  try {
    await contentClient.delete(selectedItem.value.id)
    await loadContent()
    closeSidePanel()
  } catch (error: any) {
    panelError.value = error?.message || 'Failed to delete content item'
  }
}

// ---------------------------------------------------------------------------
// Data loading
// ---------------------------------------------------------------------------
async function loadProjects() {
  if (!auth.organization.value?.id) return
  const data = await projectsClient.list(auth.organization.value.id)
  projects.value = (Array.isArray(data) ? data : []).map((p: Project) => ({ id: p.id, name: p.name, description: p.description }))

  if (!selectedProjectId.value && projects.value.length > 0) {
    selectedProjectId.value = projects.value[0]!.id
  }
}

async function loadContent() {
  if (!selectedProjectId.value) {
    contents.value = []
    treeNodes.value = []
    return
  }

  isLoading.value = true
  try {
    const [contentData, treeData] = await Promise.all([
      contentClient.list(selectedProjectId.value),
      projectsClient.getProjectTree(selectedProjectId.value),
    ])
    contents.value = (Array.isArray(contentData) ? contentData : []).map((c: ContentItem) => ({
      id: c.id,
      key: c.key,
      source: c.source,
      status: c.status,
      collectionId: c.collectionId,
    }))
    treeNodes.value = Array.isArray(treeData) ? treeData : []
  } catch {
    contents.value = []
    treeNodes.value = []
  } finally {
    isLoading.value = false
  }
}

// ---------------------------------------------------------------------------
// Add content
// ---------------------------------------------------------------------------
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
    await contentClient.create({
      projectId: selectedProjectId.value,
      key,
      source,
      status: 'draft',
      tags: [],
      context: null,
      notes: null,
      collectionId: currentFolderId.value,
    })

    await loadContent()
    closeAddContentForm()
  } catch (error: any) {
    addContentError.value = error?.message || 'Failed to add content'
  }
}

// ---------------------------------------------------------------------------
// New folder
// ---------------------------------------------------------------------------
function openNewFolderForm() {
  newFolderError.value = ''
  showNewFolderForm.value = true
}

function closeNewFolderForm() {
  showNewFolderForm.value = false
  newFolderName.value = ''
  newFolderError.value = ''
}

async function createFolder() {
  const name = newFolderName.value.trim()

  if (!selectedProjectId.value) {
    newFolderError.value = 'Please select a project first'
    return
  }

  if (!name) {
    newFolderError.value = 'Folder name is required'
    return
  }

  try {
    await projectsClient.createCollection(selectedProjectId.value, {
      name,
      parentId: currentFolderId.value,
    })

    await loadContent()
    closeNewFolderForm()
  } catch (error: any) {
    newFolderError.value = error?.message || 'Failed to create folder'
  }
}

// ---------------------------------------------------------------------------
// Delete content item
// ---------------------------------------------------------------------------
function openDeleteConfirm(item: Pick<ContentItem, 'id' | 'key'>, event: Event) {
  event.stopPropagation()
  deleteTarget.value = item
  deleteError.value = ''
  showDeleteConfirm.value = true
}

function closeDeleteConfirm() {
  showDeleteConfirm.value = false
  deleteTarget.value = null
  deleteError.value = ''
}

async function confirmDelete() {
  if (!deleteTarget.value) return
  try {
    await contentClient.delete(deleteTarget.value.id)
    await loadContent()
    closeDeleteConfirm()
  } catch (error: any) {
    deleteError.value = error?.message || 'Failed to delete content item'
  }
}

// ---------------------------------------------------------------------------
// Status badge helper
// ---------------------------------------------------------------------------
function statusBadgeClass(status: string): string {
  switch (status) {
    case 'approved':
    case 'done':
      return 'badge--done'
    case 'pending_review':
      return 'badge--review'
    case 'outdated':
      return 'badge--outdated'
    case 'draft':
      return 'badge--draft'
    default:
      return 'badge--default'
  }
}

function truncate(text: string, max: number): string {
  if (!text) return ''
  return text.length > max ? `${text.slice(0, max)}...` : text
}

// ---------------------------------------------------------------------------
// Lifecycle
// ---------------------------------------------------------------------------
onMounted(async () => {
  await loadProjects()
  await loadContent()
})

watch(selectedProjectId, async () => {
  currentFolderId.value = null
  listPage.value = 1
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
      <div class="page-header-actions">
        <NuxtLink to="/app/content/languages" class="header-link-btn">
          Languages
        </NuxtLink>
        <UiButton :disabled="!selectedProjectId" variant="secondary" @click="showExport = true">
          Export
        </UiButton>
        <UiButton :disabled="!selectedProjectId" @click="openAddContentForm">
          <svg viewBox="0 0 20 20" fill="currentColor" class="btn-icon">
            <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
          </svg>
          Add Content
        </UiButton>
        <UiButton :disabled="!selectedProjectId" variant="secondary" @click="openNewFolderForm">
          <svg viewBox="0 0 20 20" fill="currentColor" class="btn-icon">
            <path d="M2 6a2 2 0 012-2h5l2 2h5a2 2 0 012 2v6a2 2 0 01-2 2H4a2 2 0 01-2-2V6z" />
          </svg>
          New Folder
        </UiButton>
      </div>
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

    <div v-if="selectedProject" class="project-subheader">
      <p class="project-subheader-name">{{ selectedProject.name }}</p>
      <p v-if="selectedProject.description" class="project-subheader-desc">{{ selectedProject.description }}</p>
    </div>

    <AppEmptyState
      v-if="projects.length === 0"
      title="No projects available"
      description="Create a project first. Content cannot exist without a project."
    />

    <template v-else-if="selectedProjectId">
      <!-- ============================================================ -->
      <!-- Breadcrumb navigation (shared by list and grid views)        -->
      <!-- ============================================================ -->
      <nav v-if="breadcrumbs.length > 0" class="breadcrumb-bar" aria-label="Folder navigation">
        <template v-for="(crumb, idx) in breadcrumbs" :key="crumb.id ?? '__root'">
          <span v-if="idx > 0" class="breadcrumb-sep">/</span>
          <button
            v-if="idx < breadcrumbs.length - 1"
            class="breadcrumb-link"
            @click="navigateToFolder(crumb.id)"
          >
            {{ crumb.name }}
          </button>
          <span v-else class="breadcrumb-current">{{ crumb.name }}</span>
        </template>
      </nav>

      <!-- ============================================================ -->
      <!-- LIST VIEW                                                    -->
      <!-- ============================================================ -->
        <div v-if="isLoading" class="content-list">
          <div v-for="i in 3" :key="i" class="content-item"><AppSkeleton lines="2" height="1rem" /></div>
        </div>

        <template v-else>
          <AppEmptyState
            v-if="currentFolders.length === 0 && folderContentItems.length === 0 && currentFolderId === null"
            title="No content in this project"
            description="Add content items linked to this project"
          >
            <template #action>
              <UiButton @click="openAddContentForm">Add Content</UiButton>
            </template>
          </AppEmptyState>

          <div v-else class="explorer-list">
            <!-- Parent navigation -->
            <button
              v-if="currentFolderId !== null"
              class="folder-row folder-row--parent"
              :class="{ 'folder-row--drag-over': dragOverFolderId === '__parent__' }"
              @click="navigateUp"
              @dragover.prevent
              @dragenter="onFolderDragEnter('__parent__')"
              @dragleave="onFolderDragLeave('__parent__', $event)"
              @drop.prevent="onDropOnFolder(breadcrumbs.length >= 2 ? breadcrumbs[breadcrumbs.length - 2]!.id : null)"
            >
              <svg class="folder-icon" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z" clip-rule="evenodd" />
              </svg>
              <span class="folder-name">..</span>
            </button>

            <!-- Folders -->
            <button
              v-for="folder in currentFolders"
              :key="folder.id"
              class="folder-row"
              :class="{ 'folder-row--drag-over': dragOverFolderId === folder.id }"
              @click="navigateToFolder(folder.id)"
              @dragover.prevent
              @dragenter="onFolderDragEnter(folder.id)"
              @dragleave="onFolderDragLeave(folder.id, $event)"
              @drop.prevent="onDropOnFolder(folder.id)"
            >
              <svg class="folder-icon" viewBox="0 0 20 20" fill="currentColor">
                <path d="M2 6a2 2 0 012-2h5l2 2h5a2 2 0 012 2v6a2 2 0 01-2 2H4a2 2 0 01-2-2V6z" />
              </svg>
              <span class="folder-name">{{ folder.name }}</span>
              <span class="folder-count">{{ folderChildCount(folder) }} items</span>
              <svg class="folder-chevron" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd" />
              </svg>
            </button>

            <!-- Content items (paginated) -->
            <button
              v-for="(item, idx) in paginatedContentItems"
              :key="item.id"
              class="content-row content-row--clickable"
              :class="{ 'content-row--alt': idx % 2 === 1 }"
              draggable="true"
              @dragstart="onDragStart(item.id)"
              @dragend="onDragEnd"
              @click="openSidePanel(item)"
            >
              <div class="content-row-main">
                <span class="content-key">{{ item.key }}</span>
                <span class="content-source">{{ truncate(item.source, 80) }}</span>
              </div>
              <span class="content-badge" :class="statusBadgeClass(item.status)">
                {{ item.status }}
              </span>
              <div class="content-row-actions">
                <button
                  class="row-action-btn row-action-btn--edit"
                  title="Edit content"
                  @click.stop="openSidePanel(item)"
                >
                  <svg viewBox="0 0 20 20" fill="currentColor"><path d="M13.586 3.586a2 2 0 112.828 2.828l-.793.793-2.828-2.828.793-.793zM11.379 5.793L3 14.172V17h2.828l8.38-8.379-2.83-2.828z" /></svg>
                </button>
                <button
                  class="row-action-btn row-action-btn--delete"
                  title="Delete content"
                  @click="openDeleteConfirm(item, $event)"
                >
                  <svg viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd" /></svg>
                </button>
              </div>
            </button>

            <!-- Empty folder state -->
            <div
              v-if="currentFolders.length === 0 && folderContentItems.length === 0 && currentFolderId !== null"
              class="explorer-empty"
            >
              This folder is empty. Add content items or create subfolders.
            </div>

            <!-- Pagination -->
            <div v-if="listTotalPages > 1" class="list-pagination">
              <UiButton size="sm" variant="secondary" :disabled="listPage <= 1" @click="listPage--">
                Previous
              </UiButton>
              <span class="list-page-info">
                Page {{ listPage }} of {{ listTotalPages }} ({{ folderContentItems.length }} items)
              </span>
              <UiButton size="sm" variant="secondary" :disabled="listPage >= listTotalPages" @click="listPage++">
                Next
              </UiButton>
            </div>
          </div>
        </template>
    </template>

    <!-- ============================================================ -->
    <!-- Add content modal                                            -->
    <!-- ============================================================ -->
    <div v-if="showAddContentForm" class="content-form-overlay" @click.self="closeAddContentForm">
      <form class="content-form" @submit.prevent="addContent">
        <h2>Add content item</h2>

        <div v-if="currentFolderId !== null" class="content-form-folder-hint">
          Adding to: <strong>{{ breadcrumbs[breadcrumbs.length - 1]?.name }}</strong>
        </div>

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

    <!-- ============================================================ -->
    <!-- New folder modal                                             -->
    <!-- ============================================================ -->
    <div v-if="showNewFolderForm" class="content-form-overlay" @click.self="closeNewFolderForm">
      <form class="content-form" @submit.prevent="createFolder">
        <h2>New folder</h2>

        <div v-if="currentFolderId !== null" class="content-form-folder-hint">
          Creating inside: <strong>{{ breadcrumbs[breadcrumbs.length - 1]?.name }}</strong>
        </div>

        <label for="folderName" class="label-with-hint">
          <span>Folder name</span>
          <span class="label-hint">e.g. auth, common, onboarding</span>
        </label>
        <input id="folderName" v-model="newFolderName" type="text" autocomplete="off">

        <p v-if="newFolderError" class="field-error">{{ newFolderError }}</p>

        <div class="content-form-actions">
          <UiButton type="button" variant="secondary" @click="closeNewFolderForm">Cancel</UiButton>
          <UiButton type="submit">Create folder</UiButton>
        </div>
      </form>
    </div>

    <ExportPanel
      v-if="showExport && selectedProjectId"
      :project-id="selectedProjectId"
      @close="showExport = false"
    />

    <!-- ============================================================ -->
    <!-- Delete confirmation modal                                     -->
    <!-- ============================================================ -->
    <div v-if="showDeleteConfirm" class="content-form-overlay" @click.self="closeDeleteConfirm">
      <div class="content-form">
        <h2>Delete content item</h2>
        <p class="delete-confirm-text">
          Delete key <strong class="delete-key">{{ deleteTarget?.key }}</strong>? This action cannot be undone.
        </p>
        <p v-if="deleteError" class="field-error">{{ deleteError }}</p>
        <div class="content-form-actions">
          <UiButton type="button" variant="secondary" @click="closeDeleteConfirm">Cancel</UiButton>
          <UiButton type="button" variant="danger" @click="confirmDelete">Delete</UiButton>
        </div>
      </div>
    </div>

    <!-- ============================================================ -->
    <!-- Side panel for editing content                                -->
    <!-- ============================================================ -->
    <Transition name="side-panel">
      <div v-if="selectedItem" class="side-panel-overlay" @click.self="closeSidePanel">
        <aside class="side-panel">
          <div class="side-panel-header">
            <h2>Edit Content</h2>
            <button class="side-panel-close" @click="closeSidePanel">
              <svg viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" /></svg>
            </button>
          </div>

          <p class="edit-key-heading">{{ selectedItem.key }}</p>

          <label for="panelSource" class="label-with-hint">
            <span>Source text</span>
            <span class="label-hint">The default text in the source language</span>
          </label>
          <textarea id="panelSource" v-model="panelSource" rows="6" class="side-panel-textarea" />

          <label for="panelStatus" class="label-with-hint">
            <span>Status</span>
            <span class="label-hint">Content lifecycle status</span>
          </label>
          <select id="panelStatus" v-model="panelStatus" class="edit-status-select">
            <option value="draft">Draft</option>
            <option value="in_review">In Review</option>
            <option value="approved">Approved</option>
          </select>

          <label for="panelFolder" class="label-with-hint">
            <span>Folder</span>
            <span class="label-hint">Move to a different folder</span>
          </label>
          <select id="panelFolder" v-model="panelCollectionId" class="edit-status-select">
            <option v-for="opt in folderOptions" :key="opt.value" :value="opt.value === '__root__' ? null : opt.value">
              {{ opt.label }}
            </option>
          </select>

          <p v-if="panelError" class="field-error">{{ panelError }}</p>

          <div class="side-panel-actions">
            <UiButton variant="danger" size="sm" @click="deleteSidePanelItem">Delete</UiButton>
            <div class="side-panel-actions-right">
              <UiButton variant="secondary" size="sm" @click="closeSidePanel">Cancel</UiButton>
              <UiButton size="sm" @click="saveSidePanel">Save</UiButton>
            </div>
          </div>
        </aside>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.content-page { max-width: 1200px; }
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: var(--spacing-6); }
.page-header h1 { font-size: var(--font-size-2xl); font-weight: var(--font-weight-semibold); color: var(--color-text-primary); margin: 0 0 var(--spacing-1) 0; }
.page-subtitle { color: var(--color-text-muted); margin: 0; }
.page-header-actions { display: flex; gap: var(--spacing-2); align-items: center; }
.header-link-btn {
  display: inline-flex;
  align-items: center;
  padding: var(--spacing-2) var(--spacing-4);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-secondary);
  text-decoration: none;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  transition: all var(--transition-fast);
}
.header-link-btn:hover {
  background: var(--color-gray-100);
  color: var(--color-text-primary);
  border-color: var(--color-gray-300);
}
.btn-icon { width: 1em; height: 1em; flex-shrink: 0; }
.project-picker { margin-bottom: var(--spacing-5); display: flex; flex-direction: column; gap: var(--spacing-2); max-width: 420px; }
.project-subheader {
  margin-bottom: var(--spacing-5);
  padding: var(--spacing-3) var(--spacing-4);
  background: color-mix(in srgb, var(--color-primary-600) 4%, var(--color-surface));
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
}
.project-subheader-name {
  margin: 0;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-muted);
}
.project-subheader-desc {
  margin: var(--spacing-1) 0 0;
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  line-height: 1.5;
}
.lang-manager-section { margin-bottom: var(--spacing-5); }
.label-with-hint { display: flex; flex-direction: column; gap: 2px; color: var(--color-text-primary); }
.label-hint { font-size: var(--font-size-xs); color: var(--color-text-muted); }

/* Breadcrumb bar */
.breadcrumb-bar {
  display: flex;
  align-items: center;
  gap: var(--spacing-1);
  padding: var(--spacing-3) var(--spacing-4);
  margin-bottom: var(--spacing-4);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  font-size: var(--font-size-sm);
  flex-wrap: wrap;
}
.breadcrumb-sep {
  color: var(--color-text-muted);
  margin: 0 var(--spacing-1);
  user-select: none;
}
.breadcrumb-link {
  background: none;
  border: none;
  padding: var(--spacing-1) var(--spacing-2);
  border-radius: var(--radius-md);
  color: var(--color-primary-600);
  cursor: pointer;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  transition: background var(--transition-fast);
}
.breadcrumb-link:hover {
  background: color-mix(in srgb, var(--color-primary-600) 8%, transparent);
}
.breadcrumb-current {
  color: var(--color-text-primary);
  font-weight: var(--font-weight-semibold);
  padding: var(--spacing-1) var(--spacing-2);
}

/* Folder rows (shared between list and grid views) */
.folder-row {
  display: flex;
  align-items: center;
  gap: var(--spacing-3);
  padding: var(--spacing-3) var(--spacing-4);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-bottom: none;
  cursor: pointer;
  transition: background var(--transition-fast);
  font-size: var(--font-size-sm);
  text-align: left;
  width: 100%;
}
.folder-row:first-child {
  border-radius: var(--radius-lg) var(--radius-lg) 0 0;
}
.folder-row:last-child {
  border-bottom: 1px solid var(--color-border);
  border-radius: 0 0 var(--radius-lg) var(--radius-lg);
}
.folder-row:first-child:last-child {
  border-radius: var(--radius-lg);
}
.folder-row:hover {
  background: color-mix(in srgb, var(--color-primary-600) 5%, transparent);
}
.folder-row--parent {
  color: var(--color-text-muted);
  font-weight: var(--font-weight-medium);
}
.folder-row--drag-over {
  border: 2px dashed var(--color-primary-500);
  background: color-mix(in srgb, var(--color-primary-600) 10%, transparent);
}
.folder-icon {
  width: 1.25rem;
  height: 1.25rem;
  flex-shrink: 0;
  color: var(--color-primary-500);
}
.folder-row--parent .folder-icon {
  color: var(--color-text-muted);
}
.folder-name {
  flex: 1;
  font-weight: var(--font-weight-medium);
  color: var(--color-text-primary);
}
.folder-row--parent .folder-name {
  color: var(--color-text-muted);
}
.folder-count {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}
.folder-chevron {
  width: 1rem;
  height: 1rem;
  color: var(--color-text-muted);
  flex-shrink: 0;
}

/* Explorer list (list view) */
.explorer-list {
  display: flex;
  flex-direction: column;
}

/* Content rows */
.content-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--spacing-3);
  padding: var(--spacing-3) var(--spacing-4);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-bottom: none;
  font-size: var(--font-size-sm);
  width: 100%;
  text-align: left;
  font: inherit;
  color: inherit;
}
.content-row--clickable {
  cursor: pointer;
  transition: background var(--transition-fast);
}
.content-row--clickable:hover {
  background: color-mix(in srgb, var(--color-primary-600) 5%, transparent);
}
.content-row:last-of-type {
  border-bottom: 1px solid var(--color-border);
  border-radius: 0 0 var(--radius-lg) var(--radius-lg);
}
/* When there are no folders and this is the first content row */
.explorer-list > .content-row:first-child,
.explorer-list > .folder-row--parent + .content-row:first-of-type {
  border-radius: var(--radius-lg) var(--radius-lg) 0 0;
}
.content-row--alt {
  background: color-mix(in srgb, var(--color-border) 15%, var(--color-surface));
}
.content-row-main {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
  flex: 1;
}
.content-key {
  font-family: monospace;
  font-weight: var(--font-weight-medium);
  color: var(--color-text-primary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.content-source {
  color: var(--color-text-muted);
  font-size: var(--font-size-xs);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.content-badge {
  flex-shrink: 0;
  padding: 2px var(--spacing-2);
  border-radius: var(--radius-md);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  text-transform: capitalize;
}
.badge--done { background: color-mix(in srgb, #22c55e 15%, transparent); color: #16a34a; }
.badge--review { background: color-mix(in srgb, #eab308 15%, transparent); color: #a16207; }
.badge--outdated { background: color-mix(in srgb, #f97316 15%, transparent); color: #c2410c; }
.badge--draft { background: color-mix(in srgb, #6366f1 12%, transparent); color: #6366f1; }
.badge--default { background: var(--color-surface); color: var(--color-text-muted); border: 1px solid var(--color-border); }

/* Empty folder state */
.explorer-empty {
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
  padding: var(--spacing-6) var(--spacing-4);
  text-align: center;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
}

/* List pagination */
.list-pagination {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--spacing-3);
  padding-top: var(--spacing-4);
}
.list-page-info {
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
}

/* Content list (skeleton state) */
.content-list { display: flex; flex-direction: column; gap: var(--spacing-3); }
.content-item { background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-lg); padding: var(--spacing-4); }

/* Add content form */
.content-form-overlay { position: fixed; inset: 0; background: color-mix(in srgb, var(--color-black) 45%, transparent); display: grid; place-items: center; z-index: var(--z-modal); }
.content-form { width: min(560px, 92vw); background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-xl); padding: var(--spacing-6); display: flex; flex-direction: column; gap: var(--spacing-3); }
.content-form h2 { margin: 0 0 var(--spacing-2); color: var(--color-text-primary); }
.content-form input,.content-form textarea { padding: var(--spacing-3) var(--spacing-4); border: 1px solid var(--color-border); border-radius: var(--radius-lg); background: var(--color-background); color: var(--color-text-primary); }
.content-form-folder-hint { font-size: var(--font-size-sm); color: var(--color-text-muted); padding: var(--spacing-2) var(--spacing-3); background: color-mix(in srgb, var(--color-primary-600) 6%, transparent); border-radius: var(--radius-md); }
.field-error { margin: 0; color: var(--color-error); font-size: var(--font-size-xs); }
.content-form-actions { display: flex; justify-content: flex-end; gap: var(--spacing-2); }

/* Row action buttons */
.content-row-actions {
  display: flex;
  gap: var(--spacing-1);
  flex-shrink: 0;
  margin-left: var(--spacing-2);
}
.row-action-btn {
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
  transition: background var(--transition-fast), color var(--transition-fast), border-color var(--transition-fast);
}
.row-action-btn svg {
  width: 14px;
  height: 14px;
}
.row-action-btn--edit:hover {
  background: color-mix(in srgb, var(--color-primary-600) 10%, transparent);
  color: var(--color-primary-600);
  border-color: var(--color-primary-600);
}
.row-action-btn--delete:hover {
  background: color-mix(in srgb, var(--color-error) 10%, transparent);
  color: var(--color-error);
  border-color: var(--color-error);
}

/* Delete confirmation modal */
.delete-confirm-text {
  margin: 0;
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  line-height: 1.6;
}
.delete-key {
  font-family: monospace;
  color: var(--color-text-primary);
}

/* Edit content modal */
.edit-key-heading {
  margin: 0;
  font-family: monospace;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  padding: var(--spacing-2) var(--spacing-3);
  background: color-mix(in srgb, var(--color-primary-600) 6%, transparent);
  border-radius: var(--radius-md);
}
.edit-status-select {
  padding: var(--spacing-3) var(--spacing-4);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  background: var(--color-background);
  color: var(--color-text-primary);
  font-size: var(--font-size-sm);
}
.edit-status-select:focus {
  outline: none;
  border-color: var(--color-primary-500);
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.15);
}

/* Side panel */
.side-panel-overlay {
  position: fixed;
  inset: 0;
  background: color-mix(in srgb, var(--color-black) 30%, transparent);
  z-index: var(--z-modal);
  display: flex;
  justify-content: flex-end;
}
.side-panel {
  width: min(480px, 92vw);
  height: 100%;
  background: var(--color-surface);
  border-left: 1px solid var(--color-border);
  padding: var(--spacing-6);
  display: flex;
  flex-direction: column;
  gap: var(--spacing-3);
  overflow-y: auto;
  box-shadow: -4px 0 24px rgba(0, 0, 0, 0.08);
}
.side-panel-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
.side-panel-header h2 {
  margin: 0;
  color: var(--color-text-primary);
}
.side-panel-close {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  padding: 0;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-surface);
  color: var(--color-text-muted);
  cursor: pointer;
  transition: background var(--transition-fast), color var(--transition-fast);
}
.side-panel-close:hover {
  background: color-mix(in srgb, var(--color-error) 10%, transparent);
  color: var(--color-error);
}
.side-panel-close svg {
  width: 16px;
  height: 16px;
}
.side-panel-textarea {
  padding: var(--spacing-3) var(--spacing-4);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  background: var(--color-background);
  color: var(--color-text-primary);
  font-size: var(--font-size-sm);
  resize: vertical;
}
.side-panel-textarea:focus {
  outline: none;
  border-color: var(--color-primary-500);
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.15);
}
.side-panel-actions {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: var(--spacing-3);
}
.side-panel-actions-right {
  display: flex;
  gap: var(--spacing-2);
}

/* Side panel transition */
.side-panel-enter-active,
.side-panel-leave-active {
  transition: opacity 0.2s ease;
}
.side-panel-enter-active .side-panel,
.side-panel-leave-active .side-panel {
  transition: transform 0.2s ease;
}
.side-panel-enter-from,
.side-panel-leave-to {
  opacity: 0;
}
.side-panel-enter-from .side-panel,
.side-panel-leave-to .side-panel {
  transform: translateX(100%);
}
</style>
