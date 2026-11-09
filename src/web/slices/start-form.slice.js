// What the start form needs: the earliest closing date and the default goal.
import { req } from "../lib/api.js";
import { record } from "../lib/loaded.js";

export const read = () => req("GET", "/views/start-form").then((v) => record("startForm", v));
