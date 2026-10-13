// Checks every site must pass: it loads cleanly, talks only to allowed hosts, pulls its assets from the pinned
// MindAttic.Components tag, and carries no embedded base64 or "all in one file" claims.
import { test, expect } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';
import { SITES, SITES_ROOT, TEST_MODE, EXPECTED_TAG, ASSET_EXT, siteOrigin, siteUrl } from '../../lib/paths.mjs';
import { MAX_SINGLE_DATA_URI_CHARS, MAX_TOTAL_DATA_URI_CHARS } from '../../lib/budgets.mjs';
import { absOf, loadManifest } from '../../lib/walk.mjs';
import { openSite, loadSite, sharedUrlsIn, commentsOf } from '../../lib/site-session.mjs';

// Phrases claiming "everything in one file": sites load their assets from the CDN package (BIBLE MAU-LAW-7),
// so these must not appear in comments. Sync-managed MINDATTIC.UIUX marker blocks are excluded (they belong to the component).
const RETIRED_CLAIMS = [
  /\bone file\b/i, /\bsingle[- ]file\b/i, /no external requests?/i, /base64[- ]inlined/i, /inlined as base64/i,
  /inlined (?:as )?base64/i, /zero (?:external )?dependenc/i, /\bno cdn\b/i, /one http request/i, /self-contained single/i,
];
const VIEWPORTS = [[320, 568], [390, 844], [768, 1024], [1366, 768], [1920, 1080]];
const manifest = loadManifest();

for (const [name, site] of Object.entries(SITES)) {
  test.describe(`${name}`, () => {
    let html = '';
    test.beforeAll(async () => {
      if (TEST_MODE === 'live') html = await (await fetch(site.liveUrl)).text();
      else html = fs.readFileSync(path.join(SITES_ROOT, site.dir, 'index.htm'), 'utf8');
    });

    test('is a well-formed page (doctype, lang, viewport meta, title)', () => {
      expect(html).toMatch(/<!doctype html>/i);
      expect(html).toMatch(/<html[^>]*\blang="[a-z-]+"/i);
      expect(html).toMatch(/<meta[^>]+name="viewport"[^>]+width=device-width/i);
      expect(html).toMatch(/<title>[^<]{2,}<\/title>/i);
    });

    test('loads cleanly: no failed or non-2xx requests, no console errors, no uncaught exceptions', async ({ browser }) => {
      const s = await openSite(browser, site);
      try {
        await loadSite(s);
        expect(s.rec.missingFromTree, 'BLOCKED: the page references MindAttic.Components files that are not in the working tree').toEqual([]);
        expect(s.rec.failures, 'network failures').toEqual([]);
        expect(s.rec.badStatus, 'non-2xx responses').toEqual([]);
        expect(s.rec.pageErrors, 'uncaught exceptions').toEqual([]);
        expect(s.rec.consoleErrors, 'console errors').toEqual([]);
      } finally { await s.context.close(); }
    });

    test(`only talks to its own origin and ${site.allowedHosts.join(', ')}`, async ({ browser }) => {
      const s = await openSite(browser, site);
      try {
        await loadSite(s);
        const allowed = new Set([new URL(siteOrigin(site)).host, ...site.allowedHosts]);
        const hosts = [...new Set(s.rec.requests.map((r) => r.host))];
        expect(hosts.filter((h) => !allowed.has(h)), 'requests to hosts outside the allow-list').toEqual([]);
        expect(s.rec.disallowed).toEqual([]);
      } finally { await s.context.close(); }
    });

    test(`pins the expected MindAttic.Components tag (${EXPECTED_TAG}) everywhere — never @main, no stale tag left behind`, () => {
      const urls = sharedUrlsIn(html);
      expect(urls.length, 'BLOCKED/NOT CONVERTED: the page does not reference the MindAttic.Components package at all').toBeGreaterThan(0);
      const tags = [...new Set(urls.map((u) => u.tag))];
      expect(tags.filter((t) => !/^V\d+$/.test(t)), 'tags must be whole-number release tags').toEqual([]);
      expect(tags, `pin exactly ${EXPECTED_TAG}`).toEqual([EXPECTED_TAG]);
      expect(html).not.toMatch(/MindAttic\.Components@main/);
    });

    test('every MindAttic.Components file the HTML references exists in the package (and asset files are listed in the manifest)', () => {
      const urls = [...new Map(sharedUrlsIn(html).map((u) => [u.path, u])).values()];
      const missing = []; const unlisted = [];
      const listed = new Set((manifest?.files ?? []).map((f) => f.path));
      for (const u of urls) {
        if (!fs.existsSync(absOf(u.path))) { missing.push(u.path); continue; }
        if (ASSET_EXT.has(path.extname(u.path).toLowerCase()) && !listed.has(u.path)) unlisted.push(u.path);
      }
      expect(missing, 'referenced but not in MindAttic.Components').toEqual([]);
      expect(unlisted, 'asset files missing from assets-manifest.json — regenerate it').toEqual([]);
    });

    test('has a search snippet and a link-preview card (description, canonical, Open Graph, Twitter) whose image is in the package', () => {
      const head = html.slice(0, html.search(/<\/head>/i));
      const meta = (attr, key) => head.match(new RegExp(`<meta[^>]*${attr}="${key}"[^>]*content="([^"]*)"`, 'i'))?.[1];
      const desc = meta('name', 'description');
      expect(desc, 'meta description').toBeTruthy();
      expect(desc.length, 'description length (search engines show ~50-160 chars)').toBeGreaterThanOrEqual(50);
      expect(desc.length).toBeLessThanOrEqual(200);
      expect(head).toMatch(/<link[^>]*rel="canonical"[^>]*href="https:\/\/[^"]+\/"/i);
      for (const k of ['og:type', 'og:title', 'og:description', 'og:url', 'og:image']) expect(meta('property', k), k).toBeTruthy();
      for (const k of ['twitter:card', 'twitter:image']) expect(meta('name', k), k).toBeTruthy();
      expect(meta('property', 'og:description')).toBe(desc);
      const img = meta('property', 'og:image');
      const m = img.match(/MindAttic\.Components@(V\d+)\/web\/(.+)$/);
      expect(m, 'og:image is served from the pinned MindAttic.Components package').toBeTruthy();
      expect(m[1]).toBe(EXPECTED_TAG);
      const file = (manifest?.files ?? []).find((f) => f.path === m[2]);
      expect(file, `${m[2]} is in assets-manifest.json`).toBeTruthy();
      expect(meta('property', 'og:image:width')).toBe(String(file.width));
      expect(meta('property', 'og:image:height')).toBe(String(file.height));
      expect(meta('name', 'twitter:image')).toBe(img);
    });

    test('opens the jsDelivr connection early (<link rel=preconnect>)', () => {
      expect(html).toMatch(/<link[^>]+rel="preconnect"[^>]+href="https:\/\/cdn\.jsdelivr\.net"/i);
    });

    test('no embedded base64 blobs: HTML size and data: URIs stay within budget', () => {
      expect(Buffer.byteLength(html), `HTML is ${(Buffer.byteLength(html) / 1024).toFixed(0)} KB`).toBeLessThanOrEqual(site.htmlBudgetBytes);
      const uris = html.match(/data:[a-z0-9.+/-]+(?:;[a-z0-9=.-]+)*,[A-Za-z0-9+/=%._~-]{200,}/gi) ?? [];
      const big = uris.filter((u) => u.length > MAX_SINGLE_DATA_URI_CHARS).map((u) => `${u.slice(0, 40)}… (${u.length} chars)`);
      expect(big, 'oversized data: URIs — move them to MindAttic.Components').toEqual([]);
      expect(uris.reduce((n, u) => n + u.length, 0), 'total data: URI characters').toBeLessThanOrEqual(MAX_TOTAL_DATA_URI_CHARS);
    });

    test('fonts load from the CDN: Outfit (latin) / Attic where used; latin-ext stays unfetched unless needed', async ({ browser }) => {
      const s = await openSite(browser, site);
      try {
        await loadSite(s);
        const faces = await s.page.evaluate(() => [...document.fonts].map((f) => ({ family: f.family.replace(/["']/g, ''), status: f.status, range: f.unicodeRange })));
        const outfitLatin = faces.find((f) => f.family === 'Outfit' && /^U\+0(?:-|000)/i.test(f.range));
        const outfitExt = faces.find((f) => f.family === 'Outfit' && /^U\+100/i.test(f.range));
        const attic = faces.find((f) => f.family === 'Attic');
        if (site.fonts.outfit) {
          expect(outfitLatin?.status, 'Outfit latin must be declared and loaded').toBe('loaded');
          expect(outfitExt?.status, 'Outfit latin-ext should not be downloaded unless a glyph needs it').not.toBe('loaded');
        }
        if (site.fonts.attic) expect(attic?.status, 'Attic must be declared and loaded').toBe('loaded');
        const fontFiles = s.rec.requests.filter((r) => /\.woff2?$/.test(new URL(r.url).pathname));
        expect(fontFiles.every((r) => r.host === 'cdn.jsdelivr.net'), 'fonts must come from the CDN, not be embedded or self-hosted').toBe(true);
        expect(fontFiles.every((r) => r.status === 200)).toBe(true);
      } finally { await s.context.close(); }
    });

    test('no horizontal scrollbar at common viewport sizes, in every view the site has', async ({ browser }) => {
      // Every view a visitor can reach is checked, not just the first paint: the hash-routed pages of
      // mindatticcares.com (a sub-page can overflow while Home is fine), and every ryandebraal.com theme
      // at the narrow phone widths (themes change fonts and add effect layers; long skill tags rely on a
      // narrow-screen wrap rule on .tag).
      test.setTimeout(120_000); // ~40 view x viewport combinations on ryandebraal.com
      const s = await openSite(browser, site, { viewport: { width: VIEWPORTS[0][0], height: VIEWPORTS[0][1] } });
      try {
        await loadSite(s, { settleMs: 500 });
        const views = await viewsOf(name, s.page, html);
        const over = [];
        for (const [w, h] of VIEWPORTS) {
          await s.page.setViewportSize({ width: w, height: h });
          for (const v of views) {
            if (v.narrowOnly && w > 400) continue;
            await v.enter(s.page);
            await s.page.waitForTimeout(v.settleMs ?? 150);
            // Effects that are still starting (theme FX, intro transitions) can widen the scrollable area for a
            // few hundred ms. Give layout ~1.2s to settle: overflow that persists fails; a transient flash is
            // recorded as an annotation so it stays visible without making the check flaky.
            let r, first;
            for (let i = 0; i < 8; i++) {
              r = await s.page.evaluate(() => ({ doc: document.documentElement.scrollWidth - document.documentElement.clientWidth, body: document.body.scrollWidth - document.body.clientWidth }));
              first ??= r;
              if (r.doc <= 0 && r.body <= 0) break;
              await s.page.waitForTimeout(150);
            }
            if (r.doc > 0 || r.body > 0) over.push(`${w}x${h} [${v.label}]: documentElement overflows by ${r.doc}px, body by ${r.body}px`);
            else if (first.doc > 0 || first.body > 0) test.info().annotations.push({ type: 'transient-overflow', description: `${w}x${h} [${v.label}]: ${first.doc}px for a moment after entering the view` });
          }
        }
        expect(over).toEqual([]);
        expect(s.rec.pageErrors).toEqual([]);
      } finally { await s.context.close(); }
    });

    test('no in-flow element is sized with 100vw (it overflows by the scrollbar width on desktop browsers)', () => {
      // 100vw includes the vertical scrollbar, so on any browser with classic scrollbars (Windows desktop) an
      // in-flow box that is 100vw wide is ~15-17px wider than the page and adds a sideways scroll. Fixed-position
      // layers are exempt (they do not take part in the document's scrollable width). Sizes attributes on <img>
      // (e.g. sizes="100vw") are not CSS and are not checked.
      const css = [
        ...[...html.matchAll(/<style\b[^>]*>([\s\S]*?)<\/style>/gi)].map((m) => m[1]),
        ...[...html.matchAll(/\sstyle="([^"]*)"/gi)].map((m) => `inline{${m[1]}}`),
      ].join('\n').replace(/\/\*[\s\S]*?\*\//g, '');
      const hits = [];
      for (const m of css.matchAll(/([^{}]+)\{([^{}]*)\}/g)) {
        const [, selector, body] = m;
        if (/(?:^|;|\s)(?:min-|max-)?width\s*:\s*100vw\b/i.test(body) && !/position\s*:\s*fixed/i.test(body)) hits.push(`${selector.trim().slice(0, 80)} { ${body.trim().slice(0, 80)} }`);
      }
      expect(hits, 'use width:100% (or position:fixed) instead of 100vw').toEqual([]);
    });

    test('comments make no "all in one file / no external requests" claims', () => {
      const comments = commentsOf(html);
      const hits = RETIRED_CLAIMS.flatMap((re) => { const m = comments.match(new RegExp(`^.*${re.source}.*$`, 'gim')); return m ? m.map((l) => l.trim().slice(0, 140)) : []; });
      expect(hits, '"all in one file" claim stated in comments').toEqual([]);
    });
  });
}

// The views of a site the overflow check visits. Each enter() must actually switch the view; it asserts that
// it did, so a broken route cannot make the check silently cover less than it claims.
async function viewsOf(name, page, html) {
  if (name === 'mindatticcares.com') {
    return ['home', 'childs-play', 'y2k'].map((id) => ({
      label: `#${id}`,
      async enter(p) {
        await p.evaluate((h) => { window.location.hash = h; }, `#${id}`);
        await expect(p.locator(`#${id}`)).toHaveClass(/active/);
      },
    }));
  }
  if (name === 'ryandebraal.com') {
    // Theme names are read from the page's own CSS ([data-theme="<name>"] blocks), plus the default.
    const themes = [...new Set([...html.matchAll(/\[data-theme="([a-z0-9-]+)"\]\s*\{/g)].map((m) => m[1]))];
    expect(themes.length, 'ryandebraal.com should define several themes').toBeGreaterThan(3);
    const views = [{ label: 'initial theme', async enter() {} }];
    for (const t of themes) {
      views.push({
        label: `theme ${t}`, narrowOnly: true, settleMs: 250,
        async enter(p) {
          await p.evaluate((name) => { (window.applyTheme || ((n) => document.documentElement.setAttribute('data-theme', n)))(name); }, t);
          await expect(p.locator('html')).toHaveAttribute('data-theme', t);
        },
      });
    }
    return views;
  }
  return [{ label: 'page', async enter() {} }];
}

// Keep unused-import linters quiet without hiding intent: siteUrl is exercised via openSite().
void siteUrl;
