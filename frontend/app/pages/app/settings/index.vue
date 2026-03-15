<script setup lang="ts">
import UiButton from '~/components/ui/Button.vue'
import UiCard from '~/components/ui/Card.vue'
import UiInput from '~/components/ui/Input.vue'

definePageMeta({
  layout: 'app',
  middleware: ['admin'],
})

useSeoMeta({
  title: 'Settings - LocFlow',
})

const auth = useAuth()
const router = useRouter()

const orgName = ref('')
const isSaving = ref(false)

onMounted(() => {
  if (auth.organization.value) {
    orgName.value = auth.organization.value.name
  }
})

async function handleSave() {
  isSaving.value = true
  await new Promise(resolve => setTimeout(resolve, 500))
  isSaving.value = false
}
</script>

<template>
  <div class="settings-page">
    <header class="page-header">
      <div>
        <h1>Settings</h1>
        <p class="page-subtitle">Manage your organization settings</p>
      </div>
    </header>

    <div class="settings-sections">
      <UiCard class="settings-section">
        <h2>Organization</h2>
        <p class="section-description">Basic information about your organization</p>
        
        <form class="settings-form" @submit.prevent="handleSave">
          <div class="form-group">
            <label for="orgName" class="label-with-hint">
              <span>Organization Name</span>
              <span class="label-hint">Your organization name</span>
            </label>
            <UiInput
              id="orgName"
              v-model="orgName"
              type="text"
            />
          </div>
          
          <div class="form-actions">
            <UiButton type="submit" :disabled="isSaving">
              {{ isSaving ? 'Saving...' : 'Save Changes' }}
            </UiButton>
          </div>
        </form>
      </UiCard>

      <UiCard class="settings-section">
        <h2>Danger Zone</h2>
        <p class="section-description">Irreversible and destructive actions</p>
        
        <div class="danger-actions">
          <div class="danger-action">
            <div>
              <h3>Delete Organization</h3>
              <p>Permanently delete this organization and all its data</p>
            </div>
            <UiButton variant="danger">Delete</UiButton>
          </div>
        </div>
      </UiCard>
    </div>
  </div>
</template>

<style scoped>
.settings-page {
  max-width: 800px;
}

.page-header {
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

.settings-sections {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-6);
}

.settings-section {
  padding: var(--spacing-6);
}

.settings-section h2 {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-gray-900);
  margin: 0 0 var(--spacing-1) 0;
}

.section-description {
  font-size: var(--font-size-sm);
  color: var(--color-gray-500);
  margin: 0 0 var(--spacing-5) 0;
}

.settings-form {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-4);
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-2);
}

.form-group label {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-gray-700);
}

.label-with-hint {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.label-hint {
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-regular);
  color: var(--color-gray-500);
}

.form-actions {
  display: flex;
  justify-content: flex-start;
}

.danger-actions {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-4);
}

.danger-action {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--spacing-4);
  background: var(--color-red-50);
  border: 1px solid var(--color-red-200);
  border-radius: var(--radius-lg);
}

.danger-action h3 {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-red-700);
  margin: 0 0 var(--spacing-1) 0;
}

.danger-action p {
  font-size: var(--font-size-sm);
  color: var(--color-red-600);
  margin: 0;
}
</style>
