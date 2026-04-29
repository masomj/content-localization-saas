<script setup lang="ts">
import { ref, computed, watch, nextTick, onMounted } from 'vue'
import { screenshotsClient } from '~/api/screenshotsClient'
import { translationClient } from '~/api/translationClient'
import { projectsClient } from '~/api/projectsClient'
import { languagesClient } from '~/api/languagesClient'
import type { ScreenshotContextDetail, ScreenshotContextRegion, Project, ProjectLanguage } from '~/api/types'
import { useAuth } from '~/composables/useAuth'
import UiButton from '~/components/ui/Button.vue'

definePageMeta({ layout: 'canvas', middleware: ['feature-flags'] })

const route = useRoute()
const router = useRouter()
const auth = useAuth()
const workspaceId = computed(() => auth.workspace.value?.id ?? '')

const screenshotId = computed(() => route.params.id as string)

const projects = ref<Project[]>([])
const selectedProjectId = ref('')
const languages = ref<ProjectLanguage[]>([])
const selectedLanguage = ref('')

const screenshot = ref<ScreenshotContextDetail | null>(null)
const loading = ref(true)
const error = ref('')
const zoom = ref(100)
const savingRegions = ref<Set<string>>(new Set())
const savedRegions = ref<Set<string>>(new Set())
const truncatedRegions = ref<Set<string>>(new Set())

// Zoom controls
const zoomMin = 25
const zoomMax = 400
function zoomIn() {
  zoom.value = Math.min(zoom.value + 25, zoomMax)
}
function zoomOut() {
  zoom.value = Math.max(zoom.value - 25, zoomMin)
}

// Load projects + languages
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

async function loadLanguages() {
  if (!selectedProjectId.value) return
  try {
    languages.value = await languagesClient.list(selectedProjectId.value)
    if (languages.value.length > 0 && !selectedLanguage.value) {
      selectedLanguage.value = languages.value[0].code
    }
  } catch (_) { /* ignore */ }
}

async function loadScreenshot() {
  if (!selectedProjectId.value || !selectedLanguage.value || !screenshotId.value) return
  loading.value = true
  error.value = ''
  try {
    screenshot.value = await screenshotsClient.getContext(
      selectedProjectId.value,
      screenshotId.value,
      selectedLanguage.value,
    )
  } catch (err: any) {
    error.value = err?.message ?? 'Failed to load screenshot context'
  } finally {
    loading.value = false
  }
}

// Auto-save on blur
async function saveRegion(region: ScreenshotContextRegion) {
  if (!region.contentItemId) return
  const text = region.translationText ?? ''
  savingRegions.value.add(region.id)
  try {
    await translationClient.upsert({
      contentItemId: region.contentItemId,
      languageCode: selectedLanguage.value,
      status: region.translationStatus ?? 'in_progress',
      translationText: text,
    })
    savedRegions.value.add(region.id)
    setTimeout(() => savedRegions.value.delete(region.id), 2000)
  } catch (_) { /* ignore save failures silently */ }
  finally {
    savingRegions.value.delete(region.id)
  }
}

// Truncation check
function checkTruncation(region: ScreenshotContextRegion, event: Event) {
  const el = event.target as HTMLTextAreaElement
  if (el.scrollWidth > el.clientWidth || el.scrollHeight > el.clientHeight) {
    truncatedRegions.value.add(region.id)
  } else {
    truncatedRegions.value.delete(region.id)
  }
}

// Keyboard navigation
function handleKeydown(event: KeyboardEvent, index: number) {
  if (!screenshot.value) return
  const linked = screenshot.value.regions.filter(r => r.contentItemId)
  if (event.key === 'Tab') {
    event.preventDefault()
    const next = event.shiftKey
      ? (index - 1 + linked.length) % linked.length
      : (index + 1) % linked.length
    nextTick(() => {
      const el = document.querySelector(`[data-region-index="${next}"]`) as HTMLTextAreaElement | null
      el?.focus()
    })
  }
}

function goBack() {
  router.push(`/app/screenshots`)
}

// Watchers
watch([workspaceId], loadProjects, { immediate: true })
watch(selectedProjectId, loadLanguages)
watch([selectedProjectId, selectedLanguage, screenshotId], loadScreenshot)

onMounted(async () => {
  await loadProjects()
  await loadLanguages()
  await loadScreenshot()
})
</script>

<template>
  <div class="context-editor">
    <!-- Toolbar -->
    <header class="context-editor__toolbar">
      <div class="context-editor__toolbar-left">
        <UiButton variant="ghost" size="sm" @click="goBack">&larr; Back</UiButton>
        <h1 class="context-editor__title">Edit in Context</h1>
      </div>

      <div class="context-editor__toolbar-center">
        <label class="context-editor__select-label">
          <span>Project</span>
          <select v-model="selectedProjectId" class="context-editor__select">
            <option v-for="p in projects" :key="p.id" :value="p.id">{{ p.name }}</option>
          </select>
        </label>
        <label class="context-editor__select-label">
          <span>Language</span>
          <select v-model="selectedLanguage" class="context-editor__select">
            <option v-for="lang in languages" :key="lang.code" :value="lang.code">{{ lang.code }} &mdash; {{ lang.name }}</option>
          </select>
        </label>
      </div>

      <div class="context-editor__toolbar-right">
        <UiButton variant="ghost" size="sm" :disabled="zoom <= zoomMin" @click="zoomOut">&minus;</UiButton>
        <span class="context-editor__zoom-label">{{ zoom }}%</span>
        <UiButton variant="ghost" size="sm" :disabled="zoom >= zoomMax" @click="zoomIn">+</UiButton>
      </div>
    </header>

    <!-- Loading -->
    <div v-if="loading" class="context-editor__loading">Loading screenshot context&hellip;</div>

    <!-- Error -->
    <div v-else-if="error" class="context-editor__error" role="alert">
      {{ error }}
      <UiButton variant="secondary" size="sm" @click="loadScreenshot">Retry</UiButton>
    </div>

    <!-- Canvas -->
    <div v-else-if="screenshot" class="context-editor__canvas-wrapper">
      <div
        class="context-editor__canvas"
        :style="{ transform: `scale(${zoom / 100})`, transformOrigin: 'top left' }"
      >
        <img
          :src="`/${screenshot.storagePath}`"
          :alt="screenshot.fileName"
          class="context-editor__image"
          draggable="false"
        />

        <!-- Editable overlays for linked regions -->
        <template v-for="(region, idx) in screenshot.regions.filter(r => r.contentItemId)" :key="region.id">
          <div
            class="context-editor__overlay"
            :class="{
              'context-editor__overlay--truncated': truncatedRegions.has(region.id),
              'context-editor__overlay--saving': savingRegions.has(region.id),
            }"
            :style="{
              left: `${region.x}px`,
              top: `${region.y}px`,
              width: `${region.width}px`,
              height: `${region.height}px`,
            }"
          >
            <textarea
              :data-region-index="idx"
              v-model="region.translationText"
              class="context-editor__textarea"
              @blur="saveRegion(region)"
              @input="checkTruncation(region, $event)"
              @keydown="handleKeydown($event, idx)"
            />
            <!-- Save indicator -->
            <span v-if="savedRegions.has(region.id)" class="context-editor__saved" aria-label="Saved">&#10003;</span>
          </div>
        </template>
      </div>
    </div>

    <div v-else class="context-editor__empty">
      No screenshot data available.
    </div>
  </div>
</template>

<style scoped>
.context-editor {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background: var(--color-background);
  color: var(--color-text-primary);
}

.context-editor__toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--spacing-2) var(--spacing-4);
  border-bottom: 1px solid var(--color-border);
  background: var(--color-surface);
  gap: var(--spacing-4);
  flex-shrink: 0;
}

.context-editor__toolbar-left,
.context-editor__toolbar-right {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
}

.context-editor__toolbar-center {
  display: flex;
  align-items: center;
  gap: var(--spacing-4);
}

.context-editor__title {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  margin: 0;
}

.context-editor__select-label {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
}

.context-editor__select {
  padding: var(--spacing-1) var(--spacing-2);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-surface);
  color: var(--color-text-primary);
  font-size: var(--font-size-sm);
}

.context-editor__zoom-label {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  min-width: 3rem;
  text-align: center;
}

.context-editor__loading,
.context-editor__error,
.context-editor__empty {
  display: flex;
  align-items: center;
  justify-content: center;
  flex: 1;
  gap: var(--spacing-2);
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.context-editor__error {
  color: var(--color-error);
}

.context-editor__canvas-wrapper {
  flex: 1;
  overflow: auto;
  padding: var(--spacing-4);
}

.context-editor__canvas {
  position: relative;
  display: inline-block;
  transition: transform var(--transition-fast);
}

.context-editor__image {
  display: block;
  max-width: none;
  user-select: none;
}

.context-editor__overlay {
  position: absolute;
  border: 2px solid var(--color-primary-400);
  border-radius: var(--radius-sm);
  background: color-mix(in srgb, var(--color-primary-100) 50%, transparent);
  transition: border-color var(--transition-fast);
}

.context-editor__overlay--truncated {
  border-color: var(--color-error);
}

.context-editor__overlay--saving {
  opacity: 0.7;
}

.context-editor__textarea {
  width: 100%;
  height: 100%;
  border: none;
  background: transparent;
  color: var(--color-text-primary);
  font-size: var(--font-size-sm);
  font-family: inherit;
  padding: var(--spacing-1);
  resize: none;
  outline: none;
}

.context-editor__textarea:focus {
  background: color-mix(in srgb, var(--color-surface) 80%, transparent);
}

.context-editor__saved {
  position: absolute;
  top: -0.5rem;
  right: -0.5rem;
  width: 1.25rem;
  height: 1.25rem;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--color-success);
  color: #fff;
  border-radius: var(--radius-full);
  font-size: var(--font-size-xs);
  animation: context-editor-fade 2s ease-out forwards;
}

@keyframes context-editor-fade {
  0%, 70% { opacity: 1; }
  100% { opacity: 0; }
}
</style>
