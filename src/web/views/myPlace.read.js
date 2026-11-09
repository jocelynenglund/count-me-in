// Where this device stands on a check: can join, already in, or it's closed. Adds the name to prefill the form.
import { req } from "../lib/api.js";
import { record } from "../lib/loaded.js";
import { personId, nameFor } from "../lib/device.js";

export const myPlace = (checkId) =>
  req("GET", `/checks/${encodeURIComponent(checkId)}/views/my-place`, { personId: personId() })
    .then((v) => record("myPlace", { ...v, suggestedName: nameFor(checkId) }));
