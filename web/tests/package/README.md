# MindAttic.Components (web) — test suite

End-to-end tests that prove two things:

1. **The package is complete and sound** — MindAttic.Components' `web/` folder contains every file the sites need, laid out per
   [`docs/web/ASSETS.md`](../../../docs/web/ASSETS.md), well-formed (fonts decode, images decode, nothing downscaled or re-encoded) and
   described exactly by `assets-manifest.json`.
2. **The sites pull their data properly** — `mindattic.com`, `ryandebraal.com` and `mindatticcares.com` load with
   no failed requests or console errors, talk only to allowed hosts, pin the expected release tag, fetch only what they
   need (lazily, from the CDN), and keep their layout correct at every viewport.

Framework: **[Playwright Test](https://playwright.dev/docs/intro)**. It intercepts and asserts on network requests natively,
runs real Chrome across a viewport matrix, and records traces for failures. No browser download is needed — it drives the
Google Chrome that is already installed (`channel: 'chrome'`).

## Install

```powershell
cd web\tests\package
npm ci                       # installs @playwright/test + cross-env (see package-lock.json)
```

No Chrome? `set PW_CHANNEL=chromium` and run `npx playwright install chromium` once (or `PW_CHANNEL=msedge`).
Optional: `pip install fonttools brotli` enables the deep font check (the structural WOFF2 check always runs).

## Run

| Command | What it does |
|---|---|
| `npm test` / `npm run test:local` | Everything, **local mode** (default). Sites come from their folders in the MindAttic.Web checkout next to this one; every `cdn.jsdelivr.net/gh/mindattic/MindAttic.Components@<tag>/web/**` request is answered from *this working tree*, so it passes before a tag exists and fails loudly if a referenced file is missing. No other network. |
| `npm run test:live` | Everything against the **real sites and real CDN** (no interception). |
| `npm run test:assets` | Package tests only (no browser, ~2 s). |
| `npm run test:sites` | Site tests only (needs Chrome, ~20 s). |
| `npx playwright test --project=components` | Component tests only (Hyperspace library, Hyperspace Reader, Hyperspace page; ~20 s). |
| `npm run test:cdn` | Live CDN verification only (forces live mode). |
| `npm run baseline:update` | Re-record `baselines.json` after you **deliberately** add/replace an image (see below). |
| `npm run report` | Open the last HTML report (CI runs produce one). |

Environment variables:

- `TEST_MODE=live` — test the real sites and the real CDN instead of local copies.
- `SHARED_TAG=V3` — the tag every site must pin. Default: the highest whole-number `V<n>` tag of this repo (the tag the site
  build pins into every page by default), else `V1`.
- `SITES_ROOT=<dir>` — where the site folders live (default: a MindAttic.Web checkout next to this MindAttic.Components
  checkout). Point it at a folder of older page copies to prove a check catches a regression.
- `PW_CHANNEL` — `chrome` (default), `msedge` or `chromium`.
- `PW_OUTPUT_DIR=<dir>` — where traces/screenshots go (default `test-results/`). Playwright empties this folder at
  the start of a run, so give concurrent runs different folders.
- `GITHUB_TOKEN` — only raises the GitHub API rate limit for the tag test.

Chrome runs headless **with classic scrollbars** (`--hide-scrollbars` is removed in `playwright.config.mjs`), so
layout checks see the ~15-17px a desktop scrollbar takes — the overflow a Windows visitor would actually get.

Failures keep a Playwright trace: `npx playwright show-trace test-results/<test>/trace.zip`.

## What each group guards

### `specs/assets/` — the package (no browser)

| Spec | Guards |
|---|---|
| `layout.spec` | Every file the sites rely on exists (fonts, Cyberspace engine + textures, SacredGeometry, each site's logos/images/themes); each font family keeps its TTF source; **no `assets/` level** under a domain folder; domain folders are split into category folders; no junk files (`.DS_Store`, `Thumbs.db`, `*.tmp`, `*.bak`, leftover `_test.htm`). |
| `manifest.spec` | `assets-manifest.json` exists, is well formed, lists *exactly* the files on disk, and every size + SHA-256 matches; no duplicate files (MAU-LAW-5). |
| `naming.spec` | Lowercase kebab-case names, no spaces/`+`/`_`, zero-padded gap-free series (`sunset-01…20`). |
| `fonts.spec` | Every woff2 is structurally valid (signature, declared length, table directory, Brotli stream decodes to the promised size) — a corrupt woff2 makes browsers *silently* fall back to a system font, which is exactly how the old embedded Outfit-latin bug hid. TTF sources are valid sfnt. Deep check (when Python + fontTools exist): decodes, correct family, glyph count. |
| `images.spec` | Every PNG/JPEG/ICO/GIF decodes (CRCs, no truncation); Cyberspace textures are 1080×1920 PNGs; **nothing was downscaled** (dimensions ≥ baseline); **lossy files are byte-identical to their baseline** (never re-encoded). |
| `budgets.spec` | Per-type size ceilings and per-folder totals (`lib/budgets.mjs`). |

### `specs/sites/` — the sites (browser)

`common.spec` runs for every site: well-formed page; **no failed / non-2xx requests, no console errors, no uncaught
exceptions**; requests only to the site's origin + the allow-list (`lib/paths.mjs → SITES[*].allowedHosts`); pins exactly the
expected `@V<n>` tag (never `@main`, no stale tag); every MindAttic.Components URL in the HTML exists in the package and every asset is in
the manifest; `<link rel=preconnect>` to jsDelivr; HTML size budget and no oversized `data:` URIs (guards against
re-embedding base64); fonts actually load from the CDN (Outfit latin / Attic; latin-ext stays unfetched unless needed); no
horizontal scrollbar from 320 to 1920 px; comments make no "one file / no external requests" claims.

Site-specific:

* **`mindattic.spec`** — at ten viewports (320×568 … 3440×1440, 1920×300, 400×1920, 120×600): three equal-width buttons whose
  combined width equals the wordmark width (±0.5 px), lockup fully on-screen and centered (±1 px), no scrollbars, footer
  fixed to the bottom; fonts/colours (Attic wordmark, Outfit buttons, white text at rest and hover); links have the right
  `href`/`target`/`rel`; engine scripts are `defer`red, textures + fonts preloaded; the engine exposes every effect the tap
  handler uses; **tap-anywhere spawns an effect** (≥ 12 of 15 taps, auto-spawner off), tapping a link spawns nothing (no
  effect, no sparks) and opens the right site in a new window (external navigations are stubbed, so the test never leaves
  the sandbox). **Origins**: at 1600×1000 a tap clear of the keepout buffer zone starts an effect within 30px of the tap
  (measured the moment it lands); each of the 13 spawn functions (READER included) called with an origin returns `true` and starts there;
  an origin in a corner stays on screen and one beside the lockup never overlaps it; taps on the wordmark, the motto
  or in the 16px buffer margin spawn nothing. **Spark surge**: one pooled `canvas.cyberspace-surge` (fixed,
  `pointer-events: none`, z-index 1, in `<body>`) draws pixels, then within 1.5s its loop stops, frames stop advancing
  and the canvas is cleared; with `prefers-reduced-motion: reduce` there is only a flash (no particles) and the effect
  still spawns. **READER** (MAU-US-F6): `spawnHyperspaceReader` at an origin opens a Hyperspace Reader in the Cyberspace host on
  the tap point; it goes live, its Dimensional Threshold bar moves and its details scroll and advance, and its ttl closes it:
  the power-down line prints, its DOM is removed and no reader stays open.
* **`ryandebraal.spec`** — no base64 JPEG/PNG/WOFF2 payloads left; every image of the sunset/sakura series exists in the
  package + manifest; first paint requests *only* the Outfit latin font and the avatar; switching to the sakura / sunset / noir
  themes fetches that theme's artwork from the CDN with 200; the avatar lightbox opens and lazily loads the portrait; PDF
  export loads `html2pdf` on demand (stubbed in local mode, real in live mode).
* **`mindatticcares.spec`** — every `<img>` has `width`/`height`/`alt`, below-the-fold art is `loading=lazy`; first paint
  loads only the Home art; navigating to *Child's Play* / *Y2K* fetches that page's art on demand; deep links (`#y2k`,
  `#sec-budget`) work; the YouTube player is click-to-play (no third-party request before the click).

### `specs/components/` — components (browser and Node)

`hyperspace.spec` covers the Hyperspace component. **Shape library:** all 100 records are complete with unique
stable ids; every shape has geometry and projects to finite 2D and 3D coordinates within the normalised radius at
several times; every shape draws something onto a canvas in the browser. **Hyperspace Reader:** it opens, goes live,
moves the Dimensional Threshold bar, changes its readouts, scrolls and advances the details span, shows five
readouts and the UNRESOLVED flag, draws on its scope; `close()` freezes the readouts, prints the power-down line,
folds the window and removes its DOM; `ttl` and `closeAll()` work; reduced motion holds still; without the
library `show()` returns null. **Hyperspace page** (the `Hyperspace` folder of MindAttic.Web, opened as a local file, skipped
if absent): it pins the library to a whole-number tag whose file exists here, and its explorers draw and its
gallery boots to Ready both from the pinned CDN URL (answered from this tree) and from the
`../../MindAttic.Components/web/` fallback (CDN answering 404). three.js is fetched from its real CDN for the gallery; Google Fonts are blocked.

### `specs/cdn/` — live CDN (live mode only)

For every site, parses the tag it pins from the **live** HTML and checks that each referenced file is served by jsDelivr with
HTTP 200, the right `content-type`, `access-control-allow-origin: *`, a year-long/immutable `cache-control`, and **bytes +
SHA-256 identical to `assets-manifest.json`**. Also: tags are whole-number and contiguous (`V1…Vn`, via the GitHub API) and
the expected tag is published. Expect these to fail until the tag is pushed **and** the sites are deployed — that is the point.

### `pester/` — `tools/vendor.ps1` (PowerShell, no browser)

`build.Tests.ps1` runs the standalone vendoring build into a temp folder and checks that a component or theme is copied
byte-for-byte (subfolders included), reruns are identical, an ambiguous name (`Cyberspace`) needs `-Kind`, an unknown name
lists what exists, and no `Ideas/` folder is needed. Run from the repo root: `Invoke-Pester -Path web/tests/package/pester` (Pester 5+).

## Known issue encoded in the suite

`ryandebraal.com` overflows horizontally by 118 px at 320 px wide and 48 px at 390 px (measured on the page as committed
*before* the asset migration, so it is not a regression). `common.spec.mjs` marks that one test with `test.fail(...)`: it
passes while the bug exists and **fails the day it is fixed** — remove the `test.fail` line then.

## The baseline ratchet (`baselines.json`)

Records every raster asset's pixel size, plus the SHA-256 of every JPEG. The tests then enforce: images never get smaller, JPEGs
never change bytes, and every image is recorded. After adding or *deliberately* replacing an image run
`npm run baseline:update` and commit `baselines.json` with the asset (and regenerate the manifest with
`web\tools\build-asset-manifest.ps1`). A tag is immutable, so a changed lossy file normally ships under a new name or a new tag.

## Adding things

* **An asset** — put it under `assets/<domain>/<category>/` (or its component or theme folder), kebab-case; `web\tools\build-asset-manifest.ps1`;
  `npm run baseline:update`; `npm run test:assets`.
* **A site** — add an entry to `SITES` in `lib/paths.mjs` (dir, live URL, allowed hosts, HTML budget, fonts used) and a
  `<site>.spec.mjs` for its specifics; `common.spec` picks it up automatically. Add its `assets/<domain>` folder to `DOMAIN_ROOTS`.
* **A budget / allowed host** — edit `lib/budgets.mjs` / `lib/paths.mjs` and say why in the commit.

## CI

`ci.workflow.example.yml` is a ready-to-copy GitHub Actions workflow (kept here, **not** under `.github/`; copy it to
`.github/workflows/` at the MindAttic.Components root to enable it): it checks out MindAttic.Components and, beside it, MindAttic.Web (`SITES_ROOT`), runs `assets` + `sites` in local mode on every push, and `test:live` + `test:cdn` on a schedule and
after a release. Local-mode runs need no network beyond installing npm packages.

## Layout of this folder

```
web/tests/package/
├── package.json  playwright.config.mjs  baselines.json  README.md  ci.workflow.example.yml
├── lib/   paths.mjs budgets.mjs walk.mjs binfmt.mjs site-session.mjs static-server.mjs update-baselines.mjs font_check.py
├── specs/ assets/*.spec.mjs   cdn/cdn.live.spec.mjs   sites/{common,mindattic,ryandebraal,mindatticcares}.spec.mjs
│          components/hyperspace.spec.mjs
└── pester/ build.Tests.ps1
```
