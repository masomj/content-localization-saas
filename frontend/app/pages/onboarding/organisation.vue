<script setup lang="ts">
definePageMeta({
  layout: 'auth',
})

useSeoMeta({
  title: 'Create Organization - LocFlow',
  description: 'Create your organization to get started with LocFlow.',
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
    error.value = 'Select an organization to continue'
    return
  }

  isSubmitting.value = true
  error.value = undefined

  const result = await auth.switchOrganization(selectedOrgId.value)
  if (result.success) {
    router.push('/app/dashboard')
  } else {
    error.value = result.error || 'Failed to switch organization. Please try again.'
  }

  isSubmitting.value = false
}

async function handleSubmit() {
  if (!orgName.value.trim()) {
    error.value = 'Organization name is required'
    return
  }

  isSubmitting.value = true
  error.value = undefined

  const result = await auth.createOrganization(orgName.value)

  if (result.success) {
    router.push('/app/dashboard')
  } else {
    error.value = result.error || 'Failed to create organization. Please try again.'
  }

  isSubmitting.value = false
}
</script>

<template>
  <div class="auth-card">
    <h1 class="auth-title">Create your organization</h1>
    <p class="auth-subtitle">Set up your workspace to start managing translations</p>

    <form class="auth-form" novalidate @submit.prevent="handleSubmit">
      <UiFormError v-if="error" :message="error" />

      <div v-if="availableOrganizations.length > 0" class="form-group">
        <label for="existingOrg" class="label-with-hint">
          <span>Choose an organization</span>
          <span class="label-hint">Continue with an existing workspace</span>
        </label>
        <select id="existingOrg" v-model="selectedOrgId" :disabled="isSubmitting">
          <option value="">Select organization</option>
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
          <span>Organization name</span>
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
        <p class="field-hint">This is the name of your company or team</p>
      </div>

      <button
        type="submit"
        class="btn btn-primary"
        :class="{ 'btn--loading': isSubmitting }"
        :disabled="isSubmitting"
        :aria-busy="isSubmitting"
      >
        <span :class="{ 'btn-content-hidden': isSubmitting }">Create organization</span>
      </button>
    </form>
  </div>
</template>

<style scoped>
.auth-card { width: 100%; max-width: 400px; padding: 2.5rem; background: white; border-radius: 1rem; box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1), 0 1px 2px rgba(0, 0, 0, 0.06); }
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
