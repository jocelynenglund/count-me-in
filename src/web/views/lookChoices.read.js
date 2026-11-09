// Which looks can be picked, and which one is picked. With a check id: that check's look; without: the start page's.
import { req } from "../lib/api.js";
import { record } from "../lib/loaded.js";

export const lookChoices = (checkId) =>
  req("GET", checkId ? `/checks/${encodeURIComponent(checkId)}/views/look-choices` : "/views/look-choices").then((v) => record("lookChoices", v));
