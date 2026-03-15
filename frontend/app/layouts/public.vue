<script setup lang="ts">
interface Props {
  fullWidth?: boolean
}

withDefaults(defineProps<Props>(), {
  fullWidth: false,
})
</script>

<template>
  <div class="layout-public" :class="{ 'layout-public--full': fullWidth }">
    <header class="layout-public__header">
      <UiThemeToggle />
    </header>
    <main id="main-content" class="layout-public__content" tabindex="-1">
      <slot />
    </main>
  </div>
</template>

<style scoped>
.layout-public {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background: var(--color-background);
}

.layout-public__header {
  display: flex;
  justify-content: flex-end;
  padding: var(--spacing-4) var(--spacing-4) 0;
}

.layout-public--full .layout-public__content {
  flex: 1;
  display: flex;
  flex-direction: column;
}
</style>
