import type {
  LoginRequest,
  LoginResponse,
  Project,
  PushComponentPayload,
  DesignComponent,
  PullComponentResponse,
} from "./types";

// ---------------------------------------------------------------
// LocFlow API client – used from the plugin UI iframe
// ---------------------------------------------------------------

export class LocFlowApi {
  private baseUrl: string;
  private authToken: string | null = null;

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl.replace(/\/+$/, "");
  }

  // -- Auth ---------------------------------------------------

  setToken(token: string): void {
    this.authToken = token;
  }

  clearToken(): void {
    this.authToken = null;
  }

  get isAuthenticated(): boolean {
    return this.authToken !== null;
  }

  /** Authenticate with email + password. Stores the token on success. */
  async login(email: string, password: string): Promise<LoginResponse> {
    const body: LoginRequest = { email, password };
    const res = await this.post<LoginResponse>("/api/plugin/login", body);
    this.authToken = res.token;
    return res;
  }

  // -- Projects -----------------------------------------------

  /** List projects accessible to the current user. */
  async getProjects(): Promise<Project[]> {
    return this.get<Project[]>("/api/plugin/projects");
  }

  // -- Component sync -----------------------------------------

  /** Push a Figma frame structure to the backend. */
  async pushComponent(data: PushComponentPayload): Promise<DesignComponent> {
    return this.post<DesignComponent>("/api/plugin-sync/push-component", data);
  }

  /** Pull latest text fields for a component from the backend. */
  async pullComponent(componentId: string): Promise<PullComponentResponse> {
    return this.post<PullComponentResponse>(
      `/api/plugin-sync/pull-component/${componentId}`,
      {}
    );
  }

  // -- HTTP helpers -------------------------------------------

  private async get<T>(path: string): Promise<T> {
    const res = await fetch(`${this.baseUrl}${path}`, {
      method: "GET",
      headers: this.headers(),
    });
    return this.handleResponse<T>(res);
  }

  private async post<T>(path: string, body: unknown): Promise<T> {
    const res = await fetch(`${this.baseUrl}${path}`, {
      method: "POST",
      headers: this.headers(),
      body: JSON.stringify(body),
    });
    return this.handleResponse<T>(res);
  }

  private headers(): Record<string, string> {
    const h: Record<string, string> = {
      "Content-Type": "application/json",
    };
    if (this.authToken) {
      h["Authorization"] = `Bearer ${this.authToken}`;
    }
    return h;
  }

  private async handleResponse<T>(res: Response): Promise<T> {
    if (!res.ok) {
      const text = await res.text().catch(() => "Unknown error");
      throw new Error(`API ${res.status}: ${text}`);
    }
    return (await res.json()) as T;
  }
}
