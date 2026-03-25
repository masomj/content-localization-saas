<script setup lang="ts">
import UiButton from '~/components/ui/Button.vue'
import UiSelect from '~/components/ui/Select.vue'
import { translationClient } from '~/api/translationClient'
import { languagesClient } from '~/api/languagesClient'
import type { ProjectLanguage } from '~/api/types'

interface Props {
  projectId: string
}

const props = defineProps<Props>()
const emit = defineEmits<{ close: [] }>()

const languages = ref<ProjectLanguage[]>([])
const selectedLanguage = ref('__all__')
const selectedFormat = ref('json')
const isLoadingPreview = ref(false)
const isExporting = ref(false)
const previewData = ref<string>('')
const exportError = ref('')
const copiedUrl = ref(false)

const FORMAT_OPTIONS = [
  { value: 'json', label: 'JSON (i18next style)' },
  { value: 'flat', label: 'Flat JSON' },
  { value: 'nested', label: 'Nested JSON' },
]

const languageOptions = computed(() => {
  const opts = [{ value: '__all__', label: 'All languages' }]
  for (const lang of languages.value.filter(l => l.isActive)) {
    opts.push({ value: lang.bcp47Code, label: lang.bcp47Code })
  }
  return opts
})

const apiEndpointUrl = computed(() => {
  const origin = typeof window !== 'undefined' ? window.location.origin : ''
  if (selectedLanguage.value === '__all__') {
    return `${origin}/api/exports/neutral?projectId=${props.projectId}`
  }
  return `${origin}/api/integration/exports/bundle?projectId=${props.projectId}&language=${selectedLanguage.value}`
})

function transformToFlat(data: Record<string, any>, prefix = ''): Record<string, string> {
  const result: Record<string, string> = {}
  for (const [key, value] of Object.entries(data)) {
    const fullKey = prefix ? `${prefix}.${key}` : key
    if (typeof value === 'object' && value !== null && !Array.isArray(value)) {
      Object.assign(result, transformToFlat(value, fullKey))
    } else {
      result[fullKey] = String(value ?? '')
    }
  }
  return result
}

function transformToNested(data: Record<string, any>): Record<string, any> {
  // If data is already nested (has object values), return as-is
  const hasObjectValues = Object.values(data).some(v => typeof v === 'object' && v !== null)
  if (hasObjectValues) return data

  // Build nested structure from flat dot-notation keys
  const result: Record<string, any> = {}
  for (const [key, value] of Object.entries(data)) {
    const parts = key.split('.')
    let current = result
    for (let i = 0; i < parts.length - 1; i++) {
      if (!current[parts[i]!] || typeof current[parts[i]!] !== 'object') {
        current[parts[i]!] = {}
      }
      current = current[parts[i]!]
    }
    current[parts[parts.length - 1]!] = value
  }
  return result
}

function formatExportData(raw: any): string {
  if (!raw || typeof raw !== 'object') return '{}'

  let data = raw
  if (selectedFormat.value === 'flat') {
    // If exporting all languages, flatten each language object
    if (selectedLanguage.value === '__all__' && typeof data === 'object') {
      const flattened: Record<string, any> = {}
      for (const [lang, translations] of Object.entries(data)) {
        if (typeof translations === 'object' && translations !== null) {
          flattened[lang] = transformToFlat(translations as Record<string, any>)
        } else {
          flattened[lang] = translations
        }
      }
      data = flattened
    } else {
      data = transformToFlat(data)
    }
  } else if (selectedFormat.value === 'nested') {
    if (selectedLanguage.value === '__all__' && typeof data === 'object') {
      const nested: Record<string, any> = {}
      for (const [lang, translations] of Object.entries(data)) {
        if (typeof translations === 'object' && translations !== null) {
          nested[lang] = transformToNested(translations as Record<string, any>)
        } else {
          nested[lang] = translations
        }
      }
      data = nested
    } else {
      data = transformToNested(data)
    }
  }

  return JSON.stringify(data, null, 2)
}

async function loadPreview() {
  isLoadingPreview.value = true
  exportError.value = ''
  try {
    let raw: unknown
    if (selectedLanguage.value === '__all__') {
      raw = await translationClient.exportNeutral(props.projectId)
    } else {
      raw = await translationClient.exportBundle(props.projectId, selectedLanguage.value)
    }
    previewData.value = formatExportData(raw)
  } catch (error: any) {
    exportError.value = error?.message || 'Failed to load export preview'
    previewData.value = ''
  } finally {
    isLoadingPreview.value = false
  }
}

function downloadExport() {
  if (!previewData.value) return
  isExporting.value = true

  try {
    const blob = new Blob([previewData.value], { type: 'application/json' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')

    const langSuffix = selectedLanguage.value === '__all__' ? 'all' : selectedLanguage.value
    const formatSuffix = selectedFormat.value === 'json' ? '' : `-${selectedFormat.value}`
    a.href = url
    a.download = `translations-${langSuffix}${formatSuffix}.json`
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    URL.revokeObjectURL(url)
  } finally {
    isExporting.value = false
  }
}

async function copyEndpointUrl() {
  try {
    await navigator.clipboard.writeText(apiEndpointUrl.value)
    copiedUrl.value = true
    setTimeout(() => { copiedUrl.value = false }, 2000)
  } catch {
    // fallback
    const ta = document.createElement('textarea')
    ta.value = apiEndpointUrl.value
    document.body.appendChild(ta)
    ta.select()
    document.execCommand('copy')
    document.body.removeChild(ta)
    copiedUrl.value = true
    setTimeout(() => { copiedUrl.value = false }, 2000)
  }
}

const i18nextSnippet = computed(() => {
  const lang = selectedLanguage.value === '__all__' ? 'en' : selectedLanguage.value
  return `import i18next from 'i18next'
import translations from './locales/${lang}.json'

i18next.init({
  lng: '${lang}',
  resources: {
    ${lang}: { translation: translations }
  }
})`
})

const vueI18nSnippet = computed(() => {
  const lang = selectedLanguage.value === '__all__' ? 'en' : selectedLanguage.value
  return `import { createI18n } from 'vue-i18n'
import ${lang} from './locales/${lang}.json'

const i18n = createI18n({
  locale: '${lang}',
  messages: { ${lang} }
})`
})

const reactIntlSnippet = computed(() => {
  const lang = selectedLanguage.value === '__all__' ? 'en' : selectedLanguage.value
  return `import messages from './locales/${lang}.json'

// react-intl
<IntlProvider locale="${lang}" messages={messages}>
  <App />
</IntlProvider>

// next-intl
import { NextIntlClientProvider } from 'next-intl'
<NextIntlClientProvider locale="${lang}" messages={messages}>
  <App />
</NextIntlClientProvider>`
})

async function loadLanguages() {
  try {
    const data = await languagesClient.list(props.projectId)
    languages.value = Array.isArray(data) ? data : []
  } catch {
    languages.value = []
  }
}

watch([selectedLanguage, selectedFormat], () => {
  if (previewData.value) loadPreview()
})

onMounted(async () => {
  await loadLanguages()
  await loadPreview()
})
</script>

<template>
  <div class="ep-overlay" @click.self="$emit('close')">
    <aside class="ep-panel">
      <header class="ep-header">
        <div>
          <h2 class="ep-title">Export Translations</h2>
          <p class="ep-subtitle">Download or integrate your translations</p>
        </div>
        <button class="ep-close" aria-label="Close" @click="$emit('close')">&times;</button>
      </header>

      <div class="ep-body">
        <!-- Options -->
        <div class="ep-options">
          <div class="ep-field">
            <UiSelect
              id="epFormat"
              v-model="selectedFormat"
              label="Format"
              :options="FORMAT_OPTIONS"
            />
            <span class="ep-hint">Choose the JSON structure for your export</span>
          </div>

          <div class="ep-field">
            <UiSelect
              id="epLanguage"
              v-model="selectedLanguage"
              label="Language"
              :options="languageOptions"
            />
            <span class="ep-hint">Export all languages or a specific one</span>
          </div>
        </div>

        <!-- Preview -->
        <div class="ep-section">
          <h3 class="ep-section-title">Preview</h3>
          <div v-if="isLoadingPreview" class="ep-preview-loading">Loading preview...</div>
          <p v-else-if="exportError" class="ep-error">{{ exportError }}</p>
          <pre v-else class="ep-preview"><code>{{ previewData }}</code></pre>
        </div>

        <!-- Actions -->
        <div class="ep-actions">
          <UiButton
            :disabled="isExporting || !previewData"
            @click="downloadExport"
          >
            {{ isExporting ? 'Downloading...' : 'Download JSON' }}
          </UiButton>
          <UiButton variant="secondary" @click="loadPreview">
            Refresh preview
          </UiButton>
        </div>

        <!-- API Endpoint for CI/CD -->
        <div class="ep-section">
          <h3 class="ep-section-title">API Endpoint</h3>
          <span class="ep-hint">Use this URL in your CI/CD pipeline to fetch translations at build time</span>
          <div class="ep-endpoint">
            <code class="ep-endpoint-url">{{ apiEndpointUrl }}</code>
            <UiButton size="sm" variant="ghost" @click="copyEndpointUrl">
              {{ copiedUrl ? 'Copied' : 'Copy' }}
            </UiButton>
          </div>
        </div>

        <!-- Framework Snippets -->
        <div class="ep-section">
          <h3 class="ep-section-title">Framework Integration</h3>
          <span class="ep-hint">Copy-paste snippets to integrate translations into your app</span>

          <div class="ep-snippet">
            <h4 class="ep-snippet-title">i18next</h4>
            <pre class="ep-snippet-code"><code>{{ i18nextSnippet }}</code></pre>
          </div>

          <div class="ep-snippet">
            <h4 class="ep-snippet-title">vue-i18n</h4>
            <pre class="ep-snippet-code"><code>{{ vueI18nSnippet }}</code></pre>
          </div>

          <div class="ep-snippet">
            <h4 class="ep-snippet-title">react-intl / next-intl</h4>
            <pre class="ep-snippet-code"><code>{{ reactIntlSnippet }}</code></pre>
          </div>
        </div>
      </div>
    </aside>
  </div>
</template>

<style scoped>
.ep-overlay {
  position: fixed;
  inset: 0;
  background: color-mix(in srgb, var(--color-black) 45%, transparent);
  display: flex;
  justify-content: flex-end;
  z-index: var(--z-modal);
}

.ep-panel {
  width: min(620px, 92vw);
  height: 100vh;
  background: var(--color-surface);
  border-left: 1px solid var(--color-border);
  display: flex;
  flex-direction: column;
  overflow-y: auto;
}

.ep-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  padding: var(--spacing-6);
  border-bottom: 1px solid var(--color-border);
  flex-shrink: 0;
}

.ep-title {
  margin: 0;
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
}

.ep-subtitle {
  margin: var(--spacing-1) 0 0;
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
}

.ep-close {
  background: none;
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  color: var(--color-text-muted);
  padding: var(--spacing-1) var(--spacing-2);
  border-radius: var(--radius-md);
  transition: background var(--transition-fast);
}
.ep-close:hover {
  background: var(--color-border);
  color: var(--color-text-primary);
}

.ep-body {
  padding: var(--spacing-6);
  display: flex;
  flex-direction: column;
  gap: var(--spacing-5);
  flex: 1;
}

.ep-options {
  display: flex;
  gap: var(--spacing-4);
  flex-wrap: wrap;
}

.ep-field {
  display: flex;
  flex-direction: column;
  gap: 2px;
  flex: 1;
  min-width: 180px;
}

.ep-hint {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

.ep-section {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-2);
}

.ep-section-title {
  margin: 0;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
}

.ep-preview-loading {
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
  padding: var(--spacing-4);
  text-align: center;
}

.ep-error {
  margin: 0;
  color: var(--color-error);
  font-size: var(--font-size-xs);
}

.ep-preview {
  margin: 0;
  background: var(--color-background);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-4);
  font-size: var(--font-size-xs);
  font-family: monospace;
  max-height: 280px;
  overflow: auto;
  white-space: pre-wrap;
  word-break: break-all;
  color: var(--color-text-primary);
}

.ep-actions {
  display: flex;
  gap: var(--spacing-2);
}

.ep-endpoint {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
  background: var(--color-background);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-2) var(--spacing-3);
  margin-top: var(--spacing-1);
}

.ep-endpoint-url {
  flex: 1;
  font-size: var(--font-size-xs);
  font-family: monospace;
  color: var(--color-text-primary);
  word-break: break-all;
}

.ep-snippet {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-1);
  margin-top: var(--spacing-2);
}

.ep-snippet-title {
  margin: 0;
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-secondary);
}

.ep-snippet-code {
  margin: 0;
  background: var(--color-background);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-3);
  font-size: var(--font-size-xs);
  font-family: monospace;
  overflow-x: auto;
  white-space: pre;
  color: var(--color-text-primary);
}
</style>
