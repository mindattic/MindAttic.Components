---
codex: 1
project: MindAttic.UiUx
code: MAU
layer: rfc
status: planned
updated: 2026-06-07
---

# RFC 0001 — Reconcile `Ideas/Plugin.*` with the current MindAttic.Ideas SDK

## Problem
`build.ps1 -Output idea` and a plain `dotnet build` of any `Ideas/MindAttic.Ideas.Plugin.*` project fail:

```
V1.cs(11,26): error CS0246: The type or namespace name 'PluginBase' could not be found
```

The six `Plugin.*` projects (AtticFont, BackHomeM, Cyberspace, OutfitFont, SacredGeometry, Tooltip) each
declare `public sealed class V1 : PluginBase`. The sibling SDK they reference at compile time,
`MindAttic.Ideas/src/MindAttic.Ideas.Abstractions/Bases.cs`, currently defines `IdeaBase`, `ThemeBase`,
and `ControlBase` — but **no `PluginBase`**.

The drift is broader than one type. `Theme.Cyberspace` (which uses the still-present `ThemeBase`) also
fails to build with `CS0117: 'ContentKind' does not contain a definition for 'Plugin'` (×4 in
`V1.razor`) — the SDK's `ContentKind` enum dropped/renamed its `Plugin` member too. Only
`Control.Textbox` (`ControlBase`, no `ContentKind.Plugin` reference) still compiles clean. So the
Abstractions API was refactored around the "Plugin" concept generally — both the base class and the
content-kind enum value. Tracked from [BIBLE §6](../BIBLE.md#MAU-§6) / [MAU-A2](../AMENDMENTS.md).

## Options compared
1. **Re-point plugins at the new base type.** Inspect the current Abstractions and change `: PluginBase`
   to whatever superseded it (likely `IdeaBase` or a renamed plugin base), adjusting overridden members
   (`StylesheetUrls`, etc.) to the new contract. Lowest blast radius; lives entirely in this repo.
2. **Pin to an older `MindAttic.Ideas` that still has `PluginBase`.** Reverts the symptom but freezes the
   SDK and diverges from the org-current Abstractions; rejected unless the removal was accidental.
3. **Ask the SDK owner to re-add `PluginBase`** (or a shim) in `MindAttic.Ideas`. Correct if the removal
   was unintentional; cross-repo and out of scope for this repo's working tree.

## Decision
*Pending* — needs inspection of the current `MindAttic.Ideas.Abstractions` public surface to learn what
`PluginBase` became. Default lean: **Option 1** (re-point in this repo) once the replacement type/contract
is confirmed, escalating to Option 3 only if the removal was a regression.

## What NOT to do
- Do not duplicate canonical assets into `Ideas/` to "work around" the build ([MAU-LAW-5](../BIBLE.md#MAU-LAW-5)).
- Do not vendor a copy of the Abstractions source into this repo.
- Do not mark MAU-US-C1 ✅ until a real `.idea` artifact builds and is verified.

## Phased plan (with risk)
1. Read the current `MindAttic.Ideas.Abstractions` base types, the `ContentKind` enum, and any plugin
   discovery attributes. *(Risk: low.)*
2. Update the six `Plugin.*` `V1.cs` to the new base/contract, and `Theme.Cyberspace` `V1.razor` to the
   new `ContentKind` members; `dotnet build` each. *(Risk: medium — the member surface, not just the
   name, may have changed.)*
3. Run `build.ps1 -Build AtticFont -Output idea` end-to-end through `ma-idea pack`. *(Risk: medium — packer
   contract may also have moved.)*
4. Add a build-smoke check to `tools/codex.ps1` or a CI step so the break can't silently recur. *(Risk: low.)*

## Graduates into: [BIBLE §6 Verified state](../BIBLE.md#MAU-§6), [MAU-US-C1](../USER_STORIES.md)
</content>
