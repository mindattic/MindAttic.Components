// Fonts: a browser silently falls back to a system font when a woff2 is corrupt, so every font file is
// validated structurally (always) and, when Python + fontTools are installed, fully decoded (deep check).
import { test, expect } from '@playwright/test';
import { spawnSync } from 'node:child_process';
import path from 'node:path';
import { SHARED_ROOT } from '../../lib/paths.mjs';
import { listAssets, readAsset, absOf } from '../../lib/walk.mjs';
import { validateWoff2, validateSfnt } from '../../lib/binfmt.mjs';

const fonts = listAssets().filter((p) => p.includes('/fonts/'));
const woff2 = fonts.filter((p) => p.endsWith('.woff2'));
const ttf = fonts.filter((p) => p.endsWith('.ttf'));

const FAMILY = (rel) => (rel.includes('/outfit/') ? 'Outfit' : rel.includes('/attic/') ? 'Attic' : null);

test.describe('web fonts', () => {
  test('the expected font files exist', () => {
    expect(woff2.length, 'no woff2 files under fonts/').toBeGreaterThan(0);
    for (const f of ['components/OutfitFont/fonts/outfit-latin.woff2', 'components/OutfitFont/fonts/outfit-latin-ext.woff2', 'components/AtticFont/fonts/attic.woff2']) expect(woff2).toContain(f);
  });

  for (const rel of woff2) {
    test(`${rel}: valid WOFF2 (header, declared length, Brotli stream decodes to the promised size)`, () => {
      const r = validateWoff2(readAsset(rel));
      expect(r.errors, rel).toEqual([]);
      expect(r.numTables).toBeGreaterThan(5);
    });
  }

  for (const rel of ttf) {
    test(`${rel}: valid sfnt (TTF/OTF) source`, () => {
      expect(validateSfnt(readAsset(rel)).errors).toEqual([]);
    });
  }

  test('deep check with fontTools: every woff2 decodes, has the expected family name and a sane glyph count', () => {
    const py = spawnSync('python', [path.join(SHARED_ROOT, 'tests', 'lib', 'font_check.py'), ...woff2.map(absOf)], { encoding: 'utf8' });
    test.skip(py.error != null || py.status !== 0, 'python not available');
    const out = JSON.parse(py.stdout);
    test.skip(Boolean(out.__unavailable__), `fontTools not installed (${out.__unavailable__})`);
    for (const rel of woff2) {
      const r = out[absOf(rel)];
      expect(r.error, `${rel} failed to decode`).toBeUndefined();
      const fam = FAMILY(rel);
      if (fam) expect(r.family.startsWith(fam), `${rel} contains the wrong family (${r.family})`).toBe(true); // variable fonts report e.g. "Outfit Thin"
      expect(r.glyphs, `${rel} has suspiciously few glyphs`).toBeGreaterThan(100);
    }
  });
});
