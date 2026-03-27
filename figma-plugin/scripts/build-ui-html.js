/**
 * Build script: inlines the bundled ui.js into ui.html and writes dist/ui.html.
 * Figma plugins require a single HTML file with all JS inlined.
 */
const fs = require("fs");
const path = require("path");

const srcHtml = path.join(__dirname, "..", "src", "ui.html");
const bundledJs = path.join(__dirname, "..", "dist", "ui.js");
const outHtml = path.join(__dirname, "..", "dist", "ui.html");

// Ensure dist/ exists
const distDir = path.join(__dirname, "..", "dist");
if (!fs.existsSync(distDir)) {
  fs.mkdirSync(distDir, { recursive: true });
}

let html = fs.readFileSync(srcHtml, "utf8");
let js = "";

if (fs.existsSync(bundledJs)) {
  js = fs.readFileSync(bundledJs, "utf8");
}

// Replace the placeholder script tag with the inlined bundle
html = html.replace(
  /<script>\s*\/\/ Injected at build time[^<]*<\/script>/,
  `<script>\n${js}\n</script>`
);

fs.writeFileSync(outHtml, html, "utf8");
console.log("Built dist/ui.html with inlined JS");
