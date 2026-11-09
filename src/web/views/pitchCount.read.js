// How many are in, against the goal, and how the meter fills.
import { req } from "../lib/api.js";
import { record } from "../lib/loaded.js";

export const pitchCount = (checkId) => req("GET", `/checks/${encodeURIComponent(checkId)}/views/pitch-count`).then((v) => record("pitchCount", v));
