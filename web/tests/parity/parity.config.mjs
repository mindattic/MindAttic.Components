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

export const SITES = {
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
