<script setup lang="ts">
const isMenuOpen = ref(false)

function toggleMenu() {
  isMenuOpen.value = !isMenuOpen.value
}

function closeMenu() {
  isMenuOpen.value = false
}
</script>

<template>
  <header class="navbar" role="banner">
    <nav class="navbar-container" role="navigation" aria-label="Main navigation">
      <NuxtLink to="/" class="brand" aria-label="Content Localization SaaS Home">
        <span class="brand-icon" aria-hidden="true">◈</span>
        <span class="brand-text">InterCopy</span>
      </NuxtLink>

      <button 
        class="menu-toggle" 
        :aria-expanded="isMenuOpen" 
        aria-controls="nav-menu"
        aria-label="Toggle navigation menu"
        @click="toggleMenu"
      >
        <span class="menu-icon" :class="{ open: isMenuOpen }">
          <span></span>
          <span></span>
          <span></span>
        </span>
      </button>

      <ul id="nav-menu" class="nav-links" :class="{ open: isMenuOpen }" @click="closeMenu">
        <li><a href="#features">Features</a></li>
        <li><a href="#benefits">Benefits</a></li>
        <li><a href="#pricing">Pricing</a></li>
      </ul>

      <div class="nav-actions" :class="{ open: isMenuOpen }">
        <ClientOnly><UiThemeToggle class="nav-theme-toggle" /></ClientOnly>
        <NuxtLink to="/login" class="btn btn-ghost">Log in</NuxtLink>
        <NuxtLink to="/register" class="btn btn-primary">Get Started</NuxtLink>
      </div>
    </nav>
  </header>
</template>

<style scoped>
.navbar {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  z-index: 100;
  background: color-mix(in srgb, var(--color-surface) 92%, transparent);
  backdrop-filter: blur(8px);
  border-bottom: 1px solid var(--color-border);
}

.navbar-container {
  max-width: 1200px;
  margin: 0 auto;
  padding: var(--spacing-3) var(--spacing-6);
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--spacing-8);
}

.brand {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
  text-decoration: none;
  color: var(--color-text-primary);
  font-weight: var(--font-weight-bold);
  font-size: var(--font-size-xl);
}

.brand-icon {
  font-size: var(--font-size-2xl);
  color: var(--color-primary-600);
}

.brand-text {
  letter-spacing: -0.02em;
}

.menu-toggle {
  display: none;
  background: none;
  border: none;
  padding: var(--spacing-2);
  cursor: pointer;
}

.menu-toggle:focus-visible {
  outline: 2px solid var(--color-primary-500);
  outline-offset: 2px;
}

.menu-icon {
  display: flex;
  flex-direction: column;
  gap: 5px;
  width: 24px;
}

.menu-icon span {
  display: block;
  height: 2px;
  background: var(--color-text-primary);
  border-radius: 2px;
  transition: all var(--transition-slow);
}

.menu-icon.open span:nth-child(1) {
  transform: rotate(45deg) translate(5px, 5px);
}

.menu-icon.open span:nth-child(2) {
  opacity: 0;
}

.menu-icon.open span:nth-child(3) {
  transform: rotate(-45deg) translate(5px, -5px);
}

.nav-links {
  display: flex;
  gap: var(--spacing-8);
  list-style: none;
  margin: 0;
  padding: 0;
}

.nav-links a {
  text-decoration: none;
  color: var(--color-gray-600);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  transition: color var(--transition-fast);
}

.nav-links a:hover {
  color: var(--color-gray-900);
}

.nav-links a:focus-visible {
  outline: 2px solid var(--color-primary-500);
  outline-offset: 2px;
  border-radius: var(--radius-sm);
}

.nav-actions {
  display: flex;
  gap: var(--spacing-3);
  align-items: center;
}

.nav-theme-toggle {
  margin-right: var(--spacing-1);
}

.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-2) var(--spacing-4);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  border-radius: var(--radius-lg);
  text-decoration: none;
  transition: all var(--transition-fast);
}

.btn:focus-visible {
  outline: 2px solid var(--color-primary-500);
  outline-offset: 2px;
}

.btn-ghost {
  background: transparent;
  color: var(--color-gray-700);
  border: 1px solid transparent;
}

.btn-ghost:hover {
  background: var(--color-gray-100);
}

.btn-primary {
  background: var(--color-primary-600);
  color: var(--color-white);
  border: 1px solid var(--color-primary-600);
}

.btn-primary:hover {
  background: var(--color-primary-700);
  border-color: var(--color-primary-700);
}

@media (max-width: 768px) {
  .menu-toggle {
    display: block;
    order: 3;
  }

  .nav-links {
    display: none;
    width: 100%;
    order: 4;
    flex-direction: column;
    background: var(--color-surface);
    padding: var(--spacing-2) var(--spacing-4) 0;
    border-top: 1px solid var(--color-border);
    gap: 0;
  }

  .nav-links.open {
    display: flex;
  }

  .nav-links li {
    padding: var(--spacing-3) 0;
    border-bottom: 1px solid var(--color-border);
  }

  .nav-links li:last-child {
    border-bottom: none;
  }

  .nav-actions {
    display: none;
    width: 100%;
    order: 5;
    flex-direction: column;
    background: var(--color-surface);
    padding: 0 var(--spacing-4) var(--spacing-4);
    gap: var(--spacing-2);
    border-bottom: 1px solid var(--color-border);
  }

  .nav-actions.open {
    display: flex;
  }

  .nav-actions .btn,
  .nav-actions .nav-theme-toggle {
    width: 100%;
  }

  .navbar-container {
    flex-wrap: wrap;
  }
}
</style>
