<script setup lang="ts">
const theme = useTheme()

const label = computed(() => {
  if (theme.preference.value === 'dark') {
    return 'Dark mode'
  }

  if (theme.preference.value === 'light') {
    return 'Light mode'
  }

  return 'System mode'
})

const nextModeHint = computed(() => {
  if (theme.preference.value === 'light') {
    return 'Switch to dark mode'
  }

  if (theme.preference.value === 'dark') {
    return 'Switch to system mode'
  }

  return 'Switch to light mode'
})
</script>

<template>
  <button
    type="button"
    class="theme-toggle"
    :aria-label="nextModeHint"
    :title="nextModeHint"
    data-testid="theme-toggle"
    @click="theme.cycleThemePreference()"
  >
    <span class="theme-toggle__icon" aria-hidden="true">
      <template v-if="theme.preference.value === 'light'">☀️</template>
      <template v-else-if="theme.preference.value === 'dark'">🌙</template>
      <template v-else>🖥️</template>
    </span>
    <span class="theme-toggle__text">{{ label }}</span>
  </button>
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
  padding: var(--spacing-2) var(--spacing-3);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  line-height: 1;
  cursor: pointer;
  transition: background-color var(--transition-fast), color var(--transition-fast), border-color var(--transition-fast);
}

.theme-toggle:hover {
  background: var(--color-primary-50);
  border-color: var(--color-primary-300);
}

.theme-toggle__icon {
  font-size: var(--font-size-base);
}

.theme-toggle__text {
  white-space: nowrap;
}
</style>
