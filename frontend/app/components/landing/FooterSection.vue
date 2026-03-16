<script setup lang="ts">
const currentYear = new Date().getFullYear()

const footerLinks = {
  Product: ['Features', 'Pricing', 'Integrations', 'Changelog', 'Roadmap'],
  Company: ['About', 'Blog', 'Careers', 'Press', 'Partners'],
  Resources: ['Documentation', 'Help Center', 'API Reference', 'Community', 'Status'],
  Legal: ['Privacy', 'Terms', 'Security', 'Cookies', 'GDPR']
}
</script>

<template>
  <footer class="footer" role="contentinfo">
    <div class="footer-container">
      <div class="footer-main">
        <div class="footer-brand">
          <NuxtLink to="/" class="brand" aria-label="LocFlow Home">
            <span class="brand-icon" aria-hidden="true">◈</span>
            <span class="brand-text">LocFlow</span>
          </NuxtLink>
          <p class="brand-description">
            The all-in-one localization platform for modern teams.
          </p>
        </div>

        <nav class="footer-nav" aria-label="Footer navigation">
          <div v-for="(links, category) in footerLinks" :key="category" class="footer-column">
            <h3 class="footer-heading">{{ category }}</h3>
            <ul class="footer-list">
              <li v-for="link in links" :key="link">
                <a href="#" class="footer-link">{{ link }}</a>
              </li>
            </ul>
          </div>
        </nav>
      </div>

      <div class="footer-bottom">
        <p class="copyright">
          &copy; {{ currentYear }} LocFlow. All rights reserved.
        </p>
        <div class="footer-social" aria-label="Social media links">
          <a href="#" aria-label="Twitter">
            <svg width="20" height="20" fill="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z"/>
            </svg>
          </a>
          <a href="#" aria-label="GitHub">
            <svg width="20" height="20" fill="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path d="M12 0C5.374 0 0 5.373 0 12c0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23A11.509 11.509 0 0112 5.803c1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576C20.566 21.797 24 17.3 24 12c0-6.627-5.373-12-12-12z"/>
            </svg>
          </a>
          <a href="#" aria-label="LinkedIn">
            <svg width="20" height="20" fill="currentColor" viewBox="0 0 24 24" aria-hidden="true">
              <path d="M20.447 20.452h-3.554v-5.569c0-1.328-.027-3.037-1.852-3.037-1.853 0-2.136 1.445-2.136 2.939v5.667H9.351V9h3.414v1.561h.046c.477-.9 1.637-1.85 3.37-1.85 3.601 0 4.267 2.37 4.267 5.455v6.286zM5.337 7.433c-1.144 0-2.063-.926-2.063-2.065 0-1.138.92-2.063 2.063-2.063 1.14 0 2.064.925 2.064 2.063 0 1.139-.925 2.065-2.064 2.065zm1.782 13.019H3.555V9h3.564v11.452zM22.225 0H1.771C.792 0 0 .774 0 1.729v20.542C0 23.227.792 24 1.771 24h20.451C23.2 24 24 23.227 24 22.271V1.729C24 .774 23.2 0 22.222 0h.003z"/>
            </svg>
          </a>
        </div>
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
  display: grid;
  grid-template-columns: 1fr 2fr;
  gap: 4rem;
  padding-bottom: 3rem;
  border-bottom: 1px solid var(--color-text-secondary);
}

.footer-brand {
  max-width: 280px;
}

.brand {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  text-decoration: none;
  color: var(--color-white);
  font-weight: 700;
  font-size: 1.25rem;
  margin-bottom: 1rem;
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
}

.footer-nav {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 2rem;
}

.footer-heading {
  font-size: 0.875rem;
  font-weight: 700;
  color: var(--color-white);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  margin: 0 0 1rem;
}

.footer-list {
  list-style: none;
  margin: 0;
  padding: 0;
}

.footer-list li {
  margin-bottom: 0.75rem;
}

.footer-link {
  color: var(--color-gray-400);
  text-decoration: none;
  font-size: 0.9375rem;
  transition: color 0.2s;
}

.footer-link:hover {
  color: var(--color-white);
}

.footer-bottom {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding-top: 2rem;
}

.copyright {
  font-size: 0.875rem;
  color: var(--color-text-muted);
  margin: 0;
}

.footer-social {
  display: flex;
  gap: 1rem;
}

.footer-social a {
  color: var(--color-gray-400);
  transition: color 0.2s;
}

.footer-social a:hover {
  color: var(--color-white);
}

@media (max-width: 1024px) {
  .footer-main {
    grid-template-columns: 1fr;
    gap: 3rem;
  }

  .footer-brand {
    max-width: 100%;
    text-align: center;
  }

  .footer-nav {
    grid-template-columns: repeat(2, 1fr);
    text-align: center;
  }
}

@media (max-width: 640px) {
  .footer {
    padding: 3rem 1rem 1.5rem;
  }

  .footer-nav {
    grid-template-columns: 1fr;
    gap: 2rem;
  }

  .footer-bottom {
    flex-direction: column;
    gap: 1rem;
    text-align: center;
  }
}
</style>

