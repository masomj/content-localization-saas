import type {
  Project,
  PushComponentPayload,
  DesignComponent,
  PullComponentResponse,
} from "./types";

// ---------------------------------------------------------------
// InterCopy API client — used from the plugin UI iframe
// Uses Keycloak OIDC Device Authorization Flow for auth.
// ---------------------------------------------------------------

/** Response from POST /api/device-auth/start */
export interface DeviceAuthStartResponse {
  userCode: string;
  verificationUri: string;
  verificationUriComplete: string | null;
  deviceCode: string;
  expiresIn: number;
  interval: number;
}

/** Response from POST /api/device-auth/poll */
export interface DeviceAuthPollResponse {
  status: "pending" | "expired" | "complete";
  accessToken?: string;
  refreshToken?: string;
  expiresIn?: number;
  user?: { sub: string; email: string | null; name: string | null };
}

/** Response from POST /api/device-auth/refresh */
export interface DeviceAuthRefreshResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
}

/** Decoded JWT payload (minimal fields we care about). */
export interface JwtPayload {
  sub?: string;
  email?: string;
  name?: string;
  given_name?: string;
  family_name?: string;
  exp?: number;
  iat?: number;
}

export class InterCopyApi {
  private baseUrl: string;
  private accessToken: string | null = null;
  private refreshTokenValue: string | null = null;

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl.replace(/\/+$/, "");
  }

  // -- Auth ---------------------------------------------------

  setToken(token: string): void {
    this.accessToken = token;
  }

  setRefreshToken(token: string): void {
    this.refreshTokenValue = token;
  }

  clearToken(): void {
    this.accessToken = null;
    this.refreshTokenValue = null;
  }

  get isAuthenticated(): boolean {
    return this.accessToken !== null;
  }

  get token(): string | null {
    return this.accessToken;
  }

  // -- Device Auth Flow ----------------------------------------

  /**
   * Start the device authorization flow.
   * Returns user_code + verification URI for the user to complete in a browser.
   */
  async startDeviceAuth(): Promise<DeviceAuthStartResponse> {
    return this.post<DeviceAuthStartResponse>(
      "/api/device-auth/start",
      {},
      false
    );
  }

  /**
   * Poll for device auth completion.
   * Call every `interval` seconds until status is "complete" or "expired".
   */
  async pollDeviceAuth(deviceCode: string): Promise<DeviceAuthPollResponse> {
    return this.post<DeviceAuthPollResponse>(
      "/api/device-auth/poll",
      { deviceCode },
      false
    );
  }

  /**
   * Refresh an expired access token using the refresh token.
   */
  async refreshToken(): Promise<DeviceAuthRefreshResponse> {
    if (!this.refreshTokenValue) {
      throw new Error("SESSION_EXPIRED");
    }
    const res = await this.post<DeviceAuthRefreshResponse>(
      "/api/device-auth/refresh",
      { refreshToken: this.refreshTokenValue },
      false
    );
    this.accessToken = res.accessToken;
    this.refreshTokenValue = res.refreshToken;
    return res;
  }

  // -- Token utilities ----------------------------------------

  /**
   * Check if the current access token is expired (or about to expire).
   * Returns true if token is missing or expires within 60 seconds.
   */
  isTokenExpired(): boolean {
    if (!this.accessToken) return true;
    try {
      const payload = decodeJwt(this.accessToken);
      if (!payload.exp) return false;
      const nowSec = Math.floor(Date.now() / 1000);
      return payload.exp - nowSec < 60; // 60-second buffer
    } catch (_) {
      return true;
    }
  }

  /**
   * Ensure the access token is fresh. If expired, attempt refresh.
   * Throws SESSION_EXPIRED if refresh also fails.
   */
  async ensureFreshToken(): Promise<void> {
    if (!this.isTokenExpired()) return;
    try {
      await this.refreshToken();
    } catch (_) {
      this.accessToken = null;
      this.refreshTokenValue = null;
      throw new Error("SESSION_EXPIRED");
    }
  }

  // -- Projects -----------------------------------------------

  /**
   * List projects accessible to the current user.
   * Uses the Keycloak-authenticated endpoint.
   */
  async getProjects(): Promise<Project[]> {
    await this.ensureFreshToken();
    return this.get<Project[]>("/api/projects");
  }

  // -- Component sync -----------------------------------------

  /** Push a Figma frame structure to the backend. */
  async pushComponent(data: PushComponentPayload): Promise<DesignComponent> {
    await this.ensureFreshToken();
    return this.post<DesignComponent>(
      "/api/plugin-sync/push-component",
      data
    );
  }

  /** Pull latest text fields for a component from the backend. */
  async pullComponent(componentId: string, language?: string): Promise<PullComponentResponse> {
    await this.ensureFreshToken();
    const langParam = language ? `?language=${encodeURIComponent(language)}` : "";
    return this.post<PullComponentResponse>(
      `/api/plugin-sync/pull-component/${componentId}${langParam}`,
      {}
    );
  }

  // -- Design components (for change detection) ---------------

  async getComponents(projectId: string): Promise<DesignComponent[]> {
    await this.ensureFreshToken();
    return this.get<DesignComponent[]>(
      `/api/projects/${projectId}/components`
    );
  }

  async getComponent(projectId: string, componentId: string): Promise<DesignComponent> {
    await this.ensureFreshToken();
    return this.get<DesignComponent>(
      `/api/projects/${projectId}/components/${componentId}`
    );
  }

  // -- Library components (push) -------------------------------

  async pushLibraryComponent(data: {
    projectId: string;
    figmaFileId: string;
    figmaComponentKey: string;
    figmaComponentId: string;
    figmaComponentSetId: string;
    componentName: string;
    frameWidth: number;
    frameHeight: number;
    variants: Array<{
      figmaNodeId: string;
      variantName: string;
      variantProperties: string;
      backgroundColor: string;
      thumbnailUrl: string;
      textFields: Array<{
        figmaLayerId: string;
        figmaLayerName: string;
        currentText: string;
        x: number; y: number; width: number; height: number;
        fontFamily: string; fontSize: number; fontWeight: string; textAlign: string; color: string;
      }>;
    }>;
  }): Promise<unknown> {
    await this.ensureFreshToken();
    return this.post<unknown>("/api/plugin-sync/push-library-component", data);
  }

  // -- Activity feed ------------------------------------------

  async getActivity(projectId: string): Promise<{ items: ActivityFeedItem[] }> {
    try {
      await this.ensureFreshToken();
      return await this.get<{ items: ActivityFeedItem[] }>(
        `/api/activity-feed?projectId=${projectId}`
      );
    } catch (_) {
      return { items: [] };
    }
  }

  // -- HTTP helpers -------------------------------------------

  private async get<T>(path: string): Promise<T> {
    const res = await fetch(`${this.baseUrl}${path}`, {
      method: "GET",
      headers: this.headers(),
    });
    return this.handleResponse<T>(res);
  }

  private async post<T>(path: string, body: unknown, includeAuth = true): Promise<T> {
    const res = await fetch(`${this.baseUrl}${path}`, {
      method: "POST",
      headers: this.headers(includeAuth),
      body: JSON.stringify(body),
    });
    return this.handleResponse<T>(res);
  }

  private headers(includeAuth = true): Record<string, string> {
    const h: Record<string, string> = {
      "Content-Type": "application/json",
    };
    if (includeAuth && this.accessToken) {
      h["Authorization"] = `Bearer ${this.accessToken}`;
    }
    return h;
  }

  private async handleResponse<T>(res: Response): Promise<T> {
    if (!res.ok) {
      const text = await res.text().catch((_) => "Unknown error");
      if (res.status === 401) {
        this.accessToken = null;
        throw new Error("SESSION_EXPIRED");
      }
      throw new Error(`API ${res.status}: ${text}`);
    }
    return (await res.json()) as T;
  }
}

// ---------------------------------------------------------------
// JWT decoding — simple base64 decode, no library needed.
// Works in the Figma iframe sandbox.
// ---------------------------------------------------------------

export function decodeJwt(token: string): JwtPayload {
  try {
    const parts = token.split(".");
    if (parts.length < 2) return {};
    let payload = parts[1];
    // base64url → base64
    payload = payload.replace(/-/g, "+").replace(/_/g, "/");
    // pad
    while (payload.length % 4 !== 0) {
      payload += "=";
    }
    const json = atob(payload);
    return JSON.parse(json) as JwtPayload;
  } catch (_) {
    return {};
  }
}

/** Activity feed item from the backend. */
interface ActivityFeedItem {
  id: string;
  eventType: string;
  actorEmail: string;
  description: string;
  createdUtc: string;
  metadata?: Record<string, unknown>;
}
