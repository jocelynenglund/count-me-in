// Owner only (the owner key goes along): reachable-people.
import { req } from "../lib/api.js";
import { record } from "../lib/loaded.js";

export const reachablePeople = (checkId, ownerKey) =>
  req("GET", `/checks/${encodeURIComponent(checkId)}/views/reachable-people`, { ownerKey }).then((v) => record("reachablePeople", v));
