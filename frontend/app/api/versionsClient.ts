import { apiRequest } from '~/api/client'
import type { ProjectVersion, VersionSnapshotItem, VersionDiff } from '~/api/types'

export const versionsClient = {
  list(projectId: string) {
    return apiRequest<ProjectVersion[]>(`/projects/${projectId}/versions`)
  },
  get(projectId: string, versionId: string) {
    return apiRequest<ProjectVersion>(`/projects/${projectId}/versions/${versionId}`)
  },
  create(projectId: string, tag: string, title: string, notes: string) {
    return apiRequest<ProjectVersion>(`/projects/${projectId}/versions`, {
      method: 'POST',
      body: JSON.stringify({ tag, title, notes }),
    })
  },
  update(projectId: string, versionId: string, title: string, notes: string) {
    return apiRequest<ProjectVersion>(`/projects/${projectId}/versions/${versionId}`, {
      method: 'PUT',
      body: JSON.stringify({ title, notes }),
    })
  },
  remove(projectId: string, versionId: string) {
    return apiRequest<void>(`/projects/${projectId}/versions/${versionId}`, {
      method: 'DELETE',
    })
  },
  promote(projectId: string, versionId: string) {
    return apiRequest<void>(`/projects/${projectId}/versions/${versionId}/promote`, {
      method: 'POST',
    })
  },
  demote(projectId: string, versionId: string) {
    return apiRequest<void>(`/projects/${projectId}/versions/${versionId}/demote`, {
      method: 'POST',
    })
  },
  getContent(projectId: string, versionId: string) {
    return apiRequest<VersionSnapshotItem[]>(`/projects/${projectId}/versions/${versionId}/content`)
  },
  compare(projectId: string, fromId: string, toId: string) {
    return apiRequest<VersionDiff>(`/projects/${projectId}/versions/compare?from=${encodeURIComponent(fromId)}&to=${encodeURIComponent(toId)}`)
  },
}
