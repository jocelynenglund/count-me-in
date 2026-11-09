// Which look the pitch and owner pages wear.
import { req } from "../lib/api.js";
import { record } from "../lib/loaded.js";

export const pitchLook = (checkId) => req("GET", `/checks/${encodeURIComponent(checkId)}/views/pitch-look`).then((v) => record("pitchLook", v));
