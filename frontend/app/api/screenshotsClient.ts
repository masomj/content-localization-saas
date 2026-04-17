import { apiRequest } from '~/api/client'
import type { Screenshot, ScreenshotDetail, ScreenshotRegion, ScreenshotContextDetail, ContentItemScreenshot } from '~/api/types'

export const screenshotsClient = {
  list(projectId: string) {
    return apiRequest<Screenshot[]>(`/projects/${encodeURIComponent(projectId)}/screenshots`)
  },

  get(projectId: string, id: string) {
    return apiRequest<ScreenshotDetail>(`/projects/${encodeURIComponent(projectId)}/screenshots/${encodeURIComponent(id)}`)
  },

  getContext(projectId: string, id: string, language: string) {
    return apiRequest<ScreenshotContextDetail>(
      `/projects/${encodeURIComponent(projectId)}/screenshots/${encodeURIComponent(id)}/context?language=${encodeURIComponent(language)}`,
    )
  },

  getForContentItem(contentItemId: string) {
    return apiRequest<ContentItemScreenshot[]>(`/content-items/${encodeURIComponent(contentItemId)}/screenshots`)
  },

  upload(projectId: string, file: File) {
    const formData = new FormData()
    formData.append('file', file)

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
