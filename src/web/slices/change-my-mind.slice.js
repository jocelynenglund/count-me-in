// Command slice: change my mind (take me off). The POST, the button that sends it, and the wiring.
import { req } from "../lib/api.js";
import * as device from "../lib/device.js";

export const send = (checkId) => req("POST", `/checks/${encodeURIComponent(checkId)}/withdraw`, { body: { personId: device.personId() } });

export const TakeMeOffButton = () => `<button class="link" id="out" type="button" data-component="TakeMeOffButton">Changed your mind? Take me off</button>`;

export function wire(root, { checkId, onDone, onError }) {
  root.querySelector("#out")?.addEventListener("click", async () => {
    try { await send(checkId); onDone(); } catch (err) { onError(err.message); }
  });
}
