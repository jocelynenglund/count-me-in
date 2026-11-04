// S4 Change My Mind: take your interest back.
import { fold } from "../../lib/check.mjs";

export function changeMyMind(history, input) {
  const c = fold(history);
  if (!c.started) return { rejected: "No such interest check", kind: "NotFound" };
  if (!c.people.get(input.personId)?.in) return { rejected: "You're not counted in", kind: "Conflict" };
  return { events: [{ type: "InterestWithdrawn", checkId: input.checkId, personId: input.personId, on: input.on }] };
}
