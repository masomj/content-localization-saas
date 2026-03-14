<script setup lang="ts">
</script>

<template>
  <section class="hero" aria-labelledby="hero-heading">
    <div class="hero-container">
      <div class="hero-content">
        <h1 id="hero-heading" class="hero-title">
          Translate your content.<br />
          <span class="highlight">Scale globally.</span>
        </h1>
        <p class="hero-subtitle">
          The all-in-one localization platform that helps teams manage translations, 
          collaborate with reviewers, and deliver localized content to every market.
        </p>
        <div class="hero-actions">
          <NuxtLink to="/register" class="btn btn-primary btn-lg">
            Start Free Trial
          </NuxtLink>
          <NuxtLink to="/login" class="btn btn-outline btn-lg">
            View Demo
          </NuxtLink>
        </div>
        <p class="hero-note">
          No credit card required · 14-day free trial · Cancel anytime
        </p>
      </div>
      <div class="hero-visual" aria-hidden="true">
        <div class="dashboard-preview">
          <div class="preview-header">
            <span class="dot"></span>
            <span class="dot"></span>
            <span class="dot"></span>
          </div>
          <div class="preview-content">
            <div class="preview-sidebar">
              <div class="sidebar-item active"></div>
              <div class="sidebar-item"></div>
              <div class="sidebar-item"></div>
              <div class="sidebar-item"></div>
            </div>
            <div class="preview-main">
              <div class="preview-nav">
                <div class="nav-pill"></div>
                <div class="nav-pill"></div>
              </div>
              <div class="preview-grid">
                <div class="grid-card"></div>
                <div class="grid-card"></div>
                <div class="grid-card"></div>
                <div class="grid-card"></div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<style scoped>
.hero {
  padding: 8rem 1.5rem 4rem;
  background: linear-gradient(180deg, #f9fafb 0%, #ffffff 100%);
  overflow: hidden;
}

.hero-container {
  max-width: 1200px;
  margin: 0 auto;
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 3rem;
  align-items: center;
}

.hero-content {
  max-width: 560px;
}

.hero-title {
  font-size: 3.5rem;
  font-weight: 800;
  line-height: 1.1;
  letter-spacing: -0.03em;
  color: #111827;
  margin: 0 0 1.5rem;
}

.hero-title .highlight {
  color: #4f46e5;
}

.hero-subtitle {
  font-size: 1.25rem;
  line-height: 1.6;
  color: #4b5563;
  margin: 0 0 2rem;
}

.hero-actions {
  display: flex;
  gap: 1rem;
  margin-bottom: 1.5rem;
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
  background: #4f46e5;
  color: white;
  border: 1px solid #4f46e5;
}

.btn-primary:hover {
  background: #4338ca;
  border-color: #4338ca;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(79, 70, 229, 0.25);
}

.btn-outline {
  background: transparent;
  color: #374151;
  border: 1px solid #d1d5db;
}

.btn-outline:hover {
  background: #f9fafb;
  border-color: #9ca3af;
}

.hero-note {
  font-size: 0.875rem;
  color: #6b7280;
  margin: 0;
}

.hero-visual {
  display: flex;
  justify-content: center;
  align-items: center;
}

.dashboard-preview {
  width: 100%;
  max-width: 520px;
  background: white;
  border-radius: 1rem;
  box-shadow: 
    0 25px 50px -12px rgba(0, 0, 0, 0.15),
    0 0 0 1px rgba(0, 0, 0, 0.05);
  overflow: hidden;
  aspect-ratio: 4 / 3;
}

.preview-header {
  display: flex;
  gap: 6px;
  padding: 0.75rem 1rem;
  background: #f3f4f6;
  border-bottom: 1px solid #e5e7eb;
}

.dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: #d1d5db;
}

.dot:nth-child(1) { background: #ef4444; }
.dot:nth-child(2) { background: #eab308; }
.dot:nth-child(3) { background: #22c55e; }

.preview-content {
  display: flex;
  height: calc(100% - 42px);
}

.preview-sidebar {
  width: 60px;
  background: #f9fafb;
  border-right: 1px solid #e5e7eb;
  padding: 1rem 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.75rem;
}

.sidebar-item {
  width: 32px;
  height: 32px;
  border-radius: 6px;
  background: #e5e7eb;
}

.sidebar-item.active {
  background: #4f46e5;
}

.preview-main {
  flex: 1;
  padding: 1rem;
}

.preview-nav {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.nav-pill {
  height: 24px;
  width: 80px;
  border-radius: 4px;
  background: #e5e7eb;
}

.nav-pill:first-child {
  background: #4f46e5;
  width: 60px;
}

.preview-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.75rem;
}

.grid-card {
  height: 60px;
  background: #f9fafb;
  border-radius: 6px;
  border: 1px solid #e5e7eb;
}

@media (max-width: 1024px) {
  .hero-container {
    grid-template-columns: 1fr;
    text-align: center;
  }

  .hero-content {
    max-width: 100%;
  }

  .hero-title {
    font-size: 2.5rem;
  }

  .hero-actions {
    justify-content: center;
  }

  .hero-note {
    text-align: center;
  }

  .hero-visual {
    order: -1;
  }
}

@media (max-width: 640px) {
  .hero {
    padding: 7rem 1rem 3rem;
  }

  .hero-title {
    font-size: 2rem;
  }

  .hero-subtitle {
    font-size: 1.0625rem;
  }

  .hero-actions {
    flex-direction: column;
  }

  .btn-lg {
    width: 100%;
  }
}
</style>
