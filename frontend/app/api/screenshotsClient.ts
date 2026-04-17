import { apiRequest } from '~/api/client'
import type { Screenshot, ScreenshotDetail, ScreenshotRegion } from '~/api/types'

export const screenshotsClient = {
  list(projectId: string) {
    return apiRequest<Screenshot[]>(`/projects/${encodeURIComponent(projectId)}/screenshots`)
  },

  get(projectId: string, id: string) {
    return apiRequest<ScreenshotDetail>(`/projects/${encodeURIComponent(projectId)}/screenshots/${encodeURIComponent(id)}`)
  },

  upload(projectId: string, file: File) {
    const formData = new FormData()
    formData.append('file', file)

    // apiRequest auto-sets Content-Type to application/json when body is present.
    // For FormData, the browser must set it with the multipart boundary, so we
    // override with an empty string which we then strip before fetch.
    return apiRequest<Screenshot>(`/projects/${encodeURIComponent(projectId)}/screenshots`, {
      method: 'POST',
      body: formData,
    })
  },

  delete(projectId: string, id: string) {
    return apiRequest<void>(`/projects/${encodeURIComponent(projectId)}/screenshots/${encodeURIComponent(id)}`, {
      method: 'DELETE',
    })
  },

  linkRegion(regionId: string, contentItemId: string) {
    return apiRequest<ScreenshotRegion>(`/screenshot-regions/${encodeURIComponent(regionId)}/link`, {
      method: 'PUT',
      body: JSON.stringify({ contentItemId }),
    })
  },

  unlinkRegion(regionId: string) {
    return apiRequest<ScreenshotRegion>(`/screenshot-regions/${encodeURIComponent(regionId)}/unlink`, {
      method: 'PUT',
    })
  },
}
