// Command slice: count me in. The POST, the form that sends it, and the wiring.
import { req } from "../lib/api.js";
import * as device from "../lib/device.js";
import { esc } from "../lib/format.js";

export async function send(checkId, { name, email }) {
  await req("POST", `/checks/${encodeURIComponent(checkId)}/interest`, { body: { personId: device.personId(), name, email: email || undefined } });
  device.rememberName(checkId, name);
}

/** The form to count in. `name` is the one this device remembers (passed in; no view). */
export function CountMeInForm({ name = "" } = {}) {
  return `<form id="in" class="stack" data-component="CountMeInForm" novalidate>
          <label class="field"><span>Your name</span><input class="in" name="name" maxlength="60" value="${esc(name)}" autocomplete="given-name" required></label>
          <label class="field"><span>Email <small>(optional, to hear if it's on)</small></span><input class="in" name="email" type="email" maxlength="200" autocomplete="email" placeholder="you@example.com"></label>
          <p class="err" hidden></p>
          <button class="btn" type="submit">Count me in</button>
        </form>`;
}

export function wire(root, { checkId, onDone, onError }) {
  root.querySelector("#in")?.addEventListener("submit", async (e) => {
    e.preventDefault();
    const f = new FormData(e.target);
    try { await send(checkId, { name: String(f.get("name") ?? "").trim(), email: String(f.get("email") ?? "").trim() }); onDone(); }
    catch (err) { onError(err.message); }
  });
}
