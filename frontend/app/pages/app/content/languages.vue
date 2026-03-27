<script setup lang="ts">
import LanguageManager from '~/components/projects/LanguageManager.vue'
import UiButton from '~/components/ui/Button.vue'
import UiSelect from '~/components/ui/Select.vue'
import { projectsClient } from '~/api/projectsClient'
import type { Project } from '~/api/types'

definePageMeta({ layout: 'app' })
useSeoMeta({ title: 'Languages - LocFlow' })

const auth = useAuth()
const projects = ref<Array<{ id: string; name: string }>>([])
const selectedProjectId = ref('')

async function loadProjects() {
  if (!auth.organization.value?.id) return
  const data = await projectsClient.list(auth.organization.value.id)
  projects.value = (Array.isArray(data) ? data : []).map((p: Project) => ({ id: p.id, name: p.name }))
  if (!selectedProjectId.value && projects.value.length > 0) {
    selectedProjectId.value = projects.value[0]!.id
  }
}

onMounted(loadProjects)
</script>

<template>
  <div class="languages-page">
    <header class="page-header">
      <div>
        <NuxtLink to="/app/content" class="back-link">
          <svg viewBox="0 0 20 20" fill="currentColor" class="back-icon">
            <path fill-rule="evenodd" d="M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z" clip-rule="evenodd" />
          </svg>
          Back to Content
        </NuxtLink>
        <h1>Languages</h1>
        <p class="page-subtitle">Manage target languages for your project</p>
      </div>
    </header>

    <div class="project-picker">
      <UiSelect
        id="langProjectSelect"
        v-model="selectedProjectId"
        label="Project"
        :options="[
          { value: '', label: 'Select project' },
          ...projects.map(p => ({ value: p.id, label: p.name }))
        ]"
      />
      <p class="label-hint">Select a project to manage its languages</p>
    </div>

    <LanguageManager
      v-if="selectedProjectId"
      :project-id="selectedProjectId"
    />
  </div>
</template>

<style scoped>
.languages-page { max-width: 900px; }
.page-header { margin-bottom: var(--spacing-6); }
.page-header h1 { font-size: var(--font-size-2xl); font-weight: var(--font-weight-semibold); color: var(--color-text-primary); margin: var(--spacing-2) 0 var(--spacing-1) 0; }
.page-subtitle { color: var(--color-text-muted); margin: 0; }
.back-link {
  display: inline-flex;
  align-items: center;
  gap: var(--spacing-1);
  font-size: var(--font-size-sm);
  color: var(--color-primary-600);
  text-decoration: none;
  font-weight: var(--font-weight-medium);
  margin-bottom: var(--spacing-1);
}
.back-link:hover { text-decoration: underline; }
.back-icon { width: 1em; height: 1em; }
.project-picker { margin-bottom: var(--spacing-5); display: flex; flex-direction: column; gap: var(--spacing-2); max-width: 420px; }
.label-hint { font-size: var(--font-size-xs); color: var(--color-text-muted); }
</style>
