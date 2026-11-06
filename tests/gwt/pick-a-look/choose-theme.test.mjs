// Choose Theme. Written by hand from "Count me in · plan" (S7, chapter "Pick a look"; built on "just build it" before Refined), in the shape Event Book generates.
// Pending while ./choose-theme.slice.mjs throws NotBuiltYet; run it red with GWT_RUN_PENDING=1.
import { chapter, scenario } from "@jocelynenglund/itsybit-gwt";
import { chooseTheme } from "./choose-theme.slice.mjs";

chapter("Pick a look", () => {
  scenario("The owner turns it Christmas")
    .given(["InterestCheckStarted", { "checkId": "c1", "ownerKey": "k1", "title": "Advent of Vibes", "goal": 24 }])
    .when("Choose Theme", { "checkId": "c1", "ownerKey": "k1", "theme": "christmas" }, chooseTheme)
    .then(["ThemeChosen", { "checkId": "c1", "theme": "christmas" }]);

  scenario("Only the owner can change the look")
    .given(["InterestCheckStarted", { "checkId": "c1", "ownerKey": "k1" }])
    .when("Choose Theme", { "checkId": "c1", "ownerKey": "someone-else", "theme": "christmas" }, chooseTheme)
    .thenNothing("Only the person who started it can change its look.");

  scenario("Only the themes there are")
    .given(["InterestCheckStarted", { "checkId": "c1", "ownerKey": "k1" }])
    .when("Choose Theme", { "checkId": "c1", "ownerKey": "k1", "theme": "neon" }, chooseTheme)
    .thenNothing("Pick one of the themes.");

  scenario("No such check")
    .when("Choose Theme", { "checkId": "nope", "ownerKey": "k1", "theme": "christmas" }, chooseTheme)
    .thenNothing("There is no such interest check.");
});
