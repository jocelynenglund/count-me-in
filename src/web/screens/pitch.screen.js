// /?c=<id> : the pitch (public), counting in and changing your mind.
import { pitchCard } from "../views/pitchCard.read.js";
import { pitchCount } from "../views/pitchCount.read.js";
import { myPlace } from "../views/myPlace.read.js";
import { pitchLook } from "../views/pitchLook.read.js";
import { countMeIn, changeMyMind } from "../lib/commands.js";
import { PitchCard, PitchCount, MyPlace } from "./pitch.parts.js";
import { PageTheme } from "./page.parts.js";
import { show, showMissing, showError, $ } from "./page.screen.js";

export async function showPitch(checkId) {
  let card, count, place, look;
  try { [card, count, place, look] = await Promise.all([pitchCard(checkId), pitchCount(checkId), myPlace(checkId), pitchLook(checkId)]); }
  catch (err) { return showMissing(err); }

  show(card.status, PitchCard(card, { count: PitchCount(count), place: MyPlace(place) }), PageTheme(look));

  $("#in")?.addEventListener("submit", async (e) => {
    e.preventDefault();
    const f = new FormData(e.target);
    try { await countMeIn(checkId, { name: String(f.get("name") ?? "").trim(), email: String(f.get("email") ?? "").trim() }); showPitch(checkId); }
    catch (err) { showError(err.message); }
  });
  $("#out")?.addEventListener("click", async () => {
    try { await changeMyMind(checkId); showPitch(checkId); }
    catch (err) { showError(err.message); }
  });
}
