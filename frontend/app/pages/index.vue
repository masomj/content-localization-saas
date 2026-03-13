<script setup lang="ts">
const apiBase = 'http://localhost:5177'

type Workspace = { id: string; name: string }
type Project = { id: string; workspaceId: string; name: string; sourceLanguage: string; description: string }
type ProjectAuditLog = { id: string; projectId: string; action: string; details: string; createdUtc: string }

const workspaces = ref<Workspace[]>([])
const projects = ref<Project[]>([])
const projectAuditLogs = ref<ProjectAuditLog[]>([])

const workspaceForm = reactive({ name: '' })
const projectForm = reactive({ workspaceId: '', name: '', sourceLanguage: '', description: '' })
const projectSettingsForm = reactive({ id: '', name: '', sourceLanguage: '', description: '' })

const errors = reactive<Record<string, string>>({
  workspaceName: '',
  projectWorkspace: '',
  projectName: '',
  projectLanguage: '',
  projectDescription: '',
  settingsName: '',
  settingsLanguage: '',
  settingsDescription: '',
})

const apiWarning = ref('')
const settingsMessage = ref('')

const hasAnyProject = computed(() => projects.value.length > 0)
const selectedProject = computed(() => projects.value.find(x => x.id === projectSettingsForm.id) ?? null)
const selectedProjectHasContent = computed(() => false)

function validateWorkspace() {
  errors.workspaceName = workspaceForm.name.trim() ? '' : 'Workspace name is required'
  return !errors.workspaceName
}

function validateProject() {
  errors.projectWorkspace = projectForm.workspaceId ? '' : 'Workspace is required'
  errors.projectName = projectForm.name.trim() ? '' : 'Project name is required'
  errors.projectLanguage = projectForm.sourceLanguage.trim() ? '' : 'Source language is required'
  errors.projectDescription = projectForm.description.length <= 2000 ? '' : 'Description must be 2000 characters or less'
  return !errors.projectWorkspace && !errors.projectName && !errors.projectLanguage && !errors.projectDescription
}

function validateProjectSettings() {
  errors.settingsName = projectSettingsForm.name.trim() ? '' : 'Project name is required'
  errors.settingsLanguage = projectSettingsForm.sourceLanguage.trim() ? '' : 'Source language is required'
  errors.settingsDescription = projectSettingsForm.description.length <= 2000 ? '' : 'Description must be 2000 characters or less'
  return !errors.settingsName && !errors.settingsLanguage && !errors.settingsDescription
}

async function loadData() {
  try {
    workspaces.value = await $fetch(`${apiBase}/api/workspaces`)
    projects.value = await $fetch(`${apiBase}/api/projects`)
    apiWarning.value = ''

    if (!projectSettingsForm.id && projects.value.length > 0) {
      selectProject(projects.value[0])
    }

    if (projectSettingsForm.id) {
      const next = projects.value.find(x => x.id === projectSettingsForm.id)
      if (next) selectProject(next)
    }
  } catch {
    apiWarning.value = 'API is not reachable yet. Start the backend to persist data.'
  }
}

function selectProject(project: Project) {
  projectSettingsForm.id = project.id
  projectSettingsForm.name = project.name
  projectSettingsForm.sourceLanguage = project.sourceLanguage
  projectSettingsForm.description = project.description ?? ''
  void loadAuditLogs(project.id)
}

async function loadAuditLogs(projectId: string) {
  projectAuditLogs.value = await $fetch(`${apiBase}/api/projects/${projectId}/audit-logs`)
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
  projectForm.description = ''
  await loadData()
}

async function saveProjectSettings() {
  if (!projectSettingsForm.id) return
  if (!validateProjectSettings()) return

  await $fetch(`${apiBase}/api/projects/${projectSettingsForm.id}`, {
    method: 'PUT',
    body: {
      name: projectSettingsForm.name,
      sourceLanguage: projectSettingsForm.sourceLanguage,
      description: projectSettingsForm.description,
    },
  })

  settingsMessage.value = 'Project settings saved.'
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

        <label for="project-description">Description</label>
        <textarea id="project-description" v-model="projectForm.description" aria-describedby="project-description-error" @blur="validateProject" />
        <p id="project-description-error" class="error" v-if="errors.projectDescription">{{ errors.projectDescription }}</p>

        <button type="submit">Create project</button>
      </form>

      <h3>Projects</h3>
      <ul>
        <li v-for="p in projects" :key="p.id">
          <button type="button" @click="selectProject(p)">{{ p.name }} ({{ p.sourceLanguage }})</button>
        </li>
      </ul>

      <section v-if="!hasAnyProject" aria-label="Project onboarding empty state">
        <h3>No content yet</h3>
        <p>Setup complete. Start by adding your first content item and inviting collaborators.</p>
        <ul>
          <li>Create your first content item</li>
          <li>Define reusable copy components</li>
          <li>Invite translators and reviewers</li>
        </ul>
      </section>
    </article>
  </section>

  <section class="card" v-if="selectedProject">
    <h2>Project settings</h2>
    <p v-if="settingsMessage">{{ settingsMessage }}</p>
    <form @submit.prevent="saveProjectSettings" novalidate>
      <label for="settings-name">Project name</label>
      <input id="settings-name" v-model="projectSettingsForm.name" aria-describedby="settings-name-error" @blur="validateProjectSettings" />
      <p id="settings-name-error" class="error" v-if="errors.settingsName">{{ errors.settingsName }}</p>

      <label for="settings-language">Source language</label>
      <input id="settings-language" v-model="projectSettingsForm.sourceLanguage" aria-describedby="settings-language-error" @blur="validateProjectSettings" />
      <p id="settings-language-error" class="error" v-if="errors.settingsLanguage">{{ errors.settingsLanguage }}</p>

      <label for="settings-description">Description</label>
      <textarea id="settings-description" v-model="projectSettingsForm.description" aria-describedby="settings-description-error" @blur="validateProjectSettings" />
      <p id="settings-description-error" class="error" v-if="errors.settingsDescription">{{ errors.settingsDescription }}</p>

      <button type="submit">Save project settings</button>
    </form>

    <h3>Audit log</h3>
    <ul>
      <li v-for="log in projectAuditLogs" :key="log.id">{{ log.action }} · {{ log.createdUtc }}</li>
    </ul>
    <p v-if="projectAuditLogs.length === 0">No changes recorded yet.</p>
    <p v-if="!selectedProjectHasContent">This project has no content yet.</p>
  </section>
</template>
