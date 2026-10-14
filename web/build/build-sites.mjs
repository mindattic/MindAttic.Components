// Splices MindAttic.Components web components into the hand-made sites listed in ../sites.json.
//
//   node build-sites.mjs <site> [--out <file>] [--tag <V#>]   (default tag: the newest V<n> tag of this repo)
//
// Each block replaces everything between <!-- BEGIN MINDATTIC.COMPONENTS:<marker> --> and its END
// marker in the target page; nothing outside a marker pair is touched except the site's listed URL
// rewrites. Idempotent: re-running with unchanged sources produces a byte-identical page. Without
// --out the target is rewritten in place. Line endings follow the target's dominant style.
import fs from 'node:fs';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const WEB = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const COMPONENTS = path.join(WEB, 'components');
const manifest = JSON.parse(fs.readFileSync(path.join(WEB, 'sites.json'), 'utf8'));

const args = process.argv.slice(2);
const siteName = args[0];
const opt = (name) => { const i = args.indexOf(name); return i >= 0 ? args[i + 1] : undefined; };
const site = manifest.sites[siteName];
if (!site) {
  console.error(`usage: node build-sites.mjs <${Object.keys(manifest.sites).join('|')}> [--out file] [--tag V#]`);
  process.exit(2);
}
// The release the pages pin: --tag (the linked deploy passes the tag it is cutting), otherwise the
// newest whole-number V<n> tag on this repo, i.e. the latest published release.
function latestTag() {
  let out = '';
  try { out = execFileSync('git', ['-C', WEB, 'tag', '--list', 'V*'], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] }); } catch {}
  const nums = out.split(/\r?\n/).map((t) => t.trim().match(/^V(\d+)$/)).filter(Boolean).map((m) => Number(m[1]));
  if (!nums.length) { console.error('No V<n> release tag in this repo; pass --tag V<n>.'); process.exit(2); }
  return `V${Math.max(...nums)}`;
}
const tag = opt('--tag') || latestTag();
const base = manifest.cdnBase.replace('{tag}', tag);
const themesBase = base.replace(/\/components$/, '/themes');
const WEB_ROOT = WEB;
const tokens = (text) => text.replaceAll('{{BASE}}', base).replaceAll('{{THEMES}}', themesBase);
const read = (rel) => fs.readFileSync(path.join(COMPONENTS, rel), 'utf8');
const json = (rel) => JSON.parse(read(rel));

// Everything below is assembled with \n and converted to the target's line endings at the end.
const builders = {
  /** Component files inserted verbatim (tokens filled) into the page's own <style> or <script>:
   *  for code written to live inside the host page, e.g. theme effects that share its scope. */
  raw: (b) => b.files.map((rel) => tokens(fs.readFileSync(path.join(WEB_ROOT, rel), 'utf8'))).join('').trimEnd(),

  /** One component stylesheet, inline. Site CSS placed after it can override anything. */
  style: (b) => `<style id="${b.id}">\n${read(b.css).trimEnd()}\n</style>`,

  /** One component script, inline (runs where it is placed, before any later page script). */
  script: (b) => `<script id="${b.id}">\n${read(b.js).trimEnd()}\n</script>`,

  /** A font whose woff2 ships inline as base64 (no network request): the component's comment
   *  banner, then a <style> with its @font-face rules, plus a font-family rule for the selector its
   *  descriptor names (applyToSelector, e.g. 'html, body'). */
  'font-inline'(b) {
    const dir = path.join(COMPONENTS, b.component);
    const pick = (ext) => fs.readdirSync(dir).find((f) => f.endsWith(ext) && !f.endsWith('.cdn.css'));
    const d = json(`${b.component}/${pick('.json')}`);
    let css = read(`${b.component}/${pick('.css')}`);
    if (d.applyToSelector) {
      css += `\n/* applyToSelector for this subscriber */\n${d.applyToSelector} { font-family: '${d.fontFamily}', ${d.fallback}; }\n`;
    }
    return `${read(`${b.component}/${pick('.html')}`)}\n<style id="${b.id}">\n${css}\n</style>`;
  },

  /** One component stylesheet spliced into an existing stylesheet (e.g. a Blazor app's app.css). */
  'css-file': (b) => read(b.css).trimEnd(),

  /** @font-face rules for each face (CDN-URL variant), optional --font-* variables, optional apply rules. */
  'font-faces'(b) {
    const meta = Object.fromEntries(b.faces.map((name) => {
      const descriptor = fs.readdirSync(path.join(COMPONENTS, name)).find((f) => f.endsWith('.json'));
      const d = json(`${name}/${descriptor}`);
      const cdnCss = fs.readdirSync(path.join(COMPONENTS, name)).find((f) => f.endsWith('.cdn.css'));
      return [name, { ...d, css: read(`${name}/${cdnCss}`).replaceAll('{{BASE}}', base),
        stack: `'${d.fontFamily}', ${d.fallback}`, variable: `--font-${d.fontFamily.toLowerCase()}` }];
    }));
    let out = `<style id="${b.styleId}">\n` + b.faces.map((n) => meta[n].css).join('\n');
    if (b.variables) {
      const vars = b.faces.map((n) => `${meta[n].variable}: ${meta[n].stack};`).join(' ');
      const names = b.faces.map((n) => `var(${meta[n].variable})`).join(' / ');
      out += `\n/* Ergonomic reuse: ${names} instead of\n   repeating the full stacks. */\n:root { ${vars} }\n`;
    }
    if (b.apply?.length) {
      out += '\n' + b.apply.map(([selector, n]) => `${selector} { font-family: ${meta[n].stack}; }`).join('\n') + '\n';
    }
    return out + '</style>';
  },

  /** The Cyberspace backdrop: scaffold HTML, CSS and the small scripts inline; the large engine and
   *  the libraries it draws from as deferred CDN scripts; circuit-board textures by CDN URL. */
  cyberspace(b) {
    const html = read(b.html);
    const css = read(b.css);
    const textures = fs.readdirSync(path.join(COMPONENTS, b.assets))
      .filter((f) => /^circuitboard\..*\.png$/.test(f)).sort()
      .map((f) => `${base}/${b.assets}/${f}`);
    const preloads = textures.map((u) => `<link rel="preload" as="image" fetchpriority="low" href="${u}">`).join('\n');
    const texOverride = textures.length
      ? `/* ---- circuitboard textures (jsDelivr URLs @${tag}) ---- */\nwindow.__cyberspaceCircuitboardSrcs = [\n  ${textures.map((u) => `'${u}'`).join(',\n  ')}\n];\n`
      : '';
    const leaf = (p) => p.split('/').pop();
    const inlineJs = b.js.filter((p) => !b.cdnJs.includes(leaf(p)))
      .map((p) => `/* ---- ${leaf(p)} ---- */\n${read(p)}`).join('\n\n');
    const cdnScripts = b.js.filter((p) => b.cdnJs.includes(leaf(p)))
      .map((p) => `<script id="mindattic-components-cyberspace-${leaf(p).replace(/\.js$/, '')}-js" src="${base}/${p}" defer></script>`)
      .join('\n');
    return '<!-- Generated by MindAttic.Components/web/build/build-sites.mjs. Do not edit by hand.\n' +
      '     Edit MindAttic.Components/web/components/Cyberspace/* and re-run the build.\n' +
      `     console-bg.js and its libraries load from jsDelivr (pinned tag); the linked deploy pins each new release. -->\n` +
      html + '\n' +
      (preloads ? preloads + '\n' : '') +
      '<style id="mindattic-components-cyberspace-css">\n' + css + '\n</style>\n' +
      '<script id="mindattic-components-cyberspace-js">\n' + (texOverride ? texOverride + '\n' + inlineJs : inlineJs) + '\n</script>\n' +
      cdnScripts;
  },
};

const assetsBase = base.replace(/\/components$/, '/assets');

// A site is one target file (target + blocks) or several (files: [{ target, blocks }]). copies
// ([componentFile, targetFile]) are plain file copies, e.g. a component's scripts into an app's wwwroot.
for (const [from, to] of site.copies || []) {
  const dest = path.resolve(WEB, to);
  fs.mkdirSync(path.dirname(dest), { recursive: true });
  fs.copyFileSync(path.join(COMPONENTS, from), dest);
  console.log(`  copy         ${from} -> ${path.relative(WEB, dest)}`);
}
for (const file of site.files || [site]) buildFile(file, site.files ? null : opt('--out'));

function buildFile(file, outFile) {
const targetPath = path.resolve(WEB, file.target);
let page = fs.readFileSync(targetPath, 'utf8');
const crlf = (page.match(/\r\n/g) || []).length;
const lf = (page.match(/(?<!\r)\n/g) || []).length;
const eol = crlf >= lf ? '\r\n' : '\n';
page = page.replace(/\r\n/g, '\n');

for (const [from, to] of site.rewrites || []) page = page.replaceAll(from, to.replace('{base}', base).replace('{assets}', assetsBase));
// Any Components URL already in the page follows the pinned tag.
page = page.replace(/(cdn\.jsdelivr\.net\/gh\/mindattic\/MindAttic\.Components@)[^/]+\//g, `$1${tag}/`);

for (const b of file.blocks) {
  // Markers use the comment syntax of where they sit: HTML by default, /* */ inside a <style>,
  // // inside a <script>.
  const [open, close] = { html: ['<!-- ', ' -->'], css: ['/* ', ' */'], js: ['// ', ''] }[b.syntax || 'html'];
  const begin = `${open}BEGIN MINDATTIC.COMPONENTS:${b.marker}${close}`;
  const end = `${open}END MINDATTIC.COMPONENTS:${b.marker}${close}`;
  const s = page.indexOf(begin);
  const e = page.indexOf(end, s);
  if (s < 0 || e < 0) throw new Error(`${siteName}: marker pair ${b.marker} not found. Add it to the page once, by hand, then re-run.`);
  const block = builders[b.type](b).replace(/\r\n/g, '\n');
  page = page.slice(0, s) + begin + '\n' + block + '\n' + end + page.slice(e + end.length);
  console.log(`  ${b.marker.padEnd(12)} ${b.type} (${(block.length / 1024).toFixed(1)} KB)`);
}

const outPath = outFile ? path.resolve(outFile) : targetPath;
fs.writeFileSync(outPath, eol === '\n' ? page : page.replace(/\n/g, '\r\n'));
console.log(`Built ${siteName} -> ${outPath} (@${tag})`);
}
