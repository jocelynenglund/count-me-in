// Owner only (the owner key goes along): reachable-people.
import { req } from "../lib/api.js";
import { record } from "../lib/loaded.js";
import { plural, emailList } from "../lib/format.js";

export const read = (checkId, ownerKey) =>
  req("GET", `/checks/${encodeURIComponent(checkId)}/views/reachable-people`, { ownerKey }).then((v) => record("reachablePeople", v));

/** "Copy N emails" of the people who are in and want to hear; nothing when there are none (view: reachablePeople). */
export function CopyEmailsButton(v) {
  if (!v.people.length) return `<div class="c" data-component="CopyEmailsButton"></div>`;
  return `<div class="mailrow" data-component="CopyEmailsButton"><button class="look" type="button" id="copyEmails">Copy ${plural(v.people.length, "email")}</button><span>of people who are in and want to hear. Paste them in Bcc.</span></div>`;
}

/** The only thing this button does: put the addresses on the clipboard (not a command). */
export function wire(root, { people }) {
  root.querySelector("#copyEmails")?.addEventListener("click", async (e) => {
    const list = emailList(people);
    try { await navigator.clipboard.writeText(list); e.target.textContent = "Copied"; } catch { prompt("Copy these addresses", list); }
  });
}
