export interface Workspace {
  id: string
  name: string
}

export interface WorkspaceMembership extends Workspace {
  role: string
}

export interface User {
  id: string
  email: string
  name: string
  role?: string
  workspace?: Workspace
  workspaces?: WorkspaceMembership[]
}

export interface AuthResponse {
  token: string
  user: User
  workspace?: Workspace
}

export interface Project {
  id: string
  name: string
  status?: string
}

export interface Collection {
  id: string
  projectId: string
  parentId: string | null
  name: string
  isRoot: boolean
  depth: number
  sortOrder: number
}

export interface ContentItem {
  id: string
  key: string
  source: string
  status: string
  collectionId: string | null
  sortOrder: number
}

export interface CreateProjectRequest {
  workspaceId: string
  name: string
  sourceLanguage: string
  description: string
}

export interface CreateCollectionRequest {
  name: string
  parentId: string | null
}

export interface MoveCollectionRequest {
  newParentId: string | null
  newIndex: number
}

export interface ProjectTreeNode {
  id: string
  name: string
  nodeType: 'folder' | 'contentKey'
  parentId: string | null
  sortOrder: number
  depth: number
  children: ProjectTreeNode[]
  key?: string
  status?: string
}

export interface MoveContentItemRequest {
  collectionId: string | null
  sortOrder: number
}

export interface CreateContentItemRequest {
  projectId: string
  key: string
  source: string
  status: string
  tags: string[]
  context: string | null
  notes: string | null
  collectionId: string | null
}

export interface Member {
  id: string
  workspaceId: string
  email: string
  role: string
  isActive: boolean
  createdUtc: string
}

export interface Invite {
  id: string
  workspaceId: string
  email: string
  role: string
  status: 'Pending' | 'Accepted' | 'Expired' | 'Revoked'
  expiresUtc: string
  createdUtc: string
}

export interface MembershipAuditRow {
  id: string
  action: string
  targetEmail: string
  oldValue: string | null
  newValue: string | null
  createdUtc: string
}
