<script setup lang="ts">
defineProps<{
  title: string
  description?: string
  icon?: string
}>()
</script>

<template>
  <div class="empty-state">
    <div v-if="icon" class="empty-state__icon">
      {{ icon }}
    </div>
    <h3 class="empty-state__title">{{ title }}</h3>
    <p v-if="description" class="empty-state__description">{{ description }}</p>
    <div class="empty-state__action">
      <slot name="action" />
    </div>
  </div>
</template>

<style scoped>
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-12) var(--spacing-6);
  text-align: center;
}

.empty-state__icon {
  width: 4rem;
  height: 4rem;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--color-gray-100);
  border-radius: var(--radius-full);
  font-size: var(--font-size-2xl);
  color: var(--color-gray-400);
  margin-bottom: var(--spacing-4);
}

.empty-state__title {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-gray-900);
  margin: 0 0 var(--spacing-2);
}

.empty-state__description {
  font-size: var(--font-size-sm);
  color: var(--color-gray-500);
  margin: 0 0 var(--spacing-4);
  max-width: 400px;
}

.empty-state__action {
  margin-top: var(--spacing-2);
}
</style>
