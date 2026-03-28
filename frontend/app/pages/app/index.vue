<script setup lang="ts">
definePageMeta({
  layout: 'app',
})

useSeoMeta({
  title: 'App - InterCopy',
})

const auth = useAuth()

onMounted(() => {
  navigateTo('/app/dashboard')
})
</script>

<template>
  <div class="app-index">
    <p>Redirecting to dashboard...</p>
  </div>
</template>
