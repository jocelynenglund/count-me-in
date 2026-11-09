// The only helper the components import: it formats what a view says. No rules in here.
export const esc = (s) => String(s ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);

/** "2026-11-12" -> "12 Nov". */
export const fmtDay = (iso) => new Date(iso + "T12:00:00").toLocaleDateString("en-GB", { day: "numeric", month: "short" });

/** "2026-11-12" -> "Thu". */
export const weekday = (iso) => new Date(iso + "T12:00:00").toLocaleDateString("en-GB", { weekday: "short" });

/** "3 emails", "1 email". */
export const plural = (n, word) => `${n} ${word}${n === 1 ? "" : "s"}`;

/** "Ann <ann@example.com>, Bo <bo@example.org>" for pasting into Bcc. */
export const emailList = (people) => people.map((p) => `${String(p.name).replace(/[<>",]/g, "")} <${p.email}>`).join(", ");

// Links in a pitch: web addresses become clickable (https://… and bare ones like adventvibes.itsybit.se).
// Display only: the stored text is unchanged. The text is escaped first, so only addresses become HTML.
const ADDRESS = /(^|[\s(])((?:https?:\/\/)?(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z]{2,}(?::\d+)?(?:\/[^\s<]*)?)/gi;
export function linkify(text) {
  return esc(text).replace(ADDRESS, (m, before, addr) => {
    const trail = addr.match(/[.,!?:;)'"]+$/)?.[0] ?? ""; // a full stop after an address isn't part of it
    const a = trail ? addr.slice(0, -trail.length) : addr;
    const href = /^https?:\/\//i.test(a) ? a : `https://${a}`;
    return `${before}<a href="${href}" target="_blank" rel="noopener nofollow ugc">${a}</a>${trail}`;
  });
}

/** The poster header; `right` is already-escaped HTML. */
export const topBar = (left, right = "") =>
  `<header class="top" data-component="TopBar"><a class="brand" href="./"><span class="logo" aria-hidden="true"><i></i><b></b></span>${esc(left)}</a><span>${right}</span></header>`;

/** The boxes meter: the view says how many boxes there are and how many are on. */
export const meter = ({ count, goal, meterBoxes, meterOn }) =>
  `<div class="meter" style="--n:${meterBoxes}" role="img" aria-label="${count} of ${goal}">${"<b class=on></b>".repeat(meterOn)}${"<b></b>".repeat(meterBoxes - meterOn)}</div>`;

/** One button per look; the pressed one is the pitch's look. Shared by the start page's and the owner page's picker. */
export const lookButtons = (choices) => choices
  .map((c) => `<button type="button" class="look" data-theme="${esc(c.theme)}" aria-pressed="${c.picked}">${esc(c.label)}</button>`).join("");
