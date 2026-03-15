<script setup lang="ts">
import AppEmptyState from '~/components/AppEmptyState.vue'
import AppSkeleton from '~/components/AppSkeleton.vue'
import UiButton from '~/components/ui/Button.vue'

definePageMeta({
  layout: 'app',
})

useSeoMeta({
  title: 'Content - LocFlow',
})

const auth = useAuth()
const isLoading = ref(false)
const contents = ref<Array<{ id: string; title: string; type: string; lastModified: string }>>([])

onMounted(async () => {
  isLoading.value = true
  await new Promise(resolve => setTimeout(resolve, 500))
  contents.value = []
  isLoading.value = false
})
</script>

<template>
  <div class="content-page">
    <header class="page-header">
      <div>
        <h1>Content</h1>
        <p class="page-subtitle">Manage your content for translation</p>
      </div>
      <UiButton>
        <svg viewBox="0 0 20 20" fill="currentColor" class="btn-icon">
          <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
        </svg>
        Add Content
      </UiButton>
    </header>

    <div v-if="isLoading" class="content-list">
      <div v-for="i in 3" :key="i" class="content-item">
        <AppSkeleton lines="2" height="1rem" />
      </div>
    </div>

    <AppEmptyState
      v-else-if="contents.length === 0"
      title="No content yet"
      description="Add content to start translating"
    >
      <template #action>
        <UiButton>Add Content</UiButton>
      </template>
    </AppEmptyState>

    <div v-else class="content-list">
      <div v-for="item in contents" :key="item.id" class="content-item">
        <h3>{{ item.title }}</h3>
        <p>{{ item.type }}</p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.content-page {
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

.btn-icon {
  width: 1.25em;
  height: 1.25em;
  margin-right: var(--spacing-2);
}

.content-list {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-3);
}

.content-item {
  background: var(--color-white);
  border: 1px solid var(--color-gray-200);
  border-radius: var(--radius-lg);
  padding: var(--spacing-4);
}
</style>
