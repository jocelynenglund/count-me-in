// Change Closing Date. Written by hand from "Count me in · plan" (S8, chapter "Watch it grow"), in the shape Event Book generates.
// Pending while ./change-closing-date.slice.mjs throws NotBuiltYet; run it red with GWT_RUN_PENDING=1.
import { chapter, scenario } from "@jocelynenglund/itsybit-gwt";
import { changeClosingDate } from "./change-closing-date.slice.mjs";

chapter("Watch it grow", () => {
  scenario("The owner moves the deadline")
    .given(["InterestCheckStarted", { "checkId": "c1", "ownerKey": "k1", "closesOn": "2026-11-28" }])
    .when("Change Closing Date", { "checkId": "c1", "ownerKey": "k1", "closesOn": "2026-11-30", "today": "2026-11-20" }, changeClosingDate)
    .then(["ClosingDateChanged", { "checkId": "c1", "closesOn": "2026-11-30" }]);

  scenario("A deadline that passed can still move, and the check opens again")
    .given(["InterestCheckStarted", { "checkId": "c1", "ownerKey": "k1", "closesOn": "2026-11-28" }])
    .when("Change Closing Date", { "checkId": "c1", "ownerKey": "k1", "closesOn": "2026-12-05", "today": "2026-11-29" }, changeClosingDate)
    .then(["ClosingDateChanged", { "checkId": "c1", "closesOn": "2026-12-05" }]);

  scenario("No deadline at all")
    .given(["InterestCheckStarted", { "checkId": "c1", "ownerKey": "k1", "closesOn": "2026-11-28" }])
    .when("Change Closing Date", { "checkId": "c1", "ownerKey": "k1", "today": "2026-11-20" }, changeClosingDate)
    .then(["ClosingDateChanged", { "checkId": "c1" }]);

  scenario("Only the owner can change the date")
    .given(["InterestCheckStarted", { "checkId": "c1", "ownerKey": "k1" }])
    .when("Change Closing Date", { "checkId": "c1", "ownerKey": "someone-else", "closesOn": "2026-11-30", "today": "2026-11-20" }, changeClosingDate)
    .thenNothing("Only the person who started it can change its date.");

  scenario("Not a date in the past")
    .given(["InterestCheckStarted", { "checkId": "c1", "ownerKey": "k1" }])
    .when("Change Closing Date", { "checkId": "c1", "ownerKey": "k1", "closesOn": "2026-11-19", "today": "2026-11-20" }, changeClosingDate)
    .thenNothing("Pick a date from today on.");

  scenario("A check you closed stays closed")
    .given(["InterestCheckStarted", { "checkId": "c1", "ownerKey": "k1" }], ["InterestCheckClosed", { "checkId": "c1" }])
    .when("Change Closing Date", { "checkId": "c1", "ownerKey": "k1", "closesOn": "2026-11-30", "today": "2026-11-20" }, changeClosingDate)
    .thenNothing("You closed this one.");
});
