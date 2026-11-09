// How many are in, against the goal, and how the meter fills.
import { req } from "../lib/api.js";
import { record } from "../lib/loaded.js";
import { meter } from "../lib/format.js";

export const read = (checkId) => req("GET", `/checks/${encodeURIComponent(checkId)}/views/pitch-count`).then((v) => record("pitchCount", v));

/** The number in, against the goal, and the meter (view: pitchCount). */
export function PitchCount(v) {
  return `<div class="c" data-component="PitchCount"><div class="num">${v.count}<small>/${v.goal} ${v.open ? "in" : "were in"}</small></div>
      ${meter(v)}</div>`;
}
