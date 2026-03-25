<script setup lang="ts">
import UiButton from '~/components/ui/Button.vue'
import UiSelect from '~/components/ui/Select.vue'
import { languagesClient } from '~/api/languagesClient'
import type { ProjectLanguage } from '~/api/types'

interface Props {
  projectId: string
}

const props = defineProps<Props>()
const emit = defineEmits<{ updated: [] }>()

const languages = ref<ProjectLanguage[]>([])
const isLoading = ref(false)
const showAddForm = ref(false)
const addError = ref('')

const newLanguageCode = ref('')
const presetCode = ref('')

const LANGUAGE_NAMES: Record<string, string> = {
  en: 'English', fr: 'French', de: 'German', es: 'Spanish',
  ja: 'Japanese', zh: 'Chinese', ko: 'Korean', pt: 'Portuguese',
  it: 'Italian', ar: 'Arabic', nl: 'Dutch', ru: 'Russian',
  pl: 'Polish', sv: 'Swedish', da: 'Danish', fi: 'Finnish',
  nb: 'Norwegian', tr: 'Turkish', th: 'Thai', vi: 'Vietnamese',
  hi: 'Hindi', he: 'Hebrew', cs: 'Czech', ro: 'Romanian',
  uk: 'Ukrainian', id: 'Indonesian', ms: 'Malay', hu: 'Hungarian',
}

const PRESETS = [
  { value: '', label: 'Choose a preset...' },
  { value: 'en', label: 'English (en)' },
  { value: 'fr', label: 'French (fr)' },
  { value: 'de', label: 'German (de)' },
  { value: 'es', label: 'Spanish (es)' },
  { value: 'ja', label: 'Japanese (ja)' },
  { value: 'zh', label: 'Chinese (zh)' },
  { value: 'ko', label: 'Korean (ko)' },
  { value: 'pt', label: 'Portuguese (pt)' },
  { value: 'it', label: 'Italian (it)' },
  { value: 'ar', label: 'Arabic (ar)' },
  { value: 'nl', label: 'Dutch (nl)' },
  { value: 'ru', label: 'Russian (ru)' },
]

function languageName(code: string): string {
  const base = code.split('-')[0]?.toLowerCase() ?? ''
  return LANGUAGE_NAMES[base] ?? code
}

function flagEmoji(code: string): string {
  const parts = code.split('-')
  const region = parts.length > 1 ? parts[1]!.toUpperCase() : parts[0]!.toUpperCase()
  const REGION_MAP: Record<string, string> = {
    EN: 'GB', JA: 'JP', KO: 'KR', ZH: 'CN', HI: 'IN',
    AR: 'SA', HE: 'IL', VI: 'VN', CS: 'CZ', DA: 'DK',
    SV: 'SE', NB: 'NO', UK: 'UA', MS: 'MY', ID: 'ID',
  }
  const cc = region.length === 2 && region !== region.toLowerCase() ? region : (REGION_MAP[region] ?? region)
  if (cc.length !== 2) return ''
  return String.fromCodePoint(...[...cc].map(c => 0x1F1E6 + c.charCodeAt(0) - 65))
}

const sourceLanguage = computed(() => languages.value.find(l => l.isSource))
const targetLanguages = computed(() => languages.value.filter(l => !l.isSource))

async function loadLanguages() {
  isLoading.value = true
  try {
    const data = await languagesClient.list(props.projectId)
    languages.value = Array.isArray(data) ? data : []
  } catch {
    languages.value = []
  } finally {
    isLoading.value = false
  }
}

function openAddForm() {
  addError.value = ''
  newLanguageCode.value = ''
  presetCode.value = ''
  showAddForm.value = true
}

function closeAddForm() {
  showAddForm.value = false
  newLanguageCode.value = ''
  presetCode.value = ''
  addError.value = ''
}

watch(presetCode, (val) => {
  if (val) newLanguageCode.value = val
})

async function addLanguage() {
  const code = newLanguageCode.value.trim()
  if (!code) {
    addError.value = 'Language code is required'
    return
  }
  if (languages.value.some(l => l.bcp47Code.toLowerCase() === code.toLowerCase())) {
    addError.value = 'This language already exists in the project'
    return
  }
  try {
    await languagesClient.add(props.projectId, code, false)
    await loadLanguages()
    closeAddForm()
    emit('updated')
  } catch (error: any) {
    addError.value = error?.message || 'Failed to add language'
  }
}

async function toggleActive(lang: ProjectLanguage) {
  try {
    await languagesClient.toggleActive(lang.id, !lang.isActive)
    await loadLanguages()
    emit('updated')
  } catch { /* silently fail */ }
}

async function setAsSource(lang: ProjectLanguage) {
  try {
    await languagesClient.changeSource(props.projectId, lang.bcp47Code)
    await loadLanguages()
    emit('updated')
  } catch { /* silently fail */ }
}

watch(() => props.projectId, async (id) => {
  if (id) await loadLanguages()
  else languages.value = []
}, { immediate: true })
</script>

<template>
  <div class="lang-manager">
    <div class="lang-manager-header">
      <h3>Languages</h3>
      <UiButton size="sm" @click="openAddForm">Add Language</UiButton>
    </div>

    <div v-if="isLoading" class="lang-loading">Loading languages...</div>

    <template v-else>
      <div v-if="sourceLanguage" class="lang-row lang-row--source">
        <span class="lang-flag">{{ flagEmoji(sourceLanguage.bcp47Code) }}</span>
        <span class="lang-code">{{ sourceLanguage.bcp47Code }}</span>
        <span class="lang-name">{{ languageName(sourceLanguage.bcp47Code) }}</span>
        <span class="lang-badge lang-badge--source">Source</span>
      </div>

      <div v-if="targetLanguages.length === 0 && !sourceLanguage" class="lang-empty">
        No languages configured. Add a language to get started.
      </div>

      <div
        v-for="lang in targetLanguages"
        :key="lang.id"
        class="lang-row"
        :class="{ 'lang-row--inactive': !lang.isActive }"
      >
        <span class="lang-flag">{{ flagEmoji(lang.bcp47Code) }}</span>
        <span class="lang-code">{{ lang.bcp47Code }}</span>
        <span class="lang-name">{{ languageName(lang.bcp47Code) }}</span>
        <span v-if="!lang.isActive" class="lang-badge lang-badge--inactive">Inactive</span>
        <div class="lang-actions">
          <UiButton size="sm" variant="ghost" @click="toggleActive(lang)">
            {{ lang.isActive ? 'Deactivate' : 'Activate' }}
          </UiButton>
          <UiButton size="sm" variant="ghost" @click="setAsSource(lang)">
            Set as Source
          </UiButton>
        </div>
      </div>
    </template>

    <div v-if="showAddForm" class="lang-add-form">
      <div class="lang-add-form-inner">
        <h4>Add language</h4>

        <div class="lang-field">
          <UiSelect
            id="langPreset"
            v-model="presetCode"
            label="Common languages"
            :options="PRESETS"
          />
          <span class="label-hint">Pick a preset or type a custom code below</span>
        </div>

        <div class="lang-field">
          <label for="langCodeInput" class="lang-field-label">Language code</label>
          <span class="label-hint">BCP-47 format, e.g. en-US, fr, de-DE, ja</span>
          <input
            id="langCodeInput"
            v-model="newLanguageCode"
            type="text"
            autocomplete="off"
            class="lang-input"
          >
        </div>

        <p v-if="addError" class="field-error">{{ addError }}</p>

        <div class="lang-add-actions">
          <UiButton size="sm" variant="secondary" @click="closeAddForm">Cancel</UiButton>
          <UiButton size="sm" @click="addLanguage">Add</UiButton>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.lang-manager {
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  background: var(--color-surface);
  padding: var(--spacing-4);
}
.lang-manager-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: var(--spacing-3);
}
.lang-manager-header h3 {
  margin: 0;
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
}
.lang-loading {
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
  padding: var(--spacing-3) 0;
}
.lang-empty {
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
  padding: var(--spacing-3) 0;
}
.lang-row {
  display: flex;
  align-items: center;
  gap: var(--spacing-3);
  padding: var(--spacing-3) var(--spacing-2);
  border-bottom: 1px solid var(--color-border);
}
.lang-row:last-child { border-bottom: none; }
.lang-row--source {
  background: color-mix(in srgb, var(--color-primary-600) 8%, transparent);
  border-radius: var(--radius-md);
  border-bottom: 1px solid var(--color-border);
}
.lang-row--inactive { opacity: 0.6; }
.lang-flag { font-size: var(--font-size-lg); }
.lang-code {
  font-family: monospace;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-primary);
  min-width: 50px;
}
.lang-name {
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
  flex: 1;
}
.lang-badge {
  font-size: var(--font-size-xs);
  padding: 2px var(--spacing-2);
  border-radius: var(--radius-md);
  font-weight: var(--font-weight-medium);
}
.lang-badge--source {
  background: var(--color-primary-600);
  color: var(--color-white);
}
.lang-badge--inactive {
  background: var(--color-gray-200);
  color: var(--color-gray-600);
}
.lang-actions {
  display: flex;
  gap: var(--spacing-1);
  margin-left: auto;
}
.lang-add-form {
  margin-top: var(--spacing-3);
  border-top: 1px solid var(--color-border);
  padding-top: var(--spacing-3);
}
.lang-add-form-inner {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-3);
}
.lang-add-form-inner h4 {
  margin: 0;
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
}
.lang-field {
  display: flex;
  flex-direction: column;
  gap: 2px;
}
.lang-field-label {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-secondary);
}
.label-hint {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}
.lang-input {
  padding: var(--spacing-3) var(--spacing-4);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  background: var(--color-background);
  color: var(--color-text-primary);
  font-family: monospace;
  font-size: var(--font-size-base);
  margin-top: var(--spacing-1);
}
.lang-input:focus {
  outline: none;
  border-color: var(--color-primary-500);
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.15);
}
.field-error {
  margin: 0;
  color: var(--color-error);
  font-size: var(--font-size-xs);
}
.lang-add-actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-2);
}
</style>
