# Delivery pipelines

The `web/` folder of MindAttic.Components is the source of truth for shared front-end content (it replaced
MindAttic.Web.Shared, a folder of the MindAttic.Web repo). Content reaches its consumers two ways:

1. **jsDelivr CDN** — every file is served at a versioned URL. The CDN consumers are the four sites (`mindattic.com`, `ryandebraal.com`, `mindatticcares.com`, `Hyperspace`, folders of the MindAttic.Web repo), pinned to one whole-number tag by `MindAttic.Deploy`'s linked deploy.
2. **Site build** (`web/build/build-sites.mjs`, configured in `web/sites.json`) — splices component source into the marker blocks of hand-made files: the four site pages, the `MindAttic.Psst` legal pages and Tutor's `wwwroot`.

## Pipeline 1 — jsDelivr CDN (runtime)

Every file in `web/` is served by [jsDelivr](https://www.jsdelivr.com/) at a versioned URL of the public MindAttic.Components repo — zero setup, global edge cache.

**URL shape:**

```
https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Components@<ref>/web/<path>
```

Where `<ref>` is any of:

| Ref form | Example | Behavior |
|---|---|---|
| Whole-number tag (required for prod) | `@V3` | Immutable; cached forever |
| Branch | `@main` | Cached ~7 days unless purged |
| Commit SHA | `@a1b2c3d` | Immutable; cached forever |

Tags on MindAttic.Components start at `V1` and are contiguous. Older pages pinned `MindAttic.Web@V<n>/MindAttic.Web.Shared/` URLs; the site build rewrote every one of them to MindAttic.Components URLs.

Paths are case-sensitive on GitHub: the `web/` prefix and its lowercase folders (`components/`, `themes/`, `assets/`), the PascalCase component and theme folders (`components/Cyberspace`, `themes/Sakura`, …), and the lowercase domain folders under `assets/`.

**Examples:**

```html
<!-- pinned, production -->
<script src="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Components@V3/web/components/Cyberspace/console-bg.js" defer></script>

<!-- bleeding edge (dev only) -->
<script src="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Components@main/web/components/Cyberspace/console-bg.js"></script>
```

**Cutting a release:** you normally do not tag by hand. `MindAttic.Deploy`'s linked deploy (`npm run deploy -- --uiux`, or `--site` with any of the four sites) does it:

1. Preflight: MindAttic.Components and MindAttic.Web are clean, on `main`, not behind origin, and `web/assets-manifest.json` is current; the package tests (`web/tests/package`, `npm run test:local`) pass.
2. Picks the next whole-number tag of MindAttic.Components.
3. Rewrites the pins in the four site pages to that tag, runs each site's `preDeploy` hook (`web/build/build-sites.ps1 -Site <site> -Tag <tag>`) and stamps each page's Last Updated line. A hook that changes MindAttic.Components aborts the deploy: the tag must be the package exactly as committed.
4. Commits the page changes in MindAttic.Web as "Pin MindAttic.Components V<n>" and tags MindAttic.Components' `HEAD`.
5. Pushes MindAttic.Components (`main` and the tag) and MindAttic.Web (`main`).
6. Verifies every asset the sites use on jsDelivr at that tag, byte for byte.
7. FTPs ryandebraal.com, mindatticcares.com, Hyperspace and mindattic.com, in that order.

When `HEAD` already carries the latest tag and the pins match it, that tag is reused. Runtime assets (fonts, logos, theme art) are documented in [`ASSETS.md`](ASSETS.md).

To tag by hand (from the MindAttic.Components root), then rebuild the sites (the build pins the newest tag):

```bash
git tag -a V<n> -m "..."     # whole numbers only, never SemVer; never move a published tag
git push origin main V<n>
# jsDelivr serves the new tag within a minute
```

**Purging the cache (rare, only needed for branch refs):**

`https://purge.jsdelivr.net/gh/mindattic/MindAttic.Components@main/web/components/Cyberspace/console-bg.js` — GET to purge.

## Pipeline 2 — the site build (splice-in-place)

`web/sites.json` lists every target file, the marker blocks to fill in it and any URL rewrites. Each block replaces whatever sits between `BEGIN MINDATTIC.COMPONENTS:<MARKER>` and its END marker; nothing else in the file is touched. Targets resolve against `web/`, so `../../MindAttic.Web/...`, `../../MindAttic.Psst/...` and `../../Tutor/...` are repos cloned next to MindAttic.Components.

```powershell
node web/build/build-sites.mjs mindattic.com                       # one entry, in place
node web/build/build-sites.mjs mindattic.com --out candidate.htm   # single-file entry, to another file
powershell -File web/build/build-sites.ps1 -Site Tutor             # same, for PowerShell callers
```

| Entry | What the build writes | Run by |
|---|---|---|
| `mindattic.com` | FONTS (`@font-face` by CDN URL), BUTTON, CAPTUREDEMO and CYBERSPACE (scaffold, CSS and small scripts inline; `sacred-geometry.js`, `hyperspace.js`, `hyperspace-reader.js` and `console-bg.js` as deferred CDN scripts; texture preloads) | the linked deploy's `preDeploy` hook |
| `ryandebraal.com` | FONTS, the UI components (Toolbar, ToolButton, IconButton, Select, Range, Menu, Modal, Lightbox, Tag, ProgressBar, FormRow, Avatar, SkipLink), CAPTUREDEMO, and the 16 themes' CSS and effect JS inside the page's own `<style>` / `<script>` | the same |
| `mindatticcares.com` | FONTS, VIDEOEMBED, HASHPAGES, CAPTUREDEMO | the same |
| `Hyperspace` | BUTTON (the page also loads `components/Hyperspace/hyperspace.js` by pinned URL) | the same |
| `MindAttic.Psst` | OUTFITFONT in `privacy.htm` and `terms.htm`: the Outfit font inline as base64, so the legal pages make no network request | by hand, after an OutfitFont change |
| `Tutor` | USERLOGIN, USERCIRCLE, USERTIMEOUT CSS in `Tutor.Blazor/wwwroot/app.css`; their scripts copied to `wwwroot/js/` | by hand, after a change to those components |

The build is idempotent: rerunning it with unchanged sources writes byte-identical files, keeping each target's line endings. A page change is proven by the parity harness (`web/tests/parity`) before it ships.

## Choosing a pipeline for a new consumer

| Consumer kind | Recommended | Why |
|---|---|---|
| A site of the linked group | CDN for fonts, art and heavy scripts; a `sites.json` entry for CSS and small scripts that belong inline | Heavy assets are cached once on the CDN and shared; the page stays one hand-authored file |
| A page that must make no network request (legal pages) | `font-inline` / `style` / `script` blocks only | Everything inline |
| A Blazor app | `css-file` blocks in its `app.css` plus `copies` for scripts | The Blazor build needs deterministic input |
| Project pages (IdiotProof, Claudia, ChiMesh, …) | — (none) | Each repo's GitHub README is its project page |
