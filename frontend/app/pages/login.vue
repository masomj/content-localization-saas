<script setup lang="ts">
definePageMeta({
  layout: 'auth',
})

useSeoMeta({
  title: 'Login - LocFlow',
  description: 'Sign in to your LocFlow account to manage translations and collaborate with your team.',
})

const auth = useAuth()
const router = useRouter()

const email = ref('')
const password = ref('')
const rememberMe = ref(false)
const errors = ref<{ email?: string; password?: string; general?: string }>({})
const isSubmitting = ref(false)

const isFallbackMode = computed(() => auth.isFallbackMode.value)

function validateForm(): boolean {
  errors.value = {}

  if (!email.value.trim()) {
    errors.value.email = 'Email is required'
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.value)) {
    errors.value.email = 'Please enter a valid email address'
  }

  if (!password.value) {
    errors.value.password = 'Password is required'
  }

  return Object.keys(errors.value).length === 0
}

async function handleSubmit() {
  if (!validateForm()) return

  isSubmitting.value = true
  errors.value.general = undefined

  await new Promise(resolve => setTimeout(resolve, 300))

  const result = await auth.login(email.value, password.value)

  if (result.success) {
    router.push('/app')
  } else {
    errors.value.general = result.error || 'Login failed. Please try again.'
  }

  isSubmitting.value = false
}
</script>

<template>
  <div class="auth-card">
    <h1 class="auth-title">Welcome back</h1>
    <p class="auth-subtitle">Sign in to your account to continue</p>

    <div v-if="isFallbackMode" class="fallback-notice">
      <svg class="fallback-icon" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
        <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
      </svg>
      <span>Demo mode: Authentication is simulated. Real auth endpoint not configured.</span>
    </div>

    <form class="auth-form" novalidate @submit.prevent="handleSubmit">
      <UiFormError v-if="errors.general" :message="errors.general" />

      <div class="form-group">
        <label for="email">Email</label>
        <input
          id="email"
          v-model="email"
          type="text"
          placeholder="you@company.com"
          autocomplete="email"
          :class="{ 'input-error': errors.email }"
          :disabled="isSubmitting"
          @input="errors.email = undefined"
        >
        <p v-if="errors.email" class="field-error" role="alert">{{ errors.email }}</p>
      </div>

      <div class="form-group">
        <label for="password">Password</label>
        <input
          id="password"
          v-model="password"
          type="password"
          placeholder="••••••••"
          autocomplete="current-password"
          :class="{ 'input-error': errors.password }"
          :disabled="isSubmitting"
          @input="errors.password = undefined"
        >
        <p v-if="errors.password" class="field-error" role="alert">{{ errors.password }}</p>
      </div>

      <div class="form-options">
        <label class="checkbox-label">
          <input v-model="rememberMe" type="checkbox" :disabled="isSubmitting">
          <span>Remember me</span>
        </label>
        <a href="#" class="forgot-link">Forgot password?</a>
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
        <span :class="{ 'btn-content-hidden': isSubmitting }">Sign in</span>
      </button>
    </form>

    <p class="auth-footer">
      Don't have an account?
      <NuxtLink to="/register" class="link">Sign up for free</NuxtLink>
    </p>
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
  color: #111827;
  margin: 0 0 0.5rem;
  text-align: center;
}

.auth-subtitle {
  font-size: 0.9375rem;
  color: #6b7280;
  margin: 0 0 2rem;
  text-align: center;
}

.fallback-notice {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  padding: 0.875rem 1rem;
  margin-bottom: 1.5rem;
  background: #fefce8;
  border: 1px solid #fde047;
  border-radius: 0.5rem;
  font-size: 0.8125rem;
  color: #854d0e;
}

.fallback-icon {
  flex-shrink: 0;
  width: 1.25rem;
  height: 1.25rem;
  margin-top: 0.125rem;
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
  color: #374151;
}

.form-group input {
  padding: 0.75rem 1rem;
  font-size: 1rem;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  transition: border-color 0.2s, box-shadow 0.2s;
}

.form-group input:focus {
  outline: none;
  border-color: #4f46e5;
  box-shadow: 0 0 0 3px rgba(79, 70, 229, 0.1);
}

.form-group input.input-error {
  border-color: #ef4444;
}

.form-group input.input-error:focus {
  box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.1);
}

.form-group input:disabled {
  background: #f9fafb;
  cursor: not-allowed;
}

.field-error {
  font-size: 0.8125rem;
  color: #ef4444;
  margin: 0;
}

.form-options {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.checkbox-label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
  color: #4b5563;
  cursor: pointer;
}

.checkbox-label input {
  width: 1rem;
  height: 1rem;
}

.checkbox-label input:disabled {
  cursor: not-allowed;
}

.forgot-link {
  font-size: 0.875rem;
  color: #4f46e5;
  font-weight: 500;
}

.forgot-link:hover {
  text-decoration: underline;
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
  background: #4f46e5;
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background: #4338ca;
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

.auth-footer {
  margin: 1.5rem 0 0;
  font-size: 0.875rem;
  color: #6b7280;
  text-align: center;
}

.link {
  color: #4f46e5;
  font-weight: 600;
}

.link:hover {
  text-decoration: underline;
}
</style>
