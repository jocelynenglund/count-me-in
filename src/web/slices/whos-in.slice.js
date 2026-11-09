// Owner only (the owner key goes along): whos-in.
import { req } from "../lib/api.js";
import { record } from "../lib/loaded.js";
import { esc } from "../lib/format.js";

export const read = (checkId, ownerKey) =>
  req("GET", `/checks/${encodeURIComponent(checkId)}/views/whos-in`, { ownerKey }).then((v) => record("whosIn", v));

/** Who's in; those who left an email carry an @ (view: whosIn). */
export function WhosInList(v) {
  if (!v.people.length) return `<p class="pitch" data-component="WhosInList">Nobody yet. Share the link.</p>`;
  return `<div class="ppl" data-component="WhosInList">${v.people.map((p) => `<div data-key="${esc(p.name)}">${esc(p.name)}${p.leftEmail ? ` <small title="left an email">@</small>` : ""}</div>`).join("")}</div>`;
}
