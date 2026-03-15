<script setup lang="ts">
</script>

<template>
  <section class="cta" aria-labelledby="cta-heading">
    <div class="cta-container">
      <h2 id="cta-heading" class="cta-title">Ready to go global?</h2>
      <p class="cta-subtitle">
        Join thousands of teams shipping localized content faster.
      </p>
      <div class="cta-actions">
        <NuxtLink to="/register" class="btn btn-primary btn-lg">
          Start Free Trial
        </NuxtLink>
        <NuxtLink to="/login" class="btn btn-outline btn-lg">
          Talk to Sales
        </NuxtLink>
      </div>
    </div>
  </section>
</template>

<style scoped>
.cta {
  padding: 5rem 1.5rem;
  background: linear-gradient(135deg, var(--color-primary-600) 0%, #7c3aed 100%);
}

.cta-container {
  max-width: 720px;
  margin: 0 auto;
  text-align: center;
}

.cta-title {
  font-size: 2.5rem;
  font-weight: 800;
  color: white;
  margin: 0 0 1rem;
  letter-spacing: -0.02em;
}

.cta-subtitle {
  font-size: 1.25rem;
  color: rgba(255, 255, 255, 0.85);
  margin: 0 0 2rem;
  line-height: 1.6;
}

.cta-actions {
  display: flex;
  gap: 1rem;
  justify-content: center;
}

.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0.875rem 1.5rem;
  font-size: 1rem;
  font-weight: 600;
  border-radius: 0.5rem;
  text-decoration: none;
  transition: all 0.2s;
}

.btn-lg {
  padding: 1rem 2rem;
  font-size: 1.0625rem;
}

.btn-primary {
  background: white;
  color: var(--color-primary-600);
  border: 1px solid white;
}

.btn-primary:hover {
  background: var(--color-surface);
  transform: translateY(-2px);
  box-shadow: 0 8px 20px rgba(0, 0, 0, 0.2);
}

.btn-outline {
  background: transparent;
  color: white;
  border: 1px solid rgba(255, 255, 255, 0.5);
}

.btn-outline:hover {
  background: rgba(255, 255, 255, 0.1);
  border-color: white;
}

@media (max-width: 640px) {
  .cta {
    padding: 3rem 1rem;
  }

  .cta-title {
    font-size: 2rem;
  }

  .cta-subtitle {
    font-size: 1.0625rem;
  }

  .cta-actions {
    flex-direction: column;
  }

  .btn-lg {
    width: 100%;
  }
}
</style>
