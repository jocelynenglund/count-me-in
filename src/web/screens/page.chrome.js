// Screen chrome: what every page shares and no slice owns. These take no view.
import { topBar } from "../lib/format.js";

/** The header with the app's name; the owner page also says "Private". */
export const TopBar = (left, right = "") => topBar(left, right);

/** Nothing to show: a link that leads nowhere (404), or the API not answering. Only the error. */
export function Missing(err) {
  const nothing = err?.status === 404;
  return `<div class="c" data-component="Missing">${topBar("Count me in")}
    <main class="body">
      <div class="stripes"></div>
      <h1 class="big">${nothing ? "Nothing here" : "Can't reach it"}</h1>
      <p class="pitch">${nothing ? "This link doesn't lead to an interest check." : "Couldn't load it just now. Try again in a moment."}</p>
      <div class="grow"></div>
      <a class="ghost" href="./">Start your own</a>
    </main></div>`;
}
