import { apiRequest } from '~/api/client'
import type { FigmaScreenshotSync } from '~/api/types'

export const figmaSyncClient = {
  list(projectId: string) {
    return apiRequest<FigmaScreenshotSync[]>(`/projects/${encodeURIComponent(projectId)}/figma-sync`)
  },

  connect(projectId: string, figmaFileKey: string) {
    return apiRequest<FigmaScreenshotSync>(`/projects/${encodeURIComponent(projectId)}/figma-sync/connect`, {
      method: 'POST',
      body: JSON.stringify({ figmaFileKey }),
    })
  },

  sync(projectId: string, syncId: string) {
    return apiRequest<FigmaScreenshotSync>(`/projects/${encodeURIComponent(projectId)}/figma-sync/${encodeURIComponent(syncId)}/sync`, {
      method: 'POST',
    })
  },

  delete(projectId: string, syncId: string) {
    return apiRequest<void>(`/projects/${encodeURIComponent(projectId)}/figma-sync/${encodeURIComponent(syncId)}`, {
      method: 'DELETE',
    })
  },
}
