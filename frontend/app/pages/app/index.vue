<script setup lang="ts">
definePageMeta({
  layout: 'app',
})

useSeoMeta({
  title: 'App - LocFlow',
})

const auth = useAuth()
</script>

<template>
  <div class="app-index">
    <p>Loading...</p>
  </div>
</template>
