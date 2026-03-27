import { LocFlowApi } from "./api";
import type {
  MainMessage,
  UIMessage,
  Project,
  PushComponentPayload,
  DesignComponentTextField,
} from "./types";

// ---------------------------------------------------------------
// Plugin UI logic – runs inside the iframe
// Communicates with the Figma main thread via postMessage
// ---------------------------------------------------------------

const DEFAULT_BASE_URL = "https://app.locflow.io";

// --- State ---

let api: LocFlowApi;
let projects: Project[] = [];
let selectedProjectId = "";
let hasFrame = false;
let frameName = "";
let lastPushedComponentId = "";

// --- DOM refs (set after DOM ready) ---

let loginSection: HTMLElement;
let mainSection: HTMLElement;
let emailInput: HTMLInputElement;
let passwordInput: HTMLInputElement;
let loginBtn: HTMLButtonElement;
let loginError: HTMLElement;
let serverUrlInput: HTMLInputElement;
let projectSelect: HTMLSelectElement;
let pushBtn: HTMLButtonElement;
let pullBtn: HTMLButtonElement;
let selectionLabel: HTMLElement;
let statusArea: HTMLElement;
let logoutBtn: HTMLButtonElement;
let userLabel: HTMLElement;

// ---------------------------------------------------------------
// Initialise
// ---------------------------------------------------------------

document.addEventListener("DOMContentLoaded", () => {
  loginSection = document.getElementById("login-section")!;
  mainSection = document.getElementById("main-section")!;
  emailInput = document.getElementById("email") as HTMLInputElement;
  passwordInput = document.getElementById("password") as HTMLInputElement;
  loginBtn = document.getElementById("login-btn") as HTMLButtonElement;
  loginError = document.getElementById("login-error")!;
  serverUrlInput = document.getElementById("server-url") as HTMLInputElement;
  projectSelect = document.getElementById("project-select") as HTMLSelectElement;
  pushBtn = document.getElementById("push-btn") as HTMLButtonElement;
  pullBtn = document.getElementById("pull-btn") as HTMLButtonElement;
  selectionLabel = document.getElementById("selection-label")!;
  statusArea = document.getElementById("status-area")!;
  logoutBtn = document.getElementById("logout-btn") as HTMLButtonElement;
  userLabel = document.getElementById("user-label")!;

  // Try to restore session from localStorage
  const savedToken = localStorage.getItem("locflow_token");
  const savedUrl = localStorage.getItem("locflow_url") || DEFAULT_BASE_URL;
  const savedEmail = localStorage.getItem("locflow_email") || "";

  serverUrlInput.value = savedUrl;
  api = new LocFlowApi(savedUrl);

  if (savedToken) {
    api.setToken(savedToken);
    userLabel.textContent = savedEmail;
    showMainSection();
    loadProjects();
  }

  // --- Event listeners ---

  loginBtn.addEventListener("click", handleLogin);
  logoutBtn.addEventListener("click", handleLogout);
  pushBtn.addEventListener("click", handlePush);
  pullBtn.addEventListener("click", handlePull);

  projectSelect.addEventListener("change", () => {
    selectedProjectId = projectSelect.value;
  });

  // Allow pressing Enter in password field to login
  passwordInput.addEventListener("keydown", (e) => {
    if (e.key === "Enter") handleLogin();
  });
});

// ---------------------------------------------------------------
// Messages from the Figma main thread
// ---------------------------------------------------------------

window.onmessage = async (event: MessageEvent) => {
  const msg = event.data.pluginMessage as MainMessage;
  if (!msg) return;

  switch (msg.type) {
    case "selection-info":
      hasFrame = msg.hasFrame;
      frameName = msg.frameName;
      updateSelectionLabel();
      break;

    case "frame-data":
      await pushFrameData(msg.payload);
      break;

    case "error":
      setStatus(msg.message, "error");
      break;

    case "notify":
      setStatus(msg.message, "success");
      break;

    default:
      break;
  }
};

// ---------------------------------------------------------------
// Auth
// ---------------------------------------------------------------

async function handleLogin(): Promise<void> {
  const email = emailInput.value.trim();
  const password = passwordInput.value;
  const serverUrl = serverUrlInput.value.trim() || DEFAULT_BASE_URL;

  if (!email || !password) {
    loginError.textContent = "Email and password are required.";
    return;
  }

  loginError.textContent = "";
  loginBtn.disabled = true;
  loginBtn.textContent = "Logging in...";

  try {
    api = new LocFlowApi(serverUrl);
    const res = await api.login(email, password);

    // Persist session
    localStorage.setItem("locflow_token", res.token);
    localStorage.setItem("locflow_url", serverUrl);
    localStorage.setItem("locflow_email", res.email);

    userLabel.textContent = res.displayName || res.email;
    showMainSection();
    await loadProjects();
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : "Login failed";
    loginError.textContent = message;
  } finally {
    loginBtn.disabled = false;
    loginBtn.textContent = "Log in";
  }
}

function handleLogout(): void {
  api.clearToken();
  localStorage.removeItem("locflow_token");
  localStorage.removeItem("locflow_email");
  projects = [];
  selectedProjectId = "";
  lastPushedComponentId = "";
  showLoginSection();
}

// ---------------------------------------------------------------
// Projects
// ---------------------------------------------------------------

async function loadProjects(): Promise<void> {
  try {
    projects = await api.getProjects();
    projectSelect.innerHTML = "";

    if (projects.length === 0) {
      const opt = document.createElement("option");
      opt.textContent = "No projects found";
      opt.disabled = true;
      projectSelect.appendChild(opt);
      return;
    }

    for (const p of projects) {
      const opt = document.createElement("option");
      opt.value = p.id;
      opt.textContent = p.name;
      projectSelect.appendChild(opt);
    }

    selectedProjectId = projects[0].id;
    projectSelect.value = selectedProjectId;
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : "Failed to load projects";
    setStatus(message, "error");
  }
}

// ---------------------------------------------------------------
// Push
// ---------------------------------------------------------------

function handlePush(): void {
  if (!selectedProjectId) {
    setStatus("Select a project first.", "error");
    return;
  }

  if (!hasFrame) {
    setStatus("Select a frame in Figma first.", "error");
    return;
  }

  setStatus("Scanning frame...", "info");
  const msg: UIMessage = { type: "push-frame", projectId: selectedProjectId };
  parent.postMessage({ pluginMessage: msg }, "*");
}

async function pushFrameData(payload: PushComponentPayload): Promise<void> {
  setStatus(`Pushing "${payload.figmaFrameName}" (${payload.textFields.length} text layers)...`, "info");

  try {
    const component = await api.pushComponent(payload);
    lastPushedComponentId = component.id;
    setStatus(
      `Pushed "${payload.figmaFrameName}" — ${payload.textFields.length} text layer(s) synced.`,
      "success"
    );
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : "Push failed";
    setStatus(message, "error");
  }
}

// ---------------------------------------------------------------
// Pull
// ---------------------------------------------------------------

async function handlePull(): Promise<void> {
  if (!lastPushedComponentId) {
    setStatus("Push a frame first, or enter a component ID.", "error");
    return;
  }

  setStatus("Pulling latest text...", "info");

  try {
    const result = await api.pullComponent(lastPushedComponentId);
    setStatus(
      `Pulled ${result.textFields.length} text field(s). Applying to Figma...`,
      "info"
    );

    // Send text fields to main thread for application
    parent.postMessage(
      {
        pluginMessage: {
          type: "apply-pull",
          textFields: result.textFields,
        },
      },
      "*"
    );
  } catch (err: unknown) {
    const message = err instanceof Error ? err.message : "Pull failed";
    setStatus(message, "error");
  }
}

// ---------------------------------------------------------------
// UI helpers
// ---------------------------------------------------------------

function showLoginSection(): void {
  loginSection.style.display = "block";
  mainSection.style.display = "none";
}

function showMainSection(): void {
  loginSection.style.display = "none";
  mainSection.style.display = "block";
}

function updateSelectionLabel(): void {
  if (hasFrame) {
    selectionLabel.textContent = `Selected: ${frameName}`;
    selectionLabel.className = "selection-label has-frame";
  } else {
    selectionLabel.textContent = "No frame selected";
    selectionLabel.className = "selection-label no-frame";
  }
}

function setStatus(message: string, level: "info" | "success" | "error"): void {
  statusArea.textContent = message;
  statusArea.className = `status-area status-${level}`;
}
