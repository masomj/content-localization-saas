import { LocFlowApi } from "./api";
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
// LocFlow Plugin UI — Full multi-tab Frontitude-style interface
// ---------------------------------------------------------------

const DEFAULT_BASE_URL = "https://app.locflow.io";

// ---------------------------------------------------------------
// State
// ---------------------------------------------------------------

let api: LocFlowApi;
let projects: Project[] = [];
let selectedProjectId = "";
let currentTab: TabName = "activity";
let fileKey = "unknown";

// User info
let userName = "";
let userEmail = "";

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
let expandedChangeFrames: Set<string> = new Set();

// Activity tab
let activityLog: ActivityEntry[] = [];

// Settings
let autoSync = false;
let showNotifications = true;

// ---------------------------------------------------------------
// DOM init
// ---------------------------------------------------------------

document.addEventListener("DOMContentLoaded", () => {
  const savedToken = localStorage.getItem("locflow_token");
  const savedUrl = localStorage.getItem("locflow_url") || DEFAULT_BASE_URL;
  const savedEmail = localStorage.getItem("locflow_email") || "";
  const savedProject = localStorage.getItem("locflow_project") || "";
  const savedActivity = localStorage.getItem("locflow_activity");
  const savedReview = localStorage.getItem("locflow_review");

  // Restore activity and review from localStorage
  if (savedActivity) {
    try { activityLog = JSON.parse(savedActivity); } catch { activityLog = []; }
  }
  if (savedReview) {
    try { reviewQueue = JSON.parse(savedReview); } catch { reviewQueue = []; }
  }

  const serverUrlInput = $<HTMLInputElement>("server-url");
  serverUrlInput.value = savedUrl;
  api = new LocFlowApi(savedUrl);

  if (savedToken) {
    api.setToken(savedToken);
    userEmail = savedEmail;
    selectedProjectId = savedProject;
    showMainSection();
    loadProjects();
    requestFileKey();
    requestSelection();
  }

  // Load settings
  autoSync = localStorage.getItem("locflow_autosync") === "true";
  showNotifications = localStorage.getItem("locflow_notifications") !== "false";

  bindEvents();
});

// ---------------------------------------------------------------
// Event bindings
// ---------------------------------------------------------------

function bindEvents(): void {
  // Login
  $("login-btn").addEventListener("click", handleLogin);
  $<HTMLInputElement>("workspace-id").addEventListener("keydown", (e) => {
    if (e.key === "Enter") handleLogin();
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
    localStorage.setItem("locflow_autosync", String(autoSync));
  });
  $("toggle-notifications").addEventListener("click", () => {
    showNotifications = !showNotifications;
    $("toggle-notifications").classList.toggle("on", showNotifications);
    localStorage.setItem("locflow_notifications", String(showNotifications));
  });

  // Project select
  $<HTMLSelectElement>("project-select").addEventListener("change", (e) => {
    selectedProjectId = (e.target as HTMLSelectElement).value;
    localStorage.setItem("locflow_project", selectedProjectId);
    updateProjectDisplay();
    refreshChangesTab();
  });

  // Settings open in locflow
  $("settings-open-locflow").addEventListener("click", () => {
    const url = localStorage.getItem("locflow_url") || DEFAULT_BASE_URL;
    window.open(`${url}/projects/${selectedProjectId}`, "_blank");
  });

  // Edit tab buttons
  $("edit-review-btn").addEventListener("click", handleAddToReview);
  $("edit-tags-btn").addEventListener("click", () => showToast("Tags feature coming soon", "info"));

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
// AUTH
// ---------------------------------------------------------------

async function handleLogin(): Promise<void> {
  const email = $<HTMLInputElement>("email").value.trim();
  const workspaceId = $<HTMLInputElement>("workspace-id").value.trim();
  const serverUrl = $<HTMLInputElement>("server-url").value.trim() || DEFAULT_BASE_URL;

  if (!email || !workspaceId) {
    $("login-error").textContent = "Email and workspace ID are required.";
    return;
  }

  $("login-error").textContent = "";
  const btn = $<HTMLButtonElement>("login-btn");
  btn.disabled = true;
  btn.textContent = "Logging in...";

  try {
    api = new LocFlowApi(serverUrl);
    const res = await api.login(email, workspaceId);

    localStorage.setItem("locflow_token", res.token);
    localStorage.setItem("locflow_url", serverUrl);
    localStorage.setItem("locflow_email", res.email || email);
    userEmail = res.email || email;
    userName = res.displayName || email;

    showMainSection();
    await loadProjects();
    requestFileKey();
    requestSelection();
  } catch (err: unknown) {
    $("login-error").textContent = err instanceof Error ? err.message : "Login failed";
  } finally {
    btn.disabled = false;
    btn.textContent = "Log in";
  }
}

function handleLogout(): void {
  api.clearToken();
  localStorage.removeItem("locflow_token");
  localStorage.removeItem("locflow_email");
  localStorage.removeItem("locflow_project");
  projects = [];
  selectedProjectId = "";
  activityLog = [];
  reviewQueue = [];
  changeEntries = [];
  toggleSettings(false);
  showLoginSection();
}

// ---------------------------------------------------------------
// PROJECTS
// ---------------------------------------------------------------

async function loadProjects(): Promise<void> {
  try {
    projects = await api.getProjects();
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
      localStorage.setItem("locflow_project", selectedProjectId);
    }

    updateProjectDisplay();
  } catch (err: unknown) {
    if (err instanceof Error && err.message === "SESSION_EXPIRED") {
      handleLogout();
      $("login-error").textContent = "Session expired. Please log in again.";
    } else {
      showToast(err instanceof Error ? err.message : "Failed to load projects", "error");
    }
  }
}

function updateProjectDisplay(): void {
  const project = projects.find((p) => p.id === selectedProjectId);
  $("project-name-display").textContent = project?.name || "LocFlow";
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

  localStorage.setItem("locflow_review", JSON.stringify(reviewQueue));

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
  } catch {
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
  renderChangesTab();

  let syncedCount = 0;
  let errorCount = 0;
  const totalFrames = changeEntries.length;

  for (const entry of changeEntries) {
    $("sync-progress-text").textContent = `Syncing ${syncedCount + 1} of ${totalFrames} frames...`;

    // Find the local frame data
    const localFrame = allLocalFrames.find((f) => f.frameId === entry.frameId);
    if (!localFrame) {
      errorCount++;
      continue;
    }

    // Build push payload from local frame data
    const payload: PushComponentPayload = {
      figmaFileId: fileKey,
      figmaFrameId: localFrame.frameId,
      figmaFrameName: localFrame.frameName,
      thumbnailUrl: "",
      frameWidth: localFrame.frameWidth,
      frameHeight: localFrame.frameHeight,
      projectId: selectedProjectId,
      textFields: localFrame.textNodes.map((tn) => ({
        figmaLayerId: tn.layerId,
        figmaLayerName: tn.layerName,
        currentText: tn.characters,
        x: tn.x,
        y: tn.y,
        width: tn.width,
        height: tn.height,
        fontFamily: tn.fontFamily,
        fontSize: tn.fontSize,
        fontWeight: tn.fontWeight,
        textAlign: tn.textAlign,
        color: tn.color,
      })),
    };

    try {
      await api.pushComponent(payload);
      syncedCount++;
    } catch (err) {
      errorCount++;
      console.error("Sync error for frame", entry.frameName, err);
    }
  }

  isSyncing = false;

  // Log activity
  if (syncedCount > 0) {
    addActivity(
      "synced",
      `You synced ${syncedCount} frame${syncedCount !== 1 ? "s" : ""} to LocFlow`,
      syncedCount
    );
    $("settings-frame-count").textContent = String(
      remoteComponents.length + syncedCount
    );
  }

  if (errorCount > 0) {
    showToast(`Synced ${syncedCount} frames, ${errorCount} failed`, "error");
  } else {
    showToast(`Synced ${syncedCount} frame${syncedCount !== 1 ? "s" : ""} successfully`, "success");
  }

  // Refresh to show updated state
  await refreshChangesTab();
}

// ---------------------------------------------------------------
// Handle frame data from main thread (push flow)
// ---------------------------------------------------------------

async function handleFrameData(payload: PushComponentPayload): Promise<void> {
  try {
    const component = await api.pushComponent(payload);
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
  // This is called when main thread returns data for specific frame IDs
  // used in the sync flow — frames are already being pushed in handleSync
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
  localStorage.setItem("locflow_activity", JSON.stringify(activityLog));

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
