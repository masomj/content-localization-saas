import { InterCopyApi, decodeJwt } from "./api";
import type {
  MainMessage,
  UIMessage,
  Project,
  PushComponentPayload,
  DesignComponent,
  FrameInfo,
  TextNodeInfo,
  TabName,
  ActivityEntry,
  ReviewEntry,
  ChangeEntry,
  ChangedTextField,
  EditableTextField,
  SyncStatus,
} from "./types";

// ---------------------------------------------------------------
// InterCopy Plugin UI — Full multi-tab Frontitude-style interface
// ---------------------------------------------------------------

const DEFAULT_BASE_URL = "https://intercopy.co.uk";

// ---------------------------------------------------------------
// Safe storage — Figma plugin iframes block localStorage (data: URI
// sandbox), so we fall back to an in-memory Map.
// ---------------------------------------------------------------

const memoryStore = new Map<string, string>();
let storageAvailable = false;
try {
  // Test if localStorage is accessible
  const testKey = "__intercopy_test__";
  window.localStorage.setItem(testKey, "1");
  window.localStorage.removeItem(testKey);
  storageAvailable = true;
} catch (_) {
  storageAvailable = false;
}

const storage = {
  getItem(key: string): string | null {
    if (storageAvailable) {
      try { return window.localStorage.getItem(key); } catch (_) { /* fall through */ }
    }
    return memoryStore.get(key) ?? null;
  },
  setItem(key: string, value: string): void {
    if (storageAvailable) {
      try { window.localStorage.setItem(key, value); return; } catch (_) { /* fall through */ }
    }
    memoryStore.set(key, value);
    // Persist to main.ts via figma.clientStorage
    parent.postMessage({ pluginMessage: { type: "storage-set", key, value } }, "*");
  },
  removeItem(key: string): void {
    if (storageAvailable) {
      try { window.localStorage.removeItem(key); return; } catch (_) { /* fall through */ }
    }
    memoryStore.delete(key);
    parent.postMessage({ pluginMessage: { type: "storage-remove", key } }, "*");
  },
};

// ---------------------------------------------------------------
// State
// ---------------------------------------------------------------

let api: InterCopyApi;
let projects: Project[] = [];
let selectedProjectId = "";
let currentTab: TabName = "activity";
let fileKey = "unknown";

// User info
let userName = "";
let userEmail = "";

// Device auth flow state
let deviceAuthPollTimer: ReturnType<typeof setInterval> | null = null;
let currentDeviceCode: string | null = null;

// Edit tab
let selectedFrames: FrameInfo[] = [];
let editableFields: EditableTextField[] = [];
let editDebounceTimers: Record<string, ReturnType<typeof setTimeout>> = {};

// Review tab
let reviewQueue: ReviewEntry[] = [];

// Changes tab
let changeEntries: ChangeEntry[] = [];
let remoteComponents: DesignComponent[] = [];
let allLocalFrames: FrameInfo[] = [];
let isSyncing = false;
let syncInProgress = false;
let expandedChangeFrames: Set<string> = new Set();

// Activity tab
let activityLog: ActivityEntry[] = [];

// Settings
let autoSync = false;
let showNotifications = true;

// ---------------------------------------------------------------
// DOM init
// ---------------------------------------------------------------

let initialized = false;

document.addEventListener("DOMContentLoaded", () => {
  // Set up initial API with default URL (will be overridden by storage-data)
  const serverUrlInput = $<HTMLInputElement>("server-url");
  serverUrlInput.value = DEFAULT_BASE_URL;
  api = new InterCopyApi(DEFAULT_BASE_URL);

  bindEvents();

  // Request stored values from main.ts (figma.clientStorage)
  // This will trigger handleStorageData which calls initFromStorage
  parent.postMessage({ pluginMessage: { type: "storage-request" } }, "*");

  // Also try from in-memory/local storage immediately (may work if localStorage available)
  if (storageAvailable) {
    initFromStorage();
  }
});

function initFromStorage(): void {
  if (initialized) return;

  const savedAccessToken = storage.getItem("intercopy_access_token");
  const savedRefreshToken = storage.getItem("intercopy_refresh_token");
  const savedUrl = storage.getItem("intercopy_url") || DEFAULT_BASE_URL;
  const savedProject = storage.getItem("intercopy_project") || "";
  const savedActivity = storage.getItem("intercopy_activity");
  const savedReview = storage.getItem("intercopy_review");

  // Restore activity and review
  if (savedActivity) {
    try { activityLog = JSON.parse(savedActivity); } catch (_) { activityLog = []; }
  }
  if (savedReview) {
    try { reviewQueue = JSON.parse(savedReview); } catch (_) { reviewQueue = []; }
  }

  const serverUrlInput = $<HTMLInputElement>("server-url");
  serverUrlInput.value = savedUrl;
  api = new InterCopyApi(savedUrl);

  if (savedAccessToken && savedRefreshToken) {
    initialized = true;
    api.setToken(savedAccessToken);
    api.setRefreshToken(savedRefreshToken);

    // Extract user info from JWT
    const payload = decodeJwt(savedAccessToken);
    userEmail = payload.email || "";
    userName = payload.name || payload.given_name || userEmail;

    selectedProjectId = savedProject;
    showMainSection();
    loadProjects();
    loadProjectLanguages();
    requestFileKey();
    requestSelection();
  }

  // Load settings
  autoSync = storage.getItem("intercopy_autosync") === "true";
  showNotifications = storage.getItem("intercopy_notifications") !== "false";
}

function handleStorageData(entries: Record<string, string | null>): void {
  // Hydrate in-memory store with values from figma.clientStorage
  for (const [key, value] of Object.entries(entries)) {
    if (value !== null) {
      memoryStore.set(key, value);
    }
  }
  // Now re-init with the hydrated values
  initFromStorage();
}

// ---------------------------------------------------------------
// Event bindings
// ---------------------------------------------------------------

function bindEvents(): void {
  // Login — device auth flow
  $("login-btn").addEventListener("click", handleStartDeviceAuth);

  // Device auth code — copy button
  $("device-copy-btn").addEventListener("click", handleCopyCode);

  // Device auth — cancel button
  $("device-cancel-btn").addEventListener("click", handleCancelDeviceAuth);

  // Device auth — open verify URL in browser via main thread
  $("device-verify-url").addEventListener("click", () => {
    const url = $("device-verify-url").textContent || "";
    if (url && url !== "—") {
      parent.postMessage({ pluginMessage: { type: "open-external", url } }, "*");
    }
  });

  // Tabs
  document.querySelectorAll(".tab-item").forEach((el) => {
    el.addEventListener("click", () => {
      const tab = (el as HTMLElement).dataset.tab as TabName;
      switchTab(tab);
    });
  });

  // Settings
  $("settings-btn").addEventListener("click", () => toggleSettings(true));
  $("settings-back-btn").addEventListener("click", () => toggleSettings(false));
  $("settings-logout-btn").addEventListener("click", handleLogout);

  // Settings toggles
  $("toggle-autosync").addEventListener("click", () => {
    autoSync = !autoSync;
    $("toggle-autosync").classList.toggle("on", autoSync);
    storage.setItem("intercopy_autosync", String(autoSync));
  });
  $("toggle-notifications").addEventListener("click", () => {
    showNotifications = !showNotifications;
    $("toggle-notifications").classList.toggle("on", showNotifications);
    storage.setItem("intercopy_notifications", String(showNotifications));
  });

  // Project select
  $<HTMLSelectElement>("project-select").addEventListener("change", (e) => {
    selectedProjectId = (e.target as HTMLSelectElement).value;
    storage.setItem("intercopy_project", selectedProjectId);
    updateProjectDisplay();
    refreshChangesTab();
    loadProjectLanguages();
  });

  // Settings open in InterCopy
  $("settings-open-InterCopy").addEventListener("click", () => {
    const url = storage.getItem("intercopy_url") || DEFAULT_BASE_URL;
    window.open(`${url}/projects/${selectedProjectId}`, "_blank");
  });

  // Edit tab buttons
  $("edit-review-btn")?.addEventListener("click", handleAddToReview);
  $("edit-tags-btn")?.addEventListener("click", () => showToast("Tags feature coming soon", "info"));
  $("edit-pull-lang-btn")?.addEventListener("click", handlePullLanguage);

  // Changes tab
  $("sync-btn").addEventListener("click", handleSync);

  // Settings toggle init state
  $("toggle-autosync").classList.toggle("on", autoSync);
  $("toggle-notifications").classList.toggle("on", showNotifications);
}

// ---------------------------------------------------------------
// Messages from main thread
// ---------------------------------------------------------------

window.onmessage = (event: MessageEvent) => {
  const msg = event.data.pluginMessage as MainMessage;
  if (!msg) return;

  switch (msg.type) {
    case "selection-changed":
      handleSelectionChanged(msg.frames);
      break;

    case "selection-empty":
      handleSelectionEmpty();
      break;

    case "all-frames":
      handleAllFrames(msg.frames);
      break;

    case "frame-data":
      handleFrameData(msg.payload);
      break;

    case "multi-frame-data":
      handleMultiFrameData(msg.frames);
      break;

    case "multi-frame-payloads":
      handleMultiFramePayloads(msg.payloads);
      break;

    case "text-updated":
      handleTextUpdated(msg.layerId, msg.newText);
      break;

    case "file-key":
      fileKey = msg.fileKey;
      $("settings-file-key").textContent = fileKey;
      break;

    case "current-user":
      userName = msg.name;
      if (msg.email) userEmail = msg.email;
      updateUserDisplay();
      break;

    case "error":
      showToast(msg.message, "error");
      break;

    case "notify":
      showToast(msg.message, "success");
      break;

    case "storage-data": {
      const sd = msg as MainMessage & { type: "storage-data" };
      handleStorageData(sd.entries);
      break;
    }
  }
};

// ---------------------------------------------------------------
// Tab management
// ---------------------------------------------------------------

function switchTab(tab: TabName): void {
  currentTab = tab;

  // Update tab bar
  document.querySelectorAll(".tab-item").forEach((el) => {
    el.classList.toggle("active", (el as HTMLElement).dataset.tab === tab);
  });

  // Update panels
  document.querySelectorAll(".tab-panel").forEach((el) => {
    el.classList.toggle("active", el.id === `panel-${tab}`);
  });

  // Tab-specific actions
  switch (tab) {
    case "activity":
      renderActivity();
      break;
    case "edit":
      renderEditTab();
      break;
    case "review":
      renderReviewTab();
      break;
    case "changes":
      refreshChangesTab();
      break;
    case "library":
      renderLibraryTab();
      break;
  }
}

// ---------------------------------------------------------------
// AUTH — Device Authorization Flow (RFC 8628)
// ---------------------------------------------------------------

async function handleStartDeviceAuth(): Promise<void> {
  const serverUrl = $<HTMLInputElement>("server-url").value.trim() || DEFAULT_BASE_URL;

  $("login-error").textContent = "";
  const btn = $<HTMLButtonElement>("login-btn");
  btn.disabled = true;
  btn.textContent = "Connecting...";

  try {
    api = new InterCopyApi(serverUrl);
    const res = await api.startDeviceAuth();

    // Save URL immediately
    storage.setItem("intercopy_url", serverUrl);

    // Show the device code screen
    currentDeviceCode = res.deviceCode;
    $("device-user-code").textContent = res.userCode;
    $("device-verify-url").textContent = res.verificationUri;

    // Show device code section, hide login form
    $("login-form-area").style.display = "none";
    $("device-code-area").style.display = "flex";

    // Start polling
    const interval = Math.max(res.interval, 5) * 1000;
    deviceAuthPollTimer = setInterval(() => pollDeviceAuth(), interval);
  } catch (err: unknown) {
    $("login-error").textContent = err instanceof Error ? err.message : "Failed to start device auth";
  } finally {
    btn.disabled = false;
    btn.textContent = "Connect to InterCopy";
  }
}

async function pollDeviceAuth(): Promise<void> {
  if (!currentDeviceCode) return;

  try {
    const res = await api.pollDeviceAuth(currentDeviceCode);

    if (res.status === "complete" && res.accessToken) {
      // Stop polling
      stopDeviceAuthPolling();

      // Store tokens
      api.setToken(res.accessToken);
      if (res.refreshToken) {
        api.setRefreshToken(res.refreshToken);
        storage.setItem("intercopy_refresh_token", res.refreshToken);
      }
      storage.setItem("intercopy_access_token", res.accessToken);

      // Extract user info
      userEmail = res.user?.email || "";
      userName = res.user?.name || userEmail;

      showMainSection();
      await loadProjects();
      loadProjectLanguages();
      requestFileKey();
      requestSelection();
    } else if (res.status === "expired") {
      stopDeviceAuthPolling();
      handleCancelDeviceAuth();
      $("login-error").textContent = "Code expired. Please try again.";
    }
    // "pending" — continue polling
  } catch (err: unknown) {
    stopDeviceAuthPolling();
    handleCancelDeviceAuth();
    $("login-error").textContent = err instanceof Error ? err.message : "Polling failed";
  }
}

function stopDeviceAuthPolling(): void {
  if (deviceAuthPollTimer !== null) {
    clearInterval(deviceAuthPollTimer);
    deviceAuthPollTimer = null;
  }
  currentDeviceCode = null;
}

function handleCancelDeviceAuth(): void {
  stopDeviceAuthPolling();
  $("login-form-area").style.display = "flex";
  $("device-code-area").style.display = "none";
}

function handleCopyCode(): void {
  const code = $("device-user-code").textContent || "";
  // Figma iframe may block clipboard API — use fallback
  if (navigator.clipboard && navigator.clipboard.writeText) {
    navigator.clipboard.writeText(code).then(
      () => showToast("Code copied!", "success"),
      () => fallbackCopy(code)
    );
  } else {
    fallbackCopy(code);
  }
}

function fallbackCopy(text: string): void {
  // Create a temporary input, select it, and use execCommand
  const input = document.createElement("input");
  input.value = text;
  input.style.position = "fixed";
  input.style.opacity = "0";
  document.body.appendChild(input);
  input.select();
  try {
    document.execCommand("copy");
    showToast("Code copied!", "success");
  } catch (_) {
    showToast("Code: " + text + " (copy manually)", "info");
  }
  document.body.removeChild(input);
}

/** Persist current API tokens to localStorage after refresh. */
function persistTokens(): void {
  if (api.token) {
    storage.setItem("intercopy_access_token", api.token);
  }
}

function handleLogout(): void {
  api.clearToken();
  storage.removeItem("intercopy_access_token");
  storage.removeItem("intercopy_refresh_token");
  storage.removeItem("intercopy_project");
  projects = [];
  selectedProjectId = "";
  activityLog = [];
  reviewQueue = [];
  toggleSettings(false);
  showLoginSection();
}

// ---------------------------------------------------------------
// PROJECTS
// ---------------------------------------------------------------

async function loadProjects(): Promise<void> {
  try {
    projects = await api.getProjects();
    persistTokens();
    const select = $<HTMLSelectElement>("project-select");
    select.innerHTML = "";

    if (projects.length === 0) {
      select.innerHTML = '<option disabled selected>No projects found</option>';
      return;
    }

    for (const p of projects) {
      const opt = document.createElement("option");
      opt.value = p.id;
      opt.textContent = p.name;
      select.appendChild(opt);
    }

    // Restore or default
    if (selectedProjectId && projects.some((p) => p.id === selectedProjectId)) {
      select.value = selectedProjectId;
    } else {
      selectedProjectId = projects[0].id;
      select.value = selectedProjectId;
      storage.setItem("intercopy_project", selectedProjectId);
    }

    updateProjectDisplay();
  } catch (err: unknown) {
    if (err instanceof Error && err.message === "SESSION_EXPIRED") {
      handleLogout();
      $("login-error").textContent = "Session expired. Please reconnect.";
    } else {
      showToast(err instanceof Error ? err.message : "Failed to load projects", "error");
    }
  }
}

function updateProjectDisplay(): void {
  const project = projects.find((p) => p.id === selectedProjectId);
  $("project-name-display").textContent = project?.name || "InterCopy";
  $("settings-project-name").textContent = project?.name || "—";
}

// ---------------------------------------------------------------
// SELECTION HANDLING
// ---------------------------------------------------------------

function handleSelectionChanged(frames: FrameInfo[]): void {
  selectedFrames = frames;

  // Build editable fields
  editableFields = [];
  for (const frame of frames) {
    for (const tn of frame.textNodes) {
      const syncedText = findSyncedText(frame.frameId, tn.layerId);
      let status: SyncStatus = "unlinked";
      if (syncedText !== null) {
        status = syncedText === tn.characters ? "synced" : "modified";
      } else {
        status = "draft";
      }

      editableFields.push({
        layerId: tn.layerId,
        layerName: tn.layerName,
        characters: tn.characters,
        originalCharacters: tn.characters,
        syncedCharacters: syncedText,
        status,
      });
    }
  }

  // Auto-switch to Edit tab if user just selected something
  if (currentTab === "activity" || currentTab === "edit") {
    switchTab("edit");
  } else {
    renderEditTab();
  }
}

function handleSelectionEmpty(): void {
  selectedFrames = [];
  editableFields = [];
  if (currentTab === "edit") {
    renderEditTab();
  }
}

function findSyncedText(frameId: string, layerId: string): string | null {
  for (const comp of remoteComponents) {
    if (comp.figmaFrameId === frameId) {
      const field = comp.textFields?.find((tf) => tf.figmaLayerId === layerId);
      if (field) return field.currentText;
    }
  }
  return null;
}

// ---------------------------------------------------------------
// EDIT TAB
// ---------------------------------------------------------------

function renderEditTab(): void {
  const editEmpty = $("edit-empty");
  const editContent = $("edit-content");

  if (selectedFrames.length === 0 || editableFields.length === 0) {
    editEmpty.classList.remove("hidden");
    editEmpty.style.display = "";
    editContent.classList.add("hidden");
    editContent.style.display = "none";
    return;
  }

  editEmpty.classList.add("hidden");
  editEmpty.style.display = "none";
  editContent.classList.remove("hidden");
  editContent.style.display = "flex";

  const totalTexts = editableFields.length;
  $("edit-header").textContent = `Texts in selection (${totalTexts})`;
  $("edit-frame-name").textContent = selectedFrames.map((f) => f.frameName).join(", ");

  // Update status tag
  const draftCount = editableFields.filter((f) => f.status === "draft" || f.status === "modified").length;
  const syncedCount = editableFields.filter((f) => f.status === "synced").length;
  const statusTag = $("edit-status-tag");
  if (draftCount > 0) {
    statusTag.innerHTML = `<span class="status-dot draft"></span> ${draftCount} draft`;
  } else {
    statusTag.innerHTML = `<span class="status-dot synced"></span> All synced`;
  }

  // Render text fields
  const list = $("edit-text-list");
  list.innerHTML = "";

  for (const field of editableFields) {
    const item = document.createElement("div");
    item.className = "edit-text-item";
    item.innerHTML = `
      <div class="edit-text-header">
        <span class="status-dot ${field.status}"></span>
        <span class="edit-layer-name" title="${esc(field.layerName)}">${esc(field.layerName)}</span>
      </div>
      <input
        class="edit-text-input"
        type="text"
        value="${esc(field.characters)}"
        data-layer-id="${esc(field.layerId)}"
        data-original="${esc(field.originalCharacters)}"
      />
    `;

    const input = item.querySelector("input")!;
    input.addEventListener("input", (e) => {
      const target = e.target as HTMLInputElement;
      const layerId = target.dataset.layerId!;
      const newText = target.value;

      // Update local state
      const ef = editableFields.find((f) => f.layerId === layerId);
      if (ef) {
        ef.characters = newText;
        if (ef.syncedCharacters !== null) {
          ef.status = newText === ef.syncedCharacters ? "synced" : "modified";
        } else {
          ef.status = newText === ef.originalCharacters ? "draft" : "modified";
        }
        // Update the status dot
        const dot = item.querySelector(".status-dot") as HTMLElement;
        if (dot) {
          dot.className = `status-dot ${ef.status}`;
        }
      }

      // Debounced update to Figma
      if (editDebounceTimers[layerId]) {
        clearTimeout(editDebounceTimers[layerId]);
      }
      editDebounceTimers[layerId] = setTimeout(() => {
        postToMain({ type: "update-text", layerId, newText });
        delete editDebounceTimers[layerId];
      }, 400);
    });

    list.appendChild(item);
  }
}

function handleTextUpdated(layerId: string, newText: string): void {
  // Update local state after main thread confirms
  const field = editableFields.find((f) => f.layerId === layerId);
  if (field) {
    field.characters = newText;
    field.originalCharacters = newText;
  }
}

// ---------------------------------------------------------------
// REVIEW TAB
// ---------------------------------------------------------------

async function handlePullLanguage(): Promise<void> {
  const langSelect = document.getElementById("global-language-select") as HTMLSelectElement | null;
  const language = langSelect?.value || "";

  if (!selectedFrames.length) {
    showToast("No frame selected", "error");
    return;
  }

  // Find the component ID for the selected frame by checking synced components
  const frame = selectedFrames[0];
  if (!frame) return;

  try {
    showToast(language ? `Pulling ${language} translations...` : "Pulling source text...", "info");
    const components = await api.getComponents(selectedProjectId);
    const comp = components.find((c: any) => c.figmaFrameId === frame.frameId);

    if (!comp) {
      showToast("Frame not synced yet. Push first.", "error");
      return;
    }

    const result = await api.pullComponent(comp.id, language || undefined);
    // Send text updates to Figma main thread
    const textFields = result.textFields || [];
    parent.postMessage({
      pluginMessage: { type: "apply-pull", textFields }
    }, "*");

    showToast(
      language
        ? `Applied ${language} translations to ${textFields.length} fields`
        : `Pulled source text for ${textFields.length} fields`,
      "success"
    );

    // Populate language dropdown if we got languages back
    if (result.languages && langSelect) {
      populateLanguageDropdown(langSelect, result.languages);
    }
  } catch (err: unknown) {
    showToast(err instanceof Error ? err.message : "Pull failed", "error");
  }
}

function populateLanguageDropdown(
  select: HTMLSelectElement,
  languages: Array<{ bcp47Code: string; isSource: boolean }>
): void {
  const current = select.value;
  select.innerHTML = '<option value="">Source</option>';
  for (const lang of languages) {
    if (lang.isSource) continue;
    const opt = document.createElement("option");
    opt.value = lang.bcp47Code;
    opt.textContent = lang.bcp47Code;
    select.appendChild(opt);
  }
  select.value = current;
}

/** Load available languages for the selected project and populate the global dropdown */
async function loadProjectLanguages(): Promise<void> {
  if (!selectedProjectId) return;
  try {
    const components = await api.getComponents(selectedProjectId);
    if (components.length > 0) {
      // Pull component to get languages (no language param = just get metadata + languages)
      const result = await api.pullComponent(components[0].id);
      const globalSelect = document.getElementById("global-language-select") as HTMLSelectElement | null;
      if (globalSelect && result.languages) {
        populateLanguageDropdown(globalSelect, result.languages);
      }
    }
  } catch (_) {
    // Non-critical — languages just won't populate
  }
}

function handleAddToReview(): void {
  if (editableFields.length === 0) {
    showToast("No texts selected", "error");
    return;
  }

  let added = 0;
  for (const field of editableFields) {
    // Don't add duplicates
    if (reviewQueue.some((r) => r.layerId === field.layerId)) continue;

    const frame = selectedFrames.find((f) =>
      f.textNodes.some((tn) => tn.layerId === field.layerId)
    );

    reviewQueue.push({
      layerId: field.layerId,
      layerName: field.layerName,
      text: field.characters,
      frameName: frame?.frameName || "Unknown",
      frameId: frame?.frameId || "",
      addedAt: new Date().toISOString(),
    });
    added++;
  }

  storage.setItem("intercopy_review", JSON.stringify(reviewQueue));

  if (added > 0) {
    showToast(`Added ${added} text${added !== 1 ? "s" : ""} to review`, "success");
    addActivity("submitted_changes", `You submitted ${added} text${added !== 1 ? "s" : ""} for review`, added);
    updateReviewBadge();
  } else {
    showToast("All selected texts are already in review", "info");
  }
}

function renderReviewTab(): void {
  const list = $("review-list");
  const empty = $("review-empty");

  if (reviewQueue.length === 0) {
    list.style.display = "none";
    empty.classList.remove("hidden");
    empty.style.display = "";
    $("review-header").textContent = "Texts for review";
    return;
  }

  empty.classList.add("hidden");
  empty.style.display = "none";
  list.style.display = "";
  $("review-header").textContent = `Texts for review (${reviewQueue.length})`;

  list.innerHTML = "";
  for (const entry of reviewQueue) {
    const item = document.createElement("div");
    item.className = "review-item";
    item.innerHTML = `
      <div class="review-item-header">
        <span class="review-item-name">${esc(entry.layerName)}</span>
        <span class="review-item-frame">${esc(entry.frameName)}</span>
      </div>
      <div class="review-item-text">${esc(entry.text)}</div>
    `;
    list.appendChild(item);
  }
}

function updateReviewBadge(): void {
  const tab = document.querySelector('[data-tab="review"]');
  if (!tab) return;
  const existing = tab.querySelector(".tab-badge");
  if (existing) existing.remove();

  if (reviewQueue.length > 0) {
    const badge = document.createElement("span");
    badge.className = "tab-badge";
    badge.textContent = String(reviewQueue.length);
    tab.appendChild(badge);
  }
}

// ---------------------------------------------------------------
// CHANGES TAB (CRITICAL)
// ---------------------------------------------------------------

async function refreshChangesTab(): Promise<void> {
  if (!selectedProjectId || !api.isAuthenticated) return;

  // 1. Fetch remote components for this project
  try {
    remoteComponents = await api.getComponents(selectedProjectId);
    persistTokens();
  } catch (_) {
    remoteComponents = [];
  }

  // 2. Scan all local frames
  postToMain({ type: "scan-all-frames" });
}

function handleAllFrames(frames: FrameInfo[]): void {
  allLocalFrames = frames;
  computeChanges();
  renderChangesTab();
}

function computeChanges(): void {
  changeEntries = [];

  // Build a map of remote components by figmaFrameId
  const remoteMap = new Map<string, DesignComponent>();
  for (const comp of remoteComponents) {
    remoteMap.set(comp.figmaFrameId, comp);
  }

  for (const frame of allLocalFrames) {
    const remote = remoteMap.get(frame.frameId);

    if (!remote) {
      // New frame — never synced
      const changedTexts: ChangedTextField[] = frame.textNodes.map((tn) => ({
        layerId: tn.layerId,
        layerName: tn.layerName,
        localText: tn.characters,
        remoteText: null,
      }));

      if (changedTexts.length > 0) {
        changeEntries.push({
          frameId: frame.frameId,
          frameName: frame.frameName,
          isNew: true,
          changedTexts,
          componentId: null,
        });
      }
    } else {
      // Existing frame — compare texts
      const changedTexts: ChangedTextField[] = [];

      for (const tn of frame.textNodes) {
        const remoteField = remote.textFields?.find(
          (tf) => tf.figmaLayerId === tn.layerId
        );
        if (!remoteField) {
          // New text node in existing frame
          changedTexts.push({
            layerId: tn.layerId,
            layerName: tn.layerName,
            localText: tn.characters,
            remoteText: null,
          });
        } else if (remoteField.currentText !== tn.characters) {
          // Text changed
          changedTexts.push({
            layerId: tn.layerId,
            layerName: tn.layerName,
            localText: tn.characters,
            remoteText: remoteField.currentText,
          });
        }
      }

      if (changedTexts.length > 0) {
        changeEntries.push({
          frameId: frame.frameId,
          frameName: frame.frameName,
          isNew: false,
          changedTexts,
          componentId: remote.id,
        });
      }
    }
  }

  // Update the Changes badge
  updateChangesBadge();
}

function renderChangesTab(): void {
  const list = $("changes-list");
  const empty = $("changes-empty");
  const bottomBar = $("changes-bottom-bar");
  const syncingArea = $("changes-syncing");
  const header = $("changes-header");

  if (isSyncing) {
    list.style.display = "none";
    empty.classList.add("hidden");
    empty.style.display = "none";
    syncingArea.classList.remove("hidden");
    bottomBar.style.display = "none";
    return;
  }

  syncingArea.classList.add("hidden");

  if (changeEntries.length === 0) {
    list.style.display = "none";
    empty.classList.remove("hidden");
    empty.style.display = "";
    bottomBar.style.display = "none";
    header.textContent = "All changes (0)";
    return;
  }

  empty.classList.add("hidden");
  empty.style.display = "none";
  list.style.display = "";
  bottomBar.style.display = "flex";

  const totalChanges = changeEntries.reduce((sum, e) => sum + e.changedTexts.length, 0);
  header.textContent = `All changes (${totalChanges})`;

  list.innerHTML = "";

  for (const entry of changeEntries) {
    const isExpanded = expandedChangeFrames.has(entry.frameId);
    const row = document.createElement("div");
    row.className = "change-frame-row";
    row.innerHTML = `
      <div class="change-frame-header">
        <div class="change-frame-icon">
          <svg viewBox="0 0 14 14" fill="none" stroke="currentColor" stroke-width="1.2">
            <rect x="1" y="1" width="12" height="12" rx="2"/>
            <line x1="1" y1="5" x2="13" y2="5"/>
            <line x1="5" y1="5" x2="5" y2="13"/>
          </svg>
        </div>
        <div class="change-frame-info">
          <div class="change-frame-name">${esc(entry.frameName)}</div>
          <div class="change-frame-meta">
            ${entry.changedTexts.length} text${entry.changedTexts.length !== 1 ? "s" : ""}
            <span class="change-tag ${entry.isNew ? "new" : "modified"}">${entry.isNew ? "New" : "Modified"}</span>
          </div>
        </div>
        <div class="change-expand-icon ${isExpanded ? "expanded" : ""}">
          <svg viewBox="0 0 12 12" fill="currentColor"><path d="M4 2l4 4-4 4z"/></svg>
        </div>
      </div>
      <div class="change-texts ${isExpanded ? "expanded" : ""}" id="change-texts-${esc(entry.frameId)}">
        ${entry.changedTexts
          .map(
            (ct) => `
          <div class="change-text-item">
            <span class="status-dot ${ct.remoteText === null ? "new" : "modified"}"></span>
            <span class="change-text-name" title="${esc(ct.layerName)}">${esc(ct.layerName)}</span>
            <span class="change-text-value" title="${esc(ct.localText)}">${esc(ct.localText)}</span>
          </div>
        `
          )
          .join("")}
      </div>
    `;

    // Toggle expand on click
    const header = row.querySelector(".change-frame-header") as HTMLElement;
    header.addEventListener("click", () => {
      const textsDiv = row.querySelector(".change-texts") as HTMLElement;
      const icon = row.querySelector(".change-expand-icon") as HTMLElement;
      const isExp = textsDiv.classList.contains("expanded");

      if (isExp) {
        textsDiv.classList.remove("expanded");
        icon.classList.remove("expanded");
        expandedChangeFrames.delete(entry.frameId);
      } else {
        textsDiv.classList.add("expanded");
        icon.classList.add("expanded");
        expandedChangeFrames.add(entry.frameId);
      }
    });

    list.appendChild(row);
  }
}

function updateChangesBadge(): void {
  const tab = document.querySelector('[data-tab="changes"]');
  if (!tab) return;
  const existing = tab.querySelector(".tab-badge");
  if (existing) existing.remove();

  if (changeEntries.length > 0) {
    const badge = document.createElement("span");
    badge.className = "tab-badge";
    badge.textContent = String(changeEntries.length);
    tab.appendChild(badge);
  }
}

// ---------------------------------------------------------------
// SYNC (Changes tab -> push all changes to API)
// ---------------------------------------------------------------

async function handleSync(): Promise<void> {
  if (changeEntries.length === 0 || !selectedProjectId) return;

  isSyncing = true;
  syncInProgress = true;
  renderChangesTab();

  $("sync-progress-text").textContent = `Exporting ${changeEntries.length} frame(s)...`;

  // Collect frame IDs to sync
  const frameIds = changeEntries.map(e => e.frameId);

  // Send to main thread — it will export each frame with thumbnail and return payloads
  parent.postMessage({
    pluginMessage: {
      type: "push-frames",
      projectId: selectedProjectId,
      frameIds,
    }
  }, "*");

  // The response comes back as "multi-frame-payloads" message handled by handleMultiFramePayloads
  isSyncing = false;
}

// ---------------------------------------------------------------
// Handle frame data from main thread (push flow)
// ---------------------------------------------------------------

async function handleFrameData(payload: PushComponentPayload): Promise<void> {
  try {
    const component = await api.pushComponent(payload);
    persistTokens();
    showToast(
      `Pushed "${payload.figmaFrameName}" — ${payload.textFields.length} text layers`,
      "success"
    );
    addActivity(
      "connected_frame",
      `You connected frame "${payload.figmaFrameName}"`,
      1
    );
    refreshChangesTab();
  } catch (err: unknown) {
    showToast(err instanceof Error ? err.message : "Push failed", "error");
  }
}

function handleMultiFrameData(frames: FrameInfo[]): void {
  // Legacy handler — no longer used for sync
}

/** Handle payloads with thumbnails from main thread for sync */
async function handleMultiFramePayloads(payloads: PushComponentPayload[]): Promise<void> {
  let successCount = 0;
  let errorCount = 0;

  for (const payload of payloads) {
    try {
      await api.pushComponent(payload);
      persistTokens();
      successCount++;
      addActivity(
        "synced",
        `Synced frame "${payload.figmaFrameName}" — ${payload.textFields.length} text layers`,
        1
      );
    } catch (_) {
      errorCount++;
    }
  }

  if (successCount > 0) {
    showToast(`Synced ${successCount} frame(s)${errorCount > 0 ? `, ${errorCount} failed` : ''}`, "success");
  } else {
    showToast("Sync failed", "error");
  }

  syncInProgress = false;
  refreshChangesTab();
}

// ---------------------------------------------------------------
// ACTIVITY TAB
// ---------------------------------------------------------------

function addActivity(
  action: ActivityEntry["action"],
  description: string,
  count?: number
): void {
  const entry: ActivityEntry = {
    id: `act_${Date.now()}_${Math.random().toString(36).slice(2, 8)}`,
    action,
    description,
    timestamp: new Date().toISOString(),
    frameCount: action === "connected_frame" || action === "synced" ? count : undefined,
    textCount: action === "submitted_changes" || action === "edited_text" ? count : undefined,
  };

  activityLog.unshift(entry);
  // Keep last 50
  if (activityLog.length > 50) activityLog = activityLog.slice(0, 50);
  storage.setItem("intercopy_activity", JSON.stringify(activityLog));

  if (currentTab === "activity") {
    renderActivity();
  }
}

function renderActivity(): void {
  const list = $("activity-list");
  const empty = $("activity-empty");

  if (activityLog.length === 0) {
    list.style.display = "none";
    empty.classList.remove("hidden");
    empty.style.display = "";
    return;
  }

  empty.classList.add("hidden");
  empty.style.display = "none";
  list.style.display = "";

  list.innerHTML = "";
  for (const entry of activityLog) {
    const item = document.createElement("div");
    item.className = "activity-item";

    let dotClass = "";
    switch (entry.action) {
      case "synced":
      case "connected_frame":
        dotClass = "sync";
        break;
      case "edited_text":
        dotClass = "edit";
        break;
      default:
        dotClass = "";
    }

    item.innerHTML = `
      <div class="activity-dot ${dotClass}"></div>
      <div class="activity-body">
        <div class="activity-text">${esc(entry.description)}</div>
        <div class="activity-time">${formatTimestamp(entry.timestamp)}</div>
      </div>
    `;
    list.appendChild(item);
  }
}

// ---------------------------------------------------------------
// LIBRARY TAB
// ---------------------------------------------------------------

function renderLibraryTab(): void {
  // For now, show empty state. Library is populated from copy components.
  const list = $("library-list");
  const empty = $("library-empty");

  // TODO: Fetch copy components from API when endpoint is available
  list.style.display = "none";
  empty.classList.remove("hidden");
  empty.style.display = "";
}

// ---------------------------------------------------------------
// SETTINGS PANEL
// ---------------------------------------------------------------

function toggleSettings(show: boolean): void {
  const panel = $("settings-panel");
  if (show) {
    panel.classList.remove("hidden");
    updateUserDisplay();
    updateProjectDisplay();
  } else {
    panel.classList.add("hidden");
  }
}

function updateUserDisplay(): void {
  const name = userName || userEmail || "Unknown";
  $("settings-user-name").textContent = name;
  $("settings-user-email").textContent = userEmail || "—";
  $("settings-avatar").textContent = name.charAt(0).toUpperCase();
}

// ---------------------------------------------------------------
// UI HELPERS
// ---------------------------------------------------------------

function showLoginSection(): void {
  $("login-section").style.display = "";
  $("login-form-area").style.display = "flex";
  $("device-code-area").style.display = "none";
  $("main-section").classList.add("hidden");
  $("main-section").style.display = "none";
}

function showMainSection(): void {
  $("login-section").style.display = "none";
  $("main-section").classList.remove("hidden");
  $("main-section").style.display = "flex";
  updateUserDisplay();
  renderActivity();
  updateReviewBadge();
}

function showToast(message: string, type: "success" | "error" | "info"): void {
  const toast = $("toast");
  toast.textContent = message;
  toast.className = `${type} show`;
  setTimeout(() => {
    toast.classList.remove("show");
  }, 3000);
}

function formatTimestamp(iso: string): string {
  const date = new Date(iso);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMin = Math.floor(diffMs / 60000);
  const diffHr = Math.floor(diffMs / 3600000);
  const diffDays = Math.floor(diffMs / 86400000);

  if (diffMin < 1) return "Just now";
  if (diffMin < 60) return `${diffMin}m ago`;
  if (diffHr < 24) return `${diffHr}h ago`;
  if (diffDays === 0) return "Today";
  if (diffDays === 1) return "Yesterday";
  if (diffDays < 7) return `${diffDays}d ago`;
  return date.toLocaleDateString("en-US", { month: "short", day: "numeric" });
}

function esc(str: string): string {
  const div = document.createElement("div");
  div.textContent = str;
  return div.innerHTML;
}

function $<T extends HTMLElement = HTMLElement>(id: string): T {
  return document.getElementById(id) as T;
}

function postToMain(msg: UIMessage): void {
  parent.postMessage({ pluginMessage: msg }, "*");
}

function requestSelection(): void {
  postToMain({ type: "get-selection" });
}

function requestFileKey(): void {
  postToMain({ type: "get-file-key" });
}

