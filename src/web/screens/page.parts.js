// What every page shares: the header, the look it wears, and the "can't show it" page.
import { topBar } from "../lib/format.js";

/** The header with the app's name; the owner page also says "Private". No view. */
export const TopBar = (left, right = "") => topBar(left, right);

/** The look the page wears (view: pitchLook): the theme name the screen puts on the body. */
export const PageTheme = (v) => v.theme;

/** Nothing to show: a link that leads nowhere (404), or the API not answering. No view, only the error. */
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
