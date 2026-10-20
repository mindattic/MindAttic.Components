// Does MindAttic.Components contain what the sites need, in the shape docs/web/ASSETS.md promises?
import { test, expect } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';
import { SHARED_ROOT, SITES_ROOT, DOMAIN_ROOTS, FONT_ROOTS } from '../../lib/paths.mjs';
import { walk, posix, loadManifest } from '../../lib/walk.mjs';

const REQUIRED = [
  // shared web fonts (their TTF sources are kept in archive/fonts/)
  'components/OutfitFont/fonts/outfit-latin.woff2', 'components/OutfitFont/fonts/outfit-latin-ext.woff2',
  'components/AtticFont/fonts/attic.woff2',
  // Cyberspace engine + styles + textures + SacredGeometry catalogue (loaded by every Cyberspace host)
  'components/Cyberspace/console-bg.js', 'components/Cyberspace/frontpage.css', 'components/Cyberspace/frontpage.html',
  'components/Cyberspace/loader.js', 'components/Cyberspace/tv-static.js', 'components/Cyberspace/home-bg.js',
  'components/Cyberspace/assets/circuitboard.00.png', 'components/Cyberspace/assets/circuitboard.01.png', 'components/Cyberspace/assets/circuitboard.02.png',
  'components/SacredGeometry/sacred-geometry.js',
  // mindattic.com
  'assets/mindattic.com/logos/m-monogram.png', 'assets/mindattic.com/logos/m-icon-16.png', 'assets/mindattic.com/logos/favicon.ico',
  // ryandebraal.com
  'assets/ryandebraal.com/images/ryan-avatar.png', 'assets/ryandebraal.com/images/ryan-portrait.png', 'themes/Noir/noir-moon.png',
  ...[...Array(20)].flatMap((_, i) => { const n = String(i + 1).padStart(2, '0'); return [`themes/Sunset/sunset-${n}.jpg`, `themes/Sakura/sakura-${n}.jpg`]; }),
];

test.describe('package layout', () => {
  for (const rel of REQUIRED) {
    test(`has ${rel}`, () => {
      const abs = path.join(SHARED_ROOT, ...rel.split('/'));
      expect(fs.existsSync(abs), `${rel} is missing from the MindAttic.Components tree`).toBe(true);
      expect(fs.statSync(abs).size, `${rel} is empty`).toBeGreaterThan(0);
    });
  }

  test('every web font family keeps the TTF it was built from, in assets/archive/fonts/<family>/ (not served)', () => {
    const missing = [];
    for (const root of FONT_ROOTS) {
      // components/OutfitFont/fonts -> outfit
      const fam = root.split('/')[1].replace(/Font$/, '').toLowerCase();
      const served = walk(path.join(SHARED_ROOT, root)).map((f) => path.basename(f));
      if (!served.some((f) => f.endsWith('.woff2'))) continue;
      const arch = path.join(SHARED_ROOT, 'assets', 'archive', 'fonts', fam);
      const kept = fs.existsSync(arch) ? walk(arch).map((f) => path.basename(f)) : [];
      if (!kept.some((f) => f.endsWith('.ttf'))) missing.push(`assets/archive/fonts/${fam}/ has no .ttf source`);
      if (served.some((f) => f.endsWith('.ttf'))) missing.push(`${root}/ serves a .ttf; keep sources in assets/archive/fonts/${fam}/`);
    }
    expect(missing, 'woff2 families without their archived TTF source').toEqual([]);
  });

  test('assets/archive/ is kept out of the package: nothing in the manifest lives there and no site links to it', () => {
    const inManifest = (loadManifest()?.files ?? []).filter((f) => f.path.startsWith('assets/archive/')).map((f) => f.path);
    expect(inManifest).toEqual([]);
    const linked = [];
    for (const d of DOMAIN_ROOTS) {
      const page = path.join(SITES_ROOT, d.split('/')[1], 'index.htm');
      if (fs.existsSync(page) && /MindAttic\.Components@[^/]+\/web\/assets\/archive\//.test(fs.readFileSync(page, 'utf8'))) linked.push(d);
    }
    expect(linked, 'sites linking to archive/').toEqual([]);
  });

  test('every domain folder that the sites use has content in at least one category folder', () => {
    const empty = DOMAIN_ROOTS.filter((d) => walk(path.join(SHARED_ROOT, d)).length === 0);
    expect(empty, 'domain folder is empty: copy its assets in').toEqual([]);
  });

  test('domain folders have no assets/ level (assets live directly under <domain>/<category>/)', () => {
    const offenders = DOMAIN_ROOTS.filter((d) => fs.existsSync(path.join(SHARED_ROOT, d, 'assets')));
    expect(offenders, 'remove the assets/ level: ryandebraal.com/assets/x -> ryandebraal.com/x').toEqual([]);
  });

  test('every domain folder is split into category folders, not loose files', () => {
    const loose = [];
    for (const d of DOMAIN_ROOTS) {
      const dir = path.join(SHARED_ROOT, d);
      if (!fs.existsSync(dir)) continue;
      for (const e of fs.readdirSync(dir, { withFileTypes: true })) if (e.isFile() && /\.(png|jpe?g|gif|ico|svg|woff2?)$/i.test(e.name)) loose.push(`${d}/${e.name}`);
    }
    expect(loose, 'assets must sit in a category folder (logos/, icons/, images/, themes/<name>/)').toEqual([]);
  });

  test('no stray junk files in the repo root or the asset folders', () => {
    const JUNK = [/^\.DS_Store$/i, /^Thumbs\.db$/i, /^desktop\.ini$/i, /\.(tmp|bak|orig|rej|swp)$/i, /~$/, /^_(test|harness)[^/]*\.html?$/i];
    const roots = [SHARED_ROOT, ...['fonts', 'components/Cyberspace/assets', ...DOMAIN_ROOTS].map((r) => path.join(SHARED_ROOT, r))];
    const found = [];
    for (const r of roots) {
      const files = r === SHARED_ROOT ? fs.readdirSync(r, { withFileTypes: true }).filter((e) => e.isFile()).map((e) => path.join(r, e.name)) : walk(r);
      for (const f of files) if (JUNK.some((re) => re.test(path.basename(f)))) found.push(posix(path.relative(SHARED_ROOT, f)));
    }
    expect(found).toEqual([]);
  });
});
