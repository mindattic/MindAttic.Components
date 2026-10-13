---
codex: 1
project: MindAttic.Components (web)
code: MAU
layer: stories
status: living
updated: 2026-10-13
---

# MindAttic.Components (web) — User Stories
> ✅ done (shipped & tested) · 🟡 partial · ⬜ planned. Every ✅ cites the test.
>
> Tests live in `web/tests/package/` (Playwright, run from that folder; Pester in its `pester/` folder; see
> [BIBLE §6](BIBLE.md#MAU-§6)). Capabilities that exist but are only manually verified are 🟡, with the
> command that demonstrates them noted as *evidence*.

## Epic A — Authoring & catalog
- **MAU-US-A1 🟡** As a component author, I can add a self-contained component under `components/<Name>/`
  (source, plus a `.md` doc and `.json` config where it needs them) without touching any other component,
  so the catalog grows by addition. *Given a new folder, When I add a block for it to a target in
  `sites.json`, Then the next build ships it.* *(Hyperspace has component-level tests (Epic F); the UI
  components are covered through the sites suite and the parity harness; the rest are manual.)*
- **MAU-US-A2 🟡** As an author, I can add a whole-page theme under `themes/<Name>/` (palette CSS, optional
  effect drawn by `components/ThemeFx`, its artwork), so a page re-skins by switching theme. *(16 themes used
  by ryandebraal.com; `themes/Cyberspace/` is a composed bundle via `deps.json`; manual.)*
- **MAU-US-A3 🟡** As an author, I can regenerate SacredGeometry shape posters with
  `build-previews.mjs`, which doubles as a smoke test, so the 1024-shape catalog is self-checking.
  *(Script present; not run this session.)*

## Epic B — Distribution
- **MAU-US-B1 🟡** As a maintainer, I can enroll or unenroll a page in a component by editing one block in
  `sites.json`, so enrollment is declarative ([MAU-LAW-1](BIBLE.md#MAU-LAW-1)). *Given a block added (and
  its marker pair added to the page once), When the build runs, Then the block is filled.* *(Manual — no
  automated assertion in-repo.)*
- **MAU-US-B2 🟡** As a maintainer, I can run `node web/build/build-sites.mjs <entry>` for any `sites.json`
  entry and have only the `BEGIN/END MINDATTIC.COMPONENTS` marker blocks (and the declared URL rewrites)
  change, idempotently ([MAU-LAW-2](BIBLE.md#MAU-LAW-2)). *(Evidence 2026-10-13: rerunning the
  `MindAttic.Psst` and `Tutor` entries left both repos unchanged; no automated test.)*
- **MAU-US-B3 ✅** As a site maintainer, I can load any package file from `cdn.jsdelivr.net/gh/mindattic/MindAttic.Components@<Vn>/web/...` pinned to an immutable tag that every site shares ([MAU-LAW-6](BIBLE.md#MAU-LAW-6)). *(verified by `pins the expected MindAttic.Components tag`.)*
- **MAU-US-B4 ✅** As a maintainer, I can prove a rebuilt page is indistinguishable from the page it replaces before shipping it. *(verified by `Indistinguishable. Artifacts:`, the verdict `web/tests/parity/parity.mjs` prints when every scenario in `parity.config.mjs` has identical screenshots, computed styles and resource bytes.)*

## Epic C — Shared asset package
- **MAU-US-C1 ✅** As a site maintainer, I get every served asset listed in `assets-manifest.json` with its exact size and SHA-256, so a tag can be verified byte-for-byte ([MAU-LAW-7](BIBLE.md#MAU-LAW-7)). *(verified by `byte size and SHA-256 of every file match the manifest`.)*
- **MAU-US-C2 ✅** As a visitor, every font and image the sites load decodes cleanly, so no browser falls back silently. *(verified by `every raster asset decodes cleanly`.)*
- **MAU-US-C3 ✅** As a maintainer, files no site uses stay in `assets/archive/` and are never served or linked. *(verified by `assets/archive/ is kept out of the package`.)*
- **MAU-US-C4 ✅** As a visitor, each of the sites (mindattic.com, ryandebraal.com, mindatticcares.com) loads with no failed requests or console errors and no horizontal scrollbar at any viewport. *(verified by `no horizontal scrollbar at common viewport sizes`.)*

## Epic D — Packaging
- **MAU-US-D1 ✅** As a consumer, I can run `web/tools/vendor.ps1 -Build <Name> -Output standalone` to copy a component's (or, with `-Kind Theme`, a theme's) raw canonical files verbatim, so I can vendor without packaging. *(verified by `copies a component folder verbatim`.)*

## Epic E — Cyberspace interaction
- **MAU-US-E1 ✅** As a host page, I can pass an origin `{ x, y }` (viewport %) to any Cyberspace spawn function so the effect starts at that point, clamped on screen and kept out of the keepout buffer zone (keepout rects + 16px), while a call without an origin places itself as before. *Given* an origin clear of the zone, *when* I spawn any of the 12 effects, *then* it starts within 30px of the point, and near the zone or a corner it is pushed or clamped so it neither overlaps the keepout nor leaves the screen. *(verified by `every spawn function honours an origin: starts there, stays on screen, stays out of the keepout` and `a tap outside the keepout spawns an effect that starts at the tap point`.)*
- **MAU-US-E2 ✅** As a host page, I can call `consoleBg.spawnSparkBurst(x, y)` for a quiet tap hint (a small soft glow plus a single thin ring that eases out a short distance and fades, no particles) within ~0.2s, drawn on one pooled click-through canvas whose animation loop stops at idle. *(verified by `the tap sonic pulse draws on one pooled click-through canvas, animates, then goes idle`.)*
- **MAU-US-E3 ✅** As a visitor who prefers reduced motion, a sonic pulse is only a brief stationary flash. *(verified by `prefers-reduced-motion: a tap gives only a brief flash (no sparks), and the effect still spawns`.)*
- **MAU-US-E4 ✅** As a host page, I can ask `consoleBg.inKeepout(x, y)` whether a point is in the buffer zone, so taps there (and on links) spawn nothing. *(verified by `a tap inside the keepout buffer zone spawns nothing (no effect, no sparks)` and `tapping a link opens that site in a new window and spawns no effect and no sparks`.)*

## Epic F — Hyperspace shapes and the Hyperspace Reader
- **MAU-US-F1 ✅** As a page author, I can load one standard library of the 159 Hyperspace exhibits (`components/Hyperspace/hyperspace.js`), where each shape has a stable id, its plaque metadata (name, tag, family, native dimension, Schläfli symbol, facts, article, references) and its geometry. *(verified by `loads every shape with a complete record and a unique stable id`.)*
- **MAU-US-F2 ✅** As a renderer (Three.js or 2D canvas), I can project any shape at any time with pure functions and always get finite coordinates inside the normalised radius. *(verified by `every shape has geometry and projects to finite 2D and 3D coordinates` and `loads in a browser and draws every shape onto a canvas`.)*
- **MAU-US-F3 ✅** As a host page, I can call `HyperspaceReader.show({ x, y, shape })` to open a scanner window that locks on to a shape (rotating wireframe, a climbing and spiking Dimensional Threshold bar, rotating readouts, flags, fast-scrolling details) and `close()` it, which freezes the readouts, prints a power-down line, folds the window away and removes its DOM. *(verified by `opens, animates the threshold bar, scrolls the details and closes with its DOM removed` and `ttl closes it on its own, and closeAll() clears several`.)*
- **MAU-US-F4 ✅** As a visitor who prefers reduced motion, the Reader holds still: no rotation, jitter, spikes or scrolling, and a plain fade on close. *(verified by `honours prefers-reduced-motion: no scrolling, no jitter, plain fade`.)*
- **MAU-US-F5 ✅** As the Hyperspace page, I build my explorers and my 159-exhibit gallery from the pinned library, and still work opened as a local file before the tag is published (fallback to a MindAttic.Components checkout next to MindAttic.Web). *(verified by `renders its explorers and gallery from the library` and `pins the library to a whole-number MindAttic.Components tag that exists in this tree`.)*
- **MAU-US-F6 ✅** As a mindattic.com visitor, I occasionally see a Hyperspace Reader as a Cyberspace effect (READER), and a tap can open one where I tapped: it starts at the origin, shows the Dimensional Threshold bar and the scrolling details, and shuts down cleanly after 6 to 11 s. *(verified by `READER: a Hyperspace Reader spawns at an origin, shows the threshold bar and the scrolling details, and shuts down cleanly`.)*

## Priority backlog
1. Add an in-repo build-idempotency check over every `sites.json` entry so Epic B stories can graduate to ✅ with a named test.
