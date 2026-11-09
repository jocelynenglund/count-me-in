// The page shell the screens draw into: the one place that puts HTML on the page.
import { Missing } from "./page.parts.js";

const app = () => document.getElementById("app");

/** Draw a screen. `screen` and `theme` are the body hooks poster.css styles by. */
export function show(screen, html, theme = "classic") {
  document.body.dataset.screen = screen;
  document.body.dataset.theme = theme;
  app().innerHTML = html;
}

export const showMissing = (err) => show("closed", Missing(err));

/** The API's refusal, exactly as it came. */
export function showError(msg) {
  const el = app().querySelector(".err");
  if (el) { el.textContent = msg; el.hidden = !msg; }
}

/** Buttons that copy a link (data-copy). */
export function wireCopyButtons() {
  app().querySelectorAll("[data-copy]").forEach((b) => b.addEventListener("click", async () => {
    try { await navigator.clipboard.writeText(b.dataset.copy); b.textContent = "Copied"; } catch { prompt("Copy this link", b.dataset.copy); }
  }));
}

export const $ = (sel) => app().querySelector(sel);
export const $$ = (sel) => app().querySelectorAll(sel);
