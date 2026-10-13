// Opens a site in a fresh browser context and records everything a "does this page pull its data properly?"
// test needs: every request (with host + status), failures, console errors, uncaught exceptions, CDN files
// that were requested but do not exist in the local tree, and navigations that left the page.
import fs from 'node:fs';
import path from 'node:path';
import { SHARED_ROOT, TEST_MODE, CDN_HOST, SHARED_URL_RE, siteUrl, siteOrigin } from './paths.mjs';

const TYPES = {
  '.js': 'text/javascript; charset=utf-8', '.css': 'text/css; charset=utf-8', '.json': 'application/json',
  '.png': 'image/png', '.jpg': 'image/jpeg', '.jpeg': 'image/jpeg', '.gif': 'image/gif', '.ico': 'image/x-icon',
  '.svg': 'image/svg+xml', '.woff2': 'font/woff2', '.ttf': 'font/ttf',
};

/** A minimal html2pdf stand-in (local mode only): the page just needs `window.html2pdf` to exist after loading. */
const HTML2PDF_STUB = `
(function () {
  function chain() { var c = { set: function () { return c; }, from: function () { return c; }, toPdf: function () { return c; },
    get: function () { return Promise.resolve({ internal: { getNumberOfPages: function () { return 1; }, pageSize: { getWidth: function () { return 1; }, getHeight: function () { return 1; } } }, setPage: function () {}, setFontSize: function () {}, text: function () {}, addImage: function () {} }); },
    save: function () { return Promise.resolve(); }, output: function () { return Promise.resolve(); } }; return c; }
  window.html2pdf = function () { return chain(); };
  window.__html2pdfStub = true;
})();`;

export function sharedFile(urlPath) {
  const rel = decodeURIComponent(urlPath).replace(/^\/+/, '');
  const abs = path.resolve(SHARED_ROOT, rel);
  if (!abs.startsWith(SHARED_ROOT + path.sep)) return null;
  return fs.existsSync(abs) && fs.statSync(abs).isFile() ? abs : null;
}

/**
 * @param browser   Playwright Browser
 * @param site      entry from SITES
 * @param opts      { viewport, stubExternalNavigation = true, stubHtml2pdf = (mode==='local') }
 */
export async function openSite(browser, site, opts = {}) {
  const origin = siteOrigin(site);
  const allowed = new Set([new URL(origin).host, ...site.allowedHosts]);
  const rec = {
    requests: [],            // { url, host, type, status, fromTree? }
    failures: [],            // network-level failures that were not our own deliberate aborts
    badStatus: [],           // responses >= 400
    disallowed: [],          // requests to hosts outside the allow-list
    missingFromTree: [],     // MindAttic.Components files the page asked for that do not exist locally (local mode)
    navigations: [],         // external document navigations we intercepted (clicked links / popups)
    consoleErrors: [],
    pageErrors: [],
  };
  const aborted = new Set();
  const context = await browser.newContext({ viewport: opts.viewport ?? { width: 1366, height: 768 }, serviceWorkers: 'block' });
  const stubPdf = opts.stubHtml2pdf ?? TEST_MODE === 'local';

  await context.route('**/*', async (route) => {
    const req = route.request();
    const url = new URL(req.url());
    if (url.protocol === 'data:' || url.protocol === 'blob:') return route.continue();
    const host = url.host;

    // A navigation to somewhere that is not the page under test (clicked link, popup): never leave the sandbox.
    let topLevel = true; // a popup's first request has no frame yet — that is a top-level navigation too
    try { topLevel = req.frame().parentFrame() === null; } catch { /* frame not created yet */ }
    if (req.isNavigationRequest() && host !== new URL(origin).host && host !== CDN_HOST && topLevel && opts.stubExternalNavigation !== false) {
      rec.navigations.push(req.url());
      return route.fulfill({ status: 200, contentType: 'text/html', body: '<!doctype html><title>stub</title><p>external page stub</p>' });
    }
    if (!allowed.has(host)) {
      rec.disallowed.push(req.url());
      if (TEST_MODE === 'local') { aborted.add(req.url()); return route.abort('blockedbyclient'); }
      return route.continue();
    }
    if (TEST_MODE === 'local' && host === CDN_HOST) {
      const m = url.pathname.match(/^\/gh\/mindattic\/MindAttic\.Components@[^/]+\/web\/(.+)$/);
      if (m) {
        const abs = sharedFile(m[1]);
        if (!abs) { rec.missingFromTree.push(m[1]); return route.fulfill({ status: 404, body: `not in working tree: ${m[1]}` }); }
        return route.fulfill({ status: 200, body: fs.readFileSync(abs), headers: { 'content-type': TYPES[path.extname(abs).toLowerCase()] || 'application/octet-stream', 'access-control-allow-origin': '*', 'cache-control': 'public, max-age=31536000, immutable' } });
      }
      if (/^\/npm\/html2pdf\.js@/.test(url.pathname) && stubPdf) {
        return route.fulfill({ status: 200, contentType: 'text/javascript', body: HTML2PDF_STUB, headers: { 'access-control-allow-origin': '*' } });
      }
      rec.disallowed.push(req.url());
      aborted.add(req.url());
      return route.abort('blockedbyclient');
    }
    return route.continue();
  });

  const page = await context.newPage();
  const track = (p) => {
    p.on('request', (r) => { try { const u = new URL(r.url()); if (u.protocol.startsWith('http')) rec.requests.push({ url: r.url(), host: u.host, type: r.resourceType(), status: null }); } catch {} });
    p.on('response', (r) => {
      const e = rec.requests.find((x) => x.url === r.url() && x.status === null); if (e) e.status = r.status();
      if (r.status() >= 400) rec.badStatus.push(`${r.status()} ${r.url()}`);
    });
    p.on('requestfailed', (r) => { if (!aborted.has(r.url())) rec.failures.push(`${r.failure()?.errorText} ${r.url()}`); });
    p.on('console', (m) => { if (m.type() === 'error') rec.consoleErrors.push(m.text()); });
    p.on('pageerror', (e) => rec.pageErrors.push(String(e)));
  };
  track(page);
  context.on('page', track);

  return { context, page, rec, origin, url: siteUrl(site) };
}

/** Navigate and let fonts, deferred scripts and the first paint settle. */
export async function loadSite(session, { settleMs = 1500 } = {}) {
  await session.page.goto(session.url, { waitUntil: 'load' });
  await session.page.evaluate(() => document.fonts.ready);
  await session.page.waitForTimeout(settleMs);
  return session;
}

export function sharedUrlsIn(source) {
  const out = []; let m; const re = new RegExp(SHARED_URL_RE.source, 'g');
  while ((m = re.exec(source))) out.push({ tag: m[1], path: m[2], url: m[0] });
  return out;
}

/** HTML comments + CSS/JS block comments + whole-line // comments, with the sync-managed MINDATTIC.UIUX marker blocks removed. */
export function commentsOf(source) {
  const stripped = source.replace(/<!-- BEGIN MINDATTIC\.UIUX:(\w+) -->[\s\S]*?<!-- END MINDATTIC\.UIUX:\1 -->/g, '');
  return [...(stripped.match(/<!--[\s\S]*?-->/g) ?? []), ...(stripped.match(/\/\*[\s\S]*?\*\//g) ?? []), ...(stripped.match(/^[ \t]*\/\/.*$/gm) ?? [])].join('\n');
}
