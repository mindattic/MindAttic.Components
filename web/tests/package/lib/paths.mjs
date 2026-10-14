// Where things live, and which files count as "assets". Kept in one place so tests, the baseline
// tool and tools/build-asset-manifest.ps1 (which must use the same roots + extensions) stay aligned.
import fs from 'node:fs';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));

/** MindAttic.Components' web/ folder: the jsDelivr subpath (cdn.jsdelivr.net/gh/mindattic/MindAttic.Components@<tag>/web/). */
export const SHARED_ROOT = path.resolve(here, '..', '..', '..');
/** Directory that holds the site folders (mindattic.com, ryandebraal.com, mindatticcares.com, Hyperspace): a MindAttic.Web
 *  checkout, by default the one next to this MindAttic.Components checkout. */
export const SITES_ROOT = process.env.SITES_ROOT ? path.resolve(process.env.SITES_ROOT) : path.resolve(SHARED_ROOT, '..', '..', 'MindAttic.Web');

export const TEST_MODE = process.env.TEST_MODE === 'live' ? 'live' : 'local';
/**
 * The release tag every site is expected to pin: SHARED_TAG if set, otherwise the highest whole-number V<n> tag
 * in this repo (the tag build-sites.mjs pins by default and the linked deploy pins when it cuts a release).
 */
function latestWholeNumberTag() {
  try {
    const out = execFileSync('git', ['-C', SHARED_ROOT, 'tag', '--list', 'V*'], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] });
    const nums = out.split(/\r?\n/).map((t) => t.trim().match(/^V(\d+)$/)).filter(Boolean).map((m) => Number(m[1]));
    return nums.length ? `V${Math.max(...nums)}` : null;
  } catch { return null; }
}
export const EXPECTED_TAG = process.env.SHARED_TAG || latestWholeNumberTag() || 'V1';

export const CDN_HOST = 'cdn.jsdelivr.net';
export const CDN_BASE = (tag) => `https://${CDN_HOST}/gh/mindattic/MindAttic.Components@${tag}/web/`;
/** Matches a MindAttic.Components web file URL anywhere in a page's source. Groups: 1 = tag, 2 = path under web/. */
export const SHARED_URL_RE = /https:\/\/cdn\.jsdelivr\.net\/gh\/mindattic\/MindAttic\.Components@([^/"'\s)<>]+)\/web\/([^"'\s)<>?#]+)/g;

/**
 * Asset roots, relative to SHARED_ROOT (web/). Each site's own files are under assets/<domain>/; fonts live with
 * their font components; Cyberspace owns its parallax textures; themes own their images. assets/archive/ (retired
 * versions) is deliberately not an asset root. Non-asset files in these folders are ignored via ASSET_EXT.
 */
export const DOMAIN_ROOTS = ['assets/mindattic.com', 'assets/mindatticcares.com', 'assets/ryandebraal.com'];
export const FONT_ROOTS = ['components/OutfitFont/fonts', 'components/AtticFont/fonts'];
// Each theme folder is its own root: theme folders are PascalCase by MindAttic.Components convention, and the
// naming rules apply to what is inside them.
export const THEME_ROOTS = fs.existsSync(path.join(SHARED_ROOT, 'themes'))
  ? fs.readdirSync(path.join(SHARED_ROOT, 'themes'), { withFileTypes: true }).filter((e) => e.isDirectory()).map((e) => `themes/${e.name}`)
  : [];
export const ASSET_ROOTS = [...DOMAIN_ROOTS, ...FONT_ROOTS, 'components/Cyberspace/assets', ...THEME_ROOTS];
export const ASSET_EXT = new Set([
  '.png', '.jpg', '.jpeg', '.gif', '.webp', '.avif', '.ico', '.svg',
  '.woff2', '.woff', '.ttf', '.otf', '.mp3', '.ogg', '.mp4', '.webm', '.json',
]);

export const SITES = {
  'mindattic.com': {
    dir: 'mindattic.com',
    liveUrl: 'https://mindattic.com/',
    // Hosts the page itself may request. (Links the user clicks are not page requests.)
    allowedHosts: [CDN_HOST],
    htmlBudgetBytes: 120 * 1024,
    fonts: { outfit: true, attic: true },
  },
  'ryandebraal.com': {
    dir: 'ryandebraal.com',
    liveUrl: 'https://ryandebraal.com/',
    allowedHosts: [CDN_HOST],
    htmlBudgetBytes: 560 * 1024,
    fonts: { outfit: true, attic: false },
  },
  'mindatticcares.com': {
    dir: 'mindatticcares.com',
    // mindatticcares.com is a registrar 'masked forward' (a frameset) to this URL, so the real page lives here:
    liveUrl: 'https://ryandebraal.com/mindatticcares.com/',
    allowedHosts: [CDN_HOST],
    htmlBudgetBytes: 120 * 1024,
    fonts: { outfit: true, attic: false },
  },
};

export const LOCAL_ORIGIN = 'http://127.0.0.1:4173';
export const siteUrl = (site) => (TEST_MODE === 'live' ? site.liveUrl : `${LOCAL_ORIGIN}/${site.dir}/index.htm`);
export const siteOrigin = (site) => new URL(siteUrl(site)).origin;
