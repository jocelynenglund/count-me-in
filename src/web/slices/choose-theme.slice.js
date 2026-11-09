// Command slice: choose the look. The POST, the picker that sends it, and the wiring.
import { req } from "../lib/api.js";
import { lookButtons } from "../lib/format.js";

export const send = (checkId, ownerKey, theme) => req("POST", `/checks/${encodeURIComponent(checkId)}/theme`, { ownerKey, body: { theme } });

/** One button per look; the pressed one is the pitch's look (read: check-look-choices). */
export function LookPicker(v) {
  return `<div class="field" data-component="LookPicker"><span>The look</span><div class="looks" role="group" aria-label="The look">${lookButtons(v.choices)}</div></div>`;
}

export function wire(root, { checkId, ownerKey, onDone, onError }) {
  root.querySelectorAll(".look[data-theme]").forEach((b) => b.addEventListener("click", async () => {
    if (b.getAttribute("aria-pressed") === "true") return;
    try { await send(checkId, ownerKey, b.dataset.theme); onDone(); } catch (err) { onError(err.message); }
  }));
}
