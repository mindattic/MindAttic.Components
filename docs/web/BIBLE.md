---
codex: 1
project: MindAttic.Components (web)
code: MAU
layer: bible
status: living
updated: 2026-10-13
---

# MindAttic.Components (web) — Project Bible
> Single source of truth for what the `web/` half of MindAttic.Components IS, is NOT, and the rules that
> keep it coherent. README says how to build/run; this says how to think about the system.
> Formerly MindAttic.Web.Shared (a folder of the MindAttic.Web repo), and before that the MindAttic.UiUx repo.

## 1. The one sentence {#MAU-§1}
The `web/` folder of MindAttic.Components is the org's **one front-end component library and shared runtime
asset package**: a catalog of self-contained CSS/JS/HTML components and whole-page themes, plus the fonts,
logos, theme art and parallax textures of every MindAttic site, served by jsDelivr at a pinned whole-number
tag of MindAttic.Components and spliced into the hand-made pages that use them by one build
(`build/build-sites.mjs`, configured in `sites.json`).

## 2. The product promise {#MAU-§2}
- **One source of truth, two delivery modes.** A component is authored once under `components/` (or
  `themes/`); the CDN and the site build both read that same source — nothing is duplicated in source
  control. See [§4](#MAU-§4).
- **Zero build step on the consumer side.** No `npm install`, no peerdeps. A page either loads a pinned CDN
  file or receives a regenerated marker block.
- **Declarative targets.** `sites.json` is the canonical map of which component flows into which page (or
  app file), how, and with which URL rewrites; adding or removing a block is the entire enrollment action
  ([MAU-LAW-1](#MAU-LAW-1)).
- **Immutable, whole-number versioning on the CDN.** `@Vn` tags are edge-cached forever; `@main` tracks
  tip-of-tree. Consumers pick their guarantee ([HOUSE-LAW-1](../../../MindAttic.HouseRules.md#HOUSE-LAW-1)).
- **Self-contained components.** Each folder ships its own source and, where it has one, its markdown doc,
  usage HTML and JSON config. No cross-component imports — a single component can be vendored without the
  rest ([MAU-LAW-3](#MAU-LAW-3)).
- **Marker-block contract.** Every splice is bounded by `BEGIN/END MINDATTIC.COMPONENTS:<MARKER>` comments;
  only what is between the markers is regenerated ([MAU-LAW-2](#MAU-LAW-2)).
- **One shared asset backend.** The four sites (`mindattic.com`, `ryandebraal.com`, `mindatticcares.com`,
  `Hyperspace`), sibling folders in the MindAttic.Web repo, embed no binary assets; each references fonts,
  art and components by tag-pinned jsDelivr URL
  (`https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Components@V<n>/web/<path>`), all pinned to the same tag
  by `MindAttic.Deploy`'s linked deploy ([MAU-LAW-7](#MAU-LAW-7)). Layout, naming and quality rules are in
  [ASSETS.md](ASSETS.md); the verified file list is `assets-manifest.json`.

## 3. What it is NOT {#MAU-§3}
- **NOT a deploying package.** It owns no hosting. Its CDN consumers are the four sites, whose tag pin, CDN
  check and upload are done by `MindAttic.Deploy`'s linked deploy. A project's page is its GitHub README;
  nothing renders project pages from these components ([MAU-LAW-4](#MAU-LAW-4)).
- **NOT a place to hand-edit downstream copies.** Spliced blocks in the pages and apps are derived
  artifacts; the next build overwrites whatever is between the marker pairs.
- **NOT a multi-component framework with shared runtime.** There is no shared bundle, no cross-component
  import graph, no dependency resolver beyond per-component declared assets.
- **NOT semantically versioned.** Tags are whole numbers (`V1`, `V2`, …) only — never SemVer
  ([HOUSE-LAW-1](../../../MindAttic.HouseRules.md#HOUSE-LAW-1)).
- **NOT a package builder.** `tools/vendor.ps1` only copies one component or theme folder verbatim for
  vendoring; there is no Blazor RCL or bundling target here (the .NET packages live in `../dotnet/`).

## 4. Architecture canon {#MAU-§4}

```
        components/ + themes/ + assets/<domain>/        <-- single source of truth (web/)
                              |
              +---------------+----------------+
              |                                |
         jsDelivr CDN                   build/build-sites.mjs
   MindAttic.Components@Vn/web/          (splice-in-place, per sites.json)
              |                                |
      the four sites                  the four sites' marker blocks,
   (tag pinned by the                 MindAttic.Psst legal pages,
     linked deploy)                   Tutor's wwwroot (app.css + js)
```

### 4.1 Projects / top-level layout (under `web/`)
- `components/` — canonical component source, one folder each (catalog in
  [the catalog data](data/components.json) and the repo README). Each is self-contained:
  `<name>.{css,js,html}` + optional `<name>.json` config + optional `<FolderName>.md` doc. Fonts keep their
  woff2 files in `components/OutfitFont/fonts/` and `components/AtticFont/fonts/`; the Cyberspace parallax
  textures are in `components/Cyberspace/assets/`.
- `themes/` — whole-page themes: the 16 ryandebraal.com themes (palette CSS, optional effect JS drawn by
  `components/ThemeFx`, their artwork) and `themes/Cyberspace/` (`theme.css`, `body-prelude.html`,
  `deps.json`), a composed bundle no current page loads. See `themes/README.md`.
- `assets/<domain>/<category>/` — per-site runtime assets: `assets/mindattic.com/logos/`,
  `assets/mindatticcares.com/{logos,icons,images}/`, `assets/ryandebraal.com/images/`. See
  [ASSETS.md](ASSETS.md).
- `assets/archive/` — files no site references (font TTF sources, brand masters, art a page keeps inline),
  at the relative path they would have in the package. In the repo, not served ([MAU-LAW-7](#MAU-LAW-7)).
- `assets-manifest.json` — generated by `tools/build-asset-manifest.ps1`: path, bytes, SHA-256 and pixel
  size of every served asset (61 files).
- `sites.json` + `build/build-sites.mjs` (+ the PowerShell entry point `build/build-sites.ps1`) — the
  site build ([§4.3](#MAU-§4)).
- `tests/package/` — the Playwright suite that validates this package and the sites that consume it, and
  the Pester tests for `tools/vendor.ps1` (`tests/package/README.md`).
- `tests/parity/` — the parity harness that proves a rebuilt page is indistinguishable from its original.
- `tools/` — `vendor.ps1` (standalone vendoring copy), `build-asset-manifest.ps1`, `codex.ps1` (these docs'
  doctor and digest).
- Docs (this file and its siblings) live outside `web/`, in the repo's `docs/web/` folder, so they are
  never part of the served package.

### 4.2 Domain model (NOUNS)
- **Component** — a self-contained front-end bundle under `components/<Name>/`. The atom of the catalog.
- **Theme** — a whole-page look under `themes/<Name>/`: a palette, optionally an effect and artwork
  (`themes/Cyberspace/` instead references components via `deps.json`).
- **Asset** — a static runtime file (font, image, texture) served from this package over jsDelivr; lives
  under `assets/<domain>/<category>/`, a theme folder, or a component's own folder.
- **Domain folder** — `assets/<site>/` (`assets/mindattic.com/`, …), holding that site's own assets in
  category folders (`logos/`, `icons/`, `images/`).
- **Target** — a page or app file `sites.json` builds. Current entries:

  | Entry | Target | Blocks | Run by |
  |---|---|---|---|
  | `mindattic.com` | `../../MindAttic.Web/mindattic.com/index.htm` | FONTS, BUTTON, CAPTUREDEMO, CYBERSPACE | the site's linked-deploy `preDeploy` hook |
  | `Hyperspace` | `../../MindAttic.Web/Hyperspace/index.htm` | BUTTON | the same |
  | `ryandebraal.com` | `../../MindAttic.Web/ryandebraal.com/index.htm` | FONTS, the UI components, THEMES-CSS, THEMES-JS | the same |
  | `mindatticcares.com` | `../../MindAttic.Web/mindatticcares.com/index.htm` | FONTS, VIDEOEMBED, HASHPAGES, CAPTUREDEMO | the same |
  | `MindAttic.Psst` | `privacy.htm`, `terms.htm` | OUTFITFONT (base64, inline) | by hand |
  | `Tutor` | `Tutor/Tutor.Blazor/wwwroot/app.css` (+ scripts copied to `wwwroot/js/`) | USERLOGIN, USERCIRCLE, USERTIMEOUT | by hand |

- **Block** — one `{ type, marker, … }` entry on a target. Types: `style`, `script`, `raw`, `font-faces`,
  `font-inline`, `css-file`, `cyberspace`.
- **Marker block** — the `BEGIN/END MINDATTIC.COMPONENTS:<MARKER>` region in a target that the build
  regenerates; the comment syntax follows where it sits (`<!-- -->` in HTML, `/* */` in CSS, `//` in JS).

### 4.3 Key services (VERBS)
- **site build** (`build/build-sites.mjs <site> [--out <file>] [--tag <V#>]`; `build/build-sites.ps1 -Site
  <site> [-Tag <V#>]` for PowerShell callers) — fills every marker block of the target from component
  source, applies the target's URL rewrites (`{base}` = the pinned components URL, `{assets}` = the pinned
  assets URL), re-pins every Components URL already in the page to the tag, copies files listed under
  `copies`, and keeps the target's dominant line endings. Idempotent: a second run with unchanged sources
  writes byte-identical files.
- **CDN delivery** — implicit; jsDelivr serves any path at a pinned `@Vn` tag of MindAttic.Components (no
  infra here).
- **release** — `MindAttic.Deploy`'s linked deploy (`npm run deploy -- --uiux`, or `--site` for any of the
  four sites): preflight (MindAttic.Components and MindAttic.Web clean, on `main`, not behind origin;
  manifest current), pick the next tag of MindAttic.Components, rewrite the pins in the four site pages, run
  each site's `build-sites.ps1 -Tag <tag>` hook and stamp each page, commit "Pin MindAttic.Components V<n>"
  in MindAttic.Web, tag MindAttic.Components' `HEAD`, push both repos and the tag, verify every used asset on
  jsDelivr byte for byte, then upload ryandebraal.com, mindatticcares.com, Hyperspace and mindattic.com.
  When `HEAD` already carries the latest tag and the pins match, that tag is reused.
- **asset manifest** (`tools/build-asset-manifest.ps1`, `-Verify` fails if stale) — regenerates
  `assets-manifest.json`.
- **parity** (`tests/parity/parity.mjs <site> <original> <candidate> [--refactor]`) — proves a rebuilt page
  renders indistinguishably from the page it replaces.

## 5. The Laws {#MAU-§5}
This project **inherits the org-wide House Rules** verbatim — see
[`MindAttic.HouseRules.md`](../../../MindAttic.HouseRules.md): whole-number versioning
([HOUSE-LAW-1](../../../MindAttic.HouseRules.md#HOUSE-LAW-1)), soft-disable
([HOUSE-LAW-2](../../../MindAttic.HouseRules.md#HOUSE-LAW-2)), Vault-resolved credentials
([HOUSE-LAW-3](../../../MindAttic.HouseRules.md#HOUSE-LAW-3)), guarded-zip packaging lifecycle
([HOUSE-LAW-5](../../../MindAttic.HouseRules.md#HOUSE-LAW-5)), one-engine-many-front-doors
([HOUSE-LAW-6](../../../MindAttic.HouseRules.md#HOUSE-LAW-6)), verified-done
([HOUSE-LAW-8](../../../MindAttic.HouseRules.md#HOUSE-LAW-8)), and `psst`-only-on-request
([HOUSE-LAW-9](../../../MindAttic.HouseRules.md#HOUSE-LAW-9)). The following are the **project-specific** laws.

### MAU-LAW-1 — `sites.json` is the only enrollment list {#MAU-LAW-1}
No page or app has a hardcoded component list in a script. Which component flows into which target, as
which block type, and every URL rewrite, is declared in `sites.json`; `build/build-sites.mjs` reads it.
Adding or removing a block is the entire enrollment action. (It replaced the `subscribers.json` and `sync/*.ps1`
scripts of the former MindAttic.Web.Shared, now retired.)

### MAU-LAW-2 — Only marker blocks are regenerated {#MAU-LAW-2}
Every splice is bounded by a comment pair (`<!-- BEGIN MINDATTIC.COMPONENTS:<MARKER> -->` / CSS
`/* BEGIN MINDATTIC.COMPONENTS:<MARKER> */` / JS `// BEGIN MINDATTIC.COMPONENTS:<MARKER>`). Anything outside
the markers is left untouched except the target's declared URL rewrites. Spliced copies are derived
artifacts and are never hand-edited. Builds must be idempotent (running twice with no source change yields
no diff). A marker pair is added to a page once, by hand; the build refuses a target that lacks it.

### MAU-LAW-3 — Components are self-contained {#MAU-LAW-3}
Each `components/<Name>/` ships everything it needs and imports no other component at the source level
(runtime feeds like Cyberspace→SacredGeometry are explicit, optional, and resolved by the host). A single
component must be vendorable without dragging the rest.

### MAU-LAW-4 — The package does not deploy {#MAU-LAW-4}
Hosting, tag pinning and uploads belong to `MindAttic.Deploy`. Do not add targets or scripts that render
project pages; a project's page is its GitHub README.

### MAU-LAW-5 — Nothing is copied twice {#MAU-LAW-5}
Raw source under `components/` and `themes/` and every served asset exist in exactly one place. The
package holds no byte-identical duplicate files (enforced by `tests/package/specs/assets/manifest.spec.mjs`).
`tools/vendor.ps1` copies canonical files at build time into the git-ignored `dist/`; nothing commits copies.

### MAU-LAW-6 — Published CDN tags are immutable {#MAU-LAW-6}
Never mutate a published whole-number tag (`V1`, `V2`, …). Ship the next number alongside it; consumers pin
the exact one (the sites' `MindAttic.Components@V<n>/web/` URLs set by the build and the linked deploy).
This refines [HOUSE-LAW-1](../../../MindAttic.HouseRules.md#HOUSE-LAW-1).

### MAU-LAW-7 — Sites load assets from the package; only used files are served {#MAU-LAW-7}
A MindAttic site embeds no binary asset (base64 font, image, texture) in its HTML; it loads each from a
tag-pinned jsDelivr URL into this package. (The Psst legal pages are not sites of the linked group; they
inline the Outfit font on purpose.) Placement: used by one site → `assets/<domain>/<category>/`; owned by a
theme → that theme's folder; needed by a component (fonts included) → that component's folder. Every served
file is listed in `assets-manifest.json`; a file no site references lives in `assets/archive/` and is never
linked. Lossy files keep full resolution and bytes; lossless recompression must be pixel-identical; fonts
are validated before commit ([ASSETS.md](ASSETS.md)).

## 6. Verified state {#MAU-§6}
Status legend: ✅ done (verified) · 🟡 partial · ⬜ planned · living.

- ✅ **Shared asset package (`assets/`, font and Cyberspace component folders, theme art).** 61 served
  files, matching `assets-manifest.json` byte-for-byte (`tools/build-asset-manifest.ps1 -Verify`: current).
  Verified 2026-10-13 with the package suite below (manifest exact, fonts and images decode, budgets,
  naming, layout, `assets/archive/` kept out).
- ✅ **Sites consume the package at the pinned tag.** `tests/package/specs/sites/common.spec.mjs` and the
  per-site specs check mindattic.com, ryandebraal.com and mindatticcares.com for clean loads, allowed hosts,
  the expected tag (the newest `V<n>` tag, override `SHARED_TAG`), no overflow at every viewport and view,
  and CDN-loaded fonts. Verified 2026-10-13: `npm run test:local` in `web/tests/package` — 183 passed,
  6 skipped (the live-only CDN specs).
- ✅ **Cyberspace origins and the tap sonic pulse.** Every spawn function takes an optional origin
  `{ x, y }` (viewport %) and starts the effect there, clamped on screen and kept out of the keepout buffer
  zone (keepout rects + `KEEPOUT_BUFFER` 16px); `consoleBg.inKeepout` tests a point against the zone;
  `consoleBg.spawnSparkBurst` draws a quiet glow + thin ring tap hint on one pooled, click-through canvas
  whose loop stops at idle (flash only under reduced motion). Verified 2026-10-13:
  `tests/package/specs/sites/mindattic.spec.mjs` (stories MAU-US-E1 to E4) in `npm run test:local`.
- ✅ **Hyperspace shape library and Hyperspace Reader (`components/Hyperspace/`).** `hyperspace.js` holds
  the 159 exhibits of the Hyperspace page (stable ids, plaque metadata, geometry, pure projection for
  Three.js and 2D canvas); the page builds its gallery and explorers from it at a pinned tag, with a
  `../../MindAttic.Components/web/components/Hyperspace/` fallback when opened from disk.
  `hyperspace-reader.js` is the scanner window (`HyperspaceReader.show` / `close`) and the Cyberspace READER
  effect (`spawnHyperspaceReader(origin)` in `console-bg.js`). Verified 2026-10-13:
  `tests/package/specs/components/hyperspace.spec.mjs` (stories MAU-US-F1 to F5) and the READER test in
  `tests/package/specs/sites/mindattic.spec.mjs` (MAU-US-F6), in the same run.
- ✅ **Tag tip placement (`components/Tag/tag.js`).** `MindAtticTag.init({ tip })` builds a `.ma-tag` pill's
  hover tip from the page's content on first hover, flips it below near the top edge, keeps it on screen at the
  left/right edges and sets `--tt-arrow-x` so the arrow points at the tag; the `TAG` block of `sites.json` splices
  it in after `tag.css`. Verified 2026-10-20: `tests/package/specs/components/tag.spec.mjs` (MAU-US-G1).
- ✅ **Site build (`build/build-sites.mjs`).** The four sites rebuild at the pinned tag, and the parity
  harness (`tests/parity/`) proved each rebuilt page indistinguishable from the page it replaced (screenshots,
  computed styles, resource bytes) before it shipped.
- 🟡 **Component catalog (`components/`).** All components present with source; most UI components are
  verified through the sites suite and the parity harness, Hyperspace and Tag have component-level tests, and the
  rest are verified manually (harnesses such as `components/Cyberspace/index.htm`).
- 🟡 **Psst and Tutor targets.** `node build/build-sites.mjs MindAttic.Psst` and `… Tutor` run cleanly and are
  idempotent (a second run changes nothing); no automated test.
- ✅ **`tools/vendor.ps1` standalone copy.** `-Build <Name> [-Kind Component|Theme] [-Out <dir>]` copies the
  folder's files and subfolders byte-for-byte; a name that is both a component and a theme (Cyberspace)
  needs `-Kind`; an unknown name lists what exists. Verified 2026-10-13:
  `Invoke-Pester -Path web/tests/package/pester` — 8 passed (`tests/package/pester/build.Tests.ps1`).

## 7. Active frontier {#MAU-§7}
- An automated idempotency check for `build/build-sites.mjs` over every `sites.json` entry, so the Psst and
  Tutor targets can graduate to ✅.
- Epics and backlog: see [USER_STORIES.md](USER_STORIES.md).

## 8. Quality bar {#MAU-§8}
A feature is **done** (✅) only when:
1. It builds or runs clean where that applies — per [HOUSE-LAW-8](../../../MindAttic.HouseRules.md#HOUSE-LAW-8).
2. For a component change, `build/build-sites.mjs` rebuilds every affected target **idempotently** (a second
   run yields no diff), a page change is proven by `tests/parity/`, and a single component is independently
   vendorable ([MAU-LAW-3](#MAU-LAW-3)).
3. Canonical source is edited in `components/`/`themes/` only; no spliced copy is hand-edited
   ([MAU-LAW-2](#MAU-LAW-2)); nothing is copied twice ([MAU-LAW-5](#MAU-LAW-5)).
4. An asset change regenerates `assets-manifest.json` and passes `tests/package` (`npm run test:local`)
   ([MAU-LAW-7](#MAU-LAW-7)).
5. A shipped content change is published behind the **next whole-number tag**, never by mutating an
   existing one ([MAU-LAW-6](#MAU-LAW-6)).
6. The verifying evidence is named in the story (test token, or the command + observed result).

## 9. Glossary {#MAU-§9}
- **Component** — self-contained bundle under `components/<Name>/`; the catalog atom.
- **Theme** — whole-page look under `themes/<Name>/`.
- **Target** — a page or app file `sites.json` builds.
- **Block** — one marker region of a target and the component source that fills it.
- **Marker block** — `BEGIN/END MINDATTIC.COMPONENTS:<MARKER>` region the build regenerates.
- **Splice-in-place** — delivery mode that rewrites only the marker blocks in a target file.
- **jsDelivr CDN** — `cdn.jsdelivr.net/gh/mindattic/MindAttic.Components@<ref>/web/<path>`, the runtime delivery.
- **`sites.json`** — canonical map of targets, blocks and URL rewrites. The pinned tag is not stored: it is the newest `V<n>` tag of the repo.
- **`Vn` tag** — a whole-number release tag of MindAttic.Components; immutable once published
  ([MAU-LAW-6](#MAU-LAW-6)).
- **Linked deploy** — `MindAttic.Deploy`'s release flow that tags MindAttic.Components and pins that tag in
  the four sites together, in one MindAttic.Web commit.
- **Asset manifest** — `assets-manifest.json`; the generated, verifiable list of every served asset.
- **`assets/archive/`** — kept-but-unserved files ([MAU-LAW-7](#MAU-LAW-7)).
