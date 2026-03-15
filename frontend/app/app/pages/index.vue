<script setup lang="ts">
definePageMeta({
  layout: 'app',
  middleware: ['auth'],
})

useSeoMeta({
  title: 'Dashboard - LocFlow',
  description: 'Your LocFlow dashboard',
})

const auth = useAuth()
const user = computed(() => auth.user.value)
</script>

<template>
  <div class="dashboard">
    <div class="dashboard-header">
      <h1 class="dashboard-title">Dashboard</h1>
      <p class="dashboard-welcome">Welcome back, {{ user?.name || 'User' }}!</p>
    </div>

    <div class="dashboard-content">
      <div class="dashboard-card">
        <div class="card-icon">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
          </svg>
        </div>
        <h2 class="card-title">Projects</h2>
        <p class="card-description">Manage your localization projects</p>
      </div>

      <div class="dashboard-card">
        <div class="card-icon">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M3 5h12M9 3v2m1.048 9.5A18.022 18.022 0 016.412 9m6.088 9h7M11 21l5-10 5 10M12.751 5C11.783 10.77 8.07 15.61 3 18.129" />
          </svg>
        </div>
        <h2 class="card-title">Translations</h2>
        <p class="card-description">View and edit translations</p>
      </div>

      <div class="dashboard-card">
        <div class="card-icon">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
          </svg>
        </div>
        <h2 class="card-title">Team</h2>
        <p class="card-description">Manage team members and permissions</p>
      </div>

      <div class="dashboard-card">
        <div class="card-icon">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
            <path d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
          </svg>
        </div>
        <h2 class="card-title">Settings</h2>
        <p class="card-description">Configure your account and preferences</p>
      </div>
    </div>

    <div class="dashboard-logout">
      <button class="logout-btn" @click="auth.logout()">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
        </svg>
        Sign out
      </button>
    </div>
  </div>
</template>

<style scoped>
.dashboard {
  max-width: 1200px;
  margin: 0 auto;
}

.dashboard-header {
  margin-bottom: 2rem;
}

.dashboard-title {
  font-size: 1.875rem;
  font-weight: 700;
  color: var(--color-text-primary);
  margin: 0 0 0.5rem;
}

.dashboard-welcome {
  font-size: 1rem;
  color: var(--color-text-muted);
  margin: 0;
}

.dashboard-content {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  gap: 1.5rem;
  margin-bottom: 2rem;
}

.dashboard-card {
  background: white;
  border-radius: 0.75rem;
  padding: 1.5rem;
  border: 1px solid var(--color-gray-200);
  transition: all 0.2s;
}

.dashboard-card:hover {
  border-color: var(--color-primary-600);
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
}

.card-icon {
  width: 2.5rem;
  height: 2.5rem;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--color-primary-50);
  border-radius: 0.5rem;
  margin-bottom: 1rem;
  color: var(--color-primary-600);
}

.card-icon svg {
  width: 1.5rem;
  height: 1.5rem;
}

.card-title {
  font-size: 1.125rem;
  font-weight: 600;
  color: var(--color-text-primary);
  margin: 0 0 0.5rem;
}

.card-description {
  font-size: 0.875rem;
  color: var(--color-text-muted);
  margin: 0;
}

.dashboard-logout {
  display: flex;
  justify-content: flex-end;
  padding-top: 1rem;
  border-top: 1px solid var(--color-gray-200);
}

.logout-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.625rem 1rem;
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--color-text-muted);
  background: transparent;
  border: 1px solid var(--color-gray-200);
  border-radius: 0.5rem;
  cursor: pointer;
  transition: all 0.2s;
}

.logout-btn:hover {
  color: #ef4444;
  border-color: #ef4444;
  background: #fef2f2;
}

.logout-btn svg {
  width: 1.125rem;
  height: 1.125rem;
}
</style>
