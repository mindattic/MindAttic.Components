# WebSnapshot

Capture a fresh screenshot of any web page (or download any direct image URL),
crop it to a fixed preview rectangle, and save the result as a base64 data URI
ready to inline into any page.

Two halves work together:

1. **Engine + CLI** — Node.js + Playwright. Visits a URL, takes the shot,
   crops it by alignment, writes `previews/<name>.b64.txt`.
2. **Viewer** — browser JS+CSS that finds `.web-snapshot` elements and loads
   the b64 data URI into the inner `<img>`.

The engine always launches a fresh Chromium context, so every run captures
the live current version of the page — no cache reuse, no stale pixels.

---

## Layout

```
WebSnapshot/
├── web-snapshot.js          # capture engine (Node.js + Playwright)
├── snapshot.js              # CLI wrapper around the engine
├── snapshots.config.js      # declarative list of recurring targets
├── web-snapshot-viewer.js   # browser-side loader
├── web-snapshot.css         # base styles for .web-snapshot containers
├── web-snapshot.html        # paste-in usage template
├── package.json             # Playwright dep + npm scripts
└── previews/                # generated; one .b64.txt per target
```

---

## Install

One-time, in the `WebSnapshot/` folder:

```powershell
npm install
npm run install:browser
```

`npm install` only pulls the Playwright JS package. Run `npm run install:browser`
to fetch the headless Chromium binary (~150 MB) into the local cache — required
only for users of the capture CLI; the browser-side `web-snapshot-viewer.js`
does not need it.

---

## CLI

All forms produce `previews/<name>.b64.txt` (overwriting any prior file).

| Form | Behavior |
|---|---|
| `node snapshot.js` | Refresh every target listed in `snapshots.config.js`. |
| `node snapshot.js <name>` | Refresh one configured target. |
| `node snapshot.js <url>` | Ad-hoc; name derived from URL basename or host. |
| `node snapshot.js <name> <url>` | Ad-hoc with an explicit name. |

Flags (apply in any form; override config and defaults):

| Flag | Default | What it does |
|---|---|---|
| `--align=<spec>` | `center-top` | Crop position. Accepts any combo of `left/center/right` and `top/middle/bottom` in either order: `top-center`, `center-top`, `top-left`, `right-bottom`, `center-middle`, etc. |
| `--size=WxH` | `150x225` | Output crop dimensions in CSS px. |
| `--input=WxH` | (full viewport) | Region to grab from the viewport before scaling. |
| `--viewport=WxH` | `1280x1600` | Browser viewport the page renders into. Crop is taken from inside this. |
| `--delay=<ms>` | `5000` | Settle time after `load` before the screenshot fires. |
| `--wait=<event>` | `load` | Playwright `waitUntil`: `load`, `domcontentloaded`, `networkidle`. |

Examples:

```powershell
# default — uses snapshots.config.js
npm run snapshot

# ad-hoc, full control
node snapshot.js ryandebraal https://ryandebraal.com/ --align=top-center --size=150x225

# direct image URL — flags ignored, raw bytes saved
node snapshot.js logo https://www.place.com/picture.jpg
```

Direct image URLs (matched by extension or `Content-Type: image/*` detected by
a probe request) skip Playwright entirely. The bytes are downloaded as-is and
written as the data URI — no crop.

Output naming: `previews/<name>.b64.txt` — the full `data:image/png;base64,…` string.

---

## Consuming on a page

Two ways to feed the viewer.

**Fetch mode** — the viewer pulls the data URI from disk.

```html
<link rel="stylesheet" href="path/to/WebSnapshot/web-snapshot.css">
<script src="path/to/WebSnapshot/web-snapshot-viewer.js" defer></script>

<div class="web-snapshot" style="display:inline-block;width:150px;height:225px;line-height:0"
     data-src="path/to/WebSnapshot/previews/ryandebraal.b64.txt">
  <img alt="ryandebraal.com preview">
</div>
```

**Inline mode** — no network. Set `<img src>` to a data URI you've already
inlined into the page (e.g., from a sync-time marker block).

```html
<div class="web-snapshot" style="display:inline-block;width:150px;height:225px;line-height:0">
  <img src="data:image/png;base64,…">
</div>
```

The CSS only defines layout (overflow + cover-fit `<img>`); the host controls
container size. Mark any container with class `.web-snapshot` and the viewer
will populate the `<img>` inside it.

Per-element overrides via data-attributes:

| Attribute | What it does |
|---|---|
| `data-src` | URL to a `.b64.txt`. If omitted, the `<img>` keeps whatever `src` you set. |

Public API on `window.WebSnapshot`:

```js
WebSnapshot.attach(el, opts?);   // manual attach (auto-init handles this)
WebSnapshot.refresh(el);         // re-fetch the .b64.txt (cache-busted)
WebSnapshot.autoInit(root?);     // re-scan for .web-snapshot under root
```

---

## Adding a recurring target

Edit `snapshots.config.js`:

```js
module.exports = [
  { name: 'ryandebraal', url: 'https://ryandebraal.com',
    width: 150, height: 225, align: 'top-center',
    viewport: { width: 1280, height: 1600 } },
  // add more here…
];
```

Run `npm run snapshot` to refresh all, or `npm run snapshot <name>` for one.

---

## Sync delivery

`sync/sync-mindattic-com.ps1` inlines `web-snapshot.css` and
`web-snapshot-viewer.js` into `mindattic.com/index.htm` between
`<!-- BEGIN/END MINDATTIC.UIUX:WEBSNAPSHOT -->` markers. The `.b64.txt`
payloads are *not* synced — each subscriber manages its own per-tile inline
base64 (mindattic.com keeps them in a `PORTFOLIO_IMAGES` map applied at
render time).

On mindattic.com, the portfolio tile builder adds `.web-snapshot` to any tile
whose name appears in `PORTFOLIO_IMAGES`, then calls `WebSnapshot.autoInit()`
after `tabifyPortfolio()` builds the DOM so the viewer attaches to the
freshly-created containers.

---

## Programmatic use

```js
const { webSnapshot } = require('./web-snapshot');

await webSnapshot({
  url: 'https://ryandebraal.com',
  width: 150, height: 225, align: 'top-center',
  outputPath: '/abs/path/to/ryandebraal.b64.txt',
});
```

`webSnapshot()` returns `{ outputPath, mode, mimeType, byteLength, dataUri }`
(plus dimensions in page mode). The same data URI is also returned in the
result, so callers don't have to re-read the file.
