# Cyberspace

The cyberpunk console-background suite. CSS + JS that turns any page into a
living terminal: console windows boot at the edges, glyph swarms drift in
formation, network tracers fork and acknowledge across the grid, Morse
pulsars blink in code, folder-rip heists exfiltrate files in real time, and
a parallax circuit-board hums behind everything under static scan-lines.

13 named effects, content-aware spawning that stays out of your layout,
spawn-at-a-point origins for tap handlers, a tap sonic pulse, and no build step. Drops into any page with three `<div>`s, one `<link>`, and
one `<script>`.

---

## Layout

```
Cyberspace/
├── frontpage.html   # DOM scaffolding — three fixed-position layer divs
├── frontpage.css    # CYBERSPACE rules, scan-lines, neon-flicker keyframes
├── console-bg.js    # the engine (13 effects, origins, sonic pulse, keepout system, parallax)
├── home-bg.js       # torn-edge portrait compositor — exposes window.homeBg
├── tv-static.js     # navigation-transition TV-static overlay
├── loader.js        # tiny global loader show/hide helpers
├── index.htm        # ad-hoc test harness — opens the whole bundle locally
└── assets/          # parallax textures (circuitboard.00..02.png, lossless)
```

---

## Mounting

In a page's `<body>` (after the rest of your content so z-index 0 sits behind
it):

```html
<link  rel="stylesheet" href="frontpage.css">

<div class="cyberspace-sl-fine"   aria-hidden="true"></div>
<div class="cyberspace-sl-coarse" aria-hidden="true"></div>
<div class="console-bg-host"      aria-hidden="true"></div>

<script src="loader.js"></script>
<script src="tv-static.js"></script>
<script src="home-bg.js"></script>
<script src="../SacredGeometry/sacred-geometry.js"></script>
<script src="../Hyperspace/hyperspace.js"></script>
<script src="../Hyperspace/hyperspace-reader.js"></script>
<script src="console-bg.js"></script>
```

`hyperspace.js` and `hyperspace-reader.js` (the [Hyperspace](../Hyperspace/Hyperspace.md)
component) must also load **before** `console-bg.js`: the READER effect opens a
`window.HyperspaceReader` over a random `window.Hyperspace` shape. Without them READER
declines (returns `false`) and nothing else changes.

`sacred-geometry.js` must load **before** `console-bg.js`: the SCHEMATIC effect draws its
shapes from `window.SacredGeometry` (the shape catalog lives in the
[SacredGeometry](../SacredGeometry/SacredGeometry.md) component). If it's
absent, schematic windows simply don't spawn — everything else is unaffected.

Production hosts pull the same files from jsDelivr instead of inlining:

```html
<script src="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Web@V12/MindAttic.Web.Shared/Components/SacredGeometry/sacred-geometry.js" defer></script>
<script src="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Web@V12/MindAttic.Web.Shared/Components/Hyperspace/hyperspace.js" defer></script>
<script src="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Web@V12/MindAttic.Web.Shared/Components/Hyperspace/hyperspace-reader.js" defer></script>
<script src="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Web@V12/MindAttic.Web.Shared/Components/Cyberspace/console-bg.js" defer></script>
<link  rel="stylesheet" href="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Web@V12/MindAttic.Web.Shared/Components/Cyberspace/frontpage.css">
```

---

## Keepout zones

`console-bg.js` weights overlap with content rects 4× a normal window
overlap, so effects strongly prefer the margins around your layout.

Baked-in selectors (free, no setup):

- `.cyberspace-keepout` — opt-in marker; add to any container you want
  protected.
- `main` — both subscribers (`Prose`, `mindattic.com`) use `<main>` for
  page content.
- `.home-content` — Prose's Home wrapper.
- `.board-grid` — any tab/tile board.

Add more at runtime:

```js
window.__cyberspaceKeepoutSelectors = '.foo, .bar';
```

The **buffer zone** is each keepout rect grown by `KEEPOUT_BUFFER` (16px).
`consoleBg.inKeepout(x, y)` (viewport %) says whether a point is inside it.

---

## Spawning at a point (origins)

Every spawn function on `consoleBg._demo` takes an optional origin `{ x, y }` in
viewport percent as its last (or only) argument, and then starts the effect
there:

```js
consoleBg._demo.spawnError({ x: 20, y: 30 });          // popup centred on 20%,30%
consoleBg._demo.spawnArtifact('SPIDER', { x: 80, y: 70 });
consoleBg._demo.spawnWindow({ x: 10, y: 15 });           // title bar opens on the point
```

Popups and memos are centred on it. Console windows (TERMINAL, SCHEMATIC, HEIST,
CASCADE) and the Hyperspace Reader (READER) open their title bar on it. Fragments type out of it, artifacts are
centred on it, and TRACE, PULSAR and PREDATOR start from it. With an origin,
PREDATOR first releases a LATTICE to hunt when there is no prey. The box is
clamped on screen and pushed out of the buffer zone, and its transform origin
is set to the point so it grows out of it. Without an origin nothing changes.
Each function returns `true` when it spawned and `false` when it declined.

---

## Tap sonic pulse

```js
consoleBg.spawnSparkBurst(x, y);   // viewport %, or spawnSparkBurst({ x, y })
```

SURGE is a quiet hint that a tap landed, not a show. A small soft glow and a
single thin ring ease outward a short distance and fade, done within 200ms.
No particles, nothing radiating in spokes. One pooled `canvas.cyberspace-surge`
is appended to `<body>` (`position: fixed`, `pointer-events: none`,
`z-index: 1`, `data-state="running|idle"`). Its `requestAnimationFrame` loop
stops and clears when idle. With `prefers-reduced-motion: reduce` it draws
only the glow, stationary, with no ring. `consoleBg._demo.sparkStats()`
returns live counters.

---

## Texture override

The parallax background tiles three `circuitboard.0X.png` images. Hosts can
swap them by setting (before `console-bg.js` evaluates):

```js
window.__cyberspaceCircuitboardSrcs = [
  'data:image/png;base64,…',  // or any URL
  'data:image/png;base64,…',
  'data:image/png;base64,…',
];
```

mindattic.com points these at pinned jsDelivr URLs (emitted by
`sync/sync-mindattic-com.ps1`). Prose leaves the default and serves them
via `/api/media/…`. The `assets/` folder here is the canonical, lossless source (pixel-identical
to the original artwork, re-encoded as 8-bit palette PNGs); edit it here and
regenerate `assets-manifest.json`.

---

## Effect catalog

Canonical names + definitions live in the registry header of
`console-bg.js`. Toggles (`FX_*`) and spawn rates (`RATE_*`) are clustered
near the top of the file — flip any `FX_*` to `false` to kill that effect
entirely.

See the package [`README.md`](../../README.md) for the full effect table
(TERMINAL, CRASH, TREMOR, LEAK, SCHEMATIC, CASCADE, ARTIFACT — including its
7 behavior variants — FRAGMENT, TRACE, PULSAR, HEIST, PREDATOR, READER).

**SCHEMATIC** is the geometric-shape window. Its shapes (and the canvas/SVG renderer) now
live in the standalone [SacredGeometry](../SacredGeometry/SacredGeometry.md) component —
1024 indexed shapes; `spawnGeoWindow()` picks a random index and calls
`window.SacredGeometry.draw(ctx, idx, phase, …)` each frame. Cyberspace still owns the
window chrome (`.cyberspace-geo-*`), the scrolling telemetry, keepout positioning, and TTL.

**READER** is the Hyperspace Reader from the [Hyperspace](../Hyperspace/Hyperspace.md)
component. `spawnHyperspaceReader(origin)` opens `window.HyperspaceReader.show()` inside the
`.console-bg-host` layer on a random shape of `window.Hyperspace`, with a ttl of 6 to 11 s:
the window acquires, goes live (Dimensional Threshold bar, rotating readouts, fast-scrolling
details) and then runs its own shutdown and removes its DOM. Cyberspace owns the placement:
a free spot (`bestPos`) without an origin; with one, the title bar opens on the point,
clamped on screen and pushed out of the buffer zone, like SCHEMATIC. Toggle `FX_READER`,
rate `RATE_READER` (2.5%), host switch `reader`. It declines when the Hyperspace scripts are
missing or three readers are already open.

---

## Editing

Edit files here. Push to `main` and the GitHub Action delivers to both
subscribers, or run `sync/sync-all.ps1` locally
for fast iteration without round-tripping through GitHub. Downstream copies
are derived artifacts — never edit them directly.
