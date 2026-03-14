<script setup lang="ts">
useSeoMeta({
  title: 'Login - LocFlow',
  description: 'Sign in to your LocFlow account to manage translations and collaborate with your team.',
})
</script>

<template>
  <div class="auth-page">
    <header class="auth-header">
      <NuxtLink to="/" class="brand" aria-label="LocFlow Home">
        <span class="brand-icon" aria-hidden="true">◈</span>
        <span class="brand-text">LocFlow</span>
      </NuxtLink>
    </header>

    <main class="auth-main">
      <div class="auth-card">
        <h1 class="auth-title">Welcome back</h1>
        <p class="auth-subtitle">Sign in to your account to continue</p>

        <form class="auth-form" @submit.prevent>
          <div class="form-group">
            <label for="email">Email</label>
            <input 
              id="email" 
              type="email" 
              placeholder="you@company.com"
              autocomplete="email"
            >
          </div>

          <div class="form-group">
            <label for="password">Password</label>
            <input 
              id="password" 
              type="password" 
              placeholder="••••••••"
              autocomplete="current-password"
            >
          </div>

          <div class="form-options">
            <label class="checkbox-label">
              <input type="checkbox">
              <span>Remember me</span>
            </label>
            <a href="#" class="forgot-link">Forgot password?</a>
          </div>

          <button type="submit" class="btn btn-primary">Sign in</button>
        </form>

        <p class="auth-footer">
          Don't have an account? 
          <NuxtLink to="/register" class="link">Sign up for free</NuxtLink>
        </p>
      </div>
    </main>
  </div>
</template>

<style scoped>
.auth-page {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background: #f9fafb;
}

.auth-header {
  padding: 1.5rem;
  text-align: center;
}

.brand {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  text-decoration: none;
  color: #111827;
  font-weight: 700;
  font-size: 1.25rem;
}

.brand-icon {
  font-size: 1.5rem;
  color: #4f46e5;
}

.auth-main {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
}

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
}

.btn-primary {
  background: #4f46e5;
  color: white;
}

.btn-primary:hover {
  background: #4338ca;
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
