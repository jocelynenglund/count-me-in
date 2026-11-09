// The pitch page's parts. Each draws one view and nothing else.
import { esc, fmtDay, linkify, meter, topBar } from "../lib/format.js";

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

/** The number in, against the goal, and the meter (view: pitchCount). */
export function PitchCount(v) {
  return `<div class="c" data-component="PitchCount"><div class="num">${v.count}<small>/${v.goal} ${v.open ? "in" : "were in"}</small></div>
      ${meter(v)}</div>`;
}

/** This device's place: the form to count in, "You're in" with a way out, or nothing (view: myPlace). */
export function MyPlace(v) {
  if (v.status === "can-join") {
    return `<form id="in" class="stack" data-component="MyPlace" novalidate>
          <label class="field"><span>Your name</span><input class="in" name="name" maxlength="60" value="${esc(v.suggestedName)}" autocomplete="given-name" required></label>
          <label class="field"><span>Email <small>(optional, to hear if it's on)</small></span><input class="in" name="email" type="email" maxlength="200" autocomplete="email" placeholder="you@example.com"></label>
          <p class="err" hidden></p>
          <button class="btn" type="submit">Count me in</button>
        </form>`;
  }
  if (v.status === "in") {
    return `<div class="c" data-component="MyPlace"><div class="you"><div class="mid">You're in${v.name ? ", " + esc(v.name) : ""}</div></div>
        <p class="err" hidden></p>
        <button class="link" id="out" type="button">Changed your mind? Take me off</button></div>`;
  }
  return `<div class="c" data-component="MyPlace"></div>`;
}
