import { apiRequest } from '~/api/client'
import type {
  ContentReview,
  DiscussionComment,
  DiscussionThread,
  ReviewQueueItem,
  TimelineEntry,
} from '~/api/types'

export const reviewClient = {
  getQueue(reviewerEmail: string) {
    return apiRequest<ReviewQueueItem[]>(
      `/content-reviews/queue?reviewerEmail=${encodeURIComponent(reviewerEmail)}`,
    )
  },

  getReviews(contentItemId: string) {
    return apiRequest<ContentReview[]>(
      `/content-reviews?contentItemId=${encodeURIComponent(contentItemId)}`,
    )
  },

  submitReview(contentItemId: string, verdict: string, body: string) {
    return apiRequest<ContentReview>('/content-reviews', {
      method: 'POST',
      body: JSON.stringify({ contentItemId, verdict, body }),
    })
  },

  getTimeline(contentItemId: string) {
    return apiRequest<TimelineEntry[]>(
      `/content-reviews/${encodeURIComponent(contentItemId)}/timeline`,
    )
  },

  getThreads(contentItemId: string, includeResolved = false) {
    const params = new URLSearchParams({ contentItemId })
    if (includeResolved) params.set('includeResolved', 'true')
    return apiRequest<DiscussionThread[]>(
      `/discussions/threads?${params.toString()}`,
    )
  },

  getComments(threadId: string) {
    return apiRequest<DiscussionComment[]>(
      `/discussions/threads/${encodeURIComponent(threadId)}/comments`,
    )
  },

  createThread(contentItemId: string, title: string, body: string) {
    return apiRequest<DiscussionThread>('/discussions/threads', {
      method: 'POST',
      body: JSON.stringify({ contentItemId, title, body }),
    })
  },

  reply(threadId: string, body: string, parentCommentId?: string, reviewId?: string) {
    return apiRequest<DiscussionComment>('/discussions/replies', {
      method: 'POST',
      body: JSON.stringify({ threadId, body, parentCommentId, reviewId }),
    })
  },

  resolveThread(threadId: string) {
    return apiRequest<void>(
      `/discussions/threads/${encodeURIComponent(threadId)}/resolve`,
      { method: 'POST' },
    )
  },
}
