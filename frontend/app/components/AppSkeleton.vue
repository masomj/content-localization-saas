<script setup lang="ts">
defineProps<{
  lines?: number
  height?: string
}>()
</script>

<template>
  <div class="skeleton" :style="{ height: height || '1rem' }">
    <div v-if="lines && lines > 1" class="skeleton__lines">
      <div v-for="i in lines" :key="i" class="skeleton__line" :style="{ width: i === lines ? '60%' : '100%' }"></div>
    </div>
  </div>
</template>

<style scoped>
.skeleton {
  background: linear-gradient(
    90deg,
    var(--color-skeleton-base) 25%,
    var(--color-skeleton-highlight) 50%,
    var(--color-skeleton-base) 75%
  );
  background-size: 200% 100%;
  animation: shimmer 1.5s infinite;
  border-radius: var(--radius-md);
}

.skeleton__lines {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-2);
}

.skeleton__line {
  height: 1rem;
  background: linear-gradient(
    90deg,
    var(--color-skeleton-base) 25%,
    var(--color-skeleton-highlight) 50%,
    var(--color-skeleton-base) 75%
  );
  background-size: 200% 100%;
  animation: shimmer 1.5s infinite;
  border-radius: var(--radius-md);
}

@keyframes shimmer {
  0% {
    background-position: 200% 0;
  }
  100% {
    background-position: -200% 0;
  }
}
</style>
