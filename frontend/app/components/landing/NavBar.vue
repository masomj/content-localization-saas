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
        <span class="brand-text">LocFlow</span>
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
  background: rgba(255, 255, 255, 0.95);
  backdrop-filter: blur(8px);
  border-bottom: 1px solid #e5e7eb;
}

.navbar-container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0.875rem 1.5rem;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 2rem;
}

.brand {
  display: flex;
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

.brand-text {
  letter-spacing: -0.02em;
}

.menu-toggle {
  display: none;
  background: none;
  border: none;
  padding: 0.5rem;
  cursor: pointer;
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
  background: #111827;
  border-radius: 2px;
  transition: all 0.3s ease;
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
  gap: 2rem;
  list-style: none;
  margin: 0;
  padding: 0;
}

.nav-links a {
  text-decoration: none;
  color: #4b5563;
  font-size: 0.9375rem;
  font-weight: 500;
  transition: color 0.2s;
}

.nav-links a:hover {
  color: #111827;
}

.nav-actions {
  display: flex;
  gap: 0.75rem;
  align-items: center;
}

.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0.5rem 1rem;
  font-size: 0.9375rem;
  font-weight: 500;
  border-radius: 0.5rem;
  text-decoration: none;
  transition: all 0.2s;
}

.btn-ghost {
  background: transparent;
  color: #374151;
  border: 1px solid transparent;
}

.btn-ghost:hover {
  background: #f3f4f6;
}

.btn-primary {
  background: #4f46e5;
  color: white;
  border: 1px solid #4f46e5;
}

.btn-primary:hover {
  background: #4338ca;
  border-color: #4338ca;
}

@media (max-width: 768px) {
  .menu-toggle {
    display: block;
    order: 3;
  }

  .nav-links {
    display: none;
    position: absolute;
    top: 100%;
    left: 0;
    right: 0;
    flex-direction: column;
    background: white;
    padding: 1rem;
    border-bottom: 1px solid #e5e7eb;
    gap: 0;
  }

  .nav-links.open {
    display: flex;
  }

  .nav-links li {
    padding: 0.75rem 0;
    border-bottom: 1px solid #f3f4f6;
  }

  .nav-links li:last-child {
    border-bottom: none;
  }

  .nav-actions {
    display: none;
    position: absolute;
    top: calc(100% + 150px);
    left: 0;
    right: 0;
    flex-direction: column;
    background: white;
    padding: 1rem;
    gap: 0.5rem;
  }

  .nav-actions.open {
    display: flex;
  }

  .nav-actions .btn {
    width: 100%;
  }
}
</style>
