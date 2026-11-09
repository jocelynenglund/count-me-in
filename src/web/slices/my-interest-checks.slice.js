// The checks this device started, newest first. It sends the owner keys it holds; the server only lists those that match.
import { req } from "../lib/api.js";
import { record } from "../lib/loaded.js";
import { ownerLinks } from "../lib/device.js";
import { esc } from "../lib/format.js";

export async function read() {
  const links = ownerLinks();
  if (!links.length) return record("myInterestChecks", { checks: [] });
  const { checks } = await req("POST", "/views/my-interest-checks", { body: { links } });
  const keyOf = new Map(links.map((l) => [l.checkId, l.ownerKey]));
  return record("myInterestChecks", {
    checks: checks.map((c) => ({ ...c, ownerLink: `./?c=${encodeURIComponent(c.checkId)}&owner#k=${encodeURIComponent(keyOf.get(c.checkId))}` })),
  });
}

const MyCheckRow = (c) =>
  `<a class="minerow" data-component="MyCheckRow" data-key="${esc(c.checkId)}" href="${esc(c.ownerLink)}"><b>${esc(c.title)}</b><span>${c.count}/${c.goal}${c.open ? "" : " · closed"}</span></a>`;

/** The checks this device started, newest first; hidden when there are none (view: myInterestChecks). */
export function MyChecksList(v) {
  if (!v.checks.length) return `<section class="mine" id="mine" data-component="MyChecksList" hidden></section>`;
  return `<section class="mine" id="mine" data-component="MyChecksList"><div class="field"><span>Your interest checks</span></div>${v.checks.map(MyCheckRow).join("")}</section>`;
}
