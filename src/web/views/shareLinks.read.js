// The two links the owner can copy, and whether this is the first visit after starting. Device only: no API.
import { record } from "../lib/loaded.js";
import { ownerKeyFor } from "../lib/device.js";

export function shareLinks(checkId) {
  const share = new URL(`./?c=${checkId}`, location.href).toString();
  const ownerLink = new URL(`./?c=${checkId}&owner#k=${ownerKeyFor(checkId)}`, location.href).toString();
  return record("shareLinks", { share, ownerLink, isNew: new URLSearchParams(location.search).has("new") });
}
