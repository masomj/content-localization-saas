<script setup lang="ts">
useSeoMeta({
  title: 'Sign Up - LocFlow',
  description: 'Start your free trial. No credit card required. Join thousands of teams shipping localized content faster.',
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
        <h1 class="auth-title">Create your account</h1>
        <p class="auth-subtitle">Start your 14-day free trial. No credit card required.</p>

        <form class="auth-form" @submit.prevent>
          <div class="form-row">
            <div class="form-group">
              <label for="firstName">First name</label>
              <input 
                id="firstName" 
                type="text" 
                placeholder="Jane"
                autocomplete="given-name"
              >
            </div>

            <div class="form-group">
              <label for="lastName">Last name</label>
              <input 
                id="lastName" 
                type="text" 
                placeholder="Doe"
                autocomplete="family-name"
              >
            </div>
          </div>

          <div class="form-group">
            <label for="email">Work email</label>
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
              placeholder="8+ characters"
              autocomplete="new-password"
            >
            <p class="form-hint">Must be at least 8 characters</p>
          </div>

          <div class="form-group">
            <label for="company">Company name</label>
            <input 
              id="company" 
              type="text" 
              placeholder="Acme Inc."
              autocomplete="organization"
            >
          </div>

          <button type="submit" class="btn btn-primary">Create account</button>

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
  max-width: 440px;
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

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
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

.form-hint {
  font-size: 0.75rem;
  color: #6b7280;
  margin: 0;
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

.terms {
  font-size: 0.8125rem;
  color: #6b7280;
  margin: 0;
  text-align: center;
  line-height: 1.5;
}

.terms a {
  color: #4f46e5;
}

.terms a:hover {
  text-decoration: underline;
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

@media (max-width: 480px) {
  .form-row {
    grid-template-columns: 1fr;
  }
}
</style>
