#!/usr/bin/env node
// CLI for WebSnapshot.
//
// Positional forms:
//   node snapshot.js                          refresh every configured target
//   node snapshot.js <name>                   refresh one configured target
//   node snapshot.js <url>                    ad-hoc; name derived from URL
//   node snapshot.js <name> <url>             ad-hoc with explicit name
//
// Flags (any form, override config + defaults):
//   --align=<spec>            alignment used for both input and output crops:
//                             center-top, top-left, etc. Default center-top.
//   --size=<WxH>              OUTPUT dimensions in px (final saved image).
//                             Default 150x225. Alias: --outputsize.
//   --input=<WxH>             INPUT region to grab from the viewport before
//                             scaling. Default = full viewport. Use to pick
//                             exactly how much content you want, e.g.
//                             --input=1280x800 grabs the top half. Alias: --inputsize.
//   --viewport=<WxH>          browser viewport, e.g. 1280x1600. Default 1280x1600.
//   --delay=<ms>              post-load settle time. Default 5000.
//   --wait=<event>            Playwright waitUntil: load|domcontentloaded|networkidle.
//
// Examples:
//   node snapshot.js https://ryandebraal.com --align=top-center --size=150x225
//   node snapshot.js ryandebraal https://ryandebraal.com --align=top-left --size=200x300
//   node snapshot.js shot https://example.com/picture.jpg     # direct image, no crop

const path = require('path');
const { webSnapshot, deriveName } = require('./web-snapshot');
const configured = require('./snapshots.config');

const PREVIEWS_DIR = path.join(__dirname, 'previews');

function looksLikeUrl(s) {
  return /^https?:\/\//i.test(s);
}

// Flags without a value (presence implies true). Anything else either uses
// `--flag=value` or `--flag <value>`; we explicitly allow space-separated values
// only for known value-flags so positional URLs like `https://...` are never
// swallowed as a flag's value.
const VALUE_FLAGS = new Set(['align', 'size', 'outputsize', 'input', 'inputsize', 'viewport', 'delay', 'wait']);

function parseArgs(argv) {
  const positional = [];
  const flags = {};
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a.startsWith('--')) {
      const body = a.slice(2);
      const eq = body.indexOf('=');
      if (eq > -1) {
        flags[body.slice(0, eq)] = body.slice(eq + 1);
      } else if (VALUE_FLAGS.has(body) && argv[i + 1] !== undefined && !argv[i + 1].startsWith('--')) {
        flags[body] = argv[++i];
      } else {
        flags[body] = 'true';
      }
    } else {
      positional.push(a);
    }
  }
  return { positional, flags };
}

function parseWH(value, label) {
  const m = /^(\d+)\s*[x,X]\s*(\d+)$/.exec(String(value || ''));
  if (!m) throw new Error(`Invalid ${label} "${value}", expected WxH (e.g. 150x225)`);
  return { width: parseInt(m[1], 10), height: parseInt(m[2], 10) };
}

function flagsToOptions(flags) {
  const o = {};
  if (flags.align)    o.align     = String(flags.align).toLowerCase();
  const outSpec = flags.size || flags.outputsize;
  if (outSpec) Object.assign(o, parseWH(outSpec, 'size'));
  const inSpec = flags.input || flags.inputsize;
  if (inSpec) {
    const { width, height } = parseWH(inSpec, 'input');
    o.inputWidth = width;
    o.inputHeight = height;
  }
  if (flags.viewport) o.viewport  = parseWH(flags.viewport, 'viewport');
  if (flags.delay) {
    const ms = parseInt(flags.delay, 10);
    if (!Number.isFinite(ms) || ms < 0) throw new Error(`Invalid --delay "${flags.delay}", expected a non-negative integer (ms)`);
    o.delayMs = ms;
  }
  if (flags.wait) {
    const wait = String(flags.wait).toLowerCase();
    const allowed = ['load', 'domcontentloaded', 'networkidle'];
    if (!allowed.includes(wait)) throw new Error(`Invalid --wait "${flags.wait}", expected one of: ${allowed.join(', ')}`);
    o.waitUntil = wait;
  }
  return o;
}

function resolveTargets(positional) {
  if (positional.length === 0) return configured.slice();

  const [a, b] = positional;

  if (looksLikeUrl(a)) {
    return [{ name: deriveName(a), url: a }];
  }
  if (b && looksLikeUrl(b)) {
    const base = configured.find(t => t.name === a) || {};
    return [{ ...base, name: a, url: b }];
  }
  const found = configured.find(t => t.name === a);
  if (found) return [found];

  const known = configured.map(t => t.name).join(', ') || '(none)';
  throw new Error(`No configured target "${a}" and no URL provided. Known: ${known}`);
}

(async () => {
  const { positional, flags } = parseArgs(process.argv.slice(2));
  const overrides = flagsToOptions(flags);
  const targets = resolveTargets(positional).map(t => ({ ...t, ...overrides }));

  for (const t of targets) {
    const outputPath = path.join(PREVIEWS_DIR, `${t.name}.b64.txt`);
    process.stdout.write(`[WebSnapshot] ${t.name} <- ${t.url} ... `);
    const result = await webSnapshot({ ...t, outputPath });
    const tag = result.outputWidth
      ? ` ${result.inputWidth}x${result.inputHeight}->${result.outputWidth}x${result.outputHeight} @${t.align || 'center-top'}`
      : '';
    console.log(`${result.mode}${tag} ${(result.byteLength / 1024).toFixed(1)} KiB -> ${path.relative(process.cwd(), outputPath)}`);
  }
})().catch(err => {
  console.error(err.message || err);
  process.exit(1);
});
