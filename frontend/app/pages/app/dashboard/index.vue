<script setup lang="ts">
definePageMeta({
  layout: 'app',
})

useSeoMeta({
  title: 'Dashboard - InterCopy',
})

const auth = useAuth()
</script>

<template>
  <div class="dashboard">
    <h1>Welcome to InterCopy</h1>
    <template v-if="!auth.isLoading.value">
      <p>Logged in as: {{ auth.user.value?.email }}</p>
      <p>Organization: {{ auth.organization.value?.name }}</p>
    </template>
  </div>
</template>

<style scoped>
.dashboard {
  padding: 2rem;
}
</style>
