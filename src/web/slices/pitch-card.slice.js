// What the pitch page says about the idea: title, pitch, status and closing date.
import { req } from "../lib/api.js";
import { record } from "../lib/loaded.js";
import { esc, fmtDay, linkify, topBar } from "../lib/format.js";

export const read = (checkId) => req("GET", `/checks/${encodeURIComponent(checkId)}/views/pitch-card`).then((v) => record("pitchCard", v));

/**
 * The idea itself (view: pitchCard): the top bar's right-hand side, the stripes or "It's on" banner,
 * the title and pitch, and the notes once it's reached or closed. `slots` are the other parts of the
 * page, already drawn, placed where they belong: the count under the pitch, "my place" after the notes.
 */
export function PitchCard(v, slots = {}) {
  const closed = v.status === "closed";
  const reached = v.status === "reached";
  const right = closed ? `Closed${v.closedByDate ? " " + fmtDay(v.closesOn) : ""}` : v.closesOn ? `Till ${fmtDay(v.closesOn)}` : "";
  return `<div class="c" data-component="PitchCard">${topBar("Count me in", esc(right))}
    <main class="body">
      ${closed ? `<div class="stripes"></div>` : ""}
      <h1 class="big">${esc(v.title)}</h1>
      ${reached ? `<div class="banner"><span>It's on ★ It's on ★ It's on ★ It's on ★ It's on ★ It's on</span></div>` : ""}
      ${closed ? `<div class="tag">Closed</div>` : ""}
      ${v.pitch && !reached ? `<p class="pitch">${linkify(v.pitch)}</p>` : ""}
      ${slots.count ?? ""}
      ${reached ? `<p class="pitch">Goal reached.${v.closesOn ? ` Names still welcome until ${fmtDay(v.closesOn)}.` : " Names still welcome."}</p>` : ""}
      ${closed ? `<p class="pitch">They've stopped taking names. If you were in and left an email, you'll hear from them.</p>` : ""}
      ${slots.place ?? ""}
      <div class="grow"></div>
      ${closed ? `<a class="ghost" href="./">Start your own</a>` : ""}
    </main></div>`;
}
