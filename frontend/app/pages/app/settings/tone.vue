<script setup lang="ts">
import UiButton from '~/components/ui/Button.vue'
import { toneCheckClient } from '~/api/toneCheckClient'
import type { ProjectToneConfig } from '~/api/types'

definePageMeta({ layout: 'app' })

const auth = useAuth()

const projects = ref<{ id: string; name: string }[]>([])
const selectedProjectId = ref('')
const toneDescription = ref('')
const isActive = ref(false)
const isLoading = ref(false)
const isSaving = ref(false)
const saveSuccess = ref(false)
const saveError = ref('')
const hasExistingConfig = ref(false)

const TONE_PRESETS = [
  { label: 'Friendly', value: 'Friendly and approachable, using conversational language' },
  { label: 'Professional', value: 'Professional and polished, maintaining a business-appropriate tone' },
  { label: 'Technical', value: 'Technical and precise, using domain-specific terminology accurately' },
  { label: 'Casual', value: 'Casual and relaxed, like talking to a friend' },
  { label: 'Formal', value: 'Formal and authoritative, suitable for official communications' },
]

function applyPreset(preset: string) {
  toneDescription.value = preset
}

async function loadProjects() {
  try {
    const { apiRequest } = await import('~/api/client')
    projects.value = await apiRequest<{ id: string; name: string }[]>('/projects')
  } catch {
    projects.value = []
  }
}

async function loadConfig() {
  if (!selectedProjectId.value) return
  isLoading.value = true
  saveSuccess.value = false
  saveError.value = ''
  try {
    const config = await toneCheckClient.getConfig(selectedProjectId.value)
    toneDescription.value = config.toneDescription
    isActive.value = config.isActive
    hasExistingConfig.value = true
  } catch {
    toneDescription.value = ''
    isActive.value = false
    hasExistingConfig.value = false
  } finally {
    isLoading.value = false
  }
}

async function save() {
  if (!selectedProjectId.value || !toneDescription.value.trim()) return
  isSaving.value = true
  saveSuccess.value = false
  saveError.value = ''
  try {
    if (hasExistingConfig.value) {
      if (isActive.value) {
        await toneCheckClient.updateConfig(selectedProjectId.value, toneDescription.value)
      } else {
        await toneCheckClient.deleteConfig(selectedProjectId.value)
      }
    } else {
      await toneCheckClient.createConfig(selectedProjectId.value, toneDescription.value)
      hasExistingConfig.value = true
      isActive.value = true
    }
    saveSuccess.value = true
  } catch (e: any) {
    saveError.value = e?.message || 'Failed to save tone configuration'
  } finally {
    isSaving.value = false
  }
}

watch(selectedProjectId, () => {
  loadConfig()
})

onMounted(() => {
  loadProjects()
})
</script>

<template>
  <div class="tone-settings">
    <div class="tone-settings__header">
      <h1 class="tone-settings__title">AI Tone Check</h1>
      <p class="tone-settings__desc">Configure your brand voice for AI-powered tone checking on translations.</p>
    </div>

    <div class="tone-settings__form">
      <div class="tone-settings__field">
        <label for="toneProject" class="tone-settings__label">
          <span>Project</span>
          <span class="tone-settings__hint">Select the project to configure tone for</span>
        </label>
        <select id="toneProject" v-model="selectedProjectId" class="tone-settings__select">
          <option value="" disabled>Select a project...</option>
          <option v-for="p in projects" :key="p.id" :value="p.id">{{ p.name }}</option>
        </select>
      </div>

      <div v-if="isLoading" class="tone-settings__loading">Loading configuration...</div>

      <template v-if="selectedProjectId && !isLoading">
        <div class="tone-settings__field">
          <label for="tonDescription" class="tone-settings__label">
            <span>Tone Description</span>
            <span class="tone-settings__hint">Describe your brand voice. The AI will check translations against this description.</span>
          </label>
          <textarea
            id="tonDescription"
            v-model="toneDescription"
            class="tone-settings__textarea"
            rows="4"
            placeholder="e.g. Friendly and informal, using simple words and short sentences..."
          />
        </div>

        <div class="tone-settings__presets">
          <span class="tone-settings__presets-label">Quick presets:</span>
          <div class="tone-settings__presets-list">
            <button
              v-for="preset in TONE_PRESETS"
              :key="preset.label"
              class="tone-settings__preset-btn"
              @click="applyPreset(preset.value)"
            >
              {{ preset.label }}
            </button>
          </div>
        </div>

        <div v-if="hasExistingConfig" class="tone-settings__field tone-settings__toggle-row">
          <label class="tone-settings__toggle-label">
            <input type="checkbox" v-model="isActive" class="tone-settings__checkbox" />
            <span>Active</span>
          </label>
          <span class="tone-settings__hint">When disabled, tone checking is skipped for this project.</span>
        </div>

        <p v-if="saveSuccess" class="tone-settings__success">Configuration saved successfully.</p>
        <p v-if="saveError" class="tone-settings__error">{{ saveError }}</p>

        <div class="tone-settings__actions">
          <UiButton :loading="isSaving" :disabled="!toneDescription.trim()" @click="save">
            {{ hasExistingConfig ? 'Update Configuration' : 'Enable Tone Check' }}
          </UiButton>
        </div>
      </template>
    </div>
  </div>
</template>

<style scoped>
.tone-settings {
  max-width: 720px;
  margin: 0 auto;
  padding: var(--spacing-6);
}

.tone-settings__header {
  margin-bottom: var(--spacing-8);
}

.tone-settings__title {
  margin: 0;
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  color: var(--color-text-primary);
}

.tone-settings__desc {
  margin: var(--spacing-2) 0 0;
  color: var(--color-text-secondary);
  font-size: var(--font-size-sm);
}

.tone-settings__form {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-6);
}

.tone-settings__field {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-1);
}

.tone-settings__label {
  display: flex;
  flex-direction: column;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-primary);
}

.tone-settings__hint {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  font-weight: var(--font-weight-normal);
}

.tone-settings__select {
  padding: var(--spacing-2) var(--spacing-3);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-surface);
  color: var(--color-text-primary);
  font-size: var(--font-size-sm);
}

.tone-settings__textarea {
  padding: var(--spacing-3);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-surface);
  color: var(--color-text-primary);
  font-size: var(--font-size-sm);
  resize: vertical;
  font-family: inherit;
}

.tone-settings__textarea:focus,
.tone-settings__select:focus {
  outline: none;
  border-color: var(--color-primary-500);
  box-shadow: 0 0 0 2px color-mix(in srgb, var(--color-primary-500) 20%, transparent);
}

.tone-settings__presets {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-2);
}

.tone-settings__presets-label {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

.tone-settings__presets-list {
  display: flex;
  flex-wrap: wrap;
  gap: var(--spacing-2);
}

.tone-settings__preset-btn {
  padding: var(--spacing-1) var(--spacing-3);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-full);
  background: var(--color-surface);
  color: var(--color-text-secondary);
  font-size: var(--font-size-xs);
  cursor: pointer;
  transition: all var(--transition-fast);
}

.tone-settings__preset-btn:hover {
  border-color: var(--color-primary-500);
  color: var(--color-primary-600);
  background: color-mix(in srgb, var(--color-primary-50) 50%, transparent);
}

.tone-settings__toggle-row {
  flex-direction: row;
  align-items: center;
  gap: var(--spacing-3);
}

.tone-settings__toggle-label {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-primary);
  cursor: pointer;
}

.tone-settings__checkbox {
  width: 16px;
  height: 16px;
  accent-color: var(--color-primary-500);
}

.tone-settings__loading {
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.tone-settings__success {
  margin: 0;
  color: var(--color-success);
  font-size: var(--font-size-sm);
}

.tone-settings__error {
  margin: 0;
  color: var(--color-error);
  font-size: var(--font-size-sm);
}

.tone-settings__actions {
  display: flex;
  justify-content: flex-start;
}
</style>
