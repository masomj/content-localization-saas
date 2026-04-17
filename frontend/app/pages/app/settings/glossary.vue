<script setup lang="ts">
import UiButton from '~/components/ui/Button.vue'
import UiInput from '~/components/ui/Input.vue'
import UiCard from '~/components/ui/Card.vue'
import { glossaryClient } from '~/api/glossaryClient'
import type { GlossaryDto, GlossaryTermDto, GlossaryTermTranslationDto } from '~/api/glossaryClient'

definePageMeta({
  layout: 'app',
  middleware: ['admin'],
})

useSeoMeta({ title: 'Glossary - InterCopy' })

const auth = useAuth()

// Glossary list state
const glossaries = ref<GlossaryDto[]>([])
const isLoadingGlossaries = ref(false)
const showGlossaryModal = ref(false)
const editingGlossary = ref<GlossaryDto | null>(null)
const glossaryForm = reactive({ name: '', description: '' })
const glossaryError = ref('')

// Term list state
const selectedGlossary = ref<GlossaryDto | null>(null)
const terms = ref<GlossaryTermDto[]>([])
const termTotal = ref(0)
const termPage = ref(1)
const termPageSize = 50
const termSearch = ref('')
const isLoadingTerms = ref(false)

// Term modal state
const showTermModal = ref(false)
const editingTerm = ref<GlossaryTermDto | null>(null)
const termForm = reactive({
  sourceTerm: '',
  definition: '',
  caseSensitive: false,
  isForbidden: false,
  forbiddenReplacement: '',
  translations: [] as { languageCode: string; translatedTerm: string }[],
})
const termError = ref('')

// Delete confirmation state
const showDeleteConfirm = ref(false)
const deleteTarget = ref<{ type: 'glossary' | 'term'; id: string; name: string } | null>(null)

// Feedback
const feedback = ref<{ type: 'success' | 'error'; message: string } | null>(null)

function showFeedback(type: 'success' | 'error', message: string) {
  feedback.value = { type, message }
  setTimeout(() => { feedback.value = null }, 3000)
}

// File import refs
const csvImportRef = ref<HTMLInputElement | null>(null)
const tbxImportRef = ref<HTMLInputElement | null>(null)

// Glossary CRUD
async function fetchGlossaries() {
  isLoadingGlossaries.value = true
  try {
    glossaries.value = await glossaryClient.list()
  } catch (e: any) {
    showFeedback('error', e?.message || 'Failed to load glossaries')
  } finally {
    isLoadingGlossaries.value = false
  }
}

function openCreateGlossary() {
  editingGlossary.value = null
  glossaryForm.name = ''
  glossaryForm.description = ''
  glossaryError.value = ''
  showGlossaryModal.value = true
}

function openEditGlossary(g: GlossaryDto) {
  editingGlossary.value = g
  glossaryForm.name = g.name
  glossaryForm.description = g.description
  glossaryError.value = ''
  showGlossaryModal.value = true
}

async function saveGlossary() {
  glossaryError.value = ''
  if (!glossaryForm.name.trim()) {
    glossaryError.value = 'Name is required.'
    return
  }
  try {
    if (editingGlossary.value) {
      await glossaryClient.update(editingGlossary.value.id, glossaryForm.name, glossaryForm.description)
    } else {
      await glossaryClient.create(glossaryForm.name, glossaryForm.description)
    }
    showGlossaryModal.value = false
    await fetchGlossaries()
    showFeedback('success', editingGlossary.value ? 'Glossary updated' : 'Glossary created')
  } catch (e: any) {
    glossaryError.value = e?.message || 'Failed to save glossary'
  }
}

function confirmDeleteGlossary(g: GlossaryDto) {
  deleteTarget.value = { type: 'glossary', id: g.id, name: g.name }
  showDeleteConfirm.value = true
}

async function executeDelete() {
  if (!deleteTarget.value) return
  try {
    if (deleteTarget.value.type === 'glossary') {
      await glossaryClient.delete(deleteTarget.value.id)
      if (selectedGlossary.value?.id === deleteTarget.value.id) {
        selectedGlossary.value = null
        terms.value = []
      }
      await fetchGlossaries()
      showFeedback('success', 'Glossary deleted')
    } else {
      if (selectedGlossary.value) {
        await glossaryClient.deleteTerm(selectedGlossary.value.id, deleteTarget.value.id)
        await fetchTerms()
        showFeedback('success', 'Term deleted')
      }
    }
  } catch (e: any) {
    showFeedback('error', e?.message || 'Failed to delete')
  } finally {
    showDeleteConfirm.value = false
    deleteTarget.value = null
  }
}

// Term CRUD
async function selectGlossary(g: GlossaryDto) {
  selectedGlossary.value = g
  termPage.value = 1
  termSearch.value = ''
  await fetchTerms()
}

async function fetchTerms() {
  if (!selectedGlossary.value) return
  isLoadingTerms.value = true
  try {
    const result = await glossaryClient.listTerms(selectedGlossary.value.id, termPage.value, termPageSize, termSearch.value)
    terms.value = result.items
    termTotal.value = result.total
  } catch (e: any) {
    showFeedback('error', e?.message || 'Failed to load terms')
  } finally {
    isLoadingTerms.value = false
  }
}

function openCreateTerm() {
  editingTerm.value = null
  termForm.sourceTerm = ''
  termForm.definition = ''
  termForm.caseSensitive = false
  termForm.isForbidden = false
  termForm.forbiddenReplacement = ''
  termForm.translations = []
  termError.value = ''
  showTermModal.value = true
}

function openEditTerm(t: GlossaryTermDto) {
  editingTerm.value = t
  termForm.sourceTerm = t.sourceTerm
  termForm.definition = t.definition
  termForm.caseSensitive = t.caseSensitive
  termForm.isForbidden = t.isForbidden
  termForm.forbiddenReplacement = t.forbiddenReplacement
  termForm.translations = (t.translations || []).map(tr => ({ languageCode: tr.languageCode, translatedTerm: tr.translatedTerm }))
  termError.value = ''
  showTermModal.value = true
}

async function saveTerm() {
  termError.value = ''
  if (!termForm.sourceTerm.trim()) {
    termError.value = 'Source term is required.'
    return
  }
  if (!selectedGlossary.value) return
  try {
    const payload = {
      sourceTerm: termForm.sourceTerm,
      definition: termForm.definition,
      caseSensitive: termForm.caseSensitive,
      isForbidden: termForm.isForbidden,
      forbiddenReplacement: termForm.forbiddenReplacement,
      translations: termForm.translations.filter(t => t.translatedTerm.trim()),
    }
    if (editingTerm.value) {
      await glossaryClient.updateTerm(selectedGlossary.value.id, editingTerm.value.id, payload)
    } else {
      await glossaryClient.createTerm(selectedGlossary.value.id, payload)
    }
    showTermModal.value = false
    await fetchTerms()
    showFeedback('success', editingTerm.value ? 'Term updated' : 'Term created')
  } catch (e: any) {
    termError.value = e?.message || 'Failed to save term'
  }
}

function confirmDeleteTerm(t: GlossaryTermDto) {
  deleteTarget.value = { type: 'term', id: t.id, name: t.sourceTerm }
  showDeleteConfirm.value = true
}

function addTranslation() {
  termForm.translations.push({ languageCode: '', translatedTerm: '' })
}

function removeTranslation(index: number) {
  termForm.translations.splice(index, 1)
}

// Forbidden filter
const termFilter = ref<'all' | 'forbidden'>('all')
const filteredTerms = computed(() => {
  if (termFilter.value === 'forbidden') {
    return terms.value.filter(t => t.isForbidden)
  }
  return terms.value
})

function setTermFilter(filter: 'all' | 'forbidden') {
  termFilter.value = filter
}

// Search debounce
let searchTimeout: ReturnType<typeof setTimeout> | null = null
function onSearchInput() {
  if (searchTimeout) clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => {
    termPage.value = 1
    fetchTerms()
  }, 300)
}

// Pagination
const totalPages = computed(() => Math.ceil(termTotal.value / termPageSize))

function prevPage() {
  if (termPage.value > 1) {
    termPage.value--
    fetchTerms()
  }
}

function nextPage() {
  if (termPage.value < totalPages.value) {
    termPage.value++
    fetchTerms()
  }
}

// Import / Export
async function handleCsvImport(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0]
  if (!file || !selectedGlossary.value) return
  try {
    const result = await glossaryClient.importCsv(selectedGlossary.value.id, file)
    showFeedback('success', `Imported ${result.imported} terms from CSV`)
    await fetchTerms()
  } catch (err: any) {
    showFeedback('error', err?.message || 'CSV import failed')
  }
  if (csvImportRef.value) csvImportRef.value.value = ''
}

async function handleTbxImport(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0]
  if (!file || !selectedGlossary.value) return
  try {
    const result = await glossaryClient.importTbx(selectedGlossary.value.id, file)
    showFeedback('success', `Imported ${result.imported} terms from TBX`)
    await fetchTerms()
  } catch (err: any) {
    showFeedback('error', err?.message || 'TBX import failed')
  }
  if (tbxImportRef.value) tbxImportRef.value.value = ''
}

async function exportCsv() {
  if (!selectedGlossary.value) return
  try {
    const blob = await glossaryClient.exportCsv(selectedGlossary.value.id)
    downloadBlob(blob, 'glossary.csv')
  } catch (err: any) {
    showFeedback('error', err?.message || 'CSV export failed')
  }
}

async function exportTbx() {
  if (!selectedGlossary.value) return
  try {
    const blob = await glossaryClient.exportTbx(selectedGlossary.value.id)
    downloadBlob(blob, 'glossary.tbx')
  } catch (err: any) {
    showFeedback('error', err?.message || 'TBX export failed')
  }
}

function downloadBlob(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob instanceof Blob ? blob : new Blob([JSON.stringify(blob)]))
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  a.click()
  URL.revokeObjectURL(url)
}

onMounted(() => {
  fetchGlossaries()
})
</script>

<template>
  <div class="glossary-page">
    <header class="glossary-page__header">
      <div>
        <h1>Glossary</h1>
        <p class="glossary-page__subtitle">Manage termbases for consistent translations across your workspace</p>
      </div>
      <UiButton @click="openCreateGlossary">Create Glossary</UiButton>
    </header>

    <div v-if="feedback" :class="['gp-feedback', `gp-feedback--${feedback.type}`]">
      {{ feedback.message }}
    </div>

    <div class="glossary-page__layout">
      <!-- Glossary list sidebar -->
      <div class="glossary-page__sidebar">
        <div v-if="isLoadingGlossaries" class="gp-loading">Loading glossaries...</div>
        <div v-else-if="glossaries.length === 0" class="gp-empty">No glossaries yet. Create one to get started.</div>
        <div
          v-for="g in glossaries"
          :key="g.id"
          class="gp-glossary-item"
          :class="{ 'gp-glossary-item--active': selectedGlossary?.id === g.id }"
          @click="selectGlossary(g)"
        >
          <div class="gp-glossary-item__info">
            <span class="gp-glossary-item__name">{{ g.name }}</span>
            <span v-if="g.description" class="gp-glossary-item__desc">{{ g.description }}</span>
          </div>
          <div class="gp-glossary-item__actions">
            <button class="gp-icon-btn" title="Edit" @click.stop="openEditGlossary(g)">
              <svg viewBox="0 0 20 20" fill="currentColor" width="16" height="16"><path d="M13.586 3.586a2 2 0 112.828 2.828l-.793.793-2.828-2.828.793-.793zM11.379 5.793L3 14.172V17h2.828l8.38-8.379-2.83-2.828z" /></svg>
            </button>
            <button class="gp-icon-btn gp-icon-btn--danger" title="Delete" @click.stop="confirmDeleteGlossary(g)">
              <svg viewBox="0 0 20 20" fill="currentColor" width="16" height="16"><path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd" /></svg>
            </button>
          </div>
        </div>
      </div>

      <!-- Term list -->
      <div class="glossary-page__content">
        <template v-if="selectedGlossary">
          <div class="gp-terms-header">
            <h2>{{ selectedGlossary.name }}</h2>
            <div class="gp-terms-toolbar">
              <UiInput
                v-model="termSearch"
                label=""
                placeholder="Search terms..."
                class="gp-search-input"
                @input="onSearchInput"
              />
              <UiButton size="sm" @click="openCreateTerm">Add Term</UiButton>
              <UiButton size="sm" variant="secondary" @click="csvImportRef?.click()">Import CSV</UiButton>
              <UiButton size="sm" variant="secondary" @click="tbxImportRef?.click()">Import TBX</UiButton>
              <UiButton size="sm" variant="secondary" @click="exportCsv">Export CSV</UiButton>
              <UiButton size="sm" variant="secondary" @click="exportTbx">Export TBX</UiButton>
            </div>
            <input ref="csvImportRef" type="file" accept=".csv" hidden @change="handleCsvImport" />
            <input ref="tbxImportRef" type="file" accept=".tbx,.xml" hidden @change="handleTbxImport" />

            <div class="gp-filter-tabs">
              <button
                class="gp-filter-tab"
                :class="{ 'gp-filter-tab--active': termFilter === 'all' }"
                @click="setTermFilter('all')"
              >All Terms</button>
              <button
                class="gp-filter-tab"
                :class="{ 'gp-filter-tab--active': termFilter === 'forbidden' }"
                @click="setTermFilter('forbidden')"
              >Forbidden Only</button>
            </div>
          </div>

          <div v-if="isLoadingTerms" class="gp-loading">Loading terms...</div>
          <div v-else-if="filteredTerms.length === 0" class="gp-empty">No terms found.</div>
          <table v-else class="gp-terms-table">
            <thead>
              <tr>
                <th>Source Term</th>
                <th>Definition</th>
                <th>Translations</th>
                <th>Flags</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="t in filteredTerms" :key="t.id">
                <td class="gp-terms-table__source">{{ t.sourceTerm }}</td>
                <td class="gp-terms-table__def">{{ t.definition || '—' }}</td>
                <td class="gp-terms-table__trans">
                  <span v-for="tr in t.translations" :key="tr.id" class="gp-lang-badge">
                    {{ tr.languageCode }}: {{ tr.translatedTerm }}
                  </span>
                  <span v-if="!t.translations?.length" class="gp-muted">None</span>
                </td>
                <td>
                  <span v-if="t.caseSensitive" class="gp-flag">Case</span>
                  <span v-if="t.isForbidden" class="gp-flag gp-flag--forbidden">Forbidden</span>
                </td>
                <td class="gp-terms-table__actions">
                  <button class="gp-icon-btn" title="Edit" @click="openEditTerm(t)">
                    <svg viewBox="0 0 20 20" fill="currentColor" width="16" height="16"><path d="M13.586 3.586a2 2 0 112.828 2.828l-.793.793-2.828-2.828.793-.793zM11.379 5.793L3 14.172V17h2.828l8.38-8.379-2.83-2.828z" /></svg>
                  </button>
                  <button class="gp-icon-btn gp-icon-btn--danger" title="Delete" @click="confirmDeleteTerm(t)">
                    <svg viewBox="0 0 20 20" fill="currentColor" width="16" height="16"><path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd" /></svg>
                  </button>
                </td>
              </tr>
            </tbody>
          </table>

          <!-- Pagination -->
          <div v-if="totalPages > 1" class="gp-pagination">
            <UiButton size="sm" variant="secondary" :disabled="termPage <= 1" @click="prevPage">Previous</UiButton>
            <span class="gp-pagination__info">Page {{ termPage }} of {{ totalPages }} ({{ termTotal }} terms)</span>
            <UiButton size="sm" variant="secondary" :disabled="termPage >= totalPages" @click="nextPage">Next</UiButton>
          </div>
        </template>
        <div v-else class="gp-empty">Select a glossary from the sidebar to view terms.</div>
      </div>
    </div>

    <!-- Glossary Create/Edit Modal -->
    <div v-if="showGlossaryModal" class="gp-modal-overlay" @click.self="showGlossaryModal = false">
      <div class="gp-modal">
        <h3>{{ editingGlossary ? 'Edit Glossary' : 'Create Glossary' }}</h3>
        <div class="gp-modal__field">
          <UiInput v-model="glossaryForm.name" label="Name" hint="A short name for this glossary" />
        </div>
        <div class="gp-modal__field">
          <UiInput v-model="glossaryForm.description" label="Description" hint="Optional description of the glossary purpose" />
        </div>
        <p v-if="glossaryError" class="gp-error">{{ glossaryError }}</p>
        <div class="gp-modal__actions">
          <UiButton variant="secondary" @click="showGlossaryModal = false">Cancel</UiButton>
          <UiButton @click="saveGlossary">{{ editingGlossary ? 'Update' : 'Create' }}</UiButton>
        </div>
      </div>
    </div>

    <!-- Term Create/Edit Modal -->
    <div v-if="showTermModal" class="gp-modal-overlay" @click.self="showTermModal = false">
      <div class="gp-modal gp-modal--wide">
        <h3>{{ editingTerm ? 'Edit Term' : 'Add Term' }}</h3>
        <div class="gp-modal__field">
          <UiInput v-model="termForm.sourceTerm" label="Source Term" hint="The term in the source language" />
        </div>
        <div class="gp-modal__field">
          <UiInput v-model="termForm.definition" label="Definition" hint="Describe when and how this term should be used" />
        </div>
        <div class="gp-modal__row">
          <label class="gp-toggle">
            <input v-model="termForm.caseSensitive" type="checkbox" />
            <span>Case sensitive</span>
          </label>
          <label class="gp-toggle">
            <input v-model="termForm.isForbidden" type="checkbox" />
            <span>Forbidden term</span>
          </label>
        </div>
        <div v-if="termForm.isForbidden" class="gp-modal__field">
          <UiInput v-model="termForm.forbiddenReplacement" label="Replacement" hint="Suggest an alternative for this forbidden term" />
        </div>

        <div class="gp-translations-section">
          <div class="gp-translations-header">
            <strong>Translations</strong>
            <UiButton size="sm" variant="secondary" @click="addTranslation">Add Translation</UiButton>
          </div>
          <div v-for="(tr, i) in termForm.translations" :key="i" class="gp-translation-row">
            <UiInput v-model="tr.languageCode" label="Language Code" hint="e.g. fr, de, es" class="gp-translation-lang" />
            <UiInput v-model="tr.translatedTerm" label="Translated Term" hint="Translation in this language" class="gp-translation-term" />
            <button class="gp-icon-btn gp-icon-btn--danger gp-translation-remove" title="Remove" @click="removeTranslation(i)">
              <svg viewBox="0 0 20 20" fill="currentColor" width="16" height="16"><path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd" /></svg>
            </button>
          </div>
          <div v-if="termForm.translations.length === 0" class="gp-muted">No translations added yet.</div>
        </div>

        <p v-if="termError" class="gp-error">{{ termError }}</p>
        <div class="gp-modal__actions">
          <UiButton variant="secondary" @click="showTermModal = false">Cancel</UiButton>
          <UiButton @click="saveTerm">{{ editingTerm ? 'Update' : 'Create' }}</UiButton>
        </div>
      </div>
    </div>

    <!-- Delete Confirmation Modal -->
    <div v-if="showDeleteConfirm" class="gp-modal-overlay" @click.self="showDeleteConfirm = false">
      <div class="gp-modal">
        <h3>Confirm Delete</h3>
        <p>Are you sure you want to delete <strong>{{ deleteTarget?.name }}</strong>? This action cannot be undone.</p>
        <div class="gp-modal__actions">
          <UiButton variant="secondary" @click="showDeleteConfirm = false">Cancel</UiButton>
          <UiButton variant="danger" @click="executeDelete">Delete</UiButton>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.glossary-page {
  max-width: 1200px;
}

.glossary-page__header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: var(--spacing-6);
}

.glossary-page__header h1 {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-semibold);
  color: var(--color-gray-900);
  margin: 0 0 var(--spacing-1) 0;
}

.glossary-page__subtitle {
  color: var(--color-gray-500);
  margin: 0;
}

.glossary-page__layout {
  display: flex;
  gap: var(--spacing-6);
  min-height: 500px;
}

.glossary-page__sidebar {
  width: 280px;
  flex-shrink: 0;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  background: var(--color-surface);
  overflow-y: auto;
  max-height: 70vh;
}

.glossary-page__content {
  flex: 1;
  min-width: 0;
}

.gp-glossary-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--spacing-3) var(--spacing-4);
  border-bottom: 1px solid var(--color-border);
  cursor: pointer;
  transition: background var(--transition-fast);
}

.gp-glossary-item:hover {
  background: color-mix(in srgb, var(--color-primary-500) 6%, var(--color-surface));
}

.gp-glossary-item--active {
  background: color-mix(in srgb, var(--color-primary-500) 12%, var(--color-surface));
  border-left: 3px solid var(--color-primary-500);
}

.gp-glossary-item__info {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.gp-glossary-item__name {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.gp-glossary-item__desc {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.gp-glossary-item__actions {
  display: flex;
  gap: var(--spacing-1);
  flex-shrink: 0;
}

.gp-icon-btn {
  background: none;
  border: none;
  cursor: pointer;
  padding: var(--spacing-1);
  border-radius: var(--radius-md);
  color: var(--color-text-muted);
  display: flex;
  align-items: center;
  transition: background var(--transition-fast), color var(--transition-fast);
}

.gp-icon-btn:hover {
  background: var(--color-border);
  color: var(--color-text-primary);
}

.gp-icon-btn--danger:hover {
  background: color-mix(in srgb, var(--color-error) 14%, var(--color-surface));
  color: var(--color-error);
}

.gp-terms-header {
  margin-bottom: var(--spacing-4);
}

.gp-terms-header h2 {
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  margin: 0 0 var(--spacing-3) 0;
}

.gp-terms-toolbar {
  display: flex;
  gap: var(--spacing-2);
  align-items: flex-end;
  flex-wrap: wrap;
}

.gp-search-input {
  min-width: 200px;
}

.gp-terms-table {
  width: 100%;
  border-collapse: collapse;
  font-size: var(--font-size-sm);
}

.gp-terms-table th {
  text-align: left;
  padding: var(--spacing-2) var(--spacing-3);
  border-bottom: 2px solid var(--color-border);
  color: var(--color-text-secondary);
  font-weight: var(--font-weight-medium);
  font-size: var(--font-size-xs);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.gp-terms-table td {
  padding: var(--spacing-2) var(--spacing-3);
  border-bottom: 1px solid var(--color-border);
  color: var(--color-text-primary);
  vertical-align: top;
}

.gp-terms-table__source {
  font-weight: var(--font-weight-medium);
}

.gp-terms-table__def {
  max-width: 200px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.gp-terms-table__trans {
  display: flex;
  flex-wrap: wrap;
  gap: var(--spacing-1);
}

.gp-terms-table__actions {
  display: flex;
  gap: var(--spacing-1);
  white-space: nowrap;
}

.gp-lang-badge {
  display: inline-block;
  padding: 1px var(--spacing-2);
  border-radius: var(--radius-full);
  background: color-mix(in srgb, var(--color-primary-500) 10%, var(--color-surface));
  border: 1px solid color-mix(in srgb, var(--color-primary-500) 20%, var(--color-border));
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}

.gp-flag {
  display: inline-block;
  padding: 1px var(--spacing-2);
  border-radius: var(--radius-full);
  background: color-mix(in srgb, var(--color-info) 10%, var(--color-surface));
  font-size: var(--font-size-xs);
  color: var(--color-info);
}

.gp-flag--forbidden {
  background: color-mix(in srgb, var(--color-error) 10%, var(--color-surface));
  color: var(--color-error);
}

.gp-pagination {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--spacing-3);
  margin-top: var(--spacing-4);
}

.gp-pagination__info {
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
}

/* Modals */
.gp-modal-overlay {
  position: fixed;
  inset: 0;
  background: color-mix(in srgb, var(--color-black) 45%, transparent);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: var(--z-modal);
}

.gp-modal {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-xl);
  padding: var(--spacing-6);
  min-width: 400px;
  max-width: 500px;
  box-shadow: var(--shadow-xl);
}

.gp-modal--wide {
  min-width: 560px;
  max-width: 640px;
  max-height: 80vh;
  overflow-y: auto;
}

.gp-modal h3 {
  margin: 0 0 var(--spacing-4) 0;
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
}

.gp-modal__field {
  margin-bottom: var(--spacing-3);
}

.gp-modal__row {
  display: flex;
  gap: var(--spacing-4);
  margin-bottom: var(--spacing-3);
}

.gp-modal__actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-2);
  margin-top: var(--spacing-4);
}

.gp-toggle {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  cursor: pointer;
}

.gp-toggle input {
  accent-color: var(--color-primary-500);
}

.gp-translations-section {
  margin-top: var(--spacing-4);
  padding-top: var(--spacing-4);
  border-top: 1px solid var(--color-border);
}

.gp-translations-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-3);
}

.gp-translation-row {
  display: flex;
  gap: var(--spacing-2);
  align-items: flex-end;
  margin-bottom: var(--spacing-2);
}

.gp-translation-lang {
  width: 120px;
  flex-shrink: 0;
}

.gp-translation-term {
  flex: 1;
}

.gp-translation-remove {
  margin-bottom: var(--spacing-1);
}

.gp-loading {
  padding: var(--spacing-6);
  text-align: center;
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.gp-empty {
  padding: var(--spacing-6);
  text-align: center;
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.gp-muted {
  color: var(--color-text-muted);
  font-size: var(--font-size-xs);
}

.gp-error {
  color: var(--color-error);
  font-size: var(--font-size-sm);
  margin: var(--spacing-2) 0 0;
}

.gp-feedback {
  padding: var(--spacing-3) var(--spacing-4);
  border-radius: var(--radius-lg);
  margin-bottom: var(--spacing-4);
  font-size: var(--font-size-sm);
}

.gp-feedback--success {
  background: color-mix(in srgb, var(--color-success) 12%, var(--color-surface));
  color: var(--color-text-primary);
  border: 1px solid color-mix(in srgb, var(--color-success) 40%, var(--color-border));
}

.gp-feedback--error {
  background: color-mix(in srgb, var(--color-error) 14%, var(--color-surface));
  color: var(--color-text-primary);
  border: 1px solid color-mix(in srgb, var(--color-error) 45%, var(--color-border));
}

.gp-filter-tabs {
  display: flex;
  gap: var(--spacing-1);
  margin-top: var(--spacing-3);
}

.gp-filter-tab {
  padding: var(--spacing-2) var(--spacing-4);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-surface);
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.gp-filter-tab:hover {
  background: color-mix(in srgb, var(--color-primary-500) 6%, var(--color-surface));
}

.gp-filter-tab--active {
  background: var(--color-primary-50);
  color: var(--color-primary-700);
  border-color: var(--color-primary-500);
}
</style>
