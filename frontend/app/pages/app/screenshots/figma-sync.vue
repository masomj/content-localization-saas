<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { figmaSyncClient } from '~/api/figmaSyncClient'
import { projectsClient } from '~/api/projectsClient'
import type { FigmaScreenshotSync, Project } from '~/api/types'
import { useAuth } from '~/composables/useAuth'
import UiButton from '~/components/ui/Button.vue'
import UiCard from '~/components/ui/Card.vue'

definePageMeta({ layout: 'app', middleware: ['feature-flags'] })

const auth = useAuth()
const workspaceId = computed(() => auth.workspace.value?.id ?? '')

const projects = ref<Project[]>([])
const selectedProjectId = ref('')
const syncs = ref<FigmaScreenshotSync[]>([])
const loading = ref(false)
const error = ref('')

// Connect modal
const showConnectModal = ref(false)
const fileKeyInput = ref('')
const connecting = ref(false)
const connectError = ref('')

// Delete modal
const showDeleteModal = ref(false)
const deleteTarget = ref<FigmaScreenshotSync | null>(null)
const deleting = ref(false)

async function loadProjects() {
  if (!workspaceId.value) return
  try {
    projects.value = await projectsClient.list(workspaceId.value)
    if (projects.value.length > 0 && !selectedProjectId.value) {
      selectedProjectId.value = projects.value[0].id
    }
  } catch (err: any) {
    error.value = err?.message ?? 'Failed to load projects'
  }
}

async function loadSyncs() {
  if (!selectedProjectId.value) return
  loading.value = true
  error.value = ''
  try {
    syncs.value = await figmaSyncClient.list(selectedProjectId.value)
  } catch (err: any) {
    error.value = err?.message ?? 'Failed to load Figma connections'
  } finally {
    loading.value = false
  }
}

async function connectFile() {
  if (!fileKeyInput.value.trim() || !selectedProjectId.value) return
  connecting.value = true
  connectError.value = ''
  try {
    await figmaSyncClient.connect(selectedProjectId.value, fileKeyInput.value.trim())
    fileKeyInput.value = ''
    showConnectModal.value = false
    await loadSyncs()
  } catch (err: any) {
    connectError.value = err?.message ?? 'Failed to connect Figma file'
  } finally {
    connecting.value = false
  }
}

async function triggerSync(sync: FigmaScreenshotSync) {
  try {
    const updated = await figmaSyncClient.sync(selectedProjectId.value, sync.id)
    const idx = syncs.value.findIndex(s => s.id === sync.id)
    if (idx >= 0) syncs.value[idx] = updated
  } catch (_) { /* ignore */ }
}

function confirmDelete(sync: FigmaScreenshotSync) {
  deleteTarget.value = sync
  showDeleteModal.value = true
}

async function performDelete() {
  if (!deleteTarget.value) return
  deleting.value = true
  try {
    await figmaSyncClient.delete(selectedProjectId.value, deleteTarget.value.id)
    showDeleteModal.value = false
    deleteTarget.value = null
    await loadSyncs()
  } catch (_) { /* ignore */ }
  finally {
    deleting.value = false
  }
}

function statusBadgeClass(status: string): string {
  switch (status) {
    case 'idle': return 'badge--idle'
    case 'syncing': return 'badge--syncing'
    case 'completed': return 'badge--completed'
    case 'failed': return 'badge--failed'
    default: return ''
  }
}

function formatDate(d: string | null): string {
  if (!d) return 'Never'
  return new Date(d).toLocaleString(undefined, {
    month: 'short', day: 'numeric', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  })
}

watch(workspaceId, loadProjects, { immediate: true })
watch(selectedProjectId, loadSyncs)

onMounted(async () => {
  await loadProjects()
  await loadSyncs()
})
</script>

<template>
  <div class="figma-sync-page">
    <header class="figma-sync-page__header">
      <div>
        <h1 class="figma-sync-page__title">Figma Screenshot Sync</h1>
        <p class="figma-sync-page__subtitle">Connect Figma files to automatically sync screenshots for visual context.</p>
      </div>

      <div class="figma-sync-page__actions">
        <label class="figma-sync-page__select-label">
          <span>Project</span>
          <select v-model="selectedProjectId" class="figma-sync-page__select">
            <option v-for="p in projects" :key="p.id" :value="p.id">{{ p.name }}</option>
          </select>
        </label>
        <UiButton variant="primary" size="sm" @click="showConnectModal = true">Connect Figma File</UiButton>
      </div>
    </header>

    <!-- Loading -->
    <div v-if="loading" class="figma-sync-page__loading">Loading connections&hellip;</div>

    <!-- Error -->
    <div v-else-if="error" class="figma-sync-page__error" role="alert">
      {{ error }}
      <UiButton variant="secondary" size="sm" @click="loadSyncs">Retry</UiButton>
    </div>

    <!-- Empty -->
    <div v-else-if="syncs.length === 0" class="figma-sync-page__empty">
      <p>No Figma files connected yet.</p>
      <p class="figma-sync-page__empty-hint">Click "Connect Figma File" to get started.</p>
    </div>

    <!-- Sync list -->
    <div v-else class="figma-sync-page__grid">
      <UiCard v-for="sync in syncs" :key="sync.id" padding="md" class="sync-card">
        <div class="sync-card__header">
          <h2 class="sync-card__name">{{ sync.figmaFileName || sync.figmaFileKey }}</h2>
          <span :class="['sync-card__badge', statusBadgeClass(sync.syncStatus)]">
            <span v-if="sync.syncStatus === 'syncing'" class="sync-card__spinner" aria-hidden="true"></span>
            {{ sync.syncStatus }}
          </span>
        </div>

        <div class="sync-card__meta">
          <div class="sync-card__meta-item">
            <span class="sync-card__meta-label">File Key</span>
            <code class="sync-card__meta-value">{{ sync.figmaFileKey }}</code>
          </div>
          <div class="sync-card__meta-item">
            <span class="sync-card__meta-label">Last Sync</span>
            <span class="sync-card__meta-value">{{ formatDate(sync.lastSyncUtc) }}</span>
          </div>
          <div class="sync-card__meta-item">
            <span class="sync-card__meta-label">Frames</span>
            <span class="sync-card__meta-value">{{ sync.frameCount }}</span>
          </div>
        </div>

        <div class="sync-card__actions">
          <UiButton variant="primary" size="sm" @click="triggerSync(sync)" :disabled="sync.syncStatus === 'syncing'">
            Sync Now
          </UiButton>
          <UiButton variant="danger" size="sm" @click="confirmDelete(sync)">Remove</UiButton>
        </div>
      </UiCard>
    </div>

    <!-- Connect modal -->
    <Teleport to="body">
      <div v-if="showConnectModal" class="modal-overlay" @click.self="showConnectModal = false">
        <div class="modal-dialog" role="dialog" aria-modal="true" aria-labelledby="connect-modal-title">
          <h2 id="connect-modal-title" class="modal-dialog__title">Connect Figma File</h2>

          <div class="modal-dialog__field">
            <label for="figma-file-key" class="form-label">Figma File Key</label>
            <p class="form-helper">The file key from the Figma URL, e.g. the part after /file/ in figma.com/file/ABC123/...</p>
            <input
              id="figma-file-key"
              v-model="fileKeyInput"
              type="text"
              class="form-input"
              placeholder="e.g. ABC123def456"
            />
          </div>

          <div v-if="connectError" class="modal-dialog__error" role="alert">{{ connectError }}</div>

          <div class="modal-dialog__actions">
            <UiButton variant="secondary" size="sm" @click="showConnectModal = false">Cancel</UiButton>
            <UiButton variant="primary" size="sm" :loading="connecting" :disabled="!fileKeyInput.trim()" @click="connectFile">
              Connect
            </UiButton>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Delete confirmation modal -->
    <Teleport to="body">
      <div v-if="showDeleteModal" class="modal-overlay" @click.self="showDeleteModal = false">
        <div class="modal-dialog" role="dialog" aria-modal="true" aria-labelledby="delete-modal-title">
          <h2 id="delete-modal-title" class="modal-dialog__title">Remove Connection</h2>
          <p class="modal-dialog__body">
            Are you sure you want to remove the connection to
            <strong>{{ deleteTarget?.figmaFileName || deleteTarget?.figmaFileKey }}</strong>?
            This will not delete any previously synced screenshots.
          </p>
          <div class="modal-dialog__actions">
            <UiButton variant="secondary" size="sm" @click="showDeleteModal = false">Cancel</UiButton>
            <UiButton variant="danger" size="sm" :loading="deleting" @click="performDelete">Remove</UiButton>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
.figma-sync-page {
  max-width: 960px;
}

.figma-sync-page__header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: var(--spacing-6);
  gap: var(--spacing-4);
}

.figma-sync-page__title {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  color: var(--color-text-primary);
  margin: 0 0 var(--spacing-1) 0;
}

.figma-sync-page__subtitle {
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
  margin: 0;
}

.figma-sync-page__actions {
  display: flex;
  align-items: center;
  gap: var(--spacing-3);
}

.figma-sync-page__select-label {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

.figma-sync-page__select {
  padding: var(--spacing-1) var(--spacing-2);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-surface);
  color: var(--color-text-primary);
  font-size: var(--font-size-sm);
}

.figma-sync-page__loading,
.figma-sync-page__empty {
  text-align: center;
  padding: var(--spacing-12);
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.figma-sync-page__empty-hint {
  font-size: var(--font-size-xs);
  margin-top: var(--spacing-2);
}

.figma-sync-page__error {
  background: var(--color-error);
  color: #fff;
  padding: var(--spacing-3);
  border-radius: var(--radius-md);
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
  font-size: var(--font-size-sm);
}

.figma-sync-page__grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: var(--spacing-4);
}

/* Sync card */
.sync-card__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-3);
}

.sync-card__name {
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  margin: 0;
}

.sync-card__badge {
  display: inline-flex;
  align-items: center;
  gap: var(--spacing-1);
  padding: var(--spacing-1) var(--spacing-2);
  border-radius: var(--radius-full);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  text-transform: capitalize;
}

.badge--idle {
  background: var(--color-gray-100);
  color: var(--color-gray-600);
}
.badge--syncing {
  background: var(--color-primary-100);
  color: var(--color-primary-700);
}
.badge--completed {
  background: color-mix(in srgb, var(--color-success) 15%, transparent);
  color: var(--color-success);
}
.badge--failed {
  background: color-mix(in srgb, var(--color-error) 15%, transparent);
  color: var(--color-error);
}

.sync-card__spinner {
  width: 0.75rem;
  height: 0.75rem;
  border: 2px solid currentColor;
  border-top-color: transparent;
  border-radius: var(--radius-full);
  animation: figma-spin 0.8s linear infinite;
}

@keyframes figma-spin {
  to { transform: rotate(360deg); }
}

.sync-card__meta {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-2);
  margin-bottom: var(--spacing-3);
}

.sync-card__meta-item {
  display: flex;
  justify-content: space-between;
  font-size: var(--font-size-sm);
}

.sync-card__meta-label {
  color: var(--color-text-muted);
}

.sync-card__meta-value {
  color: var(--color-text-primary);
  font-size: var(--font-size-sm);
}

.sync-card__actions {
  display: flex;
  gap: var(--spacing-2);
}

/* Modal */
.modal-overlay {
  position: fixed;
  inset: 0;
  background: color-mix(in srgb, var(--color-gray-950) 60%, transparent);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: var(--z-modal-backdrop);
}

.modal-dialog {
  background: var(--color-surface);
  border-radius: var(--radius-xl);
  padding: var(--spacing-6);
  width: 100%;
  max-width: 480px;
  box-shadow: var(--shadow-xl);
  z-index: var(--z-modal);
}

.modal-dialog__title {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  margin: 0 0 var(--spacing-4) 0;
}

.modal-dialog__body {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  margin: 0 0 var(--spacing-4) 0;
  line-height: var(--line-height-relaxed);
}

.modal-dialog__field {
  margin-bottom: var(--spacing-4);
}

.modal-dialog__error {
  background: color-mix(in srgb, var(--color-error) 15%, transparent);
  color: var(--color-error);
  padding: var(--spacing-2) var(--spacing-3);
  border-radius: var(--radius-md);
  font-size: var(--font-size-sm);
  margin-bottom: var(--spacing-3);
}

.modal-dialog__actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-2);
}

.form-label {
  display: block;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-primary);
  margin-bottom: var(--spacing-1);
}

.form-helper {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  margin: 0 0 var(--spacing-2) 0;
}

.form-input {
  width: 100%;
  padding: var(--spacing-2) var(--spacing-3);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-background);
  color: var(--color-text-primary);
  font-size: var(--font-size-sm);
}

.form-input:focus {
  outline: 2px solid var(--color-primary-500);
  outline-offset: -1px;
}
</style>
