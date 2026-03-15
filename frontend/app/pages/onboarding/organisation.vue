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
const error = ref<string | undefined>()
const isSubmitting = ref(false)

async function handleSubmit() {
  if (!orgName.value.trim()) {
    error.value = 'Organization name is required'
    return
  }

  isSubmitting.value = true
  error.value = undefined

  await new Promise(resolve => setTimeout(resolve, 300))

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
        <span v-if="isSubmitting" class="btn-loader" aria-hidden="true">
          <svg class="spinner" viewBox="0 0 24 24">
            <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="3" fill="none" stroke-linecap="round">
              <animate attributeName="stroke-dasharray" values="0 150;42 150;42 150;42 150" dur="1.5s" repeatCount="indefinite" />
              <animate attributeName="stroke-dashoffset" values="0;-16;-59;-59" dur="1.5s" repeatCount="indefinite" />
            </circle>
          </svg>
        </span>
        <span :class="{ 'btn-content-hidden': isSubmitting }">Create organization</span>
      </button>
    </form>
  </div>
</template>

<style scoped>
.auth-card {
  width: 100%;
  max-width: 400px;
  padding: 2.5rem;
  background: white;
  border-radius: 1rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1), 0 1px 2px rgba(0, 0, 0, 0.06);
}

.auth-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--color-text-primary);
  margin: 0 0 0.5rem;
  text-align: center;
}

.auth-subtitle {
  font-size: 0.9375rem;
  color: var(--color-text-muted);
  margin: 0 0 2rem;
  text-align: center;
}

.auth-form {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-group label {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--color-text-secondary);
}

.label-with-hint {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.label-hint {
  font-size: 0.75rem;
  font-weight: 400;
  color: var(--color-text-muted);
}

.form-group input {
  padding: 0.75rem 1rem;
  font-size: 1rem;
  border: 1px solid var(--color-gray-300);
  border-radius: 0.5rem;
  transition: border-color 0.2s, box-shadow 0.2s;
}

.form-group input:focus {
  outline: none;
  border-color: var(--color-primary-600);
  box-shadow: 0 0 0 3px rgba(79, 70, 229, 0.1);
}

.form-group input.input-error {
  border-color: #ef4444;
}

.form-group input.input-error:focus {
  box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.1);
}

.form-group input:disabled {
  background: var(--color-surface);
  cursor: not-allowed;
}

.field-hint {
  font-size: 0.8125rem;
  color: var(--color-text-muted);
  margin: 0;
}

.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0.75rem 1rem;
  font-size: 0.9375rem;
  font-weight: 600;
  border-radius: 0.5rem;
  text-decoration: none;
  transition: all 0.2s;
  cursor: pointer;
  border: none;
  position: relative;
}

.btn:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}

.btn-primary {
  background: var(--color-primary-600);
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background: var(--color-primary-700);
}

.btn--loading {
  cursor: not-allowed;
}

.btn-loader {
  position: absolute;
  display: flex;
  align-items: center;
  justify-content: center;
}

.btn-content-hidden {
  visibility: hidden;
}

.spinner {
  width: 1.25em;
  height: 1.25em;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}
</style>
