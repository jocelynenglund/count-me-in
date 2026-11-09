// What this device remembers (localStorage): who it is, the owner keys of the checks it started,
// the name to prefill. Nothing about the checks themselves.
import { newId } from "./api.js";

const get = (k) => { try { return localStorage.getItem(k); } catch { return null; } };
const set = (k, v) => { try { localStorage.setItem(k, v); } catch {} };

/** This device's id; made once. */
export function personId() {
  let id = get("cmi_person");
  if (!id) { id = newId(16); set("cmi_person", id); }
  return id;
}

/** Arriving from the old prototype.itsybit.se address: it hands over this device's id (#p=). */
export function adoptHandover() {
  const p = new URLSearchParams(location.hash.slice(1)).get("p");
  if (p && /^[a-z0-9]{8,64}$/.test(p) && !get("cmi_person")) set("cmi_person", p);
}

export const rememberOwnerKey = (checkId, key) => set(`cmi_owner_${checkId}`, key);

/** The owner key for a check: from the link's fragment (#k=, which is kept), else the stored one. */
export function ownerKeyFor(checkId) {
  const fromHash = new URLSearchParams(location.hash.slice(1)).get("k");
  if (fromHash) rememberOwnerKey(checkId, fromHash);
  return fromHash || get(`cmi_owner_${checkId}`) || undefined;
}

/** Every owner link this device holds: [{checkId, ownerKey}]. */
export function ownerLinks() {
  const links = [];
  try {
    for (let i = 0; i < localStorage.length; i++) {
      const k = localStorage.key(i);
      if (k?.startsWith("cmi_owner_")) links.push({ checkId: k.slice(10), ownerKey: localStorage.getItem(k) });
    }
  } catch {}
  return links;
}

export const nameFor = (checkId) => get(`cmi_name_${checkId}`) || get("cmi_lastname") || "";
export const rememberName = (checkId, name) => { set(`cmi_name_${checkId}`, name); set("cmi_lastname", name); };
