// The screens, observed. Each screen module is run for real against a stand-in page (a tiny DOM) and a
// recording fetch that answers with view values like the ones the API's scenarios and the component tests use.
// Per screen and state it records
//   - the API routes the page called: a GET on a view route is a view it draws ("draws"); a POST that is
//     not a view is a command a control sent ("taps"). Who called is read from the call stack (which slice's
//     read or send), and which control sent it is the component the page was tapped on;
//   - the components that ended up in the page (the data-component roots): where each screen is "seen on".
// It then taps every control the page wired (click, then submit), the way a person would.
// Gates: every exported component is seen on some screen; every view the web reads has its component on the
// same screen; and, given the API's route list, no route is called that the API lacks and no view or command
// of the API goes uncalled (Public Log, Owner Log and the version probe aside).
// Output (for scripts/add-screens.py), when SCREENS_OUT is set: what was observed. The API's routes come from
// API_ROUTES (the file the C# tests write to GWT_EXPORT/routes.json); without it the API gates are skipped.
import { test } from "node:test";
import assert from "node:assert/strict";
import { existsSync, readFileSync, readdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const root = new URL("../", import.meta.url).pathname;
const web = join(root, "src/web");

// ---- a stand-in page -------------------------------------------------------------------------------------
const VOID = new Set(["input", "br", "img", "hr", "meta", "link"]);
class El {
  constructor(tag, attrs = {}, parent = null) { this.tag = tag; this.attrs = attrs; this.parent = parent; this.children = []; this.listeners = []; this.hidden = false; this.textContent = ""; }
  get dataset() { return new Proxy(this, { get: (t, k) => t.attrs[`data-${String(k).replace(/[A-Z]/g, (c) => "-" + c.toLowerCase())}`], set: (t, k, v) => { t.attrs[`data-${k}`] = v; return true; } }); }
  get component() { return this.attrs["data-component"] ?? this.parent?.component; }
  get value() {
    if (this.attrs.value) return this.attrs.value;
    const t = this.attrs.type;
    return t === "date" ? "2026-12-01" : t === "email" ? "ann@example.com" : t === "number" ? "10" : this.tag === "textarea" ? "Saturdays" : "Ann";
  }
  getAttribute(k) { return this.attrs[k] ?? null; }
  setAttribute(k, v) { this.attrs[k] = v; }
  addEventListener(type, fn) { this.listeners.push({ type, fn }); }
  all() { return this.children.flatMap((c) => [c, ...c.all()]); }
  matches(sel) {
    return [...sel.matchAll(/#([\w-]+)|\.([\w-]+)|\[([\w-]+)(?:="([^"]*)")?\]|^([a-z]+)/g)].every(([, id, cls, attr, val, tag]) =>
      id ? this.attrs.id === id : cls ? (this.attrs.class ?? "").split(/\s+/).includes(cls) : attr ? attr in this.attrs && (val === undefined || this.attrs[attr] === val) : this.tag === tag);
  }
  querySelectorAll(sel) { return this.all().filter((e) => e.matches(sel)); }
  querySelector(sel) { return this.querySelectorAll(sel)[0] ?? null; }
}
function parse(html, into) {
  let cur = into;
  for (const m of html.matchAll(/<(\/?)([a-zA-Z][\w-]*)([^>]*?)(\/?)>/g)) {
    const [, close, tag, rest] = m;
    if (close) { for (let e = cur; e && e !== into; e = e.parent) if (e.tag === tag) { cur = e.parent; break; } continue; }
    const attrs = {};
    for (const a of rest.matchAll(/([\w:-]+)(?:=(?:"([^"]*)"|'([^']*)'|([^\s>]+)))?/g)) attrs[a[1]] = a[2] ?? a[3] ?? a[4] ?? "";
    const el = new El(tag.toLowerCase(), attrs, cur);
    cur.children.push(el);
    if (!VOID.has(el.tag)) cur = el;
  }
}

let seenNow = null; // set per state: called with each new tree
const app = new El("div", { id: "app" });
Object.defineProperty(app, "innerHTML", { set(html) { this.children = []; parse(html, this); seenNow?.(this); }, get() { return ""; } });

const store = new Map();
globalThis.localStorage = { getItem: (k) => store.get(k) ?? null, setItem: (k, v) => store.set(k, String(v)), get length() { return store.size; }, key: (i) => [...store.keys()][i] };
globalThis.document = { body: { dataset: {} }, getElementById: () => app };
globalThis.FormData = class { constructor(form) { this.form = form; } get(name) { return this.form.querySelector(`[name="${name}"]`)?.value ?? null; } };
globalThis.confirm = () => true;
globalThis.prompt = () => null;
Object.defineProperty(globalThis, "navigator", { configurable: true, value: { clipboard: { writeText: async () => {} } } });
globalThis.location = { hostname: "test.invalid", href: "https://test.invalid/", search: "", hash: "" };

// ---- the recording fetch ----------------------------------------------------------------------------------
let views = {};            // view value by route tail, for this state
let current = null;        // { screen, control }
const observed = [];
const strays = [];
const settle = async () => { for (let i = 0; i < 6; i++) await new Promise((r) => setTimeout(r, 0)); };

globalThis.fetch = async (url, { method = "GET" } = {}) => {
  const path = String(url).replace(/^.*\/api\/countmein/, "").replace(/^\/checks\/[^/{]+/, "/checks/{checkId}");
  const route = path;
  const slice = (new Error().stack.split("\n").map((l) => l.match(/src\/web\/slices\/([a-z-]+)\.slice\.js/)).find(Boolean) ?? [])[1];
  const isView = route.includes("/views/");
  observed.push({ screen: current.screen, method, route, slice, how: isView ? "draws" : "taps", control: isView ? undefined : current.control });
  const tail = route.replace(/^.*\/views\//, "");
  const body = isView ? views[tail] : route === "/checks" ? { ownerKey: "k1" } : null;
  if (isView && body === undefined) return { ok: false, status: 404, text: async () => JSON.stringify({ error: "no such view in this state" }) };
  return { ok: true, status: 200, text: async () => (body ? JSON.stringify(body) : "") };
};

// ---- the states -------------------------------------------------------------------------------------------
// Each screen module names itself: export const screen = "<Name>".
const modules = Object.fromEntries(await Promise.all(["start", "pitch", "owner"].map(async (k) => [k, await import(join(web, `screens/${k}.screen.js`))])));
const SCREENS = Object.fromEntries(Object.entries(modules).map(([k, m]) => [k, m.screen]));
const looks = (pressed) => ({ choices: [{ theme: "classic", label: "Classic", picked: pressed === "classic" }, { theme: "christmas", label: "Christmas", picked: pressed === "christmas" }] });
const card = (over) => ({ title: "Board-game club", pitch: "Saturdays at the library", status: "open", closedByDate: false, ...over });
const count = (over) => ({ count: 3, goal: 10, open: true, meterBoxes: 10, meterOn: 3, ...over });
const progress = (over) => ({ title: "Board-game club", count: 3, goal: 10, toGo: 7, status: "open", meterBoxes: 10, meterOn: 3, ...over });
const days = [0, 0, 1, 0, 2, 0, 4].map((n, i) => ({ day: `2026-11-0${i + 1}`, in: n }));
const pitchViews = (card$, place, over = {}) => ({ "pitch-card": card$, "pitch-count": count(over), "my-place": place, "pitch-look": { theme: "christmas" } });
const ownerViews = (over = {}) => ({
  "owner-progress": progress(), "signups-per-day": { days }, "whos-in": { people: [{ name: "Ann", leftEmail: true }, { name: "Bo", leftEmail: false }] },
  "reachable-people": { people: [{ name: "Ann", email: "ann@example.com" }] }, "closing-date": { closesOn: "2026-11-12", canChange: true, earliest: "2026-11-09" },
  "close-state": { status: "open" }, "look-choices": looks("classic"), "pitch-look": { theme: "christmas" }, ...over,
});
const STATES = [
  { screen: "start", name: "no checks yet", views: { "start-form": { earliestClosingDate: "2026-11-09", defaultGoal: 10 }, "look-choices": looks("classic") } },
  { screen: "start", name: "some checks", owner: "K1", views: { "start-form": { earliestClosingDate: "2026-11-09", defaultGoal: 10 }, "look-choices": looks("classic"),
      "my-interest-checks": { checks: [{ checkId: "a1", title: "Board games", count: 3, goal: 10, open: true }] } } },
  { screen: "pitch", name: "open, can join", views: pitchViews(card({ closesOn: "2026-11-12" }), { status: "can-join" }) },
  { screen: "pitch", name: "open, in", views: pitchViews(card({}), { status: "in", name: "Ann" }) },
  { screen: "pitch", name: "reached", views: pitchViews(card({ status: "reached" }), { status: "in" }, { count: 10, meterOn: 10 }) },
  { screen: "pitch", name: "closed by the owner", views: pitchViews(card({ status: "closed" }), { status: "closed" }, { open: false }) },
  { screen: "pitch", name: "closed by its date", views: pitchViews(card({ status: "closed", closesOn: "2026-11-05", closedByDate: true }), { status: "closed" }, { open: false }) },
  { screen: "owner", name: "open", owner: "K1", views: ownerViews() },
  { screen: "owner", name: "reached", owner: "K1", views: ownerViews({ "owner-progress": progress({ count: 10, toGo: 0, status: "reached", meterOn: 10 }) }) },
  { screen: "owner", name: "closed by the owner", owner: "K1", views: ownerViews({ "close-state": { status: "closed-by-owner" }, "closing-date": { canChange: false, earliest: "2026-11-09" }, "owner-progress": progress({ status: "closed" }) }) },
  { screen: "owner", name: "closed by its date", owner: "K1", views: ownerViews({ "close-state": { status: "closed-by-date" }, "closing-date": { canChange: false, earliest: "2026-11-09" }, "owner-progress": progress({ status: "closed" }) }) },
  { screen: "owner", name: "no sign-ups", owner: "K1", views: ownerViews({ "signups-per-day": { days: [] }, "whos-in": { people: [] }, "reachable-people": { people: [] }, "closing-date": { canChange: true, earliest: "2026-11-09" } }) },
  { screen: "owner", name: "with emails", owner: "K1", views: ownerViews({ "reachable-people": { people: [{ name: "Ann", email: "ann@example.com" }, { name: "Bo", email: "bo@example.org" }] } }) },
];

const seenOn = Object.fromEntries(Object.values(SCREENS).filter(Boolean).map((s) => [s, []]));
const errors = [];
const fns = { start: () => modules.start.showStart(), pitch: () => modules.pitch.showPitch("a1"), owner: () => modules.owner.showOwner("a1") };

for (const st of STATES) {
  const screen = SCREENS[st.screen];
  store.clear(); if (st.owner) store.set("cmi_owner_a1", st.owner);
  views = st.views; current = { screen, control: undefined };
  globalThis.location.search = st.screen === "start" ? "" : st.screen === "owner" ? "?c=a1&owner" : "?c=a1";
  globalThis.location.hash = ""; globalThis.location.href = `https://test.invalid/${globalThis.location.search}`;
  document.body.dataset.theme = undefined;
  const seen = (c) => { if (!seenOn[screen].includes(c)) seenOn[screen].push(c); };
  seenNow = (tree) => {
    for (const e of tree.all()) if (e.attrs["data-component"]) seen(e.attrs["data-component"]);
    // PageTheme draws no HTML: it names the look the page takes, which shows on the body
    if (views["pitch-look"] && document.body.dataset.theme === views["pitch-look"].theme) seen("PageTheme");
  };
  try {
    await fns[st.screen]();
    await settle();
    // tap what the page wired: every click, then every submit
    const wired = app.all().flatMap((el) => el.listeners.map((l) => ({ el, ...l })));
    for (const type of ["click", "submit"]) for (const { el, fn } of wired.filter((w) => w.type === type)) {
      current.control = el.component;
      try { await fn({ target: el, preventDefault() {} }); } catch (err) { errors.push(`${screen} (${st.name}): ${el.component} ${type}: ${err.message}`); }
      await settle();
    }
  } catch (err) { errors.push(`${screen} (${st.name}): ${err.stack}`); }
}
seenNow = null;

// ---- what the web declares --------------------------------------------------------------------------------
const slicesDir = join(web, "slices");
const sliceFiles = readdirSync(slicesDir).filter((f) => f.endsWith(".slice.js"));
const components = {}; // exported component -> { slice, file, line }
for (const f of sliceFiles) readFileSync(join(slicesDir, f), "utf8").split("\n").forEach((l, i) => {
  const m = l.match(/^export (?:async )?(?:function|const) ([A-Z]\w*)/);
  if (m) components[m[1]] = { slice: f.replace(".slice.js", ""), file: `src/web/slices/${f}`, line: i + 1 };
});
const feeds = {}; // view slice -> the component that draws it, as the component tests declare it
for (const f of readdirSync(join(root, "tests/components")).filter((f) => f.endsWith(".test.mjs")))
  for (const m of readFileSync(join(root, "tests/components", f), "utf8").matchAll(/thenComponent\(\s*"([A-Za-z]+)"\s*,\s*"([a-z()-]+)"/g)) if (m[2] !== "(none)") feeds[m[2]] = m[1];
feeds["pitch-look"] = "PageTheme"; // draws no HTML, so its test is a plain one

// What was observed, one entry per screen + route (+ control), with the component that draws or taps it.
const entries = [];
for (const o of observed) {
  const component = o.how === "draws" ? feeds[o.slice] : o.control;
  const key = `${o.screen}|${o.method} ${o.route}|${component}`;
  if (!entries.some((e) => e.key === key)) entries.push({ key, screen: o.screen, screenFile: Object.entries(SCREENS).find(([, n]) => n === o.screen)[0] + ".screen.js", method: o.method, route: o.route, slice: o.slice, how: o.how, component });
}
const screenFiles = Object.fromEntries(Object.entries(SCREENS).map(([k, name]) => [name, `${k}.screen.js`]));
const result = { observed: entries.map(({ key, ...e }) => e), seenOn, screenFiles, components, feeds };
if (process.env.SCREENS_OUT) writeFileSync(process.env.SCREENS_OUT, JSON.stringify(result, null, 1));

// ---- the gates --------------------------------------------------------------------------------------------
test("every screen names itself (export const screen)", () => {
  for (const [k, m] of Object.entries(modules)) assert.ok(typeof m.screen === "string" && m.screen, `${k}.screen.js has no screen export`);
});

test("every screen state ran without an error", () => assert.deepEqual(errors, []));

test("every screen shows at least one component, and every call was made by a slice", () => {
  for (const [s, cs] of Object.entries(seenOn)) assert.ok(cs.length > 0, `${s} shows no component`);
  assert.deepEqual(observed.filter((o) => !o.slice).map((o) => `${o.screen}: ${o.method} ${o.route}`), [], "a call that no slice made");
});

test("every exported component is seen on some screen in some state", () => {
  const seenAll = new Set(Object.values(seenOn).flat());
  for (const c of Object.keys(components)) assert.ok(seenAll.has(c), `${c} (${components[c].file}) is never seen on any screen`);
});

test("every view a screen reads has its component on the same screen; every command has a control", () => {
  for (const e of result.observed) {
    assert.ok(e.component, `${e.screen}: ${e.method} ${e.route} (slice ${e.slice}) has no ${e.how === "draws" ? "component that draws it" : "control that sent it"}`);
    assert.ok(seenOn[e.screen].includes(e.component), `${e.screen}: ${e.component} is not on the page, but its ${e.route} was ${e.how === "draws" ? "read" : "sent"}`);
  }
});

test("what the web calls is what the API has, and the API's views and commands are all called", (t) => {
  const file = process.env.API_ROUTES;
  if (!file || !existsSync(file)) return t.skip("no API_ROUTES (the C# tests write GWT_EXPORT/routes.json)");
  const api = JSON.parse(readFileSync(file, "utf8")); // [{ method, route, type }]
  const same = (a, b) => a.method === b.method && a.route === b.route;
  for (const o of result.observed) assert.ok(api.some((a) => same(a, o)), `${o.screen} calls ${o.method} ${o.route}, which the API does not have`);
  const needed = api.filter((a) => a.route.includes("/views/") || a.method === "POST");
  const unused = needed.filter((a) => !result.observed.some((o) => same(a, o)));
  assert.deepEqual(unused.map((a) => `${a.method} ${a.route} (${a.type})`), [], "views and commands of the API that no screen calls");
});

test("spot checks", () => {
  const has = (screen, how, component, route) => result.observed.some((e) => e.screen === screen && e.how === how && e.component === component && e.route.endsWith(route));
  assert.ok(has("Pitch page", "draws", "PitchCard", "/views/pitch-card"));
  assert.ok(has("Pitch page", "taps", "CountMeInForm", "/interest"));
  assert.ok(has("Owner page", "taps", "CloseButton", "/close"));
  assert.ok(has("Owner page", "draws", "LookPicker", "/views/look-choices"));
  assert.ok(has("New interest check", "draws", "StartLookPicker", "/views/look-choices"));
});
