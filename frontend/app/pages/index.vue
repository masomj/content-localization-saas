<script setup lang="ts">
import MembershipAuditPanel from '../components/MembershipAuditPanel.vue'

const apiBase = 'http://localhost:5177'

type Workspace = { id: string; name: string }
type Project = { id: string; workspaceId: string; name: string; sourceLanguage: string; description: string }
type ProjectAuditLog = { id: string; projectId: string; action: string; details: string; createdUtc: string }
type PermissionState = { role: string; canRead: boolean; canWrite: boolean; canReview: boolean; canAdmin: boolean }

const workspaces = ref<Workspace[]>([])
const projects = ref<Project[]>([])
const projectAuditLogs = ref<ProjectAuditLog[]>([])
const currentRole = ref<'Viewer' | 'Editor' | 'Reviewer' | 'Admin'>('Admin')
const permissions = ref<PermissionState>({ role: 'Admin', canRead: true, canWrite: true, canReview: true, canAdmin: true })

const workspaceForm = reactive({ name: '' })
const projectForm = reactive({ workspaceId: '', name: '', sourceLanguage: '', description: '' })
const projectSettingsForm = reactive({ id: '', name: '', sourceLanguage: '', description: '' })
const contentItemForm = reactive({ projectId: '', key: '', source: '', status: 'draft', tags: '', context: '', notes: '' })
const copyComponentForm = reactive({ projectId: '', name: '', source: '' })
const copyComponents = ref<any[]>([])
const contentItems = ref<any[]>([])
const usageReferences = ref<any[]>([])
const usageFilters = reactive({ projectId: '', screen: '', component: '' })
const selectedContentItemId = ref('')
const selectedContentItemIds = ref<string[]>([])
const contentRevisions = ref<any[]>([])
const revisionCompare = ref<any>(null)
const compareLeftRevisionId = ref('')
const compareRightRevisionId = ref('')
const contentSearch = ref('')
const contentError = ref('')
const bulkStatus = ref('review')
const savedFilterPresets = ref<any[]>([])
const newPresetName = ref('')
const projectLanguages = ref<any[]>([])
const languageForm = reactive({ projectId: '', bcp47Code: '', isSource: false })
const languageTaskForm = reactive({ contentItemId: '', languageCode: '', assigneeEmail: '', dueUtc: '', status: 'todo', translationText: '' })
const languageTasks = ref<any[]>([])
const translationSuggestion = ref<any>(null)
const localizationGrid = ref<any[]>([])
const localizationGridMeta = reactive({ total: 0, page: 1, pageSize: 10 })
const localizationFilters = reactive({ stateFilter: '', sortBy: 'itemKey', desc: false })
const discussionThreads = ref<any[]>([])
const discussionComments = ref<any[]>([])
const selectedThreadId = ref('')
const newThreadForm = reactive({ contentItemId: '', title: '', body: '' })
const replyBody = ref('')
const notificationsUserEmail = ref('member@example.com')
const notifications = ref<any[]>([])
const notificationPrefs = reactive({ inAppEnabled: true, emailEnabled: false, slackEnabled: false })

const errors = reactive<Record<string, string>>({
  workspaceName: '',
  projectWorkspace: '',
  projectName: '',
  projectLanguage: '',
  projectDescription: '',
  settingsName: '',
  settingsLanguage: '',
  settingsDescription: '',
  contentKey: '',
  contentSource: '',
  contentStatus: '',
})

const apiWarning = ref('')
const settingsMessage = ref('')

const hasAnyProject = computed(() => projects.value.length > 0)
const selectedProject = computed(() => projects.value.find(x => x.id === projectSettingsForm.id) ?? null)
const selectedProjectHasContent = computed(() => false)
const canWrite = computed(() => permissions.value.canWrite)
const canAdmin = computed(() => permissions.value.canAdmin)

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

function authHeaders() {
  return { 'X-User-Role': currentRole.value }
}

function localPermissionsFor(role: string): PermissionState {
  if (role === 'Admin') return { role, canRead: true, canWrite: true, canReview: true, canAdmin: true }
  if (role === 'Reviewer') return { role, canRead: true, canWrite: false, canReview: true, canAdmin: false }
  if (role === 'Editor') return { role, canRead: true, canWrite: true, canReview: false, canAdmin: false }
  return { role, canRead: true, canWrite: false, canReview: false, canAdmin: false }
}

async function loadPermissions() {
  try {
    permissions.value = await $fetch(`${apiBase}/api/permissions/me`, { headers: authHeaders() })
  } catch {
    permissions.value = localPermissionsFor(currentRole.value)
  }
}

async function loadContentItems() {
  const params = new URLSearchParams()
  if (contentItemForm.projectId) params.set('projectId', contentItemForm.projectId)
  if (contentSearch.value.trim()) params.set('search', contentSearch.value.trim())
  const qs = params.toString()
  contentItems.value = await $fetch(`${apiBase}/api/content-items${qs ? `?${qs}` : ''}`, { headers: authHeaders() })
}

async function loadCopyComponents() {
  const params = new URLSearchParams()
  if (copyComponentForm.projectId) params.set('projectId', copyComponentForm.projectId)
  const qs = params.toString()
  copyComponents.value = await $fetch(`${apiBase}/api/copy-components${qs ? `?${qs}` : ''}`, { headers: authHeaders() })
}

async function loadUsageReferences() {
  const params = new URLSearchParams()
  if (selectedContentItemId.value) params.set('contentItemId', selectedContentItemId.value)
  if (usageFilters.projectId) params.set('projectId', usageFilters.projectId)
  if (usageFilters.screen.trim()) params.set('screen', usageFilters.screen.trim())
  if (usageFilters.component.trim()) params.set('component', usageFilters.component.trim())

  const qs = params.toString()
  usageReferences.value = await $fetch(`${apiBase}/api/usage-references${qs ? `?${qs}` : ''}`, { headers: authHeaders() })
}

async function loadSavedFilterPresets() {
  const params = new URLSearchParams()
  if (contentItemForm.projectId) params.set('projectId', contentItemForm.projectId)
  const qs = params.toString()
  savedFilterPresets.value = await $fetch(`${apiBase}/api/content-items/filter-presets${qs ? `?${qs}` : ''}`, { headers: authHeaders() })
}

async function loadProjectLanguages() {
  if (!languageForm.projectId) {
    projectLanguages.value = []
    return
  }
  projectLanguages.value = await $fetch(`${apiBase}/api/project-languages?projectId=${languageForm.projectId}`, { headers: authHeaders() })
}

async function loadLanguageTasks() {
  const params = new URLSearchParams()
  if (languageTaskForm.contentItemId) params.set('contentItemId', languageTaskForm.contentItemId)
  languageTasks.value = await $fetch(`${apiBase}/api/language-tasks?${params.toString()}`, { headers: authHeaders() })
}

async function loadLocalizationGrid() {
  const params = new URLSearchParams()
  if (languageForm.projectId) params.set('projectId', languageForm.projectId)
  if (localizationFilters.stateFilter) params.set('stateFilter', localizationFilters.stateFilter)
  if (localizationFilters.sortBy) params.set('sortBy', localizationFilters.sortBy)
  params.set('desc', String(localizationFilters.desc))
  params.set('page', String(localizationGridMeta.page))
  params.set('pageSize', String(localizationGridMeta.pageSize))

  const response: any = await $fetch(`${apiBase}/api/localization-grid?${params.toString()}`, { headers: authHeaders() })
  localizationGridMeta.total = response.total
  localizationGridMeta.page = response.page
  localizationGridMeta.pageSize = response.pageSize
  localizationGrid.value = response.rows
}

async function loadDiscussionThreads() {
  if (!newThreadForm.contentItemId) {
    discussionThreads.value = []
    discussionComments.value = []
    return
  }

  discussionThreads.value = await $fetch(`${apiBase}/api/discussions/threads?contentItemId=${newThreadForm.contentItemId}`, { headers: authHeaders() })
}

async function loadDiscussionComments(threadId: string) {
  selectedThreadId.value = threadId
  discussionComments.value = await $fetch(`${apiBase}/api/discussions/threads/${threadId}/comments`, { headers: authHeaders() })
}

async function loadNotifications() {
  notifications.value = await $fetch(`${apiBase}/api/notifications?userEmail=${encodeURIComponent(notificationsUserEmail.value)}`, { headers: authHeaders() })
}

async function setNotificationPreferences() {
  await $fetch(`${apiBase}/api/notifications/preferences`, {
    method: 'POST',
    headers: authHeaders(),
    body: {
      userEmail: notificationsUserEmail.value,
      inAppEnabled: notificationPrefs.inAppEnabled,
      emailEnabled: notificationPrefs.emailEnabled,
      slackEnabled: notificationPrefs.slackEnabled,
    },
  })
}

async function markNotificationRead(notificationId: string, isRead: boolean) {
  await $fetch(`${apiBase}/api/notifications/mark`, {
    method: 'POST',
    headers: authHeaders(),
    body: { notificationId, isRead },
  })

  await loadNotifications()
}

async function loadData() {
  try {
    await loadPermissions()
    workspaces.value = await $fetch(`${apiBase}/api/workspaces`, { headers: authHeaders() })
    projects.value = await $fetch(`${apiBase}/api/projects`, { headers: authHeaders() })
    apiWarning.value = ''

    if (!projectSettingsForm.id && projects.value.length > 0) {
      selectProject(projects.value[0])
      contentItemForm.projectId = projects.value[0].id
      copyComponentForm.projectId = projects.value[0].id
      usageFilters.projectId = projects.value[0].id
      languageForm.projectId = projects.value[0].id
      newThreadForm.contentItemId = ''
    }

    if (projectSettingsForm.id) {
      const next = projects.value.find(x => x.id === projectSettingsForm.id)
      if (next) selectProject(next)
    }

    await loadContentItems()
    await loadCopyComponents()
    await loadUsageReferences()
    await loadSavedFilterPresets()
    await loadProjectLanguages()
    await loadLanguageTasks()
    await loadLocalizationGrid()
    await loadDiscussionThreads()
    await loadNotifications()
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
  if (!canAdmin.value) {
    projectAuditLogs.value = []
    return
  }

  projectAuditLogs.value = await $fetch(`${apiBase}/api/projects/${projectId}/audit-logs`, { headers: authHeaders() })
}

async function createWorkspace() {
  if (!validateWorkspace()) return
  await $fetch(`${apiBase}/api/workspaces`, { method: 'POST', body: { name: workspaceForm.name }, headers: authHeaders() })
  workspaceForm.name = ''
  await loadData()
}

async function createProject() {
  if (!validateProject()) return
  await $fetch(`${apiBase}/api/projects`, { method: 'POST', body: projectForm, headers: authHeaders() })
  projectForm.name = ''
  projectForm.sourceLanguage = ''
  projectForm.description = ''
  await loadData()
}

function validateContentItem() {
  errors.contentKey = contentItemForm.key.trim() ? '' : 'Key is required'
  errors.contentSource = contentItemForm.source.trim() ? '' : 'Source is required'
  errors.contentStatus = contentItemForm.status.trim() ? '' : 'Status is required'
  return !errors.contentKey && !errors.contentSource && !errors.contentStatus
}

async function createContentItem() {
  if (!validateContentItem()) return
  contentError.value = ''

  try {
    await $fetch(`${apiBase}/api/content-items`, {
      method: 'POST',
      headers: authHeaders(),
      body: {
        projectId: contentItemForm.projectId,
        key: contentItemForm.key,
        source: contentItemForm.source,
        status: contentItemForm.status,
        tags: contentItemForm.tags.split(',').map(x => x.trim()).filter(Boolean),
        context: contentItemForm.context,
        notes: contentItemForm.notes,
      },
    })

    contentItemForm.key = ''
    contentItemForm.source = ''
    contentItemForm.tags = ''
    contentItemForm.context = ''
    contentItemForm.notes = ''

    await loadContentItems()
  } catch (e: any) {
    contentError.value = e?.data?.errors?.Key?.[0] ?? e?.data?.title ?? 'Could not create content item.'
  }
}

async function createCopyComponent() {
  if (!copyComponentForm.projectId || !copyComponentForm.name.trim() || !copyComponentForm.source.trim()) return

  await $fetch(`${apiBase}/api/copy-components`, {
    method: 'POST',
    headers: authHeaders(),
    body: {
      projectId: copyComponentForm.projectId,
      name: copyComponentForm.name,
      source: copyComponentForm.source,
    },
  })

  copyComponentForm.name = ''
  copyComponentForm.source = ''
  await loadCopyComponents()
}

async function propagateCopyComponent(componentId: string, source: string) {
  await $fetch(`${apiBase}/api/copy-components/${componentId}`, {
    method: 'PUT',
    headers: authHeaders(),
    body: { source },
  })

  await loadCopyComponents()
  await loadContentItems()
}

async function deleteCopyComponent(componentId: string) {
  await $fetch(`${apiBase}/api/copy-components/${componentId}`, {
    method: 'DELETE',
    headers: authHeaders(),
  })

  await loadCopyComponents()
}

async function linkContentItemToComponent(contentItemId: string, componentId: string) {
  await $fetch(`${apiBase}/api/copy-components/${componentId}/link`, {
    method: 'POST',
    headers: authHeaders(),
    body: { contentItemId },
  })

  await loadContentItems()
}

async function addUsageReference(contentItemId: string) {
  const item = contentItems.value.find(x => x.id === contentItemId)
  if (!item) return

  await $fetch(`${apiBase}/api/usage-references`, {
    method: 'POST',
    headers: authHeaders(),
    body: {
      contentItemId,
      projectId: item.projectId,
      screen: 'MainScreen',
      component: 'TextBlock',
      referencePath: 'ui.main.textblock',
    },
  })

  selectedContentItemId.value = contentItemId
  await loadUsageReferences()
}

async function loadContentRevisions(contentItemId: string) {
  selectedContentItemId.value = contentItemId
  contentRevisions.value = await $fetch(`${apiBase}/api/content-items/${contentItemId}/revisions`, { headers: authHeaders() })
}

async function compareSelectedRevisions() {
  if (!selectedContentItemId.value || !compareLeftRevisionId.value || !compareRightRevisionId.value) return
  revisionCompare.value = await $fetch(
    `${apiBase}/api/content-items/${selectedContentItemId.value}/revisions/compare?left=${compareLeftRevisionId.value}&right=${compareRightRevisionId.value}`,
    { headers: authHeaders() }
  )
}

async function rollbackRevision(revisionId: string) {
  if (!selectedContentItemId.value) return
  await $fetch(`${apiBase}/api/content-items/${selectedContentItemId.value}/revisions/${revisionId}/rollback`, {
    method: 'POST',
    headers: { ...authHeaders(), 'X-Actor-Email': 'admin@example.com' },
  })

  await loadContentItems()
  await loadContentRevisions(selectedContentItemId.value)
}

async function updateContentItem(item: any) {
  await $fetch(`${apiBase}/api/content-items/${item.id}`, {
    method: 'PUT',
    headers: { ...authHeaders(), 'X-Actor-Email': 'editor@example.com' },
    body: { source: item.source, status: item.status },
  })

  await loadContentItems()
  await loadContentRevisions(item.id)
}

function toggleContentItemSelection(id: string, checked: boolean) {
  if (checked) {
    if (!selectedContentItemIds.value.includes(id)) selectedContentItemIds.value.push(id)
  } else {
    selectedContentItemIds.value = selectedContentItemIds.value.filter(x => x !== id)
  }
}

async function bulkUpdateSelectedStatus() {
  if (selectedContentItemIds.value.length === 0) return

  await $fetch(`${apiBase}/api/content-items/bulk/status`, {
    method: 'POST',
    headers: authHeaders(),
    body: {
      itemIds: selectedContentItemIds.value,
      status: bulkStatus.value,
    },
  })

  await loadContentItems()
}

async function saveCurrentFilterPreset() {
  if (!contentItemForm.projectId || !newPresetName.value.trim()) return

  await $fetch(`${apiBase}/api/content-items/filter-presets`, {
    method: 'POST',
    headers: authHeaders(),
    body: {
      projectId: contentItemForm.projectId,
      name: newPresetName.value,
      query: contentSearch.value,
      tags: '',
      status: '',
    },
  })

  newPresetName.value = ''
  await loadSavedFilterPresets()
}

function applyFilterPreset(preset: any) {
  contentSearch.value = preset.query ?? ''
  void loadContentItems()
}

async function addProjectLanguage() {
  if (!languageForm.projectId || !languageForm.bcp47Code.trim()) return
  await $fetch(`${apiBase}/api/project-languages`, {
    method: 'POST',
    headers: authHeaders(),
    body: {
      projectId: languageForm.projectId,
      bcp47Code: languageForm.bcp47Code,
      isSource: languageForm.isSource,
    },
  })
  languageForm.bcp47Code = ''
  languageForm.isSource = false
  await loadProjectLanguages()
}

async function toggleLanguageActive(id: string, isActive: boolean) {
  await $fetch(`${apiBase}/api/project-languages/${id}/active`, {
    method: 'PUT',
    headers: authHeaders(),
    body: { isActive },
  })
  await loadProjectLanguages()
}

async function changeDefaultSourceLanguage(code: string) {
  if (!languageForm.projectId) return
  await $fetch(`${apiBase}/api/project-languages/source-language?projectId=${languageForm.projectId}`, {
    method: 'POST',
    headers: authHeaders(),
    body: { bcp47Code: code },
  })
  await loadProjectLanguages()
}

async function upsertLanguageTask() {
  if (!languageTaskForm.contentItemId || !languageTaskForm.languageCode.trim() || !languageTaskForm.status.trim()) return

  await $fetch(`${apiBase}/api/language-tasks`, {
    method: 'POST',
    headers: authHeaders(),
    body: {
      contentItemId: languageTaskForm.contentItemId,
      languageCode: languageTaskForm.languageCode,
      assigneeEmail: languageTaskForm.assigneeEmail,
      translationText: languageTaskForm.translationText,
      dueUtc: languageTaskForm.dueUtc || null,
      status: languageTaskForm.status,
    },
  })

  await loadLanguageTasks()
}

async function loadTranslationSuggestion() {
  if (!languageTaskForm.contentItemId || !languageTaskForm.languageCode.trim()) {
    translationSuggestion.value = null
    return
  }

  translationSuggestion.value = await $fetch(
    `${apiBase}/api/language-tasks/suggestions?contentItemId=${languageTaskForm.contentItemId}&languageCode=${languageTaskForm.languageCode}`,
    { headers: authHeaders() }
  )
}

async function applyTranslationSuggestion() {
  if (!languageTaskForm.contentItemId || !languageTaskForm.languageCode.trim()) return

  await $fetch(`${apiBase}/api/language-tasks/apply-memory`, {
    method: 'POST',
    headers: authHeaders(),
    body: {
      contentItemId: languageTaskForm.contentItemId,
      languageCode: languageTaskForm.languageCode,
      acceptSuggestion: true,
    },
  })

  await loadLanguageTasks()
}

async function saveManualTranslationToMemoryCandidate() {
  if (!languageTaskForm.contentItemId || !languageTaskForm.languageCode.trim() || !languageTaskForm.translationText.trim()) return

  await $fetch(`${apiBase}/api/language-tasks/apply-memory`, {
    method: 'POST',
    headers: authHeaders(),
    body: {
      contentItemId: languageTaskForm.contentItemId,
      languageCode: languageTaskForm.languageCode,
      acceptSuggestion: false,
      manualTranslationText: languageTaskForm.translationText,
    },
  })

  await loadLanguageTasks()
}

async function setLocalizationPage(nextPage: number) {
  localizationGridMeta.page = Math.max(1, nextPage)
  await loadLocalizationGrid()
}

async function applyLocalizationFilters() {
  localizationGridMeta.page = 1
  await loadLocalizationGrid()
}

async function createDiscussionThread() {
  if (!newThreadForm.contentItemId || !newThreadForm.body.trim()) return

  await $fetch(`${apiBase}/api/discussions/threads`, {
    method: 'POST',
    headers: { ...authHeaders(), 'X-Actor-Email': 'member@example.com' },
    body: {
      contentItemId: newThreadForm.contentItemId,
      title: newThreadForm.title,
      body: newThreadForm.body,
    },
  })

  newThreadForm.title = ''
  newThreadForm.body = ''
  await loadDiscussionThreads()
}

async function postDiscussionReply() {
  if (!selectedThreadId.value || !replyBody.value.trim()) return

  await $fetch(`${apiBase}/api/discussions/replies`, {
    method: 'POST',
    headers: { ...authHeaders(), 'X-Actor-Email': 'member@example.com' },
    body: {
      threadId: selectedThreadId.value,
      parentCommentId: null,
      body: replyBody.value,
    },
  })

  replyBody.value = ''
  await loadDiscussionComments(selectedThreadId.value)
}

async function resolveDiscussionThread(threadId: string) {
  await $fetch(`${apiBase}/api/discussions/threads/${threadId}/resolve`, {
    method: 'POST',
    headers: authHeaders(),
  })

  await loadDiscussionThreads()
  if (selectedThreadId.value === threadId)
  {
    await loadDiscussionComments(threadId)
  }
}

async function saveProjectSettings() {
  if (!projectSettingsForm.id) return
  if (!validateProjectSettings()) return

  await $fetch(`${apiBase}/api/projects/${projectSettingsForm.id}`, {
    method: 'PUT',
    headers: authHeaders(),
    body: {
      name: projectSettingsForm.name,
      sourceLanguage: projectSettingsForm.sourceLanguage,
      description: projectSettingsForm.description,
    },
  })

  settingsMessage.value = 'Project settings saved.'
  await loadData()
}

watch(currentRole, async () => {
  settingsMessage.value = ''
  permissions.value = localPermissionsFor(currentRole.value)
  await loadData()
})

onMounted(loadData)
</script>

<template>
  <section class="card" aria-label="Role selector">
    <h2>Role simulation</h2>
    <label for="current-role">Current role</label>
    <select id="current-role" v-model="currentRole">
      <option value="Viewer">Viewer</option>
      <option value="Editor">Editor</option>
      <option value="Reviewer">Reviewer</option>
      <option value="Admin">Admin</option>
    </select>
    <p>Permissions: read={{ permissions.canRead }}, write={{ permissions.canWrite }}, review={{ permissions.canReview }}, admin={{ permissions.canAdmin }}</p>
  </section>

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
        <p v-if="!canWrite" class="error">Your role is read-only for project write actions.</p>
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

        <button type="submit" :disabled="!canWrite">Create project</button>
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
    <p v-if="!canWrite" class="error">Role update detected: settings are read-only for this role.</p>
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

      <button type="submit" :disabled="!canWrite">Save project settings</button>
    </form>

    <h3>Audit log</h3>
    <p v-if="!canAdmin">Audit log is visible to admin role only.</p>
    <ul v-else>
      <li v-for="log in projectAuditLogs" :key="log.id">{{ log.action }} · {{ log.createdUtc }}</li>
    </ul>
    <p v-if="canAdmin && projectAuditLogs.length === 0">No changes recorded yet.</p>
    <p v-if="!selectedProjectHasContent">This project has no content yet.</p>
  </section>

  <section class="card">
    <h2>Content item schema (Story 2.1)</h2>
    <form @submit.prevent="createContentItem" novalidate>
      <label for="content-project">Project</label>
      <select id="content-project" v-model="contentItemForm.projectId">
        <option value="">Select project</option>
        <option v-for="p in projects" :key="p.id" :value="p.id">{{ p.name }}</option>
      </select>

      <label for="content-key">Key</label>
      <input id="content-key" v-model="contentItemForm.key" placeholder="auth.login.title" />
      <p class="error" v-if="errors.contentKey">{{ errors.contentKey }}</p>

      <label for="content-source">Source</label>
      <textarea id="content-source" v-model="contentItemForm.source" />
      <p class="error" v-if="errors.contentSource">{{ errors.contentSource }}</p>

      <label for="content-status">Status</label>
      <input id="content-status" v-model="contentItemForm.status" placeholder="draft" />
      <p class="error" v-if="errors.contentStatus">{{ errors.contentStatus }}</p>

      <label for="content-tags">Tags (comma separated)</label>
      <input id="content-tags" v-model="contentItemForm.tags" placeholder="auth,login" />

      <label for="content-context">Context</label>
      <input id="content-context" v-model="contentItemForm.context" />

      <label for="content-notes">Notes</label>
      <textarea id="content-notes" v-model="contentItemForm.notes" />

      <button type="submit" :disabled="!canWrite">Create content item</button>
      <p v-if="contentError" class="error">{{ contentError }}</p>
    </form>

    <h3>Search content</h3>
    <input v-model="contentSearch" @input="loadContentItems" placeholder="Search tags/context/notes/key" aria-label="Search content" />

    <h3>Saved filter presets</h3>
    <label for="new-filter-preset">Preset name</label>
    <input id="new-filter-preset" v-model="newPresetName" placeholder="My common search" />
    <button type="button" @click="saveCurrentFilterPreset">Save current filter</button>
    <ul>
      <li v-for="preset in savedFilterPresets" :key="preset.id">
        <button type="button" @click="applyFilterPreset(preset)">{{ preset.name }}</button>
      </li>
    </ul>

    <h3>Bulk actions</h3>
    <label for="bulk-status">Bulk status</label>
    <input id="bulk-status" v-model="bulkStatus" />
    <button type="button" @click="bulkUpdateSelectedStatus" :disabled="selectedContentItemIds.length === 0">Apply bulk status</button>

    <ul>
      <li v-for="item in contentItems" :key="item.id">
        <input type="checkbox" :aria-label="`select ${item.key}`" @change="toggleContentItemSelection(item.id, ($event.target as HTMLInputElement).checked)" />
        <button type="button" @click="selectedContentItemId = item.id; loadUsageReferences(); loadContentRevisions(item.id)">{{ item.key }}</button>
        · {{ item.status }} · {{ item.tags }}
        <span v-if="item.copyComponentId"> · linked</span>
        <select v-if="copyComponents.length > 0" @change="linkContentItemToComponent(item.id, ($event.target as HTMLSelectElement).value)">
          <option value="">Link to component</option>
          <option v-for="cc in copyComponents" :key="cc.id" :value="cc.id">{{ cc.name }}</option>
        </select>
        <button type="button" @click="addUsageReference(item.id)">Add usage ref</button>
        <button type="button" @click="updateContentItem(item)">Save item edit</button>
      </li>
    </ul>
  </section>

  <section class="card">
    <h2>Mentions and notifications (Story 4.2)</h2>
    <p>Use @email in comments/replies to notify collaborators.</p>

    <label for="notifications-user-email">Notifications user</label>
    <input id="notifications-user-email" v-model="notificationsUserEmail" @blur="loadNotifications" />

    <label>
      <input type="checkbox" v-model="notificationPrefs.inAppEnabled" /> In-app
    </label>
    <label>
      <input type="checkbox" v-model="notificationPrefs.emailEnabled" /> Email
    </label>
    <label>
      <input type="checkbox" v-model="notificationPrefs.slackEnabled" /> Slack
    </label>
    <button type="button" @click="setNotificationPreferences">Save notification preferences</button>

    <h3>Notifications</h3>
    <ul>
      <li v-for="n in notifications" :key="n.id">
        {{ n.message }} · channel={{ n.channelUsed }} · read={{ n.isRead }}
        <button type="button" @click="markNotificationRead(n.id, !n.isRead)">{{ n.isRead ? 'Mark unread' : 'Mark read' }}</button>
      </li>
    </ul>
  </section>

  <section class="card">
    <h2>Discussion threads (Story 4.1)</h2>

    <form @submit.prevent="createDiscussionThread" novalidate>
      <label for="discussion-content-item">Content item</label>
      <select id="discussion-content-item" v-model="newThreadForm.contentItemId" @change="loadDiscussionThreads">
        <option value="">Select item</option>
        <option v-for="item in contentItems" :key="item.id" :value="item.id">{{ item.key }}</option>
      </select>

      <label for="discussion-title">Thread title</label>
      <input id="discussion-title" v-model="newThreadForm.title" />

      <label for="discussion-body">Comment</label>
      <textarea id="discussion-body" v-model="newThreadForm.body" />

      <button type="submit" :disabled="!canWrite">Post thread</button>
    </form>

    <ul>
      <li v-for="thread in discussionThreads" :key="thread.id">
        <button type="button" @click="loadDiscussionComments(thread.id)">
          {{ thread.title || 'Untitled thread' }} · by {{ thread.createdByEmail }} · {{ thread.createdUtc }}
        </button>
        <button type="button" @click="resolveDiscussionThread(thread.id)">Resolve</button>
        <span v-if="thread.isResolved">(resolved)</span>
      </li>
    </ul>

    <div v-if="selectedThreadId">
      <h3>Thread comments</h3>
      <ul>
        <li v-for="comment in discussionComments" :key="comment.id">
          {{ comment.authorEmail }} · {{ comment.createdUtc }}
          <p>{{ comment.body }}</p>
        </li>
      </ul>

      <label for="discussion-reply">Reply</label>
      <textarea id="discussion-reply" v-model="replyBody" />
      <button type="button" @click="postDiscussionReply" :disabled="!canWrite">Post reply</button>
    </div>
  </section>

  <section class="card">
    <h2>All-languages grid (Story 3.3)</h2>

    <label for="grid-state-filter">State filter</label>
    <select id="grid-state-filter" v-model="localizationFilters.stateFilter" @change="applyLocalizationFilters">
      <option value="">All</option>
      <option value="missing">Missing</option>
      <option value="outdated">Outdated</option>
      <option value="review">Review</option>
    </select>

    <label for="grid-sort-by">Sort by</label>
    <select id="grid-sort-by" v-model="localizationFilters.sortBy" @change="applyLocalizationFilters">
      <option value="itemKey">Item key</option>
      <option value="source">Source</option>
      <option value="sourceStatus">Source status</option>
    </select>

    <label>
      <input type="checkbox" v-model="localizationFilters.desc" @change="applyLocalizationFilters" /> Descending
    </label>

    <ul>
      <li v-for="row in localizationGrid" :key="row.itemId">
        {{ row.itemKey }} · source={{ row.sourceStatus }} · missing={{ row.hasMissing }} · outdated={{ row.hasOutdated }} · review={{ row.hasReview }}
      </li>
    </ul>

    <p>Total {{ localizationGridMeta.total }} · Page {{ localizationGridMeta.page }}</p>
    <button type="button" @click="setLocalizationPage(localizationGridMeta.page - 1)" :disabled="localizationGridMeta.page <= 1">Prev</button>
    <button type="button" @click="setLocalizationPage(localizationGridMeta.page + 1)" :disabled="localizationGridMeta.page * localizationGridMeta.pageSize >= localizationGridMeta.total">Next</button>
  </section>

  <section class="card">
    <h2>Language management (Story 3.1)</h2>

    <form @submit.prevent="addProjectLanguage" novalidate>
      <label for="language-project">Project</label>
      <select id="language-project" v-model="languageForm.projectId" @change="loadProjectLanguages">
        <option value="">Select project</option>
        <option v-for="p in projects" :key="p.id" :value="p.id">{{ p.name }}</option>
      </select>

      <label for="language-code">BCP-47 code</label>
      <input id="language-code" v-model="languageForm.bcp47Code" placeholder="fr-CA" />

      <label>
        <input type="checkbox" v-model="languageForm.isSource" /> Mark as source language
      </label>

      <button type="submit" :disabled="!canWrite">Add language</button>
    </form>

    <ul>
      <li v-for="lang in projectLanguages" :key="lang.id">
        {{ lang.bcp47Code }} · source={{ lang.isSource }} · active={{ lang.isActive }}
        <button type="button" @click="toggleLanguageActive(lang.id, !lang.isActive)">{{ lang.isActive ? 'Deactivate' : 'Activate' }}</button>
        <button type="button" @click="changeDefaultSourceLanguage(lang.bcp47Code)">Set as source</button>
      </li>
    </ul>
  </section>

  <section class="card">
    <h2>Per-language tasks (Story 3.2)</h2>

    <form @submit.prevent="upsertLanguageTask" novalidate>
      <label for="task-content-item">Content item</label>
      <select id="task-content-item" v-model="languageTaskForm.contentItemId" @change="loadLanguageTasks">
        <option value="">Select item</option>
        <option v-for="item in contentItems" :key="item.id" :value="item.id">{{ item.key }}</option>
      </select>

      <label for="task-language">Language code</label>
      <input id="task-language" v-model="languageTaskForm.languageCode" placeholder="fr-CA" @blur="loadTranslationSuggestion" />

      <label for="task-assignee">Assignee email</label>
      <input id="task-assignee" v-model="languageTaskForm.assigneeEmail" placeholder="translator@example.com" />

      <label for="task-due">Due UTC</label>
      <input id="task-due" v-model="languageTaskForm.dueUtc" placeholder="2026-03-20T12:00:00Z" />

      <label for="task-status">Status</label>
      <input id="task-status" v-model="languageTaskForm.status" placeholder="in_progress" />

      <label for="task-translation">Translation text</label>
      <textarea id="task-translation" v-model="languageTaskForm.translationText" />

      <button type="button" @click="loadTranslationSuggestion">Check memory suggestion</button>
      <div v-if="translationSuggestion?.hasSuggestion">
        <p>Suggested translation: {{ translationSuggestion.suggestion.translationText }}</p>
        <button type="button" @click="applyTranslationSuggestion">Apply suggestion</button>
      </div>

      <button type="button" @click="saveManualTranslationToMemoryCandidate">Save manual as memory candidate</button>
      <button type="submit" :disabled="!canWrite">Save language task</button>
    </form>

    <ul>
      <li v-for="task in languageTasks" :key="task.id">
        {{ task.languageCode }} · {{ task.status }} · {{ task.assigneeEmail || 'unassigned' }} · due={{ task.dueUtc || 'none' }}
        <span v-if="task.isOutdated"> · OUTDATED</span>
        <span v-if="task.dueUtc && new Date(task.dueUtc) < new Date() && task.status !== 'done'"> · OVERDUE</span>
        <p v-if="task.previousApprovedTranslation">Previous approved: {{ task.previousApprovedTranslation }}</p>
      </li>
    </ul>
  </section>

  <section class="card">
    <h2>Content item history (Story 2.5)</h2>
    <p>Select a content item to view revision history.</p>

    <ul>
      <li v-for="rev in contentRevisions" :key="rev.id">
        {{ rev.createdUtc }} · {{ rev.actorEmail }} · {{ rev.eventType }} · {{ rev.diffSummary }}
        <button type="button" @click="rollbackRevision(rev.id)" :disabled="!canAdmin">Rollback to this revision</button>
      </li>
    </ul>

    <p v-if="contentRevisions.length === 0">No revisions yet for selected item.</p>

    <label for="compare-left-revision">Compare left revision</label>
    <select id="compare-left-revision" v-model="compareLeftRevisionId">
      <option value="">Select left</option>
      <option v-for="rev in contentRevisions" :key="`left-${rev.id}`" :value="rev.id">{{ rev.createdUtc }} · {{ rev.eventType }}</option>
    </select>

    <label for="compare-right-revision">Compare right revision</label>
    <select id="compare-right-revision" v-model="compareRightRevisionId">
      <option value="">Select right</option>
      <option v-for="rev in contentRevisions" :key="`right-${rev.id}`" :value="rev.id">{{ rev.createdUtc }} · {{ rev.eventType }}</option>
    </select>

    <button type="button" @click="compareSelectedRevisions">Compare selected revisions</button>

    <div v-if="revisionCompare">
      <p>Source delta: {{ revisionCompare.delta.sourceDelta }}</p>
      <p>Status delta: {{ revisionCompare.delta.statusDelta }}</p>
    </div>
  </section>

  <section class="card">
    <h2>Usage references (Story 2.3)</h2>
    <p>Open an item from the content list to inspect known linked references.</p>

    <label for="usage-project-filter">Filter by project</label>
    <select id="usage-project-filter" v-model="usageFilters.projectId" @change="loadUsageReferences">
      <option value="">All projects</option>
      <option v-for="p in projects" :key="p.id" :value="p.id">{{ p.name }}</option>
    </select>

    <label for="usage-screen-filter">Filter by screen</label>
    <input id="usage-screen-filter" v-model="usageFilters.screen" @input="loadUsageReferences" />

    <label for="usage-component-filter">Filter by component</label>
    <input id="usage-component-filter" v-model="usageFilters.component" @input="loadUsageReferences" />

    <ul v-if="usageReferences.length > 0">
      <li v-for="u in usageReferences" :key="u.id">{{ u.screen || 'n/a' }} · {{ u.component || 'n/a' }} · {{ u.referencePath || 'n/a' }}</li>
    </ul>
    <p v-else>No linked references found for current filters (unlinked content).</p>
  </section>

  <section class="card">
    <h2>Reusable copy components (Story 2.2)</h2>
    <form @submit.prevent="createCopyComponent" novalidate>
      <label for="copy-component-project">Project</label>
      <select id="copy-component-project" v-model="copyComponentForm.projectId">
        <option value="">Select project</option>
        <option v-for="p in projects" :key="p.id" :value="p.id">{{ p.name }}</option>
      </select>

      <label for="copy-component-name">Component name</label>
      <input id="copy-component-name" v-model="copyComponentForm.name" placeholder="Primary CTA label" />

      <label for="copy-component-source">Shared source</label>
      <textarea id="copy-component-source" v-model="copyComponentForm.source" />

      <button type="submit" :disabled="!canWrite">Create copy component</button>
    </form>

    <ul>
      <li v-for="cc in copyComponents" :key="cc.id">
        <strong>{{ cc.name }}</strong> · {{ cc.source }}
        <button type="button" @click="propagateCopyComponent(cc.id, cc.source)" :disabled="!canWrite">Propagate update</button>
        <button type="button" @click="deleteCopyComponent(cc.id)" :disabled="!canWrite">Delete</button>
      </li>
    </ul>
  </section>

  <MembershipAuditPanel v-if="canAdmin" :api-base="apiBase" />
</template>
