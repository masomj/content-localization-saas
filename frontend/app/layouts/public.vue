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
    <slot />
  </div>
</template>

<style scoped>
.layout-public {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background: var(--color-background);
}

.layout-public--full .layout-public__content {
  flex: 1;
  display: flex;
  flex-direction: column;
}
</style>
