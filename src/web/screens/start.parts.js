// The start page's parts. Each draws one view and nothing else.
import { esc } from "../lib/format.js";

/**
 * The form for a new interest check (view: startForm). `slots` are the look picker and the
 * list of this device's checks, already drawn, which sit inside the form.
 */
export function StartForm(v, slots = {}) {
  return `<form class="body" id="start" data-component="StartForm" novalidate>
      <h1 class="big">Got an idea? Count heads first.</h1>
      <label class="field"><span>The idea</span><input class="in" name="title" maxlength="120" placeholder="Saturday board-game club" required></label>
      <label class="field"><span>The pitch</span><textarea class="in" name="pitch" maxlength="1000" rows="3" placeholder="Where, when, and what happens if enough people are in."></textarea></label>
      <div class="row">
        <label class="field"><span>Goal</span><input class="in" name="goal" type="number" min="1" max="100000" inputmode="numeric" value="${esc(v.defaultGoal)}" required></label>
        <label class="field"><span>Closes <small>(optional)</small></span><input class="in" name="closesOn" type="date" min="${esc(v.earliestClosingDate)}"></label>
      </div>
      ${slots.looks ?? ""}
      <p class="err" hidden></p>
      <div class="grow"></div>
      <button class="btn" type="submit">Start counting</button>
      ${slots.mine ?? ""}
    </form>`;
}

/** One button per look; the pressed one is the pitch's look (view: lookChoices). Start page and owner page. */
export function LookPicker(v) {
  return `<div class="field" data-component="LookPicker"><span>The look</span><div class="looks" role="group" aria-label="The look">${v.choices
    .map((c) => `<button type="button" class="look" data-theme="${esc(c.theme)}" aria-pressed="${c.picked}">${esc(c.label)}</button>`).join("")}</div></div>`;
}

const MyCheckRow = (c) =>
  `<a class="minerow" data-component="MyCheckRow" data-key="${esc(c.checkId)}" href="${esc(c.ownerLink)}"><b>${esc(c.title)}</b><span>${c.count}/${c.goal}${c.open ? "" : " · closed"}</span></a>`;

/** The checks this device started, newest first; hidden when there are none (view: myInterestChecks). */
export function MyChecksList(v) {
  if (!v.checks.length) return `<section class="mine" id="mine" data-component="MyChecksList" hidden></section>`;
  return `<section class="mine" id="mine" data-component="MyChecksList"><div class="field"><span>Your interest checks</span></div>${v.checks.map(MyCheckRow).join("")}</section>`;
}
