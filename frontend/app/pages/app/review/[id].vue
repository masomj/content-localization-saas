<script setup lang="ts">
import AppSkeleton from '~/components/AppSkeleton.vue'
import UiButton from '~/components/ui/Button.vue'
import UiCard from '~/components/ui/Card.vue'
import { reviewClient } from '~/api/reviewClient'
import { screenshotsClient } from '~/api/screenshotsClient'
import { translationClient } from '~/api/translationClient'
import type {
  DiscussionComment,
  DiscussionThread,
  TimelineEntry,
  ContentItemScreenshot,
  ReviewQueueItem,
  LanguageTask,
} from '~/api/types'

definePageMeta({
  layout: 'app',
})

const route = useRoute()
const router = useRouter()
const auth = useAuth()
const { screenshots: screenshotsEnabled } = useFeatureFlags()

const contentItemId = computed(() => route.params.id as string)

useSeoMeta({
  title: () => `Review ${itemKey.value || ''} - InterCopy`,
})

/* ------------------------------------------------------------------ */
/*  State                                                              */
/* ------------------------------------------------------------------ */
const isLoading = ref(true)
const errorMessage = ref('')

const timelineExpanded = ref(true)
const timeline = ref<TimelineEntry[]>([])
const threads = ref<DiscussionThread[]>([])
const threadComments = ref<Record<string, DiscussionComment[]>>({})
const expandedThreads = ref<Set<string>>(new Set())

const itemKey = ref('')
const itemSource = ref('')
const itemStatus = ref('')
const itemAssignee = ref('')
const itemLocales = ref<string[]>([])

const reviewBody = ref('')
const isSubmitting = ref(false)
const submitError = ref('')

const contextScreenshots = ref<ContentItemScreenshot[]>([])
const contextExpanded = ref(true)
const contextIndex = ref(0)
const contextPanelWidth = ref(320)
const isResizingContext = ref(false)

const replyingTo = ref<{ threadId: string; parentCommentId?: string } | null>(null)
const replyBody = ref('')
const isReplying = ref(false)

/* ------------------------------------------------------------------ */
/*  Derived state                                                      */
/* ------------------------------------------------------------------ */
const userRole = computed(() => {
  const memberships = auth.user.value?.workspaces ?? []
  const orgId = auth.organization.value?.id
  if (!orgId) return ''
  const membership = memberships.find((m: any) => m.id === orgId)
  return membership?.role ?? ''
})

const canReview = computed(() => {
  const role = userRole.value
  return role === 'Reviewer' || role === 'Admin'
})

const localeLabel = computed(() => itemLocales.value.length === 1 ? 'Locale' : 'Locales')
const localeSummary = computed(() => itemLocales.value.join(', '))
const showHeaderSource = computed(() => {
  return !!itemSource.value.trim() && itemSource.value.trim() !== itemKey.value.trim()
})

/* ------------------------------------------------------------------ */
/*  Data loading                                                       */
/* ------------------------------------------------------------------ */
async function loadData() {
  isLoading.value = true
  errorMessage.value = ''

  try {
    const reviewerEmail = auth.user.value?.email ?? ''
    const [timelineData, threadsData, queueData, taskData] = await Promise.all([
      reviewClient.getTimeline(contentItemId.value),
      reviewClient.getThreads(contentItemId.value, true),
      reviewClient.getQueue(reviewerEmail),
      translationClient.getTasks(contentItemId.value),
    ])

    timeline.value = timelineData
    threads.value = threadsData

    applyItemSummary(queueData, taskData)
    await loadAllThreadComments()

    if (screenshotsEnabled) {
      try {
        contextScreenshots.value = await screenshotsClient.getForContentItem(contentItemId.value)
        contextIndex.value = 0
      } catch (_) {
        contextScreenshots.value = []
      }
    } else {
      contextScreenshots.value = []
    }
  } catch (err: any) {
    errorMessage.value = err?.message ?? 'Failed to load review details'
  } finally {
    isLoading.value = false
  }
}

function applyItemSummary(queueData: ReviewQueueItem[], taskData: LanguageTask[]) {
  const currentItem = queueData.find(item => item.id === contentItemId.value)

  itemKey.value = currentItem?.key ?? itemKey.value
  itemSource.value = currentItem?.source ?? itemSource.value
  itemStatus.value = currentItem?.status ?? itemStatus.value
  itemAssignee.value = currentItem?.reviewAssigneeEmail ?? itemAssignee.value

  const pendingLocales = taskData
    .filter(task => task.status === 'pending_review')
    .map(task => task.languageCode)

  const allLocales = taskData.map(task => task.languageCode)
  const localeSet = new Set((pendingLocales.length > 0 ? pendingLocales : allLocales).filter(Boolean))
  itemLocales.value = Array.from(localeSet).sort((left, right) => left.localeCompare(right))
}

async function loadAllThreadComments() {
  const results: Record<string, DiscussionComment[]> = {}
  await Promise.all(
    threads.value.map(async (thread) => {
      try {
        results[thread.id] = await reviewClient.getComments(thread.id)
      } catch {
        results[thread.id] = []
      }
    }),
  )
  threadComments.value = results
}

/* ------------------------------------------------------------------ */
/*  Actions                                                            */
/* ------------------------------------------------------------------ */
async function submitReview(verdict: 'approved' | 'changes_requested' | 'comment') {
  if (!reviewBody.value.trim()) return
  isSubmitting.value = true
  submitError.value = ''
  try {
    await reviewClient.submitReview(contentItemId.value, verdict, reviewBody.value)
    reviewBody.value = ''
    await loadData()
    scrollToTimeline()
  } catch (err: any) {
    submitError.value = err?.message ?? 'Failed to submit review'
  } finally {
    isSubmitting.value = false
  }
}

async function replyToComment(threadId: string, parentCommentId?: string) {
  if (!replyBody.value.trim()) return
  isReplying.value = true
  try {
    await reviewClient.reply(threadId, replyBody.value, parentCommentId)
    replyBody.value = ''
    replyingTo.value = null
    await loadData()
  } catch (err: any) {
    errorMessage.value = err?.message ?? 'Failed to post reply'
  } finally {
    isReplying.value = false
  }
}

async function resolveThread(threadId: string) {
  try {
    await reviewClient.resolveThread(threadId)
    await loadData()
  } catch (err: any) {
    errorMessage.value = err?.message ?? 'Failed to resolve thread'
  }
}

function startReply(threadId: string, parentCommentId?: string) {
  replyingTo.value = { threadId, parentCommentId }
  replyBody.value = ''
}

function cancelReply() {
  replyingTo.value = null
  replyBody.value = ''
}

function toggleThread(threadId: string) {
  if (expandedThreads.value.has(threadId)) {
    expandedThreads.value.delete(threadId)
  } else {
    expandedThreads.value.add(threadId)
  }
}

/* ------------------------------------------------------------------ */
/*  Helpers                                                            */
/* ------------------------------------------------------------------ */
function formatTimestamp(ts: string): string {
  const d = new Date(ts)
  return d.toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function verdictLabel(verdict: string): string {
  switch (verdict) {
    case 'approved': return 'Approved'
    case 'changes_requested': return 'Changes Requested'
    case 'comment': return 'Comment'
    default: return verdict
  }
}

function verdictClass(verdict: string): string {
  switch (verdict) {
    case 'approved': return 'verdict--approved'
    case 'changes_requested': return 'verdict--changes-requested'
    case 'comment': return 'verdict--comment'
    default: return ''
  }
}

function verdictIcon(verdict: string): string {
  switch (verdict) {
    case 'approved': return '\u2713'
    case 'changes_requested': return '\u2717'
    case 'comment': return '\u{1F4AC}'
    default: return ''
  }
}

function timelineTypeClass(type: string): string {
  switch (type) {
    case 'review': return 'tl-entry--review'
    case 'comment': return 'tl-entry--comment'
    case 'status_change': return 'tl-entry--status'
    case 'revision': return 'tl-entry--revision'
    default: return ''
  }
}

function timelineIcon(type: string): string {
  switch (type) {
    case 'review': return '\u{1F4DD}'
    case 'comment': return '\u{1F4AC}'
    case 'status_change': return '\u{1F504}'
    case 'revision': return '\u{1F4C4}'
    default: return '\u2022'
  }
}

function handleReviewKeydown(event: KeyboardEvent) {
  if ((event.ctrlKey || event.metaKey) && event.key === 'Enter') {
    event.preventDefault()
    submitReview('comment')
  }
}

function scrollToTimeline() {
  timelineExpanded.value = true
  nextTick(() => {
    const el = document.querySelector('.timeline-section')
    if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' })
  })
}

function startContextResize(event: MouseEvent) {
  isResizingContext.value = true
  const startX = event.clientX
  const startWidth = contextPanelWidth.value
  function onMove(e: MouseEvent) {
    contextPanelWidth.value = Math.max(200, Math.min(600, startWidth + (startX - e.clientX)))
  }
  function onUp() {
    isResizingContext.value = false
    document.removeEventListener('mousemove', onMove)
    document.removeEventListener('mouseup', onUp)
  }
  document.addEventListener('mousemove', onMove)
  document.addEventListener('mouseup', onUp)
}

function goBack() {
  router.push('/app/review')
}

onMounted(loadData)
</script>

<template>
  <div class="review-detail">
    <nav class="review-detail__nav" aria-label="Review navigation">
      <UiButton variant="ghost" size="sm" @click="goBack">
        &larr; Back to queue
      </UiButton>
    </nav>

    <div v-if="isLoading" class="review-detail__loading">
      <UiCard padding="md">
        <AppSkeleton :lines="4" height="1rem" />
      </UiCard>
      <UiCard padding="md">
        <AppSkeleton :lines="6" height="1rem" />
      </UiCard>
    </div>

    <div v-else-if="errorMessage" class="review-detail__error" role="alert">
      {{ errorMessage }}
      <UiButton variant="secondary" size="sm" @click="loadData">Retry</UiButton>
    </div>

    <template v-else>
      <header class="review-detail__header">
        <div class="review-detail__header-main">
          <h1>
            <code class="review-detail__key">{{ itemKey || 'Content review' }}</code>
          </h1>
          <div class="review-detail__header-meta">
            <span v-if="itemStatus" class="status-badge" :class="`status-badge--${itemStatus}`">
              {{ itemStatus }}
            </span>
            <span v-if="itemAssignee" class="review-detail__assignee">
              Reviewer: {{ itemAssignee }}
            </span>
          </div>

          <dl v-if="localeSummary || showHeaderSource" class="review-detail__facts">
            <div v-if="localeSummary" class="review-detail__fact">
              <dt>{{ localeLabel }}</dt>
              <dd>{{ localeSummary }}</dd>
            </div>
            <div v-if="showHeaderSource" class="review-detail__fact">
              <dt>Source text</dt>
              <dd>{{ itemSource }}</dd>
            </div>
          </dl>
        </div>
      </header>

      <div class="review-detail__content">
        <div class="review-detail__main">
          <UiCard padding="none" class="collapsible-section">
            <button
              class="collapsible-section__toggle"
              type="button"
              :aria-expanded="timelineExpanded ? 'true' : 'false'"
              @click="timelineExpanded = !timelineExpanded"
            >
              <span>
                <span class="section-title">Activity Timeline</span>
                <span class="section-helper collapsible-section__helper">Chronological history of reviews, comments, and status changes.</span>
              </span>
              <span class="collapsible-section__icon" :class="{ 'collapsible-section__icon--open': timelineExpanded }">&#9662;</span>
            </button>

            <div v-if="timelineExpanded" class="collapsible-section__body timeline-section" aria-label="Review timeline" aria-live="polite">
              <div v-if="timeline.length === 0" class="timeline-empty">
                No activity yet.
              </div>

              <ol v-else class="timeline-list" role="list">
                <li
                  v-for="(entry, idx) in timeline"
                  :key="idx"
                  :class="['tl-entry', timelineTypeClass(entry.type), { 'tl-entry--alt': idx % 2 === 1 }]"
                  role="listitem"
                >
                  <div class="tl-entry__icon" aria-hidden="true">{{ timelineIcon(entry.type) }}</div>
                  <div class="tl-entry__body">
                    <div class="tl-entry__header">
                      <span class="tl-entry__actor">{{ entry.actorEmail }}</span>
                      <span class="tl-entry__time">{{ formatTimestamp(entry.timestamp) }}</span>
                    </div>
                    <div class="tl-entry__summary">
                      {{ entry.summary }}
                    </div>
                    <span
                      v-if="entry.type === 'review' && entry.details?.verdict"
                      :class="['verdict-badge', verdictClass(entry.details.verdict)]"
                    >
                      <span class="verdict-badge__icon" aria-hidden="true">{{ verdictIcon(entry.details.verdict) }}</span>
                      {{ verdictLabel(entry.details.verdict) }}
                    </span>
                    <p v-if="entry.type === 'review' && entry.details?.body" class="tl-entry__review-body">
                      {{ entry.details.body }}
                    </p>
                  </div>
                </li>
              </ol>
            </div>
          </UiCard>

          <section class="threads-section" aria-label="Discussion threads">
            <UiCard padding="md">
              <h2 class="section-title">Discussion</h2>
              <p class="section-helper">Conversation threads about this content item.</p>

              <div v-if="threads.length === 0" class="threads-empty">
                No discussion threads yet.
              </div>

              <div v-else class="threads-list">
                <UiCard
                  v-for="thread in threads"
                  :key="thread.id"
                  padding="none"
                  :class="['thread-card', { 'thread-card--resolved': thread.isResolved }]"
                >
                  <div class="thread-card__header" @click="toggleThread(thread.id)">
                    <button
                      class="thread-card__toggle"
                      :aria-expanded="expandedThreads.has(thread.id) ? 'true' : 'false'"
                      :aria-label="`Toggle thread: ${thread.title}`"
                      type="button"
                      @click.stop="toggleThread(thread.id)"
                    >
                      <span class="thread-card__arrow" :class="{ 'thread-card__arrow--open': expandedThreads.has(thread.id) }">&#9656;</span>
                    </button>
                    <div class="thread-card__title-row">
                      <span class="thread-card__title">{{ thread.title }}</span>
                      <span v-if="thread.isResolved" class="thread-card__resolved-badge">Resolved</span>
                    </div>
                    <span class="thread-card__meta">
                      {{ thread.createdByEmail }} &middot; {{ formatTimestamp(thread.createdUtc) }}
                    </span>
                  </div>

                  <div v-if="expandedThreads.has(thread.id)" class="thread-card__comments">
                    <div
                      v-for="comment in (threadComments[thread.id] || [])"
                      :key="comment.id"
                      :class="['comment', { 'comment--nested': comment.parentCommentId }]"
                    >
                      <div class="comment__header">
                        <span class="comment__author">{{ comment.authorEmail }}</span>
                        <span class="comment__time">{{ formatTimestamp(comment.createdUtc) }}</span>
                      </div>
                      <p class="comment__body">{{ comment.body }}</p>
                      <button
                        class="comment__reply-btn"
                        type="button"
                        @click="startReply(thread.id, comment.id)"
                      >
                        Reply
                      </button>

                      <div
                        v-if="replyingTo?.threadId === thread.id && replyingTo?.parentCommentId === comment.id"
                        class="reply-form"
                      >
                        <label :for="`reply-${comment.id}`" class="reply-form__label">Your reply</label>
                        <p class="reply-form__helper">Write a reply to this comment.</p>
                        <textarea
                          :id="`reply-${comment.id}`"
                          v-model="replyBody"
                          class="reply-form__textarea"
                          rows="3"
                        />
                        <div class="reply-form__actions">
                          <UiButton
                            size="sm"
                            :loading="isReplying"
                            :disabled="!replyBody.trim()"
                            @click="replyToComment(thread.id, comment.id)"
                          >
                            Post Reply
                          </UiButton>
                          <UiButton size="sm" variant="ghost" @click="cancelReply">Cancel</UiButton>
                        </div>
                      </div>
                    </div>

                    <div v-if="!thread.isResolved" class="thread-card__actions">
                      <UiButton size="sm" variant="secondary" @click="resolveThread(thread.id)">
                        Resolve thread
                      </UiButton>
                    </div>
                  </div>
                </UiCard>
              </div>
            </UiCard>
          </section>

          <section v-if="canReview" class="submit-review-section" aria-label="Submit review">
            <UiCard padding="md" class="review-panel">
              <h2 class="review-panel__title">Submit Review</h2>
              <p class="review-panel__helper">Leave an overall review comment and choose the outcome for this content.</p>

              <div class="review-panel__field">
                <label for="review-body" class="form-label">Review comment</label>
                <p class="form-helper">Provide feedback on this content. Press Ctrl+Enter to submit as Comment.</p>
                <textarea
                  id="review-body"
                  v-model="reviewBody"
                  class="form-textarea"
                  rows="5"
                  @keydown="handleReviewKeydown"
                />
              </div>

              <div v-if="submitError" class="review-panel__error" role="alert">
                {{ submitError }}
              </div>

              <div class="review-panel__actions">
                <UiButton
                  class="review-btn review-btn--approve"
                  :loading="isSubmitting"
                  :disabled="!reviewBody.trim()"
                  @click="submitReview('approved')"
                >
                  <span aria-hidden="true">&#10003;</span> Approve
                </UiButton>
                <UiButton
                  class="review-btn review-btn--request-changes"
                  variant="danger"
                  :loading="isSubmitting"
                  :disabled="!reviewBody.trim()"
                  @click="submitReview('changes_requested')"
                >
                  <span aria-hidden="true">&#10007;</span> Request Changes
                </UiButton>
                <UiButton
                  class="review-btn review-btn--comment"
                  variant="secondary"
                  :loading="isSubmitting"
                  :disabled="!reviewBody.trim()"
                  @click="submitReview('comment')"
                >
                  <span aria-hidden="true">&#128172;</span> Comment
                </UiButton>
              </div>
            </UiCard>
          </section>
        </div>

        <aside
          v-if="screenshotsEnabled"
          class="review-detail__context"
          :style="{ width: `${contextPanelWidth}px` }"
          aria-label="Visual context"
        >
          <div class="context-resize-handle" @mousedown="startContextResize"></div>
          <div class="context-panel">
            <button class="context-panel__toggle" @click="contextExpanded = !contextExpanded">
              <span class="context-panel__toggle-icon">{{ contextExpanded ? '\u25BC' : '\u25B6' }}</span>
              Visual Context
            </button>

            <div v-if="contextExpanded" class="context-panel__body">
              <template v-if="contextScreenshots.length > 0">
                <div class="context-panel__image-wrapper">
                  <img
                    :src="`/${contextScreenshots[contextIndex].storagePath}`"
                    :alt="contextScreenshots[contextIndex].fileName"
                    class="context-panel__image"
                  />
                  <div
                    v-for="region in contextScreenshots[contextIndex].linkedRegions"
                    :key="region.id"
                    class="context-panel__region-highlight"
                    :style="{
                      left: `${(region.x / (contextScreenshots[contextIndex].width || 1)) * 100}%`,
                      top: `${(region.y / (contextScreenshots[contextIndex].height || 1)) * 100}%`,
                      width: `${(region.width / (contextScreenshots[contextIndex].width || 1)) * 100}%`,
                      height: `${(region.height / (contextScreenshots[contextIndex].height || 1)) * 100}%`,
                    }"
                  ></div>
                </div>

                <div v-if="contextScreenshots.length > 1" class="context-panel__nav">
                  <UiButton
                    variant="ghost"
                    size="sm"
                    :disabled="contextIndex <= 0"
                    @click="contextIndex--"
                  >&larr;</UiButton>
                  <span class="context-panel__nav-label">{{ contextIndex + 1 }} / {{ contextScreenshots.length }}</span>
                  <UiButton
                    variant="ghost"
                    size="sm"
                    :disabled="contextIndex >= contextScreenshots.length - 1"
                    @click="contextIndex++"
                  >&rarr;</UiButton>
                </div>
              </template>

              <div v-else class="context-panel__empty">
                <p>No visual context available</p>
                <p class="context-panel__empty-hint">Link screenshot regions to this content item to see them here.</p>
              </div>
            </div>
          </div>
        </aside>
      </div>
    </template>
  </div>
</template>

<style scoped>
.review-detail {
  max-width: 1200px;
}

.review-detail__nav {
  margin-bottom: var(--spacing-4);
}

.review-detail__loading {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-4);
}

.review-detail__error {
  background: var(--color-error);
  color: var(--color-white);
  padding: var(--spacing-4);
  border-radius: var(--radius-lg);
  display: flex;
  align-items: center;
  gap: var(--spacing-3);
}

.review-detail__header {
  margin-bottom: var(--spacing-4);
}

.review-detail__header-main h1 {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-semibold);
  color: var(--color-gray-900);
  margin: 0 0 var(--spacing-2) 0;
}

.review-detail__key {
  font-family: var(--font-family-mono);
  background: var(--color-gray-100);
  padding: var(--spacing-1) var(--spacing-3);
  border-radius: var(--radius-sm);
}

.review-detail__header-meta {
  display: flex;
  align-items: center;
  gap: var(--spacing-3);
  flex-wrap: wrap;
  font-size: var(--font-size-sm);
  color: var(--color-gray-500);
  margin-bottom: var(--spacing-3);
}

.review-detail__facts {
  display: grid;
  gap: var(--spacing-2);
  margin: 0;
}

.review-detail__fact {
  display: grid;
  grid-template-columns: 96px 1fr;
  gap: var(--spacing-3);
  align-items: start;
}

.review-detail__fact dt {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  margin: 0;
}

.review-detail__fact dd {
  font-size: var(--font-size-sm);
  color: var(--color-text-secondary);
  margin: 0;
  white-space: pre-wrap;
  word-break: break-word;
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

.status-badge--approved {
  background: #dcfce7;
  color: #166534;
}

.status-badge--draft {
  background: var(--color-gray-100);
  color: var(--color-gray-700);
}

.review-detail__assignee {
  color: var(--color-gray-500);
}

.review-detail__content {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: var(--spacing-6);
  align-items: start;
}

.review-detail__main {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-6);
  min-width: 0;
}

.section-title {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-gray-900);
  margin: 0 0 var(--spacing-1) 0;
}

.section-helper {
  font-size: var(--font-size-xs);
  color: var(--color-gray-500);
  margin: 0 0 var(--spacing-4) 0;
}

.collapsible-section {
  overflow: hidden;
}

.collapsible-section__toggle {
  width: 100%;
  border: none;
  background: transparent;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--spacing-4);
  padding: var(--spacing-5) var(--spacing-6);
  text-align: left;
  cursor: pointer;
}

.collapsible-section__toggle:hover {
  background: var(--color-gray-50);
}

.collapsible-section__helper {
  display: block;
  margin-bottom: 0;
}

.collapsible-section__icon {
  color: var(--color-text-muted);
  transition: transform var(--transition-fast);
  flex-shrink: 0;
}

.collapsible-section__icon--open {
  transform: rotate(180deg);
}

.collapsible-section__body {
  border-top: 1px solid var(--color-border);
  padding: var(--spacing-5) var(--spacing-6) var(--spacing-6);
}

.timeline-empty,
.threads-empty {
  font-size: var(--font-size-sm);
  color: var(--color-gray-400);
  padding: var(--spacing-6) 0;
  text-align: center;
}

.timeline-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0;
}

.tl-entry {
  display: flex;
  gap: var(--spacing-3);
  padding: var(--spacing-3) var(--spacing-4);
  border-left: 3px solid var(--color-gray-200);
  position: relative;
}

.tl-entry--alt {
  background: var(--color-gray-50);
}

.tl-entry--review {
  border-left-color: var(--color-primary-400);
}

.tl-entry--comment {
  border-left-color: var(--color-gray-400);
}

.tl-entry--status {
  border-left-color: var(--color-warning);
}

.tl-entry--revision {
  border-left-color: var(--color-info);
}

.tl-entry__icon {
  flex-shrink: 0;
  width: 1.5rem;
  text-align: center;
  font-size: var(--font-size-base);
  line-height: 1.5;
}

.tl-entry__body {
  flex: 1;
  min-width: 0;
}

.tl-entry__header {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
  margin-bottom: var(--spacing-1);
  flex-wrap: wrap;
}

.tl-entry__actor {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-gray-800);
}

.tl-entry__time {
  font-size: var(--font-size-xs);
  color: var(--color-gray-400);
}

.tl-entry__summary {
  font-size: var(--font-size-sm);
  color: var(--color-gray-600);
  margin-bottom: var(--spacing-1);
}

.tl-entry__review-body {
  font-size: var(--font-size-sm);
  color: var(--color-gray-700);
  background: var(--color-gray-50);
  border: 1px solid var(--color-gray-200);
  border-radius: var(--radius-md);
  padding: var(--spacing-2) var(--spacing-3);
  margin-top: var(--spacing-2);
  white-space: pre-wrap;
  word-break: break-word;
}

.verdict-badge {
  display: inline-flex;
  align-items: center;
  gap: var(--spacing-1);
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  padding: var(--spacing-1) var(--spacing-2);
  border-radius: var(--radius-sm);
  margin-top: var(--spacing-1);
}

.verdict-badge__icon {
  font-size: var(--font-size-xs);
}

.verdict--approved {
  background: #dcfce7;
  color: #166534;
}

.verdict--changes-requested {
  background: #fee2e2;
  color: #991b1b;
}

.verdict--comment {
  background: var(--color-gray-100);
  color: var(--color-gray-700);
}

.threads-list {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-3);
}

.thread-card--resolved {
  opacity: 0.7;
}

.thread-card__header {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
  padding: var(--spacing-3) var(--spacing-4);
  cursor: pointer;
  user-select: none;
}

.thread-card__header:hover {
  background: var(--color-gray-50);
}

.thread-card__toggle {
  background: none;
  border: none;
  padding: 0;
  cursor: pointer;
  font-size: var(--font-size-base);
  color: var(--color-gray-500);
  line-height: 1;
}

.thread-card__arrow {
  display: inline-block;
  transition: transform var(--transition-fast);
}

.thread-card__arrow--open {
  transform: rotate(90deg);
}

.thread-card__title-row {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
  flex: 1;
  min-width: 0;
}

.thread-card__title {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-gray-900);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.thread-card__resolved-badge {
  font-size: var(--font-size-xs);
  background: #dcfce7;
  color: #166534;
  padding: var(--spacing-1) var(--spacing-2);
  border-radius: var(--radius-sm);
  font-weight: var(--font-weight-medium);
  flex-shrink: 0;
}

.thread-card__meta {
  font-size: var(--font-size-xs);
  color: var(--color-gray-400);
  flex-shrink: 0;
}

.thread-card__comments {
  border-top: 1px solid var(--color-gray-200);
  padding: var(--spacing-3) var(--spacing-4);
}

.thread-card__actions {
  padding-top: var(--spacing-3);
  border-top: 1px solid var(--color-gray-100);
  margin-top: var(--spacing-3);
}

.comment {
  padding: var(--spacing-2) 0;
}

.comment + .comment {
  border-top: 1px solid var(--color-gray-100);
}

.comment--nested {
  margin-left: var(--spacing-6);
  padding-left: var(--spacing-3);
  border-left: 2px solid var(--color-gray-200);
}

.comment__header {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
  margin-bottom: var(--spacing-1);
}

.comment__author {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-gray-800);
}

.comment__time {
  font-size: var(--font-size-xs);
  color: var(--color-gray-400);
}

.comment__body {
  font-size: var(--font-size-sm);
  color: var(--color-gray-700);
  margin: 0 0 var(--spacing-1) 0;
  white-space: pre-wrap;
  word-break: break-word;
  line-height: var(--line-height-normal);
}

.comment__reply-btn {
  background: none;
  border: none;
  font-size: var(--font-size-xs);
  color: var(--color-primary-600);
  cursor: pointer;
  padding: 0;
  font-weight: var(--font-weight-medium);
}

.comment__reply-btn:hover {
  text-decoration: underline;
}

.comment__reply-btn:focus-visible {
  outline: 2px solid var(--color-primary-500);
  outline-offset: 2px;
}

.reply-form {
  margin-top: var(--spacing-2);
  padding: var(--spacing-3);
  background: var(--color-gray-50);
  border-radius: var(--radius-md);
}

.reply-form__label {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-secondary);
  display: block;
  margin-bottom: var(--spacing-1);
}

.reply-form__helper {
  font-size: var(--font-size-xs);
  color: var(--color-gray-500);
  margin: 0 0 var(--spacing-2) 0;
}

.reply-form__textarea,
.form-textarea {
  width: 100%;
  font-family: inherit;
  font-size: var(--font-size-sm);
  padding: var(--spacing-2) var(--spacing-3);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-surface);
  color: var(--color-text-primary);
  resize: vertical;
}

.reply-form__textarea:focus,
.form-textarea:focus {
  outline: none;
  border-color: var(--color-primary-500);
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.15);
}

.reply-form__actions {
  display: flex;
  gap: var(--spacing-2);
  margin-top: var(--spacing-2);
}

.form-label {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-text-secondary);
  margin-bottom: var(--spacing-1);
}

.form-helper {
  font-size: var(--font-size-xs);
  color: var(--color-gray-500);
  margin: 0 0 var(--spacing-2) 0;
}

.review-panel__title {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-gray-900);
  margin: 0 0 var(--spacing-1) 0;
}

.review-panel__helper {
  font-size: var(--font-size-xs);
  color: var(--color-gray-500);
  margin: 0 0 var(--spacing-4) 0;
}

.review-panel__field {
  margin-bottom: var(--spacing-4);
}

.review-panel__error {
  background: #fee2e2;
  color: #991b1b;
  padding: var(--spacing-2) var(--spacing-3);
  border-radius: var(--radius-md);
  font-size: var(--font-size-sm);
  margin-bottom: var(--spacing-3);
}

.review-panel__actions {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-2);
}

.review-btn {
  width: 100%;
  justify-content: center;
}

.review-btn--approve {
  background: #16a34a;
  color: var(--color-white);
  border-color: #16a34a;
}

.review-btn--approve:hover:not(:disabled) {
  background: #15803d;
}

.review-btn--request-changes {
  background: #dc2626;
  color: var(--color-white);
  border-color: #dc2626;
}

.review-btn--request-changes:hover:not(:disabled) {
  background: #b91c1c;
}

.review-btn--comment {
  background: var(--color-gray-100);
  color: var(--color-gray-800);
  border-color: var(--color-gray-300);
}

.review-btn--comment:hover:not(:disabled) {
  background: var(--color-gray-200);
}

.review-detail__context {
  position: relative;
  flex-shrink: 0;
}

.context-resize-handle {
  position: absolute;
  left: 0;
  top: 0;
  bottom: 0;
  width: 4px;
  cursor: col-resize;
  background: transparent;
  transition: background var(--transition-fast);
}

.context-resize-handle:hover {
  background: var(--color-primary-300);
}

.context-panel__toggle {
  display: flex;
  align-items: center;
  gap: var(--spacing-2);
  width: 100%;
  padding: var(--spacing-3);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md) var(--radius-md) 0 0;
  color: var(--color-text-primary);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  cursor: pointer;
  text-align: left;
}

.context-panel__toggle-icon {
  font-size: var(--font-size-xs);
}

.context-panel__body {
  border: 1px solid var(--color-border);
  border-top: none;
  border-radius: 0 0 var(--radius-md) var(--radius-md);
  padding: var(--spacing-3);
  background: var(--color-surface);
}

.context-panel__image-wrapper {
  position: relative;
  border-radius: var(--radius-sm);
  overflow: hidden;
  margin-bottom: var(--spacing-2);
}

.context-panel__image {
  width: 100%;
  display: block;
}

.context-panel__region-highlight {
  position: absolute;
  border: 3px solid var(--color-primary-500);
  border-radius: var(--radius-sm);
  background: color-mix(in srgb, var(--color-primary-300) 20%, transparent);
  pointer-events: none;
}

.context-panel__nav {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--spacing-2);
}

.context-panel__nav-label {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

.context-panel__empty {
  text-align: center;
  padding: var(--spacing-4);
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.context-panel__empty-hint {
  font-size: var(--font-size-xs);
  margin-top: var(--spacing-1);
}

@media (min-width: 1100px) {
  .review-detail__content {
    grid-template-columns: minmax(0, 1fr) auto;
  }
}

@media (max-width: 900px) {
  .review-detail__context {
    width: 100% !important;
  }

  .review-detail__fact {
    grid-template-columns: 1fr;
    gap: var(--spacing-1);
  }
}
</style>
