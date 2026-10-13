# MindAttic.Components

Shared design-system components for every MindAttic app, organized by toolchain rather than by
visual category — a JS bundle and a compiled .NET assembly aren't the same kind of artifact and
don't share a build or distribution path.

```
dotnet/
  src/
    MindAttic.Components.Wpf/            WPF controls + theme styles, packed to C:\LocalNuGet,
                                          consumed by desktop apps (Browser, Automata, JobHunt,
                                          MediaButler, ...).
    MindAttic.Components.Blazor.*/       One small package per theme/component/plugin, mirroring
                                          MindAttic.Ideas's existing granularity (e.g.
                                          MindAttic.Components.Blazor.Theme.Cyberspace). Ideas'
                                          own projects become thin wrappers over these.
web/
  components/<Name>/                     Plain CSS/HTML/JS, no build step, served from jsDelivr
                                          (cdn.jsdelivr.net/gh/mindattic/MindAttic.Components@V#/web/...).
  themes/<Name>/                         Same, for whole-page themes.
  sites.json + build/build-sites.mjs     Splice components into the hand-made MindAttic.Web pages.
  tests/parity/                          Proves a rebuilt page is indistinguishable from its original.
```

## Web components and the MindAttic.Web sites

The sites are hand-made, so a component only replaces part of a page once it is proven to render
identically. A component is the base; anything unique to a page stays in that page as local CSS on
top of it (mindattic.com's link buttons are the base `Button` plus their own glow and lift).

| Component | Used by | |
|---|---|---|
| Cyberspace (+ SacredGeometry, Hyperspace) | mindattic.com (Hyperspace also the Hyperspace site) | The animated backdrop; Hyperspace is the n-dimensional shape library |
| OutfitFont, AtticFont | mindattic.com, mindatticcares.com, ryandebraal.com | `@font-face` by CDN URL (`*.cdn.css`) or base64 |
| Button | mindattic.com, Hyperspace | `.ma-btn` |
| CaptureDemo | mindattic.com, mindatticcares.com | When to run a page's hands-free demo |
| VideoEmbed | mindatticcares.com | Click-to-play YouTube |
| HashPages | mindatticcares.com | Several pages in one document, hash-routed |
| ThemeFx | ryandebraal.com | Canvas engine for the theme effects |
| `web/themes/*` (16) | ryandebraal.com | Palettes, per-theme CSS and animated effects; see `web/themes/README.md` |

The other `web/components` and `web/themes` folders are still placeholders: each README records where
that component lives today (MindAttic.Web/MindAttic.Web.Shared is current; the copy in the local
MindAttic.UiUx repo is stale).

### Building a site

`web/sites.json` lists, per site, the target page, the marker blocks to fill and any URL rewrites.
Each block replaces whatever sits between `<!-- BEGIN MINDATTIC.COMPONENTS:<marker> -->` and its END
marker; nothing else in the page is touched.

```
node web/build/build-sites.mjs mindattic.com            # rewrites ../MindAttic.Web/mindattic.com/index.htm
powershell -File web/build/build-sites.ps1 -Site mindattic.com   # same, for MindAttic.Deploy's preDeploy hook
```

The pages pin `cdnTag` from `sites.json`. Ship a component change by tagging a new release (`V2`,
...), bumping `cdnTag`, and rebuilding.

### Proving parity

```
cd web/tests/parity && npm install && npx playwright install chromium --only-shell
node parity.mjs mindattic.com original.htm candidate.htm [--refactor]
```

Both pages are served at the site's real origin with every jsDelivr request answered from the local
working trees and every other host blocked. `Math.random` is seeded, the clock is paused and advanced
step by step, smooth scrolling is disabled in the engine and lazy images are loaded eagerly, so each
run is repeatable. Per scenario in `parity.config.mjs` (desktop and phone, every page, hover and
keyboard focus, the demo tour) it requires identical screenshots, identical computed styles for every
element, the same bytes for every resource loaded and the same page errors. Without `--refactor` the
served HTML (CDN URLs resolved to the files they point to) and the final DOM must also match; with it
(base component plus local CSS) the markup may differ but nothing rendered may. Effects that stay
random even when seeded (mindattic.com's Cyberspace layer) are masked for pixels and styles, and are
proven instead by their source and resource bytes being identical.

## MindAttic.Components.Wpf

Merge the theme once, in `App.xaml`:

```xml
<ResourceDictionary Source="pack://application:,,,/MindAttic.Components.Wpf;component/Theme.xaml" />
```

| Key | What |
|---|---|
| `McChrome`, `McChromeBorder`, `McSurface`, `McHover`, `McText`, `McMuted`, `McControlBorder`, `McFocus`, `McAccent`, `McRec`, `McStar` | Palette brushes |
| `McToolButton` | 34x30 icon-font toolbar button |
| `McButton` | Bordered command button |
| `McPrimaryButton` | The one emphasized action; re-key `McAccentHover` / set `Background` for a brand colour |
| `McTextButton` | Borderless dialog button |
| `McTileButton` + `McTileSubtitle` | Launcher tile; re-key `McTileHover` for a brand colour |
| `McTextBox` | Dark text box |
| `McDropdownMenu`, `McDropdownMenuItem`, `McSeparator`, `McPinToggle` | Dropdown parts (used by `DropdownMenu`) |
| *(implicit)* `ToolTip` | Every tooltip in the app |

Every templated control restores a keyboard-focus outline (`McFocus`). `Controls.DropdownMenu` is a
pinnable action menu driven by a trigger button; see its XML docs.

Consumers so far: MindAttic.Browser, JobHunt, Prose.Launcher, Prose.Writer. Automata, KdpPublish,
MediaButler, ProcessLogger and MindAttic.Log.Reader are WebView2/Blazor shells with no native WPF
chrome, so their UI belongs to the web side, not this package.

## Building and consuming the .NET packages

Same convention as MindAttic.Capture and MindAttic.Vault:

```
dotnet pack dotnet/src/MindAttic.Components.Wpf -c Release -o C:\LocalNuGet
```

Consumers add `C:\LocalNuGet` as a NuGet source (see any consumer's `nuget.config`) and reference
the package normally.
