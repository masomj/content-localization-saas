<script setup lang="ts">
import UiButton from '~/components/ui/Button.vue'
import UiSelect from '~/components/ui/Select.vue'
import { translationClient } from '~/api/translationClient'
import { glossaryClient } from '~/api/glossaryClient'
import { styleRulesClient } from '~/api/styleRulesClient'
import type { GlossarySuggestion } from '~/api/glossaryClient'
import type { LanguageTask, TranslationSuggestion, ForbiddenMatch, StyleViolation } from '~/api/types'

interface Props {
  itemId: string
  itemKey: string
  source: string
  language: string
  maxLength?: number | null
  projectId?: string | null
  contentType?: string | null
}

const props = defineProps<Props>()
const emit = defineEmits<{
  close: []
  saved: []
}>()

const translationText = ref('')
const status = ref('draft')
const previousApproved = ref('')
const isOutdated = ref(false)
const isSaving = ref(false)
const isLoadingTask = ref(false)
const isLoadingSuggestion = ref(false)
const saveError = ref('')

const suggestion = ref<TranslationSuggestion>({ hasSuggestion: false, suggestion: null })

const glossarySuggestions = ref<GlossarySuggestion[]>([])
const isLoadingGlossary = ref(false)

// Forbidden terms state
const forbiddenMatches = ref<ForbiddenMatch[]>([])
let forbiddenDebounceTimer: ReturnType<typeof setTimeout> | null = null

// Style violations state
const styleViolations = ref<StyleViolation[]>([])
const dismissedViolations = ref<Set<string>>(new Set())

const STATUS_OPTIONS = [
  { value: 'draft', label: 'Draft' },
  { value: 'pending_review', label: 'Pending review' },
  { value: 'approved', label: 'Approved' },
]

async function loadExistingTask() {
  isLoadingTask.value = true
  try {
    const tasks = await translationClient.getTasks(props.itemId)
    const match = (Array.isArray(tasks) ? tasks : []).find(
      (t: LanguageTask) => t.languageCode === props.language,
    )
    if (match) {
      translationText.value = match.translationText ?? ''
      status.value = match.status ?? 'draft'
      previousApproved.value = match.previousApprovedTranslation ?? ''
      isOutdated.value = match.isOutdated ?? false
    }
  } catch {
    // no existing task — start fresh
  } finally {
    isLoadingTask.value = false
  }
}

async function loadSuggestion() {
  isLoadingSuggestion.value = true
  try {
    suggestion.value = await translationClient.getSuggestion(props.itemId, props.language)
  } catch {
    suggestion.value = { hasSuggestion: false, suggestion: null }
  } finally {
    isLoadingSuggestion.value = false
  }
}

async function loadGlossarySuggestions() {
  if (!props.source) return
  isLoadingGlossary.value = true
  try {
    glossarySuggestions.value = await glossaryClient.suggest(props.source, props.language)
  } catch {
    glossarySuggestions.value = []
  } finally {
    isLoadingGlossary.value = false
  }
}

async function checkForbiddenTerms() {
  const text = translationText.value
  if (!text.trim()) {
    forbiddenMatches.value = []
    return
  }
  try {
    forbiddenMatches.value = await glossaryClient.forbiddenCheck(text, props.language)
  } catch {
    forbiddenMatches.value = []
  }
}

watch(translationText, () => {
  if (forbiddenDebounceTimer) clearTimeout(forbiddenDebounceTimer)
  forbiddenDebounceTimer = setTimeout(() => {
    checkForbiddenTerms()
  }, 500)
})

const visibleStyleViolations = computed(() =>
  styleViolations.value.filter(v => !dismissedViolations.value.has(v.ruleId)),
)

function dismissViolation(ruleId: string) {
  dismissedViolations.value.add(ruleId)
}

async function checkStyleRules() {
  if (!props.projectId) return
  try {
    styleViolations.value = await styleRulesClient.check(
      translationText.value,
      props.projectId,
      props.contentType || '',
    )
    dismissedViolations.value.clear()
  } catch {
    styleViolations.value = []
  }
}

function insertGlossaryTerm(translated: string) {
  const textarea = document.getElementById('teTranslation') as HTMLTextAreaElement | null
  if (textarea) {
    const start = textarea.selectionStart
    const end = textarea.selectionEnd
    translationText.value = translationText.value.substring(0, start) + translated + translationText.value.substring(end)
    nextTick(() => {
      textarea.focus()
      textarea.selectionStart = textarea.selectionEnd = start + translated.length
    })
  } else {
    translationText.value += translated
  }
}

async function applySuggestion() {
  if (!suggestion.value.suggestion) return
  try {
    const result = await translationClient.applyMemory({
      contentItemId: props.itemId,
      languageCode: props.language,
      acceptSuggestion: true,
    })
    translationText.value = result.translationText ?? suggestion.value.suggestion.translationText
    status.value = result.status ?? status.value
  } catch {
    // fallback: just copy the text in
    translationText.value = suggestion.value.suggestion.translationText
  }
}

async function save() {
  saveError.value = ''
  isSaving.value = true
  try {
    await translationClient.upsert({
      contentItemId: props.itemId,
      languageCode: props.language,
      status: status.value,
      translationText: translationText.value,
    })
    await checkStyleRules()
    emit('saved')
  } catch (error: any) {
    saveError.value = error?.message || 'Failed to save translation'
  } finally {
    isSaving.value = false
  }
}

function handleKeydown(e: KeyboardEvent) {
  if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
    e.preventDefault()
    save()
  }
  if (e.key === 'Escape') {
    emit('close')
  }
}

onMounted(async () => {
  window.addEventListener('keydown', handleKeydown)
  await Promise.all([loadExistingTask(), loadSuggestion(), loadGlossarySuggestions()])
})

onUnmounted(() => {
  window.removeEventListener('keydown', handleKeydown)
})
</script>

<template>
  <div class="te-overlay" @click.self="$emit('close')">
    <aside class="te-panel">
      <header class="te-header">
        <div>
          <h2 class="te-title">Edit Translation</h2>
          <p class="te-subtitle">{{ language }}</p>
        </div>
        <button class="te-close" aria-label="Close" @click="$emit('close')">&times;</button>
      </header>

      <div v-if="isLoadingTask" class="te-loading">Loading translation...</div>

      <div v-else class="te-body">
        <!-- Key name -->
        <div class="te-field">
          <label class="te-label">Key</label>
          <span class="te-hint">The content key identifier</span>
          <div class="te-key-display">{{ itemKey }}</div>
        </div>

        <!-- Source text (read-only) -->
        <div class="te-field">
          <label class="te-label">Source text</label>
          <span class="te-hint">Original text in the source language (read-only)</span>
          <div class="te-source-display">{{ source }}</div>
          <div v-if="glossarySuggestions.length > 0" class="te-glossary-badges">
            <span
              v-for="(gs, i) in glossarySuggestions"
              :key="i"
              class="te-glossary-badge"
              :title="`${gs.glossaryName}${gs.definition ? ': ' + gs.definition : ''}`"
              @click="insertGlossaryTerm(gs.translatedTerm || gs.term)"
            >
              {{ gs.term }}<template v-if="gs.translatedTerm"> → {{ gs.translatedTerm }}</template>
            </span>
          </div>
        </div>

        <!-- Outdated warning -->
        <div v-if="isOutdated" class="te-banner te-banner--warning">
          <span class="te-banner-icon">&#9888;</span>
          <div>
            <strong>Source text has changed</strong>
            <p class="te-banner-detail">
              The source was updated since this translation was approved. Please review and update.
            </p>
            <p v-if="previousApproved" class="te-banner-detail">
              <strong>Previous approved translation:</strong> {{ previousApproved }}
            </p>
          </div>
        </div>

        <!-- TM suggestion -->
        <div v-if="isLoadingSuggestion" class="te-banner te-banner--info">
          Checking translation memory...
        </div>
        <div v-else-if="suggestion.hasSuggestion && suggestion.suggestion" class="te-banner te-banner--info">
          <span class="te-banner-icon">&#128161;</span>
          <div class="te-banner-content">
            <strong>Translation memory suggestion</strong>
            <p class="te-banner-detail">&ldquo;{{ suggestion.suggestion.translationText }}&rdquo;</p>
            <UiButton size="sm" variant="secondary" @click="applySuggestion">Apply suggestion</UiButton>
          </div>
        </div>

        <!-- Translation textarea -->
        <div class="te-field">
          <label for="teTranslation" class="te-label">Translation</label>
          <span class="te-hint">Write the translated text for {{ language }}</span>
          <textarea
            id="teTranslation"
            v-model="translationText"
            class="te-textarea"
            rows="5"
          />
          <div v-if="props.maxLength" class="te-maxlength" :class="{ 'te-maxlength--warn': translationText.length >= props.maxLength * 0.9 && translationText.length <= props.maxLength, 'te-maxlength--over': translationText.length > props.maxLength }">
            <span v-if="translationText.length > props.maxLength" class="te-maxlength-icon">&#9888;</span>
            {{ translationText.length }} / {{ props.maxLength }}
          </div>
        </div>

        <!-- Forbidden term warnings -->
        <div v-if="forbiddenMatches.length > 0" class="te-forbidden-list">
          <div v-for="(fm, i) in forbiddenMatches" :key="i" class="te-forbidden-item">
            &#10060; '{{ fm.term }}' &rarr; use '{{ fm.replacement }}' instead
          </div>
        </div>

        <!-- Style violation warnings -->
        <div v-if="visibleStyleViolations.length > 0" class="te-style-list">
          <div v-for="v in visibleStyleViolations" :key="v.ruleId" class="te-style-item">
            <span>&#9888;&#65039; {{ v.message }}</span>
            <button class="te-style-dismiss" @click="dismissViolation(v.ruleId)">Dismiss</button>
          </div>
        </div>

        <!-- Status selector -->
        <div class="te-field">
          <UiSelect
            id="teStatus"
            v-model="status"
            label="Status"
            :options="STATUS_OPTIONS"
          />
          <span class="te-hint">Set the review status of this translation</span>
        </div>

        <!-- Error message -->
        <p v-if="saveError" class="te-error">{{ saveError }}</p>

        <!-- Actions -->
        <div class="te-actions">
          <span class="te-shortcut-hint">Ctrl+Enter to save</span>
          <UiButton variant="secondary" @click="$emit('close')">Cancel</UiButton>
          <UiButton :disabled="isSaving" @click="save">
            {{ isSaving ? 'Saving...' : 'Save translation' }}
          </UiButton>
        </div>
      </div>
    </aside>
  </div>
</template>

<style scoped>
.te-overlay {
  position: fixed;
  inset: 0;
  background: color-mix(in srgb, var(--color-black) 45%, transparent);
  display: flex;
  justify-content: flex-end;
  z-index: var(--z-modal);
}

.te-panel {
  width: min(560px, 92vw);
  height: 100vh;
  background: var(--color-surface);
  border-left: 1px solid var(--color-border);
  display: flex;
  flex-direction: column;
  overflow-y: auto;
}

.te-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  padding: var(--spacing-6);
  border-bottom: 1px solid var(--color-border);
  flex-shrink: 0;
}

.te-title {
  margin: 0;
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
}

.te-subtitle {
  margin: var(--spacing-1) 0 0;
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
}

.te-close {
  background: none;
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  color: var(--color-text-muted);
  padding: var(--spacing-1) var(--spacing-2);
  border-radius: var(--radius-md);
  transition: background var(--transition-fast);
}
.te-close:hover {
  background: var(--color-border);
  color: var(--color-text-primary);
}

.te-loading {
  padding: var(--spacing-6);
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
  text-align: center;
}

.te-body {
  padding: var(--spacing-6);
  display: flex;
  flex-direction: column;
  gap: var(--spacing-4);
  flex: 1;
}

.te-field {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.te-label {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-secondary);
}

.te-hint {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

.te-key-display {
  font-family: monospace;
  font-size: var(--font-size-sm);
  background: var(--color-background);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-3) var(--spacing-4);
  color: var(--color-text-primary);
  margin-top: var(--spacing-1);
}

.te-source-display {
  font-size: var(--font-size-sm);
  background: var(--color-background);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-3) var(--spacing-4);
  color: var(--color-text-primary);
  white-space: pre-wrap;
  margin-top: var(--spacing-1);
}

.te-banner {
  display: flex;
  gap: var(--spacing-3);
  padding: var(--spacing-3) var(--spacing-4);
  border-radius: var(--radius-lg);
  font-size: var(--font-size-sm);
}
.te-banner--warning {
  background: color-mix(in srgb, #f97316 10%, transparent);
  border: 1px solid color-mix(in srgb, #f97316 25%, transparent);
  color: #c2410c;
}
.te-banner--info {
  background: color-mix(in srgb, var(--color-primary-600) 8%, transparent);
  border: 1px solid color-mix(in srgb, var(--color-primary-600) 20%, transparent);
  color: var(--color-text-primary);
}
.te-banner-icon {
  font-size: 1.2rem;
  flex-shrink: 0;
  line-height: 1;
}
.te-banner-content {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-2);
}
.te-banner-detail {
  margin: 0;
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

.te-textarea {
  padding: var(--spacing-3) var(--spacing-4);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  background: var(--color-background);
  color: var(--color-text-primary);
  font-size: var(--font-size-base);
  font-family: inherit;
  resize: vertical;
  margin-top: var(--spacing-1);
}
.te-textarea:focus {
  outline: none;
  border-color: var(--color-primary-500);
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.15);
}

.te-error {
  margin: 0;
  color: var(--color-error);
  font-size: var(--font-size-xs);
}

.te-actions {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
  margin-top: auto;
  padding-top: var(--spacing-4);
  border-top: 1px solid var(--color-border);
}

.te-shortcut-hint {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  margin-right: auto;
}

.te-maxlength {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  text-align: right;
  margin-top: var(--spacing-1);
}
.te-maxlength--warn {
  color: #d97706;
}
.te-maxlength--over {
  color: var(--color-error);
  font-weight: var(--font-weight-medium);
}
.te-maxlength-icon {
  margin-right: 2px;
}

.te-glossary-badges {
  display: flex;
  flex-wrap: wrap;
  gap: var(--spacing-1);
  margin-top: var(--spacing-2);
}

.te-glossary-badge {
  display: inline-block;
  padding: 2px var(--spacing-2);
  border-radius: var(--radius-full);
  background: color-mix(in srgb, var(--color-primary-500) 10%, var(--color-surface));
  border: 1px solid color-mix(in srgb, var(--color-primary-500) 25%, var(--color-border));
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
  cursor: pointer;
  transition: background var(--transition-fast);
}

.te-glossary-badge:hover {
  background: color-mix(in srgb, var(--color-primary-500) 20%, var(--color-surface));
  color: var(--color-text-primary);
}

.te-forbidden-list {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-1);
}

.te-forbidden-item {
  padding: var(--spacing-2) var(--spacing-3);
  border-radius: var(--radius-md);
  background: color-mix(in srgb, var(--color-error) 10%, var(--color-surface));
  border: 1px solid color-mix(in srgb, var(--color-error) 30%, var(--color-border));
  color: var(--color-error);
  font-size: var(--font-size-sm);
}

.te-style-list {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-1);
}

.te-style-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--spacing-2);
  padding: var(--spacing-2) var(--spacing-3);
  border-radius: var(--radius-md);
  background: color-mix(in srgb, var(--color-warning) 10%, var(--color-surface));
  border: 1px solid color-mix(in srgb, var(--color-warning) 30%, var(--color-border));
  color: var(--color-warning);
  font-size: var(--font-size-sm);
}

.te-style-dismiss {
  background: none;
  border: 1px solid color-mix(in srgb, var(--color-warning) 40%, var(--color-border));
  border-radius: var(--radius-md);
  padding: 1px var(--spacing-2);
  font-size: var(--font-size-xs);
  color: var(--color-warning);
  cursor: pointer;
  flex-shrink: 0;
  transition: background var(--transition-fast);
}

.te-style-dismiss:hover {
  background: color-mix(in srgb, var(--color-warning) 15%, var(--color-surface));
}
</style>
