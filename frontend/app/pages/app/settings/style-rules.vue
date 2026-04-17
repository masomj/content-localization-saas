<script setup lang="ts">
import UiButton from '~/components/ui/Button.vue'
import UiInput from '~/components/ui/Input.vue'
import UiSelect from '~/components/ui/Select.vue'
import { projectsClient } from '~/api/projectsClient'
import { styleRulesClient } from '~/api/styleRulesClient'
import type { Project } from '~/api/types'
import type { StyleRuleDto } from '~/api/styleRulesClient'

definePageMeta({
  layout: 'app',
  middleware: ['admin'],
})

useSeoMeta({ title: 'Style Rules - InterCopy' })

// Project selection
const projects = ref<Project[]>([])
const selectedProjectId = ref('')
const isLoadingProjects = ref(false)

// Rules state
const rules = ref<StyleRuleDto[]>([])
const isLoadingRules = ref(false)

// Modal state
const showRuleModal = ref(false)
const editingRule = ref<StyleRuleDto | null>(null)
const ruleForm = reactive({
  name: '',
  ruleType: 'case_check',
  pattern: '',
  scope: 'all',
  message: '',
  isActive: true,
})
const ruleError = ref('')

// Delete confirmation
const showDeleteConfirm = ref(false)
const deleteTarget = ref<{ id: string; name: string } | null>(null)

// Feedback
const feedback = ref<{ type: 'success' | 'error'; message: string } | null>(null)
function showFeedback(type: 'success' | 'error', message: string) {
  feedback.value = { type, message }
  setTimeout(() => { feedback.value = null }, 3000)
}

const RULE_TYPE_OPTIONS = [
  { value: 'case_check', label: 'Case Check' },
  { value: 'regex', label: 'Regex' },
  { value: 'no_trailing_punctuation', label: 'No Trailing Punctuation' },
  { value: 'max_words', label: 'Max Words' },
]

const SCOPE_OPTIONS = [
  { value: 'all', label: 'All' },
  { value: 'button_label', label: 'Button Label' },
  { value: 'error_message', label: 'Error Message' },
  { value: 'heading', label: 'Heading' },
  { value: 'body_text', label: 'Body Text' },
  { value: 'placeholder', label: 'Placeholder' },
  { value: 'tooltip', label: 'Tooltip' },
  { value: 'menu_item', label: 'Menu Item' },
]

const showPatternInput = computed(() =>
  ruleForm.ruleType === 'regex' || ruleForm.ruleType === 'max_words',
)

async function fetchProjects() {
  isLoadingProjects.value = true
  try {
    projects.value = await projectsClient.list('')
  } catch (e: any) {
    showFeedback('error', e?.message || 'Failed to load projects')
  } finally {
    isLoadingProjects.value = false
  }
}

async function fetchRules() {
  if (!selectedProjectId.value) return
  isLoadingRules.value = true
  try {
    rules.value = await styleRulesClient.list(selectedProjectId.value)
  } catch (e: any) {
    showFeedback('error', e?.message || 'Failed to load style rules')
  } finally {
    isLoadingRules.value = false
  }
}

function openCreateRule() {
  editingRule.value = null
  ruleForm.name = ''
  ruleForm.ruleType = 'case_check'
  ruleForm.pattern = ''
  ruleForm.scope = 'all'
  ruleForm.message = ''
  ruleForm.isActive = true
  ruleError.value = ''
  showRuleModal.value = true
}

function openEditRule(r: StyleRuleDto) {
  editingRule.value = r
  ruleForm.name = r.name
  ruleForm.ruleType = r.ruleType
  ruleForm.pattern = r.pattern
  ruleForm.scope = r.scope
  ruleForm.message = r.message
  ruleForm.isActive = r.isActive
  ruleError.value = ''
  showRuleModal.value = true
}

async function saveRule() {
  ruleError.value = ''
  if (!ruleForm.name.trim()) {
    ruleError.value = 'Name is required.'
    return
  }
  if (!ruleForm.message.trim()) {
    ruleError.value = 'Message is required.'
    return
  }
  try {
    const payload = {
      name: ruleForm.name,
      ruleType: ruleForm.ruleType,
      pattern: ruleForm.pattern,
      scope: ruleForm.scope,
      message: ruleForm.message,
      isActive: ruleForm.isActive,
    }
    if (editingRule.value) {
      await styleRulesClient.update(selectedProjectId.value, editingRule.value.id, payload)
    } else {
      await styleRulesClient.create(selectedProjectId.value, payload)
    }
    showRuleModal.value = false
    await fetchRules()
    showFeedback('success', editingRule.value ? 'Rule updated' : 'Rule created')
  } catch (e: any) {
    ruleError.value = e?.message || 'Failed to save rule'
  }
}

function confirmDeleteRule(r: StyleRuleDto) {
  deleteTarget.value = { id: r.id, name: r.name }
  showDeleteConfirm.value = true
}

async function executeDelete() {
  if (!deleteTarget.value) return
  try {
    await styleRulesClient.delete(selectedProjectId.value, deleteTarget.value.id)
    await fetchRules()
    showFeedback('success', 'Rule deleted')
  } catch (e: any) {
    showFeedback('error', e?.message || 'Failed to delete rule')
  } finally {
    showDeleteConfirm.value = false
    deleteTarget.value = null
  }
}

function ruleTypeLabel(type: string) {
  return RULE_TYPE_OPTIONS.find(o => o.value === type)?.label ?? type
}

function scopeLabel(scope: string) {
  return SCOPE_OPTIONS.find(o => o.value === scope)?.label ?? scope
}

watch(selectedProjectId, () => {
  rules.value = []
  fetchRules()
})

onMounted(() => {
  fetchProjects()
})
</script>

<template>
  <div class="sr-page">
    <header class="sr-page__header">
      <div>
        <h1>Style Rules</h1>
        <p class="sr-page__subtitle">Configure content style checks per project</p>
      </div>
      <UiButton :disabled="!selectedProjectId" @click="openCreateRule">Add Rule</UiButton>
    </header>

    <div v-if="feedback" :class="['sr-feedback', `sr-feedback--${feedback.type}`]">
      {{ feedback.message }}
    </div>

    <!-- Project selector -->
    <div class="sr-project-selector">
      <UiSelect
        v-model="selectedProjectId"
        label="Project"
        :options="projects.map(p => ({ value: p.id, label: p.name }))"
      />
    </div>

    <!-- Rules list -->
    <div v-if="!selectedProjectId" class="sr-empty">Select a project to manage style rules.</div>
    <div v-else-if="isLoadingRules" class="sr-loading">Loading rules...</div>
    <div v-else-if="rules.length === 0" class="sr-empty">No style rules configured for this project.</div>
    <table v-else class="sr-table">
      <thead>
        <tr>
          <th>Name</th>
          <th>Type</th>
          <th>Scope</th>
          <th>Message</th>
          <th>Active</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="r in rules" :key="r.id">
          <td class="sr-table__name">{{ r.name }}</td>
          <td>{{ ruleTypeLabel(r.ruleType) }}</td>
          <td>{{ scopeLabel(r.scope) }}</td>
          <td class="sr-table__message">{{ r.message }}</td>
          <td>
            <span :class="['sr-badge', r.isActive ? 'sr-badge--active' : 'sr-badge--inactive']">
              {{ r.isActive ? 'Active' : 'Inactive' }}
            </span>
          </td>
          <td class="sr-table__actions">
            <button class="sr-icon-btn" title="Edit" @click="openEditRule(r)">
              <svg viewBox="0 0 20 20" fill="currentColor" width="16" height="16"><path d="M13.586 3.586a2 2 0 112.828 2.828l-.793.793-2.828-2.828.793-.793zM11.379 5.793L3 14.172V17h2.828l8.38-8.379-2.83-2.828z" /></svg>
            </button>
            <button class="sr-icon-btn sr-icon-btn--danger" title="Delete" @click="confirmDeleteRule(r)">
              <svg viewBox="0 0 20 20" fill="currentColor" width="16" height="16"><path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd" /></svg>
            </button>
          </td>
        </tr>
      </tbody>
    </table>

    <!-- Add/Edit Rule Modal -->
    <div v-if="showRuleModal" class="sr-modal-overlay" @click.self="showRuleModal = false">
      <div class="sr-modal">
        <h3>{{ editingRule ? 'Edit Rule' : 'Add Rule' }}</h3>
        <div class="sr-modal__field">
          <UiInput v-model="ruleForm.name" label="Name" hint="A short descriptive name for this rule" />
        </div>
        <div class="sr-modal__field">
          <UiSelect
            v-model="ruleForm.ruleType"
            label="Rule Type"
            :options="RULE_TYPE_OPTIONS"
          />
        </div>
        <div v-if="showPatternInput" class="sr-modal__field">
          <UiInput v-model="ruleForm.pattern" label="Pattern" hint="Regex pattern or max word count" />
        </div>
        <div class="sr-modal__field">
          <UiSelect
            v-model="ruleForm.scope"
            label="Scope"
            :options="SCOPE_OPTIONS"
          />
        </div>
        <div class="sr-modal__field">
          <label class="sr-label">
            <span>Message</span>
            <span class="sr-label-hint">Shown when the rule is violated</span>
          </label>
          <textarea v-model="ruleForm.message" class="sr-textarea" rows="3" />
        </div>
        <div class="sr-modal__field">
          <label class="sr-toggle">
            <input v-model="ruleForm.isActive" type="checkbox" />
            <span>Active</span>
          </label>
        </div>
        <p v-if="ruleError" class="sr-error">{{ ruleError }}</p>
        <div class="sr-modal__actions">
          <UiButton variant="secondary" @click="showRuleModal = false">Cancel</UiButton>
          <UiButton @click="saveRule">{{ editingRule ? 'Update' : 'Create' }}</UiButton>
        </div>
      </div>
    </div>

    <!-- Delete Confirmation Modal -->
    <div v-if="showDeleteConfirm" class="sr-modal-overlay" @click.self="showDeleteConfirm = false">
      <div class="sr-modal">
        <h3>Confirm Delete</h3>
        <p>Are you sure you want to delete <strong>{{ deleteTarget?.name }}</strong>? This action cannot be undone.</p>
        <div class="sr-modal__actions">
          <UiButton variant="secondary" @click="showDeleteConfirm = false">Cancel</UiButton>
          <UiButton variant="danger" @click="executeDelete">Delete</UiButton>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.sr-page {
  max-width: 1000px;
}

.sr-page__header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: var(--spacing-6);
}

.sr-page__header h1 {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
  margin: 0 0 var(--spacing-1) 0;
}

.sr-page__subtitle {
  color: var(--color-text-muted);
  margin: 0;
}

.sr-project-selector {
  max-width: 360px;
  margin-bottom: var(--spacing-6);
}

.sr-table {
  width: 100%;
  border-collapse: collapse;
  font-size: var(--font-size-sm);
}

.sr-table th {
  text-align: left;
  padding: var(--spacing-2) var(--spacing-3);
  border-bottom: 2px solid var(--color-border);
  color: var(--color-text-secondary);
  font-weight: var(--font-weight-medium);
  font-size: var(--font-size-xs);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.sr-table td {
  padding: var(--spacing-2) var(--spacing-3);
  border-bottom: 1px solid var(--color-border);
  color: var(--color-text-primary);
  vertical-align: top;
}

.sr-table__name {
  font-weight: var(--font-weight-medium);
}

.sr-table__message {
  max-width: 250px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.sr-table__actions {
  display: flex;
  gap: var(--spacing-1);
  white-space: nowrap;
}

.sr-badge {
  display: inline-block;
  padding: 1px var(--spacing-2);
  border-radius: var(--radius-full);
  font-size: var(--font-size-xs);
}

.sr-badge--active {
  background: color-mix(in srgb, var(--color-success) 12%, var(--color-surface));
  color: var(--color-success);
}

.sr-badge--inactive {
  background: color-mix(in srgb, var(--color-text-muted) 12%, var(--color-surface));
  color: var(--color-text-muted);
}

.sr-icon-btn {
  background: none;
  border: none;
  cursor: pointer;
  padding: var(--spacing-1);
  border-radius: var(--radius-md);
  color: var(--color-text-muted);
  display: flex;
  align-items: center;
  transition: background var(--transition-fast), color var(--transition-fast);
}

.sr-icon-btn:hover {
  background: var(--color-border);
  color: var(--color-text-primary);
}

.sr-icon-btn--danger:hover {
  background: color-mix(in srgb, var(--color-error) 14%, var(--color-surface));
  color: var(--color-error);
}

/* Modals */
.sr-modal-overlay {
  position: fixed;
  inset: 0;
  background: color-mix(in srgb, var(--color-black) 45%, transparent);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: var(--z-modal);
}

.sr-modal {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-xl);
  padding: var(--spacing-6);
  min-width: 440px;
  max-width: 540px;
  box-shadow: var(--shadow-xl);
}

.sr-modal h3 {
  margin: 0 0 var(--spacing-4) 0;
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text-primary);
}

.sr-modal__field {
  margin-bottom: var(--spacing-3);
}

.sr-modal__actions {
  display: flex;
  justify-content: flex-end;
  gap: var(--spacing-2);
  margin-top: var(--spacing-4);
}

.sr-label {
  display: flex;
  flex-direction: column;
  gap: 2px;
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-secondary);
  margin-bottom: var(--spacing-1);
}

.sr-label-hint {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  font-weight: var(--font-weight-normal);
}

.sr-textarea {
  width: 100%;
  padding: var(--spacing-3) var(--spacing-4);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  background: var(--color-background);
  color: var(--color-text-primary);
  font-size: var(--font-size-sm);
  font-family: inherit;
  resize: vertical;
  box-sizing: border-box;
}

.sr-textarea:focus {
  outline: none;
  border-color: var(--color-primary-500);
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.15);
}

.sr-toggle {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  cursor: pointer;
}

.sr-toggle input {
  accent-color: var(--color-primary-500);
}

.sr-loading {
  padding: var(--spacing-6);
  text-align: center;
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.sr-empty {
  padding: var(--spacing-6);
  text-align: center;
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.sr-error {
  color: var(--color-error);
  font-size: var(--font-size-sm);
  margin: var(--spacing-2) 0 0;
}

.sr-feedback {
  padding: var(--spacing-3) var(--spacing-4);
  border-radius: var(--radius-lg);
  margin-bottom: var(--spacing-4);
  font-size: var(--font-size-sm);
}

.sr-feedback--success {
  background: color-mix(in srgb, var(--color-success) 12%, var(--color-surface));
  color: var(--color-text-primary);
  border: 1px solid color-mix(in srgb, var(--color-success) 40%, var(--color-border));
}

.sr-feedback--error {
  background: color-mix(in srgb, var(--color-error) 14%, var(--color-surface));
  color: var(--color-text-primary);
  border: 1px solid color-mix(in srgb, var(--color-error) 45%, var(--color-border));
}
</style>
