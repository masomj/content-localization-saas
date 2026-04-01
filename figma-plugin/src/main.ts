import type {
  TextNodeInfo,
  FrameInfo,
  PushComponentPayload,
  PushTextField,
  MainMessage,
  UIMessage,
  DesignComponentTextField,
} from "./types";

// ---------------------------------------------------------------
// InterCopy Figma Plugin — Main thread (sandbox)
// Runs in the Figma plugin sandbox with access to figma.* API.
// ---------------------------------------------------------------

figma.showUI(__html__, { width: 380, height: 560, themeColors: true });

// ---------------------------------------------------------------
// Selection change tracking
// ---------------------------------------------------------------

figma.on("selectionchange", () => {
  sendSelectionData();
});

// Send initial state after a brief delay for UI to initialize
setTimeout(() => {
  sendStoredValues();
  sendSelectionData();
  sendCurrentUser();
}, 200);

// ---------------------------------------------------------------
// Client storage bridge — Figma clientStorage <-> UI iframe
// ---------------------------------------------------------------

const STORAGE_KEYS = [
  "intercopy_access_token",
  "intercopy_refresh_token",
  "intercopy_url",
  "intercopy_project",
  "intercopy_autosync",
  "intercopy_notifications",
  "intercopy_activity",
  "intercopy_review",
];

async function sendStoredValues(): Promise<void> {
  const entries: Record<string, string | null> = {};
  for (const key of STORAGE_KEYS) {
    const val = await figma.clientStorage.getAsync(key);
    entries[key] = val ?? null;
  }
  postToUI({ type: "storage-data", entries } as any);
}

// ---------------------------------------------------------------
// Message handler from UI iframe
// ---------------------------------------------------------------

figma.ui.onmessage = async (msg: UIMessage) => {
  switch (msg.type) {
    case "get-selection":
      sendSelectionData();
      break;

    case "scan-all-frames":
      handleScanAllFrames();
      break;

    case "push-frame":
      handlePushFrame(msg.projectId);
      break;

    case "push-frames":
      handlePushFrames(msg.projectId, msg.frameIds);
      break;

    case "update-text":
      await handleUpdateText(msg.layerId, msg.newText);
      break;

    case "apply-pull":
      await applyPulledText(msg.textFields);
      break;

    case "get-file-key":
      sendFileKey();
      break;

    case "resize":
      figma.ui.resize(msg.width, msg.height);
      break;

    case "storage-set": {
      const sm = msg as UIMessage & { type: "storage-set" };
      await figma.clientStorage.setAsync(sm.key, sm.value);
      break;
    }

    case "storage-remove": {
      const rm = msg as UIMessage & { type: "storage-remove" };
      await figma.clientStorage.deleteAsync(rm.key);
      break;
    }

    case "storage-request":
      await sendStoredValues();
      break;
  }
};

// ---------------------------------------------------------------
// Selection data extraction
// ---------------------------------------------------------------

function sendSelectionData(): void {
  const selection = figma.currentPage.selection;

  if (selection.length === 0) {
    postToUI({ type: "selection-empty" });
    return;
  }

  const frames: FrameInfo[] = [];

  for (const node of selection) {
    if (isFrameLike(node)) {
      frames.push(extractFrameInfo(node as SceneNode & ChildrenMixin));
    } else {
      // Walk up to find the parent frame
      const parentFrame = findParentFrame(node);
      if (parentFrame && !frames.some((f) => f.frameId === parentFrame.id)) {
        frames.push(extractFrameInfo(parentFrame));
      }
    }
  }

  if (frames.length === 0) {
    postToUI({ type: "selection-empty" });
  } else {
    postToUI({ type: "selection-changed", frames });
  }
}

function extractFrameInfo(frame: SceneNode & ChildrenMixin): FrameInfo {
  const textNodes: TextNodeInfo[] = [];
  walkTextNodes(frame, frame, textNodes);

  return {
    frameId: frame.id,
    frameName: frame.name,
    frameWidth: "width" in frame ? (frame as FrameNode).width : 0,
    frameHeight: "height" in frame ? (frame as FrameNode).height : 0,
    textNodes,
  };
}

function walkTextNodes(
  node: SceneNode,
  frameRoot: SceneNode,
  results: TextNodeInfo[]
): void {
  if (node.type === "TEXT") {
    const textNode = node as TextNode;
    const rootX = "absoluteTransform" in frameRoot
      ? (frameRoot as FrameNode).absoluteTransform[0][2]
      : 0;
    const rootY = "absoluteTransform" in frameRoot
      ? (frameRoot as FrameNode).absoluteTransform[1][2]
      : 0;
    const nodeX = textNode.absoluteTransform[0][2];
    const nodeY = textNode.absoluteTransform[1][2];

    results.push({
      layerId: textNode.id,
      layerName: textNode.name,
      characters: textNode.characters,
      x: Math.round(nodeX - rootX),
      y: Math.round(nodeY - rootY),
      width: Math.round(textNode.width),
      height: Math.round(textNode.height),
      fontFamily: getFont(textNode),
      fontSize: getFontSize(textNode),
      fontWeight: getFontWeight(textNode),
      textAlign: getTextAlign(textNode),
      color: getColor(textNode),
    });
  }

  if ("children" in node) {
    for (const child of (node as ChildrenMixin).children) {
      walkTextNodes(child, frameRoot, results);
    }
  }
}

// ---------------------------------------------------------------
// Scan all top-level frames on the current page
// ---------------------------------------------------------------

function handleScanAllFrames(): void {
  const frames: FrameInfo[] = [];

  for (const node of figma.currentPage.children) {
    if (isFrameLike(node)) {
      frames.push(extractFrameInfo(node as SceneNode & ChildrenMixin));
    }
  }

  postToUI({ type: "all-frames", frames });
}

// ---------------------------------------------------------------
// Push single frame (sends data back to UI for API call)
// ---------------------------------------------------------------

async function handlePushFrame(projectId: string): Promise<void> {
  const selection = figma.currentPage.selection;
  if (selection.length === 0) {
    return postToUI({ type: "error", message: "No frame selected." });
  }

  const frame = selection[0];
  if (!isFrameLike(frame)) {
    return postToUI({
      type: "error",
      message: "Please select a frame, component, or instance.",
    });
  }

  const payload = await buildPushPayload(
    frame as SceneNode & ChildrenMixin,
    projectId
  );
  postToUI({ type: "frame-data", payload });
}

// ---------------------------------------------------------------
// Push multiple specific frames (for sync from Changes tab)
// ---------------------------------------------------------------

function handlePushFrames(projectId: string, frameIds: string[]): void {
  const frames: FrameInfo[] = [];

  for (const frameId of frameIds) {
    const node = figma.getNodeById(frameId);
    if (node && isFrameLike(node)) {
      frames.push(
        extractFrameInfo(node as SceneNode & ChildrenMixin)
      );
    }
  }

  postToUI({ type: "multi-frame-data", frames });
}

// ---------------------------------------------------------------
// Build push payload from a frame node
// ---------------------------------------------------------------

async function buildPushPayload(
  frame: SceneNode & ChildrenMixin,
  projectId: string
): Promise<PushComponentPayload> {
  const textNodes: TextNodeInfo[] = [];
  walkTextNodes(frame, frame, textNodes);

  const textFields: PushTextField[] = textNodes.map((tn) => ({
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
  }));

  const fileKey = getFileKey();

  // Export frame as PNG for thumbnail
  let thumbnailUrl = "";
  try {
    const exportSettings: ExportSettings = { format: "PNG", constraint: { type: "SCALE", value: 2 } };
    const imageBytes = await (frame as SceneNode).exportAsync(exportSettings);
    const base64 = figma.base64Encode(imageBytes);
    thumbnailUrl = "data:image/png;base64," + base64;
  } catch (_) {
    // Export may fail for some node types — continue without thumbnail
  }

  return {
    figmaFileId: fileKey,
    figmaFrameId: frame.id,
    figmaFrameName: frame.name,
    thumbnailUrl,
    frameWidth: "width" in frame ? (frame as FrameNode).width : 0,
    frameHeight: "height" in frame ? (frame as FrameNode).height : 0,
    projectId,
    textFields,
  };
}

// ---------------------------------------------------------------
// Update a single text node from UI edit
// ---------------------------------------------------------------

async function handleUpdateText(
  layerId: string,
  newText: string
): Promise<void> {
  const node = figma.getNodeById(layerId);
  if (!node || node.type !== "TEXT") {
    return postToUI({
      type: "error",
      message: `Text layer not found.`,
    });
  }

  const textNode = node as TextNode;

  try {
    await loadAllFonts(textNode);
    textNode.characters = newText;
    postToUI({ type: "text-updated", layerId, newText });
  } catch (err) {
    postToUI({
      type: "error",
      message: `Failed to update text: ${err}`,
    });
  }
}

// ---------------------------------------------------------------
// Apply pulled text from API to Figma layers
// ---------------------------------------------------------------

async function applyPulledText(
  textFields: DesignComponentTextField[]
): Promise<void> {
  let updated = 0;

  for (const field of textFields) {
    // Try by ID first
    let targetNode: TextNode | null = null;
    const node = figma.getNodeById(field.figmaLayerId);
    if (node && node.type === "TEXT") {
      targetNode = node as TextNode;
    } else {
      // Fallback: search by name
      targetNode = findTextNodeByName(
        figma.currentPage,
        field.figmaLayerName
      );
    }

    if (!targetNode) continue;

    try {
      await loadAllFonts(targetNode);
      targetNode.characters = field.currentText;
      updated++;
    } catch (_) {
      // Skip nodes that fail font loading
    }
  }

  postToUI({
    type: "notify",
    message: `Updated ${updated} text layer${updated !== 1 ? "s" : ""}.`,
  });
}

// ---------------------------------------------------------------
// File key extraction
// ---------------------------------------------------------------

function sendFileKey(): void {
  postToUI({ type: "file-key", fileKey: getFileKey() });
}

function getFileKey(): string {
  try {
    return figma.fileKey ?? "unknown";
  } catch (_) {
    return "unknown";
  }
}

// ---------------------------------------------------------------
// Current user info
// ---------------------------------------------------------------

function sendCurrentUser(): void {
  try {
    const user = figma.currentUser;
    if (user) {
      postToUI({
        type: "current-user",
        name: user.name || "Unknown",
        email: "",
      });
    }
  } catch (_) {
    // Ignore if not available
  }
}

// ---------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------

function isFrameLike(node: BaseNode): boolean {
  return (
    node.type === "FRAME" ||
    node.type === "COMPONENT" ||
    node.type === "INSTANCE" ||
    node.type === "COMPONENT_SET"
  );
}

function findParentFrame(
  node: BaseNode
): (SceneNode & ChildrenMixin) | null {
  let current = node.parent;
  while (current) {
    if (
      current.type === "FRAME" ||
      current.type === "COMPONENT" ||
      current.type === "INSTANCE"
    ) {
      return current as SceneNode & ChildrenMixin;
    }
    current = current.parent;
  }
  return null;
}

function findTextNodeByName(
  parent: BaseNode & ChildrenMixin,
  name: string
): TextNode | null {
  for (const child of parent.children) {
    if (child.type === "TEXT" && child.name === name) {
      return child as TextNode;
    }
    if ("children" in child) {
      const found = findTextNodeByName(
        child as BaseNode & ChildrenMixin,
        name
      );
      if (found) return found;
    }
  }
  return null;
}

async function loadAllFonts(textNode: TextNode): Promise<void> {
  const len = textNode.characters.length;
  if (len === 0) {
    // For empty text nodes, load the default font
    const font = textNode.fontName;
    if (font !== figma.mixed) {
      await figma.loadFontAsync(font as FontName);
    }
    return;
  }
  const fonts = textNode.getRangeAllFontNames(0, len);
  for (const font of fonts) {
    await figma.loadFontAsync(font);
  }
}

function getFont(node: TextNode): string {
  const family = node.fontName;
  if (family === figma.mixed) return "Mixed";
  return (family as FontName).family || "Unknown";
}

function getFontSize(node: TextNode): number {
  const size = node.fontSize;
  if (size === figma.mixed) return 0;
  return size as number;
}

function getFontWeight(node: TextNode): string {
  const font = node.fontName;
  if (font === figma.mixed) return "Mixed";
  return (font as FontName).style || "Regular";
}

function getTextAlign(node: TextNode): string {
  switch (node.textAlignHorizontal) {
    case "LEFT":
      return "left";
    case "CENTER":
      return "center";
    case "RIGHT":
      return "right";
    case "JUSTIFIED":
      return "justify";
    default:
      return "left";
  }
}

function getColor(node: TextNode): string {
  const fills = node.fills;
  if (!Array.isArray(fills) || fills.length === 0) return "#000000";
  const fill = fills[0];
  if (fill.type !== "SOLID") return "#000000";
  return rgbToHex(fill.color.r, fill.color.g, fill.color.b);
}

function rgbToHex(r: number, g: number, b: number): string {
  const toHex = (v: number) =>
    Math.round(v * 255)
      .toString(16)
      .padStart(2, "0");
  return `#${toHex(r)}${toHex(g)}${toHex(b)}`;
}

function postToUI(msg: MainMessage): void {
  figma.ui.postMessage(msg);
}

