// whats-new — a user-facing changelog. Reads whats-new.json (generated from git
// at deploy time by whats-new.sh), shows a small corner pill with a dot when this
// device hasn't seen the latest entry, and opens a panel listing recent changes.
// Standard affordance across projects — the user-facing sibling of the ?debug pill.
// Self-mounts on DOMContentLoaded. No backend, no deps.
(function () {
  const SRC = "whats-new.json";
  const SEEN_KEY = "whatsnew_seen_sha";

  // Snackchat's copy of this slice leans on a shared UI.el; this app has no such
  // helper, and the bare reference threw on every load, so the pill never once
  // appeared. Kept local so the slice stays droppable into any project.
  function el(tag, attrs, children) {
    const n = document.createElement(tag);
    for (const [k, v] of Object.entries(attrs || {})) {
      if (k.startsWith("on") && typeof v === "function") n.addEventListener(k.slice(2), v);
      else n.setAttribute(k, v);
    }
    for (const c of [].concat(children == null ? [] : children)) {
      n.append(c instanceof Node ? c : String(c));
    }
    return n;
  }

  async function load() {
    try {
      const r = await fetch(SRC, { cache: "no-cache" });
      if (!r.ok) return [];
      const d = await r.json();
      return Array.isArray(d) ? d : [];
    } catch (_) { return []; }
  }

  function fmtDate(iso) {
    const d = new Date(iso + "T00:00:00");
    if (isNaN(d)) return iso;
    return d.toLocaleDateString("en-US", { month: "short", day: "numeric" });
  }

  function mount(items) {
    if (!items.length) return;
    const newest = items[0].sha;
    // Hide-when-read: only surface the pill while there's an entry this device
    // hasn't opened. Once read it disappears until the next deploy adds a newer one.
    let seen = null;
    try { seen = localStorage.getItem(SEEN_KEY); } catch (_) {}
    if (seen === newest) return;
    let panel = null;

    const dot = el("span", { class: "wn-pill__dot", "aria-hidden": "true" });
    const pill = el("button", { type: "button", class: "wn-pill", "aria-label": "What's new" }, [
      el("span", { class: "wn-pill__spark", "aria-hidden": "true" }, "✨"),
      el("span", { class: "wn-pill__label" }, "What's new"),
      dot,
    ]);

    function close() {
      if (!panel) return;
      panel.classList.remove("wn-panel--open");
      const p = panel; panel = null;
      setTimeout(() => p.remove(), 180);
      document.removeEventListener("keydown", onEsc);
    }
    function onEsc(e) { if (e.key === "Escape") close(); }

    function open() {
      if (panel) { close(); return; }
      // Reading marks seen and retires the pill until the next deploy adds an entry.
      try { localStorage.setItem(SEEN_KEY, newest); } catch (_) {}
      pill.remove();

      const scrim = el("div", { class: "wn-panel__scrim", onclick: close });
      panel = el("div", { class: "wn-panel" }, [
        scrim,
        el("div", { class: "wn-panel__sheet" }, [
          el("div", { class: "wn-panel__head" }, [
            el("span", { class: "wn-panel__title serif" }, "What's new"),
            el("button", { type: "button", class: "wn-panel__x", "aria-label": "Close", onclick: close }, "✕"),
          ]),
          el("div", { class: "wn-panel__list" }, items.map((it) =>
            el("div", { class: "wn-item" }, [
              el("div", { class: "wn-item__date mono" }, fmtDate(it.date)),
              el("div", { class: "wn-item__title" }, it.title),
            ]))),
        ]),
      ]);
      document.body.appendChild(panel);
      requestAnimationFrame(() => panel.classList.add("wn-panel--open"));
      document.addEventListener("keydown", onEsc);
    }

    pill.addEventListener("click", open);
    document.body.appendChild(pill);
  }

  document.addEventListener("DOMContentLoaded", async () => mount(await load()));
})();
