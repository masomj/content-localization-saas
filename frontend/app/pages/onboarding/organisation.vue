<script setup lang="ts">
definePageMeta({
  layout: 'auth',
})

useSeoMeta({
  title: 'Create Organisation - InterCopy',
  description: 'Create your organisation or workspace to get started with InterCopy.',
})

const auth = useAuth()
const router = useRouter()

const orgName = ref('')
const selectedOrgId = ref('')
const error = ref<string | undefined>()
const isSubmitting = ref(false)

const availableOrganizations = computed(() => auth.organizations.value || [])

async function handleSelectExisting() {
  if (!selectedOrgId.value) {
    error.value = 'Select an organisation to continue'
    return
  }

  isSubmitting.value = true
  error.value = undefined

  const result = await auth.switchOrganization(selectedOrgId.value)
  if (result.success) {
    router.push('/app/dashboard')
  } else {
    error.value = result.error || 'Failed to switch organisation. Please try again.'
  }

  isSubmitting.value = false
}

async function handleSubmit() {
  if (!orgName.value.trim()) {
    error.value = 'Organisation name is required'
    return
  }

  isSubmitting.value = true
  error.value = undefined

  const result = await auth.createOrganization(orgName.value)

  if (result.success) {
    router.push('/app/dashboard')
  } else {
    error.value = result.error || 'Failed to create organisation. Please try again.'
  }

  isSubmitting.value = false
}
</script>

<template>
  <div class="auth-card">
    <h1 class="auth-title">Create your organisation</h1>
    <p class="auth-subtitle">Set up your workspace to start managing translations</p>

    <form class="auth-form" novalidate @submit.prevent="handleSubmit">
      <UiFormError v-if="error" :message="error" />

      <div v-if="availableOrganizations.length > 0" class="form-group">
        <label for="existingOrg" class="label-with-hint">
          <span>Choose an existing organisation</span>
          <span class="label-hint">Join a workspace you already belong to</span>
        </label>
        <select id="existingOrg" v-model="selectedOrgId" :disabled="isSubmitting">
          <option value="">Select an organisation...</option>
          <option v-for="org in availableOrganizations" :key="org.id" :value="org.id">
            {{ org.name }} ({{ org.role }})
          </option>
        </select>
        <button type="button" class="btn btn-secondary" :disabled="isSubmitting" @click="handleSelectExisting">
          Continue
        </button>
      </div>

      <div class="form-group">
        <label for="orgName" class="label-with-hint">
          <span>Organisation name</span>
          <span class="label-hint">e.g. Acme Inc.</span>
        </label>
        <input
          id="orgName"
          v-model="orgName"
          type="text"
          autocomplete="organization"
          :class="{ 'input-error': error }"
          :disabled="isSubmitting"
          @input="error = undefined"
        >
        <p class="field-hint">This becomes the name of your workspace</p>
      </div>

      <button
        type="submit"
        class="btn btn-primary"
        :class="{ 'btn--loading': isSubmitting }"
        :disabled="isSubmitting"
        :aria-busy="isSubmitting"
      >
        <span :class="{ 'btn-content-hidden': isSubmitting }">Create organisation</span>
      </button>
    </form>
  </div>
</template>

<style scoped>
.auth-card { width: 100%; max-width: 400px; margin: 0 auto; padding: var(--spacing-6) var(--spacing-8); background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-xl); box-shadow: var(--shadow-md); }
.auth-title { font-size: 1.5rem; font-weight: 700; color: var(--color-text-primary); margin: 0 0 0.5rem; text-align: center; }
.auth-subtitle { font-size: 0.9375rem; color: var(--color-text-muted); margin: 0 0 2rem; text-align: center; }
.auth-form { display: flex; flex-direction: column; gap: 1.25rem; }
.form-group { display: flex; flex-direction: column; gap: 0.5rem; }
.label-with-hint { display: flex; flex-direction: column; gap: 2px; }
.label-hint { font-size: 0.75rem; font-weight: 400; color: var(--color-text-muted); }
.form-group input, .form-group select { padding: 0.75rem 1rem; font-size: 1rem; border: 1px solid var(--color-gray-300); border-radius: 0.5rem; }
.form-group input.input-error { border-color: #ef4444; }
.field-hint { font-size: 0.8125rem; color: var(--color-text-muted); margin: 0; }
.btn { display: inline-flex; align-items: center; justify-content: center; padding: 0.75rem 1rem; font-size: 0.9375rem; font-weight: 600; border-radius: 0.5rem; border: none; cursor: pointer; }
.btn-primary { background: var(--color-primary-600); color: white; }
.btn-secondary { background: var(--color-gray-100); color: var(--color-text-primary); }
.btn-content-hidden { visibility: hidden; }
</style>
