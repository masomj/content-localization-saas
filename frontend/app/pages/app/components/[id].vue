<script setup lang="ts">
import AppSkeleton from '~/components/AppSkeleton.vue'
import UiButton from '~/components/ui/Button.vue'
import { componentsClient } from '~/api/componentsClient'
import type { DesignComponentWithFields } from '~/api/componentsClient'
import { contentClient } from '~/api/contentClient'
import type { ContentItem, DesignComponentTextField } from '~/api/types'

definePageMeta({ layout: 'app' })
useSeoMeta({ title: 'Component Detail - InterCopy' })

const route = useRoute()
const router = useRouter()

const componentId = computed(() => route.params.id as string)
const projectId = computed(() => (route.query.projectId as string) || '')

const isLoading = ref(false)
const component = ref<DesignComponentWithFields | null>(null)
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

const selectedField = computed(() => {
  if (!selectedFieldId.value || !component.value) return null
  return component.value.textFields.find(f => f.id === selectedFieldId.value) ?? null
})

const filteredContentItems = computed(() => {
  const search = contentSearch.value.toLowerCase().trim()
  if (!search) return contentItems.value
  return contentItems.value.filter(
    item => item.key.toLowerCase().includes(search) || item.source.toLowerCase().includes(search),
  )
})

// Canvas scaling
const canvasContainerRef = ref<HTMLElement | null>(null)
const canvasScale = computed(() => {
  if (!component.value) return 1
  const maxWidth = 800
  const maxHeight = 600
  const scaleX = maxWidth / component.value.frameWidth
  const scaleY = maxHeight / component.value.frameHeight
  return Math.min(scaleX, scaleY, 1)
})

function selectField(field: DesignComponentTextField) {
  selectedFieldId.value = field.id
  editorText.value = field.currentText
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
    await componentsClient.updateTextField(
      selectedField.value.id,
      text,
      selectedField.value.contentItemId,
    )
    await loadComponent()
    // Re-select the field to keep the editor open with fresh data
    if (selectedFieldId.value) {
      const updatedField = component.value?.textFields.find(f => f.id === selectedFieldId.value)
      if (updatedField) {
        editorText.value = updatedField.currentText
      }
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
    await componentsClient.linkContentKey(selectedField.value.id, contentItemId)
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
    await componentsClient.unlinkContentKey(selectedField.value.id)
    await loadComponent()
  } catch (err: any) {
    editorError.value = err?.message ?? 'Failed to unlink content key'
  } finally {
    linkingFieldId.value = null
  }
}

function getLinkedContentKey(field: DesignComponentTextField): ContentItem | null {
  if (!field.contentItemId) return null
  return contentItems.value.find(item => item.id === field.contentItemId) ?? null
}

function truncate(text: string, max: number): string {
  if (!text) return ''
  return text.length > max ? `${text.slice(0, max)}...` : text
}

async function loadComponent() {
  if (!projectId.value || !componentId.value) return

  isLoading.value = true
  errorMessage.value = ''
  try {
    component.value = await componentsClient.get(projectId.value, componentId.value)
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
  } catch {
    contentItems.value = []
  } finally {
    contentLoading.value = false
  }
}

function goBack() {
  router.push(`/app/components?projectId=${encodeURIComponent(projectId.value)}`)
}

onMounted(async () => {
  await Promise.all([loadComponent(), loadContentItems()])
})
</script>

<template>
  <div class="component-detail">
    <!-- Top bar -->
    <div class="component-detail__topbar">
      <UiButton variant="ghost" size="sm" @click="goBack">
        <svg viewBox="0 0 20 20" fill="currentColor" class="btn-icon-inline">
          <path fill-rule="evenodd" d="M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z" clip-rule="evenodd" />
        </svg>
        Back to Components
      </UiButton>
      <h1 v-if="component" class="component-detail__title">{{ component.figmaFrameName }}</h1>
    </div>

    <!-- Loading state -->
    <div v-if="isLoading" class="component-detail__loading">
      <AppSkeleton :lines="6" height="1rem" />
    </div>

    <!-- Error state -->
    <div v-else-if="errorMessage" class="component-detail__error" role="alert">
      {{ errorMessage }}
    </div>

    <!-- Main three-panel layout -->
    <div v-else-if="component" class="component-detail__panels">
      <!-- LEFT SIDEBAR: Layer navigator -->
      <aside class="layer-nav">
        <div class="layer-nav__header">
          <h2 class="layer-nav__title">Text Layers</h2>
          <span class="layer-nav__count">{{ component.textFields.length }}</span>
        </div>
        <p class="layer-nav__hint">Click a layer to select it on the canvas and open the editor.</p>
        <div class="layer-nav__list" role="listbox" :aria-label="`Text layers in ${component.figmaFrameName}`">
          <button
            v-for="field in component.textFields"
            :key="field.id"
            class="layer-nav__item"
            :class="{ 'layer-nav__item--selected': selectedFieldId === field.id }"
            role="option"
            :aria-selected="selectedFieldId === field.id"
            @click="selectField(field)"
          >
            <div class="layer-nav__item-header">
              <span class="layer-nav__item-name">{{ truncate(field.figmaLayerName, 24) }}</span>
              <span
                v-if="field.contentItemId"
                class="layer-nav__link-badge"
                title="Linked to content key"
              >
                linked
              </span>
            </div>
            <span class="layer-nav__item-text">{{ truncate(field.currentText, 36) }}</span>
          </button>
          <div v-if="component.textFields.length === 0" class="layer-nav__empty">
            No text layers found in this component.
          </div>
        </div>
      </aside>

      <!-- CENTER: Visual canvas -->
      <div class="canvas-panel" ref="canvasContainerRef">
        <div
          class="canvas-frame"
          :style="{
            width: component.frameWidth * canvasScale + 'px',
            height: component.frameHeight * canvasScale + 'px',
          }"
        >
          <!-- Background -->
          <img
            v-if="component.thumbnailUrl"
            :src="component.thumbnailUrl"
            :alt="component.figmaFrameName"
            class="canvas-frame__bg"
          >
          <div v-else class="canvas-frame__placeholder" />

          <!-- Text field overlays -->
          <button
            v-for="field in component.textFields"
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
            {{ field.currentText }}
          </button>
        </div>
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
              <span>Current text</span>
              <span class="field-editor__label-hint">Edit the text content for this layer</span>
            </label>
            <textarea
              id="fieldText"
              v-model="editorText"
              class="field-editor__textarea"
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
            <UiButton size="sm" :disabled="editorSaving" @click="saveTextField">
              {{ editorSaving ? 'Saving...' : 'Save' }}
            </UiButton>
          </div>
        </aside>
      </Transition>
    </div>
  </div>
</template>

<style scoped>
.component-detail {
  display: flex;
  flex-direction: column;
  height: calc(100vh - 64px - var(--spacing-6) * 2);
  min-height: 0;
}

/* Top bar */
.component-detail__topbar {
  display: flex;
  align-items: center;
  gap: var(--spacing-3);
  margin-bottom: var(--spacing-4);
  flex-shrink: 0;
}

.component-detail__title {
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
.component-detail__loading {
  padding: var(--spacing-6);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
}

.component-detail__error {
  background: var(--color-error);
  color: var(--color-white);
  padding: var(--spacing-3) var(--spacing-4);
  border-radius: var(--radius-lg);
  font-size: var(--font-size-sm);
}

/* Three-panel layout */
.component-detail__panels {
  display: flex;
  flex: 1;
  min-height: 0;
  gap: 0;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-xl);
  overflow: hidden;
  background: var(--color-surface);
}

/* ========== LEFT SIDEBAR: Layer navigator ========== */
.layer-nav {
  width: 240px;
  flex-shrink: 0;
  border-right: 1px solid var(--color-border);
  display: flex;
  flex-direction: column;
  background: var(--color-surface);
}

.layer-nav__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--spacing-4);
  border-bottom: 1px solid var(--color-border);
}

.layer-nav__title {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  margin: 0;
}

.layer-nav__count {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  background: color-mix(in srgb, var(--color-border) 50%, var(--color-background));
  padding: 1px var(--spacing-2);
  border-radius: var(--radius-md);
}

.layer-nav__hint {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  padding: var(--spacing-2) var(--spacing-4);
  margin: 0;
  border-bottom: 1px solid var(--color-border);
  line-height: 1.4;
}

.layer-nav__list {
  flex: 1;
  overflow-y: auto;
  padding: var(--spacing-2);
}

.layer-nav__item {
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

.layer-nav__item:hover {
  background: color-mix(in srgb, var(--color-primary-600) 5%, transparent);
}

.layer-nav__item--selected {
  background: color-mix(in srgb, var(--color-primary-600) 10%, transparent);
  border-color: var(--color-primary-400);
}

.layer-nav__item-header {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
}

.layer-nav__item-name {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  min-width: 0;
}

.layer-nav__link-badge {
  flex-shrink: 0;
  font-size: 10px;
  font-weight: var(--font-weight-medium);
  color: #16a34a;
  background: color-mix(in srgb, #22c55e 15%, transparent);
  padding: 0 var(--spacing-1);
  border-radius: var(--radius-sm);
  text-transform: uppercase;
  letter-spacing: 0.02em;
}

.layer-nav__item-text {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.layer-nav__empty {
  padding: var(--spacing-4);
  text-align: center;
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

/* ========== CENTER: Visual canvas ========== */
.canvas-panel {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-6);
  /* Light mode: Figma-style grey canvas */
  background: #e5e5e5;
  overflow: auto;
  min-width: 0;
}

/* Dark mode: gradient from dark edges to lighter center for smooth
   transition into the always-white frame */
:root[data-theme='dark'] .canvas-panel {
  background: radial-gradient(ellipse at center, #3a3a3a 0%, #1a1a1a 100%);
}

@media (prefers-color-scheme: dark) {
  :root:not([data-theme='light']) .canvas-panel {
    background: radial-gradient(ellipse at center, #3a3a3a 0%, #1a1a1a 100%);
  }
}

.canvas-frame {
  position: relative;
  border-radius: var(--radius-md);
  overflow: hidden;
  box-shadow: var(--shadow-lg);
  /* Always white — matches Figma frame background regardless of theme */
  background: #ffffff;
}

.canvas-frame__bg {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  object-fit: cover;
  pointer-events: none;
}

.canvas-frame__placeholder {
  position: absolute;
  inset: 0;
  /* Light placeholder matching Figma's default frame fill */
  background: #ffffff;
}

/* Text field overlay — sits on the always-white canvas frame,
   so colours come from the Figma data, not the theme */
.canvas-text-field {
  position: absolute;
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
  width: 380px;
  flex-shrink: 0;
  border-left: 1px solid var(--color-border);
  display: flex;
  flex-direction: column;
  background: var(--color-surface);
  overflow-y: auto;
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
  .component-detail__panels {
    flex-direction: column;
    height: auto;
  }

  .layer-nav {
    width: 100%;
    max-height: 200px;
    border-right: none;
    border-bottom: 1px solid var(--color-border);
  }

  .layer-nav__list {
    display: flex;
    overflow-x: auto;
    gap: var(--spacing-2);
    padding: var(--spacing-2);
  }

  .layer-nav__item {
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
  .component-detail {
    height: auto;
  }
}
</style>
