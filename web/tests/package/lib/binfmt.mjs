// Dependency-free structural validators for the binary formats the package ships. They exist because a
// browser silently falls back (system font / broken image) when a file is corrupt, which hides the bug —
// the Outfit "latin" woff2 embedded in an old page was corrupt for exactly that reason.
import zlib from 'node:zlib';

// ---------------------------------------------------------------- WOFF2
// Known-table index -> tag (WOFF2 spec, section 5.1).
const KNOWN_TAGS = [
  'cmap', 'head', 'hhea', 'hmtx', 'maxp', 'name', 'OS/2', 'post', 'cvt ', 'fpgm', 'glyf', 'loca', 'prep', 'CFF ', 'VORG',
  'EBDT', 'EBLC', 'gasp', 'hdmx', 'kern', 'LTSH', 'PCLT', 'VDMX', 'vhea', 'vmtx', 'BASE', 'GDEF', 'GPOS', 'GSUB', 'EBSC',
  'JSTF', 'MATH', 'CBDT', 'CBLC', 'COLR', 'CPAL', 'SVG ', 'sbix', 'acnt', 'avar', 'bdat', 'bloc', 'bsln', 'cvar', 'fdsc',
  'feat', 'fmtx', 'fvar', 'gvar', 'hsty', 'just', 'lcar', 'mort', 'morx', 'opbd', 'prop', 'trak', 'Zapf', 'Silf', 'Glat',
  'Gloc', 'Feat', 'Sill',
];

/** Validates a WOFF2 file: header, declared length, table directory and that the Brotli stream decodes to exactly
 *  the size the directory promises. Returns { ok, errors, numTables }. */
export function validateWoff2(buf) {
  const errors = [];
  const fail = (m) => { errors.push(m); return { ok: false, errors, numTables: 0 }; };
  if (buf.length < 48) return fail(`too short (${buf.length} bytes)`);
  if (buf.toString('latin1', 0, 4) !== 'wOF2') return fail('missing wOF2 signature');
  const declared = buf.readUInt32BE(8);
  if (declared !== buf.length) errors.push(`declared length ${declared} != file length ${buf.length}`);
  const numTables = buf.readUInt16BE(12);
  const totalCompressed = buf.readUInt32BE(20);
  let off = 48;
  let expected = 0;
  const readBase128 = () => {
    let v = 0;
    for (let i = 0; i < 5; i++) {
      if (off >= buf.length) throw new Error('truncated table directory');
      const b = buf[off++];
      v = v * 128 + (b & 0x7f);
      if (!(b & 0x80)) return v;
    }
    throw new Error('bad UIntBase128');
  };
  try {
    for (let i = 0; i < numTables; i++) {
      const flags = buf[off++];
      const idx = flags & 0x3f;
      const version = (flags >> 6) & 3;
      let tag;
      if (idx === 0x3f) { tag = buf.toString('latin1', off, off + 4); off += 4; } else tag = KNOWN_TAGS[idx];
      const origLength = readBase128();
      const isGlyfLoca = tag === 'glyf' || tag === 'loca';
      const transformed = isGlyfLoca ? version === 0 : version !== 0;
      expected += transformed ? readBase128() : origLength;
    }
  } catch (e) { return fail(`table directory: ${e.message}`); }
  const stream = buf.subarray(off, off + totalCompressed);
  if (stream.length !== totalCompressed) errors.push(`compressed stream truncated (${stream.length} of ${totalCompressed})`);
  try {
    const out = zlib.brotliDecompressSync(stream);
    if (out.length !== expected) errors.push(`decompressed ${out.length} bytes, directory promises ${expected}`);
  } catch (e) { errors.push(`brotli decode failed: ${e.message}`); }
  return { ok: errors.length === 0, errors, numTables };
}

// ---------------------------------------------------------------- sfnt (TTF/OTF)
export function validateSfnt(buf) {
  const errors = [];
  const magic = buf.readUInt32BE(0);
  const ok = magic === 0x00010000 || buf.toString('latin1', 0, 4) === 'OTTO' || buf.toString('latin1', 0, 4) === 'true' || buf.toString('latin1', 0, 4) === 'ttcf';
  if (!ok) errors.push('not an sfnt font (bad magic)');
  return { ok: errors.length === 0, errors };
}

// ---------------------------------------------------------------- PNG
const PNG_SIG = Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]);
const PNG_CHANNELS = { 0: 1, 2: 3, 3: 1, 4: 2, 6: 4 };

/** Parses a PNG, verifies every chunk CRC, and (for non-interlaced images) that the pixel data inflates to exactly
 *  the size implied by the header. Returns { ok, errors, width, height, colorType, bitDepth }. */
export function parsePng(buf) {
  const errors = [];
  const bad = (m) => ({ ok: false, errors: [...errors, m], width: 0, height: 0 });
  if (buf.length < 33 || !buf.subarray(0, 8).equals(PNG_SIG)) return bad('not a PNG (bad signature)');
  let off = 8; let ihdr = null; const idat = []; let sawIend = false;
  while (off + 12 <= buf.length) {
    const len = buf.readUInt32BE(off);
    const type = buf.toString('latin1', off + 4, off + 8);
    const data = buf.subarray(off + 8, off + 8 + len);
    const crc = buf.readUInt32BE(off + 8 + len);
    if (off + 12 + len > buf.length) return bad(`chunk ${type} runs past end of file`);
    if (zlib.crc32(buf.subarray(off + 4, off + 8 + len)) >>> 0 !== crc) errors.push(`bad CRC in chunk ${type}`);
    if (type === 'IHDR') ihdr = { width: data.readUInt32BE(0), height: data.readUInt32BE(4), bitDepth: data[8], colorType: data[9], interlace: data[12] };
    else if (type === 'IDAT') idat.push(data);
    else if (type === 'IEND') { sawIend = true; break; }
    off += 12 + len;
  }
  if (!ihdr) return bad('no IHDR chunk');
  if (!sawIend) errors.push('no IEND chunk (truncated file)');
  try {
    const raw = zlib.inflateSync(Buffer.concat(idat));
    if (ihdr.interlace === 0) {
      const bpp = (PNG_CHANNELS[ihdr.colorType] ?? 0) * ihdr.bitDepth;
      const want = ihdr.height * (1 + Math.ceil((ihdr.width * bpp) / 8));
      if (raw.length !== want) errors.push(`pixel data ${raw.length} bytes, header implies ${want}`);
    }
  } catch (e) { errors.push(`IDAT inflate failed: ${e.message}`); }
  return { ok: errors.length === 0, errors, ...ihdr };
}

// ---------------------------------------------------------------- JPEG
export function parseJpeg(buf) {
  const errors = [];
  if (buf.length < 4 || buf[0] !== 0xff || buf[1] !== 0xd8) return { ok: false, errors: ['not a JPEG (missing SOI)'], width: 0, height: 0 };
  let off = 2; let width = 0; let height = 0;
  while (off + 4 <= buf.length) {
    if (buf[off] !== 0xff) { off++; continue; }
    const marker = buf[off + 1];
    if (marker === 0xff) { off++; continue; }
    if (marker === 0xd8 || (marker >= 0xd0 && marker <= 0xd7) || marker === 0x01) { off += 2; continue; }
    if (marker === 0xd9) break;
    const len = buf.readUInt16BE(off + 2);
    if ([0xc0, 0xc1, 0xc2, 0xc3, 0xc5, 0xc6, 0xc7, 0xc9, 0xca, 0xcb, 0xcd, 0xce, 0xcf].includes(marker)) {
      height = buf.readUInt16BE(off + 5); width = buf.readUInt16BE(off + 7); break;
    }
    if (marker === 0xda) break; // start of scan before any SOF => malformed
    off += 2 + len;
  }
  if (!width || !height) errors.push('no SOF marker (cannot read dimensions)');
  let end = buf.length; while (end > 0 && buf[end - 1] === 0) end--;
  if (!(buf[end - 2] === 0xff && buf[end - 1] === 0xd9)) errors.push('missing EOI marker (truncated file)');
  return { ok: errors.length === 0, errors, width, height };
}

// ---------------------------------------------------------------- ICO / GIF
export function parseIco(buf) {
  const errors = [];
  if (buf.length < 22) return { ok: false, errors: ['too short for an ICO'], width: 0, height: 0 };
  if (buf.readUInt16LE(0) !== 0 || buf.readUInt16LE(2) !== 1) errors.push('bad ICO header');
  const count = buf.readUInt16LE(4);
  if (count < 1) errors.push('ICO has no images');
  let width = 0; let height = 0;
  for (let i = 0; i < count && 6 + i * 16 + 16 <= buf.length; i++) {
    const e = 6 + i * 16;
    const size = buf.readUInt32LE(e + 8); const offset = buf.readUInt32LE(e + 12);
    if (offset + size > buf.length) errors.push(`ICO image ${i} runs past end of file`);
    if (i === 0) { width = buf[e] || 256; height = buf[e + 1] || 256; }
  }
  return { ok: errors.length === 0, errors, width, height };
}

export function parseGif(buf) {
  const sig = buf.toString('latin1', 0, 6);
  if (sig !== 'GIF87a' && sig !== 'GIF89a') return { ok: false, errors: ['not a GIF'], width: 0, height: 0 };
  return { ok: buf[buf.length - 1] === 0x3b, errors: buf[buf.length - 1] === 0x3b ? [] : ['missing GIF trailer'], width: buf.readUInt16LE(6), height: buf.readUInt16LE(8) };
}

/** Decode any raster asset by extension. Returns null for non-raster files. */
export function decodeRaster(rel, buf) {
  const ext = rel.slice(rel.lastIndexOf('.')).toLowerCase();
  if (ext === '.png') return parsePng(buf);
  if (ext === '.jpg' || ext === '.jpeg') return parseJpeg(buf);
  if (ext === '.ico') return parseIco(buf);
  if (ext === '.gif') return parseGif(buf);
  return null;
}
