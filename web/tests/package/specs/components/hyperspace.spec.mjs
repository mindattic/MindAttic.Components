// Hyperspace component: the shape library, the Hyperspace Reader, and the Hyperspace page that builds its
// gallery from the library.
//
// The page lives in the Hyperspace folder of MindAttic.Web (SITES_ROOT/Hyperspace) and is opened as a local file, the way
// its README says it must keep working. Its pinned jsDelivr URL for hyperspace.js is answered from this working
// tree (so the test passes before the tag is published); three.js is fetched from its CDN because the gallery
// needs it and MindAttic.Web does not carry a copy; Google Fonts are blocked (the page falls back to system fonts).
import { test, expect } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';
import { createRequire } from 'node:module';
import { pathToFileURL } from 'node:url';
import { SHARED_ROOT, SITES_ROOT } from '../../lib/paths.mjs';

const require = createRequire(import.meta.url);
const COMPONENT = path.join(SHARED_ROOT, 'Components', 'Hyperspace');
const LIB = path.join(COMPONENT, 'hyperspace.js');
const HARNESS = pathToFileURL(path.join(COMPONENT, 'index.htm')).href;
const PAGE_DIR = path.join(SITES_ROOT, 'Hyperspace');
const PAGE = path.join(PAGE_DIR, 'index.htm');
const COUNT = 159;   // shapes in the library; update when exhibits are added

// The gallery is WebGL; make sure headless Chrome has a software GL to give it.
test.use({ launchOptions: { ignoreDefaultArgs: ['--hide-scrollbars'], args: ['--enable-unsafe-swiftshader', '--use-angle=swiftshader'] } });

function trackErrors(page) {
  const errors = [];
  page.on('pageerror', (e) => errors.push(String(e)));
  page.on('console', (m) => { if (m.type() === 'error') errors.push(m.text()); });
  return errors;
}

test.describe('shape library', () => {
  const H = require(LIB);

  test('loads every shape with a complete record and a unique stable id', () => {
    expect(H.count).toBe(COUNT);
    expect(H.shapes).toHaveLength(COUNT);
    const ids = H.ids();
    expect(new Set(ids).size).toBe(COUNT);
    for (const s of H.shapes) {
      expect(s.id, s.name).toMatch(/^[a-z0-9]+(-[a-z0-9]+)*$/);
      expect(H.get(s.id)).toBe(s);
      expect(H.get(s.index)).toBe(s);
      expect(Object.isFrozen(s)).toBe(true);
      for (const k of ['name', 'tag', 'family', 'familyLabel', 'color', 'schlafli', 'blurb', 'article']) {
        expect(typeof s[k], `${s.id}.${k}`).toBe('string');
        expect(s[k].length, `${s.id}.${k}`).toBeGreaterThan(0);
      }
      expect(Number.isInteger(s.dim) && s.dim >= 3, `${s.id}.dim`).toBe(true);
      expect(s.rot.length, `${s.id}.rot`).toBeGreaterThan(0);
      expect(s.cites.length, `${s.id}.cites`).toBe(s.refs.length);
      expect(H.families[s.family].color).toBe(s.color);
      expect(s.row * 10 + s.col).toBe(s.index);
    }
    expect(() => H.get('no-such-shape')).toThrow(RangeError);
    expect(() => H.get(COUNT)).toThrow(RangeError);
  });

  test('every shape has geometry and projects to finite 2D and 3D coordinates', () => {
    // collect problems and assert once: an expect() per coordinate would be tens of thousands of calls
    const problems = [];
    for (const s of H.shapes) {
      const g = H.geometry(s.id);
      if (!g.verts.length || !g.edges.length) problems.push(`${s.id}: empty geometry`);
      if (g.edges.some(([a, b]) => !(a < g.verts.length && b < g.verts.length))) problems.push(`${s.id}: edge index out of range`);
      const m = H.extent(s.id);
      if (!(Number.isFinite(m) && m > 0)) problems.push(`${s.id}: extent ${m}`);
      for (const t of [0, 1.7, 13.3, 250]) {
        const p2 = H.project2D(s.id, t);
        const p3 = H.project3D(s.id, t);
        if (p2.length !== g.verts.length || p3.length !== g.verts.length) problems.push(`${s.id} @${t}: point count`);
        if (!p2.every((p) => p.every(Number.isFinite))) problems.push(`${s.id} @${t}: non-finite 2D coordinate`);
        if (!p3.every((p) => p.every(Number.isFinite))) problems.push(`${s.id} @${t}: non-finite 3D coordinate`);
        // extent() samples the rotation sweep, so a pose between samples can reach a few percent further
        if (p2.some(([x, y]) => Math.hypot(x, y) > 1.1)) problems.push(`${s.id} @${t}: 2D point outside the unit disc`);
        if (p3.some((p) => Math.hypot(...p) > 1.1)) problems.push(`${s.id} @${t}: 3D point beyond the normalised radius`);
      }
    }
    expect(problems).toEqual([]);
  });

  test('constructed exhibits draw exactly the vertices and edges their plaques state', () => {
    const exact = [
      '120-cell', 'rectified-5-cell', 'truncated-5-cell', 'cantellated-5-cell', 'runcinated-5-cell',
      'rectified-tesseract', 'truncated-tesseract', 'cantellated-tesseract', 'runcinated-tesseract',
      'rectified-24-cell', 'snub-24-cell',
      'rectified-penteract', 'truncated-penteract', 'cantellated-penteract', 'runcinated-penteract',
      'stericated-penteract', 'omnitruncated-5-simplex', '5-demicube',
      '10-cube', '10-simplex', '8-orthoplex', '9-orthoplex',
      'bitruncated-5-cell', 'cantitruncated-5-cell', 'runcitruncated-5-cell', 'omnitruncated-5-cell',
      'bitruncated-tesseract', 'cantitruncated-tesseract', 'runcitruncated-tesseract', 'omnitruncated-tesseract',
      'truncated-24-cell', 'bitruncated-24-cell', 'cantitruncated-24-cell', 'runcitruncated-24-cell', 'omnitruncated-24-cell',
      'grand-antiprism', 'square-antiprismatic-prism', 'pentagonal-antiprismatic-prism',
      'icosahedral-120-cell', 'small-stellated-120-cell', 'great-120-cell', 'grand-120-cell', 'great-stellated-120-cell',
      'grand-stellated-120-cell', 'great-grand-120-cell', 'great-icosahedral-120-cell', 'grand-600-cell',
      'great-grand-stellated-120-cell',
      'e6-polytope-221', 'e6-polytope-122', 'e7-polytope-321', 'e7-polytope-231', 'e7-polytope-132',
      '3x4x5-triaprism', '3x3x3x3-tetraprism',
    ];
    const wrong = exact.map((id) => {
      const s = H.get(id), st = H.stats(id);
      return st.vertices === s.stated.vertices && st.edges === s.stated.edges ? null : `${id}: drew ${st.vertices}V ${st.edges}E, plaque ${s.stated.vertices}V ${s.stated.edges}E`;
    }).filter(Boolean);
    expect(wrong).toEqual([]);
    // the star polychora fall into the four known edge arrangements
    const edge = (id) => { const g = H.geometry(id), [a, b] = g.edges[0]; return Math.hypot(...g.verts[a].map((x, k) => x - g.verts[b][k])).toFixed(4); };
    expect(['icosahedral-120-cell', 'great-120-cell', 'grand-120-cell'].map(edge)).toEqual(['0.6180', '0.6180', '0.6180']);
    expect(['small-stellated-120-cell', 'great-grand-120-cell'].map(edge)).toEqual(['1.0000', '1.0000']);
    expect(['great-stellated-120-cell', 'grand-stellated-120-cell', 'great-icosahedral-120-cell', 'grand-600-cell'].map(edge))
      .toEqual(['1.6180', '1.6180', '1.6180', '1.6180']);
    // E8 Gosset vertex counts from the full Wythoff orbit, before the frame is cut down
    const { wythoffVerts, diagrams } = H.gen;
    expect(wythoffVerts(8, diagrams.E8, [1, 0, 0, 0, 0, 0, 0, 0]).length).toBe(2160);
    expect(wythoffVerts(8, diagrams.E8, [0, 0, 0, 0, 0, 0, 0, 1]).length).toBe(17280);
  });

  test('family colours stay distinct for normal and red-green colour-blind vision', () => {
    // OKLab ΔE×100 with the Machado (2009) severity-1 protan/deutan transforms
    const lin = (h) => [1, 3, 5].map((i) => { const s = parseInt(h.slice(i, i + 2), 16) / 255; return s <= 0.04045 ? s / 12.92 : ((s + 0.055) / 1.055) ** 2.4; });
    const M = { protan: [[0.152286, 1.052583, -0.204868], [0.114503, 0.786281, 0.099216], [-0.003882, -0.048116, 1.051998]],
      deutan: [[0.367322, 0.860646, -0.227968], [0.280085, 0.672501, 0.047413], [-0.01182, 0.04294, 0.968881]] };
    const lab = ([r, g, b]) => { const l = Math.cbrt(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b), m = Math.cbrt(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b), s = Math.cbrt(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b);
      return [0.2104542553 * l + 0.793617785 * m - 0.0040720468 * s, 1.9779984951 * l - 2.428592205 * m + 0.4505937099 * s, 0.0259040371 * l + 0.7827717662 * m - 0.808675766 * s]; };
    const sim = (h, k) => { const c = lin(h); return k ? M[k].map((row) => Math.min(1, Math.max(0, row[0] * c[0] + row[1] * c[1] + row[2] * c[2]))) : c; };
    const dE = (a, b, k) => { const p = lab(sim(a, k)), q = lab(sim(b, k)); return 100 * Math.hypot(p[0] - q[0], p[1] - q[1], p[2] - q[2]); };
    const cols = Object.values(H.families).map((f) => f.color), close = [];
    for (let i = 0; i < cols.length; i++) for (let j = i + 1; j < cols.length; j++) {
      if (dE(cols[i], cols[j]) < 15) close.push(`${cols[i]}/${cols[j]} normal`);
      for (const k of ['protan', 'deutan']) if (dE(cols[i], cols[j], k) < 8) close.push(`${cols[i]}/${cols[j]} ${k}`);
    }
    expect(close).toEqual([]);
  });

  test('loads in a browser and draws every shape onto a canvas', async ({ page }) => {
    const errors = trackErrors(page);
    await page.goto(HARNESS);
    const res = await page.evaluate(() => {
      const H = window.Hyperspace, c = document.createElement('canvas');
      c.width = c.height = 64;
      const ctx = c.getContext('2d'), blank = [];
      for (const s of H.shapes) {
        ctx.clearRect(0, 0, 64, 64);
        H.draw(ctx, s.id, 3.1, { size: 64 });
        const d = ctx.getImageData(0, 0, 64, 64).data;
        let lit = 0; for (let i = 3; i < d.length; i += 4) if (d[i]) lit++;
        if (!lit) blank.push(s.id);
      }
      return { count: H.count, blank };
    });
    expect(res.count).toBe(COUNT);
    expect(res.blank).toEqual([]);
    expect(errors).toEqual([]);
  });
});

test.describe('Hyperspace Reader', () => {
  test('opens, animates the threshold bar, scrolls the details and closes with its DOM removed', async ({ page }) => {
    const errors = trackErrors(page);
    await page.goto(HARNESS);
    await page.evaluate(() => { window.__h = window.HyperspaceReader.show({ x: 40, y: 120, shape: 'tesseract' }); });
    const win = page.locator('.hsr');
    await expect(win).toHaveCount(1);
    await expect(win).toHaveAttribute('data-shape', 'tesseract');
    await expect(page.locator('#hsr-style')).toHaveCount(1);
    await expect(win).toHaveAttribute('data-state', 'live', { timeout: 3000 });

    // threshold bar: labelled, filled, and moving
    await expect(win.locator('.hsr-thresh-head')).toContainText('DIMENSIONAL THRESHOLD');
    const sample = () => page.evaluate(() => {
      const w = document.querySelector('.hsr');
      return {
        threshold: Number(w.dataset.threshold),
        clip: w.querySelector('.hsr-bar-fill').style.clipPath,
        scroll: w.querySelector('.hsr-details-inner').style.transform,
        text: w.querySelector('.hsr-details-inner').textContent,
        values: [...w.querySelectorAll('.hsr-ro-v')].map((e) => e.textContent).join('|'),
      };
    });
    await page.waitForTimeout(1200);
    const a = await sample();
    await page.waitForTimeout(700);
    const b = await sample();
    expect(a.threshold).toBeGreaterThan(0);
    expect(a.clip).toMatch(/^inset\(/);
    expect(b.threshold).not.toBe(a.threshold);
    expect(b.values).not.toBe(a.values);

    // details: a span of text that scrolls and advances, printing the shape's own details
    expect(await win.locator('span.hsr-details-inner').count()).toBe(1);
    expect(b.scroll).toMatch(/^translateY\(/);
    expect(b.text).not.toBe(a.text);
    await page.waitForTimeout(1500);
    const c = await sample();
    expect(a.text + b.text + c.text).toMatch(/Tesseract|tesseract|\{4,3,3\}/);

    // readouts and flags
    await expect(win.locator('.hsr-ro')).toHaveCount(5);
    await expect(win.locator('.hsr-flag').first()).toHaveText('UNRESOLVED');

    // the scope canvas has something drawn on it
    const lit = await page.evaluate(() => {
      const cv = document.querySelector('.hsr canvas'), d = cv.getContext('2d').getImageData(0, 0, cv.width, cv.height).data;
      let n = 0; for (let i = 3; i < d.length; i += 4) if (d[i] > 40) n++; return n;
    });
    expect(lit).toBeGreaterThan(200);

    // shutdown: freezes, prints the power-down line, folds away, removes itself
    // (sampled inside the page, so test-runner latency cannot outlast the 1.1 s shutdown)
    const shutdown = await page.evaluate(async () => {
      const w = document.querySelector('.hsr'), vals = () => [...w.querySelectorAll('.hsr-ro-v')].map((e) => e.textContent);
      const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
      window.__closing = window.__h.close();
      const at0 = { state: w.dataset.state, status: w.querySelector('.hsr-status').textContent, values: vals() };
      await sleep(200);
      const at200 = { values: vals() };
      await sleep(500);
      const at700 = { state: w.dataset.state, foot: w.querySelector('.hsr-foot').textContent, attached: w.isConnected };
      return { at0, at200, at700 };
    });
    expect(shutdown.at0.state).toBe('closing');
    expect(shutdown.at0.status).toBe('SHUTDOWN');
    expect(shutdown.at200.values).toEqual(shutdown.at0.values);          // readouts frozen
    expect(shutdown.at700.foot).toContain('POWERING DOWN');               // power-down line printed
    expect(shutdown.at700.state).toBe('off');                             // folding away
    await page.evaluate(() => window.__closing);
    await expect(page.locator('.hsr')).toHaveCount(0);
    expect(await page.evaluate(() => [window.HyperspaceReader.active(), window.__h.closed])).toEqual([0, true]);
    expect(errors).toEqual([]);
  });

  test('ttl closes it on its own, and closeAll() clears several', async ({ page }) => {
    await page.goto(HARNESS);
    await page.evaluate(() => { window.HyperspaceReader.show({ shape: 0, ttl: 600 }); });
    await expect(page.locator('.hsr')).toHaveCount(1);
    await expect(page.locator('.hsr')).toHaveCount(0, { timeout: 8000 });
    await page.evaluate(() => { for (const s of ['24-cell', 'hopf-fibration', 'e8-root-system']) window.HyperspaceReader.show({ shape: s }); });
    await expect(page.locator('.hsr')).toHaveCount(3);
    await page.evaluate(() => window.HyperspaceReader.closeAll());
    await expect(page.locator('.hsr')).toHaveCount(0);
  });

  test('honours prefers-reduced-motion: no scrolling, no jitter, plain fade', async ({ browser }) => {
    const ctx = await browser.newContext({ reducedMotion: 'reduce' });
    const page = await ctx.newPage();
    const errors = trackErrors(page);
    await page.goto(HARNESS);
    await page.evaluate(() => { window.__h = window.HyperspaceReader.show({ shape: 'penteract' }); });
    const win = page.locator('.hsr');
    await expect(win).toHaveClass(/hsr--reduced/);
    await expect(win).toHaveAttribute('data-state', 'live', { timeout: 3000 });
    await page.waitForTimeout(600);
    const t1 = await win.getAttribute('data-threshold');
    await page.waitForTimeout(500);
    expect(await win.getAttribute('data-threshold')).toBe(t1);
    expect(await win.locator('.hsr-details-inner').evaluate((e) => e.style.transform)).toBe('');
    await page.evaluate(() => window.__h.close());
    await expect(page.locator('.hsr')).toHaveCount(0);
    expect(errors).toEqual([]);
    await ctx.close();
  });

  test('returns null without the library', async ({ page }) => {
    await page.setContent('<!doctype html><body></body>');
    await page.addScriptTag({ path: path.join(COMPONENT, 'hyperspace-reader.js') });
    expect(await page.evaluate(() => window.HyperspaceReader.show({}))).toBeNull();
  });
});

test.describe('Hyperspace page', () => {
  test.skip(!fs.existsSync(PAGE), `Hyperspace page not found at ${PAGE}`);

  async function openPage(browser, { cdn }) {
    const ctx = await browser.newContext({ viewport: { width: 1366, height: 900 } });
    await ctx.route('**/*', (route) => {
      const url = new URL(route.request().url());
      if (url.protocol === 'file:' || url.protocol === 'data:' || url.protocol === 'blob:') return route.continue();
      const m = url.pathname.match(/^\/gh\/mindattic\/MindAttic\.Components@V\d+\/web\/(.+)$/);
      if (url.host === 'cdn.jsdelivr.net' && m) {
        const abs = path.join(SHARED_ROOT, decodeURIComponent(m[1]));
        if (cdn && fs.existsSync(abs)) return route.fulfill({ status: 200, body: fs.readFileSync(abs), headers: { 'content-type': 'text/javascript; charset=utf-8', 'access-control-allow-origin': '*' } });
        return route.fulfill({ status: 404, body: 'not published' });
      }
      if (url.host === 'cdn.jsdelivr.net' && url.pathname.startsWith('/npm/three@')) return route.continue();
      return route.abort('blockedbyclient');
    });
    const page = await ctx.newPage();
    const errors = [];
    page.on('pageerror', (e) => errors.push(String(e)));
    page.on('console', (m) => { if (m.type() === 'error' && !/Failed to load resource/.test(m.text())) errors.push(m.text()); });
    await page.goto(pathToFileURL(PAGE).href, { waitUntil: 'load' });
    return { ctx, page, errors };
  }

  test('pins the library to a whole-number MindAttic.Components tag that exists in this tree', () => {
    const html = fs.readFileSync(PAGE, 'utf8');
    const pins = [...html.matchAll(/cdn\.jsdelivr\.net\/gh\/mindattic\/MindAttic\.Components@([^/"']+)\/web\/([^"']+)/g)];
    expect(pins.length).toBeGreaterThan(0);
    for (const [, tag, file] of pins) {
      expect(tag).toMatch(/^V\d+$/);
      expect(fs.existsSync(path.join(SHARED_ROOT, file)), file).toBe(true);
    }
    expect(html).not.toMatch(/const EXHIBITS = \[/);   // the exhibits are not copied back into the page
  });

  for (const cdn of [true, false]) {
    test(`renders its explorers and gallery from the library (${cdn ? 'pinned CDN file' : 'local ../MindAttic.Components fallback'})`, async ({ browser }) => {
      test.setTimeout(120_000);
      const { ctx, page, errors } = await openPage(browser, { cdn });
      expect(await page.evaluate(() => [window.Hyperspace && window.Hyperspace.count, typeof window.HyperGallery])).toEqual([COUNT, 'object']);

      // the field-guide canvases draw
      for (const id of ['heroCanvas', 'sliceCanvas', 'projCanvas', 'ladder2']) {
        const lit = await page.evaluate((id) => {
          const cv = document.getElementById(id), d = cv.getContext('2d').getImageData(0, 0, cv.width, cv.height).data;
          let n = 0; for (let i = 3; i < d.length; i += 4) if (d[i]) n++; return n;
        }, id);
        expect(lit, id).toBeGreaterThan(50);
      }
      expect(await page.locator('#pVtx').textContent()).toBe('32');

      // the gallery boots: every exhibit built, then the Enter button
      await page.evaluate(() => window.HyperGallery.enter());
      await expect(page.locator('#enterBtn')).toBeVisible({ timeout: 90_000 });
      await expect(page.locator('#bootLabel')).toHaveText('Ready');
      expect(errors).toEqual([]);
      await ctx.close();
    });
  }
});
