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
  description?: string
  sourceLanguage?: string
  createdUtc?: string
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

export interface ProjectLanguage {
  id: string
  projectId: string
  bcp47Code: string
  isSource: boolean
  isActive: boolean
}

export interface LanguageTask {
  id: string
  contentItemId: string
  languageCode: string
  assigneeEmail: string
  translationText: string
  previousApprovedTranslation: string
  isOutdated: boolean
  dueUtc: string | null
  status: string
}

export interface TranslationSuggestion {
  hasSuggestion: boolean
  suggestion: { id: string; translationText: string; createdUtc: string } | null
}

export interface LocalizationGridRow {
  itemId: string
  itemKey: string
  source: string
  sourceStatus: string
  targets: Array<{ language: string; status: string; assigneeEmail: string; dueUtc: string | null }>
  hasMissing: boolean
  hasOutdated: boolean
  hasReview: boolean
}

export interface LocalizationGridResponse {
  total: number
  page: number
  pageSize: number
  rows: LocalizationGridRow[]
}

export interface ContentReview {
  id: string
  contentItemId: string
  reviewerEmail: string
  verdict: 'approved' | 'changes_requested' | 'comment'
  body: string
  createdUtc: string
}

export interface ReviewQueueItem {
  id: string
  key: string
  source: string
  status: string
  reviewAssigneeEmail: string
  projectId: string
  commentCount: number
  reviewCount: number
  latestReviewVerdict: string | null
}

export interface TimelineEntry {
  type: 'review' | 'comment' | 'status_change' | 'revision'
  timestamp: string
  actorEmail: string
  summary: string
  details: any
}

export interface DiscussionThread {
  id: string
  contentItemId: string
  title: string
  createdByEmail: string
  isResolved: boolean
  createdUtc: string
}

export interface DiscussionComment {
  id: string
  threadId: string
  parentCommentId: string | null
  reviewId: string | null
  body: string
  authorEmail: string
  createdUtc: string
}

export interface ProjectVersion {
  id: string
  projectId: string
  tag: string
  title: string
  notes: string
  isLive: boolean
  createdByEmail: string
  contentItemCount: number
  translationCount: number
  createdUtc: string
}

export interface VersionSnapshotItem {
  id: string
  versionId: string
  originalContentItemId: string
  key: string
  source: string
  status: string
  tags: string
  translationsJson: string
}

export interface VersionDiff {
  added: Array<{ key: string; source: string }>
  removed: Array<{ key: string; source: string }>
  changed: Array<{ key: string; oldSource: string; newSource: string }>
}

export interface DesignComponent {
  id: string
  projectId: string
  figmaFileId: string
  figmaFrameId: string
  figmaFrameName: string
  thumbnailUrl: string
  frameWidth: number
  frameHeight: number
  status: string
  createdByEmail: string
  createdUtc: string
  updatedUtc: string
  textFieldCount?: number
}

export interface DesignComponentTextField {
  id: string
  designComponentId: string
  figmaLayerId: string
  figmaLayerName: string
  currentText: string
  contentItemId?: string | null
  x: number
  y: number
  width: number
  height: number
  fontFamily: string
  fontSize: number
  fontWeight: string
  textAlign: string
  color: string
  createdUtc: string
  updatedUtc: string
  isComponentInstance?: boolean
  sourceComponentKey?: string
}

export interface LibraryComponent {
  id: string
  projectId: string
  figmaFileId: string
  figmaComponentKey: string
  figmaComponentId: string
  figmaComponentSetId: string
  name: string
  description: string
  thumbnailUrl: string
  frameWidth: number
  frameHeight: number
  createdUtc: string
  updatedUtc: string
  variantCount?: number
  textFieldCount?: number
}

export interface LibraryComponentVariant {
  id: string
  libraryComponentId: string
  figmaNodeId: string
  variantName: string
  variantProperties: string
  createdUtc: string
  updatedUtc: string
  textFields?: LibraryComponentTextField[]
}

export interface LibraryComponentTextField {
  id: string
  libraryComponentVariantId: string
  figmaLayerId: string
  figmaLayerName: string
  currentText: string
  contentItemId?: string | null
  x: number
  y: number
  width: number
  height: number
  fontFamily: string
  fontSize: number
  fontWeight: string
  textAlign: string
  color: string
  createdUtc: string
  updatedUtc: string
}

export interface LibraryComponentWithVariants extends LibraryComponent {
  variants: LibraryComponentVariant[]
}
