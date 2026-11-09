// Every button that changes something: one command each, sent as it is. The server decides;
// a refusal comes back as an Error whose message the page shows exactly as it is.
import { req, newId } from "./api.js";
import * as device from "./device.js";

const at = (checkId) => `/checks/${encodeURIComponent(checkId)}`;

/** Start a check; the look picked on the form is applied right after (the owner page can change it again). */
export async function startCheck({ title, pitch, goal, closesOn, theme }) {
  const checkId = newId();
  const { ownerKey } = await req("POST", "/checks", { body: { checkId, title, pitch, goal, closesOn } });
  device.rememberOwnerKey(checkId, ownerKey);
  if (theme && theme !== "classic") await req("POST", `${at(checkId)}/theme`, { ownerKey, body: { theme } }).catch(() => {});
  return { checkId, ownerKey };
}

export async function countMeIn(checkId, { name, email }) {
  await req("POST", `${at(checkId)}/interest`, { body: { personId: device.personId(), name, email: email || undefined } });
  device.rememberName(checkId, name);
}

export const changeMyMind = (checkId) => req("POST", `${at(checkId)}/withdraw`, { body: { personId: device.personId() } });
export const closeCheck = (checkId, ownerKey) => req("POST", `${at(checkId)}/close`, { ownerKey });
export const chooseTheme = (checkId, ownerKey, theme) => req("POST", `${at(checkId)}/theme`, { ownerKey, body: { theme } });
export const changeClosingDate = (checkId, ownerKey, closesOn) => req("POST", `${at(checkId)}/closing-date`, { ownerKey, body: { closesOn: closesOn || null } });
