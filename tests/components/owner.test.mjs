import { test } from "node:test";
import assert from "node:assert/strict";
import { thenComponent } from "./harness.mjs";
import { OwnerProgress, SignupsChart, WhosInList, CopyEmailsButton, ClosingDateField, CloseButton, ShareBox } from "../../src/web/screens/owner.parts.js";

const progress = (over) => ({ title: "Board-game club", count: 3, goal: 10, toGo: 7, status: "open", meterBoxes: 10, meterOn: 3, ...over });
thenComponent("OwnerProgress", "ownerProgress", progress({ closesOn: "2026-11-12" }), OwnerProgress, "Board-game club 3 /10 7 to go closes 12 Nov", "open, with a date");
thenComponent("OwnerProgress", "ownerProgress", progress({}), OwnerProgress, "Board-game club 3 /10 7 to go", "open");
thenComponent("OwnerProgress", "ownerProgress", progress({ count: 10, toGo: 0, status: "reached", meterOn: 10 }), OwnerProgress, "Board-game club 10 /10 Goal reached", "reached");
thenComponent("OwnerProgress", "ownerProgress", progress({ status: "closed" }), OwnerProgress, "Board-game club 3 /10 Closed", "closed");

const day = (n) => `2026-11-0${n}`;
const days = [0, 0, 1, 0, 2, 0, 4].map((n, i) => ({ day: day(i + 1), in: n }));
thenComponent("SignupsChart", "signupsPerDay", { days }, SignupsChart,
  "Sign-ups per day 0 0 1 0 2 0 4 Sun Mon Tue Wed Thu Fri Today", "seven days");
thenComponent("SignupsChart", "signupsPerDay", { days: [] }, SignupsChart, "", "nobody ever: no chart");
test("SignupsChart scales bars to the busiest day; empty days are thin", () => {
  const html = SignupsChart({ days });
  assert.match(html, /<b style="height:80%">/);
  assert.match(html, /<b style="height:20%">/);
  assert.match(html, /<b style="height:3px">/);
});

thenComponent("WhosInList", "whosIn", { people: [{ name: "Ann", leftEmail: true }, { name: "Bo", leftEmail: false }] }, WhosInList, "Ann @ Bo", "with and without email");
thenComponent("WhosInList", "whosIn", { people: [] }, WhosInList, "Nobody yet. Share the link.", "nobody");

thenComponent("CopyEmailsButton", "reachablePeople", { people: [{ name: "Ann", email: "ann@x.se" }, { name: "Bo", email: "bo@x.se" }] }, CopyEmailsButton,
  "Copy 2 emails of people who are in and want to hear. Paste them in Bcc.", "two");
thenComponent("CopyEmailsButton", "reachablePeople", { people: [{ name: "Ann", email: "ann@x.se" }] }, CopyEmailsButton,
  "Copy 1 email of people who are in and want to hear. Paste them in Bcc.", "one");
thenComponent("CopyEmailsButton", "reachablePeople", { people: [] }, CopyEmailsButton, "", "no emails: nothing");

thenComponent("ClosingDateField", "closingDate", { closesOn: "2026-11-12", canChange: true, earliest: "2026-11-09" }, ClosingDateField,
  "Closes Save date No end date", "a date set");
thenComponent("ClosingDateField", "closingDate", { canChange: true, earliest: "2026-11-09" }, ClosingDateField, "Closes Save date", "no date yet");
thenComponent("ClosingDateField", "closingDate", { closesOn: "2026-11-12", canChange: false, earliest: "2026-11-09" }, ClosingDateField, "", "can't change: nothing");
test("ClosingDateField limits the date to the view's earliest and shows the current one", () => {
  const html = ClosingDateField({ closesOn: "2026-11-12", canChange: true, earliest: "2026-11-09" });
  assert.match(html, /id="closesOn" min="2026-11-09" value="2026-11-12"/);
});

thenComponent("CloseButton", "closeState", { status: "open" }, CloseButton, "Close it", "open");
thenComponent("CloseButton", "closeState", { status: "closed-by-owner" }, CloseButton, "Closed", "closed by the owner");
thenComponent("CloseButton", "closeState", { status: "closed-by-date" }, CloseButton, "Closed", "closed by its date");

const links = { share: "https://countmein.itsybit.se/?c=a1", ownerLink: "https://countmein.itsybit.se/?c=a1&owner#k=K", isNew: false };
thenComponent("ShareBox", "shareLinks", links, ShareBox, "countmein.itsybit.se/?c=a1 Copy link", "the link");
thenComponent("ShareBox", "shareLinks", { ...links, isNew: true }, ShareBox,
  "Keep this page's link. It's your key to see who's in and to close it. Copy my private link countmein.itsybit.se/?c=a1 Copy link", "first visit: keep your link");
test("ShareBox copies the right links", () => {
  const html = ShareBox({ ...links, isNew: true });
  assert.match(html, /data-copy="https:\/\/countmein\.itsybit\.se\/\?c=a1&amp;owner#k=K"/);
  assert.match(html, /data-copy="https:\/\/countmein\.itsybit\.se\/\?c=a1"/);
});
