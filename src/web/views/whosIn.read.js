// Owner only (the owner key goes along): whos-in.
import { req } from "../lib/api.js";
import { record } from "../lib/loaded.js";

export const whosIn = (checkId, ownerKey) =>
  req("GET", `/checks/${encodeURIComponent(checkId)}/views/whos-in`, { ownerKey }).then((v) => record("whosIn", v));
