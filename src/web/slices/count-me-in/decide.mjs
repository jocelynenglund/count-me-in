// S3 Count Me In. personId comes from the device, so a second tap isn't a second person.
import { fold } from "../../lib/check.mjs";

export function countMeIn(history, input) {
  const c = fold(history);
  if (!c.started) return { rejected: "No such interest check", kind: "NotFound" };
  if (c.closed) return { rejected: "This interest check has closed", kind: "Conflict" };
  const name = (input.name ?? "").trim();
  if (!name) return { rejected: "What's your name?" };
  if (c.people.get(input.personId)?.in) return { rejected: "You're already counted in", kind: "Conflict" };
  const email = (input.email ?? "").trim();
  return {
    events: [{ type: "InterestShown", checkId: input.checkId, personId: input.personId, name, ...(email ? { email } : {}), on: input.on }],
  };
}
