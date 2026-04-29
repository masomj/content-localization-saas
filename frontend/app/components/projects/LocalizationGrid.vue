<script setup lang="ts">
import UiButton from '~/components/ui/Button.vue'
import UiSelect from '~/components/ui/Select.vue'
import { translationClient } from '~/api/translationClient'
import { languagesClient } from '~/api/languagesClient'
import type { LocalizationGridRow, ProjectLanguage } from '~/api/types'

interface Props {
  projectId: string
  /** When set, only show grid rows whose itemId belongs to this folder. null = project root keys only, undefined = show all. */
  collectionId?: string | null
  /** Map of content-item id → collectionId, used to filter grid rows to current folder */
  itemCollectionMap?: Record<string, string | null>
}

const props = defineProps<Props>()
const emit = defineEmits<{
  'edit-cell': [payload: { itemId: string; itemKey: string; source: string; language: string }]
}>()

const allRows = ref<LocalizationGridRow[]>([])
const languages = ref<ProjectLanguage[]>([])
const serverTotal = ref(0)
const page = ref(1)
const pageSize = ref(20)
const isLoading = ref(false)
const statusFilter = ref('')
const searchQuery = ref('')

const PAGE_SIZE_OPTIONS = [
  { value: '10', label: '10 rows' },
  { value: '20', label: '20 rows' },
  { value: '50', label: '50 rows' },
  { value: '100', label: '100 rows' },
]

/** Rows filtered to current folder (client-side) */
const filteredRows = computed(() => {
  if (props.collectionId === undefined && !props.itemCollectionMap) {
    return allRows.value
  }
  if (!props.itemCollectionMap) return allRows.value
  return allRows.value.filter((row) => {
    const itemCollection = props.itemCollectionMap![row.itemId] ?? null
    return itemCollection === (props.collectionId ?? null)
  })
})

/** Paginated slice of filtered rows */
const total = computed(() => filteredRows.value.length)
const paginatedRows = computed(() => {
  const start = (page.value - 1) * pageSize.value
  return filteredRows.value.slice(start, start + pageSize.value)
})

const STATUS_OPTIONS = [
  { value: '', label: 'All statuses' },
  { value: 'missing', label: 'Missing' },
  { value: 'outdated', label: 'Outdated' },
  { value: 'review', label: 'Needs review' },
]

const activeTargetLanguages = computed(() =>
  languages.value.filter(l => !l.isSource && l.isActive),
)

const totalPages = computed(() => Math.max(1, Math.ceil(total.value / pageSize.value)))

function statusClass(status: string): string {
  switch (status) {
    case 'approved':
    case 'done':
      return 'status--done'
    case 'pending_review':
      return 'status--review'
    case 'outdated':
      return 'status--outdated'
    case 'todo':
      return 'status--todo'
    default:
      return 'status--untranslated'
  }
}

function statusLabel(status: string): string {
  switch (status) {
    case 'approved': return 'Approved'
    case 'done': return 'Done'
    case 'pending_review': return 'Review'
    case 'outdated': return 'Outdated'
    case 'todo': return 'Todo'
    default: return 'Untranslated'
  }
}

function getTarget(row: LocalizationGridRow, langCode: string) {
  return row.targets.find(t => t.language === langCode)
}

function completionPct(langCode: string): string {
  if (filteredRows.value.length === 0) return '0%'
  const done = filteredRows.value.filter(r => {
    const t = getTarget(r, langCode)
    return t && (t.status === 'approved' || t.status === 'done')
  }).length
  return `${Math.round((done / filteredRows.value.length) * 100)}%`
}

async function loadLanguages() {
  try {
    const data = await languagesClient.list(props.projectId)
    languages.value = Array.isArray(data) ? data : []
  } catch {
    languages.value = []
  }
}

async function loadGrid() {
  isLoading.value = true
  try {
    // Load all rows from server (large page) so we can filter client-side by folder
    const data = await translationClient.getGrid(props.projectId, {
      page: 1,
      pageSize: 10000,
      status: statusFilter.value || undefined,
      search: searchQuery.value || undefined,
    })
    allRows.value = data.rows ?? []
    serverTotal.value = data.total ?? 0
  } catch {
    allRows.value = []
    serverTotal.value = 0
  } finally {
    isLoading.value = false
  }
}

function goToPage(p: number) {
  if (p < 1 || p > totalPages.value) return
  page.value = p
}

function applyFilters() {
  page.value = 1
  loadGrid()
}

function handleCellClick(row: LocalizationGridRow, langCode: string) {
  emit('edit-cell', {
    itemId: row.itemId,
    itemKey: row.itemKey,
    source: row.source,
    language: langCode,
  })
}

watch(() => props.projectId, async (id) => {
  if (id) {
    await loadLanguages()
    await loadGrid()
  } else {
    allRows.value = []
    languages.value = []
  }
}, { immediate: true })

watch(() => props.collectionId, () => {
  page.value = 1
})

watch(pageSize, () => {
  page.value = 1
})

watch(total, (nextTotal) => {
  const maxPage = Math.max(1, Math.ceil(nextTotal / pageSize.value))
  if (page.value > maxPage) {
    page.value = maxPage
  }
})

defineExpose({ reload: async () => { await loadLanguages(); await loadGrid() } })
</script>

<template>
  <div class="loc-grid">
    <div class="loc-grid-toolbar">
      <div class="loc-grid-search">
        <label for="gridSearch" class="loc-field-label">Search keys</label>
        <span class="label-hint">Filter by content key name</span>
        <input
          id="gridSearch"
          v-model="searchQuery"
          type="text"
          class="loc-input"
          autocomplete="off"
          @keyup.enter="applyFilters"
        >
      </div>
      <div class="loc-grid-filter">
        <UiSelect
          id="gridStatusFilter"
          v-model="statusFilter"
          label="Status"
          :options="STATUS_OPTIONS"
          @update:model-value="applyFilters"
        />
        <span class="label-hint">Filter rows by translation status</span>
      </div>
    </div>

    <div v-if="!isLoading && total > 0" class="loc-grid-meta">
      <p class="loc-grid-summary">Showing {{ paginatedRows.length }} of {{ total }} keys</p>
      <div class="loc-grid-meta-controls">
        <UiSelect
          id="gridPageSize"
          v-model="pageSize"
          label="Rows per page"
          :options="PAGE_SIZE_OPTIONS"
        />
        <div v-if="totalPages > 1" class="loc-pagination loc-pagination--compact">
          <UiButton size="sm" variant="secondary" :disabled="page <= 1" @click="goToPage(page - 1)">
            Previous
          </UiButton>
          <span class="loc-page-info">Page {{ page }} of {{ totalPages }}</span>
          <UiButton size="sm" variant="secondary" :disabled="page >= totalPages" @click="goToPage(page + 1)">
            Next
          </UiButton>
        </div>
      </div>
    </div>

    <div v-if="isLoading" class="loc-grid-loading">Loading grid...</div>

    <div v-else-if="paginatedRows.length === 0" class="loc-grid-empty">
      No content keys found. Add content items first, then manage translations here.
    </div>

    <div v-else class="loc-grid-scroll">
      <table class="loc-table">
        <thead>
          <tr>
            <th class="loc-th loc-th--key">Key</th>
            <th class="loc-th loc-th--source">Source</th>
            <th
              v-for="lang in activeTargetLanguages"
              :key="lang.bcp47Code"
              class="loc-th loc-th--lang"
            >
              {{ lang.bcp47Code }}
              <span class="loc-th-pct">{{ completionPct(lang.bcp47Code) }}</span>
            </th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="row in paginatedRows" :key="row.itemId">
            <td class="loc-td loc-td--key" :title="row.itemKey">
              {{ row.itemKey }}
            </td>
            <td class="loc-td loc-td--source" :title="row.source">
              {{ row.source }}
            </td>
            <td
              v-for="lang in activeTargetLanguages"
              :key="lang.bcp47Code"
              class="loc-td loc-td--cell"
              :class="statusClass(getTarget(row, lang.bcp47Code)?.status ?? '')"
              tabindex="0"
              role="button"
              :aria-label="`Edit ${row.itemKey} - ${lang.bcp47Code}: ${statusLabel(getTarget(row, lang.bcp47Code)?.status ?? '')}`"
              @click="handleCellClick(row, lang.bcp47Code)"
              @keyup.enter="handleCellClick(row, lang.bcp47Code)"
            >
              <template v-if="getTarget(row, lang.bcp47Code)">
                <span class="loc-status-icon">
                  <template v-if="getTarget(row, lang.bcp47Code)?.status === 'approved' || getTarget(row, lang.bcp47Code)?.status === 'done'">&#10003;</template>
                  <template v-else-if="getTarget(row, lang.bcp47Code)?.status === 'pending_review'">&#9998;</template>
                  <template v-else-if="getTarget(row, lang.bcp47Code)?.status === 'outdated'">&#9888;</template>
                  <template v-else>&#8212;</template>
                </span>
                <span class="loc-status-text">{{ statusLabel(getTarget(row, lang.bcp47Code)?.status ?? '') }}</span>
              </template>
              <template v-else>
                <span class="loc-untranslated-hint">
                  <span class="loc-status-icon">+</span>
                  <span class="loc-status-text">Untranslated</span>
                </span>
              </template>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="totalPages > 1" class="loc-pagination">
      <UiButton size="sm" variant="secondary" :disabled="page <= 1" @click="goToPage(page - 1)">
        Previous
      </UiButton>
      <span class="loc-page-info">Page {{ page }} of {{ totalPages }} ({{ total }} keys)</span>
      <UiButton size="sm" variant="secondary" :disabled="page >= totalPages" @click="goToPage(page + 1)">
        Next
      </UiButton>
    </div>
  </div>
</template>

<style scoped>
.loc-grid { display: flex; flex-direction: column; gap: var(--spacing-4); }
.loc-grid-toolbar { display: flex; gap: var(--spacing-4); flex-wrap: wrap; }
.loc-grid-search { display: flex; flex-direction: column; gap: 2px; flex: 1; min-width: 200px; }
.loc-grid-filter { display: flex; flex-direction: column; gap: 2px; min-width: 180px; }
.loc-grid-meta {
  display: flex;
  align-items: end;
  justify-content: space-between;
  gap: var(--spacing-3);
  flex-wrap: wrap;
}
.loc-grid-meta-controls {
  display: flex;
  align-items: end;
  gap: var(--spacing-3);
  flex-wrap: wrap;
}
.loc-grid-summary {
  margin: 0;
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
}
.loc-field-label {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-secondary);
}
.label-hint { font-size: var(--font-size-xs); color: var(--color-text-muted); }
.loc-input {
  padding: var(--spacing-3) var(--spacing-4);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  background: var(--color-background);
  color: var(--color-text-primary);
  font-size: var(--font-size-base);
  margin-top: var(--spacing-1);
}
.loc-input:focus {
  outline: none;
  border-color: var(--color-primary-500);
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.15);
}
.loc-grid-loading, .loc-grid-empty {
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
  padding: var(--spacing-6) 0;
  text-align: center;
}
.loc-grid-scroll {
  overflow-x: auto;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
}
.loc-table {
  width: 100%;
  border-collapse: collapse;
  font-size: var(--font-size-sm);
}
.loc-th {
  background: var(--color-surface);
  border-bottom: 2px solid var(--color-border);
  padding: var(--spacing-3) var(--spacing-3);
  text-align: left;
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  white-space: nowrap;
  position: sticky;
  top: 0;
  z-index: 1;
}
.loc-th--key { min-width: 180px; }
.loc-th--source { min-width: 200px; }
.loc-th--lang { min-width: 120px; text-align: center; }
.loc-th-pct {
  display: block;
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-normal);
  color: var(--color-text-muted);
}
.loc-td {
  padding: var(--spacing-2) var(--spacing-3);
  border-bottom: 1px solid var(--color-border);
  vertical-align: middle;
}
.loc-td--key {
  font-family: monospace;
  font-weight: var(--font-weight-medium);
  max-width: 220px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.loc-td--source {
  max-width: 300px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  color: var(--color-text-muted);
}
.loc-td--cell {
  text-align: center;
  cursor: pointer;
  transition: background var(--transition-fast);
}
.loc-td--cell:hover { background: color-mix(in srgb, var(--color-primary-600) 8%, transparent); }
.loc-td--cell:focus-visible {
  outline: 2px solid var(--color-primary-500);
  outline-offset: -2px;
}
.loc-status-icon { margin-right: var(--spacing-1); }
.loc-status-text { font-size: var(--font-size-xs); }

.status--done { background: color-mix(in srgb, #22c55e 10%, transparent); color: #16a34a; }
.status--review { background: color-mix(in srgb, #eab308 10%, transparent); color: #a16207; }
.status--outdated { background: color-mix(in srgb, #f97316 10%, transparent); color: #c2410c; }
.status--todo { background: var(--color-surface); color: var(--color-text-muted); }
.status--untranslated { background: var(--color-surface); color: var(--color-text-muted); }

.loc-untranslated-hint {
  display: inline-flex;
  align-items: center;
  gap: var(--spacing-1);
  opacity: 0.6;
  transition: opacity var(--transition-fast);
}
.loc-td--cell:hover .loc-untranslated-hint {
  opacity: 1;
}

.loc-pagination {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--spacing-3);
}
.loc-pagination--compact {
  justify-content: flex-end;
}
.loc-page-info {
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
}

@media (max-width: 900px) {
  .loc-grid-meta {
    align-items: stretch;
  }
  .loc-grid-meta-controls {
    align-items: stretch;
  }
  .loc-pagination--compact {
    justify-content: flex-start;
  }
}
</style>
