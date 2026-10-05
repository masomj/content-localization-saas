<script setup lang="ts">
/**
 * #57 Environment variants: copy that only appears in exports for a named
 * environment (e.g. ?environment=dev). Production copy is never changed here.
 */
import UiButton from '~/components/ui/Button.vue'
import { ApiError } from '~/api/client'
import { environmentVariantsClient } from '~/api/environmentVariantsClient'
import type { EnvironmentVariant } from '~/api/types'

interface Props {
  contentItemId: string
  projectId: string
  /** Active target language codes (source excluded). */
  targetLanguages: string[]
  /** Editors and above can change variants. */
  canEdit?: boolean
}

const props = withDefaults(defineProps<Props>(), { canEdit: true })

const SUGGESTED_ENVIRONMENTS = ['dev', 'staging', 'test']
const SOURCE = ''

const variants = ref<EnvironmentVariant[]>([])
const knownEnvironments = ref<string[]>([])
const isLoading = ref(false)
const isSaving = ref(false)
const error = ref('')
const pendingRemoveId = ref<string | null>(null)

const formEnvironment = ref('dev')
const formLanguage = ref(SOURCE)
const formValue = ref('')

const datalistId = computed(() => `env-suggestions-${props.contentItemId}`)

const environmentSuggestions = computed(() =>
  Array.from(new Set([...knownEnvironments.value, ...SUGGESTED_ENVIRONMENTS])).sort(),
)

const languageOptions = computed(() => [
  { value: SOURCE, label: 'Source text' },
  ...props.targetLanguages.map(code => ({ value: code, label: code })),
])

const isEditingExisting = computed(() =>
  variants.value.some(v => v.environment === normalizedFormEnvironment.value && v.languageCode === formLanguage.value),
)

const normalizedFormEnvironment = computed(() => formEnvironment.value.trim().toLowerCase())

function languageLabel(code: string) {
  return code === SOURCE ? 'Source' : code
}

function describeError(e: unknown, fallback: string) {
  if (e instanceof ApiError) {
    if (e.errors && typeof e.errors === 'object') {
      const messages = Object.values(e.errors as Record<string, string[]>).flat()
      if (messages.length) return messages.join(' ')
    }
    return e.message || fallback
  }
  return fallback
}

async function load() {
  isLoading.value = true
  error.value = ''
  try {
    const [rows, envs] = await Promise.all([
      environmentVariantsClient.list(props.contentItemId),
      environmentVariantsClient.listEnvironments(props.projectId).catch(() => []),
    ])
    variants.value = rows
    knownEnvironments.value = envs.map(e => e.name)
  } catch (e) {
    error.value = describeError(e, 'Could not load environment variants.')
  } finally {
    isLoading.value = false
  }
}

function editVariant(variant: EnvironmentVariant) {
  formEnvironment.value = variant.environment
  formLanguage.value = variant.languageCode
  formValue.value = variant.value
  pendingRemoveId.value = null
}

function resetForm() {
  formLanguage.value = SOURCE
  formValue.value = ''
}

async function save() {
  if (!normalizedFormEnvironment.value || !formValue.value.trim()) {
    error.value = 'Enter an environment name and the variant copy.'
    return
  }
  isSaving.value = true
  error.value = ''
  try {
    const saved = await environmentVariantsClient.upsert(props.contentItemId, {
      environment: normalizedFormEnvironment.value,
      languageCode: formLanguage.value,
      value: formValue.value,
    })
    const idx = variants.value.findIndex(v => v.id === saved.id)
    if (idx >= 0) variants.value.splice(idx, 1, saved)
    else variants.value.push(saved)
    variants.value.sort((a, b) => a.environment.localeCompare(b.environment) || a.languageCode.localeCompare(b.languageCode))
    if (!knownEnvironments.value.includes(saved.environment)) knownEnvironments.value.push(saved.environment)
    resetForm()
  } catch (e) {
    error.value = describeError(e, 'Could not save the variant.')
  } finally {
    isSaving.value = false
  }
}

async function remove(variant: EnvironmentVariant) {
  if (pendingRemoveId.value !== variant.id) {
    pendingRemoveId.value = variant.id
    return
  }
  error.value = ''
  try {
    await environmentVariantsClient.remove(props.contentItemId, variant.id)
    variants.value = variants.value.filter(v => v.id !== variant.id)
  } catch (e) {
    error.value = describeError(e, 'Could not remove the variant.')
  } finally {
    pendingRemoveId.value = null
  }
}

watch(() => props.contentItemId, () => {
  resetForm()
  pendingRemoveId.value = null
  load()
}, { immediate: true })
</script>

<template>
  <section class="env-variants" aria-labelledby="env-variants-heading">
    <h3 id="env-variants-heading" class="env-variants-heading">Environment variants</h3>
    <p class="env-variants-intro">
      Test alternative copy in one environment while production keeps the text above. Variants appear only in exports that request their environment, e.g.
      <code>?environment=dev</code>.
    </p>

    <p v-if="isLoading" class="env-variants-muted">Loading variants…</p>

    <ul v-else-if="variants.length" class="env-variants-list">
      <li v-for="variant in variants" :key="variant.id" class="env-variant-row">
        <div class="env-variant-meta">
          <span class="env-badge">{{ variant.environment }}</span>
          <span class="env-lang">{{ languageLabel(variant.languageCode) }}</span>
        </div>
        <p class="env-variant-value">{{ variant.value }}</p>
        <div v-if="canEdit" class="env-variant-actions">
          <UiButton size="sm" variant="ghost" @click="editVariant(variant)">Edit</UiButton>
          <UiButton
            size="sm"
            :variant="pendingRemoveId === variant.id ? 'danger' : 'ghost'"
            @click="remove(variant)"
          >
            {{ pendingRemoveId === variant.id ? 'Confirm remove' : 'Remove' }}
          </UiButton>
        </div>
      </li>
    </ul>

    <p v-else class="env-variants-muted">No variants yet. Every environment gets the production copy.</p>

    <form v-if="canEdit" class="env-variant-form" @submit.prevent="save">
      <div class="env-variant-form-row">
        <label :for="`env-name-${contentItemId}`" class="env-label">
          <span>Environment</span>
          <span class="env-hint">Lowercase name, e.g. dev or staging</span>
          <input
            :id="`env-name-${contentItemId}`"
            v-model="formEnvironment"
            :list="datalistId"
            class="env-input"
            maxlength="32"
            autocomplete="off"
          >
        </label>
        <datalist :id="datalistId">
          <option v-for="env in environmentSuggestions" :key="env" :value="env" />
        </datalist>

        <label :for="`env-lang-${contentItemId}`" class="env-label">
          <span>Language</span>
          <span class="env-hint">Source text or a target language</span>
          <select :id="`env-lang-${contentItemId}`" v-model="formLanguage" class="env-input">
            <option v-for="opt in languageOptions" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
          </select>
        </label>
      </div>

      <label :for="`env-value-${contentItemId}`" class="env-label">
        <span>Variant copy</span>
        <span class="env-hint">Replaces the production text for this environment only</span>
        <textarea
          :id="`env-value-${contentItemId}`"
          v-model="formValue"
          rows="2"
          maxlength="4000"
          class="env-input env-textarea"
        />
      </label>

      <div class="env-variant-form-actions">
        <UiButton type="submit" size="sm" variant="secondary" :loading="isSaving">
          {{ isEditingExisting ? 'Update variant' : 'Add variant' }}
        </UiButton>
      </div>
    </form>

    <p v-if="error" class="env-error" role="alert">{{ error }}</p>
  </section>
</template>

<style scoped>
.env-variants {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-3);
  margin-top: var(--spacing-2);
  padding-top: var(--spacing-3);
  border-top: 1px solid var(--color-border);
}
.env-variants-heading {
  margin: 0;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
}
.env-variants-intro,
.env-variants-muted {
  margin: 0;
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}
.env-variants-intro code {
  font-size: var(--font-size-xs);
  padding: 0 var(--spacing-1);
  border-radius: var(--radius-sm);
  background: var(--color-gray-100);
  color: var(--color-text-secondary);
}
.env-variants-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: var(--spacing-2);
}
.env-variant-row {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-1);
  padding: var(--spacing-2) var(--spacing-3);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  background: var(--color-surface);
}
.env-variant-meta {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
}
.env-badge {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  padding: 0 var(--spacing-2);
  border-radius: var(--radius-full);
  background: var(--color-primary-50);
  color: var(--color-primary-700);
}
.env-lang {
  font-size: var(--font-size-xs);
  color: var(--color-text-secondary);
}
.env-variant-value {
  margin: 0;
  font-size: var(--font-size-sm);
  color: var(--color-text-primary);
  white-space: pre-wrap;
  word-break: break-word;
}
.env-variant-actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-1);
}
.env-variant-form {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-3);
}
.env-variant-form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--spacing-3);
}
.env-label {
  display: flex;
  flex-direction: column;
  gap: 2px;
  font-size: var(--font-size-sm);
  color: var(--color-text-primary);
}
.env-hint {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  margin-bottom: var(--spacing-1);
}
.env-input {
  padding: var(--spacing-2) var(--spacing-3);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  background: var(--color-background);
  color: var(--color-text-primary);
  font-size: var(--font-size-sm);
  font-family: inherit;
}
.env-input:focus {
  outline: none;
  border-color: var(--color-primary-500);
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--color-primary-500) 20%, transparent);
}
.env-textarea {
  resize: vertical;
  min-height: 56px;
}
.env-variant-form-actions {
  display: flex;
  justify-content: flex-end;
}
.env-error {
  margin: 0;
  font-size: var(--font-size-sm);
  color: var(--color-error);
}
@media (max-width: 480px) {
  .env-variant-form-row {
    grid-template-columns: 1fr;
  }
}
</style>
