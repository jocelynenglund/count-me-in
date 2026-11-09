// Command slice: close the interest check. The POST, the button that sends it, and the wiring.
import { req } from "../lib/api.js";

export const send = (checkId, ownerKey) => req("POST", `/checks/${encodeURIComponent(checkId)}/close`, { ownerKey });

export const CloseButton = () => `<button class="ghost danger" id="close" type="button" data-component="CloseButton">Close it</button>`;

export function wire(root, { checkId, ownerKey, onDone, onError }) {
  root.querySelector("#close")?.addEventListener("click", async () => {
    if (!confirm("Close it? Nobody can count themselves in after this.")) return;
    try { await send(checkId, ownerKey); onDone(); } catch (err) { onError(err.message); }
  });
}
