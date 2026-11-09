// /  : start an interest check. Fetches its views, composes the parts, sends the command.
import { startForm } from "../views/startForm.read.js";
import { lookChoices } from "../views/lookChoices.read.js";
import { myInterestChecks } from "../views/myInterestChecks.read.js";
import { startCheck } from "../lib/commands.js";
import { StartForm, LookPicker, MyChecksList } from "./start.parts.js";
import { TopBar } from "./page.parts.js";
import { show, showMissing, showError, $, $$ } from "./page.screen.js";

export async function showStart() {
  let form, looks, mine;
  try {
    // The list of this device's checks is a nicety: the form is there without it.
    [form, looks, mine] = await Promise.all([startForm(), lookChoices(), myInterestChecks().catch(() => ({ checks: [] }))]);
  } catch (err) { return showMissing(err); }

  show("start", `${TopBar("Count me in")}${StartForm(form, { looks: LookPicker(looks), mine: MyChecksList(mine) })}`);
  let theme = looks.choices.find((c) => c.picked)?.theme;
  $$(".look").forEach((b) => b.addEventListener("click", () => {
    theme = b.dataset.theme;
    document.body.dataset.theme = theme; // preview it on the form
    $$(".look").forEach((o) => o.setAttribute("aria-pressed", String(o === b)));
  }));
  $("#start").addEventListener("submit", async (e) => {
    e.preventDefault();
    const f = new FormData(e.target);
    try {
      const { checkId, ownerKey } = await startCheck({ title: f.get("title"), pitch: f.get("pitch"), goal: Number(f.get("goal")), closesOn: f.get("closesOn") || undefined, theme });
      location.href = `./?c=${checkId}&owner&new#k=${ownerKey}`;
    } catch (err) { showError(err.message); }
  });
}
