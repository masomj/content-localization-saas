<script setup lang="ts">
import AppSkeleton from '~/components/AppSkeleton.vue'
import UiButton from '~/components/ui/Button.vue'
import { libraryClient } from '~/api/libraryClient'
import { contentClient } from '~/api/contentClient'
import type { LibraryComponentWithVariants, LibraryComponentVariant, LibraryComponentTextField, ContentItem } from '~/api/types'

definePageMeta({ layout: 'canvas' })
useSeoMeta({ title: 'Library Component - InterCopy' })

const route = useRoute()
const router = useRouter()

const componentId = computed(() => route.params.id as string)
const projectId = computed(() => (route.query.projectId as string) || '')

const isLoading = ref(false)
const component = ref<LibraryComponentWithVariants | null>(null)
const selectedVariantId = ref<string | null>(null)
const selectedFieldId = ref<string | null>(null)
const errorMessage = ref('')

// Editor state
const editorText = ref('')
const editorSaving = ref(false)
const editorError = ref('')

// Content key linking
const contentItems = ref<ContentItem[]>([])
const contentSearch = ref('')
const contentLoading = ref(false)
const linkingFieldId = ref<string | null>(null)

const selectedVariant = computed(() => {
  if (!selectedVariantId.value || !component.value) return null
  return component.value.variants.find(v => v.id === selectedVariantId.value) ?? null
})

const variantFields = computed<LibraryComponentTextField[]>(() => {
  return selectedVariant.value?.textFields ?? []
})

const selectedField = computed(() => {
  if (!selectedFieldId.value) return null
  return variantFields.value.find(f => f.id === selectedFieldId.value) ?? null
})

const filteredContentItems = computed(() => {
  const search = contentSearch.value.toLowerCase().trim()
  if (!search) return contentItems.value
  return contentItems.value.filter(
    item => item.key.toLowerCase().includes(search) || item.source.toLowerCase().includes(search),
  )
})

// Canvas scaling — use selected variant dimensions
const canvasContainerRef = ref<HTMLElement | null>(null)
const MIN_ZOOM = 0.25
const MAX_ZOOM = 4
const zoomLevel = ref(1)

const baseScale = computed(() => {
  if (!selectedVariant.value) return 1
  const w = selectedVariant.value.frameWidth || component.value?.frameWidth || 100
  const h = selectedVariant.value.frameHeight || component.value?.frameHeight || 100
  const maxWidth = 600
  const maxHeight = 400
  const scaleX = maxWidth / w
  const scaleY = maxHeight / h
  // Allow scaling UP for small components (buttons etc), cap at 8x
  return Math.min(scaleX, scaleY, 8)
})

const canvasScale = computed(() => baseScale.value * zoomLevel.value)

// Variant background — treat #ffffff as "no background captured" since
// that was the migration default before we started capturing fills
const variantBgColor = computed(() => {
  const bg = selectedVariant.value?.backgroundColor
  if (!bg || bg === '#ffffff' || bg === '#FFFFFF') return '#374151'
  return bg
})

const canvasWidth = computed(() => {
  if (!selectedVariant.value) return 0
  return selectedVariant.value.frameWidth || component.value?.frameWidth || 0
})

const canvasHeight = computed(() => {
  if (!selectedVariant.value) return 0
  return selectedVariant.value.frameHeight || component.value?.frameHeight || 0
})

const zoomPercent = computed(() => Math.round(zoomLevel.value * 100) + '%')

function zoomIn() { zoomLevel.value = Math.min(zoomLevel.value * 1.25, MAX_ZOOM) }
function zoomOut() { zoomLevel.value = Math.max(zoomLevel.value / 1.25, MIN_ZOOM) }
function zoomReset() {
  zoomLevel.value = 1
  panX.value = 0
  panY.value = 0
}
function handleCanvasWheel(e: WheelEvent) {
  if (e.ctrlKey || e.metaKey) {
    e.preventDefault()
    if (e.deltaY < 0) zoomIn()
    else zoomOut()
  }
}

// Canvas drag-to-pan (transform-based for Figma-like behavior)
const isPanning = ref(false)
const panX = ref(0)
const panY = ref(0)
const panStart = ref({ x: 0, y: 0, panX: 0, panY: 0 })

const canvasTransform = computed(() => {
  return `translate(${panX.value}px, ${panY.value}px)`
})

function handlePanStart(e: MouseEvent) {
  // Only pan on middle-click, or left-click on empty canvas (not on text field buttons)
  const target = e.target as HTMLElement
  const isCanvas = target.closest('.canvas-scroll-area') !== null
    && target.closest('.canvas-text-field') === null
  if (e.button === 1 || (e.button === 0 && isCanvas)) {
    e.preventDefault()
    isPanning.value = true
    panStart.value = { x: e.clientX, y: e.clientY, panX: panX.value, panY: panY.value }
  }
}

function handlePanMove(e: MouseEvent) {
  if (!isPanning.value) return
  e.preventDefault()
  const dx = e.clientX - panStart.value.x
  const dy = e.clientY - panStart.value.y
  panX.value = panStart.value.panX + dx
  panY.value = panStart.value.panY + dy
}

function handlePanEnd() {
  if (!isPanning.value) return
  isPanning.value = false
}

function selectVariant(variant: LibraryComponentVariant) {
  selectedVariantId.value = variant.id
  selectedFieldId.value = null
  editorText.value = ''
  editorError.value = ''
  zoomLevel.value = 1
  panX.value = 0
  panY.value = 0
}

function selectField(field: LibraryComponentTextField) {
  selectedFieldId.value = field.id
  // If linked to a content key, show the content key's source text
  if (field.contentItemId) {
    const linked = contentItems.value.find(ci => ci.id === field.contentItemId)
    editorText.value = linked?.source ?? field.currentText
  } else {
    editorText.value = field.currentText
  }
  editorError.value = ''
}

function closeEditor() {
  selectedFieldId.value = null
  editorText.value = ''
  editorError.value = ''
}

async function saveTextField() {
  if (!selectedField.value) return
  const text = editorText.value.trim()
  if (!text) {
    editorError.value = 'Text cannot be empty'
    return
  }
  editorSaving.value = true
  editorError.value = ''
  try {
    await libraryClient.updateTextField(selectedField.value.id, text, selectedField.value.contentItemId)
    await loadComponent()
    if (selectedFieldId.value) {
      const updatedField = variantFields.value.find(f => f.id === selectedFieldId.value)
      if (updatedField) editorText.value = updatedField.currentText
    }
  } catch (err: any) {
    editorError.value = err?.message ?? 'Failed to save text field'
  } finally {
    editorSaving.value = false
  }
}

async function linkContentKey(contentItemId: string) {
  if (!selectedField.value) return
  linkingFieldId.value = selectedField.value.id
  try {
    await libraryClient.linkContentKey(selectedField.value.id, contentItemId)
    await loadComponent()
    contentSearch.value = ''
  } catch (err: any) {
    editorError.value = err?.message ?? 'Failed to link content key'
  } finally {
    linkingFieldId.value = null
  }
}

async function unlinkContentKey() {
  if (!selectedField.value) return
  linkingFieldId.value = selectedField.value.id
  try {
    await libraryClient.unlinkContentKey(selectedField.value.id)
    await loadComponent()
  } catch (err: any) {
    editorError.value = err?.message ?? 'Failed to unlink content key'
  } finally {
    linkingFieldId.value = null
  }
}

function getLinkedContentKey(field: LibraryComponentTextField): ContentItem | null {
  if (!field.contentItemId) return null
  return contentItems.value.find(item => item.id === field.contentItemId) ?? null
}

function truncate(text: string, max: number): string {
  if (!text) return ''
  return text.length > max ? `${text.slice(0, max)}...` : text
}

/** Resolve displayed text for a text field based on linked content key */
function getDisplayText(field: LibraryComponentTextField): string {
  if (field.contentItemId) {
    const linked = contentItems.value.find(ci => ci.id === field.contentItemId)
    return linked?.source ?? field.currentText
  }
  return field.currentText
}

async function loadComponent() {
  if (!componentId.value) return
  isLoading.value = true
  errorMessage.value = ''
  try {
    component.value = await libraryClient.get(componentId.value)
    if (component.value.variants.length > 0 && !selectedVariantId.value) {
      selectedVariantId.value = component.value.variants[0]!.id
    }
    // Set breadcrumb label to component name instead of GUID
    if (component.value?.name) {
      route.meta.breadcrumbLabel = component.value.name
    }
  } catch (err: any) {
    errorMessage.value = err?.message ?? 'Failed to load component'
  } finally {
    isLoading.value = false
  }
}

async function loadContentItems() {
  if (!projectId.value) return
  contentLoading.value = true
  try {
    const data = await contentClient.list(projectId.value)
    contentItems.value = Array.isArray(data) ? data : []
  } catch (_) {
    contentItems.value = []
  } finally {
    contentLoading.value = false
  }
}

function goBack() {
  router.push(`/app/library?projectId=${encodeURIComponent(projectId.value)}`)
}

// Delete library component
const showDeleteConfirm = ref(false)
const deleteError = ref('')

async function confirmDeleteComponent() {
  if (!component.value) return
  deleteError.value = ''
  try {
    await libraryClient.delete(component.value.id)
    goBack()
  } catch (err: any) {
    deleteError.value = err?.message ?? 'Failed to delete library component'
  }
}

onMounted(async () => {
  await Promise.all([loadComponent(), loadContentItems()])
})
</script>

<template>
  <div class="lib-detail">
    <!-- Top bar (hidden — info moved to left sidebar) -->
    <div class="lib-detail__topbar">
      <UiButton variant="ghost" size="sm" @click="goBack">
        <svg viewBox="0 0 20 20" fill="currentColor" class="btn-icon-inline">
          <path fill-rule="evenodd" d="M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z" clip-rule="evenodd" />
        </svg>
        Back to Library
      </UiButton>
      <h1 v-if="component" class="lib-detail__title">{{ component.name || 'Library Component' }}</h1>
    </div>

    <!-- Loading state -->
    <div v-if="isLoading" class="lib-detail__loading">
      <AppSkeleton :lines="6" height="1rem" />
    </div>

    <!-- Error state -->
    <div v-else-if="errorMessage" class="lib-detail__error" role="alert">{{ errorMessage }}</div>

    <!-- Main three-panel layout -->
    <div v-else-if="component" class="lib-detail__panels">
      <!-- LEFT SIDEBAR: Variant navigator -->
      <aside class="variant-nav">
        <div class="variant-nav__header">
          <UiButton variant="ghost" size="sm" @click="goBack" title="Back to library">
            <svg viewBox="0 0 20 20" fill="currentColor" style="width:1em;height:1em"><path fill-rule="evenodd" d="M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z" clip-rule="evenodd"/></svg>
          </UiButton>
          <h2 class="variant-nav__title">{{ component.name || 'Library Component' }}</h2>
          <UiButton variant="danger" size="sm" @click="showDeleteConfirm = true" title="Delete library component" style="margin-left:auto;padding:4px 8px;font-size:10px;">Delete</UiButton>
        </div>
        <p class="variant-nav__hint">Select a variant to view its text fields and edit copy.</p>

        <div class="variant-nav__list" role="listbox" :aria-label="`Variants of ${component.name}`">
          <button
            v-for="variant in component.variants"
            :key="variant.id"
            class="variant-nav__item"
            :class="{ 'variant-nav__item--selected': selectedVariantId === variant.id }"
            role="option"
            :aria-selected="selectedVariantId === variant.id"
            @click="selectVariant(variant)"
          >
            <div class="variant-nav__item-header">
              <span class="variant-nav__item-name">{{ variant.variantName || 'Default' }}</span>
              <span class="variant-nav__item-count">{{ (variant.textFields ?? []).length }} fields</span>
            </div>
          </button>
          <div v-if="component.variants.length === 0" class="variant-nav__empty">
            No variants found. Push this component from the Figma plugin.
          </div>
        </div>

        <!-- Zoom controls -->
        <div class="variant-nav__zoom">
          <button class="canvas-zoom-btn" title="Zoom out" @click="zoomOut">&#x2212;</button>
          <span class="canvas-zoom-label">{{ zoomPercent }}</span>
          <button class="canvas-zoom-btn" title="Zoom in" @click="zoomIn">+</button>
          <button class="canvas-zoom-btn canvas-zoom-btn--reset" title="Reset zoom" @click="zoomReset">&#x27F2;</button>
        </div>
      </aside>

      <!-- CENTER: Visual canvas -->
      <div class="canvas-panel" ref="canvasContainerRef" @wheel="handleCanvasWheel">
        <div
          class="canvas-scroll-area"
          :class="{ 'canvas-scroll-area--panning': isPanning }"
          @mousedown="handlePanStart"
          @mousemove="handlePanMove"
          @mouseup="handlePanEnd"
          @mouseleave="handlePanEnd"
        >
          <div
            v-if="selectedVariant"
            class="canvas-frame"
            :style="{
              width: canvasWidth * canvasScale + 'px',
              height: canvasHeight * canvasScale + 'px',
              transform: canvasTransform,
            }"
          >
            <!-- Background -->
            <div class="canvas-frame__placeholder" :style="{ background: variantBgColor }" />

            <!-- Text field overlays -->
            <button
              v-for="field in variantFields"
              :key="field.id"
              class="canvas-text-field"
              :class="{ 'canvas-text-field--selected': selectedFieldId === field.id }"
              :style="{
                left: field.x * canvasScale + 'px',
                top: field.y * canvasScale + 'px',
                width: field.width * canvasScale + 'px',
                height: field.height * canvasScale + 'px',
                fontFamily: field.fontFamily || 'inherit',
                fontSize: (field.fontSize * canvasScale) + 'px',
                fontWeight: field.fontWeight || 'normal',
                textAlign: field.textAlign || 'left',
                color: field.color || 'var(--color-text-primary)',
              }"
              :title="field.figmaLayerName"
              @click="selectField(field)"
            >
              {{ getDisplayText(field) }}
            </button>
          </div>
          <div v-else class="canvas-empty">Select a variant to preview</div>
        </div><!-- /canvas-scroll-area -->
      </div>

      <!-- RIGHT SIDEBAR: Text field editor -->
      <Transition name="editor-slide">
        <aside v-if="selectedField" class="field-editor">
          <div class="field-editor__header">
            <h2 class="field-editor__title">{{ selectedField.figmaLayerName }}</h2>
            <button class="field-editor__close" aria-label="Close editor" @click="closeEditor">
              <svg viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" />
              </svg>
            </button>
          </div>

          <!-- Current text -->
          <div class="field-editor__section">
            <label for="fieldText" class="field-editor__label">
              <span>{{ selectedField.contentItemId ? 'Text (set by content key)' : 'Current text' }}</span>
              <span class="field-editor__label-hint">
                {{ selectedField.contentItemId
                  ? 'Unlink the content key to edit directly'
                  : 'Edit the text content for this layer' }}
              </span>
            </label>
            <textarea
              id="fieldText"
              v-model="editorText"
              class="field-editor__textarea"
              :class="{ 'field-editor__textarea--readonly': !!selectedField.contentItemId }"
              :readonly="!!selectedField.contentItemId"
              rows="4"
            />
          </div>

          <!-- Content key link section -->
          <div class="field-editor__section">
            <label class="field-editor__label">
              <span>Content key</span>
              <span class="field-editor__label-hint">Link to an i18n content key for translations</span>
            </label>

            <!-- Linked state -->
            <div v-if="selectedField.contentItemId" class="field-editor__linked">
              <div class="field-editor__linked-info">
                <span class="field-editor__linked-key">
                  {{ getLinkedContentKey(selectedField)?.key ?? selectedField.contentItemId }}
                </span>
                <span v-if="getLinkedContentKey(selectedField)" class="field-editor__linked-source">
                  {{ truncate(getLinkedContentKey(selectedField)!.source, 60) }}
                </span>
              </div>
              <UiButton
                variant="secondary"
                size="sm"
                :disabled="linkingFieldId === selectedField.id"
                @click="unlinkContentKey"
              >
                Unlink
              </UiButton>
            </div>

            <!-- Unlinked state: search & link -->
            <template v-else>
              <input
                v-model="contentSearch"
                type="text"
                class="field-editor__search"
                placeholder="Search content keys..."
                autocomplete="off"
              >
              <div v-if="contentSearch.trim()" class="field-editor__search-results">
                <button
                  v-for="item in filteredContentItems.slice(0, 8)"
                  :key="item.id"
                  class="field-editor__search-item"
                  :disabled="linkingFieldId === selectedField.id"
                  @click="linkContentKey(item.id)"
                >
                  <span class="field-editor__search-item-key">{{ item.key }}</span>
                  <span class="field-editor__search-item-source">{{ truncate(item.source, 50) }}</span>
                </button>
                <div v-if="filteredContentItems.length === 0" class="field-editor__search-empty">
                  No matching content keys found.
                </div>
              </div>
            </template>
          </div>

          <!-- Error -->
          <p v-if="editorError" class="field-editor__error">{{ editorError }}</p>

          <!-- Actions -->
          <div class="field-editor__actions">
            <UiButton variant="secondary" size="sm" @click="closeEditor">Close</UiButton>
            <UiButton v-if="!selectedField?.contentItemId" size="sm" :disabled="editorSaving" @click="saveTextField">
              {{ editorSaving ? 'Saving...' : 'Save' }}
            </UiButton>
          </div>
        </aside>
      </Transition>
    </div>

    <!-- Delete confirmation modal -->
    <div v-if="showDeleteConfirm" class="delete-overlay" @click.self="showDeleteConfirm = false">
      <div class="delete-modal">
        <h2>Delete library component</h2>
        <p class="delete-text">Delete <strong>{{ component?.name }}</strong>? This will remove the component, all variants, and all text fields from InterCopy. This cannot be undone.</p>
        <p v-if="deleteError" class="delete-error">{{ deleteError }}</p>
        <div class="delete-actions">
          <UiButton variant="secondary" size="sm" @click="showDeleteConfirm = false">Cancel</UiButton>
          <UiButton variant="danger" size="sm" @click="confirmDeleteComponent">Delete</UiButton>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.lib-detail {
  /* Fill the canvas layout's main area (no top header bar) */
  position: fixed;
  top: 0;
  left: 260px;
  right: 0;
  bottom: 0;
  display: flex;
  flex-direction: column;
  z-index: 1;
}

/* Top bar — hidden, info moved to left sidebar */
.lib-detail__topbar {
  display: none;
}

.lib-detail__title {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  margin: 0;
}

.btn-icon-inline {
  width: 1em;
  height: 1em;
  flex-shrink: 0;
}

/* Loading & error */
.lib-detail__loading {
  padding: var(--spacing-6);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
}

.lib-detail__error {
  background: var(--color-error);
  color: var(--color-white);
  padding: var(--spacing-3) var(--spacing-4);
  border-radius: var(--radius-lg);
  font-size: var(--font-size-sm);
}

/* Three-panel layout */
.lib-detail__panels {
  display: flex;
  flex: 1;
  min-height: 0;
  position: relative;
  overflow: hidden;
}

/* ========== LEFT SIDEBAR: Variant navigator ========== */
.variant-nav {
  width: 240px;
  flex-shrink: 0;
  border-right: 1px solid var(--color-border);
  display: flex;
  flex-direction: column;
  background: var(--color-surface);
  overflow-y: auto;
}

.variant-nav__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--spacing-4);
  border-bottom: 1px solid var(--color-border);
}

.variant-nav__title {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  margin: 0;
}

.variant-nav__count {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  background: color-mix(in srgb, var(--color-border) 50%, var(--color-background));
  padding: 1px var(--spacing-2);
  border-radius: var(--radius-md);
}

.variant-nav__hint {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  padding: var(--spacing-2) var(--spacing-4);
  margin: 0;
  border-bottom: 1px solid var(--color-border);
  line-height: 1.4;
}

.variant-nav__list {
  flex: 1;
  overflow-y: auto;
  padding: var(--spacing-2);
}

.variant-nav__item {
  display: flex;
  flex-direction: column;
  gap: 2px;
  width: 100%;
  padding: var(--spacing-2) var(--spacing-3);
  border: 1px solid transparent;
  border-radius: var(--radius-md);
  background: none;
  cursor: pointer;
  text-align: left;
  font: inherit;
  color: inherit;
  transition: background var(--transition-fast), border-color var(--transition-fast);
}

.variant-nav__item:hover {
  background: color-mix(in srgb, var(--color-primary-600) 5%, transparent);
}

.variant-nav__item--selected {
  background: color-mix(in srgb, var(--color-primary-600) 10%, transparent);
  border-color: var(--color-primary-400);
}

.variant-nav__item-header {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
}

.variant-nav__item-name {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  min-width: 0;
}

.variant-nav__item-count {
  flex-shrink: 0;
  font-size: 10px;
  font-weight: var(--font-weight-medium);
  color: var(--color-text-muted);
}

.variant-nav__empty {
  padding: var(--spacing-4);
  text-align: center;
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

.variant-nav__zoom {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--spacing-1);
  padding: var(--spacing-2) var(--spacing-3);
  border-top: 1px solid var(--color-border);
  margin-top: auto;
  flex-shrink: 0;
}

.canvas-zoom-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-background);
  color: var(--color-text-primary);
  cursor: pointer;
  font-size: 16px;
  font-weight: 600;
  transition: background var(--transition-fast);
}
.canvas-zoom-btn:hover { background: var(--color-gray-100); }
.canvas-zoom-btn--reset { font-size: 14px; margin-left: var(--spacing-1); }
.canvas-zoom-label {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  min-width: 36px;
  text-align: center;
}

/* ========== CENTER: Visual canvas ========== */
.canvas-panel {
  flex: 1;
  display: flex;
  flex-direction: column;
  padding: 0;
  background: #e5e5e5;
  overflow: hidden;
  min-width: 0;
}

.canvas-scroll-area {
  flex: 1;
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-6);
  cursor: grab;
  user-select: none;
}
.canvas-scroll-area--panning {
  cursor: grabbing;
}

/* Dark mode: gradient from dark edges to lighter center for smooth
   transition into the always-white frame */
:root[data-theme='dark'] .canvas-scroll-area {
  background: radial-gradient(ellipse at center, #3a3a3a 0%, #1a1a1a 100%);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .canvas-scroll-area {
    background: radial-gradient(ellipse at center, #3a3a3a 0%, #1a1a1a 100%);
  }
}

.canvas-frame {
  position: relative;
  border-radius: var(--radius-md);
  overflow: hidden;
  box-shadow: var(--shadow-lg);
  /* Dark default so white text from Figma is visible; overridden by placeholder */
  background: #374151;
  flex-shrink: 0;
  will-change: transform;
}

.canvas-frame__placeholder {
  position: absolute;
  inset: 0;
  z-index: 0;
  /* Dark default — matches the inline style fallback */
  background: #374151;
}

.canvas-empty {
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

/* Text field overlay — sits on the canvas frame,
   so colours come from the Figma data, not the theme */
.canvas-text-field {
  position: absolute;
  z-index: 1;
  overflow: hidden;
  border: 1px solid transparent;
  border-radius: 2px;
  background: transparent;
  cursor: pointer;
  padding: 0;
  margin: 0;
  line-height: 1.2;
  transition: border-color var(--transition-fast), background var(--transition-fast);
  display: flex;
  align-items: flex-start;
  word-break: break-word;
}

.canvas-text-field:hover {
  border-color: var(--color-primary-300);
  background: color-mix(in srgb, var(--color-primary-500) 8%, transparent);
}

.canvas-text-field--selected {
  border-color: var(--color-primary-500);
  border-width: 2px;
  background: color-mix(in srgb, var(--color-primary-500) 12%, transparent);
  box-shadow: 0 0 0 2px color-mix(in srgb, var(--color-primary-500) 25%, transparent);
}

/* ========== RIGHT SIDEBAR: Field editor ========== */
.field-editor {
  position: fixed;
  right: 0;
  top: 0;
  bottom: 0;
  width: 380px;
  border-left: 1px solid var(--color-border);
  display: flex;
  flex-direction: column;
  background: var(--color-surface);
  overflow-y: auto;
  z-index: 10;
  box-shadow: -4px 0 24px rgba(0, 0, 0, 0.08);
}

.field-editor__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--spacing-4);
  border-bottom: 1px solid var(--color-border);
}

.field-editor__title {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  margin: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  min-width: 0;
}

.field-editor__close {
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
  flex-shrink: 0;
  transition: background var(--transition-fast), color var(--transition-fast);
}

.field-editor__close:hover {
  background: color-mix(in srgb, var(--color-error) 10%, transparent);
  color: var(--color-error);
}

.field-editor__close svg {
  width: 14px;
  height: 14px;
}

/* Sections */
.field-editor__section {
  padding: var(--spacing-4);
  border-bottom: 1px solid var(--color-border);
  display: flex;
  flex-direction: column;
  gap: var(--spacing-2);
}

.field-editor__label {
  display: flex;
  flex-direction: column;
  gap: 1px;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-primary);
}

.field-editor__label-hint {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  font-weight: var(--font-weight-normal);
}

.field-editor__textarea {
  padding: var(--spacing-3) var(--spacing-4);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  background: var(--color-background);
  color: var(--color-text-primary);
  font-size: var(--font-size-sm);
  resize: vertical;
  font-family: inherit;
}
.field-editor__textarea--readonly {
  background: var(--color-surface);
  color: var(--color-text-muted);
  cursor: not-allowed;
  opacity: 0.8;
}

.field-editor__textarea:focus {
  outline: none;
  border-color: var(--color-primary-500);
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.15);
}

/* Content key linking - linked state */
.field-editor__linked {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--spacing-3);
  padding: var(--spacing-3);
  background: color-mix(in srgb, #22c55e 6%, transparent);
  border: 1px solid color-mix(in srgb, #22c55e 20%, transparent);
  border-radius: var(--radius-lg);
}

.field-editor__linked-info {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.field-editor__linked-key {
  font-family: monospace;
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.field-editor__linked-source {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* Content key search */
.field-editor__search {
  padding: var(--spacing-2) var(--spacing-3);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  background: var(--color-background);
  color: var(--color-text-primary);
  font-size: var(--font-size-sm);
}

.field-editor__search:focus {
  outline: none;
  border-color: var(--color-primary-500);
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.15);
}

.field-editor__search-results {
  display: flex;
  flex-direction: column;
  max-height: 200px;
  overflow-y: auto;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  background: var(--color-surface);
}

.field-editor__search-item {
  display: flex;
  flex-direction: column;
  gap: 1px;
  padding: var(--spacing-2) var(--spacing-3);
  border: none;
  border-bottom: 1px solid var(--color-border);
  background: none;
  cursor: pointer;
  text-align: left;
  font: inherit;
  color: inherit;
  transition: background var(--transition-fast);
}

.field-editor__search-item:last-child {
  border-bottom: none;
}

.field-editor__search-item:hover {
  background: color-mix(in srgb, var(--color-primary-600) 5%, transparent);
}

.field-editor__search-item:focus-visible {
  position: relative;
  outline: 2px solid var(--color-primary-500);
  outline-offset: -2px;
  background: color-mix(in srgb, var(--color-primary-600) 10%, transparent);
  z-index: 1;
}

.field-editor__search-item:disabled {
  opacity: 0.5;
  cursor: default;
}

.field-editor__search-item-key {
  font-family: monospace;
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
}

.field-editor__search-item-source {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

.field-editor__search-empty {
  padding: var(--spacing-3);
  text-align: center;
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

/* Error */
.field-editor__error {
  margin: 0;
  padding: 0 var(--spacing-4);
  color: var(--color-error);
  font-size: var(--font-size-xs);
}

/* Actions */
.field-editor__actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-2);
  padding: var(--spacing-4);
  margin-top: auto;
}

/* Editor slide transition */
.editor-slide-enter-active,
.editor-slide-leave-active {
  transition: transform 0.2s ease, opacity 0.2s ease;
}

.editor-slide-enter-from,
.editor-slide-leave-to {
  transform: translateX(100%);
  opacity: 0;
}

/* Responsive */
@media (max-width: 1024px) {
  .lib-detail__panels {
    flex-direction: column;
    height: auto;
  }

  .variant-nav {
    width: 100%;
    max-height: 200px;
    border-right: none;
    border-bottom: 1px solid var(--color-border);
  }

  .variant-nav__list {
    display: flex;
    overflow-x: auto;
    gap: var(--spacing-2);
    padding: var(--spacing-2);
  }

  .variant-nav__item {
    flex-shrink: 0;
    min-width: 160px;
  }

  .canvas-panel {
    min-height: 300px;
  }

  .field-editor {
    width: 100%;
    border-left: none;
    border-top: 1px solid var(--color-border);
  }
}

@media (max-width: 640px) {
  .lib-detail {
    height: auto;
  }
}

/* Delete confirmation modal */
.delete-overlay { position: fixed; inset: 0; background: color-mix(in srgb, var(--color-black) 45%, transparent); display: grid; place-items: center; z-index: 100; }
.delete-modal { width: min(480px, 92vw); background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-xl); padding: var(--spacing-6); display: flex; flex-direction: column; gap: var(--spacing-3); }
.delete-modal h2 { margin: 0; color: var(--color-text-primary); }
.delete-text { margin: 0; font-size: var(--font-size-sm); color: var(--color-text-secondary); line-height: 1.6; }
.delete-error { margin: 0; color: var(--color-error); font-size: var(--font-size-xs); }
.delete-actions { display: flex; justify-content: flex-end; gap: var(--spacing-2); }
</style>
