<script setup lang="ts">
const theme = useTheme()

const options = [
  { value: 'system', label: 'System' },
  { value: 'light', label: 'Light' },
  { value: 'dark', label: 'Dark' },
] as const
</script>

<template>
  <label class="theme-toggle" data-testid="theme-toggle">
    <span class="theme-toggle__icon" aria-hidden="true">🌓</span>
    <span class="sr-only">Theme</span>
    <select
      class="theme-toggle__select"
      :value="theme.preference.value"
      aria-label="Theme"
      @change="theme.setThemePreference(($event.target as HTMLSelectElement).value as 'light' | 'dark' | 'system')"
    >
      <option v-for="option in options" :key="option.value" :value="option.value">
        {{ option.label }}
      </option>
    </select>
  </label>
</template>

<style scoped>
.theme-toggle {
  border: 1px solid var(--color-border);
  border-radius: var(--radius-full);
  background: var(--color-surface);
  color: var(--color-text-primary);
  display: inline-flex;
  align-items: center;
  gap: var(--spacing-2);
  padding: var(--spacing-1) var(--spacing-3);
  transition: background-color var(--transition-fast), color var(--transition-fast), border-color var(--transition-fast);
}

.theme-toggle:hover {
  background: var(--color-primary-50);
  border-color: var(--color-primary-300);
}

.theme-toggle__icon {
  font-size: var(--font-size-sm);
}

.theme-toggle__select {
  border: 0;
  background: transparent;
  color: inherit;
  font-family: inherit;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  cursor: pointer;
  appearance: none;
  color-scheme: light dark;
  padding: var(--spacing-1) var(--spacing-5) var(--spacing-1) 0;
  background-image: url("data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' fill='none' viewBox='0 0 20 20'%3e%3cpath stroke='%236b7280' stroke-linecap='round' stroke-linejoin='round' stroke-width='1.5' d='M6 8l4 4 4-4'/%3e%3c/svg%3e");
  background-position: right 0 center;
  background-repeat: no-repeat;
  background-size: 1.125em;
}

.theme-toggle__select:focus {
  outline: none;
}

.theme-toggle__select option {
  background: var(--color-surface);
  color: var(--color-text-primary);
}

.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  border: 0;
}
</style>
