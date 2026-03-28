<script setup lang="ts">
import AppEmptyState from '~/components/AppEmptyState.vue'
import AppSkeleton from '~/components/AppSkeleton.vue'
import UiButton from '~/components/ui/Button.vue'
import UiInput from '~/components/ui/Input.vue'
import UiSelect from '~/components/ui/Select.vue'
import { versionsClient } from '~/api/versionsClient'
import { contentClient } from '~/api/contentClient'
import type { ProjectVersion, VersionDiff } from '~/api/types'

definePageMeta({ layout: 'app' })
useSeoMeta({ title: 'Releases - InterCopy' })

const route = useRoute()
const auth = useAuth()

const projectId = computed(() => route.params.projectId as string)

// State
const isLoading = ref(false)
const versions = ref<ProjectVersion[]>([])
const loadError = ref('')
const actionError = ref('')

// Content count for snapshot preview
const workingCopyCount = ref(0)

// Create modal state
const showCreateModal = ref(false)
const createTag = ref('')
const createTitle = ref('')
const createNotes = ref('')
const createError = ref('')
const isCreating = ref(false)

// Edit modal state
const showEditModal = ref(false)
const editVersionId = ref('')
const editTitle = ref('')
const editNotes = ref('')
const editError = ref('')
const isEditing = ref(false)

// Expanded notes tracking
const expandedNotes = ref<Set<string>>(new Set())

// Confirm dialogs
const confirmAction = ref<{ type: 'promote' | 'demote' | 'delete'; versionId: string; tag: string } | null>(null)

// Compare state
const showCompare = ref(false)
const compareFromId = ref('')
const compareToId = ref('')
const compareDiff = ref<VersionDiff | null>(null)
const isComparing = ref(false)
const compareError = ref('')

const liveVersion = computed(() => versions.value.find(v => v.isLive))
const sortedVersions = computed(() => [...versions.value].sort((a, b) => new Date(b.createdUtc).getTime() - new Date(a.createdUtc).getTime()))

async function loadVersions() {
  isLoading.value = true
  loadError.value = ''
  try {
    const data = await versionsClient.list(projectId.value)
    versions.value = Array.isArray(data) ? data : []
  } catch (error: any) {
    loadError.value = error?.message || 'Failed to load versions'
    versions.value = []
  } finally {
    isLoading.value = false
  }
}

async function loadWorkingCopyCount() {
  try {
    const items = await contentClient.list(projectId.value)
    workingCopyCount.value = Array.isArray(items) ? items.length : 0
  } catch {
    workingCopyCount.value = 0
  }
}

function openCreateModal() {
  createTag.value = ''
  createTitle.value = ''
  createNotes.value = ''
  createError.value = ''
  showCreateModal.value = true
  loadWorkingCopyCount()
}

function closeCreateModal() {
  showCreateModal.value = false
}

async function handleCreate() {
  const tag = createTag.value.trim()
  const title = createTitle.value.trim()
  if (!tag) {
    createError.value = 'Version tag is required'
    return
  }
  if (!title) {
    createError.value = 'Release title is required'
    return
  }

  isCreating.value = true
  createError.value = ''
  try {
    await versionsClient.create(projectId.value, tag, title, createNotes.value.trim())
    closeCreateModal()
    await loadVersions()
  } catch (error: any) {
    createError.value = error?.message || 'Failed to create release'
  } finally {
    isCreating.value = false
  }
}

function openEditModal(version: ProjectVersion) {
  editVersionId.value = version.id
  editTitle.value = version.title
  editNotes.value = version.notes
  editError.value = ''
  showEditModal.value = true
}

function closeEditModal() {
  showEditModal.value = false
}

async function handleEdit() {
  const title = editTitle.value.trim()
  if (!title) {
    editError.value = 'Release title is required'
    return
  }

  isEditing.value = true
  editError.value = ''
  try {
    await versionsClient.update(projectId.value, editVersionId.value, title, editNotes.value.trim())
    closeEditModal()
    await loadVersions()
  } catch (error: any) {
    editError.value = error?.message || 'Failed to update release'
  } finally {
    isEditing.value = false
  }
}

function requestConfirm(type: 'promote' | 'demote' | 'delete', version: ProjectVersion) {
  confirmAction.value = { type, versionId: version.id, tag: version.tag }
}

function cancelConfirm() {
  confirmAction.value = null
}

async function executeConfirm() {
  if (!confirmAction.value) return
  const { type, versionId } = confirmAction.value
  actionError.value = ''

  try {
    if (type === 'promote') {
      await versionsClient.promote(projectId.value, versionId)
    } else if (type === 'demote') {
      await versionsClient.demote(projectId.value, versionId)
    } else if (type === 'delete') {
      await versionsClient.remove(projectId.value, versionId)
    }
    confirmAction.value = null
    await loadVersions()
  } catch (error: any) {
    actionError.value = error?.message || `Failed to ${type} version`
    confirmAction.value = null
  }
}

function toggleNotes(versionId: string) {
  if (expandedNotes.value.has(versionId)) {
    expandedNotes.value.delete(versionId)
  } else {
    expandedNotes.value.add(versionId)
  }
}

function formatDate(dateStr: string): string {
  const date = new Date(dateStr)
  return date.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })
}

const confirmMessage = computed(() => {
  if (!confirmAction.value) return ''
  const { type, tag } = confirmAction.value
  if (type === 'promote') return `Promote "${tag}" to live? This will replace the current live version. CLI and export endpoints will serve content from this release.`
  if (type === 'demote') return `Demote "${tag}" from live? Exports will fall back to the working copy.`
  if (type === 'delete') return `Delete release "${tag}"? This action cannot be undone. All snapshot data for this version will be permanently removed.`
  return ''
})

const versionSelectOptions = computed(() =>
  sortedVersions.value.map(v => ({ value: v.id, label: `${v.tag}${v.isLive ? ' (LIVE)' : ''}` }))
)

function toggleCompare() {
  showCompare.value = !showCompare.value
  if (!showCompare.value) {
    compareDiff.value = null
    compareError.value = ''
  }
}

async function runCompare() {
  if (!compareFromId.value || !compareToId.value) {
    compareError.value = 'Select two versions to compare'
    return
  }
  if (compareFromId.value === compareToId.value) {
    compareError.value = 'Select two different versions'
    return
  }

  isComparing.value = true
  compareError.value = ''
  compareDiff.value = null
  try {
    compareDiff.value = await versionsClient.compare(projectId.value, compareFromId.value, compareToId.value)
  } catch (error: any) {
    compareError.value = error?.message || 'Failed to compare versions'
  } finally {
    isComparing.value = false
  }
}

const diffTotalCount = computed(() => {
  if (!compareDiff.value) return 0
  return compareDiff.value.added.length + compareDiff.value.removed.length + compareDiff.value.changed.length
})

onMounted(loadVersions)
</script>

<template>
  <div class="versions-page">
    <header class="page-header">
      <div>
        <div class="page-header-breadcrumb">
          <NuxtLink to="/app/projects" class="breadcrumb-link">Projects</NuxtLink>
          <span class="breadcrumb-separator">/</span>
          <span>Releases</span>
        </div>
        <h1>Releases</h1>
        <p class="page-subtitle">Manage versioned snapshots of your project content</p>
      </div>
      <div class="page-header-actions">
        <UiButton v-if="versions.length >= 2" variant="secondary" @click="toggleCompare">
          {{ showCompare ? 'Hide Compare' : 'Compare Versions' }}
        </UiButton>
        <UiButton @click="openCreateModal">Create Release</UiButton>
      </div>
    </header>

    <!-- Action error banner -->
    <div v-if="actionError" class="action-error" role="alert">
      <span>{{ actionError }}</span>
      <button class="action-error-dismiss" aria-label="Dismiss" @click="actionError = ''">&times;</button>
    </div>

    <!-- Loading -->
    <div v-if="isLoading" class="version-list">
      <div v-for="i in 3" :key="i" class="version-card">
        <AppSkeleton lines="3" height="1.5rem" />
      </div>
    </div>

    <!-- Error -->
    <div v-else-if="loadError" class="load-error">
      <p>{{ loadError }}</p>
      <UiButton variant="secondary" @click="loadVersions">Retry</UiButton>
    </div>

    <!-- Empty state -->
    <AppEmptyState
      v-else-if="versions.length === 0"
      title="No releases yet"
      description="Create your first release to snapshot the current working copy. Releases freeze content at a point in time."
    >
      <template #action>
        <UiButton @click="openCreateModal">Create Release</UiButton>
      </template>
    </AppEmptyState>

    <!-- Version list -->
    <template v-else>
      <!-- Live version banner -->
      <div v-if="liveVersion" class="live-banner">
        <div class="live-banner-content">
          <span class="live-badge" aria-label="Live version">
            <svg class="live-badge-icon" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
              <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
            </svg>
            LIVE
          </span>
          <span class="live-banner-tag">{{ liveVersion.tag }}</span>
          <span class="live-banner-title">{{ liveVersion.title }}</span>
        </div>
        <span class="live-banner-meta">{{ liveVersion.contentItemCount }} content items &middot; {{ liveVersion.translationCount }} translations</span>
      </div>

      <div class="version-list">
        <article
          v-for="version in sortedVersions"
          :key="version.id"
          class="version-card"
          :class="{ 'version-card--live': version.isLive }"
        >
          <div class="version-card-header">
            <div class="version-card-title-row">
              <code class="version-tag">{{ version.tag }}</code>
              <span v-if="version.isLive" class="live-badge" aria-label="This is the live version">
                <svg class="live-badge-icon" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                  <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
                </svg>
                LIVE
              </span>
            </div>
            <h3 class="version-title">{{ version.title }}</h3>
          </div>

          <!-- Collapsible notes -->
          <div v-if="version.notes" class="version-notes-section">
            <button
              class="version-notes-toggle"
              :aria-expanded="expandedNotes.has(version.id)"
              @click="toggleNotes(version.id)"
            >
              <svg
                class="version-notes-chevron"
                :class="{ 'version-notes-chevron--open': expandedNotes.has(version.id) }"
                viewBox="0 0 20 20"
                fill="currentColor"
                aria-hidden="true"
              >
                <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd" />
              </svg>
              Release notes
            </button>
            <div v-if="expandedNotes.has(version.id)" class="version-notes-content">
              {{ version.notes }}
            </div>
          </div>

          <div class="version-card-meta">
            <span class="version-stat">{{ version.contentItemCount }} content items</span>
            <span class="version-stat-sep">&middot;</span>
            <span class="version-stat">{{ version.translationCount }} translations</span>
            <span class="version-stat-sep">&middot;</span>
            <span class="version-meta-info">{{ version.createdByEmail }}</span>
            <span class="version-stat-sep">&middot;</span>
            <time class="version-meta-info" :datetime="version.createdUtc">{{ formatDate(version.createdUtc) }}</time>
          </div>

          <div class="version-card-actions">
            <UiButton
              v-if="!version.isLive"
              size="sm"
              variant="primary"
              @click="requestConfirm('promote', version)"
            >
              Promote to Live
            </UiButton>
            <UiButton
              v-else
              size="sm"
              variant="secondary"
              @click="requestConfirm('demote', version)"
            >
              Demote
            </UiButton>
            <UiButton size="sm" variant="ghost" @click="openEditModal(version)">Edit</UiButton>
            <UiButton
              v-if="auth.isAdmin.value"
              size="sm"
              variant="danger"
              :disabled="version.isLive"
              @click="requestConfirm('delete', version)"
            >
              Delete
            </UiButton>
          </div>
        </article>
      </div>

      <!-- Compare section -->
      <div v-if="showCompare" class="compare-section">
        <h2 class="compare-title">Compare Versions</h2>
        <p class="compare-subtitle">Select two versions to see what changed between them</p>

        <div class="compare-selectors">
          <div class="compare-field">
            <UiSelect
              id="compareFrom"
              v-model="compareFromId"
              label="From (older)"
              :options="[{ value: '', label: 'Select version' }, ...versionSelectOptions]"
            />
          </div>
          <span class="compare-arrow" aria-hidden="true">
            <svg viewBox="0 0 20 20" fill="currentColor" width="20" height="20">
              <path fill-rule="evenodd" d="M12.293 5.293a1 1 0 011.414 0l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414-1.414L14.586 11H3a1 1 0 110-2h11.586l-2.293-2.293a1 1 0 010-1.414z" clip-rule="evenodd" />
            </svg>
          </span>
          <div class="compare-field">
            <UiSelect
              id="compareTo"
              v-model="compareToId"
              label="To (newer)"
              :options="[{ value: '', label: 'Select version' }, ...versionSelectOptions]"
            />
          </div>
          <UiButton :loading="isComparing" :disabled="!compareFromId || !compareToId" @click="runCompare">
            Compare
          </UiButton>
        </div>

        <p v-if="compareError" class="field-error" role="alert">{{ compareError }}</p>

        <div v-if="compareDiff" class="compare-results">
          <p class="compare-summary">
            {{ diffTotalCount }} difference{{ diffTotalCount !== 1 ? 's' : '' }} found:
            <span v-if="compareDiff.added.length" class="diff-count diff-count--added">{{ compareDiff.added.length }} added</span>
            <span v-if="compareDiff.removed.length" class="diff-count diff-count--removed">{{ compareDiff.removed.length }} removed</span>
            <span v-if="compareDiff.changed.length" class="diff-count diff-count--changed">{{ compareDiff.changed.length }} changed</span>
          </p>

          <!-- Added keys -->
          <div v-if="compareDiff.added.length" class="diff-group">
            <h3 class="diff-group-title diff-group-title--added">Added keys</h3>
            <div v-for="item in compareDiff.added" :key="'add-' + item.key" class="diff-row diff-row--added">
              <code class="diff-key">{{ item.key }}</code>
              <span class="diff-source">{{ item.source }}</span>
            </div>
          </div>

          <!-- Removed keys -->
          <div v-if="compareDiff.removed.length" class="diff-group">
            <h3 class="diff-group-title diff-group-title--removed">Removed keys</h3>
            <div v-for="item in compareDiff.removed" :key="'rem-' + item.key" class="diff-row diff-row--removed">
              <code class="diff-key">{{ item.key }}</code>
              <span class="diff-source">{{ item.source }}</span>
            </div>
          </div>

          <!-- Changed keys -->
          <div v-if="compareDiff.changed.length" class="diff-group">
            <h3 class="diff-group-title diff-group-title--changed">Changed keys</h3>
            <div v-for="item in compareDiff.changed" :key="'chg-' + item.key" class="diff-row diff-row--changed">
              <code class="diff-key">{{ item.key }}</code>
              <div class="diff-changes">
                <div class="diff-old">
                  <span class="diff-label">Before:</span>
                  <span>{{ item.oldSource }}</span>
                </div>
                <div class="diff-new">
                  <span class="diff-label">After:</span>
                  <span>{{ item.newSource }}</span>
                </div>
              </div>
            </div>
          </div>

          <p v-if="diffTotalCount === 0" class="compare-empty">No differences found between these versions.</p>
        </div>
      </div>
    </template>

    <!-- Create Release modal -->
    <div v-if="showCreateModal" class="modal-overlay" @click.self="closeCreateModal">
      <div class="modal" role="dialog" aria-labelledby="create-modal-title">
        <h2 id="create-modal-title">Create Release</h2>
        <p class="modal-subtitle">Snapshot the current working copy as a new release</p>

        <div class="modal-form">
          <div class="form-field">
            <label for="create-tag" class="form-label">Version tag</label>
            <span class="form-hint">e.g. v1.0.0, 2024-Q1-release</span>
            <input id="create-tag" v-model="createTag" type="text" class="form-input form-input--mono" autocomplete="off">
          </div>

          <div class="form-field">
            <label for="create-title" class="form-label">Release title</label>
            <span class="form-hint">Human-readable name for this release</span>
            <input id="create-title" v-model="createTitle" type="text" class="form-input" autocomplete="off">
          </div>

          <div class="form-field">
            <label for="create-notes" class="form-label">Release notes</label>
            <span class="form-hint">Describe what changed in this version</span>
            <textarea id="create-notes" v-model="createNotes" class="form-textarea" rows="4" />
          </div>

          <div class="snapshot-preview">
            <svg class="snapshot-preview-icon" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
              <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
            </svg>
            This release will snapshot <strong>{{ workingCopyCount }}</strong> content item{{ workingCopyCount !== 1 ? 's' : '' }} from the working copy.
          </div>

          <p v-if="createError" class="field-error" role="alert">{{ createError }}</p>
        </div>

        <div class="modal-actions">
          <UiButton variant="secondary" @click="closeCreateModal">Cancel</UiButton>
          <UiButton :loading="isCreating" @click="handleCreate">Create Release</UiButton>
        </div>
      </div>
    </div>

    <!-- Edit modal -->
    <div v-if="showEditModal" class="modal-overlay" @click.self="closeEditModal">
      <div class="modal" role="dialog" aria-labelledby="edit-modal-title">
        <h2 id="edit-modal-title">Edit Release</h2>

        <div class="modal-form">
          <div class="form-field">
            <label for="edit-title" class="form-label">Release title</label>
            <span class="form-hint">Human-readable name for this release</span>
            <input id="edit-title" v-model="editTitle" type="text" class="form-input" autocomplete="off">
          </div>

          <div class="form-field">
            <label for="edit-notes" class="form-label">Release notes</label>
            <span class="form-hint">Describe what changed in this version</span>
            <textarea id="edit-notes" v-model="editNotes" class="form-textarea" rows="4" />
          </div>

          <p v-if="editError" class="field-error" role="alert">{{ editError }}</p>
        </div>

        <div class="modal-actions">
          <UiButton variant="secondary" @click="closeEditModal">Cancel</UiButton>
          <UiButton :loading="isEditing" @click="handleEdit">Save Changes</UiButton>
        </div>
      </div>
    </div>

    <!-- Confirm dialog -->
    <div v-if="confirmAction" class="modal-overlay" @click.self="cancelConfirm">
      <div class="modal modal--sm" role="alertdialog" aria-labelledby="confirm-title">
        <h2 id="confirm-title">
          {{ confirmAction.type === 'promote' ? 'Promote to Live' : confirmAction.type === 'demote' ? 'Demote Version' : 'Delete Release' }}
        </h2>
        <p class="confirm-message">{{ confirmMessage }}</p>
        <div class="modal-actions">
          <UiButton variant="secondary" @click="cancelConfirm">Cancel</UiButton>
          <UiButton
            :variant="confirmAction.type === 'delete' ? 'danger' : 'primary'"
            @click="executeConfirm"
          >
            {{ confirmAction.type === 'promote' ? 'Promote' : confirmAction.type === 'demote' ? 'Demote' : 'Delete' }}
          </UiButton>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.versions-page {
  max-width: 1200px;
}

/* Header */
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
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

.page-header-breadcrumb {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
  margin-bottom: var(--spacing-2);
}

.breadcrumb-link {
  color: var(--color-primary-600);
  text-decoration: none;
}

.breadcrumb-link:hover {
  text-decoration: underline;
}

.breadcrumb-separator {
  color: var(--color-text-muted);
}

/* Action error banner */
.action-error {
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: color-mix(in srgb, var(--color-error) 10%, transparent);
  border: 1px solid var(--color-error);
  border-radius: var(--radius-lg);
  padding: var(--spacing-3) var(--spacing-4);
  margin-bottom: var(--spacing-4);
  color: var(--color-error);
  font-size: var(--font-size-sm);
}

.action-error-dismiss {
  background: none;
  border: none;
  color: var(--color-error);
  cursor: pointer;
  font-size: 1.25rem;
  line-height: 1;
  padding: 0 var(--spacing-1);
}

/* Load error */
.load-error {
  text-align: center;
  padding: var(--spacing-8);
  color: var(--color-text-muted);
}

.load-error p {
  margin: 0 0 var(--spacing-4);
}

/* Live banner */
.live-banner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: var(--spacing-3);
  background: color-mix(in srgb, #16a34a 8%, transparent);
  border: 1px solid color-mix(in srgb, #16a34a 30%, transparent);
  border-radius: var(--radius-xl);
  padding: var(--spacing-4) var(--spacing-5);
  margin-bottom: var(--spacing-5);
}

.live-banner-content {
  display: flex;
  align-items: center;
  gap: var(--spacing-3);
}

.live-banner-tag {
  font-family: monospace;
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
}

.live-banner-title {
  color: var(--color-text-secondary);
}

.live-banner-meta {
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
}

/* Live badge */
.live-badge {
  display: inline-flex;
  align-items: center;
  gap: var(--spacing-1);
  padding: 2px var(--spacing-2);
  background: #16a34a;
  color: #fff;
  border-radius: var(--radius-md);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  line-height: 1.4;
  white-space: nowrap;
}

.live-badge-icon {
  width: 0.875em;
  height: 0.875em;
}

/* Version list */
.version-list {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-4);
}

/* Version card */
.version-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-xl);
  padding: var(--spacing-5);
}

.version-card--live {
  border-color: color-mix(in srgb, #16a34a 40%, transparent);
}

.version-card-header {
  margin-bottom: var(--spacing-3);
}

.version-card-title-row {
  display: flex;
  align-items: center;
  gap: var(--spacing-3);
  margin-bottom: var(--spacing-2);
}

.version-tag {
  font-family: monospace;
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  background: var(--color-background);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  padding: var(--spacing-1) var(--spacing-2);
}

.version-title {
  margin: 0;
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
}

/* Notes section */
.version-notes-section {
  margin-bottom: var(--spacing-3);
}

.version-notes-toggle {
  display: inline-flex;
  align-items: center;
  gap: var(--spacing-1);
  background: none;
  border: none;
  cursor: pointer;
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
  padding: 0;
}

.version-notes-toggle:hover {
  color: var(--color-text-primary);
}

.version-notes-chevron {
  width: 1em;
  height: 1em;
  transition: transform var(--transition-fast);
}

.version-notes-chevron--open {
  transform: rotate(90deg);
}

.version-notes-content {
  margin-top: var(--spacing-2);
  padding: var(--spacing-3) var(--spacing-4);
  background: var(--color-background);
  border-radius: var(--radius-lg);
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  white-space: pre-wrap;
  word-break: break-word;
}

/* Meta */
.version-card-meta {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: var(--spacing-2);
  font-size: var(--font-size-sm);
  margin-bottom: var(--spacing-3);
}

.version-stat {
  color: var(--color-text-secondary);
}

.version-stat-sep {
  color: var(--color-text-muted);
}

.version-meta-info {
  color: var(--color-text-muted);
}

/* Actions */
.version-card-actions {
  display: flex;
  gap: var(--spacing-2);
  flex-wrap: wrap;
}

/* Modals */
.modal-overlay {
  position: fixed;
  inset: 0;
  background: color-mix(in srgb, var(--color-black) 45%, transparent);
  display: grid;
  place-items: center;
  z-index: var(--z-modal);
}

.modal {
  width: min(560px, 92vw);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-xl);
  padding: var(--spacing-6);
}

.modal--sm {
  width: min(440px, 92vw);
}

.modal h2 {
  margin: 0 0 var(--spacing-2);
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
}

.modal-subtitle {
  margin: 0 0 var(--spacing-4);
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
}

.modal-form {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-4);
  margin-bottom: var(--spacing-5);
}

.form-field {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.form-label {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-primary);
}

.form-hint {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  margin-bottom: var(--spacing-1);
}

.form-input,
.form-textarea {
  padding: var(--spacing-3) var(--spacing-4);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  background: var(--color-background);
  color: var(--color-text-primary);
  font-family: inherit;
  font-size: var(--font-size-base);
}

.form-input:focus,
.form-textarea:focus {
  outline: none;
  border-color: var(--color-primary-500);
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.15);
}

.form-input--mono {
  font-family: monospace;
}

.form-textarea {
  resize: vertical;
  min-height: 80px;
}

.snapshot-preview {
  display: flex;
  align-items: flex-start;
  gap: var(--spacing-2);
  padding: var(--spacing-3) var(--spacing-4);
  background: color-mix(in srgb, var(--color-primary-600) 8%, transparent);
  border-radius: var(--radius-lg);
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

.snapshot-preview-icon {
  width: 1.25em;
  height: 1.25em;
  flex-shrink: 0;
  margin-top: 1px;
  color: var(--color-primary-600);
}

.field-error {
  margin: 0;
  color: var(--color-error);
  font-size: var(--font-size-xs);
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-2);
}

.confirm-message {
  margin: 0 0 var(--spacing-5);
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  line-height: 1.5;
}

/* Responsive: cards stack */
@media (max-width: 640px) {
  .page-header {
    flex-direction: column;
    gap: var(--spacing-3);
  }

  .live-banner {
    flex-direction: column;
    align-items: flex-start;
  }

  .version-card-meta {
    flex-direction: column;
    align-items: flex-start;
    gap: var(--spacing-1);
  }

  .version-stat-sep {
    display: none;
  }

  .compare-selectors {
    flex-direction: column;
  }

  .compare-arrow {
    transform: rotate(90deg);
  }
}

/* Header actions */
.page-header-actions {
  display: flex;
  gap: var(--spacing-2);
  align-items: center;
}

/* Compare section */
.compare-section {
  margin-top: var(--spacing-6);
  padding: var(--spacing-5);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-xl);
}

.compare-title {
  margin: 0 0 var(--spacing-1);
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
}

.compare-subtitle {
  margin: 0 0 var(--spacing-4);
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
}

.compare-selectors {
  display: flex;
  align-items: flex-end;
  gap: var(--spacing-3);
  margin-bottom: var(--spacing-4);
  flex-wrap: wrap;
}

.compare-field {
  flex: 1;
  min-width: 180px;
}

.compare-arrow {
  color: var(--color-text-muted);
  flex-shrink: 0;
  align-self: flex-end;
  margin-bottom: var(--spacing-2);
}

.compare-results {
  margin-top: var(--spacing-4);
}

.compare-summary {
  margin: 0 0 var(--spacing-4);
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
  flex-wrap: wrap;
}

.diff-count {
  padding: 1px var(--spacing-2);
  border-radius: var(--radius-md);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
}

.diff-count--added {
  background: color-mix(in srgb, #16a34a 12%, transparent);
  color: #16a34a;
}

.diff-count--removed {
  background: color-mix(in srgb, var(--color-error) 12%, transparent);
  color: var(--color-error);
}

.diff-count--changed {
  background: color-mix(in srgb, #d97706 12%, transparent);
  color: #d97706;
}

.diff-group {
  margin-bottom: var(--spacing-4);
}

.diff-group-title {
  margin: 0 0 var(--spacing-2);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
}

.diff-group-title--added { color: #16a34a; }
.diff-group-title--removed { color: var(--color-error); }
.diff-group-title--changed { color: #d97706; }

.diff-row {
  padding: var(--spacing-3) var(--spacing-4);
  border-radius: var(--radius-lg);
  margin-bottom: var(--spacing-2);
  border-left: 3px solid transparent;
}

.diff-row--added {
  background: color-mix(in srgb, #16a34a 6%, transparent);
  border-left-color: #16a34a;
}

.diff-row--removed {
  background: color-mix(in srgb, var(--color-error) 6%, transparent);
  border-left-color: var(--color-error);
}

.diff-row--changed {
  background: color-mix(in srgb, #d97706 6%, transparent);
  border-left-color: #d97706;
}

.diff-key {
  font-family: monospace;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  display: block;
  margin-bottom: var(--spacing-1);
}

.diff-source {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

.diff-changes {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-1);
}

.diff-old,
.diff-new {
  font-size: var(--font-size-sm);
  display: flex;
  gap: var(--spacing-2);
}

.diff-label {
  font-weight: var(--font-weight-medium);
  color: var(--color-text-muted);
  white-space: nowrap;
}

.diff-old span:last-child {
  color: var(--color-error);
  text-decoration: line-through;
}

.diff-new span:last-child {
  color: #16a34a;
}

.compare-empty {
  margin: 0;
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
  text-align: center;
  padding: var(--spacing-4);
}
</style>
