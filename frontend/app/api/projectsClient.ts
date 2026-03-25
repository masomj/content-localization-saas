import { apiRequest } from '~/api/client'
import type { Collection, CreateCollectionRequest, CreateProjectRequest, MoveCollectionRequest, Project, ProjectTreeNode } from '~/api/types'

export const projectsClient = {
  list(workspaceId: string) {
    return apiRequest<Project[]>(`/projects?workspaceId=${encodeURIComponent(workspaceId)}`)
  },
  create(payload: CreateProjectRequest) {
    return apiRequest<Project>('/projects', {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },
  listCollections(projectId: string) {
    return apiRequest<Collection[]>(`/projects/${projectId}/collections`)
  },
  createCollection(projectId: string, payload: CreateCollectionRequest) {
    return apiRequest<Collection>(`/projects/${projectId}/collections`, {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },
  renameCollection(projectId: string, collectionId: string, name: string) {
    return apiRequest<void>(`/projects/${projectId}/collections/${collectionId}/rename`, {
      method: 'PUT',
      body: JSON.stringify({ name }),
    })
  },
  moveCollection(projectId: string, collectionId: string, payload: MoveCollectionRequest) {
    return apiRequest<Collection[]>(`/projects/${projectId}/collections/${collectionId}/move`, {
      method: 'PUT',
      body: JSON.stringify(payload),
    })
  },
  getProjectTree(projectId: string) {
    return apiRequest<ProjectTreeNode[]>(`/projects/${projectId}/tree`)
  },
}
