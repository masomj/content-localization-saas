<script setup lang="ts">
definePageMeta({
  layout: 'auth',
})

useSeoMeta({
  title: 'Login - InterCopy',
  description: 'Sign in to your InterCopy account to manage translations and collaborate with your team.',
})

const auth = useAuth()
const isSubmitting = ref(false)
const error = ref('')

async function handleSubmit() {
  isSubmitting.value = true
  error.value = ''
  const result = await auth.login()
  if (!result.success) {
    error.value = result.error || 'Login failed. Please try again.'
    isSubmitting.value = false
  }
}

onMounted(() => {
  handleSubmit()
})
</script>

<template>
  <div class="auth-card">
    <h1 class="auth-title">Redirecting to sign in…</h1>
    <p class="auth-subtitle">Taking you to your organization login</p>

    <form class="auth-form" novalidate @submit.prevent="handleSubmit">
      <UiFormError v-if="error" :message="error" />

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
        <span :class="{ 'btn-content-hidden': isSubmitting }">Continue with Keycloak</span>
      </button>
    </form>
  </div>
</template>

<style scoped>
.auth-card {
  width: 100%;
  max-width: 400px;
  margin: 0 auto;
  padding: var(--spacing-6) var(--spacing-8);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-xl);
  box-shadow: var(--shadow-md);
}

.auth-title {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  color: var(--color-text-primary);
  margin: 0 0 var(--spacing-2);
  text-align: center;
}

.auth-subtitle {
  font-size: var(--font-size-base);
  color: var(--color-text-secondary);
  margin: 0 0 var(--spacing-8);
  text-align: center;
}

.auth-form {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-5);
}

.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-3) var(--spacing-4);
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-semibold);
  border-radius: var(--radius-lg);
  text-decoration: none;
  transition: all var(--transition-fast);
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
  color: var(--color-white);
}

.btn-primary:hover:not(:disabled) {
  background: var(--color-primary-700);
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
