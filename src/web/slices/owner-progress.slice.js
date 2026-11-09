// Owner only (the owner key goes along): owner-progress.
import { req } from "../lib/api.js";
import { record } from "../lib/loaded.js";
import { esc, fmtDay, meter } from "../lib/format.js";

export const read = (checkId, ownerKey) =>
  req("GET", `/checks/${encodeURIComponent(checkId)}/views/owner-progress`, { ownerKey }).then((v) => record("ownerProgress", v));

/** Title, how many are in against the goal, what's left (view: ownerProgress). */
export function OwnerProgress(v) {
  const closed = String(v.status).startsWith("closed");
  const aside = closed ? "Closed" : v.toGo ? `${v.toGo} to go` : "Goal reached";
  return `<div class="c" data-component="OwnerProgress"><h1 class="mid">${esc(v.title)}</h1>
      <div class="split"><div class="num acid">${v.count}<small>/${v.goal}</small></div>
        <div class="aside">${aside}${v.closesOn ? `<br>closes ${fmtDay(v.closesOn)}` : ""}</div></div>
      ${meter(v)}</div>`;
}
