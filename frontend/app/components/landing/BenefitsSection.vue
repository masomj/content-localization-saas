<script setup lang="ts">
const benefits = [
  {
    metric: '50%',
    label: 'Faster time-to-market',
    description: 'Streamline your localization workflow and ship to new markets faster.'
  },
  {
    metric: '30%',
    label: 'Reduction in translation costs',
    description: 'Leverage translation memory and AI suggestions to minimize redundant work.'
  },
  {
    metric: '99.9%',
    label: 'Uptime guarantee',
    description: 'Enterprise-grade infrastructure you can rely on.'
  }
]

const testimonials = [
  {
    quote: 'LocFlow transformed how we handle translations. Our team ships localized content 3x faster.',
    author: 'Sarah Chen',
    role: 'Product Lead at TechCorp'
  },
  {
    quote: 'The Figma integration alone has saved us countless hours of manual copy-pasting.',
    author: 'Marcus Johnson',
    role: 'Design Manager at StartupXYZ'
  }
]

const trustedBy = ['Acme Corp', 'Globex', 'Initech', 'Umbrella', 'Stark Industries']
</script>

<template>
  <section id="benefits" class="benefits" aria-labelledby="benefits-heading">
    <div class="benefits-container">
      <div class="benefits-header">
        <h2 id="benefits-heading" class="benefits-title">Why teams choose LocFlow</h2>
        <p class="benefits-subtitle">
          Join hundreds of companies scaling their global content.
        </p>
      </div>

      <div class="benefits-metrics">
        <div 
          v-for="(benefit, index) in benefits" 
          :key="index" 
          class="metric-card"
        >
          <div class="metric-value">{{ benefit.metric }}</div>
          <div class="metric-label">{{ benefit.label }}</div>
          <p class="metric-description">{{ benefit.description }}</p>
        </div>
      </div>

      <div class="trusted-by" aria-label="Trusted by companies">
        <p class="trusted-label">Trusted by teams at</p>
        <div class="trusted-logos">
          <span v-for="(company, index) in trustedBy" :key="index" class="trusted-company">
            {{ company }}
          </span>
        </div>
      </div>

      <div class="testimonials">
        <blockquote 
          v-for="(testimonial, index) in testimonials" 
          :key="index" 
          class="testimonial"
        >
          <p class="testimonial-quote">"{{ testimonial.quote }}"</p>
          <footer class="testimonial-author">
            <cite>{{ testimonial.author }}</cite>
            <span>{{ testimonial.role }}</span>
          </footer>
        </blockquote>
      </div>
    </div>
  </section>
</template>

<style scoped>
.benefits {
  padding: 5rem 1.5rem;
  background: var(--color-surface);
}

.benefits-container {
  max-width: 1200px;
  margin: 0 auto;
}

.benefits-header {
  text-align: center;
  max-width: 640px;
  margin: 0 auto 4rem;
}

.benefits-title {
  font-size: 2.5rem;
  font-weight: 800;
  color: var(--color-text-primary);
  margin: 0 0 1rem;
  letter-spacing: -0.02em;
}

.benefits-subtitle {
  font-size: 1.125rem;
  color: var(--color-text-muted);
  margin: 0;
  line-height: 1.6;
}

.benefits-metrics {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 2rem;
  margin-bottom: 4rem;
}

.metric-card {
  text-align: center;
  padding: 2rem;
  background: var(--color-background);
  border-radius: 1rem;
  border: 1px solid var(--color-border);
}

.metric-value {
  font-size: 3.5rem;
  font-weight: 800;
  color: var(--color-primary-600);
  line-height: 1;
  margin-bottom: 0.75rem;
}

.metric-label {
  font-size: 1.125rem;
  font-weight: 700;
  color: var(--color-text-primary);
  margin-bottom: 0.5rem;
}

.metric-description {
  font-size: 0.9375rem;
  color: var(--color-text-muted);
  margin: 0;
}

.trusted-by {
  text-align: center;
  padding: 3rem 0;
  border-top: 1px solid var(--color-gray-200);
  border-bottom: 1px solid var(--color-gray-200);
  margin-bottom: 4rem;
}

.trusted-label {
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--color-gray-400);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  margin: 0 0 1.5rem;
}

.trusted-logos {
  display: flex;
  justify-content: center;
  gap: 3rem;
  flex-wrap: wrap;
}

.trusted-company {
  font-size: 1.25rem;
  font-weight: 600;
  color: var(--color-text-muted);
}

.testimonials {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 2rem;
}

.testimonial {
  margin: 0;
  padding: 2rem;
  background: var(--color-background);
  border-radius: 1rem;
  border: 1px solid var(--color-border);
}

.testimonial-quote {
  font-size: 1.125rem;
  line-height: 1.7;
  color: var(--color-text-secondary);
  margin: 0 0 1.5rem;
  font-style: italic;
}

.testimonial-author {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.testimonial-author cite {
  font-style: normal;
  font-weight: 700;
  color: var(--color-text-primary);
}

.testimonial-author span {
  font-size: 0.875rem;
  color: var(--color-text-muted);
}

@media (max-width: 1024px) {
  .benefits-metrics {
    grid-template-columns: 1fr;
    gap: 1.5rem;
  }

  .trusted-logos {
    gap: 1.5rem;
  }

  .testimonials {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 640px) {
  .benefits {
    padding: 3rem 1rem;
  }

  .benefits-title {
    font-size: 2rem;
  }

  .metric-value {
    font-size: 2.5rem;
  }

  .trusted-logos {
    gap: 1rem;
  }

  .trusted-company {
    font-size: 1rem;
  }
}
</style>

