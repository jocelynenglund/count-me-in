// The checks this device started, newest first. It sends the owner keys it holds; the server only lists those that match.
import { req } from "../lib/api.js";
import { record } from "../lib/loaded.js";
import { ownerLinks } from "../lib/device.js";

export async function myInterestChecks() {
  const links = ownerLinks();
  if (!links.length) return record("myInterestChecks", { checks: [] });
  const { checks } = await req("POST", "/views/my-interest-checks", { body: { links } });
  const keyOf = new Map(links.map((l) => [l.checkId, l.ownerKey]));
  return record("myInterestChecks", {
    checks: checks.map((c) => ({ ...c, ownerLink: `./?c=${encodeURIComponent(c.checkId)}&owner#k=${encodeURIComponent(keyOf.get(c.checkId))}` })),
  });
}
