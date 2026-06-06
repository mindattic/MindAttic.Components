/* MindAttic.UiUx — Tooltip
 *
 * A standalone, dependency-free, accessible tooltip. Zero per-element wiring:
 * any element with a `data-tooltip="..."` attribute (added at any time, even
 * after async render) gets a tooltip on hover AND keyboard focus.
 *
 * Attributes on the trigger:
 *   data-tooltip       (required)  the text to show
 *   data-tooltip-pos   (optional)  preferred side: top | bottom | left | right
 *                                  (default "top"; auto-flips to stay on screen)
 *   data-tooltip-delay (optional)  show delay in ms (default 0)
 *   data-tooltip-html  (optional)  if present, value of data-tooltip is treated
 *                                  as trusted HTML instead of plain text
 *
 * One floating node is created and reused. Self-contained: include the CSS +
 * this script (defer) and you're done. Safe to load more than once.
 */
(function () {
  'use strict';
  if (window.__maTooltip) return;
  window.__maTooltip = true;

  var SEL = '[data-tooltip]';
  var GAP = 10;        // distance from trigger
  var EDGE = 6;        // min distance from viewport edge
  var tip, arrow, body, current, timer;

  function build() {
    if (tip) return;
    tip = document.createElement('div');
    tip.className = 'ma-tooltip';
    tip.id = 'ma-tooltip';
    tip.setAttribute('role', 'tooltip');
    arrow = document.createElement('span');
    arrow.className = 'ma-tooltip__arrow';
    body = document.createElement('span');
    body.className = 'ma-tooltip__body';
    tip.appendChild(arrow);
    tip.appendChild(body);
    document.body.appendChild(tip);
  }

  function place(target) {
    var pref = (target.getAttribute('data-tooltip-pos') || 'top').toLowerCase();
    var r = target.getBoundingClientRect();
    var w = tip.offsetWidth, h = tip.offsetHeight;
    var vw = document.documentElement.clientWidth;
    var vh = document.documentElement.clientHeight;
    var cx = r.left + r.width / 2;
    var cy = r.top + r.height / 2;

    var fits = {
      top:    r.top - h - GAP >= 0,
      bottom: r.bottom + h + GAP <= vh,
      left:   r.left - w - GAP >= 0,
      right:  r.right + w + GAP <= vw
    };
    var order = [pref, 'top', 'bottom', 'right', 'left'];
    var pos = pref;
    for (var i = 0; i < order.length; i++) {
      if (fits[order[i]]) { pos = order[i]; break; }
    }

    var x, y;
    if (pos === 'top')    { x = cx - w / 2;   y = r.top - h - GAP; }
    else if (pos === 'bottom') { x = cx - w / 2; y = r.bottom + GAP; }
    else if (pos === 'left')   { x = r.left - w - GAP; y = cy - h / 2; }
    else                  { x = r.right + GAP; y = cy - h / 2; } // right

    // Clamp into the viewport.
    x = Math.max(EDGE, Math.min(x, vw - w - EDGE));
    y = Math.max(EDGE, Math.min(y, vh - h - EDGE));

    tip.setAttribute('data-pos', pos);
    tip.style.left = (x + window.scrollX) + 'px';
    tip.style.top = (y + window.scrollY) + 'px';

    // Point the arrow at the trigger's center, clamped to the box.
    arrow.style.left = arrow.style.top = '';
    if (pos === 'top' || pos === 'bottom') {
      arrow.style.left = Math.max(8, Math.min(cx - x, w - 8)) - 4 + 'px';
    } else {
      arrow.style.top = Math.max(8, Math.min(cy - y, h - 8)) - 4 + 'px';
    }
  }

  function show(target) {
    var txt = target.getAttribute('data-tooltip');
    if (!txt) return;
    build();
    if (target.hasAttribute('data-tooltip-html')) body.innerHTML = txt;
    else body.textContent = txt;
    current = target;
    target.setAttribute('aria-describedby', 'ma-tooltip');
    // Make measurable before placing.
    tip.style.visibility = 'hidden';
    tip.classList.add('is-visible');
    place(target);
    tip.style.visibility = '';
  }

  function hide() {
    clearTimeout(timer);
    if (!tip) return;
    tip.classList.remove('is-visible');
    if (current) { current.removeAttribute('aria-describedby'); current = null; }
  }

  function trigger(node) {
    return node && node.closest ? node.closest(SEL) : null;
  }

  function onOver(e) {
    var t = trigger(e.target);
    if (!t || t === current) return;
    clearTimeout(timer);
    var delay = parseInt(t.getAttribute('data-tooltip-delay') || '0', 10) || 0;
    if (delay > 0) timer = setTimeout(function () { show(t); }, delay);
    else show(t);
  }

  function onOut(e) {
    var t = trigger(e.target);
    if (!t) return;
    // Ignore moves that stay within the same trigger.
    if (e.relatedTarget && t.contains(e.relatedTarget)) return;
    hide();
  }

  function onKey(e) { if (e.key === 'Escape') hide(); }
  function onScrollResize() { if (current) place(current); }

  document.addEventListener('mouseover', onOver, true);
  document.addEventListener('mouseout', onOut, true);
  document.addEventListener('focusin', onOver, true);
  document.addEventListener('focusout', onOut, true);
  document.addEventListener('keydown', onKey, true);
  window.addEventListener('scroll', onScrollResize, true);
  window.addEventListener('resize', onScrollResize);
})();
