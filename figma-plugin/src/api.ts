import type {
  LoginResponse,
  Project,
  PushComponentPayload,
  DesignComponent,
  PullComponentResponse,
} from "./types";

// ---------------------------------------------------------------
// LocFlow API client — used from the plugin UI iframe
// ---------------------------------------------------------------

export class LocFlowApi {
  private baseUrl: string;
  private sessionToken: string | null = null;

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl.replace(/\/+$/, "");
  }

  // -- Auth ---------------------------------------------------

  setToken(token: string): void {
    this.sessionToken = token;
  }

  clearToken(): void {
    this.sessionToken = null;
  }

  get isAuthenticated(): boolean {
    return this.sessionToken !== null;
  }

  get token(): string | null {
    return this.sessionToken;
  }

  /**
   * Authenticate with email + workspaceId.
   * Backend: POST /api/plugin-auth/login
   * Returns session token (8h TTL).
   */
  async login(email: string, workspaceId: string): Promise<LoginResponse> {
    const body = { userEmail: email, workspaceId };
    const res = await this.post<LoginResponse>(
      "/api/plugin-auth/login",
      body,
      false
    );
    this.sessionToken = res.token;
    return res;
  }

  // -- Projects -----------------------------------------------

  /**
   * List projects accessible to the current session.
   * Backend: GET /api/plugin-auth/projects?token=X
   */
  async getProjects(): Promise<Project[]> {
    return this.get<Project[]>(
      `/api/plugin-auth/projects?token=${encodeURIComponent(this.sessionToken || "")}`
    );
  }

  // -- Component sync -----------------------------------------

  /** Push a Figma frame structure to the backend. */
  async pushComponent(data: PushComponentPayload): Promise<DesignComponent> {
    return this.post<DesignComponent>(
      "/api/plugin-sync/push-component",
      data
    );
  }

  /** Pull latest text fields for a component from the backend. */
  async pullComponent(componentId: string): Promise<PullComponentResponse> {
    return this.post<PullComponentResponse>(
      `/api/plugin-sync/pull-component/${componentId}`,
      {}
    );
  }

  // -- Design components (for change detection) ---------------

  /**
   * List all components for a project.
   * Backend: GET /api/projects/{projectId}/components
   */
  async getComponents(projectId: string): Promise<DesignComponent[]> {
    return this.get<DesignComponent[]>(
      `/api/projects/${projectId}/components`
    );
  }

  /**
   * Get a single component by ID.
   * Backend: GET /api/projects/{projectId}/components/{id}
   */
  async getComponent(
    projectId: string,
    componentId: string
  ): Promise<DesignComponent> {
    return this.get<DesignComponent>(
      `/api/projects/${projectId}/components/${componentId}`
    );
  }

  // -- Activity feed ------------------------------------------

  /**
   * Get activity feed for a project.
   * Backend: GET /api/activity-feed?projectId={projectId}
   * Falls back gracefully if endpoint not available.
   */
  async getActivity(
    projectId: string
  ): Promise<{ items: ActivityFeedItem[] }> {
    try {
      return await this.get<{ items: ActivityFeedItem[] }>(
        `/api/activity-feed?projectId=${projectId}`
      );
    } catch {
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

  private async post<T>(
    path: string,
    body: unknown,
    includeAuth = true
  ): Promise<T> {
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
    if (includeAuth && this.sessionToken) {
      h["Authorization"] = `Bearer ${this.sessionToken}`;
    }
    return h;
  }

  private async handleResponse<T>(res: Response): Promise<T> {
    if (!res.ok) {
      const text = await res.text().catch(() => "Unknown error");
      if (res.status === 401) {
        this.sessionToken = null;
        throw new Error("SESSION_EXPIRED");
      }
      throw new Error(`API ${res.status}: ${text}`);
    }
    return (await res.json()) as T;
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
