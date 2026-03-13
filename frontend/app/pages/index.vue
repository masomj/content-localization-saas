<script setup lang="ts">
const apiBase = 'http://localhost:5177'

type Workspace = { id: string; name: string }
type Project = { id: string; workspaceId: string; name: string; sourceLanguage: string }

const workspaces = ref<Workspace[]>([])
const projects = ref<Project[]>([])
const workspaceForm = reactive({ name: '' })
const projectForm = reactive({ workspaceId: '', name: '', sourceLanguage: '' })
const errors = reactive<Record<string, string>>({
  workspaceName: '',
  projectWorkspace: '',
  projectName: '',
  projectLanguage: '',
})
const apiWarning = ref('')

function validateWorkspace() {
  errors.workspaceName = workspaceForm.name.trim() ? '' : 'Workspace name is required'
  return !errors.workspaceName
}

function validateProject() {
  errors.projectWorkspace = projectForm.workspaceId ? '' : 'Workspace is required'
  errors.projectName = projectForm.name.trim() ? '' : 'Project name is required'
  errors.projectLanguage = projectForm.sourceLanguage.trim() ? '' : 'Source language is required'
  return !errors.projectWorkspace && !errors.projectName && !errors.projectLanguage
}

async function loadData() {
  try {
    workspaces.value = await $fetch(`${apiBase}/api/workspaces`)
    projects.value = await $fetch(`${apiBase}/api/projects`)
    apiWarning.value = ''
  } catch {
    apiWarning.value = 'API is not reachable yet. Start the backend to persist data.'
  }
}

async function createWorkspace() {
  if (!validateWorkspace()) return
  await $fetch(`${apiBase}/api/workspaces`, { method: 'POST', body: { name: workspaceForm.name } })
  workspaceForm.name = ''
  await loadData()
}

async function createProject() {
  if (!validateProject()) return
  await $fetch(`${apiBase}/api/projects`, { method: 'POST', body: projectForm })
  projectForm.name = ''
  projectForm.sourceLanguage = ''
  await loadData()
}

onMounted(loadData)
</script>

<template>
  <section class="grid" aria-label="MVP vertical slice">
    <article class="card">
      <h2>Create workspace</h2>
      <p v-if="apiWarning" class="error">{{ apiWarning }}</p>
      <form @submit.prevent="createWorkspace" novalidate>
        <label for="workspace-name">Workspace name</label>
        <input id="workspace-name" v-model="workspaceForm.name" aria-describedby="workspace-name-error" @blur="validateWorkspace" />
        <p id="workspace-name-error" class="error" v-if="errors.workspaceName">{{ errors.workspaceName }}</p>
        <button type="submit">Create workspace</button>
      </form>
      <h3>Workspaces</h3>
      <ul>
        <li v-for="w in workspaces" :key="w.id">{{ w.name }}</li>
      </ul>
    </article>

    <article class="card">
      <h2>Create project</h2>
      <form @submit.prevent="createProject" novalidate>
        <label for="project-workspace">Workspace</label>
        <select id="project-workspace" v-model="projectForm.workspaceId" aria-describedby="project-workspace-error" @blur="validateProject">
          <option value="">Select workspace</option>
          <option v-for="w in workspaces" :key="w.id" :value="w.id">{{ w.name }}</option>
        </select>
        <p id="project-workspace-error" class="error" v-if="errors.projectWorkspace">{{ errors.projectWorkspace }}</p>

        <label for="project-name">Project name</label>
        <input id="project-name" v-model="projectForm.name" aria-describedby="project-name-error" @blur="validateProject" />
        <p id="project-name-error" class="error" v-if="errors.projectName">{{ errors.projectName }}</p>

        <label for="source-language">Source language</label>
        <input id="source-language" v-model="projectForm.sourceLanguage" placeholder="e.g. en, fr-CA" aria-describedby="source-language-error" @blur="validateProject" />
        <p id="source-language-error" class="error" v-if="errors.projectLanguage">{{ errors.projectLanguage }}</p>

        <button type="submit">Create project</button>
      </form>

      <h3>Projects</h3>
      <ul>
        <li v-for="p in projects" :key="p.id">{{ p.name }} ({{ p.sourceLanguage }})</li>
      </ul>
    </article>
  </section>
</template>
