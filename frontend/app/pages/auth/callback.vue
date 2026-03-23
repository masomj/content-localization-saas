<script setup lang="ts">
definePageMeta({
  layout: 'auth',
})

const route = useRoute()
const router = useRouter()
const auth = useAuth()
const error = ref('')

onMounted(async () => {
  const code = String(route.query.code || '')

  if (!code) {
    error.value = 'Missing identity provider callback parameters.'
    return
  }

  const result = await auth.handleCallback()
  if (!result.success) {
    error.value = result.error || 'Could not complete sign in.'
    return
  }

  await router.replace('/app/dashboard')
})
</script>

<template>
  <div class="auth-card">
    <h1 class="auth-title">Signing you in…</h1>
    <p class="auth-subtitle">Finalizing your secure session.</p>
    <UiFormError v-if="error" :message="error" />
  </div>
</template>
