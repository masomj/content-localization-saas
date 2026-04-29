<script setup lang="ts">
interface Props {
  id?: string
  name?: string
  type?: 'text' | 'email' | 'password' | 'number' | 'tel' | 'url'
  value?: string | number
  placeholder?: string
  disabled?: boolean
  required?: boolean
  autocomplete?: string
  error?: string
  label?: string
  hint?: string
}

const props = withDefaults(defineProps<Props>(), {
  type: 'text',
  disabled: false,
  required: false,
})

const modelValue = defineModel<string | number>()

const inputId = computed(() => props.id || `input-${props.name}`)
const errorId = computed(() => `${inputId.value}-error`)
const hintId = computed(() => `${inputId.value}-hint`)

const ariaDescribedBy = computed(() => {
  const ids: string[] = []
  if (props.error) ids.push(errorId.value)
  if (props.hint) ids.push(hintId.value)
  return ids.length > 0 ? ids.join(' ') : undefined
})
</script>

<template>
  <div class="ui-input-wrapper">
    <label v-if="label" :for="inputId" class="ui-input-label">
      {{ label }}
      <span v-if="required" class="ui-input-required" aria-hidden="true">*</span>
    </label>
    <div class="ui-input-container" :class="{ 'ui-input-container--error': error }">
      <input
        :id="inputId"
        v-model="modelValue"
        :name="name"
        :type="type"
        :placeholder="placeholder"
        :disabled="disabled"
        :required="required"
        :autocomplete="autocomplete"
        :aria-describedby="ariaDescribedBy"
        :aria-invalid="error ? 'true' : undefined"
        class="ui-input"
      >
    </div>
    <p v-if="hint && !error" :id="hintId" class="ui-input-hint">{{ hint }}</p>
    <p v-if="error" :id="errorId" class="ui-input-error" role="alert">{{ error }}</p>
  </div>
</template>

<style scoped>
.ui-input-wrapper {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-2);
}

.ui-input-label {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-primary);
}

.ui-input-required {
  color: var(--color-error);
  margin-left: var(--spacing-1);
}

.ui-input-container {
  position: relative;
}

.ui-input {
  width: 100%;
  padding: var(--spacing-3) var(--spacing-4);
  font-family: inherit;
  font-size: var(--font-size-base);
  color: var(--color-text-primary);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  transition: border-color var(--transition-fast), box-shadow var(--transition-fast);
}

.ui-input::placeholder {
  color: var(--color-gray-400);
}

.ui-input:focus {
  outline: none;
  border-color: var(--color-primary-500);
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.15);
}

.ui-input:disabled {
  background: var(--color-surface-muted, color-mix(in srgb, var(--color-surface) 85%, var(--color-border)));
  cursor: not-allowed;
}

.ui-input-container--error .ui-input {
  border-color: var(--color-error);
}

.ui-input-container--error .ui-input:focus {
  box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.15);
}

.ui-input-hint {
  font-size: var(--font-size-xs);
  color: var(--color-gray-500);
  margin: 0;
}

.ui-input-error {
  font-size: var(--font-size-sm);
  color: var(--color-error);
  margin: 0;
}
</style>
