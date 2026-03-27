import type {
  PushComponentPayload,
  PushTextField,
  MainMessage,
  UIMessage,
  DesignComponentTextField,
} from "./types";

// ---------------------------------------------------------------
// Main plugin thread – runs in the Figma sandbox
// Has access to the Figma Plugin API (figma.*)
// ---------------------------------------------------------------

figma.showUI(__html__, { width: 360, height: 520, themeColors: true });

// Notify UI of the current selection on startup and whenever it changes
figma.on("selectionchange", () => sendSelectionInfo());
sendSelectionInfo();

// ---------------------------------------------------------------
// Message handler – messages arrive from the UI iframe
// ---------------------------------------------------------------

figma.ui.onmessage = (msg: UIMessage) => {
  switch (msg.type) {
    case "push-frame":
      handlePushFrame(msg.projectId);
      break;
    case "pull-component":
      handlePullComponent(msg.componentId);
      break;
    default:
      break;
  }
};

// ---------------------------------------------------------------
// Push: extract text layers from the selected frame
// ---------------------------------------------------------------

function handlePushFrame(projectId: string): void {
  const selection = figma.currentPage.selection;

  if (selection.length !== 1) {
    sendError("Please select exactly one frame.");
    return;
  }

  const node = selection[0];
  if (node.type !== "FRAME" && node.type !== "COMPONENT" && node.type !== "INSTANCE") {
    sendError("Selected node must be a Frame, Component, or Instance.");
    return;
  }

  const frame = node as FrameNode;
  const textFields = extractTextFields(frame);

  const payload: PushComponentPayload = {
    figmaFileId: figma.fileKey ?? "",
    figmaFrameId: frame.id,
    figmaFrameName: frame.name,
    thumbnailUrl: "",
    frameWidth: Math.round(frame.width),
    frameHeight: Math.round(frame.height),
    projectId,
    textFields,
  };

  const message: MainMessage = { type: "frame-data", payload };
  figma.ui.postMessage(message);
}

// ---------------------------------------------------------------
// Pull: update text layers with data from the backend
// ---------------------------------------------------------------

function handlePullComponent(_componentId: string): void {
  // The UI will call the API and then send us the text fields to apply.
  // We listen for a follow-up "pull-text" message from the UI.
  // This function is a no-op; actual application happens below.
}

// Handle text application from UI after API call completes
figma.ui.onmessage = (msg: UIMessage | { type: "apply-pull"; textFields: DesignComponentTextField[] }) => {
  if (msg.type === "push-frame") {
    handlePushFrame((msg as UIMessage & { type: "push-frame" }).projectId);
    return;
  }

  if (msg.type === "pull-component") {
    // UI will handle the API call and send back apply-pull
    return;
  }

  if (msg.type === "apply-pull") {
    applyPulledText(msg.textFields);
    return;
  }
};

function applyPulledText(textFields: DesignComponentTextField[]): void {
  const selection = figma.currentPage.selection;
  if (selection.length !== 1) {
    sendError("Please select the frame to update.");
    return;
  }

  const frame = selection[0] as FrameNode;
  let updatedCount = 0;

  for (const field of textFields) {
    const matchingNode = findTextNodeByName(frame, field.figmaLayerName);
    if (matchingNode) {
      // Load the font before changing characters
      const fontName = matchingNode.fontName as FontName;
      figma
        .loadFontAsync(fontName)
        .then(() => {
          matchingNode.characters = field.currentText;
          updatedCount++;
          if (updatedCount === textFields.length) {
            figma.notify(`Updated ${updatedCount} text layer(s).`);
            const message: MainMessage = {
              type: "notify",
              message: `Updated ${updatedCount} text layer(s).`,
            };
            figma.ui.postMessage(message);
          }
        })
        .catch((err) => {
          sendError(`Font load error for "${field.figmaLayerName}": ${err}`);
        });
    }
  }

  if (textFields.length === 0) {
    figma.notify("No text fields to update.");
  }
}

// ---------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------

/** Recursively walk children of `parent` and collect TextNode data. */
function extractTextFields(parent: FrameNode): PushTextField[] {
  const fields: PushTextField[] = [];
  walkNodes(parent, parent, fields);
  return fields;
}

function walkNodes(
  node: SceneNode,
  frameRoot: FrameNode,
  fields: PushTextField[]
): void {
  if (node.type === "TEXT") {
    const textNode = node as TextNode;

    // Compute position relative to the frame root
    const relX = textNode.absoluteTransform[0][2] - frameRoot.absoluteTransform[0][2];
    const relY = textNode.absoluteTransform[1][2] - frameRoot.absoluteTransform[1][2];

    // Extract font info (use first character's style if mixed)
    let fontFamily = "";
    let fontSize = 0;
    let fontWeight = "";
    let color = "#000000";

    const fontName = textNode.fontName;
    if (fontName !== figma.mixed) {
      fontFamily = fontName.family;
      fontWeight = fontName.style;
    }

    const fs = textNode.fontSize;
    if (fs !== figma.mixed) {
      fontSize = fs;
    }

    // Extract fill color (first solid paint)
    const fills = textNode.fills;
    if (Array.isArray(fills) && fills.length > 0) {
      const firstFill = fills[0];
      if (firstFill.type === "SOLID") {
        const { r, g, b } = firstFill.color;
        color = rgbToHex(r, g, b);
      }
    }

    fields.push({
      figmaLayerId: textNode.id,
      figmaLayerName: textNode.name,
      currentText: textNode.characters,
      x: Math.round(relX),
      y: Math.round(relY),
      width: Math.round(textNode.width),
      height: Math.round(textNode.height),
      fontFamily,
      fontSize,
      fontWeight,
      textAlign: getTextAlign(textNode),
      color,
    });
  }

  if ("children" in node) {
    for (const child of (node as ChildrenMixin).children) {
      walkNodes(child, frameRoot, fields);
    }
  }
}

/** Find a TextNode by name within a subtree. */
function findTextNodeByName(parent: SceneNode, name: string): TextNode | null {
  if (parent.type === "TEXT" && parent.name === name) {
    return parent as TextNode;
  }
  if ("children" in parent) {
    for (const child of (parent as ChildrenMixin).children) {
      const found = findTextNodeByName(child, name);
      if (found) return found;
    }
  }
  return null;
}

function getTextAlign(node: TextNode): string {
  const align = node.textAlignHorizontal;
  switch (align) {
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

function rgbToHex(r: number, g: number, b: number): string {
  const toHex = (v: number) =>
    Math.round(v * 255)
      .toString(16)
      .padStart(2, "0");
  return `#${toHex(r)}${toHex(g)}${toHex(b)}`;
}

function sendSelectionInfo(): void {
  const selection = figma.currentPage.selection;
  const hasFrame =
    selection.length === 1 &&
    (selection[0].type === "FRAME" ||
      selection[0].type === "COMPONENT" ||
      selection[0].type === "INSTANCE");

  const message: MainMessage = {
    type: "selection-info",
    hasFrame,
    frameName: hasFrame ? selection[0].name : "",
  };
  figma.ui.postMessage(message);
}

function sendError(message: string): void {
  const msg: MainMessage = { type: "error", message };
  figma.ui.postMessage(msg);
}
