// /?c=<id> : the pitch (public), counting in and changing your mind.
export const screen = "Pitch page";
import { read as readPitchCard, PitchCard } from "../slices/pitch-card.slice.js";
import { read as readPitchCount, PitchCount } from "../slices/pitch-count.slice.js";
import { read as readMyPlace, MyPlace } from "../slices/my-place.slice.js";
import { read as readPitchLook, PageTheme } from "../slices/pitch-look.slice.js";
import { CountMeInForm, wire as wireCountMeIn } from "../slices/count-me-in.slice.js";
import { TakeMeOffButton, wire as wireChangeMyMind } from "../slices/change-my-mind.slice.js";
import { show, showMissing, showError, root } from "./page.screen.js";

export async function showPitch(checkId) {
  let card, count, place, look;
  try { [card, count, place, look] = await Promise.all([readPitchCard(checkId), readPitchCount(checkId), readMyPlace(checkId), readPitchLook(checkId)]); }
  catch (err) { return showMissing(err); }

  const place$ = MyPlace(place, { join: CountMeInForm({ name: place.suggestedName }), takeMeOff: TakeMeOffButton() });
  show(card.status, PitchCard(card, { count: PitchCount(count), place: place$ }), PageTheme(look));

  const again = { checkId, onDone: () => showPitch(checkId), onError: showError };
  wireCountMeIn(root(), again);
  wireChangeMyMind(root(), again);
}
