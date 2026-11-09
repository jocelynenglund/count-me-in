// Which look the pitch and owner pages wear.
import { req } from "../lib/api.js";
import { record } from "../lib/loaded.js";

export const read = (checkId) => req("GET", `/checks/${encodeURIComponent(checkId)}/views/pitch-look`).then((v) => record("pitchLook", v));

/** The look the page wears (view: pitchLook): the theme name the screen puts on the body. */
export const PageTheme = (v) => v.theme;
