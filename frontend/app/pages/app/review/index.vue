<script setup lang="ts">
import AppEmptyState from '~/components/AppEmptyState.vue'
import AppSkeleton from '~/components/AppSkeleton.vue'
import UiButton from '~/components/ui/Button.vue'

definePageMeta({
  layout: 'app',
})

useSeoMeta({
  title: 'Review - LocFlow',
})

const auth = useAuth()
const isLoading = ref(false)
const reviews = ref<Array<{ id: string; title: string; status: string; pendingReview: number }>>([])

onMounted(async () => {
  isLoading.value = true
  await new Promise(resolve => setTimeout(resolve, 500))
  reviews.value = []
  isLoading.value = false
})
</script>

<template>
  <div class="review-page">
    <header class="page-header">
      <div>
        <h1>Review</h1>
        <p class="page-subtitle">Review and approve translations</p>
      </div>
    </header>

    <div v-if="isLoading" class="review-list">
      <div v-for="i in 3" :key="i" class="review-item">
        <AppSkeleton lines="2" height="1rem" />
      </div>
    </div>

    <AppEmptyState
      v-else-if="reviews.length === 0"
      title="No translations pending review"
      description="Translations that need review will appear here"
    >
      <template #action>
        <UiButton>View All Translations</UiButton>
      </template>
    </AppEmptyState>

    <div v-else class="review-list">
      <div v-for="item in reviews" :key="item.id" class="review-item">
        <h3>{{ item.title }}</h3>
        <p>{{ item.pendingReview }} pending</p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.review-page {
  max-width: 1200px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: var(--spacing-6);
}

.page-header h1 {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-semibold);
  color: var(--color-gray-900);
  margin: 0 0 var(--spacing-1) 0;
}

.page-subtitle {
  color: var(--color-gray-500);
  margin: 0;
}

.review-list {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-3);
}

.review-item {
  background: var(--color-white);
  border: 1px solid var(--color-gray-200);
  border-radius: var(--radius-lg);
  padding: var(--spacing-4);
}
</style>
