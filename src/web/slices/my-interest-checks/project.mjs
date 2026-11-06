// S2 My Interest Checks: the checks started on this device, newest first. The page gathers their owner logs (it keeps each owner link); this view only counts.
import { fold } from "../../lib/check.mjs";

export function myInterestChecks(history) {
  const byCheck = new Map(); // in order of starting
  for (const e of history) {
    if (e.type === "InterestCheckStarted") byCheck.set(e.checkId, []);
    byCheck.get(e.checkId)?.push(e);
  }
  const checks = [...byCheck.entries()].reverse().map(([checkId, events]) => {
    const c = fold(events);
    return { checkId, title: c.title, count: c.count, goal: c.goal, open: !c.closed };
  });
  return { checks };
}
