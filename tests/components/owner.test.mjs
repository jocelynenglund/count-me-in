import { test } from "node:test";
import assert from "node:assert/strict";
import { thenComponent } from "./harness.mjs";
import { OwnerProgress } from "../../src/web/slices/owner-progress.slice.js";
import { SignupsChart } from "../../src/web/slices/signups-per-day.slice.js";
import { WhosInList } from "../../src/web/slices/whos-in.slice.js";
import { CopyEmailsButton } from "../../src/web/slices/reachable-people.slice.js";
import { ClosingDateField } from "../../src/web/slices/change-closing-date.slice.js";
import { CloseState } from "../../src/web/slices/close-state.slice.js";
import { CloseButton } from "../../src/web/slices/close-interest-check.slice.js";
import { LookPicker } from "../../src/web/slices/choose-theme.slice.js";
import { ShareBox } from "../../src/web/slices/share-links.slice.js";

const progress = (over) => ({ title: "Board-game club", count: 3, goal: 10, toGo: 7, status: "open", meterBoxes: 10, meterOn: 3, ...over });
thenComponent("OwnerProgress", "owner-progress", progress({ closesOn: "2026-11-12" }), OwnerProgress, "Board-game club 3 /10 7 to go closes 12 Nov", "open, with a date");
thenComponent("OwnerProgress", "owner-progress", progress({}), OwnerProgress, "Board-game club 3 /10 7 to go", "open");
thenComponent("OwnerProgress", "owner-progress", progress({ count: 10, toGo: 0, status: "reached", meterOn: 10 }), OwnerProgress, "Board-game club 10 /10 Goal reached", "reached");
thenComponent("OwnerProgress", "owner-progress", progress({ status: "closed" }), OwnerProgress, "Board-game club 3 /10 Closed", "closed");

const day = (n) => `2026-11-0${n}`;
const days = [0, 0, 1, 0, 2, 0, 4].map((n, i) => ({ day: day(i + 1), in: n }));
thenComponent("SignupsChart", "signups-per-day", { days }, SignupsChart,
  "Sign-ups per day 0 0 1 0 2 0 4 Sun Mon Tue Wed Thu Fri Today", "seven days");
thenComponent("SignupsChart", "signups-per-day", { days: [] }, SignupsChart, "", "nobody ever: no chart");
test("SignupsChart scales bars to the busiest day; empty days are thin", () => {
  const html = SignupsChart({ days });
  assert.match(html, /<b style="height:80%">/);
  assert.match(html, /<b style="height:20%">/);
  assert.match(html, /<b style="height:3px">/);
});

thenComponent("WhosInList", "whos-in", { people: [{ name: "Ann", leftEmail: true }, { name: "Bo", leftEmail: false }] }, WhosInList, "Ann @ Bo", "with and without email");
thenComponent("WhosInList", "whos-in", { people: [] }, WhosInList, "Nobody yet. Share the link.", "nobody");

thenComponent("CopyEmailsButton", "reachable-people", { people: [{ name: "Ann", email: "ann@example.com" }, { name: "Bo", email: "bo@example.org" }] }, CopyEmailsButton,
  "Copy 2 emails of people who are in and want to hear. Paste them in Bcc.", "two");
thenComponent("CopyEmailsButton", "reachable-people", { people: [{ name: "Ann", email: "ann@example.com" }] }, CopyEmailsButton,
  "Copy 1 email of people who are in and want to hear. Paste them in Bcc.", "one");
thenComponent("CopyEmailsButton", "reachable-people", { people: [] }, CopyEmailsButton, "", "no emails: nothing");

thenComponent("ClosingDateField", "closing-date", { closesOn: "2026-11-12", canChange: true, earliest: "2026-11-09" }, ClosingDateField,
  "Closes Save date No end date", "a date set");
thenComponent("ClosingDateField", "closing-date", { canChange: true, earliest: "2026-11-09" }, ClosingDateField, "Closes Save date", "no date yet");
thenComponent("ClosingDateField", "closing-date", { closesOn: "2026-11-12", canChange: false, earliest: "2026-11-09" }, ClosingDateField, "", "can't change: nothing");
test("ClosingDateField limits the date to the view's earliest and shows the current one", () => {
  const html = ClosingDateField({ closesOn: "2026-11-12", canChange: true, earliest: "2026-11-09" });
  assert.match(html, /id="closesOn" min="2026-11-09" value="2026-11-12"/);
});

const closeState = (v) => CloseState(v, { close: "[close]" });
thenComponent("CloseState", "close-state", { status: "open" }, closeState, "[close]", "open: the close control goes here");
thenComponent("CloseState", "close-state", { status: "closed-by-owner" }, closeState, "Closed", "closed by the owner");
thenComponent("CloseState", "close-state", { status: "closed-by-date" }, closeState, "Closed", "closed by its date");
thenComponent("CloseButton", "(none)", undefined, CloseButton, "Close it", "the button");

const looks = { choices: [{ theme: "classic", label: "Classic", picked: true }, { theme: "christmas", label: "Christmas", picked: false }] };
thenComponent("LookPicker", "check-look-choices", looks, LookPicker, "The look Classic Christmas", "classic picked");
test("LookPicker presses the picked look only", () => {
  const html = LookPicker({ choices: [{ theme: "classic", label: "Classic", picked: false }, { theme: "christmas", label: "Christmas", picked: true }] });
  assert.match(html, /data-theme="classic" aria-pressed="false"/);
  assert.match(html, /data-theme="christmas" aria-pressed="true"/);
});

const links = { share: "https://countmein.itsybit.se/?c=a1", ownerLink: "https://countmein.itsybit.se/?c=a1&owner#k=K", isNew: false };
thenComponent("ShareBox", "share-links", links, ShareBox, "countmein.itsybit.se/?c=a1 Copy link", "the link");
thenComponent("ShareBox", "share-links", { ...links, isNew: true }, ShareBox,
  "Keep this page's link. It's your key to see who's in and to close it. Copy my private link countmein.itsybit.se/?c=a1 Copy link", "first visit: keep your link");
test("ShareBox copies the right links", () => {
  const html = ShareBox({ ...links, isNew: true });
  assert.match(html, /data-copy="https:\/\/countmein\.itsybit\.se\/\?c=a1&amp;owner#k=K"/);
  assert.match(html, /data-copy="https:\/\/countmein\.itsybit\.se\/\?c=a1"/);
});
