<script setup lang="ts">
import AppEmptyState from '~/components/AppEmptyState.vue'
import AppSkeleton from '~/components/AppSkeleton.vue'
import UiButton from '~/components/ui/Button.vue'
import UiSelect from '~/components/ui/Select.vue'
import UiCard from '~/components/ui/Card.vue'
import { reviewClient } from '~/api/reviewClient'
import type { ReviewQueueItem } from '~/api/types'

definePageMeta({
  layout: 'app',
})

useSeoMeta({
  title: 'Review - LocFlow',
})

const auth = useAuth()
const router = useRouter()

const isLoading = ref(false)
const errorMessage = ref('')
const queueItems = ref<ReviewQueueItem[]>([])
const filter = ref('all')

const filterOptions = [
  { value: 'all', label: 'All items' },
  { value: 'assigned', label: 'Assigned to me' },
  { value: 'unassigned', label: 'Unassigned' },
]

const filteredItems = computed(() => {
  const userEmail = auth.user.value?.email ?? ''
  if (filter.value === 'assigned') {
    return queueItems.value.filter(item => item.reviewAssigneeEmail === userEmail)
  }
  if (filter.value === 'unassigned') {
    return queueItems.value.filter(item => !item.reviewAssigneeEmail)
  }
  return queueItems.value
})

function verdictLabel(verdict: string | null): string {
  if (!verdict) return ''
  switch (verdict) {
    case 'approved': return 'Approved'
    case 'changes_requested': return 'Changes Requested'
    case 'comment': return 'Comment'
    default: return verdict
  }
}

function verdictClass(verdict: string | null): string {
  if (!verdict) return ''
  switch (verdict) {
    case 'approved': return 'verdict-badge--approved'
    case 'changes_requested': return 'verdict-badge--changes-requested'
    case 'comment': return 'verdict-badge--comment'
    default: return ''
  }
}

function verdictIcon(verdict: string | null): string {
  if (!verdict) return ''
  switch (verdict) {
    case 'approved': return '\u2713'
    case 'changes_requested': return '\u2717'
    case 'comment': return '\u{1F4AC}'
    default: return ''
  }
}

function truncateSource(source: string, maxLen = 120): string {
  if (source.length <= maxLen) return source
  return source.slice(0, maxLen) + '\u2026'
}

function openReview(item: ReviewQueueItem) {
  router.push(`/app/review/${item.id}`)
}

async function loadQueue() {
  isLoading.value = true
  errorMessage.value = ''
  try {
    const email = auth.user.value?.email ?? ''
    queueItems.value = await reviewClient.getQueue(email)
  } catch (err: any) {
    errorMessage.value = err?.message ?? 'Failed to load review queue'
  } finally {
    isLoading.value = false
  }
}

onMounted(loadQueue)
</script>

<template>
  <div class="review-page">
    <header class="page-header">
      <div>
        <h1>Review</h1>
        <p class="page-subtitle">Content items waiting for your review</p>
      </div>
      <div class="page-header__actions">
        <UiSelect
          v-model="filter"
          :options="filterOptions"
          label="Filter"
          name="queue-filter"
        />
      </div>
    </header>

    <div v-if="errorMessage" class="queue-error" role="alert">
      {{ errorMessage }}
    </div>

    <div v-if="isLoading" class="queue-list">
      <UiCard v-for="i in 3" :key="i" padding="md">
        <AppSkeleton :lines="3" height="1rem" />
      </UiCard>
    </div>

    <AppEmptyState
      v-else-if="filteredItems.length === 0"
      title="No items pending review"
      description="Content items that need review will appear here. Check back later or adjust your filter."
    >
      <template #action>
        <UiButton variant="secondary" @click="filter = 'all'">Show all items</UiButton>
      </template>
    </AppEmptyState>

    <div v-else class="queue-list" role="list">
      <UiCard
        v-for="item in filteredItems"
        :key="item.id"
        padding="none"
        class="queue-card"
        role="listitem"
      >
        <div class="queue-card__body">
          <div class="queue-card__header">
            <code class="queue-card__key">{{ item.key }}</code>
            <span class="status-badge">{{ item.status }}</span>
          </div>

          <p class="queue-card__source">{{ truncateSource(item.source) }}</p>

          <div class="queue-card__meta">
            <span v-if="item.reviewAssigneeEmail" class="queue-card__assignee">
              Assigned to: {{ item.reviewAssigneeEmail }}
            </span>
            <span v-else class="queue-card__assignee queue-card__assignee--unassigned">
              Unassigned
            </span>

            <span class="queue-card__counts">
              {{ item.commentCount }} comment{{ item.commentCount !== 1 ? 's' : '' }}
              &middot;
              {{ item.reviewCount }} review{{ item.reviewCount !== 1 ? 's' : '' }}
            </span>

            <span
              v-if="item.latestReviewVerdict"
              :class="['verdict-badge', verdictClass(item.latestReviewVerdict)]"
            >
              <span class="verdict-badge__icon" aria-hidden="true">{{ verdictIcon(item.latestReviewVerdict) }}</span>
              {{ verdictLabel(item.latestReviewVerdict) }}
            </span>
          </div>
        </div>

        <div class="queue-card__action">
          <UiButton size="sm" @click="openReview(item)">Open Review</UiButton>
        </div>
      </UiCard>
    </div>
  </div>
</template>

<style scoped>
.review-page {
  max-width: 1200px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: var(--spacing-6);
  gap: var(--spacing-4);
  flex-wrap: wrap;
}

.page-header h1 {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-semibold);
  color: var(--color-gray-900);
  margin: 0 0 var(--spacing-1) 0;
}

.page-subtitle {
  color: var(--color-gray-500);
  margin: 0;
}

.page-header__actions {
  flex-shrink: 0;
  min-width: 180px;
}

.queue-error {
  background: var(--color-error);
  color: var(--color-white);
  padding: var(--spacing-3) var(--spacing-4);
  border-radius: var(--radius-lg);
  margin-bottom: var(--spacing-4);
  font-size: var(--font-size-sm);
}

.queue-list {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-3);
}

.queue-card {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--spacing-4);
  padding: var(--spacing-4) var(--spacing-5);
  transition: box-shadow var(--transition-fast);
}

.queue-card:hover {
  box-shadow: var(--shadow-md);
}

.queue-card__body {
  flex: 1;
  min-width: 0;
}

.queue-card__header {
  display: flex;
  align-items: center;
  gap: var(--spacing-3);
  margin-bottom: var(--spacing-2);
}

.queue-card__key {
  font-family: var(--font-family-mono);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-gray-900);
  background: var(--color-gray-100);
  padding: var(--spacing-1) var(--spacing-2);
  border-radius: var(--radius-sm);
}

.status-badge {
  display: inline-block;
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  padding: var(--spacing-1) var(--spacing-2);
  border-radius: var(--radius-sm);
  background: var(--color-primary-50);
  color: var(--color-primary-700);
  text-transform: capitalize;
}

.queue-card__source {
  font-size: var(--font-size-sm);
  color: var(--color-gray-600);
  margin: 0 0 var(--spacing-2) 0;
  line-height: var(--line-height-normal);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.queue-card__meta {
  display: flex;
  align-items: center;
  gap: var(--spacing-3);
  flex-wrap: wrap;
  font-size: var(--font-size-xs);
  color: var(--color-gray-500);
}

.queue-card__assignee--unassigned {
  font-style: italic;
}

.queue-card__counts {
  color: var(--color-gray-400);
}

.queue-card__action {
  flex-shrink: 0;
}

/* Verdict badges */
.verdict-badge {
  display: inline-flex;
  align-items: center;
  gap: var(--spacing-1);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  padding: var(--spacing-1) var(--spacing-2);
  border-radius: var(--radius-sm);
}

.verdict-badge__icon {
  font-size: var(--font-size-xs);
}

.verdict-badge--approved {
  background: #dcfce7;
  color: #166534;
}

.verdict-badge--changes-requested {
  background: #fee2e2;
  color: #991b1b;
}

.verdict-badge--comment {
  background: var(--color-gray-100);
  color: var(--color-gray-700);
}

@media (max-width: 640px) {
  .queue-card {
    flex-direction: column;
    align-items: flex-start;
  }

  .queue-card__action {
    align-self: flex-end;
  }

  .page-header {
    flex-direction: column;
  }

  .page-header__actions {
    width: 100%;
  }
}
</style>
