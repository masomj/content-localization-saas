<script setup lang="ts">
interface Props {
  variant?: 'primary' | 'secondary' | 'ghost' | 'danger'
  size?: 'sm' | 'md' | 'lg'
  type?: 'button' | 'submit' | 'reset'
  disabled?: boolean
  loading?: boolean
  block?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  variant: 'primary',
  size: 'md',
  type: 'button',
  disabled: false,
  loading: false,
  block: false,
})

const emit = defineEmits<{
  click: [event: MouseEvent]
}>()

function handleClick(event: MouseEvent) {
  if (!props.disabled && !props.loading) {
    emit('click', event)
  }
}
</script>

<template>
  <button
    :type="type"
    :class="[
      'ui-button',
      `ui-button--${variant}`,
      `ui-button--${size}`,
      { 'ui-button--block': block, 'ui-button--loading': loading }
    ]"
    :disabled="disabled || loading"
    @click="handleClick"
  >
    <span v-if="loading" class="ui-button__loader" aria-hidden="true">
      <svg class="spinner" viewBox="0 0 24 24">
        <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="3" fill="none" stroke-linecap="round">
          <animate attributeName="stroke-dasharray" values="0 150;42 150;42 150;42 150" dur="1.5s" repeatCount="indefinite" />
          <animate attributeName="stroke-dashoffset" values="0;-16;-59;-59" dur="1.5s" repeatCount="indefinite" />
        </circle>
      </svg>
    </span>
    <span :class="{ 'ui-button__content--hidden': loading }">
      <slot />
    </span>
  </button>
</template>

<style scoped>
.ui-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: var(--spacing-2);
  font-family: inherit;
  font-weight: var(--font-weight-medium);
  border-radius: var(--radius-lg);
  border: 1px solid transparent;
  cursor: pointer;
  transition: all var(--transition-fast);
  position: relative;
  white-space: nowrap;
}

.ui-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.ui-button--sm {
  padding: var(--spacing-2) var(--spacing-3);
  font-size: var(--font-size-sm);
}

.ui-button--md {
  padding: var(--spacing-2) var(--spacing-4);
  font-size: var(--font-size-sm);
}

.ui-button--lg {
  padding: var(--spacing-3) var(--spacing-6);
  font-size: var(--font-size-base);
}

.ui-button--block {
  width: 100%;
}

.ui-button--primary {
  background: var(--color-primary-600);
  color: var(--color-white);
}

.ui-button--primary:hover:not(:disabled) {
  background: var(--color-primary-700);
}

.ui-button--primary:active:not(:disabled) {
  background: var(--color-primary-800);
}

.ui-button--secondary {
  background: var(--color-gray-100);
  color: var(--color-gray-800);
  border-color: var(--color-gray-300);
}

.ui-button--secondary:hover:not(:disabled) {
  background: var(--color-gray-200);
}

.ui-button--ghost {
  background: transparent;
  color: var(--color-gray-700);
}

.ui-button--ghost:hover:not(:disabled) {
  background: var(--color-gray-100);
}

.ui-button--danger {
  background: var(--color-error);
  color: var(--color-white);
}

.ui-button--danger:hover:not(:disabled) {
  background: #dc2626;
}

.ui-button__loader {
  position: absolute;
  display: flex;
  align-items: center;
  justify-content: center;
}

.ui-button__content--hidden {
  visibility: hidden;
}

.spinner {
  width: 1.25em;
  height: 1.25em;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  from {
    transform: rotate(0deg);
  }
  to {
    transform: rotate(360deg);
  }
}
</style>
