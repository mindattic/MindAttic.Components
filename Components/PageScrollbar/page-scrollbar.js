/* MindAttic.UiUx — PageScrollbar
 *
 * Replaces the browser's native page scrollbar with a draggable themed overlay.
 * Zero config: include the CSS + this script (defer) and it self-mounts. If a
 * `.ma-scrollbar` element already exists (e.g. emitted by the Blazor wrapper for
 * flash-free SSR) it is reused instead of created.
 *
 * - Thumb size tracks viewport / content ratio; position tracks scroll.
 * - Drag the thumb to scroll; click the track to page toward that point.
 * - Recomputes on scroll, resize, font swap, and DOM mutations.
 * - Idempotent (window.__maScrollbar guard); safe to load twice.
 */
(function () {
  'use strict';
  if (window.__maScrollbar) return;
  window.__maScrollbar = true;

  var docEl = document.documentElement;
  var track, thumb, raf = 0, dragging = false, dragOffset = 0;

  function maxScroll() {
    var docH = Math.max(
      docEl.scrollHeight,
      document.body ? document.body.scrollHeight : 0
    );
    return { docH: docH, vh: window.innerHeight, max: docH - window.innerHeight };
  }

  function update() {
    raf = 0;
    if (!track) return;
    var m = maxScroll();
    var trackH = track.clientHeight;
    if (m.max <= 1 || trackH <= 0) { track.classList.add('is-hidden'); return; }
    track.classList.remove('is-hidden');

    var minThumb = parseInt(getComputedStyle(thumb).minHeight, 10) || 28;
    var th = Math.max(minThumb, Math.round((m.vh / m.docH) * trackH));
    var scrollTop = window.scrollY || docEl.scrollTop || 0;
    var top = (scrollTop / m.max) * (trackH - th);
    thumb.style.height = th + 'px';
    thumb.style.transform = 'translateY(' + Math.round(top) + 'px)';
  }

  function schedule() { if (!raf) raf = requestAnimationFrame(update); }

  function onThumbDown(e) {
    e.preventDefault();
    e.stopPropagation();
    dragging = true;
    dragOffset = e.clientY - thumb.getBoundingClientRect().top;
    document.body.classList.add('ma-sb-dragging');
    if (thumb.setPointerCapture && e.pointerId != null) {
      try { thumb.setPointerCapture(e.pointerId); } catch (_) {}
    }
    window.addEventListener('pointermove', onThumbMove, true);
    window.addEventListener('pointerup', onThumbUp, true);
  }
  function onThumbMove(e) {
    if (!dragging) return;
    var m = maxScroll();
    var rect = track.getBoundingClientRect();
    var th = thumb.offsetHeight;
    var ratio = (e.clientY - rect.top - dragOffset) / (rect.height - th);
    ratio = Math.max(0, Math.min(1, ratio));
    window.scrollTo(0, ratio * m.max);
  }
  function onThumbUp() {
    dragging = false;
    document.body.classList.remove('ma-sb-dragging');
    window.removeEventListener('pointermove', onThumbMove, true);
    window.removeEventListener('pointerup', onThumbUp, true);
  }

  function onTrackClick(e) {
    if (e.target === thumb) return;
    var m = maxScroll();
    var rect = track.getBoundingClientRect();
    var th = thumb.offsetHeight;
    var ratio = (e.clientY - rect.top - th / 2) / (rect.height - th);
    ratio = Math.max(0, Math.min(1, ratio));
    window.scrollTo({ top: ratio * m.max, behavior: 'smooth' });
  }

  function mount() {
    track = document.querySelector('.ma-scrollbar');
    if (!track) {
      track = document.createElement('div');
      track.className = 'ma-scrollbar';
      track.setAttribute('aria-hidden', 'true');
      document.body.appendChild(track);
    }
    thumb = track.querySelector('.ma-scrollbar__thumb');
    if (!thumb) {
      thumb = document.createElement('div');
      thumb.className = 'ma-scrollbar__thumb';
      track.appendChild(thumb);
    }

    docEl.classList.add('ma-sb-active');

    window.addEventListener('scroll', schedule, { passive: true });
    window.addEventListener('resize', schedule);
    thumb.addEventListener('pointerdown', onThumbDown, true);
    track.addEventListener('click', onTrackClick);
    if (document.fonts && document.fonts.ready) document.fonts.ready.then(schedule);
    if (window.MutationObserver) {
      new MutationObserver(schedule).observe(document.body, {
        childList: true, subtree: true, characterData: true
      });
    }
    update();
  }

  if (document.body) mount();
  else document.addEventListener('DOMContentLoaded', mount);
})();
