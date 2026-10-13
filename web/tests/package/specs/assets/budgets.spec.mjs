// Size budgets: catch a 6 MB lossless PNG or a bloated folder before it ships. Ceilings live in lib/budgets.mjs.
import { test, expect } from '@playwright/test';
import path from 'node:path';
import { listAssets, absOf, rootOf } from '../../lib/walk.mjs';
import fs from 'node:fs';
import { MAX_BYTES_BY_EXT, DEFAULT_MAX_BYTES, MAX_TOTAL_BY_ROOT } from '../../lib/budgets.mjs';

const files = listAssets().map((rel) => ({ rel, bytes: fs.statSync(absOf(rel)).size, ext: path.extname(rel).toLowerCase() }));

test.describe('asset budgets', () => {
  test('no single file exceeds its per-type ceiling', () => {
    const over = files.filter((f) => f.bytes > (MAX_BYTES_BY_EXT[f.ext] ?? DEFAULT_MAX_BYTES)).map((f) => `${f.rel}: ${(f.bytes / 1024).toFixed(0)} KB > ${((MAX_BYTES_BY_EXT[f.ext] ?? DEFAULT_MAX_BYTES) / 1024).toFixed(0)} KB`);
    expect(over).toEqual([]);
  });

  for (const [root, max] of Object.entries(MAX_TOTAL_BY_ROOT)) {
    test(`${root}/ stays under ${(max / 1024).toFixed(0)} KB in total`, () => {
      const total = files.filter((f) => rootOf(f.rel) === root).reduce((n, f) => n + f.bytes, 0);
      expect(total, `${root} is ${(total / 1024).toFixed(0)} KB`).toBeLessThanOrEqual(max);
    });
  }

  test('no empty files', () => {
    expect(files.filter((f) => f.bytes === 0).map((f) => f.rel)).toEqual([]);
  });
});
