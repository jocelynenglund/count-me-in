// S1 Start Interest Check: the idea, its goal and (optionally) a closing date.
// Pure: the ids, the owner key and today come in with the command.

export function startInterestCheck(history, input) {
  const title = (input.title ?? "").trim();
  if (!title) return { rejected: "What's the idea called?" };
  const goal = Number(input.goal);
  if (!Number.isInteger(goal) || goal < 1) return { rejected: "How many people would make it worth doing? At least 1." };
  if (input.closesOn && input.today && input.closesOn < input.today) return { rejected: "The closing date has already passed" };
  if (history.some((e) => e.type === "InterestCheckStarted")) return { rejected: "This interest check already exists", kind: "Conflict" };
  return {
    events: [{
      type: "InterestCheckStarted",
      checkId: input.checkId,
      title,
      pitch: (input.pitch ?? "").trim(),
      goal,
      ...(input.closesOn ? { closesOn: input.closesOn } : {}),
      ownerKey: input.ownerKey,
    }],
  };
}
