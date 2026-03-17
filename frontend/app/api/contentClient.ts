import { apiRequest } from '~/api/client'
import type { ContentItem, CreateContentItemRequest } from '~/api/types'

export const contentClient = {
  list(projectId: string) {
    return apiRequest<ContentItem[]>(`/content-items?projectId=${encodeURIComponent(projectId)}`)
  },
  create(payload: CreateContentItemRequest) {
    return apiRequest<ContentItem>('/content-items', {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },
}
