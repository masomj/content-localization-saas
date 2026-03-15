<script setup lang="ts">
import AppEmptyState from '~/components/AppEmptyState.vue'
import AppSkeleton from '~/components/AppSkeleton.vue'
import UiButton from '~/components/ui/Button.vue'

definePageMeta({
  layout: 'app',
})

useSeoMeta({
  title: 'Projects - LocFlow',
})

const auth = useAuth()
const isLoading = ref(false)
const projects = ref<Array<{ id: string; name: string; status: string; progress: number; languages: number }>>([])

onMounted(async () => {
  isLoading.value = true
  await new Promise(resolve => setTimeout(resolve, 500))
  projects.value = []
  isLoading.value = false
})
</script>

<template>
  <div class="projects-page">
    <header class="page-header">
      <div>
        <h1>Projects</h1>
        <p class="page-subtitle">Manage your translation projects</p>
      </div>
      <UiButton>
        <svg viewBox="0 0 20 20" fill="currentColor" class="btn-icon">
          <path fill-rule="evenodd" d="M10 3a1 1 0 011 1v5h5a1 1 0 110 2h-5v5a1 1 0 11-2 0v-5H4a1 1 0 110-2h5V4a1 1 0 011-1z" clip-rule="evenodd" />
        </svg>
        New Project
      </UiButton>
    </header>

    <div v-if="isLoading" class="projects-grid">
      <div v-for="i in 3" :key="i" class="project-card">
        <AppSkeleton lines="3" height="1.5rem" />
      </div>
    </div>

    <AppEmptyState
      v-else-if="projects.length === 0"
      title="No projects yet"
      description="Create your first project to start managing translations"
    >
      <template #action>
        <UiButton>Create Project</UiButton>
      </template>
    </AppEmptyState>

    <div v-else class="projects-grid">
      <div v-for="project in projects" :key="project.id" class="project-card">
        <h3>{{ project.name }}</h3>
        <p>Status: {{ project.status }}</p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.projects-page {
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

.projects-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: var(--spacing-4);
}

.project-card {
  background: var(--color-white);
  border: 1px solid var(--color-gray-200);
  border-radius: var(--radius-lg);
  padding: var(--spacing-4);
}
</style>
