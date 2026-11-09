// Count me in: three screens on one page. This file only routes.
//   /                       start an interest check
//   /?c=<id>                the pitch (public)
//   /?c=<id>&owner#k=<key>  how it's going (owner; the key stays in the fragment)
// The page decides nothing: each screen fetches the API's views, draws them through its
// components and sends commands. See screens/*.screen.js.

import { adoptHandover } from "./lib/device.js";
import { loadedViews } from "./lib/loaded.js";
import { showStart } from "./screens/start.screen.js";
import { showPitch } from "./screens/pitch.screen.js";
import { showOwner } from "./screens/owner.screen.js";

adoptHandover(); // arriving from the old prototype.itsybit.se address

// What the debug pill shows (?debug): the views this page last loaded.
window.LOG = { events: async () => loadedViews() };

const params = new URLSearchParams(location.search);
const checkId = params.get("c");
if (!checkId) showStart();
else if (params.has("owner")) showOwner(checkId);
else showPitch(checkId);
