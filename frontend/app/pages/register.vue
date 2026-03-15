<script setup lang="ts">
definePageMeta({
  layout: 'auth',
})

useSeoMeta({
  title: 'Sign Up - LocFlow',
  description: 'Start your free trial. No credit card required. Join thousands of teams shipping localized content faster.',
})

const auth = useAuth()
const router = useRouter()

const firstName = ref('')
const lastName = ref('')
const email = ref('')
const password = ref('')
const company = ref('')
const acceptTerms = ref(false)
const errors = ref<{
  firstName?: string
  lastName?: string
  email?: string
  password?: string
  general?: string
}>({})
const isSubmitting = ref(false)

const isFallbackMode = computed(() => auth.isFallbackMode.value)

function validateForm(): boolean {
  errors.value = {}

  if (!firstName.value.trim()) {
    errors.value.firstName = 'First name is required'
  }

  if (!lastName.value.trim()) {
    errors.value.lastName = 'Last name is required'
  }

  if (!email.value.trim()) {
    errors.value.email = 'Work email is required'
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.value)) {
    errors.value.email = 'Please enter a valid email address'
  }

  if (!password.value) {
    errors.value.password = 'Password is required'
  } else if (password.value.length < 8) {
    errors.value.password = 'Must be at least 8 characters'
  }

  return Object.keys(errors.value).length === 0
}

async function handleSubmit() {
  if (!validateForm()) return

  isSubmitting.value = true
  errors.value.general = undefined

  await new Promise(resolve => setTimeout(resolve, 300))

  const result = await auth.register({
    firstName: firstName.value,
    lastName: lastName.value,
    email: email.value,
    password: password.value,
    company: company.value || undefined,
  })

  if (result.success) {
    router.push('/app/dashboard')
  } else {
    errors.value.general = result.error || 'Registration failed. Please try again.'
  }

  isSubmitting.value = false
}
</script>

<template>
  <div class="auth-card">
    <h1 class="auth-title">Create your account</h1>
    <p class="auth-subtitle">Start your 14-day free trial. No credit card required.</p>

    <div v-if="isFallbackMode" class="fallback-notice">
      <svg class="fallback-icon" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
        <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
      </svg>
      <span>Demo mode: Authentication is simulated. Real auth endpoint not configured.</span>
    </div>

    <form class="auth-form" novalidate @submit.prevent="handleSubmit">
      <UiFormError v-if="errors.general" :message="errors.general" />

      <div class="form-row">
        <div class="form-group">
          <label for="firstName">First name</label>
          <input
            id="firstName"
            v-model="firstName"
            type="text"
            placeholder="Jane"
            autocomplete="given-name"
            :class="{ 'input-error': errors.firstName }"
            :disabled="isSubmitting"
            @input="errors.firstName = undefined"
          >
          <p v-if="errors.firstName" class="field-error" role="alert">{{ errors.firstName }}</p>
        </div>

        <div class="form-group">
          <label for="lastName">Last name</label>
          <input
            id="lastName"
            v-model="lastName"
            type="text"
            placeholder="Doe"
            autocomplete="family-name"
            :class="{ 'input-error': errors.lastName }"
            :disabled="isSubmitting"
            @input="errors.lastName = undefined"
          >
          <p v-if="errors.lastName" class="field-error" role="alert">{{ errors.lastName }}</p>
        </div>
      </div>

      <div class="form-group">
        <label for="email">Work email</label>
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
          placeholder="8+ characters"
          autocomplete="new-password"
          :class="{ 'input-error': errors.password }"
          :disabled="isSubmitting"
          @input="errors.password = undefined"
        >
        <p v-if="errors.password" class="field-error" role="alert">{{ errors.password }}</p>
        <p v-else class="form-hint">Must be at least 8 characters</p>
      </div>

      <div class="form-group">
        <label for="company">Company name</label>
        <input
          id="company"
          v-model="company"
          type="text"
          placeholder="Acme Inc."
          autocomplete="organization"
          :disabled="isSubmitting"
        >
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
        <span :class="{ 'btn-content-hidden': isSubmitting }">Create account</span>
      </button>

      <p class="terms">
        By signing up, you agree to our
        <a href="#">Terms of Service</a> and <a href="#">Privacy Policy</a>.
      </p>
    </form>

    <p class="auth-footer">
      Already have an account?
      <NuxtLink to="/login" class="link">Sign in</NuxtLink>
    </p>
  </div>
</template>

<style scoped>
.auth-card {
  width: 100%;
  max-width: 440px;
  padding: var(--spacing-6) var(--spacing-8);
  background: var(--color-white);
  border-radius: var(--radius-xl);
  box-shadow: var(--shadow-md);
}

.auth-title {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  color: var(--color-gray-900);
  margin: 0 0 var(--spacing-2);
  text-align: center;
}

.auth-subtitle {
  font-size: var(--font-size-base);
  color: var(--color-gray-500);
  margin: 0 0 var(--spacing-8);
  text-align: center;
}

.fallback-notice {
  display: flex;
  align-items: flex-start;
  gap: var(--spacing-3);
  padding: var(--spacing-3) var(--spacing-4);
  margin-bottom: var(--spacing-6);
  background: #fefce8;
  border: 1px solid #fde047;
  border-radius: var(--radius-lg);
  font-size: var(--font-size-xs);
  color: #854d0e;
}

.fallback-icon {
  flex-shrink: 0;
  width: var(--spacing-5);
  height: var(--spacing-5);
  margin-top: 2px;
}

.auth-form {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-5);
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--spacing-4);
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-2);
}

.form-group label {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-gray-700);
}

.form-group input {
  padding: var(--spacing-3) var(--spacing-4);
  font-size: var(--font-size-base);
  border: 1px solid var(--color-gray-300);
  border-radius: var(--radius-lg);
  transition: border-color var(--transition-fast), box-shadow var(--transition-fast);
  font-family: inherit;
  color: var(--color-gray-900);
  background: var(--color-white);
}

.form-group input:focus {
  outline: none;
  border-color: var(--color-primary-500);
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.15);
}

.form-group input.input-error {
  border-color: var(--color-error);
}

.form-group input.input-error:focus {
  box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.15);
}

.form-group input:disabled {
  background: var(--color-gray-100);
  cursor: not-allowed;
}

.field-error {
  font-size: var(--font-size-xs);
  color: var(--color-error);
  margin: 0;
}

.form-hint {
  font-size: var(--font-size-xs);
  color: var(--color-gray-500);
  margin: 0;
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

.terms {
  font-size: var(--font-size-xs);
  color: var(--color-gray-500);
  margin: 0;
  text-align: center;
  line-height: var(--line-height-normal);
}

.terms a {
  color: var(--color-primary-600);
  text-decoration: none;
}

.terms a:hover {
  text-decoration: underline;
}

.auth-footer {
  margin: var(--spacing-6) 0 0;
  font-size: var(--font-size-sm);
  color: var(--color-gray-500);
  text-align: center;
}

.link {
  color: var(--color-primary-600);
  font-weight: var(--font-weight-semibold);
  text-decoration: none;
}

.link:hover {
  text-decoration: underline;
}

@media (max-width: 480px) {
  .auth-card {
    padding: var(--spacing-4);
  }

  .form-row {
    grid-template-columns: 1fr;
  }
}
</style>
