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
  themes/<Name>/                         Plain CSS/HTML/JS, no build step. PLACEHOLDERS ONLY for now:
  components/<Name>/                     each folder's README records where the component lives today
                                          (MindAttic.Web/MindAttic.Web.Shared is current; the copy in
                                          the local MindAttic.UiUx repo is stale). The MindAttic.Web
                                          sites are hand-made, so nothing is moved or wired up until a
                                          shared version is shown to reproduce each page exactly.
```

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
