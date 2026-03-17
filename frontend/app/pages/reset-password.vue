<script setup lang="ts">
import { authClient } from '~/api/authClient'

definePageMeta({ layout: 'auth' })
useSeoMeta({ title: 'Reset password - LocFlow' })

const route = useRoute()

const email = ref(String(route.query.email || ''))
const token = ref(String(route.query.token || ''))
const newPassword = ref('')
const confirmPassword = ref('')
const showPassword = ref(false)
const showConfirmPassword = ref(false)
const error = ref('')
const success = ref('')
const isSubmitting = ref(false)

async function handleSubmit() {
  error.value = ''
  success.value = ''
  if (!email.value || !token.value) {
    error.value = 'Missing reset token or email.'
    return
  }
  if (newPassword.value.length < 8) {
    error.value = 'Password must be at least 8 characters.'
    return
  }
  if (newPassword.value !== confirmPassword.value) {
    error.value = 'Passwords do not match.'
    return
  }

  isSubmitting.value = true
  try {
    await authClient.resetPassword(email.value, token.value, newPassword.value)
    success.value = 'Password reset successful. You can now sign in.'
  } catch (e: any) {
    error.value = e?.message || 'Reset failed.'
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <div class="auth-card">
    <h1 class="auth-title">Reset password</h1>
    <p class="auth-subtitle">Set a new password for your account.</p>
    <UiFormError v-if="error" :message="error" />
    <p v-if="success" class="success">{{ success }}</p>

    <form class="auth-form" @submit.prevent="handleSubmit">
      <div class="form-group">
        <label for="email">Email</label>
        <input id="email" v-model="email" type="email" :disabled="isSubmitting">
      </div>

      <div class="form-group">
        <label for="newPassword">New password</label>
        <div class="password-field">
          <input id="newPassword" v-model="newPassword" :type="showPassword ? 'text' : 'password'" :disabled="isSubmitting">
          <button type="button" class="password-toggle" @click="showPassword = !showPassword">{{ showPassword ? 'Hide' : 'Show' }}</button>
        </div>
      </div>

      <div class="form-group">
        <label for="confirmPassword">Confirm password</label>
        <div class="password-field">
          <input id="confirmPassword" v-model="confirmPassword" :type="showConfirmPassword ? 'text' : 'password'" :disabled="isSubmitting">
          <button type="button" class="password-toggle" @click="showConfirmPassword = !showConfirmPassword">{{ showConfirmPassword ? 'Hide' : 'Show' }}</button>
        </div>
      </div>

      <button class="btn btn-primary" type="submit" :disabled="isSubmitting">Reset password</button>
    </form>
  </div>
</template>

<style scoped>
.auth-card { width: 100%; max-width: 420px; margin: 0 auto; padding: var(--spacing-6) var(--spacing-8); background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-xl); }
.auth-title { margin: 0 0 var(--spacing-2); text-align: center; color: var(--color-text-primary); }
.auth-subtitle { margin: 0 0 var(--spacing-6); text-align: center; color: var(--color-text-secondary); }
.auth-form { display: flex; flex-direction: column; gap: var(--spacing-4); }
.form-group { display: flex; flex-direction: column; gap: var(--spacing-2); }
.form-group input { padding: var(--spacing-3) var(--spacing-4); border: 1px solid var(--color-border); border-radius: var(--radius-lg); background: var(--color-background); color: var(--color-text-primary); }
.password-field { display: flex; gap: var(--spacing-2); }
.password-field input { flex: 1; }
.password-toggle { border: 1px solid var(--color-border); background: var(--color-surface); color: var(--color-text-secondary); border-radius: var(--radius-md); padding: var(--spacing-2) var(--spacing-3); }
.success { color: var(--color-success); font-size: var(--font-size-sm); }
</style>
