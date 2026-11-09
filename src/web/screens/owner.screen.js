// /?c=<id>&owner#k=<key> : how it's going, and closing it (owner). The key stays in the fragment
// (so it never reaches a server log) and on this device.
import { ownerProgress } from "../views/ownerProgress.read.js";
import { signupsPerDay } from "../views/signupsPerDay.read.js";
import { whosIn } from "../views/whosIn.read.js";
import { reachablePeople } from "../views/reachablePeople.read.js";
import { closingDate } from "../views/closingDate.read.js";
import { closeState } from "../views/closeState.read.js";
import { lookChoices } from "../views/lookChoices.read.js";
import { pitchLook } from "../views/pitchLook.read.js";
import { shareLinks } from "../views/shareLinks.read.js";
import { ownerKeyFor } from "../lib/device.js";
import { changeClosingDate, chooseTheme, closeCheck } from "../lib/commands.js";
import { emailList } from "../lib/format.js";
import { OwnerProgress, SignupsChart, WhosInList, CopyEmailsButton, ClosingDateField, CloseButton, ShareBox } from "./owner.parts.js";
import { LookPicker } from "./start.parts.js";
import { TopBar, PageTheme } from "./page.parts.js";
import { show, showMissing, showError, wireCopyButtons, $, $$ } from "./page.screen.js";
import { showPitch } from "./pitch.screen.js";

export async function showOwner(checkId) {
  const ownerKey = ownerKeyFor(checkId);
  let progress, chart, people, reachable, closing, closed, looks, look;
  try {
    [progress, chart, people, reachable, closing, closed, looks, look] = await Promise.all([
      ownerProgress(checkId, ownerKey), signupsPerDay(checkId, ownerKey), whosIn(checkId, ownerKey), reachablePeople(checkId, ownerKey),
      closingDate(checkId, ownerKey), closeState(checkId, ownerKey), lookChoices(checkId), pitchLook(checkId),
    ]);
  } catch (err) { return err.status === 403 ? showPitch(checkId) : showMissing(err); }
  const links = shareLinks(checkId);

  show("owner", `${TopBar("How it's going", `<span class="acid">Private</span>`)}
    <main class="body">
      ${OwnerProgress(progress)}
      ${SignupsChart(chart)}
      ${CopyEmailsButton(reachable)}
      ${WhosInList(people)}
      <div class="grow"></div>
      ${ShareBox(links)}
      ${ClosingDateField(closing)}
      ${LookPicker(looks)}
      <p class="err" hidden></p>
      ${CloseButton(closed)}
    </main>`, PageTheme(look));

  const run = async (command) => { try { await command(); showOwner(checkId); } catch (err) { showError(err.message); } };
  $("#copyEmails")?.addEventListener("click", async (e) => {
    const list = emailList(reachable.people);
    try { await navigator.clipboard.writeText(list); e.target.textContent = "Copied"; } catch { prompt("Copy these addresses", list); }
  });
  $("#saveDate")?.addEventListener("click", () => run(() => changeClosingDate(checkId, ownerKey, $("#closesOn").value)));
  $("#noDate")?.addEventListener("click", () => run(() => changeClosingDate(checkId, ownerKey, null)));
  $$(".look[data-theme]").forEach((b) => b.addEventListener("click", () => {
    if (b.getAttribute("aria-pressed") === "true") return;
    run(() => chooseTheme(checkId, ownerKey, b.dataset.theme));
  }));
  wireCopyButtons();
  $("#close")?.addEventListener("click", () => {
    if (!confirm("Close it? Nobody can count themselves in after this.")) return;
    run(() => closeCheck(checkId, ownerKey));
  });
}
