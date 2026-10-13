// ryandebraal.com: a large résumé page whose heavy art (theme backgrounds, portrait, moon) and fonts come from
// the MindAttic.Components package on jsDelivr — fetched lazily, only when a theme/lightbox actually needs them.
import { test, expect } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';
import { SITES, SITES_ROOT, TEST_MODE, EXPECTED_TAG, CDN_BASE } from '../../lib/paths.mjs';
import { absOf, loadManifest } from '../../lib/walk.mjs';
import { openSite, loadSite } from '../../lib/site-session.mjs';

const site = SITES['ryandebraal.com'];
const html = TEST_MODE === 'live' ? null : fs.readFileSync(path.join(SITES_ROOT, site.dir, 'index.htm'), 'utf8');
const BASE = CDN_BASE(EXPECTED_TAG) + 'themes/';
const sharedReqs = (s) => s.rec.requests.filter((r) => r.url.startsWith('https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Components@'));
const relOf = (url) => url.replace(/^.*MindAttic\.Components@[^/]+\/web\//, '');

test.describe('ryandebraal.com — static analysis', () => {
  test.skip(!html, 'reads the HTML from the working tree (local mode)');

  test('no embedded base64 JPEG/PNG/WOFF2 payloads are left in the page', () => {
    expect(html.match(/\/9j\/4AAQ/g) ?? [], 'base64 JPEG data').toEqual([]);
    const rasters = html.match(/data:image\/(?:png|jpeg|gif|webp);base64,[A-Za-z0-9+/=]*/g) ?? [];
    expect(rasters.filter((u) => u.length > 2048).map((u) => `${u.slice(0, 30)}… ${u.length} chars`), 'base64 raster data URIs over 2 KB (tiny icons are allowed)').toEqual([]);
    expect(html.match(/data:font\/woff2?;base64,/g) ?? [], 'base64 fonts').toEqual([]);
  });

  test('theme background series are built from ASSET_BASE and every image in them exists in the package', () => {
    const base = html.match(/const ASSET_BASE = '([^']+)'/)?.[1];
    expect(base, 'ASSET_BASE constant').toBe(BASE);
    const series = [...html.matchAll(/bgSet\('([a-z]+)', (\d+)\)/g)].map((m) => ({ theme: m[1], count: Number(m[2]) }));
    expect(series.map((s) => s.theme).sort()).toEqual(['sakura', 'sunset']);
    const manifest = loadManifest(); const listed = new Set((manifest?.files ?? []).map((f) => f.path));
    const problems = [];
    for (const { theme, count } of series) {
      for (let i = 1; i <= count; i++) {
        const rel = `themes/${theme[0].toUpperCase() + theme.slice(1)}/${theme}-${String(i).padStart(2, '0')}.jpg`;
        if (!fs.existsSync(absOf(rel))) problems.push(`${rel}: not in package`);
        else if (!listed.has(rel)) problems.push(`${rel}: not in assets-manifest.json`);
      }
    }
    expect(problems).toEqual([]);
  });

  test('the avatar is preloaded with high priority and has explicit width/height (no layout shift)', () => {
    expect(html).toMatch(/<link rel="preload" as="image" href="[^"]+ryan-avatar\.png" fetchpriority="high">/);
    expect(html).toMatch(/<img src="[^"]+ryan-avatar\.png" width="28" height="28"/);
  });
});

test.describe('ryandebraal.com — runtime asset loading', () => {
  test('first paint requests only what is above the fold: the Outfit latin font and the avatar', async ({ browser }) => {
    const s = await openSite(browser, site);
    try {
      await loadSite(s, { settleMs: 800 });
      const files = new Set(sharedReqs(s).map((r) => relOf(r.url)));
      expect([...files].sort()).toEqual(['assets/ryandebraal.com/images/ryan-avatar.png', 'components/OutfitFont/fonts/outfit-latin.woff2']);
      expect(sharedReqs(s).every((r) => r.status === 200)).toBe(true);
    } finally { await s.context.close(); }
  });

  for (const [theme, expectPattern] of [['sakura', /web\/themes\/Sakura\/sakura-\d\d\.jpg$/], ['sunset', /web\/themes\/Sunset\/sunset-\d\d\.jpg$/], ['noir', /web\/themes\/Noir\/noir-moon\.png$/]]) {
    test(`switching to the ${theme} theme fetches its artwork from the CDN (200)`, async ({ browser }) => {
      const s = await openSite(browser, site);
      try {
        await loadSite(s, { settleMs: 500 });
        await s.page.evaluate(() => { if (typeof stopThemeRotation === 'function') stopThemeRotation(); }); // stop auto-rotation so only this theme loads
        const before = sharedReqs(s).length;
        await s.page.evaluate((t) => setTheme(t), theme);
        await expect.poll(() => sharedReqs(s).slice(before).filter((r) => expectPattern.test(r.url) && r.status === 200).length, { timeout: 15_000, message: `no ${theme} artwork request succeeded` }).toBeGreaterThan(0);
        expect(await s.page.evaluate(() => document.documentElement.getAttribute('data-theme'))).toBe(theme);
        expect(s.rec.badStatus).toEqual([]);
        expect(s.rec.pageErrors).toEqual([]);
      } finally { await s.context.close(); }
    });
  }

  test('the avatar lightbox opens, loads the portrait from the CDN, and closes', async ({ browser }) => {
    const s = await openSite(browser, site);
    try {
      await loadSite(s, { settleMs: 500 });
      expect(sharedReqs(s).some((r) => /ryan-portrait\.png$/.test(r.url)), 'portrait must not be fetched before the lightbox opens').toBe(false);
      await s.page.locator('img[alt*="open profile photo"]').click();
      await expect(s.page.locator('#avatar-lightbox')).toHaveClass(/is-open/);
      await expect.poll(() => sharedReqs(s).filter((r) => /ryan-portrait\.png$/.test(r.url) && r.status === 200).length, { timeout: 10_000 }).toBeGreaterThan(0);
      const box = await s.page.locator('#avatar-lightbox .lb-img').boundingBox();
      expect(box.width).toBeGreaterThan(100);
      await s.page.locator('#avatar-lightbox').click({ position: { x: 5, y: 5 } });
      await expect(s.page.locator('#avatar-lightbox')).not.toHaveClass(/is-open/);
    } finally { await s.context.close(); }
  });

  test('PDF export loads html2pdf on demand (not at page load)', async ({ browser }) => {
    const s = await openSite(browser, site);
    try {
      await loadSite(s, { settleMs: 500 });
      const pdfReq = () => s.rec.requests.filter((r) => /html2pdf\.js@/.test(r.url));
      expect(pdfReq(), 'html2pdf must not be fetched at page load').toEqual([]);
      expect(await s.page.evaluate(() => typeof window.html2pdf)).toBe('undefined');
      await s.page.evaluate(() => { exportPDF(); });
      await s.page.waitForFunction(() => typeof window.html2pdf === 'function', null, { timeout: 30_000 });
      expect(pdfReq().length).toBeGreaterThan(0);
      expect(pdfReq().every((r) => r.host === 'cdn.jsdelivr.net')).toBe(true);
      if (TEST_MODE === 'local') expect(await s.page.evaluate(() => window.__html2pdfStub)).toBe(true); // hermetic: the real 880 KB library is stubbed
    } finally { await s.context.close(); }
  });
});
