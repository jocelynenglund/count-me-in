// Which looks can be picked on the start page (no check yet), and which one is picked.
import { req } from "../lib/api.js";
import { record } from "../lib/loaded.js";

export const read = () => req("GET", "/views/look-choices").then((v) => record("lookChoices", v));
