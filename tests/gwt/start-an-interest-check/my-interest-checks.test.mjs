// My Interest Checks. Written by hand from "Count me in · plan" (S2, chapter "Start an interest check"), in the shape Event Book generates.
// Pending while ./my-interest-checks.slice.mjs throws NotBuiltYet; run it red with GWT_RUN_PENDING=1.
import { chapter, scenario } from "@jocelynenglund/itsybit-gwt";
import { myInterestChecks } from "./my-interest-checks.slice.mjs";

chapter("Start an interest check", () => {
  scenario("Your checks, newest first")
    .given(["InterestCheckStarted", { "checkId": "c1", "title": "Board games", "goal": 10 }], ["InterestShown", { "checkId": "c1", "personId": "p1", "name": "Ana" }], ["InterestCheckClosed", { "checkId": "c1" }], ["InterestCheckStarted", { "checkId": "c2", "title": "Advent of Vibes", "goal": 24 }], ["InterestShown", { "checkId": "c2", "personId": "p2", "name": "Ben" }], ["InterestShown", { "checkId": "c2", "personId": "p3", "name": "Dev" }])
    .thenView("My Interest Checks", myInterestChecks, { "checks": [ { "checkId": "c2", "title": "Advent of Vibes", "count": 2, "goal": 24, "open": true }, { "checkId": "c1", "title": "Board games", "count": 1, "goal": 10, "open": false } ] });

  scenario("None started here yet")
    .thenView("My Interest Checks", myInterestChecks, { "checks": [] });
});
