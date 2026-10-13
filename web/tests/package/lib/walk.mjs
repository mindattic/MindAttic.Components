import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { SHARED_ROOT, ASSET_ROOTS, ASSET_EXT } from './paths.mjs';

const SKIP_DIRS = new Set(['node_modules', '.git', 'test-results', 'playwright-report']);

export const posix = (p) => p.split(path.sep).join('/');

/** Recursively list files under an absolute dir (returns absolute paths). Missing dir -> []. */
export function walk(absDir) {
  const out = [];
  if (!fs.existsSync(absDir)) return out;
  for (const e of fs.readdirSync(absDir, { withFileTypes: true })) {
    if (e.isDirectory()) {
      if (SKIP_DIRS.has(e.name)) continue;
      out.push(...walk(path.join(absDir, e.name)));
    } else if (e.isFile()) out.push(path.join(absDir, e.name));
  }
  return out;
}

/** Every asset file (by extension) under the asset roots, as posix paths relative to the repo root, sorted. */
export function listAssets() {
  const rels = [];
  for (const root of ASSET_ROOTS) {
    for (const abs of walk(path.join(SHARED_ROOT, root))) {
      if (ASSET_EXT.has(path.extname(abs).toLowerCase())) rels.push(posix(path.relative(SHARED_ROOT, abs)));
    }
  }
  return rels.sort();
}

export const absOf = (rel) => path.join(SHARED_ROOT, ...rel.split('/'));
export const readAsset = (rel) => fs.readFileSync(absOf(rel));
export const sha256 = (buf) => crypto.createHash('sha256').update(buf).digest('hex');

/** The asset root a repo-relative path belongs to (longest match), or null. */
export const rootOf = (rel) =>
  ASSET_ROOTS.filter((r) => rel === r || rel.startsWith(r + '/')).sort((a, b) => b.length - a.length)[0] ?? null;

export function loadManifest() {
  const p = path.join(SHARED_ROOT, 'assets-manifest.json');
  if (!fs.existsSync(p)) return null;
  return JSON.parse(fs.readFileSync(p, 'utf8').replace(/^﻿/, ''));
}
