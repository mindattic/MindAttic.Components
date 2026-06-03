# MindAttic.UiUx

**One repo, every front-end. Drop-in CSS, JS, and HTML bundles, delivered three ways.**

A growing catalog of self-contained components — fonts, effects, helpers — that any subscriber can pull in via jsDelivr CDN at runtime, splice in via marker-block sync at build time, or accept as a cross-repo PR from GitHub Actions. Zero build step on the subscriber side, no `npm install`, no peerdeps.

Currently powers `mindattic.com`, the `StreetSamurai` Blazor home page, and the `MindAttic.Psst` legal pages via splice-in-place sync. Everything else in the MindAttic fleet — the catalog landing pages and the Claudia/ChiMesh long-form guides — is rendered by [`MindAttic.Deploy`](../MindAttic.Deploy/README.md), which pulls components from jsDelivr at runtime.

```html
<!-- pinned production -->
<script src="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.UiUx@v1.0.0/Components/Cyberspace/console-bg.js"></script>
<link  rel="stylesheet" href="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.UiUx@v1.0.0/Components/Cyberspace/frontpage.css">
```

**Why MindAttic.UiUx:**

- **Three delivery modes, one source of truth.** jsDelivr CDN for runtime, GitHub Actions for cross-repo PRs, PowerShell scripts for local dev — all reading the same [`subscribers.json`](subscribers.json).
- **Subscribers are declarative.** Add `{ "component": "AtticFont", "applyToSelector": ".site-name" }` to a subscriber's array. The next sync enrolls it. Remove the line, the next sync unenrolls. No hardcoded lists.
- **Versioned by tag, immutable on CDN.** `@v1.0.0` is edge-cached forever; `@main` always tracks tip-of-tree. Subscribers pick their guarantee.
- **Self-contained components.** Each folder ships its own source, usage HTML, markdown doc, and JSON config. No cross-component imports — you can vendor a single component without dragging the rest.
- **Marker-block contract.** Every splice is bounded by `BEGIN/END MINDATTIC.UIUX:<MARKER>` comments. Subscribers hand-author the rest of the file without conflict; only what's between the markers is regenerated.

---

## Components

Each component folder is fully self-contained: source files, a usage `.html`
comment, a `<FolderName>.md` doc, and any companion `.json` config.

| Component | Type | What it does | Docs |
|---|---|---|---|
| **[Cyberspace](Components/Cyberspace/Cyberspace.md)** | HTML + CSS + JS bundle | Cyberpunk console-background effects engine — 12 named effects (TERMINAL, CRASH, TREMOR, LEAK, SCHEMATIC, CASCADE, ARTIFACT × 7 variants, FRAGMENT, TRACE, PULSAR, HEIST, PREDATOR), scan-line overlay, parallax circuit-board, keepout zones around content. SCHEMATIC pulls its shapes from SacredGeometry. | [Cyberspace.md](Components/Cyberspace/Cyberspace.md) |
| **[SacredGeometry](Components/SacredGeometry/SacredGeometry.md)** | JS (UMD) + SVG | A catalog of 1024 unique, animatable line-art shapes (polyhedra, parametric curves, knots, fractals). One pure renderer targets a live `<canvas>` or a static SVG string; a Node build emits a poster per shape. Feeds Cyberspace's SCHEMATIC effect and the MindAttic.Legion persona-gallery avatars. | [SacredGeometry.md](Components/SacredGeometry/SacredGeometry.md) |
| **[OutfitFont](Components/OutfitFont/OutfitFont.md)** | font + CSS | Outfit variable font (Google Fonts, weights 100–900) inlined as base64 woff2. Two `@font-face` declarations (Latin + Latin-Extended) plus `:root { --font-outfit: 'Outfit', system-ui, sans-serif; }` for ergonomic reuse. | [OutfitFont.md](Components/OutfitFont/OutfitFont.md) |
| **[AtticFont](Components/AtticFont/AtticFont.md)** | font + CSS | Attic display face inlined as base64 woff2. Single `@font-face` plus `:root { --font-attic: 'Attic', serif; }`. Per-subscriber `applyToSelector` controls where Attic is auto-applied (`#claudia`, `#chimesh`, `.site-name`, …). | [AtticFont.md](Components/AtticFont/AtticFont.md) |
| **[PinFooter](Components/PinFooter/PinFooter.md)** | CSS + JS | Pin-when-short footer. Toggles `position: fixed; bottom: 0` on any element with class `pin-when-short` while the document is shorter than the viewport; releases it when content overflows. | [PinFooter.md](Components/PinFooter/PinFooter.md) |
| **[BackHomeM](Components/BackHomeM/BackHomeM.md)** | CSS only | A capital "M" in AtticFont pinned to the upper-left, linking back to mindattic.com. Used on satellite sites (Claudia, ChiMesh) so a visitor can always get home. | [BackHomeM.md](Components/BackHomeM/BackHomeM.md) |
| **[WebSnapshot](Components/WebSnapshot/WebSnapshot.md)** | Node CLI + browser viewer | Capture a fresh screenshot of any URL with Playwright, scale + crop it to a preview rectangle (cover-fit + alignment crop), and inline the result as a base64 data URI inside any `.web-snapshot` container. | [WebSnapshot.md](Components/WebSnapshot/WebSnapshot.md) |

---

## Layout

```
MindAttic.UiUx/
│
├── Components/
│   ├── Cyberspace/              # Cyberpunk console-background effects
│   │   ├── frontpage.html       #   DOM scaffolding (3 fixed-position layer divs)
│   │   ├── frontpage.css        #   12 effects + scan-lines + flicker keyframes
│   │   ├── console-bg.js        #   animation engine
│   │   ├── home-bg.js           #   torn-edge portrait compositor
│   │   ├── tv-static.js         #   navigation-transition TV-static overlay
│   │   ├── loader.js            #   tiny global loader show/hide helper
│   │   ├── index.htm            #   ad-hoc local test harness
│   │   ├── assets/              #   parallax textures (circuitboard.*.png)
│   │   └── Cyberspace.md
│   │
│   ├── SacredGeometry/          # 1024 animatable line-art shapes (feeds Cyberspace + Legion)
│   │   ├── sacred-geometry.js   #   catalog + canvas/SVG renderer (UMD)
│   │   ├── build-previews.mjs   #   emits previews/*.svg; doubles as smoke test
│   │   ├── package.json
│   │   ├── previews/            #   shape-0000.svg .. shape-1023.svg (built posters)
│   │   ├── index.htm            #   QA grid of every shape
│   │   └── SacredGeometry.md
│   │
│   ├── OutfitFont/              # Outfit variable font, base64 woff2
│   │   ├── outfit-font.html
│   │   ├── outfit-font.css      #   @font-face + --font-outfit
│   │   ├── outfit-font.json     #   { fontFamily, fallback, applyToSelector }
│   │   └── OutfitFont.md
│   │
│   ├── AtticFont/               # Attic display face, base64 woff2
│   │   ├── attic-font.html
│   │   ├── attic-font.css       #   @font-face + --font-attic
│   │   ├── attic-font.json      #   { fontFamily, fallback }
│   │   └── AtticFont.md
│   │
│   ├── PinFooter/               # Pin-when-short footer
│   │   ├── pin-footer.html
│   │   ├── pin-footer.css       #   .pin-when-short.pinned { position: fixed; bottom: 0 }
│   │   ├── pin-footer.js        #   toggles .pinned on resize / mutation / fonts.ready
│   │   └── PinFooter.md
│   │
│   ├── BackHomeM/               # "M" return-home anchor (upper-left)
│   │   ├── back-home-m.html
│   │   ├── back-home-m.css      #   AtticFont stack + position: fixed top/left
│   │   └── BackHomeM.md
│   │
│   └── WebSnapshot/             # Snapshot capture + inline base64 viewer
│       ├── web-snapshot.js      #   Playwright capture engine
│       ├── snapshot.js          #   CLI wrapper
│       ├── snapshots.config.js  #   declarative recurring targets
│       ├── web-snapshot-viewer.js #   browser-side loader (data-src → <img>)
│       ├── web-snapshot.css     #   .web-snapshot container behavior
│       ├── web-snapshot.html    #   paste-in usage template
│       ├── package.json         #   Playwright dep + npm scripts
│       ├── previews/            #   generated: <name>.b64.txt
│       └── WebSnapshot.md
│
├── sync/                        # Distribution scripts (PowerShell) -- only the three remaining splice-in-place subscribers
│   ├── _subscribers.ps1                   # helper dot-sourced by every sync script (reads subscribers.json)
│   ├── sync-all.ps1                       # umbrella runner (glob-discovers sync-*.ps1)
│   ├── sync-mindattic-com.ps1             # inlines bundles into mindattic.com/index.htm
│   ├── sync-streetsamurai.ps1             # rewrites StreetSamurai.Blazor wwwroot/*
│   ├── sync-mindattic-psst.ps1            # splices terms.htm + privacy.htm in MindAttic.Psst
│   ├── bootstrap-textures.ps1             # one-shot: pull circuitboard PNGs from StreetSamurai
│   ├── bootstrap-streetsamurai-appcss.ps1 # one-shot: insert CYBERSPACE markers into app.css
│   └── sync.md
│
├── subscribers.json             # canonical map: which components flow to which subscribers + per-subscriber config
├── README.md                    # (this file)
├── CLAUDE.md                    # working-directory rules for the AI agent
└── .github/                     # PIPELINES.md + workflows/sync-subscribers.yml
```

---

## Delivery pipelines

See [`.github/PIPELINES.md`](.github/PIPELINES.md) for the full setup
(including the one-time PAT step) and CDN URL/tagging conventions.

| Pipeline | What it does | When it runs |
|---|---|---|
| **jsDelivr CDN** | Serves any file at `https://cdn.jsdelivr.net/gh/mindattic/MindAttic.UiUx@<ref>/<path>` — versioned, edge-cached, no infra to run | Continuously; cache-immutable for `@v*` tags. Consumed by `MindAttic.Deploy` (pins `componentsVersion` in `projects.json`) for every catalog landing page and the Claudia/ChiMesh long-form builds |
| **GitHub Actions cross-repo sync** | On push to `main`, opens PRs against `mindattic/mindattic.com`, `mindattic/StreetSamurai`, and `mindattic/MindAttic.Psst` with refreshed marker blocks / wwwroot copies | Every push to `main` (workflow: [`.github/workflows/sync-subscribers.yml`](.github/workflows/sync-subscribers.yml)) |
| **PowerShell `sync/*.ps1`** | Local dev fallback — same logic as the Action, runs against your working copies. Also invoked by `MindAttic.Deploy` as a `preDeploy` hook for `mindattic.com` and `StreetSamurai` so the bundle is fresh before FTPS upload | Manual (`powershell -File sync/sync-all.ps1`) |

| Subscriber | Runtime source | In-repo copy |
|---|---|---|
| `mindattic.com` | Inlined HTML+CSS+JS marker blocks in `index.htm` | Refreshed by GitHub Action / `sync-mindattic-com.ps1` |
| `StreetSamurai` (Blazor) | `wwwroot/js/*.js` + CSS marker blocks in `wwwroot/app.css` | Refreshed by GitHub Action / `sync-streetsamurai.ps1` |
| `MindAttic.Psst` (legal pages) | Inlined HTML+CSS marker blocks in `terms.htm` + `privacy.htm` | Refreshed by GitHub Action / `sync-mindattic-psst.ps1`. The repo's `index.htm` is rendered by `MindAttic.Deploy` from its own `README.md` — not touched here |
| **Everyone else** (IdiotProof, GridGame2026, MindAttic.Legion, MediaButler, MindAttic.Vault, TaxRateCollector, ThinkTank, Tutor, MindAttic.Psst.Index, Claudia, ChiMesh) | jsDelivr CDN at runtime, pinned via `MindAttic.Deploy/projects.json:componentsVersion` | Owned by [`MindAttic.Deploy`](../MindAttic.Deploy/README.md); not in this repo's `subscribers.json` |

### GitHub Action PAT — `SUBSCRIBER_REPO_TOKEN`

The cross-repo sync workflow needs a fine-grained personal access token so
it can open PRs against subscriber repos. Stored as the repository secret
**`SUBSCRIBER_REPO_TOKEN`** at
[`Settings → Secrets and variables → Actions`](https://github.com/mindattic/MindAttic.UiUx/settings/secrets/actions).

Generate at
[github.com/settings/personal-access-tokens/new](https://github.com/settings/personal-access-tokens/new)
with:

| Field | Value |
|---|---|
| Resource owner | `mindattic` |
| Repository access | *All repositories owned by `mindattic`* — covers every current and future subscriber automatically |
| Expiration | ~1 year (rotate on calendar) |
| Permission: **Metadata** | Read-only *(auto-included on every fine-grained PAT)* |
| Permission: **Contents** | **Read and write** *(push the `auto/sync-components` branch)* |
| Permission: **Pull requests** | **Read and write** *(open/update the cross-repo PR)* |

Any other permission is unnecessary — leave Pages, Secrets, security
advisories, etc. unchecked. Full walkthrough including the one-time secret
upload step is in [`.github/PIPELINES.md`](.github/PIPELINES.md).

> **Never paste the PAT value into the repo, into chat, or into a commit
> message.** If you do, treat it as compromised — revoke it immediately on
> the PAT settings page and generate a fresh one before saving the new
> value into the GitHub secret.

**Local retrieval via [`MindAttic.Vault`](../MindAttic.Vault/README.md).** For
local tools that need to call the GitHub API on behalf of this repo, the
same PAT is mirrored into the family-wide token store at
`%APPDATA%\MindAttic\GitHub\tokens.json` under the key
`mindattic-uiux-pat`:

```csharp
using MindAttic.Vault.Credentials;

var pat = TokenStore.ForBucket("GitHub").Get("mindattic-uiux-pat");
```

The two stores (GitHub repo secret and Vault `tokens.json`) are *independent*
copies — rotating the PAT means updating both. Keep them in sync, or pick
one (the GitHub secret) as authoritative and rewrite the Vault entry from
it whenever the PAT changes.

---

## Subscribers config

**Canonical source of truth: [`subscribers.json`](subscribers.json)**.
It has two sections: a `components` registry (every shippable component
with its source files and marker name) and a `subscribers` map (one entry
per subscriber, declaring `target`, `syncScript`, and a `subscriptions`
array).

Per-subscription overrides — like AtticFont's `applyToSelector` — live on
the subscription entry. Override precedence: explicit subscription value >
component JSON default > no apply rule. Pass `null` on the subscription to
explicitly suppress (e.g. StreetSamurai opts out of AtticFont's auto-apply
rule).

```jsonc
"StreetSamurai": {
  "kind":       "blazor-wwwroot",
  "target":     "D:/Projects/MindAttic/StreetSamurai/v3/StreetSamurai.Blazor",
  "syncScript": "sync-streetsamurai.ps1",
  "subscriptions": [
    { "component": "OutfitFont" },
    { "component": "AtticFont",  "applyToSelector": null },
    { "component": "Cyberspace" },
    { "component": "PinFooter",  "jsOnly": true }
  ]
}
```

Every sync script reads this file via the shared `_subscribers.ps1`
helper and iterates its subscriber's `subscriptions` array — no subscriber has
a hardcoded component list anymore. Adding `{ "component": "BackHomeM" }`
to a subscriber's subscriptions enrolls that subscriber in the component on
the next run; removing the line unenrolls it.

| Subscriber kind        | What "add a subscription" means |
|---|---|
| `html-inline` (mindattic.com)         | Edit `subscribers.json` only **if** the component's type already has a `switch` case in `sync-mindattic-com.ps1`. New types need a builder + dispatch case. HTML marker pair must exist in `index.htm` once. |
| `blazor-wwwroot` (StreetSamurai)      | Edit `subscribers.json` only **if** the component's type already has a `switch` case in `sync-streetsamurai.ps1`. CSS marker pairs in `app.css` are one-time hand-inserts (`bootstrap-streetsamurai-appcss.ps1` helps). |
| `html-inline-multi` (MindAttic.Psst legal) | Same contract as `html-inline`, but `target` is a folder and `targets[]` lists the files (currently `terms.htm` + `privacy.htm`). |

Adding a brand-new catalog landing page or long-form HTML build is **not** done here — that work belongs in `MindAttic.Deploy/projects.json`.

---

## Marker contract

Every sync edit is bounded by a comment pair. HTML subscribers use
`<!-- BEGIN MINDATTIC.UIUX:<MARKER> --> … <!-- END … -->`; CSS
subscribers (including the `<style>` literal inside `build-html.js`) use
`/* == BEGIN MINDATTIC.UIUX:<MARKER>.CSS == */ … /* == END … == */`.
Anything outside the markers is left untouched, so subscriber projects can
hand-author the rest of the file without conflict.

The script-generated body always opens with a `Generated by …` comment
warning subscribers not to hand-edit, because the next sync will overwrite.

---

## Editing a component

Edit files in the component's folder (e.g. `Components/Cyberspace/console-bg.js`).
Push to `main` and the GitHub Action delivers to the three splice-in-place
subscribers, or run `sync/sync-all.ps1` locally for fast iteration without
round-tripping through GitHub.

```powershell
# from MindAttic.UiUx — push to all three splice-in-place subscribers in one shot (local)
powershell -File sync/sync-all.ps1

# or invoke an individual target
powershell -File sync/sync-mindattic-com.ps1
powershell -File sync/sync-streetsamurai.ps1
powershell -File sync/sync-mindattic-psst.ps1
```

Downstream copies are derived artifacts — never edit them directly; the
next sync overwrites whatever's between the marker pairs.

To ship a component change to the CDN-loaded subscribers managed by
[`MindAttic.Deploy`](../MindAttic.Deploy/README.md), tag a new version of
this repo and bump `componentsVersion` in `MindAttic.Deploy/projects.json`,
then run that repo's deploy.

---

## Adding a new component

1. Create a folder at the repo root with the source files
   (`<name>.html`, `<name>.css`, optional `<name>.js`, optional
   `<name>.json` for config-driven components, and a `<FolderName>.md` doc).
2. Register it in `subscribers.json` under `components` with its `type`
   (`font-css`, `static-css`, or `html-bundle`), source-file paths, and a
   base marker name (without `.CSS` / `.HTML` suffix).
3. Subscribe subscribers to it: add `{ "component": "<Name>" }` to each
   subscriber's `subscriptions` array in `subscribers.json`.
4. For each subscribed project:
   - Insert the marker pair into the subscriber's target file (one-time
     hand-edit).
   - For `html-bundle` types on `html-inline` / `blazor-wwwroot` subscribers,
     add a builder function + `switch` case in the relevant sync script if
     the type doesn't already have one.
5. Run `sync/sync-all.ps1` and confirm a clean splice.

---

## Adding a new subscriber

1. Add an entry to `subscribers.json` under `subscribers` with `kind`,
   `target`, `syncScript`, and `subscriptions`.
2. Create `sync/sync-<subscriber>.ps1` that dot-sources `_subscribers.ps1`,
   reads its subscriber via `Get-Subscriber`, and iterates `$sub.subscriptions`.
3. Make it idempotent — running twice with no source changes produces no diff.
4. `sync-all.ps1` picks it up automatically (it discovers `sync-*.ps1` by glob).
5. If the subscriber also needs the GitHub Action delivery, mirror the logic in
   `.github/workflows/sync-subscribers.yml`.

---

## Keepout zones (Cyberspace)

`console-bg.js` ships a keepout system that prevents effects from spawning
behind page content. The placer (`bestPos` / `safePos`) weights overlap
with these rects 4× a normal window overlap, so it strongly prefers
spawning in the margins.

Baked-in selectors — any host gets these for free:
- `.cyberspace-keepout` — opt-in marker; add to any container you want protected.
- `main` — both subscribers use `<main>` for their content area.
- `.home-content` — StreetSamurai's Home wrapper.
- `.board-grid` — any tab/tile board.

Hosts can extend at runtime:

```js
window.__cyberspaceKeepoutSelectors = '.foo, .bar';
```

---

## Cyberspace effect catalog

Canonical names + definitions live in the registry header of
`console-bg.js`. Toggles (`FX_*`) and spawn rates (`RATE_*`) are clustered
near the top of the file — flip any `FX_*` to `false` to kill that effect.

### Top-level effects (tick-loop dispatch)

| Name        | Spawn fn                  | Toggle / Rate              | What it does |
|-------------|---------------------------|----------------------------|--------------|
| **TERMINAL**| `spawnWindow`             | `FX_WIN` / remainder       | Generic console window (the workhorse) |
| **CRASH**   | `spawnError`              | `FX_ERROR` / 1%            | Fatal-error popup |
| **TREMOR**  | `spawnWarning`            | `FX_WARN` / 1%             | Warning popup |
| **LEAK**    | `spawnMemo`               | `FX_MEMO` / 4%             | Leaked corporate memo, character-by-character erase |
| **SCHEMATIC**| `spawnGeoWindow`         | `FX_GEO` / 10%             | Geometric schematic window (shape drawn from [SacredGeometry](Components/SacredGeometry/SacredGeometry.md)) |
| **CASCADE** | `spawnCascade`            | `FX_CASCADE` / 3%          | Burst of 3–6 cascaded console windows |
| **ARTIFACT**| `spawnArtifact`           | `FX_ARTIFACT` / 12%        | Floating glyph cluster — 7 variants below |
| **FRAGMENT**| `spawnFrag`               | `FX_FRAG` / 40%            | Floating code fragments (most frequent effect) |
| **TRACE**   | `spawnNetConnect`         | `FX_NET` / 8%              | Tron-cycle network wire route |
| **PULSAR**  | `spawnMorseDot`           | `FX_MORSE` / 5%            | Morse-code glowing dot |
| **HEIST**   | `spawnFolderRip`          | `FX_FOLDER` / 4%           | Folder-rip file-extraction sequence |
| **PREDATOR**| `spawnArtifactPredator`   | `FX_PREDATOR` / 1.2%       | Rare artifact-hunting swarm |

### ARTIFACT — 7 behavior variants

| Variant       | Behavior |
|---------------|----------|
| **SCATTER**   | Random blob; all glyphs drift one direction |
| **LATTICE**   | Fibonacci grid; whole lattice drifts with corner-wave delay |
| **ANCHOR**    | Stationary grid; glitches in place; emits feelers |
| **SLUG**      | Single grid crawls + per-cell undulation |
| **CENTIPEDE** | Multi-segment chain; peristaltic wave + leader feelers |
| **PULSE**     | Concentric Fibonacci rings; lub-dub heartbeat radiating outward |
| **WANDERER**  | Small grid walks the screen, pauses to "look around" |

### PULSAR — 2 modes

| Mode      | Share | Behavior |
|-----------|-------|----------|
| **BLINK** | ~90%  | Classic on/off pulse |
| **SHIFT** | ~10%  | Slides cardinal directions, color-swaps each symbol |

### TRACE — 3 sub-behaviors

| Sub       | Behavior |
|-----------|----------|
| **ARC**   | Sharp-turn spark burst at ~30% of corners |
| **ACK**   | Three-blink success signal then synced fade-out |
| **SEVER** | Direction-aligned CONNECTION-LOST message on failure |

### HEIST — 3 sequential phases

| Phase         | Behavior |
|---------------|----------|
| **HIGHLIGHT** | Cyan selection glow on adjacent run of files |
| **EXTRACT**   | Slide-right exit with shimmer + per-file stagger |
| **DISSOLVE**  | Window fade-out tied to extract completion |

### PREDATOR — 5 sequential sub-behaviors

| Sub          | Behavior |
|--------------|----------|
| **STALK**    | Off-screen swarm origin, homes on prey |
| **SCAN**     | Prey detection cone (max forward, min behind) |
| **FLEE**     | Prey panic-redirect of crawl vector away from swarm |
| **DEVOUR**   | Cell consume-and-convert (cell adopts wasp glyph then dissolves) |
| **DISPERSE** | Wasps scatter and fade after kill |
