// /  : start an interest check. Reads its slices, composes their components, wires their commands.
import { read as readStartForm } from "../slices/start-form.slice.js";
import { read as readLookChoices } from "../slices/look-choices.slice.js";
import { read as readMyInterestChecks, MyChecksList } from "../slices/my-interest-checks.slice.js";
import { StartForm, StartLookPicker, wire as wireStart } from "../slices/start-interest-check.slice.js";
import { TopBar } from "./page.chrome.js";
import { show, showMissing, showError, root } from "./page.screen.js";

export async function showStart() {
  let form, looks, mine;
  try {
    // The list of this device's checks is a nicety: the form is there without it.
    [form, looks, mine] = await Promise.all([readStartForm(), readLookChoices(), readMyInterestChecks().catch(() => ({ checks: [] }))]);
  } catch (err) { return showMissing(err); }

  show("start", `${TopBar("Count me in")}${StartForm(form, { looks: StartLookPicker(looks), mine: MyChecksList(mine) })}`);
  wireStart(root(), { looks, onDone: ({ checkId, ownerKey }) => { location.href = `./?c=${checkId}&owner&new#k=${ownerKey}`; }, onError: showError });
}
