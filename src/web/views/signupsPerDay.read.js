// Owner only (the owner key goes along): signups-per-day.
import { req } from "../lib/api.js";
import { record } from "../lib/loaded.js";

export const signupsPerDay = (checkId, ownerKey) =>
  req("GET", `/checks/${encodeURIComponent(checkId)}/views/signups-per-day`, { ownerKey }).then((v) => record("signupsPerDay", v));
