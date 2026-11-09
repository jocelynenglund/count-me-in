// Owner only (the owner key goes along): closing-date.
import { req } from "../lib/api.js";
import { record } from "../lib/loaded.js";

export const closingDate = (checkId, ownerKey) =>
  req("GET", `/checks/${encodeURIComponent(checkId)}/views/closing-date`, { ownerKey }).then((v) => record("closingDate", v));
