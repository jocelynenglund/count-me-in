// The two links the owner can copy, and whether this is the first visit after starting. Device only: no API.
import { record } from "../lib/loaded.js";
import { ownerKeyFor } from "../lib/device.js";
import { esc } from "../lib/format.js";

export function read(checkId) {
  const share = new URL(`./?c=${checkId}`, location.href).toString();
  const ownerLink = new URL(`./?c=${checkId}&owner#k=${ownerKeyFor(checkId)}`, location.href).toString();
  return record("shareLinks", { share, ownerLink, isNew: new URLSearchParams(location.search).has("new") });
}

/**
 * The link to share, and on the first visit the note to keep the private link (view: shareLinks).
 * The note floats to the top of the page (order: -1); the share row sits where the part is placed.
 */
export function ShareBox(v) {
  return `<div class="c" data-component="ShareBox">${v.isNew ? `<div class="note" style="order:-1"><b>Keep this page's link.</b> It's your key to see who's in and to close it. <button class="link" data-copy="${esc(v.ownerLink)}">Copy my private link</button></div>` : ""}
      <div class="share"><span>${esc(v.share.replace(/^https?:\/\//, ""))}</span><button class="link" data-copy="${esc(v.share)}">Copy link</button></div></div>`;
}
