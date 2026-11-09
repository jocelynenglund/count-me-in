// Which looks can be picked for a check, and which one is picked.
import { req } from "../lib/api.js";
import { record } from "../lib/loaded.js";

export const read = (checkId) =>
  req("GET", `/checks/${encodeURIComponent(checkId)}/views/look-choices`).then((v) => record("checkLookChoices", v));
