// S8 Change Closing Date: the owner moves the deadline, or removes it. A date that already passed can move (the check opens again); a check closed with "Close it" stays closed.
import { fold } from "../../lib/check.mjs";

export function changeClosingDate(history, input) {
  const c = fold(history);
  if (!c.started) return { rejected: "There is no such interest check.", kind: "NotFound" };
  if (input.ownerKey !== c.ownerKey) return { rejected: "Only the person who started it can change its date.", kind: "Invalid" };
  if (c.closed) return { rejected: "You closed this one.", kind: "Conflict" };
  if (input.closesOn && input.today && input.closesOn < input.today) return { rejected: "Pick a date from today on.", kind: "Invalid" };
  return { events: [{ type: "ClosingDateChanged", checkId: input.checkId, ...(input.closesOn ? { closesOn: input.closesOn } : {}) }] };
}
