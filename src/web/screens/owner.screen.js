// /?c=<id>&owner#k=<key> : how it's going, and closing it (owner). The key stays in the fragment
// (so it never reaches a server log) and on this device.
import { read as readOwnerProgress, OwnerProgress } from "../slices/owner-progress.slice.js";
import { read as readSignupsPerDay, SignupsChart } from "../slices/signups-per-day.slice.js";
import { read as readWhosIn, WhosInList } from "../slices/whos-in.slice.js";
import { read as readReachablePeople, CopyEmailsButton, wire as wireCopyEmails } from "../slices/reachable-people.slice.js";
import { read as readClosingDate } from "../slices/closing-date.slice.js";
import { ClosingDateField, wire as wireClosingDate } from "../slices/change-closing-date.slice.js";
import { read as readCloseState, CloseState } from "../slices/close-state.slice.js";
import { CloseButton, wire as wireClose } from "../slices/close-interest-check.slice.js";
import { read as readCheckLookChoices } from "../slices/check-look-choices.slice.js";
import { LookPicker, wire as wireChooseTheme } from "../slices/choose-theme.slice.js";
import { read as readPitchLook, PageTheme } from "../slices/pitch-look.slice.js";
import { read as readShareLinks, ShareBox } from "../slices/share-links.slice.js";
import { ownerKeyFor } from "../lib/device.js";
import { TopBar } from "./page.chrome.js";
import { show, showMissing, showError, wireCopyButtons, root } from "./page.screen.js";
import { showPitch } from "./pitch.screen.js";

export async function showOwner(checkId) {
  const ownerKey = ownerKeyFor(checkId);
  let progress, chart, people, reachable, closing, closed, looks, look;
  try {
    [progress, chart, people, reachable, closing, closed, looks, look] = await Promise.all([
      readOwnerProgress(checkId, ownerKey), readSignupsPerDay(checkId, ownerKey), readWhosIn(checkId, ownerKey), readReachablePeople(checkId, ownerKey),
      readClosingDate(checkId, ownerKey), readCloseState(checkId, ownerKey), readCheckLookChoices(checkId), readPitchLook(checkId),
    ]);
  } catch (err) { return err.status === 403 ? showPitch(checkId) : showMissing(err); }
  const links = readShareLinks(checkId);

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
      ${CloseState(closed, { close: CloseButton() })}
    </main>`, PageTheme(look));

  const again = { checkId, ownerKey, onDone: () => showOwner(checkId), onError: showError };
  wireCopyEmails(root(), { people: reachable.people });
  wireClosingDate(root(), again);
  wireChooseTheme(root(), again);
  wireCopyButtons();
  wireClose(root(), again);
}
