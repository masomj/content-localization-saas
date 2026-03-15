<script setup lang="ts">
interface Props {
  tag?: 'section' | 'div' | 'main' | 'header' | 'footer'
  size?: 'sm' | 'md' | 'lg' | 'full'
  centered?: boolean
  background?: 'none' | 'light' | 'primary' | 'gradient'
}

withDefaults(defineProps<Props>(), {
  tag: 'section',
  size: 'lg',
  centered: true,
  background: 'none',
})

const tagName = defineProps<Props>().tag
</script>

<template>
  <component :is="tag" :class="['ui-section', `ui-section--${size}`, `ui-section--bg-${background}`, { 'ui-section--centered': centered }]">
    <div class="ui-section__inner">
      <slot />
    </div>
  </component>
</template>

<style scoped>
.ui-section {
  width: 100%;
}

.ui-section--centered .ui-section__inner {
  margin-left: auto;
  margin-right: auto;
}

.ui-section--sm .ui-section__inner {
  max-width: 640px;
}

.ui-section--md .ui-section__inner {
  max-width: 960px;
}

.ui-section--lg .ui-section__inner {
  max-width: 1200px;
}

.ui-section--full .ui-section__inner {
  max-width: 100%;
}

.ui-section__inner {
  width: 100%;
  padding-left: var(--spacing-4);
  padding-right: var(--spacing-4);
}

.ui-section--bg-light {
  background: var(--color-gray-50);
}

.ui-section--bg-primary {
  background: var(--color-primary-600);
  color: var(--color-white);
}

.ui-section--bg-gradient {
  background: linear-gradient(135deg, var(--color-primary-600) 0%, var(--color-primary-800) 100%);
  color: var(--color-white);
}

@media (min-width: 640px) {
  .ui-section__inner {
    padding-left: var(--spacing-6);
    padding-right: var(--spacing-6);
  }
}

@media (min-width: 1024px) {
  .ui-section__inner {
    padding-left: var(--spacing-8);
    padding-right: var(--spacing-8);
  }
}
</style>
