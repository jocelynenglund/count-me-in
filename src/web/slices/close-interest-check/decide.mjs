// S6 Close Interest Check: the owner stops new sign-ups.
import { fold } from "../../lib/check.mjs";

export function closeInterestCheck(history, input) {
  const c = fold(history);
  if (!c.started) return { rejected: "No such interest check", kind: "NotFound" };
  if (input.ownerKey !== c.ownerKey) return { rejected: "Only the owner can close this", kind: "Invalid" };
  if (c.closed) return { rejected: "It's already closed", kind: "Conflict" };
  return { events: [{ type: "InterestCheckClosed", checkId: input.checkId, on: input.on }] };
}
