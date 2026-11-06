// Links in a pitch: web addresses become clickable (https://… and bare ones like adventvibes.itsybit.se).
// Display only: the stored text is unchanged. The text is escaped first, so only addresses become HTML.
const esc = (s) => String(s ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);
const ADDRESS = /(^|[\s(])((?:https?:\/\/)?(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z]{2,}(?::\d+)?(?:\/[^\s<]*)?)/gi;

export function linkify(text) {
  return esc(text).replace(ADDRESS, (m, before, addr) => {
    const trail = addr.match(/[.,!?:;)'"]+$/)?.[0] ?? ""; // a full stop after an address isn't part of it
    const a = trail ? addr.slice(0, -trail.length) : addr;
    const href = /^https?:\/\//i.test(a) ? a : `https://${a}`;
    return `${before}<a href="${href}" target="_blank" rel="noopener nofollow ugc">${a}</a>${trail}`;
  });
}
