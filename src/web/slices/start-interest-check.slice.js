// Command slice: start an interest check. The POST, the form that sends it, the look picker on it, and the wiring.
import { req, newId } from "../lib/api.js";
import * as device from "../lib/device.js";
import { esc, lookButtons } from "../lib/format.js";

/** Start a check; the look picked on the form is applied right after (the owner page can change it again). */
export async function send({ title, pitch, goal, closesOn, theme }) {
  const checkId = newId();
  const { ownerKey } = await req("POST", "/checks", { body: { checkId, title, pitch, goal, closesOn } });
  device.rememberOwnerKey(checkId, ownerKey);
  if (theme && theme !== "classic") await req("POST", `/checks/${encodeURIComponent(checkId)}/theme`, { ownerKey, body: { theme } }).catch(() => {});
  return { checkId, ownerKey };
}

/**
 * The form for a new interest check (read: start-form). `slots` are the look picker and the
 * list of this device's checks, already drawn, which sit inside the form.
 */
export function StartForm(v, slots = {}) {
  return `<form class="body" id="start" data-component="StartForm" novalidate>
      <h1 class="big">Got an idea? Count heads first.</h1>
      <label class="field"><span>The idea</span><input class="in" name="title" maxlength="120" placeholder="Saturday board-game club" required></label>
      <label class="field"><span>The pitch</span><textarea class="in" name="pitch" maxlength="1000" rows="3" placeholder="Where, when, and what happens if enough people are in."></textarea></label>
      <div class="row">
        <label class="field"><span>Goal</span><input class="in" name="goal" type="number" min="1" max="100000" inputmode="numeric" value="${esc(v.defaultGoal)}" required></label>
        <label class="field"><span>Closes <small>(optional)</small></span><input class="in" name="closesOn" type="date" min="${esc(v.earliestClosingDate)}"></label>
      </div>
      ${slots.looks ?? ""}
      <p class="err" hidden></p>
      <div class="grow"></div>
      <button class="btn" type="submit">Start counting</button>
      ${slots.mine ?? ""}
    </form>`;
}

/** One button per look on the start page (read: look-choices). Picking one only previews it; it is sent after the check starts. */
export function StartLookPicker(v) {
  return `<div class="field" data-component="StartLookPicker"><span>The look</span><div class="looks" role="group" aria-label="The look">${lookButtons(v.choices)}</div></div>`;
}

/** Preview a look on the form, send the form, then onDone({ checkId, ownerKey }) or onError(message). */
export function wire(root, { looks, onDone, onError }) {
  let theme = looks.choices.find((c) => c.picked)?.theme;
  const buttons = root.querySelectorAll(".look");
  buttons.forEach((b) => b.addEventListener("click", () => {
    theme = b.dataset.theme;
    document.body.dataset.theme = theme; // preview it on the form
    buttons.forEach((o) => o.setAttribute("aria-pressed", String(o === b)));
  }));
  root.querySelector("#start")?.addEventListener("submit", async (e) => {
    e.preventDefault();
    const f = new FormData(e.target);
    try { onDone(await send({ title: f.get("title"), pitch: f.get("pitch"), goal: Number(f.get("goal")), closesOn: f.get("closesOn") || undefined, theme })); }
    catch (err) { onError(err.message); }
  });
}
