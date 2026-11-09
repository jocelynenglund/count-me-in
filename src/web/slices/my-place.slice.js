// Where this device stands on a check: can join, already in, or it's closed. Adds the name to prefill the form.
import { req } from "../lib/api.js";
import { record } from "../lib/loaded.js";
import { personId, nameFor } from "../lib/device.js";
import { esc } from "../lib/format.js";

export const read = (checkId) =>
  req("GET", `/checks/${encodeURIComponent(checkId)}/views/my-place`, { personId: personId() })
    .then((v) => record("myPlace", { ...v, suggestedName: nameFor(checkId) }));

/**
 * This device's place (view: myPlace): the form to count in when it can-join, "You're in" with a way out
 * when in, nothing when closed. `slots` are the controls, already drawn: join (CountMeInForm), takeMeOff (TakeMeOffButton).
 */
export function MyPlace(v, slots = {}) {
  if (v.status === "can-join") return `<div class="c" data-component="MyPlace">${slots.join ?? ""}</div>`;
  if (v.status === "in") {
    return `<div class="c" data-component="MyPlace"><div class="you"><div class="mid">You're in${v.name ? ", " + esc(v.name) : ""}</div></div>
        <p class="err" hidden></p>
        ${slots.takeMeOff ?? ""}</div>`;
  }
  return `<div class="c" data-component="MyPlace"></div>`;
}
