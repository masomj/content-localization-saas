<script setup lang="ts">
const currentYear = new Date().getFullYear()
</script>

<template>
  <footer class="footer" role="contentinfo">
    <div class="footer-container">
      <div class="footer-main">
        <NuxtLink to="/" class="brand" aria-label="InterCopy Home">
          <span class="brand-icon" aria-hidden="true">◈</span>
          <span class="brand-text">InterCopy</span>
        </NuxtLink>
        <p class="brand-description">
          The all-in-one localization platform for modern teams.
        </p>
      </div>

      <div class="footer-bottom">
        <p class="copyright">
          &copy; {{ currentYear }} InterCopy. All rights reserved.
        </p>
      </div>
    </div>
  </footer>
</template>

<style scoped>
.footer {
  background: #0f172a;
  color: var(--color-gray-300);
  padding: 4rem 1.5rem 2rem;
}

.footer-container {
  max-width: 1200px;
  margin: 0 auto;
}

.footer-main {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
  padding-bottom: 2rem;
  border-bottom: 1px solid var(--color-text-secondary);
  text-align: center;
}

.brand {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  text-decoration: none;
  color: var(--color-white);
  font-weight: 700;
  font-size: 1.25rem;
}

.brand-icon {
  font-size: 1.5rem;
  color: #818cf8;
}

.brand-description {
  font-size: 0.9375rem;
  line-height: 1.6;
  color: var(--color-gray-400);
  margin: 0;
  max-width: 420px;
}

.footer-bottom {
  display: flex;
  justify-content: center;
  align-items: center;
  padding-top: 2rem;
}

.copyright {
  font-size: 0.875rem;
  color: var(--color-text-muted);
  margin: 0;
}

@media (max-width: 640px) {
  .footer {
    padding: 3rem 1rem 1.5rem;
  }
}
</style>
