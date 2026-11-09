// Owner only (the owner key goes along): owner-progress.
import { req } from "../lib/api.js";
import { record } from "../lib/loaded.js";

export const ownerProgress = (checkId, ownerKey) =>
  req("GET", `/checks/${encodeURIComponent(checkId)}/views/owner-progress`, { ownerKey }).then((v) => record("ownerProgress", v));
