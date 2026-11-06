// S8 Pitch Look: which look the pitch page and the owner page use. Classic until a theme is chosen, then the newest choice.
import { fold } from "../../lib/check.mjs";

export function pitchLook(history) {
  const c = fold(history);
  return { checkId: c.checkId, theme: c.theme };
}
