<script setup lang="ts">
interface Props {
  message?: string
  id?: string
}

const props = withDefaults(defineProps<Props>(), {
  message: '',
})

const errorId = computed(() => props.id || 'form-error')
</script>

<template>
  <div v-if="message" :id="errorId" class="ui-form-error" role="alert">
    <svg class="ui-form-error-icon" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
      <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.28 7.22a.75.75 0 00-1.06 1.06L8.94 10l-1.72 1.72a.75.75 0 101.06 1.06L10 11.06l1.72 1.72a.75.75 0 101.06-1.06L11.06 10l1.72-1.72a.75.75 0 00-1.06-1.06L10 8.94 8.28 7.22z" clip-rule="evenodd" />
    </svg>
    <span>{{ message }}</span>
  </div>
</template>

<style scoped>
.ui-form-error {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
  padding: var(--spacing-3) var(--spacing-4);
  background: #fef2f2;
  border: 1px solid #fecaca;
  border-radius: var(--radius-lg);
  color: var(--color-error);
  font-size: var(--font-size-sm);
}

.ui-form-error-icon {
  flex-shrink: 0;
  width: 1.25rem;
  height: 1.25rem;
}
</style>
