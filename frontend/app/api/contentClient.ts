import { apiRequest } from '~/api/client'
import type { ContentItem, CreateContentItemRequest, MoveContentItemRequest } from '~/api/types'

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
  move(id: string, payload: MoveContentItemRequest) {
    return apiRequest<ContentItem>(`/content-items/${id}/move`, {
      method: 'PUT',
      body: JSON.stringify(payload),
    })
  },
}
