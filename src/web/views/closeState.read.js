// Owner only (the owner key goes along): close-state.
import { req } from "../lib/api.js";
import { record } from "../lib/loaded.js";

export const closeState = (checkId, ownerKey) =>
  req("GET", `/checks/${encodeURIComponent(checkId)}/views/close-state`, { ownerKey }).then((v) => record("closeState", v));
