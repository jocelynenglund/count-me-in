// The owner page's parts. Each draws one view and nothing else.
import { esc, fmtDay, weekday, plural, linkify, meter } from "../lib/format.js";

/** Title, how many are in against the goal, what's left (view: ownerProgress). */
export function OwnerProgress(v) {
  const closed = String(v.status).startsWith("closed");
  const aside = closed ? "Closed" : v.toGo ? `${v.toGo} to go` : "Goal reached";
  return `<div class="c" data-component="OwnerProgress"><h1 class="mid">${esc(v.title)}</h1>
      <div class="split"><div class="num acid">${v.count}<small>/${v.goal}</small></div>
        <div class="aside">${aside}${v.closesOn ? `<br>closes ${fmtDay(v.closesOn)}` : ""}</div></div>
      ${meter(v)}</div>`;
}

/** Sign-ups per day, the last seven days; no chart when nobody ever signed up (view: signupsPerDay). */
export function SignupsChart(v) {
  if (!v.days.length) return `<div class="c" data-component="SignupsChart"></div>`;
  const max = Math.max(1, ...v.days.map((d) => d.in));
  const last = v.days.length - 1;
  return `<div class="c" data-component="SignupsChart"><div class="field"><span>Sign-ups per day</span></div>
      <div class="chart">${v.days.map((d) => `<div><span>${d.in}</span><b style="height:${d.in ? Math.round((d.in / max) * 80) + "%" : "3px"}"></b></div>`).join("")}</div>
      <div class="days">${v.days.map((d, i) => `<span>${i === last ? "Today" : weekday(d.day)}</span>`).join("")}</div></div>`;
}

/** Who's in; those who left an email carry an @ (view: whosIn). */
export function WhosInList(v) {
  if (!v.people.length) return `<p class="pitch" data-component="WhosInList">Nobody yet. Share the link.</p>`;
  return `<div class="ppl" data-component="WhosInList">${v.people.map((p) => `<div data-key="${esc(p.name)}">${esc(p.name)}${p.leftEmail ? ` <small title="left an email">@</small>` : ""}</div>`).join("")}</div>`;
}

/** "Copy N emails" of the people who are in and want to hear; nothing when there are none (view: reachablePeople). */
export function CopyEmailsButton(v) {
  if (!v.people.length) return `<div class="c" data-component="CopyEmailsButton"></div>`;
  return `<div class="mailrow" data-component="CopyEmailsButton"><button class="look" type="button" id="copyEmails">Copy ${plural(v.people.length, "email")}</button><span>of people who are in and want to hear. Paste them in Bcc.</span></div>`;
}

/** The closing date and how to change it; nothing once it can't be changed (view: closingDate). */
export function ClosingDateField(v) {
  if (!v.canChange) return `<div class="c" data-component="ClosingDateField"></div>`;
  return `<div class="field" data-component="ClosingDateField"><span>Closes</span><div class="dateline"><input class="in" type="date" id="closesOn" min="${esc(v.earliest)}" value="${esc(v.closesOn ?? "")}"><button class="look" type="button" id="saveDate">Save date</button></div>${v.closesOn ? `<button class="link" type="button" id="noDate">No end date</button>` : ""}</div>`;
}

/** "Close it", or the Closed tag once it is (view: closeState). */
export function CloseButton(v) {
  if (v.status === "open") return `<button class="ghost danger" id="close" type="button" data-component="CloseButton">Close it</button>`;
  return `<div class="tag" data-component="CloseButton">Closed</div>`;
}

/**
 * The link to share, and on the first visit the note to keep the private link (view: shareLinks).
 * The note floats to the top of the page (order: -1); the share row sits where the part is placed.
 */
export function ShareBox(v) {
  return `<div class="c" data-component="ShareBox">${v.isNew ? `<div class="note" style="order:-1"><b>Keep this page's link.</b> It's your key to see who's in and to close it. <button class="link" data-copy="${esc(v.ownerLink)}">Copy my private link</button></div>` : ""}
      <div class="share"><span>${esc(v.share.replace(/^https?:\/\//, ""))}</span><button class="link" data-copy="${esc(v.share)}">Copy link</button></div></div>`;
}
