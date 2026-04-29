<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { screenshotsClient } from '~/api/screenshotsClient'
import { projectsClient } from '~/api/projectsClient'
import { contentClient } from '~/api/contentClient'
import type { Screenshot, ScreenshotDetail, ScreenshotRegion, Project, ContentItem } from '~/api/types'
import { useAuth } from '~/composables/useAuth'

definePageMeta({ layout: 'app', middleware: ['feature-flags'] })

const auth = useAuth()
const workspaceId = computed(() => auth.workspace.value?.id ?? '')

const projects = ref<Project[]>([])
const selectedProjectId = ref('')
const screenshots = ref<Screenshot[]>([])
const loading = ref(false)
const uploading = ref(false)
const error = ref('')

// Detail view
const selectedScreenshot = ref<ScreenshotDetail | null>(null)
const showDetail = ref(false)

// Delete confirmation modal
const showDeleteModal = ref(false)
const deleteTarget = ref<Screenshot | null>(null)
const deleting = ref(false)

// Link search modal
const showLinkModal = ref(false)
const linkTarget = ref<ScreenshotRegion | null>(null)
const contentItems = ref<ContentItem[]>([])
const linkSearch = ref('')
const linking = ref(false)

const filteredContentItems = computed(() => {
  if (!linkSearch.value) return contentItems.value
  const q = linkSearch.value.toLowerCase()
  return contentItems.value.filter(ci =>
    ci.key.toLowerCase().includes(q) || ci.source.toLowerCase().includes(q)
  )
})

async function loadProjects() {
  if (!workspaceId.value) return
  try {
    projects.value = await projectsClient.list(workspaceId.value)
    if (projects.value.length > 0 && !selectedProjectId.value) {
      selectedProjectId.value = projects.value[0].id
    }
  } catch (e: any) {
    error.value = e.message || 'Failed to load projects'
  }
}

async function loadScreenshots() {
  if (!selectedProjectId.value) {
    screenshots.value = []
    return
  }
  loading.value = true
  error.value = ''
  try {
    screenshots.value = await screenshotsClient.list(selectedProjectId.value)
  } catch (e: any) {
    error.value = e.message || 'Failed to load screenshots'
  } finally {
    loading.value = false
  }
}

watch(() => workspaceId.value, () => { loadProjects() }, { immediate: true })
watch(() => selectedProjectId.value, () => { loadScreenshots() })

// Upload
const dragOver = ref(false)

function onDrop(e: DragEvent) {
  dragOver.value = false
  const files = e.dataTransfer?.files
  if (files?.length) uploadFile(files[0])
}

function onFileSelect(e: Event) {
  const input = e.target as HTMLInputElement
  if (input.files?.length) uploadFile(input.files[0])
  input.value = ''
}

async function uploadFile(file: File) {
  const allowed = ['image/png', 'image/jpeg', 'image/webp']
  if (!allowed.includes(file.type)) {
    error.value = 'Only PNG, JPG, and WebP files are accepted.'
    return
  }
  if (file.size > 10 * 1024 * 1024) {
    error.value = 'File exceeds 10MB limit.'
    return
  }
  uploading.value = true
  error.value = ''
  try {
    await screenshotsClient.upload(selectedProjectId.value, file)
    await loadScreenshots()
  } catch (e: any) {
    error.value = e.message || 'Upload failed'
  } finally {
    uploading.value = false
  }
}

// Detail
async function openDetail(s: Screenshot) {
  try {
    selectedScreenshot.value = await screenshotsClient.get(s.projectId, s.id)
    showDetail.value = true
  } catch (e: any) {
    error.value = e.message || 'Failed to load screenshot details'
  }
}

function closeDetail() {
  showDetail.value = false
  selectedScreenshot.value = null
}

// Delete
function confirmDelete(s: Screenshot) {
  deleteTarget.value = s
  showDeleteModal.value = true
}

async function executeDelete() {
  if (!deleteTarget.value) return
  deleting.value = true
  try {
    await screenshotsClient.delete(deleteTarget.value.projectId, deleteTarget.value.id)
    showDeleteModal.value = false
    deleteTarget.value = null
    if (showDetail.value && selectedScreenshot.value?.id === deleteTarget.value?.id) {
      closeDetail()
    }
    await loadScreenshots()
  } catch (e: any) {
    error.value = e.message || 'Delete failed'
  } finally {
    deleting.value = false
  }
}

// Region linking
async function openLinkModal(region: ScreenshotRegion) {
  linkTarget.value = region
  linkSearch.value = ''
  showLinkModal.value = true
  try {
    contentItems.value = await contentClient.list(selectedProjectId.value)
  } catch (e: any) {
    error.value = e.message || 'Failed to load content items'
  }
}

async function linkRegion(contentItemId: string) {
  if (!linkTarget.value) return
  linking.value = true
  try {
    await screenshotsClient.linkRegion(linkTarget.value.id, contentItemId)
    showLinkModal.value = false
    // Refresh detail
    if (selectedScreenshot.value) {
      selectedScreenshot.value = await screenshotsClient.get(
        selectedScreenshot.value.projectId, selectedScreenshot.value.id
      )
    }
  } catch (e: any) {
    error.value = e.message || 'Link failed'
  } finally {
    linking.value = false
  }
}

async function unlinkRegion(region: ScreenshotRegion) {
  try {
    await screenshotsClient.unlinkRegion(region.id)
    if (selectedScreenshot.value) {
      selectedScreenshot.value = await screenshotsClient.get(
        selectedScreenshot.value.projectId, selectedScreenshot.value.id
      )
    }
  } catch (e: any) {
    error.value = e.message || 'Unlink failed'
  }
}

function regionColor(region: ScreenshotRegion): string {
  if (region.isManualLink && region.contentItemId) return 'var(--color-info)'
  if (region.contentItemId) return 'var(--color-success)'
  return 'var(--color-warning)'
}

function regionLabel(region: ScreenshotRegion): string {
  if (region.isManualLink && region.contentItemId) return 'Manually linked'
  if (region.contentItemId) return 'Auto-linked'
  return 'Unmatched'
}

function ocrStatusLabel(status: string): string {
  if (status === 'completed') return 'OCR Complete'
  if (status === 'pending') return 'OCR Pending'
  if (status === 'failed') return 'OCR Failed'
  return status
}

function ocrStatusClass(status: string): string {
  if (status === 'completed') return 'badge--success'
  if (status === 'pending') return 'badge--warning'
  if (status === 'failed') return 'badge--error'
  return ''
}

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}
</script>

<template>
  <div class="screenshots-page">
    <div class="screenshots-page__header">
      <h1 class="screenshots-page__title">Screenshots</h1>
      <div class="screenshots-page__project-select">
        <label for="project-select">Project</label>
        <select id="project-select" v-model="selectedProjectId" class="screenshots-page__select">
          <option value="" disabled>Select a project</option>
          <option v-for="p in projects" :key="p.id" :value="p.id">{{ p.name }}</option>
        </select>
      </div>
    </div>

    <div v-if="error" class="screenshots-page__error">{{ error }}</div>

    <!-- Upload area -->
    <div
      v-if="selectedProjectId"
      class="screenshots-page__upload"
      :class="{ 'screenshots-page__upload--dragover': dragOver }"
      @dragover.prevent="dragOver = true"
      @dragleave.prevent="dragOver = false"
      @drop.prevent="onDrop"
    >
      <div class="screenshots-page__upload-content">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="screenshots-page__upload-icon">
          <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
          <polyline points="17 8 12 3 7 8" />
          <line x1="12" y1="3" x2="12" y2="15" />
        </svg>
        <p v-if="uploading">Uploading...</p>
        <p v-else>Drag & drop a screenshot here, or <label class="screenshots-page__upload-link"><input type="file" accept="image/png,image/jpeg,image/webp" @change="onFileSelect" hidden>browse</label></p>
        <span class="screenshots-page__upload-hint">PNG, JPG, or WebP — max 10MB</span>
      </div>
    </div>

    <!-- Grid -->
    <div v-if="loading" class="screenshots-page__loading">Loading...</div>
    <div v-else-if="screenshots.length === 0 && selectedProjectId" class="screenshots-page__empty">
      No screenshots yet. Upload one above.
    </div>
    <div v-else class="screenshots-page__grid">
      <div
        v-for="s in screenshots"
        :key="s.id"
        class="screenshot-card"
        @click="openDetail(s)"
      >
        <div class="screenshot-card__image-wrap">
          <img :src="`/${s.storagePath}`" :alt="s.fileName" class="screenshot-card__image" loading="lazy" />
        </div>
        <div class="screenshot-card__info">
          <span class="screenshot-card__name" :title="s.fileName">{{ s.fileName }}</span>
          <div class="screenshot-card__meta">
            <span class="screenshot-card__size">{{ formatSize(s.fileSizeBytes) }}</span>
            <span :class="['badge', ocrStatusClass(s.ocrStatus)]">{{ ocrStatusLabel(s.ocrStatus) }}</span>
          </div>
          <span v-if="s.regionCount !== undefined" class="screenshot-card__regions">{{ s.regionCount }} region{{ s.regionCount === 1 ? '' : 's' }}</span>
        </div>
        <button class="screenshot-card__delete" @click.stop="confirmDelete(s)" title="Delete screenshot">
          <svg viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd" /></svg>
        </button>
      </div>
    </div>

    <!-- Detail overlay -->
    <div v-if="showDetail && selectedScreenshot" class="modal-overlay" @click.self="closeDetail">
      <div class="screenshots-detail">
        <div class="screenshots-detail__header">
          <h2>{{ selectedScreenshot.fileName }}</h2>
          <div class="screenshots-detail__actions">
            <NuxtLink :to="`/app/screenshots/${selectedScreenshot.id}/edit`" class="btn btn--primary btn--sm">Edit in Context</NuxtLink>
            <button class="btn btn--danger btn--sm" @click="confirmDelete(selectedScreenshot)" :disabled="deleting">Delete</button>
            <button class="btn btn--ghost btn--sm" @click="closeDetail">Close</button>
          </div>
        </div>
        <div class="screenshots-detail__body">
          <div class="screenshots-detail__image-container">
            <img :src="`/${selectedScreenshot.storagePath}`" :alt="selectedScreenshot.fileName" class="screenshots-detail__image" />
            <!-- Bounding box overlays -->
            <div
              v-for="region in selectedScreenshot.regions"
              :key="region.id"
              class="screenshots-detail__region"
              :style="{
                left: region.x + '%',
                top: region.y + '%',
                width: region.width + '%',
                height: region.height + '%',
                borderColor: regionColor(region)
              }"
              :title="region.detectedText"
              @click="!region.contentItemId ? openLinkModal(region) : undefined"
            >
              <span class="screenshots-detail__region-label" :style="{ background: regionColor(region) }">
                {{ region.contentItemKey || region.detectedText }}
              </span>
            </div>
          </div>
          <div class="screenshots-detail__sidebar">
            <h3>Regions ({{ selectedScreenshot.regions.length }})</h3>
            <div v-if="selectedScreenshot.regions.length === 0" class="screenshots-detail__empty-regions">
              No text regions detected.
            </div>
            <div
              v-for="region in selectedScreenshot.regions"
              :key="region.id"
              class="region-item"
            >
              <div class="region-item__header">
                <span class="region-item__badge" :style="{ background: regionColor(region) }">{{ regionLabel(region) }}</span>
                <span class="region-item__confidence">{{ (region.confidence * 100).toFixed(0) }}%</span>
              </div>
              <p class="region-item__text">{{ region.detectedText }}</p>
              <p v-if="region.contentItemKey" class="region-item__key">Linked to: <strong>{{ region.contentItemKey }}</strong></p>
              <div class="region-item__actions">
                <button v-if="!region.contentItemId" class="btn btn--sm btn--primary" @click="openLinkModal(region)">Link</button>
                <button v-if="region.contentItemId" class="btn btn--sm btn--ghost" @click="unlinkRegion(region)">Unlink</button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Delete confirmation modal -->
    <div v-if="showDeleteModal" class="modal-overlay" @click.self="showDeleteModal = false">
      <div class="modal-dialog">
        <h3>Delete Screenshot</h3>
        <p>Are you sure you want to delete <strong>{{ deleteTarget?.fileName }}</strong>? This will also remove all detected regions.</p>
        <div class="modal-dialog__actions">
          <button class="btn btn--ghost" @click="showDeleteModal = false" :disabled="deleting">Cancel</button>
          <button class="btn btn--danger" @click="executeDelete" :disabled="deleting">{{ deleting ? 'Deleting...' : 'Delete' }}</button>
        </div>
      </div>
    </div>

    <!-- Link search modal -->
    <div v-if="showLinkModal" class="modal-overlay" @click.self="showLinkModal = false">
      <div class="modal-dialog modal-dialog--wide">
        <h3>Link to Content Key</h3>
        <p class="modal-dialog__subtitle">Detected text: <em>{{ linkTarget?.detectedText }}</em></p>
        <input
          v-model="linkSearch"
          type="text"
          placeholder="Search content keys..."
          class="modal-dialog__search"
        />
        <div class="modal-dialog__list">
          <div
            v-for="ci in filteredContentItems"
            :key="ci.id"
            class="modal-dialog__list-item"
            @click="linkRegion(ci.id)"
          >
            <span class="modal-dialog__list-key">{{ ci.key }}</span>
            <span class="modal-dialog__list-source">{{ ci.source }}</span>
          </div>
          <div v-if="filteredContentItems.length === 0" class="modal-dialog__list-empty">No matching content keys found.</div>
        </div>
        <div class="modal-dialog__actions">
          <button class="btn btn--ghost" @click="showLinkModal = false" :disabled="linking">Cancel</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.screenshots-page {
  padding: var(--spacing-6);
  max-width: 1200px;
}

.screenshots-page__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--spacing-6);
  flex-wrap: wrap;
  gap: var(--spacing-4);
}

.screenshots-page__title {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  color: var(--color-text-primary);
  margin: 0;
}

.screenshots-page__project-select {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
}

.screenshots-page__project-select label {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  font-weight: var(--font-weight-medium);
}

.screenshots-page__select {
  padding: var(--spacing-2) var(--spacing-3);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-surface);
  color: var(--color-text-primary);
  font-size: var(--font-size-sm);
  min-width: 200px;
}

.screenshots-page__error {
  padding: var(--spacing-3) var(--spacing-4);
  background: color-mix(in srgb, var(--color-error) 10%, transparent);
  color: var(--color-error);
  border-radius: var(--radius-md);
  margin-bottom: var(--spacing-4);
  font-size: var(--font-size-sm);
}

/* Upload area */
.screenshots-page__upload {
  border: 2px dashed var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-10);
  text-align: center;
  margin-bottom: var(--spacing-6);
  transition: var(--transition-fast);
  cursor: pointer;
}

.screenshots-page__upload--dragover {
  border-color: var(--color-primary-500);
  background: color-mix(in srgb, var(--color-primary-500) 5%, transparent);
}

.screenshots-page__upload-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--spacing-2);
}

.screenshots-page__upload-icon {
  width: 48px;
  height: 48px;
  color: var(--color-text-muted);
}

.screenshots-page__upload-content p {
  margin: 0;
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
}

.screenshots-page__upload-link {
  color: var(--color-primary-600);
  cursor: pointer;
  text-decoration: underline;
}

.screenshots-page__upload-hint {
  color: var(--color-text-muted);
  font-size: var(--font-size-xs);
}

.screenshots-page__loading,
.screenshots-page__empty {
  text-align: center;
  color: var(--color-text-muted);
  padding: var(--spacing-10);
  font-size: var(--font-size-sm);
}

/* Grid */
.screenshots-page__grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  gap: var(--spacing-4);
}

.screenshot-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  overflow: hidden;
  cursor: pointer;
  transition: var(--transition-fast);
  position: relative;
}

.screenshot-card:hover {
  box-shadow: var(--shadow-md);
}

.screenshot-card__image-wrap {
  aspect-ratio: 16 / 10;
  overflow: hidden;
  background: var(--color-gray-100);
}

.screenshot-card__image {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.screenshot-card__info {
  padding: var(--spacing-3);
  display: flex;
  flex-direction: column;
  gap: var(--spacing-1);
}

.screenshot-card__name {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-primary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.screenshot-card__meta {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
}

.screenshot-card__size {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

.screenshot-card__regions {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

.screenshot-card__delete {
  position: absolute;
  top: var(--spacing-2);
  right: var(--spacing-2);
  background: color-mix(in srgb, var(--color-background) 80%, transparent);
  border: none;
  border-radius: var(--radius-sm);
  padding: var(--spacing-1);
  cursor: pointer;
  color: var(--color-text-muted);
  opacity: 0;
  transition: var(--transition-fast);
}

.screenshot-card:hover .screenshot-card__delete {
  opacity: 1;
}

.screenshot-card__delete:hover {
  color: var(--color-error);
}

.screenshot-card__delete svg {
  width: 16px;
  height: 16px;
}

/* Badges */
.badge {
  display: inline-block;
  font-size: var(--font-size-xs);
  padding: 2px var(--spacing-2);
  border-radius: var(--radius-full);
  font-weight: var(--font-weight-medium);
}

.badge--success {
  background: color-mix(in srgb, var(--color-success) 15%, transparent);
  color: var(--color-success);
}

.badge--warning {
  background: color-mix(in srgb, var(--color-warning) 15%, transparent);
  color: var(--color-warning);
}

.badge--error {
  background: color-mix(in srgb, var(--color-error) 15%, transparent);
  color: var(--color-error);
}

/* Detail overlay */
.screenshots-detail {
  background: var(--color-background);
  border-radius: var(--radius-xl);
  width: 95vw;
  max-width: 1400px;
  max-height: 90vh;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.screenshots-detail__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--spacing-4) var(--spacing-6);
  border-bottom: 1px solid var(--color-border);
}

.screenshots-detail__header h2 {
  margin: 0;
  font-size: var(--font-size-lg);
  color: var(--color-text-primary);
}

.screenshots-detail__actions {
  display: flex;
  gap: var(--spacing-2);
}

.screenshots-detail__body {
  display: flex;
  flex: 1;
  overflow: hidden;
}

.screenshots-detail__image-container {
  flex: 1;
  position: relative;
  overflow: auto;
  padding: var(--spacing-4);
}

.screenshots-detail__image {
  max-width: 100%;
  display: block;
}

.screenshots-detail__region {
  position: absolute;
  border: 2px solid;
  border-radius: var(--radius-sm);
  cursor: pointer;
  transition: var(--transition-fast);
}

.screenshots-detail__region:hover {
  background: color-mix(in srgb, currentColor 10%, transparent);
}

.screenshots-detail__region-label {
  position: absolute;
  top: -20px;
  left: 0;
  font-size: 10px;
  color: white;
  padding: 1px 4px;
  border-radius: var(--radius-sm);
  white-space: nowrap;
  max-width: 150px;
  overflow: hidden;
  text-overflow: ellipsis;
}

.screenshots-detail__sidebar {
  width: 320px;
  min-width: 320px;
  border-left: 1px solid var(--color-border);
  overflow-y: auto;
  padding: var(--spacing-4);
}

.screenshots-detail__sidebar h3 {
  margin: 0 0 var(--spacing-4);
  font-size: var(--font-size-base);
  color: var(--color-text-primary);
}

.screenshots-detail__empty-regions {
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.region-item {
  padding: var(--spacing-3);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  margin-bottom: var(--spacing-2);
}

.region-item__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--spacing-1);
}

.region-item__badge {
  font-size: 10px;
  color: white;
  padding: 1px 6px;
  border-radius: var(--radius-full);
  font-weight: var(--font-weight-medium);
}

.region-item__confidence {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

.region-item__text {
  margin: 0 0 var(--spacing-1);
  font-size: var(--font-size-sm);
  color: var(--color-text-primary);
  word-break: break-word;
}

.region-item__key {
  margin: 0 0 var(--spacing-2);
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.region-item__actions {
  display: flex;
  gap: var(--spacing-2);
}

/* Modal styles */
.modal-overlay {
  position: fixed;
  inset: 0;
  background: color-mix(in srgb, var(--color-gray-950) 50%, transparent);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: var(--z-modal-backdrop);
}

.modal-dialog {
  background: var(--color-background);
  border-radius: var(--radius-xl);
  padding: var(--spacing-6);
  max-width: 480px;
  width: 90vw;
  z-index: var(--z-modal);
}

.modal-dialog--wide {
  max-width: 600px;
}

.modal-dialog h3 {
  margin: 0 0 var(--spacing-3);
  font-size: var(--font-size-lg);
  color: var(--color-text-primary);
}

.modal-dialog p {
  margin: 0 0 var(--spacing-4);
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
}

.modal-dialog__subtitle {
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.modal-dialog__search {
  width: 100%;
  padding: var(--spacing-2) var(--spacing-3);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-surface);
  color: var(--color-text-primary);
  font-size: var(--font-size-sm);
  margin-bottom: var(--spacing-3);
}

.modal-dialog__list {
  max-height: 300px;
  overflow-y: auto;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  margin-bottom: var(--spacing-4);
}

.modal-dialog__list-item {
  padding: var(--spacing-2) var(--spacing-3);
  cursor: pointer;
  border-bottom: 1px solid var(--color-border);
  transition: var(--transition-fast);
}

.modal-dialog__list-item:last-child {
  border-bottom: none;
}

.modal-dialog__list-item:hover {
  background: color-mix(in srgb, var(--color-primary-500) 8%, transparent);
}

.modal-dialog__list-key {
  display: block;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-primary);
}

.modal-dialog__list-source {
  display: block;
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.modal-dialog__list-empty {
  padding: var(--spacing-4);
  text-align: center;
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.modal-dialog__actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-2);
}

/* Buttons */
.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-2) var(--spacing-4);
  border: 1px solid transparent;
  border-radius: var(--radius-md);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  cursor: pointer;
  transition: var(--transition-fast);
}

.btn--sm {
  padding: var(--spacing-1) var(--spacing-3);
  font-size: var(--font-size-xs);
}

.btn--primary {
  background: var(--color-primary-600);
  color: white;
}

.btn--primary:hover {
  background: var(--color-primary-700);
}

.btn--danger {
  background: var(--color-error);
  color: white;
}

.btn--danger:hover {
  opacity: 0.9;
}

.btn--ghost {
  background: transparent;
  color: var(--color-text-secondary);
  border-color: var(--color-border);
}

.btn--ghost:hover {
  background: var(--color-surface);
}

.btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
</style>
