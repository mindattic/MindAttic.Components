/* MindAttic.Components - ThemeToggle/theme-toggle.js
   Wires every .ma-theme-toggle button to flip <html data-theme> between "dark" and "light" and
   remember the choice in localStorage ("ma-theme"). Call MindAtticThemeToggle.init() once after
   the DOM is ready; it is idempotent. The stored preference should also be applied as early as
   possible (an inline <head> script setting data-theme) to avoid a flash on load. */
(function (root) {
  'use strict';
  var KEY = 'ma-theme';

  function stored() {
    try { return localStorage.getItem(KEY); } catch (e) { return null; }
  }
  function remember(theme) {
    try { localStorage.setItem(KEY, theme); } catch (e) { /* private mode: ignore */ }
  }

  // Resolve the theme to show first: a saved choice wins, else the OS preference, else dark.
  function initialTheme() {
    var s = stored();
    if (s === 'light' || s === 'dark') return s;
    try {
      if (window.matchMedia && window.matchMedia('(prefers-color-scheme: light)').matches) return 'light';
    } catch (e) { /* no matchMedia */ }
    return 'dark';
  }

  function apply(theme) {
    document.documentElement.setAttribute('data-theme', theme);
  }

  function set(theme, persist) {
    apply(theme);
    if (persist !== false) remember(theme);
    document.querySelectorAll('.ma-theme-toggle').forEach(function (btn) {
      btn.setAttribute('aria-pressed', theme === 'light' ? 'true' : 'false');
      btn.setAttribute('aria-label', theme === 'light' ? 'Switch to dark theme' : 'Switch to light theme');
      btn.setAttribute('title', btn.getAttribute('aria-label'));
    });
    try {
      document.dispatchEvent(new CustomEvent('ma-theme-change', { detail: { theme: theme } }));
    } catch (e) { /* older browsers: ignore */ }
  }

  function toggle() {
    var current = document.documentElement.getAttribute('data-theme') === 'light' ? 'light' : 'dark';
    set(current === 'light' ? 'dark' : 'light', true);
  }

  var ICONS =
    '<svg class="ma-theme-toggle__icon ma-theme-toggle__sun" viewBox="0 0 24 24" aria-hidden="true">' +
      '<circle cx="12" cy="12" r="4"></circle>' +
      '<path d="M12 2v2M12 20v2M2 12h2M20 12h2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M19.1 4.9l-1.4 1.4M6.3 17.7l-1.4 1.4"></path>' +
    '</svg>' +
    '<svg class="ma-theme-toggle__icon ma-theme-toggle__moon" viewBox="0 0 24 24" aria-hidden="true">' +
      '<path d="M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8z"></path>' +
    '</svg>';

  function init() {
    // Make sure data-theme is set before the first paint-sensitive read.
    if (!document.documentElement.getAttribute('data-theme')) apply(initialTheme());
    document.querySelectorAll('.ma-theme-toggle').forEach(function (btn) {
      if (btn.dataset.maThemeWired) return;
      btn.dataset.maThemeWired = '1';
      btn.type = 'button';
      if (!btn.querySelector('.ma-theme-toggle__icon')) btn.innerHTML = ICONS;
      btn.addEventListener('click', toggle);
    });
    // Sync button state to whatever theme is currently applied, without re-persisting.
    set(document.documentElement.getAttribute('data-theme') === 'light' ? 'light' : 'dark', false);

    // Follow the OS only while the user has made no explicit choice.
    try {
      window.matchMedia('(prefers-color-scheme: light)').addEventListener('change', function (e) {
        if (!stored()) set(e.matches ? 'light' : 'dark', false);
      });
    } catch (e) { /* no matchMedia / older API */ }
  }

  root.MindAtticThemeToggle = { init: init, set: set, toggle: toggle, initialTheme: initialTheme };

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})(typeof window !== 'undefined' ? window : this);
