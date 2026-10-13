// mindattic.com: centered wordmark + three equal-width link buttons whose combined width equals the wordmark's,
// no scrollbar at any size/aspect ratio, a fixed footer, and tap-anywhere-spawns-a-Cyberspace-effect.
import { test, expect } from '@playwright/test';
import { SITES } from '../../lib/paths.mjs';
import { openSite, loadSite } from '../../lib/site-session.mjs';

const site = SITES['mindattic.com'];
const VIEWPORTS = [[320, 568], [390, 844], [568, 320], [768, 1024], [1366, 768], [1920, 1080], [3440, 1440], [1920, 300], [400, 1920], [120, 600]]
  .map(([width, height]) => ({ width, height }));
const LINKS = [
  { text: 'Résumé', href: 'https://ryandebraal.com' },
  { text: 'GitHub', href: 'https://github.com/mindattic' },
  { text: 'MindAttic Cares', href: 'https://mindatticcares.com' },
];

const measure = () => {
  const btns = [...document.querySelectorAll('.link-btn')].map((b) => b.getBoundingClientRect());
  const rg = document.createRange(); rg.selectNodeContents(document.getElementById('site-name'));
  const word = rg.getBoundingClientRect();
  const lock = document.querySelector('.lockup').getBoundingClientRect();
  const de = document.documentElement; const bd = document.body;
  const foot = document.getElementById('site-footer'); const fr = foot.getBoundingClientRect();
  return {
    w: window.innerWidth, h: window.innerHeight, n: btns.length,
    widths: btns.map((b) => b.width), left: btns[0]?.left, right: btns[btns.length - 1]?.right, wordWidth: word.width,
    lock: { l: lock.left, r: lock.right, t: lock.top, b: lock.bottom },
    scroll: { docX: de.scrollWidth - de.clientWidth, docY: de.scrollHeight - de.clientHeight, bodyX: bd.scrollWidth - bd.clientWidth, bodyY: bd.scrollHeight - bd.clientHeight, gutter: window.innerWidth - de.clientWidth },
    footer: { position: getComputedStyle(foot).position, bottomGap: window.innerHeight - fr.bottom, text: foot.innerText.replace(/\s+/g, ' ').trim() },
  };
};

test.describe('mindattic.com — structure', () => {
  test('main landmark is a <div role=main> (a <main> would become a full-screen Cyberspace keepout); the lockup is the keepout', async ({ browser }) => {
    const s = await openSite(browser, site);
    try {
      await loadSite(s, { settleMs: 300 });
      const r = await s.page.evaluate(() => ({
        mains: document.querySelectorAll('main').length,
        landmark: document.querySelector('#content')?.getAttribute('role'),
        landmarkTag: document.querySelector('#content')?.tagName,
        keepout: document.querySelectorAll('.lockup.cyberspace-keepout').length,
      }));
      expect(r).toEqual({ mains: 0, landmark: 'main', landmarkTag: 'DIV', keepout: 1 });
    } finally { await s.context.close(); }
  });

  test('the three links: correct labels, hrefs, target=_blank and rel=noopener', async ({ browser }) => {
    const s = await openSite(browser, site);
    try {
      await loadSite(s, { settleMs: 300 });
      const links = await s.page.evaluate(() => [...document.querySelectorAll('.link-btn')].map((a) => ({ text: a.textContent.trim(), href: a.getAttribute('href'), target: a.getAttribute('target'), rel: a.getAttribute('rel') })));
      expect(links.map((l) => l.text)).toEqual(LINKS.map((l) => l.text));
      links.forEach((l, i) => { expect(l.href).toBe(LINKS[i].href); expect(l.target).toBe('_blank'); expect(l.rel).toMatch(/noopener/); });
    } finally { await s.context.close(); }
  });

  test('the Cyberspace engine scripts are deferred (they must never block the first paint)', async ({ browser }) => {
    const s = await openSite(browser, site);
    try {
      await loadSite(s, { settleMs: 300 });
      const d = await s.page.evaluate(() => ['mindattic-components-cyberspace-console-bg-js', 'mindattic-components-cyberspace-sacred-geometry-js'].map((id) => { const e = document.getElementById(id); return e ? e.defer : null; }));
      expect(d).toEqual([true, true]);
    } finally { await s.context.close(); }
  });

  test('parallax textures and both fonts are preloaded', async ({ browser }) => {
    const s = await openSite(browser, site);
    try {
      await loadSite(s, { settleMs: 300 });
      const pre = await s.page.evaluate(() => [...document.querySelectorAll('link[rel=preload]')].map((l) => ({ as: l.as, href: l.href, prio: l.getAttribute('fetchpriority'), cors: l.crossOrigin })));
      expect(pre.filter((p) => p.as === 'image' && /circuitboard\.0[012]\.png$/.test(p.href) && p.prio === 'low').length).toBe(3);
      expect(pre.some((p) => p.as === 'font' && /attic\.woff2$/.test(p.href) && p.cors !== null)).toBe(true);
      expect(pre.some((p) => p.as === 'font' && /outfit-latin\.woff2$/.test(p.href) && p.cors !== null)).toBe(true);
    } finally { await s.context.close(); }
  });
});

test.describe('mindattic.com — layout is exact at every size and aspect ratio', () => {
  let s;
  test.beforeAll(async ({ browser }) => {
    s = await openSite(browser, site, { viewport: VIEWPORTS[4] });
    await loadSite(s, { settleMs: 500 });
  });
  test.afterAll(async () => { await s?.context.close(); });

  for (const vp of VIEWPORTS) {
    test(`${vp.width}x${vp.height}`, async () => {
      await s.page.setViewportSize(vp);
      await s.page.evaluate(() => document.fonts.ready);
      await s.page.waitForTimeout(200);
      const m = await s.page.evaluate(measure);
      expect(m.n, 'three link buttons').toBe(3);
      // equal-width buttons
      expect(Math.max(...m.widths) - Math.min(...m.widths), `button widths ${m.widths.map((x) => x.toFixed(2))}`).toBeLessThanOrEqual(0.5);
      // combined width == wordmark text width
      expect(Math.abs((m.right - m.left) - m.wordWidth), `buttons ${(m.right - m.left).toFixed(2)}px vs wordmark ${m.wordWidth.toFixed(2)}px`).toBeLessThanOrEqual(0.5);
      // lockup fully on-screen
      expect(m.lock.l).toBeGreaterThanOrEqual(-0.5); expect(m.lock.t).toBeGreaterThanOrEqual(-0.5);
      expect(m.lock.r).toBeLessThanOrEqual(m.w + 0.5); expect(m.lock.b).toBeLessThanOrEqual(m.h + 0.5);
      // and centered both ways
      expect(Math.abs((m.lock.l + m.lock.r) / 2 - m.w / 2), 'horizontal centering').toBeLessThanOrEqual(1);
      expect(Math.abs((m.lock.t + m.lock.b) / 2 - m.h / 2), 'vertical centering').toBeLessThanOrEqual(1);
      // no scrollbars at all
      expect(m.scroll, 'scroll overflow').toEqual({ docX: 0, docY: 0, bodyX: 0, bodyY: 0, gutter: 0 });
      // footer fixed to the bottom edge
      expect(m.footer.position).toBe('fixed');
      expect(Math.abs(m.footer.bottomGap)).toBeLessThanOrEqual(1);
      expect(m.footer.text).toMatch(/© \d{4} MindAttic LLC/);
    });
  }

  test('typography: wordmark in Attic, buttons in Outfit, button text white at rest and on hover', async () => {
    await s.page.setViewportSize({ width: 1366, height: 768 });
    const fam = await s.page.evaluate(() => ({ word: getComputedStyle(document.getElementById('site-name')).fontFamily, btn: getComputedStyle(document.querySelector('.link-btn')).fontFamily, colors: [...document.querySelectorAll('.link-btn')].map((b) => getComputedStyle(b).color) }));
    expect(fam.word).toMatch(/^"?Attic"?/);
    expect(fam.btn).toMatch(/^"?Outfit"?/);
    expect(fam.colors).toEqual(['rgb(255, 255, 255)', 'rgb(255, 255, 255)', 'rgb(255, 255, 255)']);
    for (let i = 0; i < 3; i++) {
      await s.page.locator('.link-btn').nth(i).hover();
      await s.page.waitForTimeout(350); // let the hover transition finish
      expect(await s.page.locator('.link-btn').nth(i).evaluate((e) => getComputedStyle(e).color), `hover colour of link ${i}`).toBe('rgb(255, 255, 255)');
    }
  });
});

// Wait until the Cyberspace host has had no new direct children for a full second (max ~8s), then reset the
// counter. Effects scheduled before setAutoSpawn(false) can still land a few seconds later; counting from a
// quiet baseline keeps them from being mistaken for (or masking) the effects a tap is supposed to cause.
async function untilQuiet(page) {
  for (let i = 0; i < 8; i++) {
    const n0 = await page.evaluate(() => window.__added);
    await page.waitForTimeout(1000);
    if ((await page.evaluate(() => window.__added)) === n0) break;
  }
  await page.evaluate(() => { window.__added = 0; window.__anchors = []; });
}

// Switch the auto-spawner off and watch the Cyberspace host's direct children (one per spawned effect; lines
// typed inside an existing window do not count). For each added node the observer also records, at the moment
// it lands (before any animation frame moves it), how far its box is from the last pointerdown point:
// `window.__anchors = [{ cls, d }]`. A 0×0 container (ARTIFACT, PULSAR) is measured by its glyphs; full-screen
// canvases (a TRACE wire) are skipped because they contain every point.
async function watchHost(page) {
  await page.evaluate(() => {
    window.consoleBg._demo.setAutoSpawn(false);
    window.__added = 0; window.__anchors = []; window.__tap = null;
    document.addEventListener('pointerdown', (e) => { window.__tap = { x: e.clientX, y: e.clientY }; }, true);
    const boxOf = (n) => {
      const r = n.getBoundingClientRect();
      if (r.width * r.height > 0) return r;
      let l = Infinity, t = Infinity, rr = -Infinity, b = -Infinity;
      for (const c of n.querySelectorAll('*')) {
        const q = c.getBoundingClientRect();
        if (!q.width && !q.height) continue;
        l = Math.min(l, q.left); t = Math.min(t, q.top); rr = Math.max(rr, q.right); b = Math.max(b, q.bottom);
      }
      return rr > l ? { left: l, top: t, right: rr, bottom: b } : r;
    };
    window.__distTo = (n, p) => {
      const r = boxOf(n);
      return Math.hypot(Math.max(r.left - p.x, 0, p.x - r.right), Math.max(r.top - p.y, 0, p.y - r.bottom));
    };
    new MutationObserver((ml) => {
      for (const m of ml) {
        for (const n of m.addedNodes) {
          window.__added++;
          if (n.nodeType !== 1 || n.tagName === 'CANVAS' || !window.__tap) continue;
          window.__anchors.push({ cls: String(n.className), d: window.__distTo(n, window.__tap) });
        }
      }
    }).observe(document.querySelector('.console-bg-host'), { childList: true });
  });
}

// At 1600×1000 the lockup (~580×210px) sits in the middle, leaving ~490px either side and ~380px above and
// below. These tap points are clear of the keepout buffer zone (each test asserts that) and have room for the
// largest effect (a ~220×260px HEIST window) to start on them without being pushed off the tap by the zone.
// (Near the zone or an edge an effect is clamped/pushed instead; the "origin" test covers that separately.)
const ROOMY = { width: 1600, height: 1000 };
const CLEAR_TAPS = [[150, 120], [1450, 120], [150, 880], [1450, 880], [300, 200], [1300, 200], [300, 800], [1300, 800]];

test.describe('mindattic.com — Cyberspace', () => {
  test('the engine exposes every effect the tap handler uses', async ({ browser }) => {
    const s = await openSite(browser, site);
    try {
      await loadSite(s);
      const missing = await s.page.evaluate(() => {
        const fx = window.consoleBg && window.consoleBg._demo;
        const need = ['spawnError', 'spawnWarning', 'spawnWindow', 'spawnMemo', 'spawnFrag', 'spawnArtifact', 'spawnGeoWindow', 'spawnNetConnect', 'spawnMorseDot', 'spawnFolderRip', 'spawnCascade', 'spawnArtifactPredator', 'spawnHyperspaceReader', 'setAutoSpawn'];
        return fx ? need.filter((n) => typeof fx[n] !== 'function') : ['window.consoleBg._demo'];
      });
      expect(missing).toEqual([]);
      expect(await s.page.locator('.console-bg-host').count(), 'Cyberspace host layer').toBe(1);
      expect(await s.page.locator('canvas.cyberspace-tex').count(), 'parallax texture canvas').toBe(1);
    } finally { await s.context.close(); }
  });

  test('tapping empty space spawns an effect (most taps), without help from the auto-spawner', async ({ browser }) => {
    const s = await openSite(browser, site, { viewport: { width: 1000, height: 700 } });
    try {
      await loadSite(s);
      await watchHost(s.page);
      await untilQuiet(s.page); // effects scheduled before the auto-spawner was switched off must land first
      const lock = await s.page.evaluate(() => { const r = document.querySelector('.lockup').getBoundingClientRect(); return { l: r.left, r: r.right, t: r.top, b: r.bottom }; });
      const taps = [];
      for (let i = 0; taps.length < 15; i++) {
        const x = 40 + ((i * 197) % 920); const y = 40 + ((i * 131) % 560);
        if (!(x > lock.l - 20 && x < lock.r + 20 && y > lock.t - 20 && y < lock.b + 20)) taps.push([x, y]);
      }
      let hits = 0;
      for (const [x, y] of taps) {
        const before = await s.page.evaluate(() => window.__added);
        await s.page.mouse.click(x, y);
        // Some effects attach their DOM after a short internal delay (cascades, network traces), so give each tap
        // up to 600ms to show up instead of a single fixed 250ms look (that made the live run flaky at 11/15).
        for (let t = 0; t < 6; t++) {
          await s.page.waitForTimeout(100);
          if ((await s.page.evaluate(() => window.__added)) > before) { hits++; break; }
        }
      }
      expect(hits, `${hits}/15 taps spawned an effect`).toBeGreaterThanOrEqual(12);
      expect(s.rec.pageErrors).toEqual([]);
    } finally { await s.context.close(); }
  });

  test('tapping a link opens that site in a new window and spawns no effect and no sparks', async ({ browser }) => {
    const s = await openSite(browser, site, { viewport: { width: 1000, height: 700 } });
    try {
      await loadSite(s);
      await watchHost(s.page);
      await untilQuiet(s.page);
      for (let i = 0; i < LINKS.length; i++) {
        const [popup] = await Promise.all([s.context.waitForEvent('page'), s.page.locator('.link-btn').nth(i).click()]);
        await popup.waitForLoadState('domcontentloaded');
        expect(popup.url().replace(/\/$/, ''), `link ${i} (${LINKS[i].text})`).toBe(LINKS[i].href);
        await popup.close();
      }
      await s.page.waitForTimeout(500);
      expect(await s.page.evaluate(() => window.__added), 'link taps must not spawn effects').toBe(0);
      expect(await s.page.evaluate(() => window.consoleBg._demo.sparkStats().frames), 'link taps must not spark').toBe(0);
      expect(s.rec.navigations.map((u) => u.replace(/\/$/, '')).sort(), 'every external navigation was intercepted').toEqual(LINKS.map((l) => l.href).sort());
    } finally { await s.context.close(); }
  });

  test('a tap outside the keepout spawns an effect that starts at the tap point', async ({ browser }) => {
    const s = await openSite(browser, site, { viewport: ROOMY });
    try {
      await loadSite(s);
      await watchHost(s.page);
      await untilQuiet(s.page);
      const misses = [];
      for (const [x, y] of CLEAR_TAPS) {
        expect(await s.page.evaluate(([px, py]) => window.consoleBg.inKeepout(px / innerWidth * 100, py / innerHeight * 100), [x, y]), `(${x},${y}) is clear of the buffer zone`).toBe(false);
        await s.page.evaluate(() => { window.__anchors = []; });
        await s.page.mouse.click(x, y);
        await s.page.waitForTimeout(150); // CASCADE/TERMINAL attach their first window on the next timer tick
        const a = await s.page.evaluate(() => window.__anchors);
        const best = a.length ? Math.min(...a.map((e) => e.d)) : Infinity;
        if (!(best <= 30)) misses.push(`(${x},${y}): ${a.length ? a.map((e) => `${e.cls}@${e.d.toFixed(0)}px`).join(', ') : 'nothing spawned'}`);
        await s.page.waitForTimeout(250);
      }
      expect(misses, 'every tap spawns an effect within 30px of the tap point').toEqual([]);
      expect(s.rec.pageErrors).toEqual([]);
    } finally { await s.context.close(); }
  });

  test('every spawn function honours an origin: starts there, stays on screen, stays out of the keepout', async ({ browser }) => {
    const s = await openSite(browser, site, { viewport: ROOMY });
    try {
      await loadSite(s);
      await watchHost(s.page);
      await untilQuiet(s.page);
      const FNS = ['spawnError', 'spawnWarning', 'spawnWindow', 'spawnMemo', 'spawnFrag', 'spawnArtifact', 'spawnGeoWindow',
        'spawnNetConnect', 'spawnMorseDot', 'spawnFolderRip', 'spawnCascade', 'spawnArtifactPredator', 'spawnHyperspaceReader'];
      await s.page.evaluate(() => window.HyperspaceReader.closeAll()); // READER caps open readers at three
      const out = [];
      for (const fn of FNS) {
        const [x, y] = CLEAR_TAPS[FNS.indexOf(fn) % CLEAR_TAPS.length];
        // A TRACE declines (returns false) when its route planner finds no path for the random destination it
        // rolled, or while another trace is still running (one at a time; an ambient trace started before the
        // auto-spawner was switched off can outlive untilQuiet). Both are a legitimate "nothing to do", so give
        // it several rolls, spaced out so a running trace can finish.
        let r;
        for (let attempt = 0; attempt < (fn === 'spawnNetConnect' ? 20 : 1); attempt++) {
          if (attempt) await s.page.waitForTimeout(400);
          r = await s.page.evaluate(async ([name, px, py]) => {
            const host = document.querySelector('.console-bg-host');
            const before = new Set(host.children);
            const ret = window.consoleBg._demo[name]({ x: px / innerWidth * 100, y: py / innerHeight * 100 });
            await new Promise((res) => setTimeout(res, 30)); // CASCADE/TERMINAL attach on the next timer tick
            const added = [...host.children].filter((n) => !before.has(n) && n.tagName !== 'CANVAS');
            return { ret, n: added.length, d: added.length ? Math.min(...added.map((n) => window.__distTo(n, { x: px, y: py }))) : null };
          }, [fn, x, y]);
          if (r.ret) break;
        }
        out.push({ fn, ...r });
      }
      for (const r of out) {
        expect(r.ret, `${r.fn} returns true`).toBe(true);
        expect(r.n, `${r.fn} added an element`).toBeGreaterThan(0);
        expect(r.d, `${r.fn} starts at the origin`).toBeLessThanOrEqual(30);
      }

      // Clamped: an origin in a corner keeps a popup fully on screen.
      const corner = await s.page.evaluate(() => {
        const host = document.querySelector('.console-bg-host'); const before = new Set(host.children);
        window.consoleBg._demo.spawnError({ x: 99.5, y: 99.5 });
        const el = [...host.children].find((n) => !before.has(n)); const r = el.getBoundingClientRect();
        return { l: r.left, t: r.top, r: r.right, b: r.bottom, w: innerWidth, h: innerHeight };
      });
      expect(corner.l).toBeGreaterThanOrEqual(-0.5); expect(corner.t).toBeGreaterThanOrEqual(-0.5);
      expect(corner.r).toBeLessThanOrEqual(corner.w + 0.5); expect(corner.b).toBeLessThanOrEqual(corner.h + 0.5);

      // Pushed out: an origin just outside the buffer zone, beside the lockup, never lets a window overlap it.
      // READER allows three open readers at a time, so close the ones already open before spawning two more.
      await s.page.evaluate(() => window.HyperspaceReader.closeAll());
      const beside = await s.page.evaluate(() => {
        const lock = document.querySelector('.lockup').getBoundingClientRect();
        const pad = window.consoleBg._demo.KEEPOUT_BUFFER;
        const res = [];
        for (const name of ['spawnError', 'spawnWarning', 'spawnMemo', 'spawnFolderRip', 'spawnGeoWindow', 'spawnHyperspaceReader']) {
          for (const [px, py] of [[lock.left - pad - 2, (lock.top + lock.bottom) / 2], [(lock.left + lock.right) / 2, lock.bottom + pad + 2]]) {
            const host = document.querySelector('.console-bg-host'); const before = new Set(host.children);
            window.consoleBg._demo[name]({ x: px / innerWidth * 100, y: py / innerHeight * 100 });
            const el = [...host.children].find((n) => !before.has(n)); const r = el.getBoundingClientRect();
            const ov = Math.max(0, Math.min(r.right, lock.right) - Math.max(r.left, lock.left)) * Math.max(0, Math.min(r.bottom, lock.bottom) - Math.max(r.top, lock.top));
            res.push({ name, ov, on: r.left >= -0.5 && r.top >= -0.5 && r.right <= innerWidth + 0.5 && r.bottom <= innerHeight + 0.5 });
          }
        }
        return res;
      });
      for (const b of beside) {
        expect(b.ov, `${b.name} beside the lockup must not overlap it`).toBe(0);
        expect(b.on, `${b.name} stays on screen`).toBe(true);
      }
      expect(s.rec.pageErrors).toEqual([]);
    } finally { await s.context.close(); }
  });

  test('READER: a Hyperspace Reader spawns at an origin, shows the threshold bar and the scrolling details, and shuts down cleanly', async ({ browser }) => {
    const s = await openSite(browser, site, { viewport: ROOMY });
    try {
      await loadSite(s);
      await watchHost(s.page);
      await untilQuiet(s.page);
      await s.page.evaluate(() => window.HyperspaceReader.closeAll()); // an ambient reader must not be mistaken for ours
      const [x, y] = CLEAR_TAPS[0];
      const r = await s.page.evaluate(([px, py]) => {
        const host = document.querySelector('.console-bg-host');
        const before = new Set(host.children);
        const ret = window.consoleBg._demo.spawnHyperspaceReader({ x: px / innerWidth * 100, y: py / innerHeight * 100 });
        const el = [...host.children].find((n) => !before.has(n) && n.classList.contains('hsr'));
        return { ret, inHost: !!el, d: el ? window.__distTo(el, { x: px, y: py }) : null, lib: !!window.Hyperspace && !!window.HyperspaceReader };
      }, [x, y]);
      expect(r.lib, 'hyperspace.js and hyperspace-reader.js load before console-bg.js').toBe(true);
      expect(r.ret, 'spawnHyperspaceReader returns true').toBe(true);
      expect(r.inHost, 'the reader opens inside the Cyberspace host').toBe(true);
      expect(r.d, 'the reader starts at the origin').toBeLessThanOrEqual(30);

      const win = s.page.locator('.console-bg-host .hsr');
      await expect(win).toHaveAttribute('data-state', 'live', { timeout: 3000 });
      await expect(win.locator('.hsr-thresh-head')).toContainText('DIMENSIONAL THRESHOLD');
      const sample = () => s.page.evaluate(() => {
        const w = document.querySelector('.console-bg-host .hsr');
        return {
          threshold: Number(w.dataset.threshold),
          clip: w.querySelector('.hsr-bar-fill').style.clipPath,
          scroll: w.querySelector('.hsr-details-inner').style.transform,
          text: w.querySelector('.hsr-details-inner').textContent,
        };
      });
      await s.page.waitForTimeout(1200);
      const a = await sample();
      await s.page.waitForTimeout(700);
      const b = await sample();
      expect(a.threshold, 'threshold bar is filled').toBeGreaterThan(0);
      expect(a.clip).toMatch(/^inset\(/);
      expect(b.threshold, 'threshold bar moves').not.toBe(a.threshold);
      expect(b.scroll, 'details scroll').toMatch(/^translateY\(/);
      expect(b.text, 'details advance').not.toBe(a.text);

      // Shutdown: the ttl (6-11 s) closes it on its own; the power-down line prints and its DOM is removed.
      await expect(win).toHaveAttribute('data-state', /closing|off/, { timeout: 12_000 });
      await expect(win.locator('.hsr-foot')).toContainText('POWERING DOWN', { timeout: 2000 });
      await expect(s.page.locator('.console-bg-host .hsr')).toHaveCount(0, { timeout: 3000 });
      expect(await s.page.evaluate(() => window.HyperspaceReader.active())).toBe(0);
      expect(s.rec.pageErrors).toEqual([]);
    } finally { await s.context.close(); }
  });

  test('a tap inside the keepout buffer zone spawns nothing (no effect, no sparks)', async ({ browser }) => {
    const s = await openSite(browser, site, { viewport: { width: 1000, height: 700 } });
    try {
      await loadSite(s);
      await watchHost(s.page);
      await untilQuiet(s.page);
      const pts = await s.page.evaluate(() => {
        const lock = document.querySelector('.lockup').getBoundingClientRect();
        const word = document.getElementById('site-name').getBoundingClientRect();
        const motto = document.getElementById('site-motto').getBoundingClientRect();
        const pad = window.consoleBg._demo.KEEPOUT_BUFFER;
        return [
          [(word.left + word.right) / 2, (word.top + word.bottom) / 2],     // on the wordmark
          [(motto.left + motto.right) / 2, (motto.top + motto.bottom) / 2], // on the motto
          [lock.left - pad / 2, (lock.top + lock.bottom) / 2],             // in the margin, left of the lockup
          [(lock.left + lock.right) / 2, lock.top - pad / 2],              // in the margin, above it
          [lock.right + pad / 2, lock.bottom + pad / 2],                   // in the margin, bottom-right corner
        ];
      });
      for (const [x, y] of pts) {
        expect(await s.page.evaluate(([px, py]) => window.consoleBg.inKeepout(px / innerWidth * 100, py / innerHeight * 100), [x, y]), `(${x.toFixed(0)},${y.toFixed(0)}) is in the zone`).toBe(true);
        await s.page.mouse.click(x, y);
      }
      await s.page.waitForTimeout(600);
      expect(await s.page.evaluate(() => window.__added), 'taps in the buffer zone must not spawn effects').toBe(0);
      expect(await s.page.evaluate(() => window.consoleBg._demo.sparkStats().frames), 'taps in the buffer zone must not spark').toBe(0);
      expect(await s.page.locator('canvas.cyberspace-surge').count(), 'no spark canvas created').toBe(0);
    } finally { await s.context.close(); }
  });

  test('the tap sonic pulse draws on one pooled click-through canvas, animates, then goes idle', async ({ browser }) => {
    const s = await openSite(browser, site, { viewport: { width: 1000, height: 700 } });
    try {
      await loadSite(s);
      await watchHost(s.page);
      await untilQuiet(s.page);
      await s.page.mouse.click(150, 120);
      const live = await s.page.evaluate(async () => {
        await new Promise((r) => requestAnimationFrame(() => requestAnimationFrame(r)));
        const cv = document.querySelector('canvas.cyberspace-surge');
        const cs = getComputedStyle(cv);
        const px = cv.getContext('2d').getImageData(0, 0, cv.width, cv.height).data;
        let lit = 0; for (let i = 3; i < px.length; i += 4) if (px[i]) lit++;
        return { stats: window.consoleBg._demo.sparkStats(), state: cv.dataset.state, lit, pe: cs.pointerEvents, pos: cs.position, z: cs.zIndex, parent: cv.parentElement.tagName };
      });
      expect(live.stats.running).toBe(true);
      expect(live.stats.flashes, 'the glow + ring is alive').toBe(1);
      expect(live.state).toBe('running');
      expect(live.lit, 'pixels drawn').toBeGreaterThan(5);
      expect({ pe: live.pe, pos: live.pos, z: live.z, parent: live.parent }).toEqual({ pe: 'none', pos: 'fixed', z: '1', parent: 'BODY' });

      await s.page.mouse.click(860, 560); // a second tap reuses the same canvas
      expect(await s.page.locator('canvas.cyberspace-surge').count(), 'one pooled canvas').toBe(1);

      // The hint fades out within ~200ms; then the loop stops and the canvas is cleared.
      await s.page.waitForFunction(() => !window.consoleBg._demo.sparkStats().running, null, { timeout: 1000 });
      const idle = await s.page.evaluate(async () => {
        const cv = document.querySelector('canvas.cyberspace-surge');
        const f0 = window.consoleBg._demo.sparkStats().frames;
        await new Promise((r) => setTimeout(r, 300));
        const px = cv.getContext('2d').getImageData(0, 0, cv.width, cv.height).data;
        let lit = 0; for (let i = 3; i < px.length; i += 4) if (px[i]) lit++;
        return { s: window.consoleBg._demo.sparkStats(), f0, state: cv.dataset.state, lit };
      });
      expect(idle.s.sparks + idle.s.flashes, 'nothing left alive').toBe(0);
      expect(idle.s.frames, 'no frames drawn while idle').toBe(idle.f0);
      expect(idle.state).toBe('idle');
      expect(idle.lit, 'canvas cleared').toBe(0);

      // The canvas never blocks the page: the link under it still takes the click.
      const hit = await s.page.evaluate(() => { const b = document.querySelector('.link-btn').getBoundingClientRect(); return document.elementFromPoint(b.left + b.width / 2, b.top + b.height / 2).className; });
      expect(hit).toMatch(/link-btn/);
      expect(s.rec.pageErrors).toEqual([]);
    } finally { await s.context.close(); }
  });

  test('prefers-reduced-motion: a tap gives only a brief flash (no sparks), and the effect still spawns', async ({ browser }) => {
    const s = await openSite(browser, site, { viewport: { width: 1000, height: 700 } });
    try {
      await s.page.emulateMedia({ reducedMotion: 'reduce' });
      await loadSite(s);
      await watchHost(s.page);
      await untilQuiet(s.page);
      // Straight from the engine: one stationary flash, no particles, and it is over within ~200ms.
      const st = await s.page.evaluate(() => { window.consoleBg.spawnSparkBurst(20, 20); return window.consoleBg._demo.sparkStats(); });
      expect(st.reducedMotion).toBe(true);
      expect(st.sparks, 'no particles').toBe(0);
      expect(st.flashes, 'one flash').toBe(1);
      await s.page.waitForFunction(() => !window.consoleBg._demo.sparkStats().running, null, { timeout: 600 });
      // Through a real tap: the flash draws (frames advance), still no particles, the effect still spawns.
      const f0 = await s.page.evaluate(() => window.consoleBg._demo.sparkStats().frames);
      await s.page.mouse.click(150, 120);
      expect(await s.page.evaluate(() => window.consoleBg._demo.sparkStats().sparks), 'no particles on a tap').toBe(0);
      await s.page.waitForFunction(() => !window.consoleBg._demo.sparkStats().running, null, { timeout: 600 });
      expect(await s.page.evaluate(() => window.consoleBg._demo.sparkStats().frames), 'the flash drew').toBeGreaterThan(f0);
      expect(await s.page.evaluate(() => window.__added), 'the effect itself still spawns').toBeGreaterThan(0);
    } finally { await s.context.close(); }
  });
});
