// Naming convention (docs/ASSETS.md): lowercase kebab-case, no spaces; the variant goes last
// (m-monogram-transparent.png); ordinals use zero-padded dashes or dots (sunset-01.jpg, circuitboard.00.png).
import { test, expect } from '@playwright/test';
import { listAssets, rootOf } from '../../lib/walk.mjs';

const SEGMENT = /^[a-z0-9]+(?:[-.][a-z0-9]+)*$/;
const FILE = /^[a-z0-9]+(?:[-.][a-z0-9]+)*\.[a-z0-9]+$/;

test.describe('asset naming', () => {
  test('every folder and file name below an asset root is lowercase kebab-case', () => {
    const bad = [];
    for (const rel of listAssets()) {
      const root = rootOf(rel);
      const below = rel.slice(root.length + 1).split('/'); // segments after the asset root
      const file = below.pop();
      for (const seg of below) if (!SEGMENT.test(seg)) bad.push(`${rel}  (folder "${seg}")`);
      if (!FILE.test(file)) bad.push(`${rel}  (file "${file}")`);
    }
    expect(bad, 'rename to lowercase-kebab-case').toEqual([]);
  });

  test('file names contain no spaces, "+" or underscores', () => {
    expect(listAssets().filter((r) => /[\s+_]/.test(r.split('/').pop()))).toEqual([]);
  });

  test('numbered series are zero-padded and gap-free (sunset-01..20, sakura-01..20, neko-00..)', () => {
    const series = new Map();
    for (const rel of listAssets()) {
      const m = rel.match(/^(.*\/)([a-z0-9-]+?)-(\d+)\.(jpg|png|ico)$/);
      if (!m) continue;
      const key = m[1] + m[2] + '.' + m[4];
      series.set(key, [...(series.get(key) ?? []), m[3]]);
    }
    const problems = [];
    for (const [key, nums] of series) {
      if (nums.length < 3) continue; // not a series
      const width = nums[0].length;
      if (nums.some((n) => n.length !== width)) problems.push(`${key}: inconsistent zero padding`);
      const vals = nums.map(Number).sort((a, b) => a - b);
      const start = vals[0];
      vals.forEach((v, i) => { if (v !== start + i) problems.push(`${key}: gap before ${String(v).padStart(width, '0')}`); });
    }
    expect(problems).toEqual([]);
  });
});
