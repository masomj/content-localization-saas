// ---------------------------------------------------------------
// Shared types matching the InterCopy backend API
// ---------------------------------------------------------------

/** Represents a Figma frame / component pushed to the platform. */
export interface DesignComponent {
  id: string;
  projectId: string;
  figmaFileId: string;
  figmaFrameId: string;
  figmaFrameName: string;
  thumbnailUrl: string;
  frameWidth: number;
  frameHeight: number;
  status: "draft" | "in_review" | "approved";
  createdByEmail: string;
  createdUtc: string;
  updatedUtc: string;
  textFields: DesignComponentTextField[];
}

/** A single text layer within a design component. */
export interface DesignComponentTextField {
  id: string;
  designComponentId: string;
  figmaLayerId: string;
  figmaLayerName: string;
  currentText: string;
  contentItemId: string | null;
  x: number;
  y: number;
  width: number;
  height: number;
  fontFamily: string;
  fontSize: number;
  fontWeight: string;
  textAlign: string;
  color: string;
  createdUtc: string;
  updatedUtc: string;
}

// ---------------------------------------------------------------
// API request / response shapes
// ---------------------------------------------------------------

export interface LoginRequest {
  email: string;
  workspaceId: string;
}

export interface LoginResponse {
  token: string;
  email: string;
  displayName: string;
}

export interface Project {
  id: string;
  name: string;
  slug: string;
}

/** Payload sent from the Figma plugin when pushing a frame. */
export interface PushComponentPayload {
  figmaFileId: string;
  figmaFrameId: string;
  figmaFrameName: string;
  thumbnailUrl: string;
  frameWidth: number;
  frameHeight: number;
  projectId: string;
  textFields: PushTextField[];
}

export interface PushTextField {
  figmaLayerId: string;
  figmaLayerName: string;
  currentText: string;
  x: number;
  y: number;
  width: number;
  height: number;
  fontFamily: string;
  fontSize: number;
  fontWeight: string;
  textAlign: string;
  color: string;
}

/** Response from pull-component. */
export interface PullComponentResponse {
  componentId: string;
  textFields: DesignComponentTextField[];
  languages?: Array<{ bcp47Code: string; isSource: boolean }>;
  requestedLanguage?: string | null;
}

// ---------------------------------------------------------------
// Plugin-specific types for multi-tab UI
// ---------------------------------------------------------------

export type TabName = "activity" | "edit" | "review" | "changes" | "library";

export type SyncStatus = "synced" | "draft" | "modified" | "new" | "unlinked";

/** A text node extracted from the Figma canvas. */
export interface TextNodeInfo {
  layerId: string;
  layerName: string;
  characters: string;
  x: number;
  y: number;
  width: number;
  height: number;
  fontFamily: string;
  fontSize: number;
  fontWeight: string;
  textAlign: string;
  color: string;
}

/** A frame with its extracted text nodes (sent from main thread). */
export interface FrameInfo {
  frameId: string;
  frameName: string;
  frameWidth: number;
  frameHeight: number;
  textNodes: TextNodeInfo[];
}

/** Change detection entry for a frame. */
export interface ChangeEntry {
  frameId: string;
  frameName: string;
  /** Whether the frame has ever been synced. */
  isNew: boolean;
  /** Text nodes that differ from the synced version (or all, if new). */
  changedTexts: ChangedTextField[];
  /** The remote component ID if synced before, null if new. */
  componentId: string | null;
}

export interface ChangedTextField {
  layerId: string;
  layerName: string;
  localText: string;
  remoteText: string | null;
}

/** Activity feed entry. */
export interface ActivityEntry {
  id: string;
  action: "connected_frame" | "submitted_changes" | "synced" | "pulled" | "edited_text";
  description: string;
  timestamp: string; // ISO
  frameCount?: number;
  textCount?: number;
}

/** Review queue entry. */
export interface ReviewEntry {
  layerId: string;
  layerName: string;
  text: string;
  frameName: string;
  frameId: string;
  addedAt: string; // ISO
}

/** Edit tab text field with local status tracking. */
export interface EditableTextField {
  layerId: string;
  layerName: string;
  characters: string;
  originalCharacters: string;
  syncedCharacters: string | null;
  status: SyncStatus;
}

/** Library component (reusable copy). */
export interface LibraryComponent {
  id: string;
  name: string;
  textCount: number;
  status: string;
}

// ---------------------------------------------------------------
// Messages between Figma main thread <-> UI iframe
// ---------------------------------------------------------------

export type UIMessage =
  | { type: "push-frame"; projectId: string }
  | { type: "push-frames"; projectId: string; frameIds: string[] }
  | { type: "pull-component"; componentId: string }
  | { type: "login"; email: string; workspaceId: string }
  | { type: "get-projects" }
  | { type: "update-text"; layerId: string; newText: string }
  | { type: "scan-all-frames" }
  | { type: "get-selection" }
  | { type: "apply-pull"; textFields: DesignComponentTextField[] }
  | { type: "get-file-key" }
  | { type: "resize"; width: number; height: number }
  | { type: "storage-request" }
  | { type: "storage-set"; key: string; value: string }
  | { type: "storage-remove"; key: string }
  | { type: "scan-components" };

export interface FigmaVariantInfo {
  nodeId: string;
  variantName: string;
  variantProperties: Record<string, string>;
  width: number;
  height: number;
  backgroundColor: string;
  thumbnailUrl: string;
  textNodes: TextNodeInfo[];
}

export interface FigmaComponentInfo {
  componentKey: string;
  componentId: string;
  componentSetId: string;
  name: string;
  width: number;
  height: number;
  isComponentSet: boolean;
  variants: FigmaVariantInfo[];
}

export type MainMessage =
  | { type: "frame-data"; payload: PushComponentPayload }
  | { type: "multi-frame-data"; frames: FrameInfo[] }
  | { type: "pull-text"; textFields: DesignComponentTextField[] }
  | { type: "error"; message: string }
  | { type: "notify"; message: string }
  | { type: "selection-changed"; frames: FrameInfo[] }
  | { type: "selection-empty" }
  | { type: "all-frames"; frames: FrameInfo[] }
  | { type: "text-updated"; layerId: string; newText: string }
  | { type: "file-key"; fileKey: string }
  | { type: "current-user"; name: string; email: string }
  | { type: "storage-data"; entries: Record<string, string | null> }
  | { type: "components-list"; components: FigmaComponentInfo[] };
