// The CountMeIn module on the shared itsybit host. HTTP plumbing only: the rules
// live on the server, and the page only shows what its views say.
const LOCAL = location.hostname === "localhost" || location.hostname === "127.0.0.1";
export const API_BASE = LOCAL
  ? "http://localhost:5199/api/countmein"
  : "https://labsapi-hmeva5cfhdfkejhz.westeurope-01.azurewebsites.net/api/countmein";

export async function req(method, path, { body, ownerKey, personId } = {}) {
  const headers = {};
  if (body !== undefined) headers["Content-Type"] = "application/json";
  if (ownerKey) headers["X-Owner-Key"] = ownerKey;
  if (personId) headers["X-Person-Id"] = personId;
  const res = await fetch(API_BASE + path, { method, headers, body: body === undefined ? undefined : JSON.stringify(body) });
  const text = await res.text();
  const data = text ? JSON.parse(text) : null;
  if (!res.ok) throw Object.assign(new Error(data?.error || `${res.status}`), { status: res.status });
  return data;
}

/** A short url-safe random id. */
export const newId = (n = 10) =>
  Array.from(crypto.getRandomValues(new Uint8Array(n)), (b) => "abcdefghijkmnpqrstuvwxyz23456789"[b % 32]).join("");
