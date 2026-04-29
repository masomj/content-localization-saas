<script setup lang="ts">
import UiButton from '~/components/ui/Button.vue'
import { localeImportsClient } from '~/api/localeImportsClient'
import type { LocaleImportFileMapping, LocaleImportResult } from '~/api/types'

const props = defineProps<{
  projectId: string
}>()

const emit = defineEmits<{
  close: []
  imported: [result: LocaleImportResult]
}>()

interface ImportFileRow extends LocaleImportFileMapping {
  file: File
}

const fileInput = ref<HTMLInputElement | null>(null)
const fileRows = ref<ImportFileRow[]>([])
const sourceLanguageCode = ref('')
const createMissingLanguages = ref(true)
const isImporting = ref(false)
const importError = ref('')

const canImport = computed(() => {
  if (!props.projectId || !sourceLanguageCode.value.trim()) return false
  if (fileRows.value.length === 0) return false
  return fileRows.value.every(row => row.languageCode.trim().length > 0)
})

function openPicker() {
  fileInput.value?.click()
}

function resetFiles() {
  fileRows.value = []
  sourceLanguageCode.value = ''
  importError.value = ''
  if (fileInput.value) {
    fileInput.value.value = ''
  }
}

function onFilesSelected(event: Event) {
  const input = event.target as HTMLInputElement
  const files = Array.from(input.files ?? [])
  importError.value = ''

  if (files.length === 0) {
    fileRows.value = []
    sourceLanguageCode.value = ''
    return
  }

  fileRows.value = files.map(file => {
    const inferred = inferMapping(file)
    return {
      file,
      fileName: file.name,
      languageCode: inferred.languageCode,
      namespacePrefix: inferred.namespacePrefix,
    }
  })

  const preferredSource = fileRows.value.find(row => row.languageCode)?.languageCode ?? ''
  sourceLanguageCode.value = preferredSource
}

function inferMapping(file: File): LocaleImportFileMapping {
  const baseName = file.name.replace(/\.[^.]+$/, '')
  const parts = baseName.split('.').filter(Boolean)
  const languageIndex = [...parts].map((part, index) => ({ part, index })).reverse().find(x => isValidBcp47(normalizeLanguageCode(x.part)))?.index ?? -1

  if (languageIndex < 0) {
    return {
      fileName: file.name,
      languageCode: '',
      namespacePrefix: null,
    }
  }

  const namespacePrefix = parts.slice(0, languageIndex).map(sanitizeNamespaceSegment).filter(Boolean).join('.') || null

  return {
    fileName: file.name,
    languageCode: normalizeLanguageCode(parts[languageIndex] || ''),
    namespacePrefix,
  }
}

function normalizeLanguageCode(value: string) {
  const parts = value.trim().split('-').filter(Boolean)
  return parts
    .map((part, index) => {
      if (index === 0) return part.toLowerCase()
      if (part.length === 2 || part.length === 3) return part.toUpperCase()
      return part.charAt(0).toUpperCase() + part.slice(1).toLowerCase()
    })
    .join('-')
}

function isValidBcp47(value: string) {
  return /^[A-Za-z]{2,3}(-[A-Za-z0-9]{2,8})*$/.test(value)
}

function sanitizeNamespaceSegment(value: string) {
  return value
    .trim()
    .toLowerCase()
    .replace(/[\/\s]+/g, '.')
    .replace(/[^a-z0-9._-]/g, '_')
    .replace(/\.{2,}/g, '.')
    .replace(/_{2,}/g, '_')
    .replace(/^[._-]+|[._-]+$/g, '')
}

async function submitImport() {
  if (!canImport.value) return

  const normalizedSourceLanguage = normalizeLanguageCode(sourceLanguageCode.value)
  if (!isValidBcp47(normalizedSourceLanguage)) {
    importError.value = 'Choose a valid source language code.'
    return
  }

  const invalidMapping = fileRows.value.find(row => !isValidBcp47(normalizeLanguageCode(row.languageCode)))
  if (invalidMapping) {
    importError.value = `File ${invalidMapping.fileName} needs a valid language code.`
    return
  }

  isImporting.value = true
  importError.value = ''

  try {
    const formData = new FormData()
    formData.append('sourceLanguageCode', normalizedSourceLanguage)
    formData.append('createMissingLanguages', String(createMissingLanguages.value))
    formData.append('mappingsJson', JSON.stringify(fileRows.value.map(row => ({
      fileName: row.fileName,
      languageCode: normalizeLanguageCode(row.languageCode),
      namespacePrefix: row.namespacePrefix || null,
    }))))

    for (const row of fileRows.value) {
      formData.append('files', row.file)
    }

    const result = await localeImportsClient.import(props.projectId, formData)
    emit('imported', result)
  } catch (error: any) {
    importError.value = error?.message || 'Import failed'
  } finally {
    isImporting.value = false
  }
}
</script>

<template>
  <div class="modal-overlay" @click.self="emit('close')">
    <div class="modal-card locale-import-modal">
      <div class="locale-import-header">
        <div>
          <h2>Import locale files</h2>
          <p class="locale-import-subtitle">Upload existing i18n JSON files and turn them into content keys and translations.</p>
        </div>
        <button class="locale-import-close" @click="emit('close')">
          <svg viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" /></svg>
        </button>
      </div>

      <input
        ref="fileInput"
        class="sr-only"
        type="file"
        accept=".json,application/json"
        multiple
        @change="onFilesSelected"
      >

      <section class="locale-import-section">
        <label class="label-with-hint">
          <span>Files</span>
          <span class="label-hint">One file per locale is ideal. Filenames like <code>common.en.json</code> or <code>fr-FR.json</code> are auto-detected.</span>
        </label>
        <div class="locale-import-file-actions">
          <UiButton variant="secondary" @click="openPicker">Choose files</UiButton>
          <UiButton v-if="fileRows.length > 0" variant="ghost" @click="resetFiles">Clear</UiButton>
        </div>
      </section>

      <section v-if="fileRows.length > 0" class="locale-import-section">
        <label class="label-with-hint">
          <span>Detected files</span>
          <span class="label-hint">Adjust any language code before importing.</span>
        </label>

        <div class="locale-import-file-list">
          <div v-for="row in fileRows" :key="row.fileName" class="locale-import-file-row">
            <div class="locale-import-file-meta">
              <p class="locale-import-file-name">{{ row.fileName }}</p>
              <p v-if="row.namespacePrefix" class="locale-import-file-namespace">
                Key prefix: <code>{{ row.namespacePrefix }}</code>
              </p>
              <p v-else class="locale-import-file-namespace">No filename namespace prefix detected.</p>
            </div>
            <div class="locale-import-file-language">
              <label :for="`lang-${row.fileName}`" class="label-with-hint compact-label">
                <span>Language</span>
                <span class="label-hint">BCP-47 code</span>
              </label>
              <input :id="`lang-${row.fileName}`" v-model="row.languageCode" type="text" class="locale-import-input">
            </div>
          </div>
        </div>
      </section>

      <section v-if="fileRows.length > 0" class="locale-import-section locale-import-options">
        <div>
          <label for="sourceLanguageCode" class="label-with-hint">
            <span>Source language</span>
            <span class="label-hint">This file becomes the source text for content items.</span>
          </label>
          <select id="sourceLanguageCode" v-model="sourceLanguageCode" class="locale-import-select">
            <option value="">Choose source language</option>
            <option v-for="row in fileRows" :key="`${row.fileName}-source`" :value="normalizeLanguageCode(row.languageCode)">
              {{ normalizeLanguageCode(row.languageCode) || `${row.fileName} (set language first)` }}
            </option>
          </select>
        </div>

        <label class="locale-import-checkbox">
          <input v-model="createMissingLanguages" type="checkbox">
          <span>Create missing project languages automatically</span>
        </label>
      </section>

      <p v-if="importError" class="locale-import-error">{{ importError }}</p>

      <div class="locale-import-actions">
        <UiButton variant="secondary" @click="emit('close')">Cancel</UiButton>
        <UiButton :disabled="!canImport || isImporting" @click="submitImport">
          {{ isImporting ? 'Importing...' : 'Import locale files' }}
        </UiButton>
      </div>
    </div>
  </div>
</template>

<style scoped>
.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}

.modal-overlay {
  position: fixed;
  inset: 0;
  display: grid;
  place-items: center;
  padding: var(--spacing-4);
  background: color-mix(in srgb, var(--color-black) 45%, transparent);
  z-index: var(--z-modal);
}

.modal-card {
  width: min(920px, 100%);
  max-height: min(88vh, 920px);
  overflow: auto;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-xl);
  box-shadow: var(--shadow-xl);
}

.locale-import-modal {
  padding: var(--spacing-6);
  display: flex;
  flex-direction: column;
  gap: var(--spacing-4);
}

.locale-import-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: var(--spacing-4);
}

.locale-import-header h2 {
  margin: 0;
  color: var(--color-text-primary);
}

.locale-import-subtitle {
  margin: var(--spacing-1) 0 0;
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.locale-import-close {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-surface);
  color: var(--color-text-muted);
  cursor: pointer;
}

.locale-import-close svg {
  width: 16px;
  height: 16px;
}

.locale-import-close:hover {
  color: var(--color-error);
  border-color: color-mix(in srgb, var(--color-error) 50%, var(--color-border));
  background: color-mix(in srgb, var(--color-error) 8%, var(--color-surface));
}

.locale-import-section {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-2);
}

.label-with-hint {
  display: flex;
  flex-direction: column;
  gap: 2px;
  color: var(--color-text-primary);
}

.label-hint {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

.compact-label {
  margin-bottom: var(--spacing-1);
}

.locale-import-file-actions {
  display: flex;
  gap: var(--spacing-2);
  align-items: center;
}

.locale-import-file-list {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-3);
}

.locale-import-file-row {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 190px;
  gap: var(--spacing-4);
  padding: var(--spacing-4);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  background: color-mix(in srgb, var(--color-primary-600) 4%, var(--color-surface));
}

.locale-import-file-meta {
  min-width: 0;
}

.locale-import-file-name {
  margin: 0;
  color: var(--color-text-primary);
  font-weight: var(--font-weight-semibold);
  word-break: break-word;
}

.locale-import-file-namespace {
  margin: var(--spacing-1) 0 0;
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.locale-import-file-language {
  display: flex;
  flex-direction: column;
}

.locale-import-input,
.locale-import-select {
  width: 100%;
  padding: var(--spacing-3) var(--spacing-4);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  background: var(--color-background);
  color: var(--color-text-primary);
  font-size: var(--font-size-sm);
}

.locale-import-input:focus,
.locale-import-select:focus {
  outline: none;
  border-color: var(--color-primary-500);
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--color-primary-500) 18%, transparent);
}

.locale-import-options {
  padding: var(--spacing-4);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  background: var(--color-background);
}

.locale-import-checkbox {
  display: inline-flex;
  align-items: center;
  gap: var(--spacing-2);
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
}

.locale-import-checkbox input {
  width: 16px;
  height: 16px;
}

.locale-import-error {
  margin: 0;
  color: var(--color-error);
  font-size: var(--font-size-sm);
}

.locale-import-actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-2);
}

@media (max-width: 720px) {
  .locale-import-file-row {
    grid-template-columns: 1fr;
  }
}
</style>
