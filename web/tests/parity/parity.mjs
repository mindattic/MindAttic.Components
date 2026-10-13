// Parity check: does a page rebuilt from MindAttic.Components render indistinguishably from its
// hand-made original?
//
//   node parity.mjs <site> <original.htm> <candidate.htm> [--out <dir>]
//
// Both pages are served at the site's real origin, with every jsDelivr request answered from the
// local working trees (MindAttic.Web/MindAttic.Web.Shared for @MindAttic.Web, this repo for
// @MindAttic.Components) and every other host blocked. Math.random is seeded and the clock is
// frozen and advanced by hand, so animated canvases replay frame-for-frame. Each scenario then
// compares three things, and any difference fails the run:
//
//   pixels     every screenshot, exactly (no tolerance)
//   resources  the multiset of (type, sha256) of everything the page loaded
//   dom        the final DOM with comments stripped and each CDN URL replaced by the sha256 of the
//              file it resolves to (a file that moved but kept its bytes compares equal)
import { chromium } from '@playwright/test';
import { PNG } from 'pngjs';
import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { SITES } from './parity.config.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const COMPONENTS_ROOT = path.resolve(here, '../../..');                       // MindAttic.Components
const SHARED_ROOT = path.resolve(COMPONENTS_ROOT, '../MindAttic.Web/MindAttic.Web.Shared');
const CDN_RE = /https:\/\/cdn\.jsdelivr\.net\/gh\/mindattic\/(MindAttic\.Web|MindAttic\.Components)@[^/'"\s)]+\/([^'"\s)]+)/g;

const TYPES = {
  '.js': 'text/javascript; charset=utf-8', '.css': 'text/css; charset=utf-8', '.json': 'application/json',
  '.png': 'image/png', '.jpg': 'image/jpeg', '.jpeg': 'image/jpeg', '.gif': 'image/gif', '.ico': 'image/x-icon',
  '.svg': 'image/svg+xml', '.woff2': 'font/woff2', '.ttf': 'font/ttf', '.txt': 'text/plain; charset=utf-8',
};

const sha = (buf) => crypto.createHash('sha256').update(buf).digest('hex');

/** The local file a jsDelivr URL resolves to, or null. */
function cdnFile(repo, rel) {
  rel = decodeURIComponent(rel.split(/[?#]/)[0]);
  const root = repo === 'MindAttic.Web' ? path.dirname(SHARED_ROOT) : COMPONENTS_ROOT;
  const abs = path.resolve(root, rel);
  if (!abs.startsWith(root + path.sep)) return null;
  return fs.existsSync(abs) && fs.statSync(abs).isFile() ? abs : null;
}

function normalizeDom(html) {
  return html
    .replace(/\r\n/g, '\n')
    // A comment on lines of its own goes with its line break (marker pairs, generator banners), so
    // renamed or moved markers don't register as whitespace differences.
    .replace(/^[ \t]*<!--[\s\S]*?-->[ \t]*\n/gm, '')
    .replace(/<!--[\s\S]*?-->/g, '')
    .replace(CDN_RE, (url, repo, rel) => {
      const abs = cdnFile(repo, rel);
      return abs ? `cdn:${sha(fs.readFileSync(abs)).slice(0, 16)}` : `cdn:MISSING:${rel}`;
    })
    // Release-tag mentions left in code comments (e.g. "jsDelivr URLs @V16").
    .replace(/@V\d+\b/g, '@V#');
}

// Deterministic Math.random (mulberry32). Same seed on both sides, so "random" effects match.
const SEED_SCRIPT = `(() => { let a = 0x9E3779B9; window.__parityReseed = () => { a = 0x9E3779B9; }; Math.random = function () {
  a |= 0; a = a + 0x6D2B79F5 | 0; let t = Math.imul(a ^ a >>> 15, 1 | a);
  t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t; return ((t ^ t >>> 14) >>> 0) / 4294967296; }; })();`;

async function runVariant(browser, site, scenario, htmlFile, outDir, label) {
  // loading="lazy" images load whenever they scroll near the viewport, in real time, which races the
  // frozen clock (a demo tour's scroll length depends on whether they have arrived). Both sides are
  // served with eager images instead, so every image is in before any step runs.
  const html = fs.readFileSync(htmlFile, 'utf8').replaceAll('loading="lazy"', 'loading="eager"');
  const context = await browser.newContext({
    viewport: scenario.viewport, deviceScaleFactor: 1, serviceWorkers: 'block', reducedMotion: 'no-preference',
  });
  const resources = [];
  const missing = [];
  await context.route('**/*', async (route) => {
    const req = route.request();
    const url = new URL(req.url());
    if (url.protocol === 'data:' || url.protocol === 'blob:') return route.continue();
    if (url.origin === new URL(site.origin).origin && req.isNavigationRequest()) {
      return route.fulfill({ status: 200, contentType: 'text/html; charset=utf-8', body: html });
    }
    if (url.host === 'cdn.jsdelivr.net') {
      const m = url.pathname.match(/^\/gh\/mindattic\/(MindAttic\.Web|MindAttic\.Components)@[^/]+\/(.+)$/);
      const abs = m && cdnFile(m[1], m[2]);
      if (abs) {
        const body = fs.readFileSync(abs);
        resources.push(`${req.resourceType()} ${sha(body)}`);
        return route.fulfill({ status: 200, body, headers: {
          'content-type': TYPES[path.extname(abs).toLowerCase()] || 'application/octet-stream',
          'access-control-allow-origin': '*' } });
      }
      missing.push(url.pathname);
      return route.fulfill({ status: 404, body: 'not in working tree' });
    }
    return route.abort('blockedbyclient');
  });

  const page = await context.newPage();
  const errors = [];
  page.on('pageerror', (e) => errors.push(String(e)));
  await page.addInitScript(SEED_SCRIPT);
  // Site-specific setup before any page script, identical on both sides (e.g. a preference that
  // turns a real-time crossfade off).
  if (site.initScript) await page.addInitScript(site.initScript);
  // Installed but paused: time moves only when a step says so, never with however long loading took.
  const t0 = new Date('2026-01-01T12:00:00Z').getTime();
  await page.clock.install({ time: t0 });
  await page.clock.pauseAt(t0 + 1000);
  await page.goto(site.origin + (scenario.path || ''), { waitUntil: 'load' });
  await page.evaluate(() => document.fonts.ready);
  const imagesSettled = () => page.waitForFunction(() => Array.from(document.images).every((i) => i.complete), null, { polling: 50 });
  await imagesSettled();

  // Randomized layers (site.mask) can't be compared pixel-for-pixel even against themselves; they are
  // hidden on both sides for screenshots and left out of the DOM and computed-style comparisons. Their
  // parity is proven by the source and resource-bytes checks instead.
  const mask = site.mask || null;
  if (mask) {
    await page.evaluate((css) => {
      const s = document.createElement('style');
      s.setAttribute('data-parity-mask', '');
      s.textContent = css;
      document.head.appendChild(s);
    }, `${mask} { visibility: hidden !important; }`);
  }

  const shots = {};
  const computed = {};
  for (const step of scenario.steps) {
    if (step.runFor) { await page.clock.runFor(step.runFor); await imagesSettled(); }
    if (step.click) await page.mouse.click(step.click[0], step.click[1]);
    if (step.clickSel) await page.click(step.clickSel);
    if (step.press) await page.keyboard.press(step.press);
    if (step.eval) {
      // Each scripted change starts from the same random state, so what it generates (e.g. a theme's
      // terrain) does not depend on how many random numbers earlier steps happened to draw.
      await page.evaluate(() => window.__parityReseed && window.__parityReseed());
      await page.evaluate(step.eval);
      // Theme switches may run a View Transition, which animates in real time like CSS transitions.
      await page.waitForTimeout(step.settleMs ?? 700);
    }
    if (step.scrollTo !== undefined) {
      await page.evaluate((y) => window.scrollTo(0, y), step.scrollTo);
      await page.waitForTimeout(400); // IntersectionObserver callbacks (reveal-on-scroll) run in real time
    }
    if (step.hover) {
      await page.hover(step.hover);
      // CSS transitions run on the document timeline, not the faked JS clock: give them real time to end.
      await page.waitForTimeout(step.settleMs ?? 900);
    }
    if (step.shot) {
      await imagesSettled();
      // CSS animations and transitions run on the document timeline (real time), not the paused clock.
      // As Playwright's animations:'disabled' does for screenshots: finite ones jump to their end state,
      // infinite (decorative, looping) ones reset to their start. Applied to the live page so computed
      // styles are read in the same settled state as the pixels.
      await page.evaluate(() => {
        for (const a of document.getAnimations()) {
          if (a.effect && a.effect.getComputedTiming().endTime === Infinity) a.cancel(); else a.finish();
        }
      });
      const file = path.join(outDir, `${scenario.name}.${step.shot}.${label}.png`);
      await page.screenshot({ path: file, fullPage: !!step.fullPage, animations: 'disabled', caret: 'hide' });
      shots[step.shot] = file;
      computed[step.shot] = normalizeDom(await page.evaluate(computedStyles, { mask, skipVars: refactor }));
    }
    if (step.unhover) await page.mouse.move(0, 0);
  }
  const dom = normalizeDom(await page.evaluate((m) => {
    const root = document.documentElement.cloneNode(true);
    if (m) root.querySelectorAll(m).forEach((el) => el.remove());
    root.querySelectorAll('style[data-parity-mask]').forEach((el) => el.remove());
    return root.outerHTML;
  }, mask));
  await context.close();
  return { shots, computed, resources: resources.sort(), dom, errors, missing };
}

/** Runs in the page: every visible element's full computed style (plus ::before/::after when they
 *  render), keyed by its structural path. Serialized as one line per element. */
function computedStyles({ mask, skipVars }) {
  const pathOf = (el) => {
    const parts = [];
    for (let e = el; e && e.nodeType === 1 && e !== document.documentElement; e = e.parentElement) {
      let i = 1;
      for (let s = e.previousElementSibling; s; s = s.previousElementSibling) if (s.tagName === e.tagName) i++;
      parts.unshift(`${e.tagName.toLowerCase()}${e.id ? '#' + e.id : ''}:${i}`);
    }
    return parts.join('>');
  };
  // Custom properties are how a refactor themes a component (--ma-btn-u...), not what renders; in
  // --refactor mode only real properties are compared.
  // Sorted: the order getComputedStyle lists custom properties in follows when script set them,
  // which is not a rendering difference.
  const ser = (cs) => {
    const parts = [];
    for (let i = 0; i < cs.length; i++) {
      if (skipVars && cs[i].startsWith('--')) continue;
      parts.push(`${cs[i]}:${cs.getPropertyValue(cs[i])};`);
    }
    return parts.sort().join('');
  };
  const lines = [];
  for (const el of document.querySelectorAll('body, body *')) {
    if (mask && el.closest(mask)) continue;
    if (el.tagName === 'SCRIPT' || el.tagName === 'STYLE' || el.tagName === 'LINK') continue;
    lines.push(`${pathOf(el)} ${ser(getComputedStyle(el))}`);
    for (const pseudo of ['::before', '::after']) {
      const p = getComputedStyle(el, pseudo);
      if (p.content && p.content !== 'none' && p.content !== 'normal') lines.push(`${pathOf(el)}${pseudo} ${ser(p)}`);
    }
  }
  return lines.join('\n');
}

/** The served HTML with comments stripped and CDN URLs replaced by the hash of what they point to. */
function normalizedSource(file) {
  return normalizeDom(fs.readFileSync(file, 'utf8'));
}

function comparePng(a, b, diffFile) {
  const pa = PNG.sync.read(fs.readFileSync(a));
  const pb = PNG.sync.read(fs.readFileSync(b));
  if (pa.width !== pb.width || pa.height !== pb.height) {
    return { diffPixels: -1, note: `size ${pa.width}x${pa.height} vs ${pb.width}x${pb.height}` };
  }
  const diff = new PNG({ width: pa.width, height: pa.height });
  let n = 0;
  for (let i = 0; i < pa.data.length; i += 4) {
    const same = pa.data[i] === pb.data[i] && pa.data[i + 1] === pb.data[i + 1]
      && pa.data[i + 2] === pb.data[i + 2] && pa.data[i + 3] === pb.data[i + 3];
    if (!same) n++;
    diff.data[i] = same ? pa.data[i] >> 2 : 255;
    diff.data[i + 1] = same ? pa.data[i + 1] >> 2 : 0;
    diff.data[i + 2] = same ? pa.data[i + 2] >> 2 : 0;
    diff.data[i + 3] = 255;
  }
  if (n) fs.writeFileSync(diffFile, PNG.sync.write(diff));
  return { diffPixels: n };
}

function firstDifference(a, b) {
  let i = 0;
  while (i < a.length && i < b.length && a[i] === b[i]) i++;
  return { at: i, original: a.slice(Math.max(0, i - 80), i + 120), candidate: b.slice(Math.max(0, i - 80), i + 120) };
}

const [siteName, originalFile, candidateFile, ...rest] = process.argv.slice(2);
const site = SITES[siteName];
if (!site || !originalFile || !candidateFile) {
  console.error(`usage: node parity.mjs <${Object.keys(SITES).join('|')}> <original.htm> <candidate.htm> [--out dir]`);
  process.exit(2);
}
const outIdx = rest.indexOf('--out');
const outDir = path.resolve(outIdx >= 0 ? rest[outIdx + 1] : path.join(here, 'out', siteName));
// --refactor: the candidate restructures markup/CSS (base component + local overrides), so its source
// text and DOM may legitimately differ. Pixels and computed styles must still match exactly.
const refactor = rest.includes('--refactor');
// --only <prefix>: run just the scenarios whose name starts with it (e.g. --only ui-).
const onlyIdx = rest.indexOf('--only');
const only = onlyIdx >= 0 ? rest[onlyIdx + 1] : null;
fs.rmSync(outDir, { recursive: true, force: true });
fs.mkdirSync(outDir, { recursive: true });

let failed = false;
{
  const so = normalizedSource(originalFile);
  const sc = normalizedSource(candidateFile);
  if (so === sc) {
    console.log('PASS  source  (served HTML identical once CDN URLs are resolved to the files they point to)');
  } else {
    const d = firstDifference(so, sc);
    fs.writeFileSync(path.join(outDir, 'source.original.htm'), so);
    fs.writeFileSync(path.join(outDir, 'source.candidate.htm'), sc);
    console.log(`${refactor ? 'INFO' : 'FAIL'}  source  first difference at char ${d.at}`);
    console.log(`      original : ${JSON.stringify(d.original)}`);
    console.log(`      candidate: ${JSON.stringify(d.candidate)}`);
    if (!refactor) failed = true;
  }
}

// Smooth scrolling (CSS scroll-behavior: smooth) animates on the compositor in real time, outside the
// frozen clock. Disabling it in the engine keeps scroll positions deterministic while the page's own
// scroll-behavior value is still part of the computed-style comparison.
const browser = await chromium.launch({ args: ['--disable-smooth-scrolling'] });
for (const scenario of site.scenarios.filter((x) => !only || x.name.startsWith(only))) {
  const o = await runVariant(browser, site, scenario, originalFile, outDir, 'original');
  const c = await runVariant(browser, site, scenario, candidateFile, outDir, 'candidate');
  const problems = [];
  for (const [name, file] of Object.entries(o.shots)) {
    const r = comparePng(file, c.shots[name], path.join(outDir, `${scenario.name}.${name}.DIFF.png`));
    if (r.diffPixels) problems.push(`pixels   ${name}: ${r.diffPixels === -1 ? r.note : r.diffPixels + ' px differ'}`);
  }
  // Declared renames (e.g. a keyframes name a component namespaces): the original's computed styles
  // are read through them, so only an undeclared difference fails.
  for (const [name, cs] of Object.entries(o.computed)) {
    let rs = cs;
    for (const [from, to] of site.equivalent || []) rs = rs.replaceAll(from, to);
    o.computed[name] = rs;
  }
  for (const [name, cs] of Object.entries(o.computed)) {
    if (cs === c.computed[name]) continue;
    const lo = cs.split('\n');
    const lc = (c.computed[name] || '').split('\n');
    const byPath = (lines) => new Map(lines.map((l) => [l.slice(0, l.indexOf(' ')), l.slice(l.indexOf(' ') + 1)]));
    const mo = byPath(lo);
    const mc = byPath(lc);
    const diffs = [];
    for (const [p, v] of mo) {
      if (!mc.has(p)) { diffs.push(`${p}: missing in candidate`); continue; }
      if (mc.get(p) === v) continue;
      const po = Object.fromEntries(v.split(';').filter(Boolean).map((kv) => [kv.slice(0, kv.indexOf(':')), kv.slice(kv.indexOf(':') + 1)]));
      const pc = Object.fromEntries(mc.get(p).split(';').filter(Boolean).map((kv) => [kv.slice(0, kv.indexOf(':')), kv.slice(kv.indexOf(':') + 1)]));
      const props = [...new Set([...Object.keys(po), ...Object.keys(pc)])].filter((k) => po[k] !== pc[k]);
      diffs.push(`${p}: ${props.slice(0, 6).map((k) => `${k} ${JSON.stringify(po[k])} -> ${JSON.stringify(pc[k])}`).join(', ')}${props.length > 6 ? ` (+${props.length - 6} more)` : ''}`);
    }
    for (const p of mc.keys()) if (!mo.has(p)) diffs.push(`${p}: extra in candidate`);
    problems.push(`styles   ${name}: ${diffs.length} element(s) differ\n           ${diffs.slice(0, 8).join('\n           ')}${diffs.length > 8 ? `\n           (+${diffs.length - 8} more)` : ''}`);
  }
  if (o.resources.join('\n') !== c.resources.join('\n')) {
    const only = (x, y) => x.filter((v) => !y.includes(v));
    problems.push(`resources original-only: ${only(o.resources, c.resources).join(', ') || '-'}; candidate-only: ${only(c.resources, o.resources).join(', ') || '-'}`);
  }
  if (o.dom !== c.dom && !refactor) {
    const d = firstDifference(o.dom, c.dom);
    fs.writeFileSync(path.join(outDir, `${scenario.name}.dom.original.html`), o.dom);
    fs.writeFileSync(path.join(outDir, `${scenario.name}.dom.candidate.html`), c.dom);
    problems.push(`dom      first difference at char ${d.at}\n           original : ${JSON.stringify(d.original)}\n           candidate: ${JSON.stringify(d.candidate)}`);
  }
  // Files neither working tree has (a third-party CDN package, e.g. three.js) are blocked identically
  // on both sides; only a difference between the sides is a problem.
  if (o.missing.join() !== c.missing.join()) {
    problems.push(`missing  original: ${o.missing.join(', ') || '-'}; candidate: ${c.missing.join(', ') || '-'}`);
  }
  if (o.errors.join() !== c.errors.join()) problems.push(`errors   original ${JSON.stringify(o.errors)} vs candidate ${JSON.stringify(c.errors)}`);
  const shots = Object.keys(o.shots).length;
  if (problems.length) {
    failed = true;
    console.log(`FAIL  ${scenario.name}  (${shots} screenshots, ${o.resources.length} resources)`);
    for (const p of problems) console.log('      ' + p);
  } else {
    console.log(`PASS  ${scenario.name}  (${shots} screenshots + computed styles identical, ${o.resources.length} resources identical${refactor ? '' : ', DOM identical'})`);
  }
}
await browser.close();
console.log(failed ? `\nNOT indistinguishable. Artifacts: ${outDir}` : `\nIndistinguishable. Artifacts: ${outDir}`);
process.exit(failed ? 1 : 0);
