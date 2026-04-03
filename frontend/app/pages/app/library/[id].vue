<script setup lang="ts">
import AppSkeleton from '~/components/AppSkeleton.vue'
import UiButton from '~/components/ui/Button.vue'
import { libraryClient } from '~/api/libraryClient'
import { contentClient } from '~/api/contentClient'
import type { LibraryComponentWithVariants, LibraryComponentVariant, LibraryComponentTextField, ContentItem } from '~/api/types'

definePageMeta({ layout: 'app' })
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

// Canvas scaling
const canvasScale = computed(() => {
  if (!component.value) return 1
  const maxWidth = 800
  const maxHeight = 600
  const scaleX = maxWidth / component.value.frameWidth
  const scaleY = maxHeight / component.value.frameHeight
  return Math.min(scaleX, scaleY, 1)
})

function selectVariant(variant: LibraryComponentVariant) {
  selectedVariantId.value = variant.id
  selectedFieldId.value = null
  editorText.value = ''
  editorError.value = ''
}

function selectField(field: LibraryComponentTextField) {
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

async function loadComponent() {
  if (!componentId.value) return
  isLoading.value = true
  errorMessage.value = ''
  try {
    component.value = await libraryClient.get(componentId.value)
    if (component.value.variants.length > 0 && !selectedVariantId.value) {
      selectedVariantId.value = component.value.variants[0]!.id
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

onMounted(async () => {
  await Promise.all([loadComponent(), loadContentItems()])
})
</script>

<template>
  <div class="lib-detail">
    <div class="lib-detail__topbar">
      <UiButton variant="ghost" size="sm" @click="goBack">
        <svg viewBox="0 0 20 20" fill="currentColor" class="btn-icon-inline">
          <path fill-rule="evenodd" d="M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z" clip-rule="evenodd" />
        </svg>
        Back to Library
      </UiButton>
      <h1 v-if="component" class="lib-detail__title">{{ component.name || 'Library Component' }}</h1>
    </div>

    <div v-if="isLoading" class="lib-detail__loading">
      <AppSkeleton :lines="6" height="1rem" />
    </div>

    <div v-else-if="errorMessage" class="lib-detail__error" role="alert">{{ errorMessage }}</div>

    <div v-else-if="component" class="lib-detail__panels">
      <!-- LEFT: Variant navigator -->
      <aside class="variant-nav">
        <div class="variant-nav__header">
          <h2 class="variant-nav__title">Variants</h2>
          <span class="variant-nav__count">{{ component.variants.length }}</span>
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
            <span class="variant-nav__item-name">{{ variant.variantName || 'Default' }}</span>
            <span class="variant-nav__item-count">{{ (variant.textFields ?? []).length }} fields</span>
          </button>
          <div v-if="component.variants.length === 0" class="variant-nav__empty">
            No variants found. Push this component from the Figma plugin.
          </div>
        </div>
      </aside>

      <!-- CENTER: Canvas -->
      <div class="canvas-panel">
        <div
          v-if="selectedVariant"
          class="canvas-frame"
          :style="{
            width: component.frameWidth * canvasScale + 'px',
            height: component.frameHeight * canvasScale + 'px',
          }"
        >
          <div class="canvas-frame__placeholder" />
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
              color: field.color || '#333',
            }"
            :title="field.figmaLayerName"
            @click="selectField(field)"
          >
            {{ field.currentText }}
          </button>
        </div>
        <div v-else class="canvas-empty">Select a variant to preview</div>
      </div>

      <!-- RIGHT: Field editor -->
      <Transition name="editor-slide">
        <aside v-if="selectedField" class="field-editor">
          <div class="field-editor__header">
            <h2 class="field-editor__title">{{ selectedField.figmaLayerName }}</h2>
            <button class="field-editor__close" aria-label="Close editor" @click="closeEditor">
              <svg viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" /></svg>
            </button>
          </div>

          <div class="field-editor__section">
            <label for="fieldText" class="field-editor__label">
              <span>Current text</span>
              <span class="field-editor__label-hint">Edit the text content for this layer</span>
            </label>
            <textarea id="fieldText" v-model="editorText" class="field-editor__textarea" rows="4" />
          </div>

          <div class="field-editor__section">
            <label class="field-editor__label">
              <span>Content key</span>
              <span class="field-editor__label-hint">Link to an i18n content key for translations</span>
            </label>

            <div v-if="selectedField.contentItemId" class="field-editor__linked">
              <div class="field-editor__linked-info">
                <span class="field-editor__linked-key">{{ getLinkedContentKey(selectedField)?.key ?? selectedField.contentItemId }}</span>
                <span v-if="getLinkedContentKey(selectedField)" class="field-editor__linked-source">{{ truncate(getLinkedContentKey(selectedField)!.source, 60) }}</span>
              </div>
              <UiButton variant="secondary" size="sm" :disabled="linkingFieldId === selectedField.id" @click="unlinkContentKey">Unlink</UiButton>
            </div>

            <template v-else>
              <input v-model="contentSearch" type="text" class="field-editor__search" placeholder="Search content keys..." autocomplete="off">
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
                <div v-if="filteredContentItems.length === 0" class="field-editor__search-empty">No matching content keys found.</div>
              </div>
            </template>
          </div>

          <p v-if="editorError" class="field-editor__error">{{ editorError }}</p>

          <div class="field-editor__actions">
            <UiButton variant="secondary" size="sm" @click="closeEditor">Close</UiButton>
            <UiButton size="sm" :disabled="editorSaving" @click="saveTextField">{{ editorSaving ? 'Saving...' : 'Save' }}</UiButton>
          </div>
        </aside>
      </Transition>
    </div>
  </div>
</template>

<style scoped>
.lib-detail { display: flex; flex-direction: column; height: calc(100vh - 64px - var(--spacing-6) * 2); min-height: 0; }
.lib-detail__topbar { display: flex; align-items: center; gap: var(--spacing-3); margin-bottom: var(--spacing-4); flex-shrink: 0; }
.lib-detail__title { font-size: var(--font-size-lg); font-weight: var(--font-weight-semibold); color: var(--color-text-primary); margin: 0; }
.btn-icon-inline { width: 1em; height: 1em; flex-shrink: 0; }
.lib-detail__loading { padding: var(--spacing-6); background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-lg); }
.lib-detail__error { background: var(--color-error); color: var(--color-white); padding: var(--spacing-3) var(--spacing-4); border-radius: var(--radius-lg); font-size: var(--font-size-sm); }

.lib-detail__panels { display: flex; flex: 1; min-height: 0; gap: 0; border: 1px solid var(--color-border); border-radius: var(--radius-xl); overflow: hidden; background: var(--color-surface); }

/* Variant navigator */
.variant-nav { width: 240px; flex-shrink: 0; border-right: 1px solid var(--color-border); display: flex; flex-direction: column; background: var(--color-surface); }
.variant-nav__header { display: flex; align-items: center; justify-content: space-between; padding: var(--spacing-4); border-bottom: 1px solid var(--color-border); }
.variant-nav__title { font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold); color: var(--color-text-primary); margin: 0; }
.variant-nav__count { font-size: var(--font-size-xs); color: var(--color-text-muted); background: color-mix(in srgb, var(--color-border) 50%, var(--color-background)); padding: 1px var(--spacing-2); border-radius: var(--radius-md); }
.variant-nav__hint { font-size: var(--font-size-xs); color: var(--color-text-muted); padding: var(--spacing-2) var(--spacing-4); margin: 0; border-bottom: 1px solid var(--color-border); line-height: 1.4; }
.variant-nav__list { flex: 1; overflow-y: auto; padding: var(--spacing-2); }
.variant-nav__item { display: flex; flex-direction: column; gap: 2px; width: 100%; padding: var(--spacing-2) var(--spacing-3); border: 1px solid transparent; border-radius: var(--radius-md); background: none; cursor: pointer; text-align: left; font: inherit; color: inherit; transition: background var(--transition-fast), border-color var(--transition-fast); }
.variant-nav__item:hover { background: color-mix(in srgb, var(--color-primary-600) 5%, transparent); }
.variant-nav__item--selected { background: color-mix(in srgb, var(--color-primary-600) 10%, transparent); border-color: var(--color-primary-400); }
.variant-nav__item-name { font-size: var(--font-size-xs); font-weight: var(--font-weight-semibold); color: var(--color-text-primary); }
.variant-nav__item-count { font-size: var(--font-size-xs); color: var(--color-text-muted); }
.variant-nav__empty { padding: var(--spacing-4); text-align: center; font-size: var(--font-size-xs); color: var(--color-text-muted); }

/* Canvas */
.canvas-panel { flex: 1; display: flex; align-items: center; justify-content: center; padding: var(--spacing-6); background: #e5e5e5; overflow: auto; min-width: 0; }
:root[data-theme='dark'] .canvas-panel { background: radial-gradient(ellipse at center, #3a3a3a 0%, #1a1a1a 100%); }
@media (prefers-color-scheme: dark) { :root:not([data-theme='light']) .canvas-panel { background: radial-gradient(ellipse at center, #3a3a3a 0%, #1a1a1a 100%); } }
.canvas-frame { position: relative; border-radius: var(--radius-md); overflow: hidden; box-shadow: var(--shadow-lg); background: #ffffff; }
.canvas-frame__placeholder { position: absolute; inset: 0; background: #ffffff; }
.canvas-empty { color: var(--color-text-muted); font-size: var(--font-size-sm); }
.canvas-text-field { position: absolute; overflow: hidden; border: 1px solid transparent; border-radius: 2px; background: transparent; cursor: pointer; padding: 0; margin: 0; line-height: 1.2; transition: border-color var(--transition-fast), background var(--transition-fast); display: flex; align-items: flex-start; word-break: break-word; }
.canvas-text-field:hover { border-color: var(--color-primary-300); background: color-mix(in srgb, var(--color-primary-500) 8%, transparent); }
.canvas-text-field--selected { border-color: var(--color-primary-500); border-width: 2px; background: color-mix(in srgb, var(--color-primary-500) 12%, transparent); box-shadow: 0 0 0 2px color-mix(in srgb, var(--color-primary-500) 25%, transparent); }

/* Field editor */
.field-editor { width: 380px; flex-shrink: 0; border-left: 1px solid var(--color-border); display: flex; flex-direction: column; background: var(--color-surface); overflow-y: auto; }
.field-editor__header { display: flex; align-items: center; justify-content: space-between; padding: var(--spacing-4); border-bottom: 1px solid var(--color-border); }
.field-editor__title { font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold); color: var(--color-text-primary); margin: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; min-width: 0; }
.field-editor__close { display: inline-flex; align-items: center; justify-content: center; width: 28px; height: 28px; padding: 0; border: 1px solid var(--color-border); border-radius: var(--radius-md); background: var(--color-surface); color: var(--color-text-muted); cursor: pointer; flex-shrink: 0; transition: background var(--transition-fast), color var(--transition-fast); }
.field-editor__close:hover { background: color-mix(in srgb, var(--color-error) 10%, transparent); color: var(--color-error); }
.field-editor__close svg { width: 14px; height: 14px; }
.field-editor__section { padding: var(--spacing-4); border-bottom: 1px solid var(--color-border); display: flex; flex-direction: column; gap: var(--spacing-2); }
.field-editor__label { display: flex; flex-direction: column; gap: 1px; font-size: var(--font-size-sm); font-weight: var(--font-weight-medium); color: var(--color-text-primary); }
.field-editor__label-hint { font-size: var(--font-size-xs); color: var(--color-text-muted); font-weight: var(--font-weight-normal); }
.field-editor__textarea { padding: var(--spacing-3) var(--spacing-4); border: 1px solid var(--color-border); border-radius: var(--radius-lg); background: var(--color-background); color: var(--color-text-primary); font-size: var(--font-size-sm); resize: vertical; font-family: inherit; }
.field-editor__textarea:focus { outline: none; border-color: var(--color-primary-500); box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.15); }
.field-editor__linked { display: flex; align-items: center; justify-content: space-between; gap: var(--spacing-3); padding: var(--spacing-3); background: color-mix(in srgb, #22c55e 6%, transparent); border: 1px solid color-mix(in srgb, #22c55e 20%, transparent); border-radius: var(--radius-lg); }
.field-editor__linked-info { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
.field-editor__linked-key { font-family: monospace; font-size: var(--font-size-xs); font-weight: var(--font-weight-semibold); color: var(--color-text-primary); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.field-editor__linked-source { font-size: var(--font-size-xs); color: var(--color-text-muted); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.field-editor__search { padding: var(--spacing-2) var(--spacing-3); border: 1px solid var(--color-border); border-radius: var(--radius-lg); background: var(--color-background); color: var(--color-text-primary); font-size: var(--font-size-sm); }
.field-editor__search:focus { outline: none; border-color: var(--color-primary-500); box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.15); }
.field-editor__search-results { display: flex; flex-direction: column; max-height: 200px; overflow-y: auto; border: 1px solid var(--color-border); border-radius: var(--radius-lg); background: var(--color-surface); }
.field-editor__search-item { display: flex; flex-direction: column; gap: 1px; padding: var(--spacing-2) var(--spacing-3); border: none; border-bottom: 1px solid var(--color-border); background: none; cursor: pointer; text-align: left; font: inherit; color: inherit; transition: background var(--transition-fast); }
.field-editor__search-item:last-child { border-bottom: none; }
.field-editor__search-item:hover { background: color-mix(in srgb, var(--color-primary-600) 5%, transparent); }
.field-editor__search-item:disabled { opacity: 0.5; cursor: default; }
.field-editor__search-item-key { font-family: monospace; font-size: var(--font-size-xs); font-weight: var(--font-weight-semibold); color: var(--color-text-primary); }
.field-editor__search-item-source { font-size: var(--font-size-xs); color: var(--color-text-muted); }
.field-editor__search-empty { padding: var(--spacing-3); text-align: center; font-size: var(--font-size-xs); color: var(--color-text-muted); }
.field-editor__error { margin: 0; padding: 0 var(--spacing-4); color: var(--color-error); font-size: var(--font-size-xs); }
.field-editor__actions { display: flex; justify-content: flex-end; gap: var(--spacing-2); padding: var(--spacing-4); margin-top: auto; }

.editor-slide-enter-active, .editor-slide-leave-active { transition: transform 0.2s ease, opacity 0.2s ease; }
.editor-slide-enter-from, .editor-slide-leave-to { transform: translateX(100%); opacity: 0; }

@media (max-width: 1024px) {
  .lib-detail__panels { flex-direction: column; height: auto; }
  .variant-nav { width: 100%; max-height: 200px; border-right: none; border-bottom: 1px solid var(--color-border); }
  .canvas-panel { min-height: 300px; }
  .field-editor { width: 100%; border-left: none; border-top: 1px solid var(--color-border); }
}
</style>
