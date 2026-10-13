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
  themes/<Name>/                         Plain CSS/HTML, no build step. File-synced (not npm) into
  components/<Name>/                     static sites (mindattic.com, mindatticcares.com,
                                          ryandebraal.com) the same way MindAttic.UiUx/
                                          Web.Shared's sync-*.ps1 scripts already do, just sourced
                                          from here instead.
```

## Building and consuming the .NET packages

Same convention as MindAttic.Capture and MindAttic.Vault:

```
dotnet pack dotnet/src/MindAttic.Components.Wpf -c Release -o C:\LocalNuGet
```

Consumers add `C:\LocalNuGet` as a NuGet source (see any consumer's `nuget.config`) and reference
the package normally.
