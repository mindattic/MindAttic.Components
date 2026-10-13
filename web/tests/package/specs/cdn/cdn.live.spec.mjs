// LIVE only: is what jsDelivr actually serves for the tag each site pins byte-identical to this package, with the
// headers a browser needs (CORS for fonts, long immutable caching)? Run: npm run test:cdn
// Skipped in local mode (the live CDN may not have the tag yet; local mode answers CDN requests from this tree).
import { test, expect } from '@playwright/test';
import path from 'node:path';
import { SITES, TEST_MODE, EXPECTED_TAG, ASSET_EXT } from '../../lib/paths.mjs';
import { sha256, loadManifest } from '../../lib/walk.mjs';
import { sharedUrlsIn } from '../../lib/site-session.mjs';

test.skip(TEST_MODE !== 'live', 'live-only: run with TEST_MODE=live (npm run test:cdn)');

const manifest = loadManifest();
const byPath = new Map((manifest?.files ?? []).map((f) => [f.path, f]));
const TYPES = {
  '.png': /^image\/png$/, '.jpg': /^image\/jpeg$/, '.jpeg': /^image\/jpeg$/, '.gif': /^image\/gif$/, '.webp': /^image\/webp$/,
  '.ico': /^image\/(x-icon|vnd\.microsoft\.icon)$/, '.svg': /^image\/svg\+xml/, '.woff2': /^(font\/woff2|application\/(font-woff2|octet-stream))$/,
  '.js': /javascript/, '.css': /^text\/css/,
};

async function mapLimit(items, limit, fn) {
  const out = []; let i = 0;
  await Promise.all(Array.from({ length: Math.min(limit, items.length) }, async () => { while (i < items.length) { const k = i++; out[k] = await fn(items[k]); } }));
  return out;
}

for (const [name, site] of Object.entries(SITES)) {
  test(`${name}: every MindAttic.Components file referenced by the live page is served intact`, async ({ request }) => {
    expect(manifest, 'BLOCKED: assets-manifest.json missing — run tools/build-asset-manifest.ps1').not.toBeNull();
    const html = await (await request.get(site.liveUrl)).text();
    const found = [...new Map(sharedUrlsIn(html).map((u) => [u.url, u])).values()];
    // A URL ending in '/' is a base prefix the page appends file names to at runtime (ryandebraal.com builds its theme
    // image URLs that way), not a file: replace it by every manifest file under that prefix so those are checked too.
    const refs = [];
    for (const u of found) {
      if (!u.path.endsWith('/')) { refs.push(u); continue; }
      for (const f of manifest.files) {
        if (f.path.startsWith(u.path)) refs.push({ ...u, path: f.path, url: u.url.slice(0, u.url.length - u.path.length) + f.path });
      }
    }
    expect(refs.length, `${name}: the live page does not reference MindAttic.Components (not deployed yet?)`).toBeGreaterThan(0);

    const problems = [];
    await mapLimit(refs, 6, async (u) => {
      const res = await request.get(u.url);
      const where = `${u.path} @${u.tag}`;
      if (res.status() !== 200) { problems.push(`${where}: HTTP ${res.status()}`); return; }
      const h = res.headers();
      const ext = path.extname(u.path).toLowerCase();
      if (TYPES[ext] && !TYPES[ext].test((h['content-type'] ?? '').split(';')[0])) problems.push(`${where}: content-type ${h['content-type']}`);
      if (h['access-control-allow-origin'] !== '*') problems.push(`${where}: access-control-allow-origin is "${h['access-control-allow-origin']}" (fonts need *)`);
      const cc = h['cache-control'] ?? '';
      const maxAge = Number(cc.match(/max-age=(\d+)/)?.[1] ?? 0);
      if (!/immutable/.test(cc) && maxAge < 31_536_000) problems.push(`${where}: cache-control "${cc}" is not long-lived`);
      if (ASSET_EXT.has(ext)) {
        const want = byPath.get(u.path);
        if (!want) { problems.push(`${where}: not in assets-manifest.json`); return; }
        const body = await res.body();
        if (body.length !== want.bytes) problems.push(`${where}: served ${body.length} bytes, manifest says ${want.bytes}`);
        else if (sha256(body) !== want.sha256) problems.push(`${where}: served bytes differ from the repository (sha256 mismatch)`);
      }
    });
    expect(problems.sort(), `jsDelivr is not serving ${name}'s files as committed`).toEqual([]);
  });
}

test.describe('release tags', () => {
  test('whole-number tags V1..Vn exist, are contiguous, and the expected tag is published', async ({ request }) => {
    const headers = { 'user-agent': 'mindattic-web-shared-tests', accept: 'application/vnd.github+json', ...(process.env.GITHUB_TOKEN ? { authorization: `Bearer ${process.env.GITHUB_TOKEN}` } : {}) };
    const res = await request.get('https://api.github.com/repos/mindattic/MindAttic.Components/git/matching-refs/tags/V', { headers });
    test.skip(res.status() === 403 || res.status() === 429, 'GitHub API rate limit hit (set GITHUB_TOKEN to raise it)');
    expect(res.status()).toBe(200);
    const tags = (await res.json()).map((r) => r.ref.replace('refs/tags/', ''));
    expect(tags.filter((t) => !/^V\d+$/.test(t)), 'non whole-number tags').toEqual([]);
    const nums = tags.map((t) => Number(t.slice(1))).sort((a, b) => a - b);
    // MindAttic.Components' release tags are whole numbers, V1 onward.
    nums.forEach((n, i) => expect(n, `tags must be contiguous from V1; got ${tags.join(', ')}`).toBe(i + 1));
    expect(tags, `expected tag ${EXPECTED_TAG} is not published yet`).toContain(EXPECTED_TAG);
  });

  test('a published tag is immutable: its jsDelivr response is edge-cached for a year', async ({ request }) => {
    const res = await request.get(`https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Components@${EXPECTED_TAG}/web/components/AtticFont/fonts/attic.woff2`);
    expect(res.status()).toBe(200);
    expect(res.headers()['cache-control']).toMatch(/max-age=31536000/);
  });
});
