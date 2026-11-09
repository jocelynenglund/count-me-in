// Command slice: change the closing date. The POST, the field that sends it, and the wiring.
import { req } from "../lib/api.js";
import { esc } from "../lib/format.js";

export const send = (checkId, ownerKey, closesOn) => req("POST", `/checks/${encodeURIComponent(checkId)}/closing-date`, { ownerKey, body: { closesOn: closesOn || null } });

/** The closing date and how to change it; nothing once it can't be changed (read: closing-date). */
export function ClosingDateField(v) {
  if (!v.canChange) return `<div class="c" data-component="ClosingDateField"></div>`;
  return `<div class="field" data-component="ClosingDateField"><span>Closes</span><div class="dateline"><input class="in" type="date" id="closesOn" min="${esc(v.earliest)}" value="${esc(v.closesOn ?? "")}"><button class="look" type="button" id="saveDate">Save date</button></div>${v.closesOn ? `<button class="link" type="button" id="noDate">No end date</button>` : ""}</div>`;
}

export function wire(root, { checkId, ownerKey, onDone, onError }) {
  const run = async (date) => { try { await send(checkId, ownerKey, date); onDone(); } catch (err) { onError(err.message); } };
  root.querySelector("#saveDate")?.addEventListener("click", () => run(root.querySelector("#closesOn").value));
  root.querySelector("#noDate")?.addEventListener("click", () => run(null));
}
