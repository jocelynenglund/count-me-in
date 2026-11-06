// S7 Choose Theme: the owner picks how the pitch looks, at the start or any time later.
import { fold } from "../../lib/check.mjs";
import { THEMES } from "../../lib/themes.mjs";

export function chooseTheme(history, input) {
  const c = fold(history);
  if (!c.started) return { rejected: "There is no such interest check.", kind: "NotFound" };
  if (input.ownerKey !== c.ownerKey) return { rejected: "Only the person who started it can change its look.", kind: "Invalid" };
  if (!Object.hasOwn(THEMES, input.theme)) return { rejected: "Pick one of the themes.", kind: "Invalid" };
  return { events: [{ type: "ThemeChosen", checkId: input.checkId, theme: input.theme }] };
}
