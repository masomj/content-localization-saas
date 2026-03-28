<script setup lang="ts">
import AppEmptyState from '~/components/AppEmptyState.vue'
import AppSkeleton from '~/components/AppSkeleton.vue'
import UiButton from '~/components/ui/Button.vue'

definePageMeta({
  layout: 'app',
})

useSeoMeta({
  title: 'Integrations - InterCopy',
})

const auth = useAuth()
const isLoading = ref(false)
const integrations = ref<Array<{ id: string; name: string; status: string; description: string }>>([])

const availableIntegrations = [
  { id: '1', name: 'GitHub', description: 'Connect your repositories for automatic content sync', icon: '🐙' },
  { id: '2', name: 'Contentful', description: 'Import and export content from Contentful', icon: '📦' },
  { id: '3', name: 'Strapi', description: 'Integrate with your Strapi CMS', icon: '🔧' },
  { id: '4', name: 'Slack', description: 'Get notifications in your Slack workspace', icon: '💬' },
]

onMounted(async () => {
  isLoading.value = true
  await new Promise(resolve => setTimeout(resolve, 500))
  isLoading.value = false
})
</script>

<template>
  <div class="integrations-page">
    <header class="page-header">
      <div>
        <h1>Integrations</h1>
        <p class="page-subtitle">Connect your favorite tools</p>
      </div>
    </header>

    <div v-if="isLoading" class="integrations-grid">
      <div v-for="i in 4" :key="i" class="integration-card">
        <AppSkeleton lines="2" height="1.5rem" />
      </div>
    </div>

    <div v-else class="integrations-grid">
      <div
        v-for="integration in availableIntegrations"
        :key="integration.id"
        class="integration-card"
      >
        <div class="integration-icon">{{ integration.icon }}</div>
        <h3>{{ integration.name }}</h3>
        <p>{{ integration.description }}</p>
        <UiButton variant="secondary" size="sm">Connect</UiButton>
      </div>
    </div>

    <AppEmptyState
      v-if="!isLoading && integrations.length > 0"
      title="No active integrations"
      description="Connect integrations to automate your workflow"
    />
  </div>
</template>

<style scoped>
.integrations-page {
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
  color: var(--color-text-primary);
  margin: 0 0 var(--spacing-1) 0;
}

.page-subtitle {
  color: var(--color-text-muted);
  margin: 0;
}

.integrations-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: var(--spacing-4);
}

.integration-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--spacing-5);
  display: flex;
  flex-direction: column;
  gap: var(--spacing-3);
}

.integration-icon {
  font-size: var(--font-size-2xl);
  width: 3rem;
  height: 3rem;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--color-surface);
  border-radius: var(--radius-lg);
}

.integration-card h3 {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  margin: 0;
}

.integration-card p {
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
  margin: 0;
  flex: 1;
}
</style>

