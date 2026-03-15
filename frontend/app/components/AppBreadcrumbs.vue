<script setup lang="ts">
interface BreadcrumbItem {
  label: string
  to?: string
}

defineProps<{
  items: BreadcrumbItem[]
}>()
</script>

<template>
  <nav aria-label="Breadcrumb" class="breadcrumbs">
    <ol class="breadcrumbs__list">
      <li v-for="(item, index) in items" :key="index" class="breadcrumbs__item">
        <template v-if="item.to">
          <NuxtLink :to="item.to" class="breadcrumbs__link">
            {{ item.label }}
          </NuxtLink>
        </template>
        <template v-else>
          <span class="breadcrumbs__current">{{ item.label }}</span>
        </template>
        <svg
          v-if="index < items.length - 1"
          class="breadcrumbs__separator"
          viewBox="0 0 20 20"
          fill="currentColor"
        >
          <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd" />
        </svg>
      </li>
    </ol>
  </nav>
</template>

<style scoped>
.breadcrumbs {
  display: flex;
  align-items: center;
}

.breadcrumbs__list {
  display: flex;
  align-items: center;
  gap: var(--spacing-1);
  list-style: none;
  margin: 0;
  padding: 0;
}

.breadcrumbs__item {
  display: flex;
  align-items: center;
  gap: var(--spacing-1);
}

.breadcrumbs__link {
  color: var(--color-gray-500);
  text-decoration: none;
  font-size: var(--font-size-sm);
  transition: color var(--transition-fast);
}

.breadcrumbs__link:hover {
  color: var(--color-primary-600);
}

.breadcrumbs__current {
  color: var(--color-gray-900);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
}

.breadcrumbs__separator {
  width: 1rem;
  height: 1rem;
  color: var(--color-gray-400);
  flex-shrink: 0;
}
</style>
