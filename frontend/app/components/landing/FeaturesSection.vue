<script setup lang="ts">
const features = [
  {
    icon: '◉',
    title: 'Translation Management',
    description: 'Organize translations in one place. Track progress, manage workflows, and maintain consistency across all languages.'
  },
  {
    icon: '⬡',
    title: 'Team Collaboration',
    description: 'Invite translators, reviewers, and editors. Set permissions and streamline your review process.'
  },
  {
    icon: '◎',
    title: 'Translation Memory',
    description: 'Leverage AI-powered suggestions from your existing translations. Save time and ensure consistency.'
  },
  {
    icon: '◇',
    title: 'Figma Integration',
    description: 'Sync translations directly with your Figma designs. Keep content in sync across design and development.'
  },
  {
    icon: '◈',
    title: 'API & Webhooks',
    description: 'Export translations in any format. Integrate with your CI/CD pipeline using our robust API.'
  },
  {
    icon: '⬢',
    title: 'Quality Assurance',
    description: 'Catch errors before they go live. Built-in checks for placeholders, missing translations, and more.'
  }
]
</script>

<template>
  <section id="features" class="features" aria-labelledby="features-heading">
    <div class="features-container">
      <div class="features-header">
        <h2 id="features-heading" class="features-title">Everything you need to go global</h2>
        <p class="features-subtitle">
          Built for modern teams shipping products to worldwide audiences.
        </p>
      </div>
      
      <div class="features-grid">
        <article 
          v-for="(feature, index) in features" 
          :key="index" 
          class="feature-card"
        >
          <div class="feature-icon" aria-hidden="true">{{ feature.icon }}</div>
          <h3 class="feature-title">{{ feature.title }}</h3>
          <p class="feature-description">{{ feature.description }}</p>
        </article>
      </div>
    </div>
  </section>
</template>

<style scoped>
.features {
  padding: 5rem 1.5rem;
  background: var(--color-surface);
}

.features-container {
  max-width: 1200px;
  margin: 0 auto;
}

.features-header {
  text-align: center;
  max-width: 640px;
  margin: 0 auto 4rem;
}

.features-title {
  font-size: 2.5rem;
  font-weight: 800;
  color: var(--color-text-primary);
  margin: 0 0 1rem;
  letter-spacing: -0.02em;
}

.features-subtitle {
  font-size: 1.125rem;
  color: var(--color-text-muted);
  margin: 0;
  line-height: 1.6;
}

.features-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 2rem;
}

.feature-card {
  padding: 2rem;
  background: var(--color-surface);
  border-radius: 1rem;
  border: 1px solid var(--color-gray-200);
  transition: all 0.3s ease;
}

.feature-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 12px 24px -8px rgba(0, 0, 0, 0.1);
  border-color: var(--color-gray-300);
}

.feature-icon {
  width: 48px;
  height: 48px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.5rem;
  background: var(--color-primary-100);
  color: var(--color-primary-600);
  border-radius: 0.75rem;
  margin-bottom: 1.25rem;
}

.feature-title {
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--color-text-primary);
  margin: 0 0 0.75rem;
}

.feature-description {
  font-size: 0.9375rem;
  color: var(--color-text-muted);
  line-height: 1.6;
  margin: 0;
}

@media (max-width: 1024px) {
  .features-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 640px) {
  .features {
    padding: 3rem 1rem;
  }

  .features-title {
    font-size: 2rem;
  }

  .features-grid {
    grid-template-columns: 1fr;
    gap: 1.5rem;
  }

  .feature-card {
    padding: 1.5rem;
  }
}
</style>
