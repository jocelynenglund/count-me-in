// The page decides nothing: its shape is enforced here, on every deploy.
//   src/web/slices/<slice>.slice.js  one file per board slice (kebab-case, as named on the board):
//       view slice     exports `read` (one fetch) and at most one component (view -> HTML)
//       command slice  exports `send` (one POST), its control component(s), and `wire(root, ...)`
//   src/web/screens/*.screen.js      compose slices: define no components, add no event listeners, no storage, no fetch
//   src/web/screens/page.chrome.js   TopBar and Missing: chrome that takes no view
// Components only draw: tests/components/harness.mjs runs each one with fetch and localStorage trapped.
import { test } from "node:test";
import assert from "node:assert/strict";
import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import { join } from "node:path";

const web = new URL("../src/web/", import.meta.url).pathname;
const ls = (dir, suffix) => readdirSync(join(web, dir)).filter((f) => f.endsWith(suffix)).map((f) => join(dir, f));
const read = (f) => readFileSync(join(web, f), "utf8");
const code = (f) => read(f).replace(/\/\*[\s\S]*?\*\//g, "").replace(/^\s*\/\/.*$/gm, ""); // without comments
const importsOf = (f) => [...read(f).matchAll(/^import\s[^;]*?from\s+"([^"]+)"/gm)].map((m) => m[1]);
const walk = (dir) => readdirSync(join(web, dir)).flatMap((f) => statSync(join(web, dir, f)).isDirectory() ? walk(join(dir, f)) : [join(dir, f)]);

const STORAGE_OR_NETWORK = /localStorage|sessionStorage|\bfetch\s*\(|\breq\s*\(/;
const sliceFiles = ls("slices", ".slice.js");
const screens = ls("screens", ".screen.js");
const screenFiles = [...screens, "screens/page.chrome.js"];
const exportsOf = (f) => [...read(f).matchAll(/^export (?:async )?(?:const|function) (\w+)/gm)].map((m) => m[1]);
const componentsIn = (f) => exportsOf(f).filter((n) => /^[A-Z]/.test(n));
const nameOf = (f) => f.replace(/^slices\//, "").replace(/\.slice\.js$/, "");
const isCommand = (f) => exportsOf(f).includes("send");

test("the layout: slices are flat, kebab-case files; screens are screens; the old layout is gone", () => {
  assert.deepEqual(readdirSync(join(web, "slices")).filter((f) => !f.endsWith(".slice.js")).sort(), ["debug", "whats-new"]);
  for (const f of sliceFiles) assert.match(nameOf(f), /^[a-z]+(-[a-z]+)*$/, `${f} is not a kebab-case slice name`);
  assert.deepEqual(readdirSync(join(web, "screens")).filter((f) => !f.endsWith(".screen.js")).sort(), ["page.chrome.js"]);
  assert.ok(!existsSync(join(web, "views")), "views/ must not exist");
  assert.ok(!existsSync(join(web, "lib/commands.js")), "lib/commands.js must not exist");
  assert.deepEqual(walk(".").filter((f) => f.endsWith(".parts.js") || f.endsWith(".read.js") || f.endsWith(".command.js")), []);
});

test("a view slice exports read and at most one component; a command slice exports send, a control and wire", () => {
  for (const f of sliceFiles) {
    const ex = exportsOf(f);
    const comps = componentsIn(f);
    if (isCommand(f)) {
      assert.ok(ex.includes("wire"), `${f} (command slice) has no wire`);
      assert.ok(comps.length >= 1, `${f} (command slice) has no control component`);
      assert.ok(!ex.includes("read"), `${f} is both a command and a view slice`);
    } else {
      assert.ok(ex.includes("read"), `${f} (view slice) has no read`);
      assert.ok(comps.length <= 1, `${f} (view slice) has ${comps.length} components`);
      if (ex.includes("wire")) assert.equal(comps.length, 1, `${f} wires a component it does not have`);
    }
  }
});

test("slices import only the lib helpers; components only draw (no read, no device, no storage inside them)", () => {
  for (const f of sliceFiles) {
    for (const i of importsOf(f)) assert.match(i, /^\.\.\/lib\/(api|device|loaded|format)\.js$/, `${f} imports ${i}`);
    const src = code(f);
    // each component: from its export to the next top-level export
    const blocks = src.split(/^(?=export )/m).filter((b) => /^export (?:function|const) [A-Z]/.test(b));
    for (const b of blocks) assert.doesNotMatch(b, /localStorage|sessionStorage|\bfetch\s*\(|\breq\s*\(|\bdevice\.|\bread\s*\(|\bsend\s*\(|\brecord\s*\(/, `a component in ${f} reaches for state: ${b.slice(0, 60)}`);
    if (!isCommand(f)) assert.doesNotMatch(src, /\bsend\b/, `${f} (view slice) sends a command`);
  }
  for (const f of sliceFiles.filter(isCommand)) assert.doesNotMatch(importsOf(f).join(" "), /loaded/, `${f} records a view`);
});

test("screens and the router never touch storage or the network themselves, and add no event listeners", () => {
  for (const f of [...screens, "app.mjs"]) {
    const c = code(f);
    assert.doesNotMatch(c, STORAGE_OR_NETWORK, `${f} reads state or calls the API directly`);
    for (const i of importsOf(f)) assert.notEqual(i, "../lib/api.js", `${f} imports api.js; commands live in slices`);
    if (f !== "screens/page.screen.js") assert.doesNotMatch(c, /addEventListener/, `${f} wires an event itself; a slice's wire does that`);
  }
});

test("screens define no components", () => {
  for (const f of screens) {
    assert.doesNotMatch(read(f), /data-component/, `${f} draws a component`);
    assert.doesNotMatch(code(f), /^(export )?(function|const) [A-Z]/m, `${f} defines a component`);
  }
});

test("only slices, the device and api.js reach the network or storage", () => {
  const allowed = new Set([...sliceFiles, "lib/api.js", "lib/device.js"]);
  const all = [...ls("lib", ".js"), ...screenFiles, "app.mjs"];
  for (const f of all) if (!allowed.has(f)) assert.doesNotMatch(code(f), STORAGE_OR_NETWORK, `${f} must not read storage or call the API`);
});

test("no innerHTML outside the page shell", () => {
  for (const f of [...sliceFiles, ...ls("lib", ".js"), "app.mjs", ...screenFiles.filter((s) => s !== "screens/page.screen.js")])
    assert.doesNotMatch(code(f), /innerHTML|insertAdjacentHTML|outerHTML/, `${f} draws HTML itself`);
});

// What the component tests say each component draws from (the read slice, or "(none)").
const calls = ls("../../tests/components", ".test.mjs").flatMap((f) =>
  [...readFileSync(join(web, f), "utf8").matchAll(/thenComponent\(\s*"([A-Za-z]+)"\s*,\s*"([a-z()-]+)"/g)].map((m) => ({ component: m[1], read: m[2] })));
const readsOf = new Map(); const componentsOf = new Map();
for (const { component, read: r } of calls) {
  readsOf.set(component, new Set([...(readsOf.get(component) ?? []), r]));
  if (r !== "(none)") componentsOf.set(r, new Set([...(componentsOf.get(r) ?? []), component]));
}
// PageTheme draws no HTML (it names the theme), so its test is a plain one; it still draws from one read.
readsOf.set("PageTheme", new Set(["pitch-look"])); componentsOf.set("pitch-look", new Set(["PageTheme"]));

test("each component draws from at most one read", () => {
  for (const [c, rs] of readsOf) assert.equal(rs.size, 1, `${c} draws from ${[...rs].join(", ")}`);
});

test("each read feeds at most one component", () => {
  for (const [r, cs] of componentsOf) assert.equal(cs.size, 1, `${r} feeds ${[...cs].join(", ")}`);
});

test("every component has a component test, and the read it names is a view slice", () => {
  for (const f of sliceFiles) for (const name of componentsIn(f)) assert.ok(readsOf.has(name), `${name} (${f}) has no thenComponent test`);
  const reads = new Set(sliceFiles.filter((f) => !isCommand(f)).map(nameOf));
  for (const r of componentsOf.keys()) assert.ok(reads.has(r), `a test names ${r}, which is not a view slice`);
});

test("every view slice feeds a tested component", () => {
  for (const f of sliceFiles.filter((f) => !isCommand(f))) assert.ok(componentsOf.has(nameOf(f)), `${nameOf(f)} feeds no tested component`);
});

test("every slice file is imported by a screen, and every component is used by one", () => {
  const screenImports = screens.flatMap(importsOf);
  for (const f of sliceFiles) assert.ok(screenImports.includes(`../${f}`), `${f} is not used by any screen`);
  const screenCode = screens.map(code).join("\n");
  for (const f of sliceFiles) for (const name of componentsIn(f)) assert.match(screenCode, new RegExp(`\\b${name}\\b`), `${name} is not used by any screen`);
});
