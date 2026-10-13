# Changelog

Whole-number release tags (`V1`, `V2`, …). A published tag is immutable ([MAU-LAW-6](BIBLE.md#MAU-LAW-6)).
Today jsDelivr serves any file at `https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Components@<tag>/web/<path>`.
Entries below the first are history from MindAttic.Web.Shared (tags `V12` onward on MindAttic.Web) and, before
that, the MindAttic.UiUx repo; their paths and URLs are as they were then.

## MindAttic.Components V3 — MindAttic.Web.Shared moves into MindAttic.Components
- **New home.** Everything MindAttic.Web.Shared served now lives in the `web/` folder of MindAttic.Components:
  components in `web/components/`, themes in `web/themes/`, each site's assets in `web/assets/<domain>/`,
  font woff2 files with their font components, retired files in `web/assets/archive/`. URLs are
  `https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Components@V<n>/web/<path>`; tags continue V1, V2, V3 on
  MindAttic.Components. The MindAttic.Web.Shared folder is deleted from MindAttic.Web; the MindAttic.UiUx repo
  stays archived.
- **The site build replaces the subscriber sync.** `web/build/build-sites.mjs` (+ `build-sites.ps1`) fills
  `BEGIN/END MINDATTIC.COMPONENTS:<MARKER>` blocks from `web/sites.json`, which now also carries the
  `MindAttic.Psst` legal pages (Outfit inline as base64) and Tutor (auth-visual CSS in `app.css`, scripts
  copied to `wwwroot/js/`). `subscribers.json`, `sync/*.ps1` and the `sync-subscribers.yml` workflow are
  retired.
- **Psst font fix.** The Latin Outfit face the legal pages inlined was a corrupt base64 string that did not
  decode, so browsers fell back silently; the build now inlines the valid `outfit-latin.woff2`.
- **Tests and tools moved.** The Playwright suite is `web/tests/package/` (paths, the expected tag from
  `sites.json` `cdnTag`, and the contiguous-from-V1 tag check updated); `build.ps1` is `web/tools/vendor.ps1`;
  `tools/build-asset-manifest.ps1` and `tools/codex.ps1` are in `web/tools/`; these docs are in `docs/web/`.
- **Linked deploy.** MindAttic.Deploy tags MindAttic.Components and commits "Pin MindAttic.Components V<n>"
  in MindAttic.Web, pushing both.

## Next tag (V12, not yet published)
- **Hyperspace component** (`Components/Hyperspace/`): `hyperspace.js`, the standard library of the 100
  Hyperspace exhibits (stable ids, plaque metadata, geometry, pure projection for Three.js and 2D canvas),
  and `hyperspace-reader.js`, the Hyperspace Reader scanner window. The Hyperspace page pins this tag.
- Tests: new `components` project (`tests/specs/components/hyperspace.spec.mjs`).
- **READER Cyberspace effect**: `console-bg.js` gains `spawnHyperspaceReader(origin)` (`FX_READER`,
  `RATE_READER` 2.5%, ttl 6 to 11 s, host switch `reader`). `hyperspace.js` and `hyperspace-reader.js` are listed in
  `subscribers.json` and `Themes/Cyberspace/deps.json`, emitted by `sync-mindattic-com.ps1` as deferred CDN tags
  before `console-bg.js`, and loaded by the Cyberspace harness; mindattic.com's tap list includes READER.
- **MindAttic.Web monorepo**: the package is the `MindAttic.Web.Shared/` folder of the MindAttic.Web repo, next to
  mindattic.com, ryandebraal.com, mindatticcares.com and Hyperspace, and loads from
  `https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Web@V<n>/MindAttic.Web.Shared/<path>`. Whole-number tags continue
  at V12 on MindAttic.Web; tags V1 to V11 stay served from the MindAttic.UiUx repo URL.
- Subscriber targets in `subscribers.json` are relative to this folder (`../mindattic.com/index.htm`,
  `../../MindAttic.Psst`, `../../Tutor/Tutor.Blazor`, ...). The sync workflow lives at the MindAttic.Web root and
  opens the mindattic.com PR in MindAttic.Web itself.
- The Hyperspace site joins the linked group: the linked deploy pins, CDN-checks and uploads it with the other three.
- Tests: `SHARED_ROOT` / `SITES_ROOT` (MindAttic.Web root) and the `SHARED_TAG` override; the live tag check expects
  V12..Vn; package name `mindattic-web-shared-tests`.

## V10
- README rewritten as full documentation and project page.
- Docs, theme and component headers describe the three sites as the only CDN consumers (each repo's GitHub
  README is its project page; nothing renders project pages from UiUx components).

## V9
- Unused files moved out of the package into `archive/` (kept in the repo, same relative paths): font TTF
  sources, the MindAttic Interactive wordmark and transparent monogram, the 1024px M-Cares master, and
  ryandebraal.com's 32 Neko frames + link icon. The package now serves 60 files (was 99)
  ([MAU-LAW-7](docs/BIBLE.md#MAU-LAW-7)).
- Stray tool-markup lines removed from four docs.
- Tests: TTF sources are expected in `archive/fonts/<family>/`; new check that `archive/` is never in the
  manifest or linked from a site; cares spec follows the removal of the "Back to contents" links.

## V8
- Retired the Prose.Writer, Prose.Codex and Ideas subscribers (targets no longer exist); their sync scripts now
  report and exit 0, so `sync-all.ps1` is green again.
- `sync-subscribers.yml`: removed the dead Prose job, trimmed the paths filter to spliced components, full-history
  checkout for tag derivation.
- Cyberspace test harness (`Components/Cyberspace/index.htm`) no longer throws on load.
- Tests: tag expectation follows the latest tag; classic scrollbars; overflow checked in every view; 100vw guard;
  cares anchor routing; `PW_OUTPUT_DIR`; live CDN spec expands base-prefix URLs and checks the framed cares page.
- Docs brought in line with the tree (README, sync.md, PIPELINES.md, BIBLE, script headers).

## V7
- **Shared runtime asset package** ([MAU-LAW-7](docs/BIBLE.md#MAU-LAW-7), [docs/ASSETS.md](docs/ASSETS.md)):
  global web fonts in `fonts/`; per-site assets in `mindattic.com/`, `mindatticcares.com/`, `ryandebraal.com/`;
  generated `assets-manifest.json` (path, bytes, SHA-256, size) via `tools/build-asset-manifest.ps1`.
- Cyberspace parallax textures losslessly recompressed: 6.2 MB → 1.7 MB, pixel-identical.
- **Fix:** the Outfit *Latin* font embedded in `Components/OutfitFont/outfit-font.css` was corrupt (browsers
  silently fell back to a system font); replaced with the genuine Google Fonts file.
- `sync-mindattic-com.ps1`: `-CyberspaceCdnTag` defaults to the latest `V*` tag (`git describe`), CDN scripts
  `defer`red, texture preloads emitted.
- `subscribers.json`: `mindattic.com` is now enrolled in `Cyberspace` only (fonts load from the CDN).
- Removed obsolete one-shot scripts `sync/bootstrap-textures.ps1` and `sync/bootstrap-streetsamurai-appcss.ps1`.
- Added a Playwright test suite in `tests/`.
- Added the linked-deploy `/deploy` command (`.claude/commands/deploy.md`).

## V6
- Cyberspace: host switches (`window.__cyberspaceFx`), keepout and comment updates; `console-bg.js` refresh.

## V5
- UserLogin / UserCircle / UserTimeout auth components; Ideas and Tutor subscribed.

## V4
- SacredGeometry harmonographs removed (still exactly 1024 shapes); whole-number version scheme.

## V3
- SacredGeometry component (1024 shapes); Cyberspace SCHEMATIC and `sacred-geometry.js` externalised to jsDelivr.

## V2
- Cyberspace `console-bg.js` and circuitboard textures via jsDelivr; component and sync fixes.

## V1
- Initial release (Cyberspace console-background bundle).
