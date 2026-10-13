// Images decode cleanly, keep their full resolution, and lossy files are never re-encoded.
// The ratchet lives in tests/baselines.json (regenerate deliberately with `npm run baseline:update`).
import { test, expect } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';
import { SHARED_ROOT } from '../../lib/paths.mjs';
import { listAssets, readAsset, sha256 } from '../../lib/walk.mjs';
import { decodeRaster } from '../../lib/binfmt.mjs';

const baselinePath = path.join(SHARED_ROOT, 'tests', 'package', 'baselines.json');
const baselines = fs.existsSync(baselinePath) ? JSON.parse(fs.readFileSync(baselinePath, 'utf8')).images : {};
const raster = listAssets().filter((p) => /\.(png|jpe?g|gif|ico)$/i.test(p));

test.describe('images', () => {
  test('every raster asset decodes cleanly (valid structure, CRCs, no truncation)', () => {
    const bad = [];
    for (const rel of raster) {
      const d = decodeRaster(rel, readAsset(rel));
      if (!d.ok) bad.push(`${rel}: ${d.errors.join('; ')}`);
    }
    expect(bad).toEqual([]);
  });

  test('Cyberspace textures are 1080x1920 (the engine tiles them; a different size breaks seams)', () => {
    for (const n of ['00', '01', '02']) {
      const rel = `components/Cyberspace/assets/circuitboard.${n}.png`;
      const d = decodeRaster(rel, readAsset(rel));
      expect([d.width, d.height], rel).toEqual([1080, 1920]);
    }
  });

  test('the Cyberspace textures stay PNG (JPEG block compression breaks the seams between tiles)', () => {
    expect(listAssets().filter((p) => p.startsWith('components/Cyberspace/assets/')).every((p) => p.endsWith('.png'))).toBe(true);
  });

  test('every raster asset is recorded in baselines.json', () => {
    const missing = raster.filter((r) => !(r in baselines));
    expect(missing, 'new/renamed images: run `npm run baseline:update` and commit baselines.json').toEqual([]);
  });

  test('no image was downscaled (dimensions >= baseline)', () => {
    const shrunk = [];
    for (const rel of raster) {
      const b = baselines[rel]; if (!b) continue;
      const d = decodeRaster(rel, readAsset(rel));
      if (d.width < b.width || d.height < b.height) shrunk.push(`${rel}: ${d.width}x${d.height} < baseline ${b.width}x${b.height}`);
    }
    expect(shrunk, 'lossy/lossless art must keep its full resolution').toEqual([]);
  });

  test('lossy files (JPEG) are byte-identical to their baseline (never re-encoded)', () => {
    const changed = [];
    for (const rel of raster.filter((r) => /\.jpe?g$/i.test(r))) {
      const b = baselines[rel]; if (!b?.sha256) continue;
      if (sha256(readAsset(rel)) !== b.sha256) changed.push(rel);
    }
    expect(changed, 'JPEG bytes changed — if deliberate, ship it under a new name/tag and run `npm run baseline:update`').toEqual([]);
  });

  test('every baselined image still exists (BLOCKED on copy if a domain folder has not been populated yet)', () => {
    const present = new Set(listAssets());
    expect(Object.keys(baselines).filter((r) => !present.has(r))).toEqual([]);
  });
});
