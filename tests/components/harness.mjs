// A component test: a read's value goes in, the component draws it, the text a person would read comes out.
// thenComponent(componentName, readSlice, readValue, draw, expectedText)
//   readSlice is the slice whose `read` feeds the component ("(none)" when it takes no read);
//   draw(readValue) -> the component's HTML; its root must be marked data-component="<componentName>".
// Drawing runs with fetch and localStorage trapped: a component is a pure function of what it is given.
// tests/structure.test.mjs reads these calls to check that each component draws from one read only.
import assert from "node:assert/strict";
import { test } from "node:test";

// The slices import lib/api.js, which reads location when loaded: give it one (nothing is fetched in tests).
globalThis.location ??= { hostname: "test.invalid", href: "https://test.invalid/", search: "" };

export const text = (html) => html.replace(/<[^>]*>/g, " ").replace(/&#39;/g, "'").replace(/&amp;/g, "&").replace(/\s+/g, " ").trim();

/** Run draw(value) with the network and storage trapped; fails if the component reached for either. */
export function drawPure(draw, value) {
  const touched = [];
  const savedFetch = globalThis.fetch;
  const savedStorage = Object.getOwnPropertyDescriptor(globalThis, "localStorage");
  globalThis.fetch = () => { touched.push("fetch"); throw new Error("a component called fetch"); };
  Object.defineProperty(globalThis, "localStorage", { configurable: true, get() { touched.push("localStorage"); throw new Error("a component read localStorage"); } });
  try {
    const html = draw(value);
    assert.deepEqual(touched, [], "a component must only draw what it is given");
    return html;
  } finally {
    globalThis.fetch = savedFetch;
    if (savedStorage) Object.defineProperty(globalThis, "localStorage", savedStorage); else delete globalThis.localStorage;
  }
}

export function thenComponent(componentName, readSlice, readValue, draw, expectedText, title = "") {
  test(`${componentName} from ${readSlice}${title ? ": " + title : ""}`, () => {
    const html = drawPure(draw, readValue);
    assert.match(html.trim(), new RegExp(`^<[a-z]+[^>]*data-component="${componentName}"`), `the root must be marked data-component="${componentName}"`);
    assert.equal(text(html), expectedText);
  });
}
