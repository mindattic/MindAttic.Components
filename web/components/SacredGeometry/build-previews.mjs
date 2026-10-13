/* build-previews.mjs — emit Components/SacredGeometry/previews/shape-0000.svg .. shape-1023.svg
 * and double as the catalog smoke test.
 *
 *   node build-previews.mjs            # write posters + validate
 *   node build-previews.mjs --check    # validate only (no files written)
 *
 * Exits non-zero on any failure: wrong count, a spec that throws, empty geometry, a duplicate
 * fingerprint (two shapes that look identical), or malformed SVG. sacred-geometry.js is a classic
 * browser script with a CommonJS export branch, so createRequire loads it without a bundler.
 */
import { createRequire } from 'node:module';
import { mkdirSync, writeFileSync, rmSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const require = createRequire(import.meta.url);
const SG = require('./sacred-geometry.js');

const here = dirname(fileURLToPath(import.meta.url));
const outDir = join(here, 'previews');
const checkOnly = process.argv.includes('--check');

const problems = [];
if (SG.count !== 1024) problems.push(`count is ${SG.count}, expected 1024`);

if (!checkOnly) {
    rmSync(outDir, { recursive: true, force: true });
    mkdirSync(outDir, { recursive: true });
}

const fingerprints = new Map();
let totalSegments = 0;

for (let i = 0; i < SG.count; i++) {
    let spec;
    try { spec = SG.spec(i); } catch (e) { problems.push(`#${i}: spec() threw: ${e.message}`); continue; }

    let sampled;
    try { sampled = SG.sample(i, spec.posterT); } catch (e) { problems.push(`#${i} ${spec.label}: sample() threw: ${e.message}`); continue; }

    const segs = sampled.polylines.reduce((a, pl) => a + pl.length, 0) + sampled.dots.length;
    totalSegments += segs;
    if (segs === 0) problems.push(`#${i} ${spec.label}: empty geometry`);

    // every coordinate must be finite
    for (const pl of sampled.polylines)
        for (const [x, y] of pl)
            if (!Number.isFinite(x) || !Number.isFinite(y)) { problems.push(`#${i} ${spec.label}: non-finite coord`); break; }

    let fp;
    try { fp = SG.fingerprint(i); } catch (e) { problems.push(`#${i} ${spec.label}: fingerprint() threw: ${e.message}`); continue; }
    if (fingerprints.has(fp)) problems.push(`#${i} ${spec.label}: duplicate fingerprint ${fp} (collides with #${fingerprints.get(fp)} ${SG.label(fingerprints.get(fp))})`);
    else fingerprints.set(fp, i);

    let markup;
    try { markup = SG.svg(i, { size: 300 }); } catch (e) { problems.push(`#${i} ${spec.label}: svg() threw: ${e.message}`); continue; }
    if (!/^<svg[\s\S]*<\/svg>$/.test(markup)) problems.push(`#${i} ${spec.label}: malformed SVG`);

    if (!checkOnly) writeFileSync(join(outDir, `shape-${String(i).padStart(4, '0')}.svg`), markup, 'utf8');
}

if (problems.length) {
    console.error(`SacredGeometry build FAILED — ${problems.length} problem(s):`);
    for (const p of problems.slice(0, 40)) console.error('  • ' + p);
    if (problems.length > 40) console.error(`  … and ${problems.length - 40} more`);
    process.exit(1);
}

console.log(`OK — ${SG.count} shapes, ${fingerprints.size} unique fingerprints, ${totalSegments} total points.`);
if (!checkOnly) console.log(`Wrote ${SG.count} posters to ${outDir}`);
