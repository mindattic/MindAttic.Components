// Scenarios per site. Each step may advance the frozen clock (runFor ms), click a point or a
// selector, press a key, scroll, and/or take a screenshot. Both pages run the identical steps.
const desktop = { width: 1366, height: 768 };
const phone = { width: 390, height: 844 };

const cyberspace = (name, viewport, tap) => ({
  name, viewport,
  steps: [
    { shot: 'first-paint' },
    { runFor: 1500, shot: 't1500' },
    { runFor: 6000, shot: 't7500' },
    { click: tap, runFor: 900, shot: 'tap-effect' },
    { runFor: 20000, shot: 't28400' },
  ],
});

const THEMES = ['light', 'dark', 'sakura', 'sand', 'spring', 'summer', 'autumn', 'winter', 'ocean', 'sunset',
  'forest', 'matrix', 'neko', 'cyberpunk', 'synthwave', 'noir'];

export const SITES = {
  'Hyperspace': {
    origin: 'https://hyperspace.mindattic.com/',
    scenarios: [
      ...[['article-desktop', desktop], ['article-phone', phone]].map(([name, viewport]) => ({
        name, viewport,
        steps: [
          { runFor: 1500, shot: 'hero' },
          { scrollTo: 900, runFor: 1500, shot: 'ladder' },
          { scrollTo: 1900, runFor: 1500, shot: 'slice' },
          { scrollTo: 2900, runFor: 1500, shot: 'projection' },
          { scrollTo: 99999, runFor: 1500, shot: 'footer' },
          { scrollTo: 0, hover: '#enterGalleryBtn', shot: 'enter-hover', unhover: true },
          { press: 'Tab', hover: 'body', shot: 'enter-focus' },
        ],
      })),
    ],
  },
  'ryandebraal.com': {
    origin: 'https://ryandebraal.com/',
    // Theme switches crossfade through a View Transition, which runs in real time; switch instantly
    // instead (the page's own 'Theme Transition: None' setting) so every theme is captured settled.
    initScript: `localStorage.setItem('resume-settings', JSON.stringify({ theme_transition: 'none' }));`,
    // The theme-rotation progress bar's width is elapsed time since load; it differs by fractions of a
    // millisecond run to run (and is site UI the themes never touch).
    mask: '#rotate-bar',
    scenarios: [
      // Every piece of chrome in its open/hovered/focused states, on a light and a dark theme.
      ...[['light', desktop], ['dark', desktop], ['light', phone], ['dark', phone]].map(([theme, viewport]) => ({
        name: `ui-${theme}-${viewport.width}`, viewport,
        steps: [
          { eval: `toggleThemeRotation(); setTheme('${theme}')`, runFor: 500, shot: 'page' },
          { hover: '#transport button', shot: 'transport-hover', unhover: true },
          { clickSel: '#theme-swatch', runFor: 200, shot: 'theme-menu' },
          { hover: '#theme-swatch-menu [data-theme="sakura"]', shot: 'theme-menu-hover' },
          { press: 'Escape', runFor: 200 },
          { clickSel: '#more-btn', runFor: 200, shot: 'more-menu' },
          { hover: '#more-menu button', shot: 'more-menu-hover' },
          { press: 'Escape', runFor: 200 },
          { clickSel: '#resume-nav-btn', runFor: 200, shot: 'nav-menu' },
          { hover: '#resume-nav-menu a', shot: 'nav-menu-hover' },
          { press: 'Escape', runFor: 200 },
          { hover: '.r-skill-tags > *', settleMs: 300, shot: 'tag-tooltip', unhover: true },
          { eval: 'openSettings()', runFor: 200, shot: 'settings' },
          { press: 'Escape', runFor: 200 },
          { eval: 'openAttributions()', runFor: 200, shot: 'attributions' },
          { press: 'Escape', runFor: 200 },
          { eval: 'openAvatarLightbox()', runFor: 400, shot: 'lightbox' },
          { press: 'Escape', runFor: 200 },
          { press: 'Tab', shot: 'focus-1' },
          { press: 'Tab', shot: 'focus-2' },
          { press: 'Tab', shot: 'focus-3' },
        ],
      })),
      ...[['themes-desktop', desktop], ['themes-phone', phone]].map(([name, viewport]) => ({
        name, viewport,
        steps: [
          { runFor: 1000, shot: 'initial' },
          { eval: 'toggleThemeRotation()', runFor: 100 },
          ...THEMES.flatMap((t) => [{ eval: `setTheme('${t}')`, runFor: 4000, shot: t }]),
        ],
      })),
    ],
  },
  'mindattic.com': {
    origin: 'https://mindattic.com/',
    // Everything Cyberspace draws is randomized beyond seeding (load-order and timing races), so it
    // is hidden for pixel/style comparison and proven by the source + resource-bytes checks instead.
    // .cyberspace-keepout is the page's own lockup (it only marks the zone effects stay out of).
    mask: '[class^="cyberspace-"]:not(.cyberspace-keepout), [class*=" cyberspace-"]:not(.cyberspace-keepout), .console-bg-host, canvas, #hsr-style',
    scenarios: [
      cyberspace('desktop', desktop, [160, 620]),
      cyberspace('phone', phone, [40, 760]),
      ...[['buttons-desktop', desktop], ['buttons-phone', phone]].map(([name, viewport]) => ({
        name, viewport,
        steps: [
          { runFor: 1500 },
          ...[1, 2, 3].map((n) => ({ hover: `.link-row a:nth-child(${n})`, shot: `hover-${n}`, unhover: true })),
          { runFor: 100, press: 'Tab', settleMs: 900, hover: 'body', shot: 'focus-1' },
          { press: 'Tab', hover: 'body', shot: 'focus-2' },
        ],
      })),
      { name: 'demo-desktop', viewport: desktop, path: '?demo=1',
        steps: [{ runFor: 15000, shot: 't15s' }, { runFor: 15000, shot: 't30s' }] },
    ],
  },
  'mindatticcares.com': {
    origin: 'https://mindatticcares.com/',
    scenarios: [
      ...[['desktop', desktop], ['phone', phone]].map(([name, viewport]) => ({
        name, viewport,
        steps: [
          { runFor: 500, shot: 'home', fullPage: true },
          { clickSel: 'header nav a[data-page="childs-play"]', runFor: 500, shot: 'childs-play', fullPage: true },
          { clickSel: '[data-yt]', runFor: 500, shot: 'video-clicked' },
          { clickSel: 'header nav a[data-page="y2k"]', runFor: 500, shot: 'y2k', fullPage: true },
          { clickSel: 'header a.brand', runFor: 500, shot: 'brand-home' },
        ],
      })),
      { name: 'deeplink', viewport: desktop, path: '#sec-budget', steps: [{ runFor: 500, shot: 'anchor' }] },
      { name: 'demo', viewport: desktop, path: '?demo=1',
        steps: [{ runFor: 5000, shot: 't5s' }, { runFor: 12000, shot: 't17s' }, { runFor: 12000, shot: 't29s' }] },
    ],
  },
};
