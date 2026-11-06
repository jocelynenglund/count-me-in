// Links in a pitch (display only): addresses become links; everything else stays escaped text.
import { test } from "node:test";
import assert from "node:assert/strict";
import { linkify } from "../../../src/web/lib/links.mjs";

test("a bare address and a full one become links; a full stop after one stays text", () => {
  const html = linkify("Peek at adventvibes.itsybit.se. Or https://example.com/a?b=1, if you like");
  assert.match(html, /<a href="https:\/\/adventvibes\.itsybit\.se" [^>]*>adventvibes\.itsybit\.se<\/a>\. Or/);
  assert.match(html, /<a href="https:\/\/example\.com\/a\?b=1" [^>]*>https:\/\/example\.com\/a\?b=1<\/a>, if/);
});

test("no HTML gets in: the text is escaped before links are made", () => {
  const html = linkify('<script>alert(1)</script> see "x.se"');
  assert.ok(!html.includes("<script>"));
  assert.match(html, /&lt;script&gt;/);
});

test("emails, numbers and plain words are left alone", () => {
  for (const s of ["mail me at ana@example.com", "version 1.5 is out", "e.g. this", "hello world"]) assert.ok(!linkify(s).includes("<a "), s);
});
