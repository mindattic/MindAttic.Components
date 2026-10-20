# MindAttic.Components

The shared design system behind every MindAttic site and desktop app: plain CSS/JS web components and themes served from jsDelivr, and a WPF theme and controls package for the .NET apps.

[![Stack](https://img.shields.io/badge/stack-HTML%20CSS%20JS%20and%20WPF-blue)](#project-layout) [![.NET](https://img.shields.io/badge/.NET-net10.0--windows-512BD4)](#mindatticcomponentswpf) [![Package](https://img.shields.io/badge/MindAttic.Components.Wpf-14.0.0-brightgreen)](#building-and-consuming-the-net-package) [![License](https://img.shields.io/badge/license-all%20rights%20reserved-lightgrey)](#license) [![Status](https://img.shields.io/badge/status-in%20use-success)](#current-state)

![The Cyberspace backdrop component as it renders on mindattic.com: dim circuit-board panels, terminal readouts and a file browser over a dark grid](docs/web/images/cyberspace-backdrop.png)

Try it: the web package is served as is from `cdn.jsdelivr.net/gh/mindattic/MindAttic.Components@V<n>/web/...`; the WPF package is packed to `C:\LocalNuGet`.

## Why

- Change a button, font or theme once and every MindAttic site and app picks it up on its next build.
- Hand-made pages stay hand-made: a component only replaces part of a page once it is proven to render identically.
- No build step for the sites that consume it: a page loads a pinned CDN file or receives a regenerated marker block.
- Desktop apps get the same dark chrome, focus outlines and dialogs without copying styles between repos.
- Every served asset is listed with its size and SHA-256, and the CDN copy is checked byte for byte on release.

## Features

The repo is organized by toolchain rather than by visual category: a JS bundle and a compiled .NET assembly are not the same kind of artifact and do not share a build or distribution path.

- **Web components** (`web/components/`): 32 self-contained CSS/HTML/JS components, from fonts and buttons to the animated Cyberspace backdrop and the Hyperspace n-dimensional shape library.
- **Whole-page themes** (`web/themes/`): 16 palette themes with animated effects, plus the Cyberspace theme.
- **Site build** (`web/build/`): splices components into the hand-made pages and apps listed in `web/sites.json`, touching nothing outside their marker blocks.
- **Parity proof** (`web/tests/parity/`): shows a rebuilt page is indistinguishable from its original.
- **Site assets** (`web/assets/`): each site's logos, icons and images, with a manifest of every served file.
- **WPF package** (`dotnet/src/MindAttic.Components.Wpf`): a dark theme (palette brushes, button, text box, progress bar and menu styles, tooltips), building blocks (pill choice row, pill toggle, card, stat tiles, focus ring, text styles) and the `AddressBar`, `DropdownMenu`, `McBadge`, `McLineChart`, `McModal`, `McToast` / `McToastHost` and `McWindow` controls.

## Quick start

Prerequisites: Node 22 for the web build and tests, the .NET 10 SDK on Windows for the WPF package, and the consuming repos checked out next to this one.

```powershell
git clone https://github.com/mindattic/MindAttic.Components
cd MindAttic.Components
node web/build/build-sites.mjs mindattic.com                       # rewrite ../MindAttic.Web/mindattic.com/index.htm
powershell -File dotnet/tools/publish-wpf.ps1                     # only after the version bump has landed on main
```

The first command fills the page's `MINDATTIC.COMPONENTS` marker blocks from `web/components/`; the second packs main's `MindAttic.Components.Wpf.14.0.0.nupkg` into the local NuGet feed (and refuses if that version is already there; see [Building and consuming the .NET package](#building-and-consuming-the-net-package)).

## Web components and the MindAttic.Web sites

The sites are hand-made, so a component only replaces part of a page once it is proven to render identically. A component is the base; anything unique to a page stays in that page as local CSS on top of it (mindattic.com's link buttons are the base `Button` plus their own glow and lift).

| Component | Used by | What |
|---|---|---|
| Cyberspace (+ SacredGeometry, Hyperspace) | mindattic.com (Hyperspace also the Hyperspace site) | The animated backdrop; Hyperspace is the n-dimensional shape library |
| OutfitFont, AtticFont | mindattic.com, mindatticcares.com, ryandebraal.com | `@font-face` by CDN URL (`*.cdn.css`) or base64 |
| Button | mindattic.com, Hyperspace | `.ma-btn` |
| CaptureDemo | mindattic.com, mindatticcares.com, ryandebraal.com | When to run a page's hands-free demo |
| VideoEmbed | mindatticcares.com | Click-to-play YouTube |
| HashPages | mindatticcares.com | Several pages in one document, hash-routed |
| ThemeFx | ryandebraal.com | Canvas engine for the theme effects |
| Toolbar | ryandebraal.com | `.ma-toolbar` (sticky themed bar), `__label` |
| ToolButton | ryandebraal.com | `.ma-tool-btn` (`--primary`, `--block`), `.ma-tool-link` |
| IconButton | ryandebraal.com | `.ma-icon-btn` 32x32 (`--fixed`) |
| Select, Range | ryandebraal.com | `.ma-select`, `.ma-range` |
| Menu | ryandebraal.com | `.ma-menu` dropdown panel, `__group`, `__item` (`--active`); open with `display:block` or `.is-open` |
| Modal | ryandebraal.com | `.ma-modal` overlay, `__dialog`, `__header`, `__title`, `__close`, `__footer` |
| Lightbox | ryandebraal.com | `.ma-lightbox` (`.is-open`), `__inner`, `__links`, `__img` |
| Tag | ryandebraal.com | `.ma-tag` pill with a hover tip `__tip` (`--below`) |
| ProgressBar | ryandebraal.com | `.ma-progress` + `__fill`; `--edge` for a thin bar along a container's bottom |
| FormRow | ryandebraal.com | `.ma-form-section__title`, `__rows`, `.ma-form-row`, `__label`, `__value` |
| Avatar, SkipLink | ryandebraal.com | `.ma-avatar` (`--ma-avatar-size`), `.ma-skip-link` |
| UserLogin, UserCircle, UserTimeout | Tutor | Auth visuals: CSS spliced into `app.css`, scripts copied to `wwwroot/js/` |
| `web/themes/*` (16) | ryandebraal.com | Palettes, per-theme CSS and animated effects; see `web/themes/README.md` |

The UI components above read the same tokens the themes define (`--bg`, `--bg2`, `--bg3`, `--border`, `--accent`, `--text`, `--text2`, `--text3`, `--rule`, `--tag-*`, `--font-resume`), so a page re-skins them by switching theme, and a site keeps only what is unique to it as local CSS.

The rest of the catalog came over from MindAttic.Web.Shared (formerly the MindAttic.UiUx repo, now archived), byte for byte: PageScrollbar, Textbox, Tooltip, PinFooter, BackHomeM, WebSnapshot and the auth visuals UserLogin, UserCircle and UserTimeout, plus the Cyberspace theme. The full catalog is in `docs/web/data/components.json`.

## Building a site

`web/sites.json` lists, per entry, the target file(s), the marker blocks to fill and any URL rewrites. Each block replaces whatever sits between `BEGIN MINDATTIC.COMPONENTS:<marker>` and its END marker (an HTML, CSS or JS comment, by where it sits); nothing else in the file is touched, and a rerun with unchanged sources writes identical bytes.

| Entry | Target (next to this checkout) |
|---|---|
| `mindattic.com`, `ryandebraal.com`, `mindatticcares.com`, `Hyperspace` | `../MindAttic.Web/<site>/index.htm` |
| `MindAttic.Psst` | `../MindAttic.Psst/privacy.htm` and `terms.htm`: the Outfit font inline as base64 |
| `Tutor` | `../Tutor/Tutor.Blazor/wwwroot/app.css` (UserLogin, UserCircle, UserTimeout CSS); their scripts copied to `wwwroot/js/` |

```powershell
node web/build/build-sites.mjs mindattic.com                                  # rewrites ../MindAttic.Web/mindattic.com/index.htm
powershell -File web/build/build-sites.ps1 -Site mindattic.com [-Tag V4]      # same, for MindAttic.Deploy's preDeploy hook
```

The pages pin the newest `V<n>` tag of this repo (or `-Tag`), so there is no tag to keep in sync by hand.

## Deployment

MindAttic.Deploy's linked deploy (`npm run deploy -- --uiux`) releases a web change: it tags this repo with the next whole-number tag, runs the four sites' builds with that tag, commits "Pin MindAttic.Components V<n>" in MindAttic.Web, pushes both, checks every asset on jsDelivr byte for byte and uploads the sites.

After an OutfitFont change, the `sync-psst` GitHub workflow (`.github/workflows/sync-psst.yml`) rebuilds MindAttic.Psst's `privacy.htm` and `terms.htm` and opens a pull request there; the `MindAttic.Psst` entry can also be run by hand. After a change to the auth visuals, run the `Tutor` entry by hand.

## MindAttic.Components.Wpf

Merge the theme once, in `App.xaml`:

```xml
<ResourceDictionary Source="pack://application:,,,/MindAttic.Components.Wpf;component/Theme.xaml" />
```

| Key | What |
|---|---|
| `McChrome`, `McChromeBorder`, `McSurface`, `McHover`, `McText`, `McMuted`, `McControlBorder`, `McFocus`, `McAccent`, `McRec`, `McStar` | Palette brushes |
| `McGood`, `McWarn`, `McBad`, `McChartGrid` | Status colours for data, and chart gridlines |
| `McLink` | Link text: the accent hue lightened to 5.6:1 on `McChrome` (`McAccent` is a fill colour) |
| `McFocusRing` | 2px `McFocus` outline just outside a control; set it as `FocusVisualStyle` |
| `McHeading`, `McSectionLabel`, `McMutedText` | Text styles: a level-2 heading, a muted level-3 section label, trimmed muted text |
| `McHyperlink` | A `Hyperlink` in text: `McLink`, SemiBold, no underline at rest, underlined on hover and keyboard focus, `McMuted` when disabled. Base a style on it to add a `Command` (e.g. the Orchestrator's ticket ids) |
| `McCard` | A raised, bordered `Border` that groups content |
| `McPillChoice` | A `ListBox` as a wrapping row of single-choice pills; the chosen one fills with `McAccent` |
| `McPillToggle` | A `ToggleButton` pill, filled with `McAccent` while on |
| `McStatCard`, `McSmallStat` | Stat tiles on `McTileButton` (value over caption); the small one has no size of its own and reads its tooltip as help text |
| `McToolButton` | 34x30 icon-font toolbar button |
| `McButton` | Bordered command button |
| `McPrimaryButton` | The one emphasized action; re-key `McAccentHover` / set `Background` for a brand colour |
| `McTextButton` | Borderless dialog button; content is centred unless you set `HorizontalContentAlignment` / `VerticalContentAlignment` (e.g. `Left` for a fill-width link) |
| `McTileButton` + `McTileSubtitle` | Launcher tile; re-key `McTileHover` for a brand colour |
| `McTextBox` | Dark single-line text box; `Controls.McTextBox.Placeholder` shows muted text while it's empty |
| `McTextArea` | Multi-line `McTextBox` (Enter adds a line, wraps, top-aligned) whose thin scroll bars show when the text overflows |
| `McScrollBar` | Thin dark `ScrollBar` (the text boxes use it); base an implicit `ScrollBar` style on it to use it elsewhere |
| `McProgressBar` | Thin rounded `ProgressBar`: `McSurface` track, `McControlBorder` outline, `Foreground` fill; determinate or indeterminate |
| `McDropdownMenu`, `McDropdownMenuItem`, `McSeparator`, `McPinToggle` | Dropdown parts (used by `DropdownMenu`) |
| *(implicit)* `ToolTip` | Every tooltip in the app |
| `Controls.AddressBar` | Back/Forward/Reload(/Home) + URL textbox for a WebView2 pane |
| `Controls.DropdownMenu` | Pinnable action menu driven by a trigger button |
| `Controls.McBadge` (+ `McTag`) | Outlined state pill: optional icon-font glyph and a word in a state colour; `McTag` is the plain type/category tag |
| `Controls.McLineChart` | Percent-over-time line chart: time and percent axes, reference lines, per-point segment colours, a dotted projection |
| `Controls.McModal` (+ `McBackdrop`) | In-window modal dialog: dimmed backdrop, themed card, heading, close button, footer |
| `Controls.McToast`, `Controls.McToastHost` | Notification card (accent stripe, clickable title + two-line body, dismiss button) and the row that stacks and announces them |
| `Controls.McWindow.DarkTitleBar` | Attached property: asks Windows to draw the window's title bar dark |

Every templated control restores a keyboard-focus outline (`McFocus`). `Controls.DropdownMenu` closes its own popup before invoking an action, so it is never on screen for a screenshot or the start of a recording; see its XML docs.

`Controls.AddressBar` is a themed WPF address bar for a WebView2 pane: `Attach(CoreWebView2?)` binds (or rebinds) it to a live core, Enter navigates with an optional `{query}` search-template fallback, and an optional `NavigationGuard` lets a host veto Back/Forward/Reload/Navigate/Home (e.g. while recording or mid-run) with a reason string surfaced via `NavigationBlocked`.

`Controls.McWindow.DarkTitleBar="True"` on a `Window` asks Windows 10 20H1 and later to draw its title bar and frame dark, so the system caption matches the `McChrome` theme. Older Windows ignores the request.

`Controls.McModal` is an in-window modal dialog. Put it last in the window's root `Grid`, spanning every row and column, and bind `IsOpen` (two-way by default):

```xml
<ctl:McModal IsOpen="{Binding IsSettingsOpen}" Title="Settings" DialogWidth="520"
             Grid.RowSpan="3" Grid.ColumnSpan="2">
    <StackPanel>...</StackPanel>
    <ctl:McModal.Footer>
        <Button Style="{StaticResource McPrimaryButton}" Content="Done" Command="{Binding ToggleSettingsCommand}" />
    </ctl:McModal.Footer>
</ctl:McModal>
```

Esc, the close button and a backdrop click close it (through `CloseCommand` when set, e.g. a view model whose open flag is read-only, otherwise by clearing `IsOpen`). While open, Tab cycles inside the card, access keys are scoped to it and focus can't be moved out; on close focus goes back to where it was. The title is a level-2 heading and the dialog's accessible name (unless `AutomationProperties.Name` is set), and UI Automation sees it as a dialog window. `Opened` and `Closed` are raised after each change.

`ctl:McTextBox.Placeholder` on a `TextBox` styled `McTextBox` or `McTextArea` shows that text in `McMuted` while the box is empty, so a host no longer overlays its own `TextBlock`. It ignores the mouse (a click on it lands in the box) and is not part of the box's accessible name, so still set `AutomationProperties.Name`. `McTextArea` is the multi-line box: `AcceptsReturn`, wrapping, text from the top, and `McScrollBar`s that show when the text overflows. Set `TextWrapping="NoWrap"` (e.g. a read-only log) and the horizontal bar shows for long lines too. `McTextBox` hides its scroll bars unless the box sets `VerticalScrollBarVisibility` / `HorizontalScrollBarVisibility` itself.

```xml
<TextBox Style="{StaticResource McTextBox}" Text="{Binding Search, UpdateSourceTrigger=PropertyChanged}"
         ctl:McTextBox.Placeholder="Search repos and tickets   Ctrl+F" AutomationProperties.Name="Search repos and tickets" />
<TextBox Style="{StaticResource McTextArea}" Height="64" Text="{Binding Prompt, UpdateSourceTrigger=PropertyChanged}"
         ctl:McTextBox.Placeholder="Opening prompt (optional)" AutomationProperties.Name="Opening prompt (optional)" />
<TextBox Style="{StaticResource McTextArea}" IsReadOnly="True" TextWrapping="NoWrap" FontFamily="Consolas" />
```

`McProgressBar` styles a stock `ProgressBar`. Determinate fills `Value`'s share of `Minimum..Maximum` (0..100 by default) from the left; `IsIndeterminate="True"` sweeps a segment across the track instead. The fill is the bar's `Foreground` (`McAccent` by default), so a host can bind it, e.g. a budget colour that turns amber then red:

```xml
<ProgressBar Style="{StaticResource McProgressBar}" Maximum="1" Value="{Binding BudgetFill}"
             Foreground="{Binding BudgetBrush}" />
<ProgressBar Style="{StaticResource McProgressBar}" IsIndeterminate="True" />
```

It is 6px tall; set `Height` for a thicker bar.

`Controls.McBadge` is a small outlined pill for a state: set `Foreground` to the state colour and the glyph, the word and the outline all follow it (the outline unless `BorderBrush` is set), so the state reads as a word and a glyph as well as a colour (WCAG 1.4.1). `Glyph` is an icon-font character (Segoe MDL2 Assets by default, `GlyphFontFamily` to change it); leave it empty for a word only. The `McTag` style is the plain variant: a squarer, neutral-outlined, normal-weight label for a type or category. The glyph is decoration and is left out of the UI Automation tree; the badge reads as text named by its content.

```xml
<ctl:McBadge Glyph="{Binding StateGlyph}" Content="{Binding StateLabel}" Foreground="{Binding StateBrush}" />
<ctl:McBadge Style="{StaticResource McTag}" Content="{Binding Type}" />
```

`Controls.McLineChart` draws a `McLineChartData`: the time span (`StartUtc`..`EndUtc`), the `Points` (each `McChartPoint` carries the brush of the segment ending at it, `McAccent` by default), an `EndLabel` for the last point's dot, an optional `Projection` drawn dotted from the last point and labelled at its end, and `ReferenceLines` (`McChartReferenceLine.Level` for a target, `.Curve` for a pace line). Without a projection it draws the line as it ended; with no points, just the axes. The percent axis has a gridline every 25% and headroom over the highest point; the time axis labels both ends and every whole local hour between (stepped in UTC, so a clock change never stacks or drops a mark), and the last point's label moves aside rather than run into the projection's. Its brushes are the theme's (`McText`, `McMuted`, `McChartGrid`, `McAccent`); pair it with a caption that says the same in words.

```xml
<ctl:McLineChart Data="{Binding Utilization}" Height="260" />
```

`Controls.McToastHost` is a row of `Controls.McToast` notification cards. Give it its own row in the window, so new toasts push the page down instead of covering whatever has focus, and bind it to a collection of view models with `Title`, `Body` and (optionally) an `Accent` brush:

```xml
<ctl:McToastHost Grid.Row="2" ItemsSource="{Binding Toasts}" Margin="24,0,24,6"
                 OpenCommand="{Binding OpenToastCommand}" DismissCommand="{Binding DismissToastCommand}" />
```

Each toast is a McSurface card outlined in its accent (McAccent by default) with a 4px stripe on the left; its title and a body clipped to two lines are one clickable button (`OpenCommand`, or a toast's own `Command` and `Click`), and a McToolButton (x) dismisses it (`DismissCommand`, both with the item as parameter; with no command the host removes the item itself). `TitlePath`, `BodyPath` and `AccentPath` rename the item properties. The card is named by its title with the body as help text, the dismiss button is "Dismiss: {Title}", the row is the "Notifications" group, and every toast added after the first load is read out politely ("{Title}. {Body}") through a hidden live region without moving focus. A `McToast` also works on its own, or as a direct item of the host.

Consumers so far: MindAttic.Browser, JobHunt, Prose.Launcher, Prose.Writer, Automata, KdpPublish. MediaButler, ProcessLogger and MindAttic.Log.Reader are still WebView2/Blazor shells with no native WPF chrome, so their UI belongs to the web side, not this package.

## Building and consuming the .NET package

Publish only with `dotnet/tools/publish-wpf.ps1`, never a bare `dotnet pack -o C:\LocalNuGet`. Every ticket that bumps the package publishes this way, and only after its commit has landed on main:

```powershell
git fetch origin; git rebase origin/main; git push origin HEAD:main   # land the version bump first
powershell -File dotnet/tools/publish-wpf.ps1                         # then publish it
powershell -File dotnet/tools/publish-wpf.ps1 -Ref <sha>              # or an older main commit whose package is missing
```

The script fetches origin and refuses unless the commit (`-Ref`, default `HEAD`) is on `origin/main`. It takes the version from that commit's csproj, not from the working tree, and refuses if `MindAttic.Components.Wpf.<version>.nupkg` is already in the feed (`-Feed`, default `C:\LocalNuGet`). It packs a clean export of the commit (`git archive`), so uncommitted edits never get in, and copies the package into the feed without overwriting. This exists because parallel tickets used to pack from their own branch before rebasing: on 2026-10-20 M.COMP-T31 packed 8.0.0 over M.COMP-T36's package and M.COMP-T30 packed 10.0.0 over M.COMP-T31's, and both originals had to be rebuilt from their commits. If the script refuses an existing version, don't delete the package to get past it. Rebase, bump to the next free major, land that, and publish again.

Consumers add `C:\LocalNuGet` as a NuGet source (see any consumer's `nuget.config`) and reference the package normally. Versions are whole numbers only (HOUSE-LAW-1): a release bumps the major. `MindAttic.Components.slnx` holds the package and its tests.

## Testing

The web package and the sites:

```powershell
cd web/tests/package; npm ci
npm run test:local                              # package + sites + components; the CDN is answered from this tree
Invoke-Pester -Path web/tests/package/pester    # tools/vendor.ps1 (from the repo root; Pester 5+)
```

`web/tests/package/README.md` describes what each group guards, live mode and the CDN checks.

The WPF package (xUnit, covering `McBadge`, `McHyperlink`, `McModal`, `McProgressBar`, `McTextBox` / `McTextArea`, `DropdownMenu`, `McTextButton`, `McLineChart`, `McToast` / `McToastHost` and the building-block styles):

```powershell
dotnet test dotnet/tests/MindAttic.Components.Wpf.Tests
```

The publish script (Pester, against a throwaway git repo and a temp feed folder; it never touches `C:\LocalNuGet`):

```powershell
Invoke-Pester -Path dotnet/tests/publish        # from the repo root; Pester 5+
```

## Proving parity

```powershell
cd web/tests/parity; npm install; npx playwright install chromium --only-shell
node parity.mjs mindattic.com original.htm candidate.htm [--refactor]
```

Both pages are served at the site's real origin with every jsDelivr request answered from the local working trees and every other host blocked. `Math.random` is seeded, the clock is paused and advanced step by step, smooth scrolling is disabled in the engine and lazy images are loaded eagerly, so each run is repeatable. Per scenario in `parity.config.mjs` (desktop and phone, every page, hover and keyboard focus, the demo tour) it requires identical screenshots, identical computed styles for every element, the same bytes for every resource loaded and the same page errors. Without `--refactor` the served HTML (CDN URLs resolved to the files they point to) and the final DOM must also match; with it (base component plus local CSS) the markup may differ but nothing rendered may. Effects that stay random even when seeded (mindattic.com's Cyberspace layer) are masked for pixels and styles, and are proven instead by their source and resource bytes being identical.

## Tools

```powershell
powershell -File web/tools/build-asset-manifest.ps1 [-Verify]   # regenerate (or check) assets-manifest.json
powershell -File web/tools/vendor.ps1 -Build OutfitFont         # copy one component or theme to web/dist/
powershell -File web/tools/codex.ps1 doctor                     # check docs/web
powershell -File web/tools/codex.ps1 digest                     # regenerate its digest
```

## Project layout

```text
dotnet/
  src/MindAttic.Components.Wpf/          WPF theme (Theme.xaml) + controls, published to C:\LocalNuGet
  tests/MindAttic.Components.Wpf.Tests/  xUnit tests for the controls
  tests/publish/                         Pester tests for the publish script
  tools/publish-wpf.ps1                  The one way to publish the package (refuses to overwrite a version)
web/                                     Served as is from jsDelivr:
                                          cdn.jsdelivr.net/gh/mindattic/MindAttic.Components@V<n>/web/...
  components/<Name>/                     Plain CSS/HTML/JS, no build step (fonts keep their woff2 here)
  themes/<Name>/                         Same, for whole-page themes (and their artwork)
  assets/<domain>/                       Each site's own logos, icons and images
  assets/archive/                        Kept but never served (font TTF sources, brand masters)
  assets-manifest.json                   Path, bytes, SHA-256 and pixel size of every served asset
  sites.json + build/build-sites.mjs     Splice components into the hand-made pages and apps that use them
  tests/package/                         Playwright suite for the package and the sites; Pester for tools
  tests/parity/                          Proves a rebuilt page is indistinguishable from its original
  tools/                                 vendor.ps1, build-asset-manifest.ps1, codex.ps1
docs/web/                                The web package's bible, stories, asset and pipeline docs
.github/workflows/sync-psst.yml          Splices OutfitFont into MindAttic.Psst and opens a PR there
```

## Current state

- The web package is live: the four MindAttic.Web sites, MindAttic.Psst and Tutor are built from it.
- `MindAttic.Components.Wpf` is at 14.0.0 (adds `McTextBox.Placeholder`, the multi-line `McTextArea` and `McScrollBar`; 13.0.0 added the `McHyperlink` link style, the Orchestrator's ticket-id links; 12.0.0 added `McToast` / `McToastHost`, the Orchestrator ToastRow's card and row; 11.0.0 moved in the Orchestrator's own building blocks and its utilization chart, `McLineChart`) and used by six desktop apps.

## Roadmap

- Planned, not built: `MindAttic.Components.Blazor.*`, one small package per theme, component or plugin, mirroring MindAttic.Ideas's granularity (e.g. `MindAttic.Components.Blazor.Theme.Cyberspace`), so Ideas' own projects become thin wrappers over them. No Blazor project exists in `dotnet/src/` yet.

## Documentation

- [docs/web/BIBLE.md](docs/web/BIBLE.md): what the web package is and the rules it keeps
- [docs/web/ASSETS.md](docs/web/ASSETS.md) and [docs/web/PIPELINES.md](docs/web/PIPELINES.md): assets and delivery pipelines
- [docs/web/AMENDMENTS.md](docs/web/AMENDMENTS.md) and [docs/web/CHANGELOG.md](docs/web/CHANGELOG.md)
- [docs/USER_STORIES.md](docs/USER_STORIES.md): stories and tickets (generated from MindAttic.Workflow; change them with the workflow CLI)
- [web/themes/README.md](web/themes/README.md) and [web/tests/package/README.md](web/tests/package/README.md)
- [AGENTS.md](AGENTS.md): entry point for coding agents

## License

This repo has no LICENSE file; all rights reserved. Copyright (c) 2026 MindAttic.

Part of [MindAttic](https://mindattic.com) — see more projects at [github.com/mindattic](https://github.com/mindattic). Related: [MindAttic.Web](https://github.com/mindattic/MindAttic.Web), [MindAttic.Deploy](https://github.com/mindattic/MindAttic.Deploy), [MindAttic.Psst](https://github.com/mindattic/MindAttic.Psst).
