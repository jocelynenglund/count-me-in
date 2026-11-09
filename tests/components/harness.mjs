// A component test: a view's value goes in, the component draws it, the text a person would read comes out.
// thenComponent(componentName, viewName, viewValue, draw, expectedText)
//   draw(viewValue) -> the component's HTML; its root must be marked data-component="<componentName>".
// tests/structure.test.mjs reads these calls to check that each component draws from one view only.
import assert from "node:assert/strict";
import { test } from "node:test";

export const text = (html) => html.replace(/<[^>]*>/g, " ").replace(/&#39;/g, "'").replace(/&amp;/g, "&").replace(/\s+/g, " ").trim();

export function thenComponent(componentName, viewName, viewValue, draw, expectedText, title = "") {
  test(`${componentName} from ${viewName}${title ? ": " + title : ""}`, () => {
    const html = draw(viewValue);
    assert.match(html.trim(), new RegExp(`^<[a-z]+[^>]*data-component="${componentName}"`), `the root must be marked data-component="${componentName}"`);
    assert.equal(text(html), expectedText);
  });
}
