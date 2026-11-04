window.DEBUG = window.DEBUG || {};

(function () {
  function isEnabled() {
    const params = new URLSearchParams(location.search);
    return params.has('debug');
  }

  function fmtTime(iso) {
    const d = new Date(iso);
    return d.toLocaleTimeString('sv-SE', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
  }

  function fmtPayload(payload) {
    if (payload == null) return '';
    try {
      return JSON.stringify(payload);
    } catch (_) {
      return String(payload);
    }
  }

  function ensurePill() {
    let pill = document.getElementById('debug-pill');
    if (pill) return pill;
    pill = document.createElement('button');
    pill.id = 'debug-pill';
    pill.className = 'debug-pill';
    pill.type = 'button';
    pill.setAttribute('aria-label', 'event log');
    pill.innerHTML = '<span class="debug-pill-dot"></span><span class="debug-pill-label">log</span>';
    document.body.appendChild(pill);
    pill.addEventListener('click', DEBUG.openPanel);
    return pill;
  }

  function closePanel() {
    const panel = document.getElementById('debug-panel');
    if (panel) panel.remove();
  }

  DEBUG.openPanel = function () {
    closePanel();
    const panel = document.createElement('div');
    panel.id = 'debug-panel';
    panel.className = 'debug-panel';
    panel.innerHTML = `
      <div class="debug-panel-head">
        <div class="debug-panel-title">event log</div>
        <button class="debug-panel-close" type="button" aria-label="close">×</button>
      </div>
      <div class="debug-panel-meta" id="debug-panel-meta">loading…</div>
      <div class="debug-panel-list" id="debug-panel-list"></div>
    `;
    document.body.appendChild(panel);
    panel.querySelector('.debug-panel-close').addEventListener('click', closePanel);

    const meta = panel.querySelector('#debug-panel-meta');
    const list = panel.querySelector('#debug-panel-list');

    if (typeof LOG === 'undefined') {
      meta.textContent = 'log store not loaded';
      return;
    }

    meta.textContent = 'local log · loading…';

    // The event source is the only project-specific part of this slice. Here it
    // is the interest check's events as the page last loaded them.
    LOG.events()
      .then(function (raw) {
        meta.textContent = `this interest check · ${raw.length} events`;
        const events = raw.slice().reverse().map(function (e) {
          const payload = Object.assign({}, e);
          delete payload.seq; delete payload.type; delete payload.at;
          return { type: e.type, timestamp: e.at, payload: payload };
        });
        if (events.length === 0) {
          list.innerHTML = '<div class="debug-empty">no events</div>';
          return;
        }
        list.innerHTML = events.map(function (e) {
          return `
            <div class="debug-event">
              <div class="debug-event-head">
                <span class="debug-event-type">${e.type}</span>
                <span class="debug-event-time">${fmtTime(e.timestamp)}</span>
              </div>
              <pre class="debug-event-payload">${fmtPayload(e.payload)}</pre>
            </div>
          `;
        }).join('');
      })
      .catch(function (err) {
        meta.textContent = 'failed to load events';
        list.innerHTML = `<div class="debug-empty">${err && err.message ? err.message : 'error'}</div>`;
      });
  };

  DEBUG.mount = function () {
    if (!isEnabled()) return;
    ensurePill();
  };

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', DEBUG.mount);
  } else {
    DEBUG.mount();
  }
})();
