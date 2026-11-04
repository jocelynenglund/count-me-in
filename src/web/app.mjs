// Count me in: three screens on one page.
//   /                       start an interest check
//   /?c=<id>                the pitch (public)
//   /?c=<id>&owner#k=<key>  how it's going (owner; the key stays in the fragment,
//                           so it never reaches a server log, and in localStorage)
// Every button runs the slice first (the same code the Given/When/Then specs test),
// then asks the server. The server re-checks what matters and keeps names private.

import { req, newId, personOf } from "./lib/api.js";
import { startInterestCheck } from "./slices/start-interest-check/decide.mjs";
import { pitch } from "./slices/pitch/project.mjs";
import { countMeIn } from "./slices/count-me-in/decide.mjs";
import { changeMyMind } from "./slices/change-my-mind/decide.mjs";
import { howItSGoing } from "./slices/how-it-s-going/project.mjs";
import { closeInterestCheck } from "./slices/close-interest-check/decide.mjs";

const app = document.getElementById("app");
const params = new URLSearchParams(location.search);
const checkId = params.get("c");
const today = () => new Date().toISOString().slice(0, 10);
const esc = (s) => String(s ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);
const fmtDay = (iso) => new Date(iso + "T12:00:00").toLocaleDateString("en-GB", { day: "numeric", month: "short" });
const store = {
  get: (k) => { try { return localStorage.getItem(k); } catch { return null; } },
  set: (k, v) => { try { localStorage.setItem(k, v); } catch {} },
};

// What the debug pill shows (?debug): the events this page last loaded.
let loaded = [];
window.LOG = { events: async () => loaded };

const logo = `<span class="logo" aria-hidden="true"><i></i><b></b></span>`;
const top = (left, right = "") => `<header class="top"><a class="brand" href="./">${logo}${esc(left)}</a><span>${right}</span></header>`;

function meter(count, goal) {
  const boxes = Math.min(goal, 10);
  const on = Math.min(boxes, Math.round((count / goal) * boxes));
  return `<div class="meter" style="--n:${boxes}" role="img" aria-label="${count} of ${goal}">${"<b class=on></b>".repeat(on)}${"<b></b>".repeat(boxes - on)}</div>`;
}

function show(screen, html) {
  document.body.dataset.screen = screen;
  app.innerHTML = html;
}

function error(msg) {
  const el = app.querySelector(".err");
  if (el) { el.textContent = msg; el.hidden = !msg; }
}

// ── Start an interest check (S1) ─────────────────────────────────────────────
function renderStart() {
  show("start", `
    ${top("Count me in")}
    <form class="body" id="start" novalidate>
      <h1 class="big">Got an idea? Count heads first.</h1>
      <label class="field"><span>The idea</span><input class="in" name="title" maxlength="120" placeholder="Saturday board-game club" required></label>
      <label class="field"><span>The pitch</span><textarea class="in" name="pitch" maxlength="1000" rows="3" placeholder="Where, when, and what happens if enough people are in."></textarea></label>
      <div class="row">
        <label class="field"><span>Goal</span><input class="in" name="goal" type="number" min="1" max="100000" inputmode="numeric" placeholder="10" required></label>
        <label class="field"><span>Closes <small>(optional)</small></span><input class="in" name="closesOn" type="date" min="${today()}"></label>
      </div>
      <p class="err" hidden></p>
      <div class="grow"></div>
      <button class="btn" type="submit">Start counting</button>
    </form>`);
  app.querySelector("#start").addEventListener("submit", async (e) => {
    e.preventDefault();
    const f = new FormData(e.target);
    const input = { checkId: newId(), title: f.get("title"), pitch: f.get("pitch"), goal: Number(f.get("goal")), closesOn: f.get("closesOn") || undefined, ownerKey: "set by the server", today: today() };
    const d = startInterestCheck([], input);
    if (d.rejected) return error(d.rejected);
    try {
      const { ownerKey } = await req("POST", "/checks", { body: { checkId: input.checkId, title: input.title, pitch: input.pitch, goal: input.goal, closesOn: input.closesOn } });
      store.set(`cmi_owner_${input.checkId}`, ownerKey);
      location.href = `./?c=${input.checkId}&owner&new#k=${ownerKey}`;
    } catch (err) { error(err.message); }
  });
}

// ── The pitch (S2), counting in (S3) and changing your mind (S4) ────────────
async function renderPitch() {
  let events;
  try { events = await req("GET", `/checks/${encodeURIComponent(checkId)}/events`); }
  catch (err) { return renderMissing(err); }
  loaded = events;
  // The public log names people by "person" (a hash), never by personId.
  const history = events.map((e) => (e.person ? { ...e, personId: e.person } : e));
  const v = pitch(history);
  const closedByDate = v.closesOn && v.closesOn < today();
  const open = v.open && !closedByDate;

  let personId = store.get("cmi_person");
  if (!personId) { personId = newId(16); store.set("cmi_person", personId); }
  const me = await personOf(checkId, personId);
  const myName = store.get(`cmi_name_${checkId}`) || store.get("cmi_lastname") || "";
  const imIn = history.filter((e) => e.personId === me).reduce((inn, e) => (e.type === "InterestShown" ? true : e.type === "InterestWithdrawn" ? false : inn), false);

  const screen = !open ? "closed" : v.goalReached ? "reached" : "open";
  const right = !open ? `Closed${closedByDate ? " " + fmtDay(v.closesOn) : ""}` : v.closesOn ? `Till ${fmtDay(v.closesOn)}` : "";
  show(screen, `
    ${top("Count me in", esc(right))}
    <main class="body">
      ${!open ? `<div class="stripes"></div>` : ""}
      <h1 class="big">${esc(v.title)}</h1>
      ${screen === "reached" ? `<div class="banner"><span>It's on ★ It's on ★ It's on ★ It's on ★ It's on ★ It's on</span></div>` : ""}
      ${!open ? `<div class="tag">Closed</div>` : ""}
      ${v.pitch && screen !== "reached" ? `<p class="pitch">${esc(v.pitch)}</p>` : ""}
      <div class="num">${v.count}<small>/${v.goal} ${open ? "in" : "were in"}</small></div>
      ${meter(v.count, v.goal)}
      ${screen === "reached" ? `<p class="pitch">Goal reached.${v.closesOn ? ` Names still welcome until ${fmtDay(v.closesOn)}.` : " Names still welcome."}</p>` : ""}
      ${!open ? `<p class="pitch">They've stopped taking names. If you were in and left an email, you'll hear from them.</p>` : ""}
      ${open && !imIn ? `
        <form id="in" class="stack" novalidate>
          <label class="field"><span>Your name</span><input class="in" name="name" maxlength="60" value="${esc(myName)}" autocomplete="given-name" required></label>
          <label class="field"><span>Email <small>(optional, to hear if it's on)</small></span><input class="in" name="email" type="email" maxlength="200" autocomplete="email" placeholder="you@example.com"></label>
          <p class="err" hidden></p>
          <button class="btn" type="submit">Count me in</button>
        </form>` : ""}
      ${open && imIn ? `
        <div class="you"><div class="mid">You're in${myName ? ", " + esc(myName) : ""}</div></div>
        <p class="err" hidden></p>
        <button class="link" id="out" type="button">Changed your mind? Take me off</button>` : ""}
      <div class="grow"></div>
      ${!open ? `<a class="ghost" href="./">Start your own</a>` : ""}
    </main>`);

  app.querySelector("#in")?.addEventListener("submit", async (e) => {
    e.preventDefault();
    const f = new FormData(e.target);
    const name = String(f.get("name") ?? "").trim();
    const d = countMeIn(history, { checkId, personId: me, name, email: f.get("email"), on: today() });
    if (d.rejected) return error(d.rejected);
    try {
      await req("POST", `/checks/${encodeURIComponent(checkId)}/interest`, { body: { personId, name, email: String(f.get("email") ?? "").trim() || undefined } });
      store.set(`cmi_name_${checkId}`, name); store.set("cmi_lastname", name);
      renderPitch();
    } catch (err) { error(err.message); }
  });
  app.querySelector("#out")?.addEventListener("click", async () => {
    const d = changeMyMind(history, { checkId, personId: me, on: today() });
    if (d.rejected) return error(d.rejected);
    try { await req("POST", `/checks/${encodeURIComponent(checkId)}/withdraw`, { body: { personId } }); renderPitch(); }
    catch (err) { error(err.message); }
  });
}

// ── How it's going (S5) and closing it (S6), for the owner ──────────────────
async function renderOwner() {
  const fromHash = new URLSearchParams(location.hash.slice(1)).get("k");
  if (fromHash) store.set(`cmi_owner_${checkId}`, fromHash);
  const ownerKey = fromHash || store.get(`cmi_owner_${checkId}`);
  let events;
  try { events = await req("GET", `/checks/${encodeURIComponent(checkId)}/owner-events`, { ownerKey }); }
  catch (err) { return err.status === 403 ? renderPitch() : renderMissing(err); }
  loaded = events;
  const v = howItSGoing(events);
  const p = pitch(events);
  const open = p.open && !(p.closesOn && p.closesOn < today());
  const emails = new Map(events.filter((e) => e.type === "InterestShown").map((e) => [e.name, !!e.email]));

  // Sign-ups per day: the last 7 days up to today, empty days included.
  const days = [];
  for (let i = 6; i >= 0; i--) { const d = new Date(); d.setUTCDate(d.getUTCDate() - i); days.push(d.toISOString().slice(0, 10)); }
  const per = new Map(v.byDay.map((d) => [d.day, d.in]));
  const max = Math.max(1, ...days.map((d) => per.get(d) ?? 0));
  const share = new URL(`./?c=${checkId}`, location.href).toString();
  const ownerLink = new URL(`./?c=${checkId}&owner#k=${ownerKey}`, location.href).toString();
  const toGo = Math.max(0, v.goal - v.count);

  show("owner", `
    ${top("How it's going", `<span class="acid">Private</span>`)}
    <main class="body">
      ${params.has("new") ? `<div class="note"><b>Keep this page's link.</b> It's your key to see who's in and to close it. <button class="link" data-copy="${esc(ownerLink)}">Copy my private link</button></div>` : ""}
      <h1 class="mid">${esc(p.title)}</h1>
      <div class="split"><div class="num acid">${v.count}<small>/${v.goal}</small></div>
        <div class="aside">${!open ? "Closed" : toGo ? `${toGo} to go` : "Goal reached"}${p.closesOn ? `<br>closes ${fmtDay(p.closesOn)}` : ""}</div></div>
      ${meter(v.count, v.goal)}
      <div class="field"><span>Sign-ups per day</span></div>
      <div class="chart">${days.map((d) => { const n = per.get(d) ?? 0; return `<div><span>${n}</span><b style="height:${n ? Math.round((n / max) * 80) + "%" : "3px"}"></b></div>`; }).join("")}</div>
      <div class="days">${days.map((d, i) => `<span>${i === 6 ? "Today" : new Date(d + "T12:00:00").toLocaleDateString("en-GB", { weekday: "short" })}</span>`).join("")}</div>
      ${v.people.length ? `<div class="ppl">${v.people.map((n) => `<div>${esc(n)}${emails.get(n) ? " <small title=\"left an email\">@</small>" : ""}</div>`).join("")}</div>` : `<p class="pitch">Nobody yet. Share the link.</p>`}
      <div class="grow"></div>
      <div class="share"><span>${esc(share.replace(/^https?:\/\//, ""))}</span><button class="link" data-copy="${esc(share)}">Copy link</button></div>
      <p class="err" hidden></p>
      ${open ? `<button class="ghost danger" id="close" type="button">Close it</button>` : `<div class="tag">Closed</div>`}
    </main>`);

  app.querySelectorAll("[data-copy]").forEach((b) => b.addEventListener("click", async () => {
    try { await navigator.clipboard.writeText(b.dataset.copy); b.textContent = "Copied"; } catch { prompt("Copy this link", b.dataset.copy); }
  }));
  app.querySelector("#close")?.addEventListener("click", async () => {
    if (!confirm("Close it? Nobody can count themselves in after this.")) return;
    // The owner key is the server's to check: the owner's log carries no key (nor its
    // hash), so the slice's own owner check compares nothing with nothing here.
    const d = closeInterestCheck(events, { checkId, ownerKey: undefined, on: today() });
    if (d.rejected) return error(d.rejected);
    try { await req("POST", `/checks/${encodeURIComponent(checkId)}/close`, { ownerKey }); renderOwner(); }
    catch (err) { error(err.message); }
  });
}

function renderMissing(err) {
  show("closed", `
    ${top("Count me in")}
    <main class="body">
      <div class="stripes"></div>
      <h1 class="big">${err?.status === 404 ? "Nothing here" : "Can't reach it"}</h1>
      <p class="pitch">${err?.status === 404 ? "This link doesn't lead to an interest check." : "Couldn't load it just now. Try again in a moment."}</p>
      <div class="grow"></div>
      <a class="ghost" href="./">Start your own</a>
    </main>`);
}

if (!checkId) renderStart();
else if (params.has("owner")) renderOwner();
else renderPitch();
