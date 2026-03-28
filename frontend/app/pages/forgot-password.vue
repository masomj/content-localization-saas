<script setup lang="ts">
definePageMeta({
  layout: 'auth',
})

useSeoMeta({
  title: 'Forgot password - InterCopy',
})

const auth = useAuth()
const email = ref('')
const isSubmitting = ref(false)
const error = ref('')
const success = ref('')
const resetLink = ref('')

async function handleSubmit() {
  error.value = ''
  success.value = ''
  resetLink.value = ''

  isSubmitting.value = true
  const result = await auth.requestPasswordReset(email.value)
  isSubmitting.value = false

  if (!result.success) {
    error.value = result.error || 'Unable to request password reset.'
    return
  }

  success.value = 'If an account exists, password reset instructions have been sent.'
  if (result.resetLink) {
    resetLink.value = result.resetLink
  }
}
</script>

<template>
  <div class="auth-card">
    <h1 class="auth-title">Forgot password</h1>
    <p class="auth-subtitle">Enter your email and we’ll send reset instructions.</p>

    <form class="auth-form" @submit.prevent="handleSubmit">
      <UiFormError v-if="error" :message="error" />
      <p v-if="success" class="success-message">{{ success }}</p>

      <div class="form-group">
        <label for="email" class="label-with-hint">
          <span>Email</span>
          <span class="label-hint">you@company.com</span>
        </label>
        <input id="email" v-model="email" type="email" autocomplete="email" :disabled="isSubmitting">
      </div>

      <button class="btn btn-primary" type="submit" :disabled="isSubmitting">
        Send reset link
      </button>
    </form>

    <p v-if="resetLink" class="dev-link">
      Dev reset link: <code>{{ resetLink }}</code>
    </p>

    <p class="auth-footer">
      Remembered it?
      <NuxtLink to="/login" class="link">Back to login</NuxtLink>
    </p>
  </div>
</template>

<style scoped>
.auth-card {
  width: 100%;
  max-width: 420px;
  margin: 0 auto;
  padding: var(--spacing-6) var(--spacing-8);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-xl);
}
.auth-title { margin: 0 0 var(--spacing-2); text-align: center; color: var(--color-text-primary); }
.auth-subtitle { margin: 0 0 var(--spacing-6); text-align: center; color: var(--color-text-secondary); }
.auth-form { display: flex; flex-direction: column; gap: var(--spacing-4); }
.form-group { display: flex; flex-direction: column; gap: var(--spacing-2); }
.form-group input { padding: var(--spacing-3) var(--spacing-4); border: 1px solid var(--color-border); border-radius: var(--radius-lg); background: var(--color-background); color: var(--color-text-primary); }
.success-message { color: var(--color-success); margin: 0; font-size: var(--font-size-sm); }
.dev-link { margin-top: var(--spacing-4); font-size: var(--font-size-xs); color: var(--color-text-muted); word-break: break-all; }
.dev-link code { background: var(--color-gray-100); padding: 2px 6px; border-radius: var(--radius-sm); }
.auth-footer { margin-top: var(--spacing-6); text-align: center; color: var(--color-text-muted); }
.link { color: var(--color-primary-600); }
</style>
