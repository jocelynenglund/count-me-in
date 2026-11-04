// S5 How It's Going: the owner's numbers. The server shows it only to the owner link
// (ownerKey), so the view itself is just the count, the goal, the days and the names.
import { fold } from "../../lib/check.mjs";

export function howItSGoing(history) {
  const c = fold(history);
  const days = new Map();
  const tally = (day, key) => {
    if (!day) return;
    if (!days.has(day)) days.set(day, { day, in: 0, out: 0 });
    days.get(day)[key]++;
  };
  for (const e of history) {
    if (e.type === "InterestShown") tally(e.on, "in");
    else if (e.type === "InterestWithdrawn") tally(e.on, "out");
  }
  return {
    count: c.count,
    goal: c.goal,
    goalReached: c.count >= c.goal,
    byDay: [...days.values()].sort((a, b) => a.day.localeCompare(b.day)),
    people: [...c.people.values()].filter((p) => p.in).map((p) => p.name),
  };
}
