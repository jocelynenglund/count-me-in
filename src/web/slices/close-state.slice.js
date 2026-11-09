// Owner only (the owner key goes along): close-state.
import { req } from "../lib/api.js";
import { record } from "../lib/loaded.js";

export const read = (checkId, ownerKey) =>
  req("GET", `/checks/${encodeURIComponent(checkId)}/views/close-state`, { ownerKey }).then((v) => record("closeState", v));

/** The close control while it is open (`slots.close`, already drawn), the Closed tag once it is not (view: closeState). */
export function CloseState(v, slots = {}) {
  if (v.status === "open") return `<div class="c" data-component="CloseState">${slots.close ?? ""}</div>`;
  return `<div class="tag" data-component="CloseState">Closed</div>`;
}
