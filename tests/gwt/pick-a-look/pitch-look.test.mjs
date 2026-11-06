// Pitch Look. Written by hand from "Count me in · plan" (S8, chapter "Pick a look"), in the shape Event Book generates.
// Pending while ./pitch-look.slice.mjs throws NotBuiltYet; run it red with GWT_RUN_PENDING=1.
import { chapter, scenario } from "@jocelynenglund/itsybit-gwt";
import { pitchLook } from "./pitch-look.slice.mjs";

chapter("Pick a look", () => {
  scenario("Classic until a theme is chosen")
    .given(["InterestCheckStarted", { "checkId": "c1" }])
    .thenView("Pitch Look", pitchLook, { "checkId": "c1", "theme": "classic" });

  scenario("Christmas once chosen")
    .given(["InterestCheckStarted", { "checkId": "c1" }], ["ThemeChosen", { "checkId": "c1", "theme": "christmas" }])
    .thenView("Pitch Look", pitchLook, { "checkId": "c1", "theme": "christmas" });

  scenario("The newest choice wins")
    .given(["InterestCheckStarted", { "checkId": "c1" }], ["ThemeChosen", { "checkId": "c1", "theme": "christmas" }], ["ThemeChosen", { "checkId": "c1", "theme": "classic" }])
    .thenView("Pitch Look", pitchLook, { "checkId": "c1", "theme": "classic" });
});
