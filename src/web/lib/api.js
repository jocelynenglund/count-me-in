// The CountMeIn module on the shared itsybit host. HTTP plumbing only: the rules
// live in the slices (src/web/slices/*), which the page runs before it calls here.
const LOCAL = location.hostname === "localhost" || location.hostname === "127.0.0.1";
export const API_BASE = LOCAL
  ? "http://localhost:5199/api/countmein"
  : "https://labsapi-hmeva5cfhdfkejhz.westeurope-01.azurewebsites.net/api/countmein";

export async function req(method, path, { body, ownerKey } = {}) {
  const headers = {};
  if (body !== undefined) headers["Content-Type"] = "application/json";
  if (ownerKey) headers["X-Owner-Key"] = ownerKey;
  const res = await fetch(API_BASE + path, { method, headers, body: body === undefined ? undefined : JSON.stringify(body) });
  const text = await res.text();
  const data = text ? JSON.parse(text) : null;
  if (!res.ok) throw Object.assign(new Error(data?.error || `${res.status}`), { status: res.status });
  return data;
}

/** A short url-safe random id. */
export const newId = (n = 10) =>
  Array.from(crypto.getRandomValues(new Uint8Array(n)), (b) => "abcdefghijkmnpqrstuvwxyz23456789"[b % 32]).join("");

/** The same "person" the server stores: first 16 hex of SHA-256("<checkId>:<personId>"). */
export async function personOf(checkId, personId) {
  const buf = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(`${checkId}:${personId}`));
  return [...new Uint8Array(buf)].map((b) => b.toString(16).padStart(2, "0")).join("").slice(0, 16);
}
