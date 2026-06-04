# sync

PowerShell distribution scripts. They mirror what
`.github/workflows/sync-subscribers.yml` does on every push to `main`, but run
locally against working copies — so you can iterate on content changes
without round-tripping through GitHub.

This folder only handles the **three splice-in-place subscribers** that
MindAttic.UiUx still owns: `mindattic.com`, `StreetSamurai`, and the
`MindAttic.Psst` legal pages. Everything else in the MindAttic fleet
(catalog landing pages, Claudia, ChiMesh) is rendered by
[`MindAttic.Deploy`](../../MindAttic.Deploy/README.md) and pulls components
from jsDelivr at runtime — those subscribers are not present in
`subscribers.json` and have no script here.

Each `sync-*.ps1` targets one subscriber. Each subscriber has one or more
**marker blocks** (HTML comment pairs or CSS comment pairs) that the sync
script overwrites in place. Anything outside the markers is left alone.

`sync-all.ps1` is the umbrella runner — it invokes every `sync-*.ps1` it
finds and aggregates failures.

**Canonical config: [`../subscribers.json`](../subscribers.json)** declares
the component registry and which subscribers consume which components.
Every sync script reads this file via the shared helper `_subscribers.ps1`
and iterates its subscriber's `subscriptions` array — no subscriber has a
hardcoded component list. Per-subscription config (like AtticFont's
`applyToSelector`) lives on the subscription entry; the helper applies it
with precedence: explicit subscription override > component JSON default
> no apply rule.

| Subscriber kind                            | What "add a subscription" means |
|---|---|
| `html-inline` (mindattic.com)              | Edit `subscribers.json` only **if** the component's type already has a `switch` case in `sync-mindattic-com.ps1` (font-css, html-bundle for Cyberspace/PinFooter/WebSnapshot). New component types need a builder + dispatch case. Marker pair must exist in `index.htm` once. |
| `blazor-wwwroot` (StreetSamurai)           | Edit `subscribers.json` only **if** the component's type already has a `switch` case in `sync-streetsamurai.ps1`. New types need a dispatch case. CSS marker pairs in `app.css` are one-time hand-inserts (`bootstrap-streetsamurai-appcss.ps1` helps). |
| `html-inline-multi` (MindAttic.Psst legal) | Same contract as `html-inline`, but `target` is a folder and `targets[]` lists the files (currently `terms.htm` + `privacy.htm`). |

---

## Layout

```
sync/
├── _subscribers.ps1                   # helper dot-sourced by each sync-*.ps1 (reads subscribers.json)
├── sync-all.ps1                       # umbrella; invokes every sync-*.ps1 in this folder
├── sync-mindattic-com.ps1             # inlines bundles into mindattic.com/index.htm
├── sync-streetsamurai.ps1             # rewrites StreetSamurai.Blazor wwwroot files
├── sync-mindattic-psst.ps1            # inlines bundles into MindAttic.Psst/{terms,privacy}.htm
├── bootstrap-textures.ps1             # one-shot: pull circuitboard PNGs from StreetSamurai
└── bootstrap-streetsamurai-appcss.ps1 # one-shot: insert CYBERSPACE markers into app.css
```

---

## Pipelines vs scripts

| Trigger | What runs | When |
|---|---|---|
| **jsDelivr CDN** | Every tag is served at `https://cdn.jsdelivr.net/gh/mindattic/MindAttic.UiUx@<ref>/<path>` — versioned, edge-cached, no infra. Consumed by `MindAttic.Deploy` for every catalog landing page and Claudia/ChiMesh. | Continuously; cache-immutable for `@v*` tags. |
| **GitHub Action** | `.github/workflows/sync-subscribers.yml` opens cross-repo PRs against `mindattic/mindattic.com`, `mindattic/StreetSamurai`, and `mindattic/MindAttic.Psst` with refreshed marker blocks. | Every push to `main`. |
| **`sync/*.ps1`** | Same logic as the Action, but runs locally against working copies. Also invoked by `MindAttic.Deploy` as a `preDeploy` hook for `mindattic.com` and `StreetSamurai`. | Manual (`powershell -File sync-all.ps1`). |

---

## Per-script summary

### `sync-all.ps1`

Discovers every `sync-*.ps1` in this folder (excluding itself), runs them
sequentially, and aggregates failures. Safe to re-run after any edit.

```powershell
powershell -File sync/sync-all.ps1
```

### `sync-mindattic-com.ps1`

Inlines each MindAttic.UiUx group into `mindattic.com/index.htm` between
its own marker pair. Groups (in load order):

| Group | Marker pair | Source |
|---|---|---|
| OutfitFont  | `BEGIN/END MINDATTIC.UIUX:OUTFITFONT`  | `Components/OutfitFont/`  |
| AtticFont   | `BEGIN/END MINDATTIC.UIUX:ATTICFONT`   | `Components/AtticFont/`   |
| Cyberspace  | `BEGIN/END MINDATTIC.UIUX:CYBERSPACE`  | `Components/Cyberspace/`  |
| PinFooter   | `BEGIN/END MINDATTIC.UIUX:PINFOOTER`   | `Components/PinFooter/`   |
| WebSnapshot | `BEGIN/END MINDATTIC.UIUX:WEBSNAPSHOT` | `Components/WebSnapshot/` (CSS + viewer JS only; `.b64.txt` payloads are inlined per-tile by the subscriber) |

### `sync-streetsamurai.ps1`

1. Overwrites `wwwroot/js/{loader,tv-static,home-bg,console-bg}.js` from `Components/Cyberspace/`.
2. Overwrites `wwwroot/js/pin-footer.js` from `Components/PinFooter/`.
3. Rewrites the CYBERSPACE marker block in `wwwroot/app.css` from `Components/Cyberspace/frontpage.css`.
4. Rewrites the OUTFITFONT + ATTICFONT marker blocks in `wwwroot/app.css` from `Components/OutfitFont/` and `Components/AtticFont/`.

All CSS marker pairs must already exist in `wwwroot/app.css` before the
first run. If you're standing up a new subscriber, use
`bootstrap-streetsamurai-appcss.ps1` first to insert them.

```powershell
powershell -File sync/sync-streetsamurai.ps1
powershell -File sync/sync-streetsamurai.ps1 -BlazorRoot 'D:/path/StreetSamurai.Blazor'
```

### `sync-mindattic-psst.ps1`

Inlines OutfitFont into `MindAttic.Psst/terms.htm` and `MindAttic.Psst/privacy.htm`
between `<!-- BEGIN/END MINDATTIC.UIUX:OUTFITFONT -->` markers. The repo's
`index.htm` is NOT touched here — that file is rendered by `MindAttic.Deploy`
from `MindAttic.Psst/README.md`.

```powershell
powershell -File sync/sync-mindattic-psst.ps1
powershell -File sync/sync-mindattic-psst.ps1 -TargetRoot 'D:/path/to/MindAttic.Psst'
```

### `bootstrap-textures.ps1`

One-shot. Pulls the three `circuitboard.0X.png` parallax textures from
StreetSamurai's media folder into `Components/Cyberspace/assets/` as lossless
copies. JPEG block compression breaks the edge-pixel match between tile
copies and produces visible seams — keep these as PNG.

Re-run only when upstream PNGs change.

### `bootstrap-streetsamurai-appcss.ps1`

One-shot. Replaces a hard-coded line range in
`StreetSamurai/wwwroot/app.css` (the legacy CYBERSPACE block) with a
marker pair, so subsequent `sync-streetsamurai.ps1` runs have somewhere to
write into. Read the file before running on a new branch — the line numbers
are tied to a specific historical revision.

---

## Adding a new subscriber

Before adding one here, **make sure it doesn't belong in MindAttic.Deploy
instead**. If the subscriber:
- has a `README.md` that's already a polished long-form page, OR
- is a catalog landing page (just a title, tagline, button row, and the README rendered below), OR
- is a long-form HTML build (Claudia, ChiMesh class) that pulls components from CDN —

then it belongs in `MindAttic.Deploy/projects.json`, not here. Only add a
`sync-<name>.ps1` here if the subscriber genuinely needs build-time
splice-in-place (e.g. it has hand-authored content outside the marker
blocks, like `mindattic.com/index.htm` or `MindAttic.Psst/{terms,privacy}.htm`).

If you do need a new splice-in-place subscriber:

1. Create `sync-<subscriber>.ps1` in this folder.
2. Make it idempotent — running twice in a row should produce no diff.
3. Use marker pairs (HTML or CSS comments) so the script only touches its
   own region of each downstream file.
4. `sync-all.ps1` picks it up automatically (discovers `sync-*.ps1` by glob).
5. Mirror the logic in `.github/workflows/sync-subscribers.yml`.

---

## Idempotency contract

Every script in this folder must be safe to re-run. The canonical pattern:

1. Read the subscriber file.
2. Find the marker pair.
3. Replace everything between markers with the freshly-built block.
4. Write back.

That way a no-op edit produces no diff, and a real edit produces exactly
the diff that the source change implies.

### Line-ending normalization

Component sources are stored LF; most subscriber host files are CRLF.
Splicing LF blocks into a CRLF file would leave mixed endings and a huge
EOL-only git diff that buries the real content change in every sync PR.
To prevent that, each marker-splice script detects the host file's
dominant line ending *before* splicing (`Get-DominantEol` in
`_subscribers.ps1`, CRLF wins ties) and normalizes the whole written file
to it (`ConvertTo-Eol`) on the way out. So a subscriber only ever sees the
content diff, never an EOL flip. (The first sync after this was introduced
may show a one-time, content-free normalization for any host file that was
previously committed with mixed endings — `git diff --ignore-cr-at-eol`
confirms it's pure EOL. The wholesale `wwwroot/js/*` copies in
`sync-streetsamurai.ps1` are byte-for-byte with source and are not
normalized.)

### Cyberspace JS is CDN-loaded for mindattic.com

`console-bg.js` is ~580 KB and `sacred-geometry.js` (the SacredGeometry shape
catalog it draws from) is ~60 KB. Inlining them into `index.htm` bloats the page
on every load and prevents the browser from caching them separately. So
`sync-mindattic-com.ps1` keeps the small Cyberspace scripts (loader, tv-static,
home-bg) and the circuitboard texture override (jsDelivr URLs) inline, but emits
**both** `sacred-geometry.js` and `console-bg.js` as external
`<script src="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.UiUx@<tag>/…">`
tags (the CDN set is `@('sacred-geometry.js','console-bg.js')`). The tag is the
`-CyberspaceCdnTag` parameter (default `V4`).

Ordering is preserved because non-async `<script>` tags execute in document
order, and the CDN tags are emitted in `jsFiles` order: the inline block (which
defines `window.__cyberspaceCircuitboardSrcs`) runs first, then
`sacred-geometry.js` (defining `window.SacredGeometry`), then `console-bg.js`
(whose `TEX_SRCS` reads the circuitboard global and whose SCHEMATIC effect draws
from `window.SacredGeometry`) — both dependencies are in place before it runs.

To ship a change to either externalized file: edit the source, tag a new release
(next whole number, e.g. `V5`), bump `-CyberspaceCdnTag` to match, then re-run the sync/deploy.
A content change that *doesn't* touch those two files needs no tag bump. (Only
mindattic.com uses this split; StreetSamurai still copies all Cyberspace +
SacredGeometry JS into `wwwroot/js` byte-for-byte.)
