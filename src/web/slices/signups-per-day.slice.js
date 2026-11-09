// Owner only (the owner key goes along): signups-per-day.
import { req } from "../lib/api.js";
import { record } from "../lib/loaded.js";
import { weekday } from "../lib/format.js";

export const read = (checkId, ownerKey) =>
  req("GET", `/checks/${encodeURIComponent(checkId)}/views/signups-per-day`, { ownerKey }).then((v) => record("signupsPerDay", v));

/** Sign-ups per day, the last seven days; no chart when nobody ever signed up (view: signupsPerDay). */
export function SignupsChart(v) {
  if (!v.days.length) return `<div class="c" data-component="SignupsChart"></div>`;
  const max = Math.max(1, ...v.days.map((d) => d.in));
  const last = v.days.length - 1;
  return `<div class="c" data-component="SignupsChart"><div class="field"><span>Sign-ups per day</span></div>
      <div class="chart">${v.days.map((d) => `<div><span>${d.in}</span><b style="height:${d.in ? Math.round((d.in / max) * 80) + "%" : "3px"}"></b></div>`).join("")}</div>
      <div class="days">${v.days.map((d, i) => `<span>${i === last ? "Today" : weekday(d.day)}</span>`).join("")}</div></div>`;
}
