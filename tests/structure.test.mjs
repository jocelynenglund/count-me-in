// The page decides nothing: its shape is enforced here, on every deploy.
//   views/*.read.js   one fetch each (the only code that reads the API, with lib/api.js, lib/commands.js)
//   screens/*.parts.js components: view -> HTML, importing nothing but ../lib/format.js
//   screens/*.screen.js compose parts and send commands: no storage, no fetch, no innerHTML except page.screen.js
import { test } from "node:test";
import assert from "node:assert/strict";
import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";

const web = new URL("../src/web/", import.meta.url).pathname;
const ls = (dir, suffix) => readdirSync(join(web, dir)).filter((f) => f.endsWith(suffix)).map((f) => join(dir, f));
const read = (f) => readFileSync(join(web, f), "utf8");
const code = (f) => read(f).replace(/\/\*[\s\S]*?\*\//g, "").replace(/^\s*\/\/.*$/gm, ""); // without comments
const importsOf = (f) => [...read(f).matchAll(/^import\s[^;]*?from\s+"([^"]+)"/gm)].map((m) => m[1]);

const parts = ls("screens", ".parts.js");
const screens = [...ls("screens", ".screen.js"), "app.mjs"];
const views = ls("views", ".read.js");

test("components import nothing but the formatting helper", () => {
  for (const f of parts) for (const i of importsOf(f)) assert.equal(i, "../lib/format.js", `${f} imports ${i}`);
});

test("screens and the router never touch storage or the network themselves", () => {
  for (const f of screens) {
    const c = code(f);
    assert.doesNotMatch(c, /localStorage|sessionStorage|\bfetch\s*\(|\breq\s*\(/, `${f} reads state or calls the API directly`);
    for (const i of importsOf(f)) assert.notEqual(i, "../lib/api.js", `${f} imports api.js; commands go through lib/commands.js`);
  }
});

test("only views, commands, the device and api.js reach the network or storage", () => {
  const allowed = new Set([...views, "lib/api.js", "lib/commands.js", "lib/device.js"]);
  const all = [...ls("lib", ".js"), ...ls("lib", ".mjs"), ...ls("screens", ".js"), ...views, "app.mjs"];
  for (const f of all) if (!allowed.has(f)) assert.doesNotMatch(code(f), /localStorage|sessionStorage|\bfetch\s*\(|\breq\s*\(/, `${f} must not read storage or call the API`);
});

test("no innerHTML outside the page shell", () => {
  for (const f of [...parts, ...views, ...ls("lib", ".js"), "app.mjs", ...screens.filter((s) => s !== "screens/page.screen.js")])
    assert.doesNotMatch(code(f), /innerHTML|insertAdjacentHTML|outerHTML/, `${f} draws HTML itself`);
});

// What the component tests say each component draws from.
const calls = ls("../../tests/components", ".test.mjs").flatMap((f) =>
  [...readFileSync(join(web, f), "utf8").matchAll(/thenComponent\(\s*"([A-Za-z]+)"\s*,\s*"([A-Za-z]+)"/g)].map((m) => ({ component: m[1], view: m[2] })));
const viewsOf = new Map(); const componentsOf = new Map();
for (const { component, view } of calls) {
  viewsOf.set(component, new Set([...(viewsOf.get(component) ?? []), view]));
  componentsOf.set(view, new Set([...(componentsOf.get(view) ?? []), component]));
}

// PageTheme draws no HTML (it names the theme), so its test is a plain one; it still draws from one view.
viewsOf.set("PageTheme", new Set(["pitchLook"])); componentsOf.set("pitchLook", new Set(["PageTheme"]));

test("each component draws from exactly one view", () => {
  for (const [c, vs] of viewsOf) assert.equal(vs.size, 1, `${c} draws from ${[...vs].join(", ")}`);
});

test("each view feeds exactly one component", () => {
  for (const [v, cs] of componentsOf) assert.equal(cs.size, 1, `${v} feeds ${[...cs].join(", ")}`);
});

const exported = parts.flatMap((f) => [...read(f).matchAll(/^export (?:const|function) ([A-Z]\w*)/gm)].map((m) => ({ name: m[1], file: f })));
const noView = new Set(["TopBar", "Missing"]); // static chrome and an error: no view
const notHtml = new Set(["PageTheme"]);        // draws no HTML: the theme name the screen puts on the body

test("every component that takes a view has a component test", () => {
  for (const { name } of exported) if (!noView.has(name) && !notHtml.has(name)) assert.ok(viewsOf.has(name), `${name} has no thenComponent test`);
});

test("every exported component is used by a screen", () => {
  const screenCode = screens.map(code).join("\n");
  for (const { name } of exported) assert.match(screenCode, new RegExp(`\\b${name}\\b`), `${name} is not used by any screen`);
});

test("every view is imported by a screen, and feeds a tested component", () => {
  const screenImports = screens.flatMap(importsOf);
  for (const v of views) {
    assert.ok(screenImports.some((i) => i === "../" + v), `${v} is not used by any screen`);
    const name = v.replace(/^views\//, "").replace(/\.read\.js$/, "");
    assert.ok(componentsOf.has(name), `${name} feeds no tested component`);
  }
  for (const v of componentsOf.keys()) assert.ok(views.includes(`views/${v}.read.js`), `${v} has no views/${v}.read.js`);
});

test("views are named by function, files by view", () => {
  for (const v of views) assert.match(v, /^views\/[a-z][A-Za-z]+\.read\.js$/);
  for (const bad of ["view", "screen", "read", "rowView", "state"]) assert.ok(!views.includes(`views/${bad}.read.js`));
});

test("no logic left from the old slices", () => {
  assert.deepEqual(readdirSync(join(web, "slices")).sort(), ["debug", "whats-new"]);
});
