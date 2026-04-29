<script setup lang="ts">
interface Props {
  padding?: 'none' | 'sm' | 'md' | 'lg'
  elevated?: boolean
}

withDefaults(defineProps<Props>(), {
  padding: 'md',
  elevated: false,
})
</script>

<template>
  <div :class="['ui-card', `ui-card--padding-${padding}`, { 'ui-card--elevated': elevated }]">
    <slot />
  </div>
</template>

<style scoped>
.ui-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-xl);
}

.ui-card--padding-none {
  padding: 0;
}

.ui-card--padding-sm {
  padding: var(--spacing-4);
}

.ui-card--padding-md {
  padding: var(--spacing-6);
}

.ui-card--padding-lg {
  padding: var(--spacing-8);
}

.ui-card--elevated {
  box-shadow: var(--shadow-md);
}
</style>
