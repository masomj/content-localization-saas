<script setup lang="ts">
const theme = useTheme()

const options = [
  { value: 'light', label: 'Light', icon: '☀️' },
  { value: 'dark', label: 'Dark', icon: '🌙' },
  { value: 'system', label: 'System', icon: '💻' },
] as const

type ThemeValue = 'light' | 'dark' | 'system'

const isOpen = ref(false)
const triggerRef = ref<HTMLButtonElement | null>(null)
const listboxRef = ref<HTMLUListElement | null>(null)
const focusedIndex = ref(-1)
const listboxId = 'theme-toggle-listbox'

const currentOption = computed(
  () => options.find(o => o.value === theme.preference.value) ?? options[2]
)

function open() {
  isOpen.value = true
  focusedIndex.value = options.findIndex(o => o.value === theme.preference.value)
  nextTick(() => {
    const items = listboxRef.value?.querySelectorAll<HTMLElement>('[role="option"]')
    items?.[focusedIndex.value]?.focus()
  })
}

function close() {
  isOpen.value = false
  triggerRef.value?.focus()
}

function toggle() {
  isOpen.value ? close() : open()
}

function select(value: ThemeValue) {
  theme.setThemePreference(value)
  close()
}

function onTriggerKeydown(e: KeyboardEvent) {
  if (e.key === 'ArrowDown' || e.key === 'Enter' || e.key === ' ') {
    e.preventDefault()
    open()
  }
}

function onOptionKeydown(e: KeyboardEvent, index: number) {
  if (e.key === 'ArrowDown') {
    e.preventDefault()
    const next = (index + 1) % options.length
    const items = listboxRef.value?.querySelectorAll<HTMLElement>('[role="option"]')
    items?.[next]?.focus()
    focusedIndex.value = next
  } else if (e.key === 'ArrowUp') {
    e.preventDefault()
    const prev = (index - 1 + options.length) % options.length
    const items = listboxRef.value?.querySelectorAll<HTMLElement>('[role="option"]')
    items?.[prev]?.focus()
    focusedIndex.value = prev
  } else if (e.key === 'Enter' || e.key === ' ') {
    e.preventDefault()
    select(options[index].value)
  } else if (e.key === 'Escape' || e.key === 'Tab') {
    close()
  }
}

function onClickOutside(e: MouseEvent) {
  const target = e.target as Node
  if (!triggerRef.value?.contains(target) && !listboxRef.value?.contains(target)) {
    close()
  }
}

onMounted(() => document.addEventListener('mousedown', onClickOutside))
onUnmounted(() => document.removeEventListener('mousedown', onClickOutside))
</script>

<template>
  <div class="tt-root" data-testid="theme-toggle">
    <!-- Trigger button -->
    <button
      ref="triggerRef"
      class="tt-trigger"
      type="button"
      :aria-expanded="isOpen"
      aria-haspopup="listbox"
      :aria-controls="isOpen ? listboxId : undefined"
      :aria-label="`Theme: ${currentOption.label}`"
      @click="toggle"
      @keydown="onTriggerKeydown"
    >
      <span class="tt-trigger__icon" aria-hidden="true">{{ currentOption.icon }}</span>
      <span class="tt-trigger__label">{{ currentOption.label }}</span>
      <svg
        class="tt-trigger__chevron"
        :class="{ 'tt-trigger__chevron--open': isOpen }"
        width="16"
        height="16"
        viewBox="0 0 20 20"
        fill="none"
        aria-hidden="true"
      >
        <path d="M6 8l4 4 4-4" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
      </svg>
    </button>

    <!-- Dropdown listbox -->
    <Transition name="tt-fade">
      <ul
        v-if="isOpen"
        :id="listboxId"
        ref="listboxRef"
        class="tt-listbox"
        role="listbox"
        :aria-label="'Theme'"
        :aria-activedescendant="isOpen ? `tt-option-${theme.preference.value}` : undefined"
      >
        <li
          v-for="(option, i) in options"
          :id="`tt-option-${option.value}`"
          :key="option.value"
          class="tt-option"
          :class="{ 'tt-option--selected': option.value === theme.preference.value }"
          role="option"
          :aria-selected="option.value === theme.preference.value"
          tabindex="-1"
          @click="select(option.value)"
          @keydown="onOptionKeydown($event, i)"
        >
          <span class="tt-option__icon" aria-hidden="true">{{ option.icon }}</span>
          <span class="tt-option__label">{{ option.label }}</span>
          <svg
            v-if="option.value === theme.preference.value"
            class="tt-option__check"
            width="16"
            height="16"
            viewBox="0 0 20 20"
            fill="none"
            aria-hidden="true"
          >
            <path d="M4 10l4.5 4.5L16 7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
          </svg>
        </li>
      </ul>
    </Transition>
  </div>
</template>

<style scoped>
/* ── Root ─────────────────────────────────────── */
.tt-root {
  position: relative;
  display: inline-block;
}

/* ── Trigger ──────────────────────────────────── */
.tt-trigger {
  display: inline-flex;
  align-items: center;
  gap: var(--spacing-2);
  height: 2.25rem;
  padding: 0 var(--spacing-3);
  background: var(--color-surface);
  color: var(--color-text-primary);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  font-family: var(--font-family-sans);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  cursor: pointer;
  white-space: nowrap;
  transition:
    border-color var(--transition-fast),
    background-color var(--transition-fast),
    box-shadow var(--transition-fast);
}

.tt-trigger:hover {
  background: var(--color-background);
  border-color: var(--color-border);
}

.tt-trigger:focus-visible {
  outline: none;
  border-color: var(--color-primary-500);
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.15);
}

.tt-trigger__icon {
  font-size: var(--font-size-sm);
  line-height: 1;
}

.tt-trigger__label {
  line-height: 1;
}

.tt-trigger__chevron {
  color: var(--color-text-muted);
  flex-shrink: 0;
  transition: transform var(--transition-fast);
}

.tt-trigger__chevron--open {
  transform: rotate(180deg);
}

/* ── Listbox ──────────────────────────────────── */
.tt-listbox {
  position: absolute;
  top: calc(100% + var(--spacing-2));
  right: 0;
  z-index: var(--z-dropdown);
  min-width: 9rem;
  margin: 0;
  padding: var(--spacing-1);
  list-style: none;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-lg);
  outline: none;
}

/* ── Options ──────────────────────────────────── */
.tt-option {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
  padding: var(--spacing-2) var(--spacing-3);
  border-radius: var(--radius-md);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-secondary);
  cursor: pointer;
  outline: none;
  transition:
    background-color var(--transition-fast),
    color var(--transition-fast);
}

.tt-option:hover,
.tt-option:focus {
  background: var(--color-primary-50);
  color: var(--color-text-primary);
}

.tt-option--selected {
  color: var(--color-primary-600);
  background: var(--color-primary-50);
}

.tt-option__icon {
  font-size: var(--font-size-sm);
  line-height: 1;
  flex-shrink: 0;
}

.tt-option__label {
  flex: 1;
}

.tt-option__check {
  color: var(--color-primary-500);
  flex-shrink: 0;
}

/* ── Transition ───────────────────────────────── */
.tt-fade-enter-active,
.tt-fade-leave-active {
  transition:
    opacity var(--transition-fast),
    transform var(--transition-fast);
}

.tt-fade-enter-from,
.tt-fade-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}
</style>
