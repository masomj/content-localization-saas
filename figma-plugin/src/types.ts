// ---------------------------------------------------------------
// Shared types matching the LocFlow backend API
// (mirrors DesignComponent + DesignComponentTextField entities)
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
  password: string;
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

/** Response from pull-component – the current text for every layer. */
export interface PullComponentResponse {
  componentId: string;
  textFields: DesignComponentTextField[];
}

// ---------------------------------------------------------------
// Messages between Figma main thread ↔ UI iframe
// ---------------------------------------------------------------

export type UIMessage =
  | { type: "push-frame"; projectId: string }
  | { type: "pull-component"; componentId: string }
  | { type: "login"; email: string; password: string }
  | { type: "get-projects" };

export type MainMessage =
  | { type: "frame-data"; payload: PushComponentPayload }
  | { type: "pull-text"; textFields: DesignComponentTextField[] }
  | { type: "error"; message: string }
  | { type: "notify"; message: string }
  | { type: "selection-info"; hasFrame: boolean; frameName: string };
