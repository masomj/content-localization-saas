<script setup lang="ts">
import UiSelect from '~/components/ui/Select.vue'

const theme = useTheme()

const options = [
  { value: 'system', label: 'System' },
  { value: 'light', label: 'Light' },
  { value: 'dark', label: 'Dark' },
] as const
</script>

<template>
  <div class="theme-toggle" data-testid="theme-toggle">
    <span class="theme-toggle__icon" aria-hidden="true">🌓</span>
    <UiSelect
      id="theme-preference"
      v-model="theme.preference.value"
      class="theme-toggle__select"
      :options="options"
      label="Theme"
      @update:model-value="theme.setThemePreference($event as 'light' | 'dark' | 'system')"
    />
  </div>
</template>

<style scoped>
.theme-toggle {
  display: inline-flex;
  align-items: flex-end;
  gap: var(--spacing-2);
}

.theme-toggle__icon {
  font-size: var(--font-size-sm);
  margin-bottom: 0.55rem;
}

.theme-toggle__select {
  min-width: 9rem;
}

:deep(.theme-toggle__select .ui-select-wrapper) {
  gap: var(--spacing-1);
}

:deep(.theme-toggle__select .ui-select-label) {
  font-size: var(--font-size-xs);
}

:deep(.theme-toggle__select .ui-select) {
  padding-top: var(--spacing-2);
  padding-bottom: var(--spacing-2);
  font-size: var(--font-size-sm);
}
</style>
