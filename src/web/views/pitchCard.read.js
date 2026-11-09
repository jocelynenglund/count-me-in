// What the pitch page says about the idea: title, pitch, status and closing date.
import { req } from "../lib/api.js";
import { record } from "../lib/loaded.js";

export const pitchCard = (checkId) => req("GET", `/checks/${encodeURIComponent(checkId)}/views/pitch-card`).then((v) => record("pitchCard", v));
