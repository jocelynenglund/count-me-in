// S2 Pitch: what visitors see. "open" is the owner's Close; a closing date that has
// passed is the page's job (the view has no clock), so closesOn is passed through.
import { fold } from "../../lib/check.mjs";

export function pitch(history) {
  const c = fold(history);
  return {
    title: c.title,
    pitch: c.pitch,
    goal: c.goal,
    count: c.count,
    goalReached: c.count >= c.goal,
    open: !c.closed,
    ...(c.closesOn ? { closesOn: c.closesOn } : {}),
  };
}
