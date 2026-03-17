<script setup lang="ts">
interface Props {
  id?: string
  name?: string
  value?: string
  disabled?: boolean
  required?: boolean
  error?: string
  label?: string
  options: { value: string; label: string }[]
}

const props = withDefaults(defineProps<Props>(), {
  disabled: false,
  required: false,
})

const modelValue = defineModel<string>()

const inputId = computed(() => props.id || `select-${props.name}`)
const errorId = computed(() => `${inputId.value}-error`)

const ariaDescribedBy = computed(() => {
  return props.error ? errorId.value : undefined
})
</script>

<template>
  <div class="ui-select-wrapper">
    <label v-if="label" :for="inputId" class="ui-select-label">
      {{ label }}
      <span v-if="required" class="ui-select-required" aria-hidden="true">*</span>
    </label>
    <div class="ui-select-container" :class="{ 'ui-select-container--error': error }">
      <select
        :id="inputId"
        v-model="modelValue"
        :name="name"
        :disabled="disabled"
        :required="required"
        :aria-describedby="ariaDescribedBy"
        :aria-invalid="error ? 'true' : undefined"
        class="ui-select"
      >
        <option v-for="option in options" :key="option.value" :value="option.value">
          {{ option.label }}
        </option>
      </select>
    </div>
    <p v-if="error" :id="errorId" class="ui-select-error" role="alert">{{ error }}</p>
  </div>
</template>

<style scoped>
.ui-select-wrapper {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-2);
}

.ui-select-label {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-secondary);
}

.ui-select-required {
  color: var(--color-error);
  margin-left: var(--spacing-1);
}

.ui-select-container {
  position: relative;
}

.ui-select {
  width: 100%;
  padding: var(--spacing-3) var(--spacing-4);
  font-family: inherit;
  font-size: var(--font-size-base);
  color: var(--color-text-primary);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  transition: border-color var(--transition-fast), box-shadow var(--transition-fast);
  appearance: none;
  color-scheme: light dark;
  background-image: url("data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' fill='none' viewBox='0 0 20 20'%3e%3cpath stroke='%236b7280' stroke-linecap='round' stroke-linejoin='round' stroke-width='1.5' d='M6 8l4 4 4-4'/%3e%3c/svg%3e");
  background-position: right var(--spacing-3) center;
  background-repeat: no-repeat;
  background-size: 1.25em;
  padding-right: 2.5rem;
}

.ui-select option {
  background: var(--color-surface);
  color: var(--color-text-primary);
}

.ui-select:focus {
  outline: none;
  border-color: var(--color-primary-500);
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.15);
}

.ui-select:disabled {
  background: var(--color-gray-100);
  cursor: not-allowed;
}

.ui-select-container--error .ui-select {
  border-color: var(--color-error);
}

.ui-select-container--error .ui-select:focus {
  box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.15);
}

.ui-select-error {
  font-size: var(--font-size-sm);
  color: var(--color-error);
  margin: 0;
}
</style>
