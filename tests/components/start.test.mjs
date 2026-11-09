import { test } from "node:test";
import assert from "node:assert/strict";
import { thenComponent } from "./harness.mjs";
import { StartForm, LookPicker, MyChecksList } from "../../src/web/screens/start.parts.js";
import { Missing, TopBar } from "../../src/web/screens/page.parts.js";

const form = { earliestClosingDate: "2026-11-09", defaultGoal: 10 };
thenComponent("StartForm", "startForm", form, (v) => StartForm(v, { looks: "", mine: "" }),
  "Got an idea? Count heads first. The idea The pitch Goal Closes (optional) Start counting", "the form");
thenComponent("StartForm", "startForm", form, (v) => StartForm(v, {}), "Got an idea? Count heads first. The idea The pitch Goal Closes (optional) Start counting", "without slots");

test("StartForm takes its date limit and default goal from the view", () => {
  const html = StartForm({ earliestClosingDate: "2026-12-01", defaultGoal: 25 });
  assert.match(html, /name="goal"[^>]*value="25"/);
  assert.match(html, /name="closesOn" type="date" min="2026-12-01"/);
});

const looks = { choices: [{ theme: "classic", label: "Classic", picked: true }, { theme: "christmas", label: "Christmas", picked: false }] };
thenComponent("LookPicker", "lookChoices", looks, LookPicker, "The look Classic Christmas", "classic picked");
test("LookPicker presses the picked look only", () => {
  const html = LookPicker({ choices: [{ theme: "classic", label: "Classic", picked: false }, { theme: "christmas", label: "Christmas", picked: true }] });
  assert.match(html, /data-theme="classic" aria-pressed="false"/);
  assert.match(html, /data-theme="christmas" aria-pressed="true"/);
});

thenComponent("MyChecksList", "myInterestChecks", { checks: [
  { checkId: "a1", title: "Board games", count: 3, goal: 10, open: true, ownerLink: "./?c=a1&owner#k=K1" },
  { checkId: "b2", title: "Choir & co", count: 12, goal: 12, open: false, ownerLink: "./?c=b2&owner#k=K2" },
] }, MyChecksList, "Your interest checks Board games 3/10 Choir & co 12/12 · closed", "newest first, closed marked");
thenComponent("MyChecksList", "myInterestChecks", { checks: [] }, MyChecksList, "", "none: hidden");
test("MyChecksList rows link to the owner page and are keyed; an empty list is hidden", () => {
  const html = MyChecksList({ checks: [{ checkId: "a1", title: "T", count: 1, goal: 2, open: true, ownerLink: "./?c=a1&owner#k=K1" }] });
  assert.match(html, /data-key="a1" href="\.\/\?c=a1&amp;owner#k=K1"/);
  assert.match(MyChecksList({ checks: [] }), /\shidden>/);
});

test("Missing says Nothing here on a 404 and Can't reach it otherwise", () => {
  assert.match(Missing({ status: 404 }), /Nothing here/);
  assert.match(Missing(new Error("down")), /Can't reach it/);
});
test("TopBar is marked", () => assert.match(TopBar("Count me in"), /data-component="TopBar"/));
