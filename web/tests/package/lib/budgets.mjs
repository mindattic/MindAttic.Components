// Size budgets. They are ceilings with headroom, not targets: they exist to catch an accidental
// 6 MB lossless PNG or a re-inlined base64 font, not to police every kilobyte.
const KB = 1024;
export const MAX_BYTES_BY_EXT = {
  '.woff2': 80 * KB,
  '.woff': 120 * KB,
  '.ttf': 160 * KB,
  '.otf': 160 * KB,
  // JPEGs are lossy originals kept at FULL resolution (never downscaled), so the ceiling is generous.
  '.jpg': 1500 * KB,
  '.jpeg': 1500 * KB,
  '.png': 800 * KB,
  '.gif': 150 * KB,
  '.ico': 32 * KB,
  '.svg': 100 * KB,
};
export const DEFAULT_MAX_BYTES = 1024 * KB;
export const MAX_TOTAL_BY_ROOT = {
  'components/OutfitFont/fonts': 300 * KB,
  'components/AtticFont/fonts': 100 * KB,
  'components/Cyberspace/assets': 2200 * KB,
  'assets/mindattic.com': 250 * KB,
  'assets/mindatticcares.com': 3000 * KB,
  'assets/ryandebraal.com': 1000 * KB,
  'themes/Sakura': 3000 * KB,
  'themes/Sunset': 3000 * KB,
  'themes/Noir': 800 * KB,
};
// Guard against re-embedding base64 in a site's HTML.
export const MAX_SINGLE_DATA_URI_CHARS = 8 * KB;
export const MAX_TOTAL_DATA_URI_CHARS = 100 * KB;
