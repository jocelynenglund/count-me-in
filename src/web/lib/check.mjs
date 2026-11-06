// One interest check's events, folded into what the slices need. The store keeps
// one stream per check, so every event here belongs to it.

export function fold(history) {
  const check = { started: false, closed: false, theme: "classic", people: new Map() };
  for (const e of history) {
    if (e.type === "InterestCheckStarted") {
      Object.assign(check, { started: true, checkId: e.checkId, title: e.title, pitch: e.pitch ?? "", goal: e.goal, closesOn: e.closesOn, ownerKey: e.ownerKey });
    } else if (e.type === "InterestShown") {
      check.people.set(e.personId, { name: e.name, in: true });
    } else if (e.type === "InterestWithdrawn") {
      const p = check.people.get(e.personId);
      if (p) p.in = false;
    } else if (e.type === "InterestCheckClosed") {
      check.closed = true;
    } else if (e.type === "ThemeChosen") {
      check.theme = e.theme;
    }
  }
  check.count = [...check.people.values()].filter((p) => p.in).length;
  return check;
}
