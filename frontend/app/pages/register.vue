<script setup lang="ts">
definePageMeta({
  layout: 'auth',
})

useSeoMeta({
  title: 'Sign Up - InterCopy',
  description: 'Start your free trial. No credit card required. Join thousands of teams shipping localized content faster.',
})

const auth = useAuth()
const router = useRouter()

const firstName = ref('')
const lastName = ref('')
const email = ref('')
const password = ref('')
const confirmPassword = ref('')
const showPassword = ref(false)
const showConfirmPassword = ref(false)
const company = ref('')
const acceptTerms = ref(false)
const errors = ref<{
  firstName?: string
  lastName?: string
  email?: string
  password?: string
  confirmPassword?: string
  general?: string
}>({})
const isSubmitting = ref(false)

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

  if (!confirmPassword.value) {
    errors.value.confirmPassword = 'Please confirm your password'
  } else if (confirmPassword.value !== password.value) {
    errors.value.confirmPassword = 'Passwords do not match'
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

    <form class="auth-form" novalidate @submit.prevent="handleSubmit">
      <UiFormError v-if="errors.general" :message="errors.general" />

      <div class="form-row">
        <div class="form-group">
          <label for="firstName" class="label-with-hint">
            <span>First name</span>
            <span class="label-hint">e.g. Jane</span>
          </label>
          <input
            id="firstName"
            v-model="firstName"
            type="text"
            autocomplete="given-name"
            :class="{ 'input-error': errors.firstName }"
            :disabled="isSubmitting"
            @input="errors.firstName = undefined"
          >
          <p v-if="errors.firstName" class="field-error" role="alert">{{ errors.firstName }}</p>
        </div>

        <div class="form-group">
          <label for="lastName" class="label-with-hint">
            <span>Last name</span>
            <span class="label-hint">e.g. Doe</span>
          </label>
          <input
            id="lastName"
            v-model="lastName"
            type="text"
            autocomplete="family-name"
            :class="{ 'input-error': errors.lastName }"
            :disabled="isSubmitting"
            @input="errors.lastName = undefined"
          >
          <p v-if="errors.lastName" class="field-error" role="alert">{{ errors.lastName }}</p>
        </div>
      </div>

      <div class="form-group">
        <label for="email" class="label-with-hint">
          <span>Work email</span>
          <span class="label-hint">you@company.com</span>
        </label>
        <input
          id="email"
          v-model="email"
          type="text"
          autocomplete="email"
          :class="{ 'input-error': errors.email }"
          :disabled="isSubmitting"
          @input="errors.email = undefined"
        >
        <p v-if="errors.email" class="field-error" role="alert">{{ errors.email }}</p>
      </div>

      <div class="form-group">
        <label for="password" class="label-with-hint">
          <span>Password</span>
          <span class="label-hint">At least 8 characters</span>
        </label>
        <div class="password-field">
          <input
            id="password"
            v-model="password"
            :type="showPassword ? 'text' : 'password'"
            autocomplete="new-password"
            :class="{ 'input-error': errors.password }"
            :disabled="isSubmitting"
            @input="errors.password = undefined"
          >
          <button type="button" class="password-toggle" @click="showPassword = !showPassword">
            {{ showPassword ? 'Hide' : 'Show' }}
          </button>
        </div>
        <p v-if="errors.password" class="field-error" role="alert">{{ errors.password }}</p>
        <p v-else class="form-hint">Must be at least 8 characters</p>
      </div>

      <div class="form-group">
        <label for="confirmPassword" class="label-with-hint">
          <span>Confirm password</span>
          <span class="label-hint">Re-enter your password</span>
        </label>
        <div class="password-field">
          <input
            id="confirmPassword"
            v-model="confirmPassword"
            :type="showConfirmPassword ? 'text' : 'password'"
            autocomplete="new-password"
            :class="{ 'input-error': errors.confirmPassword }"
            :disabled="isSubmitting"
            @input="errors.confirmPassword = undefined"
          >
          <button type="button" class="password-toggle" @click="showConfirmPassword = !showConfirmPassword">
            {{ showConfirmPassword ? 'Hide' : 'Show' }}
          </button>
        </div>
        <p v-if="errors.confirmPassword" class="field-error" role="alert">{{ errors.confirmPassword }}</p>
      </div>

      <div class="form-group">
        <label for="company" class="label-with-hint">
          <span>Company name</span>
          <span class="label-hint">e.g. Acme Inc.</span>
        </label>
        <input
          id="company"
          v-model="company"
          type="text"
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

.fallback-notice {
  display: flex;
  align-items: flex-start;
  gap: var(--spacing-3);
  padding: var(--spacing-3) var(--spacing-4);
  margin-bottom: var(--spacing-6);
  background: color-mix(in srgb, var(--color-warning) 12%, var(--color-surface));
  border: 1px solid color-mix(in srgb, var(--color-warning) 60%, var(--color-border));
  border-radius: var(--radius-lg);
  font-size: var(--font-size-xs);
  color: var(--color-text-primary);
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
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: var(--spacing-4);
}

.form-group {
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: var(--spacing-2);
}

.form-group label {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
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

.form-group input {
  width: 100%;
  max-width: 100%;
  padding: var(--spacing-3) var(--spacing-4);
  font-size: var(--font-size-base);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  transition: border-color var(--transition-fast), box-shadow var(--transition-fast);
  font-family: inherit;
  color: var(--color-text-primary);
  background: var(--color-background);
}

.password-field {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
}

.password-field input {
  flex: 1;
}

.password-toggle {
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-surface);
  color: var(--color-text-secondary);
  padding: var(--spacing-2) var(--spacing-3);
  font-size: var(--font-size-xs);
  cursor: pointer;
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
  overflow-wrap: anywhere;
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
