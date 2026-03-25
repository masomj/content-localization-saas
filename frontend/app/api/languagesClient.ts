import { apiRequest } from '~/api/client'
import type { ProjectLanguage } from '~/api/types'

export const languagesClient = {
  list(projectId: string) {
    return apiRequest<ProjectLanguage[]>(`/project-languages?projectId=${encodeURIComponent(projectId)}`)
  },
  add(projectId: string, bcp47Code: string, isSource = false) {
    return apiRequest<ProjectLanguage>('/project-languages', {
      method: 'POST',
      body: JSON.stringify({ projectId, bcp47Code, isSource }),
    })
  },
  toggleActive(id: string, isActive: boolean) {
    return apiRequest<void>(`/project-languages/${id}/active`, {
      method: 'PUT',
      body: JSON.stringify({ isActive }),
    })
  },
  changeSource(projectId: string, bcp47Code: string) {
    return apiRequest<void>(`/project-languages/source-language?projectId=${encodeURIComponent(projectId)}`, {
      method: 'POST',
      body: JSON.stringify({ bcp47Code }),
    })
  },
}
