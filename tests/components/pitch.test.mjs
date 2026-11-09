import { test } from "node:test";
import assert from "node:assert/strict";
import { thenComponent } from "./harness.mjs";
import { PitchCard, PitchCount, MyPlace } from "../../src/web/screens/pitch.parts.js";
import { PageTheme } from "../../src/web/screens/page.parts.js";

const slots = { count: "[count]", place: "[place]" };
const card = (over) => ({ title: "Board-game club", pitch: "Saturdays at the library", status: "open", closedByDate: false, ...over });

thenComponent("PitchCard", "pitchCard", card({ closesOn: "2026-11-12" }), (v) => PitchCard(v, slots),
  "Count me in Till 12 Nov Board-game club Saturdays at the library [count] [place]", "open, with a closing date");
thenComponent("PitchCard", "pitchCard", card({}), (v) => PitchCard(v, slots),
  "Count me in Board-game club Saturdays at the library [count] [place]", "open, no date");
thenComponent("PitchCard", "pitchCard", card({ status: "reached", closesOn: "2026-11-12" }), (v) => PitchCard(v, slots),
  "Count me in Till 12 Nov Board-game club It's on ★ It's on ★ It's on ★ It's on ★ It's on ★ It's on [count] Goal reached. Names still welcome until 12 Nov. [place]", "goal reached");
thenComponent("PitchCard", "pitchCard", card({ status: "reached" }), (v) => PitchCard(v, slots),
  "Count me in Board-game club It's on ★ It's on ★ It's on ★ It's on ★ It's on ★ It's on [count] Goal reached. Names still welcome. [place]", "goal reached, no date");
thenComponent("PitchCard", "pitchCard", card({ status: "closed", closesOn: "2026-11-05", closedByDate: true }), (v) => PitchCard(v, slots),
  "Count me in Closed 5 Nov Board-game club Closed Saturdays at the library [count] They've stopped taking names. If you were in and left an email, you'll hear from them. [place] Start your own", "closed by its date");
thenComponent("PitchCard", "pitchCard", card({ status: "closed" }), (v) => PitchCard(v, slots),
  "Count me in Closed Board-game club Closed Saturdays at the library [count] They've stopped taking names. If you were in and left an email, you'll hear from them. [place] Start your own", "closed by the owner");

test("PitchCard escapes the title and makes links in the pitch clickable", () => {
  const html = PitchCard(card({ title: "<b>x</b>", pitch: "See adventvibes.itsybit.se." }), slots);
  assert.match(html, /&lt;b&gt;x&lt;\/b&gt;/);
  assert.match(html, /<a href="https:\/\/adventvibes\.itsybit\.se" target="_blank"/);
});

thenComponent("PitchCount", "pitchCount", { count: 3, goal: 10, open: true, meterBoxes: 10, meterOn: 3 }, PitchCount, "3 /10 in", "open");
thenComponent("PitchCount", "pitchCount", { count: 12, goal: 12, open: false, meterBoxes: 10, meterOn: 10 }, PitchCount, "12 /12 were in", "closed");
test("PitchCount draws the meter the view describes", () => {
  const html = PitchCount({ count: 3, goal: 10, open: true, meterBoxes: 10, meterOn: 3 });
  assert.equal(html.match(/<b class=on>/g).length, 3);
  assert.match(html, /--n:10/);
  assert.match(html, /aria-label="3 of 10"/);
});

thenComponent("MyPlace", "myPlace", { status: "can-join", suggestedName: "Ann" }, MyPlace,
  "Your name Email (optional, to hear if it's on) Count me in", "can join");
test("MyPlace prefills the suggested name", () => assert.match(MyPlace({ status: "can-join", suggestedName: 'A"nn' }), /name="name" maxlength="60" value="A&quot;nn"/));
thenComponent("MyPlace", "myPlace", { status: "in", name: "Ann" }, MyPlace, "You're in, Ann Changed your mind? Take me off", "in");
thenComponent("MyPlace", "myPlace", { status: "in" }, MyPlace, "You're in Changed your mind? Take me off", "in, no name");
thenComponent("MyPlace", "myPlace", { status: "closed" }, MyPlace, "", "closed: nothing");

test("PageTheme is the theme the view names", () => assert.equal(PageTheme({ checkId: "a1", theme: "christmas" }), "christmas"));
