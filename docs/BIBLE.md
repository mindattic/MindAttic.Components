---
codex: 1
project: MindAttic.UiUx
code: MAU
layer: bible
status: living
updated: 2026-06-07
---

# MindAttic.UiUx — Project Bible
> Single source of truth for what MindAttic.UiUx IS, is NOT, and the rules that keep it coherent.
> README says how to build/run; this says how to think about the system.

## 1. The one sentence {#MAU-§1}
MindAttic.UiUx is the org's **one-repo, every-front-end component library**: a catalog of
self-contained CSS/JS/HTML bundles (fonts, effects, helpers) that is the single source of truth,
delivered three ways — jsDelivr CDN at runtime, splice-in-place marker-block sync at build time,
and a cross-repo GitHub Actions PR — with the **same source** additionally packageable as first-party
`.idea` widget packages via the sibling `MindAttic.Ideas` SDK.

## 2. The product promise {#MAU-§2}
- **One source of truth, three delivery modes.** A component is authored once under `Components/`
  (or `Themes/`); the CDN, the sync scripts, and the `.idea` build all read that same source — nothing
  is duplicated in source control. See [§4](#MAU-§4).
- **Zero build step on the subscriber side.** No `npm install`, no peerdeps. Subscribers either pull a
  pinned CDN tag or receive a regenerated marker block.
- **Declarative subscribers.** `subscribers.json` is the canonical map of which component flows to which
  subscriber and with what per-subscription override; adding/removing a line enrolls/unenrolls on the
  next sync ([MAU-LAW-1](#MAU-LAW-1)).
- **Immutable, whole-number versioning on the CDN.** `@Vn` tags are edge-cached forever; `@main` tracks
  tip-of-tree. Subscribers pick their guarantee ([HOUSE-LAW-1](../MindAttic.HouseRules.md#HOUSE-LAW-1)).
- **Self-contained components.** Each folder ships its own source, usage HTML, markdown doc, and JSON
  config. No cross-component imports — a single component can be vendored without the rest
  ([MAU-LAW-3](#MAU-LAW-3)).
- **Marker-block contract.** Every splice is bounded by `BEGIN/END MINDATTIC.UIUX:<MARKER>` comments;
  only what is between the markers is regenerated ([MAU-LAW-2](#MAU-LAW-2)).

## 3. What it is NOT {#MAU-§3}
- **NOT a deploying repo.** It owns no hosting. Catalog landing pages and the Claudia/ChiMesh long-form
  builds are rendered by `MindAttic.Deploy`, which pulls components from jsDelivr at runtime. Do not add
  `landing-page` or `build-html-js` subscriber kinds here ([MAU-LAW-4](#MAU-LAW-4)).
- **NOT a place to hand-edit downstream copies.** Spliced/derived copies in subscriber repos are
  derived artifacts; the next sync overwrites whatever is between the marker pairs.
- **NOT a multi-component framework with shared runtime.** There is no shared bundle, no cross-component
  import graph, no dependency resolver beyond per-component declared assets.
- **NOT semantically versioned.** Tags are whole numbers (`V1`, `V2`, …) only — never SemVer
  ([HOUSE-LAW-1](../MindAttic.HouseRules.md#HOUSE-LAW-1)).
- **NOT the owner of the `.idea` runtime/SDK.** The `Ideas/` projects only *consume* the sibling
  `MindAttic.Ideas` Abstractions + `ma-idea` packer at compile time; the SDK lives in that repo.

## 4. Architecture canon {#MAU-§4}

```
                       Components/  +  Themes/        <-- single source of truth (raw css/js/html/json)
                              |
        +---------------------+----------------------+--------------------------+
        |                     |                      |                          |
   jsDelivr CDN          sync/*.ps1            .github Action            build.ps1  ->  Ideas/*.csproj
   @Vn / @main          (splice-in-place)     (cross-repo PRs)          (stage wwwroot) -> ma-idea pack
        |                     |                      |                          |
   MindAttic.Deploy   mindattic.com / SS /    same 3 splice repos        *.idea packages
   (runtime loader)   Psst / Ideas / Tutor    (PR on push to main)       (dist/*.V1.idea)
                      via subscribers.json
```

### 4.1 Projects / top-level layout
- `Components/` — canonical component source. Each is self-contained: `<name>.{html,css,js}` +
  optional `<name>.json` config + `<FolderName>.md` doc. Catalog: Cyberspace, SacredGeometry, OutfitFont,
  AtticFont, PinFooter, BackHomeM, WebSnapshot. (See `README.md` for the full per-component table.)
- `Themes/` — composed bundles built from components (currently `Themes/Cyberspace/`: `theme.css`,
  `body-prelude.html`, `deps.json`).
- `sync/` — PowerShell splice scripts + `sync-all.ps1` umbrella, all dot-sourcing `_subscribers.ps1`.
- `subscribers.json` — canonical `components` registry + `subscribers` map. Cited as L5 data; see
  [§4.2](#MAU-§4) and `docs/data/`.
- `Ideas/` — .NET 10 Razor RCL projects (`MindAttic.Ideas.{Plugin|Theme|Control}.<Name>`) that wrap the
  canonical assets as `.idea` packages via `idea.assets.json` + `build.ps1`. Reference the sibling
  `MindAttic.Ideas.Abstractions` (compile-time, `ExcludeAssets=runtime`).
- `build.ps1` — triple-duty build CLI (`-Output idea|standalone|blazor`).
- `.github/` — `.github/PIPELINES.md` + `.github/workflows/sync-subscribers.yml`.

### 4.2 Domain model (NOUNS)
- **Component** — a self-contained front-end bundle under `Components/<Name>/`. The atom of the catalog.
- **Theme** — a composed bundle under `Themes/<Name>/` referencing components via `deps.json`.
- **Subscriber** — a consuming repo/property declared in `subscribers.json` (`kind` ∈
  `html-inline`, `blazor-wwwroot`, `html-inline-multi`).
- **Subscription** — one `{ component, …overrides }` entry on a subscriber (e.g. `applyToSelector`,
  `jsOnly`). Override precedence: subscription value > component JSON default > none.
- **Marker block** — the `BEGIN/END MINDATTIC.UIUX:<MARKER>` region in a subscriber file that a sync
  regenerates.
- **Idea project** — an `Ideas/MindAttic.Ideas.{Plugin|Theme|Control}.<Name>` RCL whose
  `idea.assets.json` stages canonical assets into a package `wwwroot/`, packed to a `.idea`.

### 4.3 Key services (VERBS)
- **sync** (`sync/sync-*.ps1`, umbrella `sync-all.ps1`) — splice a component's bundle into a subscriber's
  marker block, idempotently. `_subscribers.ps1`'s `Get-Subscriber` reads `subscribers.json`.
- **bootstrap** (`sync/bootstrap-*.ps1`) — one-shot inserts (texture pull, `app.css` marker seeding).
- **build** (`build.ps1`) — resolve artifact → stage `wwwroot/` from `idea.assets.json` → `dotnet build`
  the RCL → `ma-idea pack` to a `.idea` (or copy raw for `standalone`).
- **CDN delivery** — implicit; jsDelivr serves any path at a pinned `@Vn` tag (no infra here).
- **cross-repo sync** (`.github/workflows/sync-subscribers.yml`) — opens PRs into the 3 splice repos on
  push to `main`, using the `SUBSCRIBER_REPO_TOKEN` PAT.

## 5. The Laws {#MAU-§5}
This project **inherits the org-wide House Rules** verbatim — see
[`MindAttic.HouseRules.md`](../MindAttic.HouseRules.md): whole-number versioning
([HOUSE-LAW-1](../MindAttic.HouseRules.md#HOUSE-LAW-1)), soft-disable
([HOUSE-LAW-2](../MindAttic.HouseRules.md#HOUSE-LAW-2)), Vault-resolved credentials
([HOUSE-LAW-3](../MindAttic.HouseRules.md#HOUSE-LAW-3)), guarded-zip packaging lifecycle
([HOUSE-LAW-5](../MindAttic.HouseRules.md#HOUSE-LAW-5)), one-engine-many-front-doors
([HOUSE-LAW-6](../MindAttic.HouseRules.md#HOUSE-LAW-6)), verified-done
([HOUSE-LAW-8](../MindAttic.HouseRules.md#HOUSE-LAW-8)), and `psst`-only-on-request
([HOUSE-LAW-9](../MindAttic.HouseRules.md#HOUSE-LAW-9)). The following are the **project-specific** laws.

### MAU-LAW-1 — `subscribers.json` is the only enrollment list {#MAU-LAW-1}
No subscriber has a hardcoded component list. Which component flows to which subscriber, and every
per-subscription override, is declared in `subscribers.json`. Sync scripts iterate the subscriber's
`subscriptions` array via `Get-Subscriber`. Adding/removing a line is the entire enrollment action.

### MAU-LAW-2 — Only marker blocks are regenerated {#MAU-LAW-2}
Every splice is bounded by a comment pair (`<!-- BEGIN MINDATTIC.UIUX:<MARKER> -->` / CSS
`/* == BEGIN MINDATTIC.UIUX:<MARKER>.CSS == */`). Anything outside the markers is left untouched.
The generated body opens with a `Generated by …` warning. Downstream copies are derived artifacts and
are never hand-edited. Syncs must be idempotent (running twice with no source change yields no diff).

### MAU-LAW-3 — Components are self-contained {#MAU-LAW-3}
Each `Components/<Name>/` ships everything it needs and imports no other component at the source level
(runtime feeds like Cyberspace→SacredGeometry are explicit, optional, and resolved by the host). A single
component must be vendorable without dragging the rest.

### MAU-LAW-4 — This repo does not deploy {#MAU-LAW-4}
Hosting, catalog landing pages, and the Claudia/ChiMesh long-form builds belong to `MindAttic.Deploy`,
which pulls from jsDelivr at runtime. Do not add `landing-page`/`build-html-js` kinds to
`subscribers.json` or recreate the deleted `sync-landing-page.ps1` / `sync-claudia.ps1` /
`sync-chimesh.ps1`. Brand-new catalog pages are configured in `MindAttic.Deploy/projects.json`.

### MAU-LAW-5 — Canonical assets are never duplicated into `Ideas/` {#MAU-LAW-5}
An `Ideas/*` project declares the canonical UiUx assets it bundles in `idea.assets.json` (`src` relative
to repo root, `dest` under the package `wwwroot/`). `build.ps1` stages them at build time. Raw source
under `Components/`/`Themes/` stays the single source of truth and is never copied into source control
under `Ideas/`.

### MAU-LAW-6 — Published CDN tags are immutable {#MAU-LAW-6}
Never mutate a published whole-number tag (`V1`, `V2`, …). Ship the next number alongside it; subscribers
pin the exact one (e.g. `MindAttic.Deploy/projects.json:componentsVersion`, or
`sync-mindattic-com.ps1 -CyberspaceCdnTag`). This refines [HOUSE-LAW-1](../MindAttic.HouseRules.md#HOUSE-LAW-1).

## 6. Verified state {#MAU-§6}
Status legend: ✅ done (verified) · 🟡 partial · ⬜ planned · 🗑️ cut · living.

- 🟡 **Component catalog (`Components/`).** Seven self-contained components present with source + docs.
  No automated test suite in this repo; correctness is verified manually / downstream. Marked 🟡 (present,
  not test-proven here).
- 🟡 **Distribution (`sync/`, CDN, Action).** Eight sync scripts + umbrella present; the GitHub Action
  workflow is committed. Not exercised in this session — 🟡 (no green run captured here).
- ⬜ **`.idea` packaging build (`build.ps1` + `Ideas/*`).** **PARTIALLY BROKEN — SDK drift.** The sibling
  `MindAttic.Ideas.Abstractions` API has diverged from what these projects expect:
  - **`Plugin.*` (6 projects): FAIL.** `dotnet build Ideas/MindAttic.Ideas.Plugin.AtticFont` →
    `CS0246: 'PluginBase' could not be found`. `MindAttic.Ideas/src/.../Bases.cs` defines `IdeaBase`,
    `ThemeBase`, `ControlBase` but **no `PluginBase`**. All six (AtticFont, BackHomeM, Cyberspace,
    OutfitFont, SacredGeometry, Tooltip) inherit `PluginBase`.
  - **`Theme.Cyberspace`: FAIL.** Build → `CS0117: 'ContentKind' does not contain a definition for
    'Plugin'` (×4 in `V1.razor`) — the SDK's `ContentKind` enum also dropped/renamed its `Plugin` member.
  - **`Control.Textbox`: ✅ builds clean** (verified 2026-06-07, `ControlBase` still matches the SDK).
  See [RFC 0001](rfc/0001-pluginbase-sdk-drift.md).
- ⬜ **Tests.** There is **no test project** in this repo (the only `*.test.*` hits are inside
  `Components/WebSnapshot/node_modules/`). The nearest thing to a smoke test is SacredGeometry's
  `build-previews.mjs`. No `✅` story may claim test verification until a test exists.

**Build evidence (2026-06-07):** `dotnet --version` = 10.0.300; sibling `MindAttic.Ideas` repo present at
`../MindAttic.Ideas`; `MindAttic.Ideas.Abstractions` compiles. `Plugin.AtticFont` → CS0246 (no
`PluginBase`); `Theme.Cyberspace` → CS0117 (`ContentKind.Plugin` gone); `Control.Textbox` → build
succeeded, 0 warnings, 0 errors. No test project exists in the repo (`dotnet test` n/a).

## 7. Active frontier {#MAU-§7}
- [RFC 0001 — PluginBase SDK drift](rfc/0001-pluginbase-sdk-drift.md): reconcile `Ideas/Plugin.*`
  with the current `MindAttic.Ideas.Abstractions` so `build.ps1 -Output idea` compiles again.
- `blazor` output target in `build.ps1` is a stub (`-Output blazor` warns "not yet implemented").
- Auth-visual components (UserLogin/UserCircle/UserTimeout) referenced by the Ideas/Tutor subscribers in
  `subscribers.json` are not yet authored — those syncs are intentional no-ops until they land.
- Epics and backlog: see [USER_STORIES.md](USER_STORIES.md).

## 8. Quality bar {#MAU-§8}
A feature is **done** (✅) only when:
1. It builds clean where a build applies (`dotnet build` of the relevant `Ideas/*` project; or `build.ps1`
   produces the artifact) — per [HOUSE-LAW-8](../MindAttic.HouseRules.md#HOUSE-LAW-8).
2. For a component change, the relevant `sync/sync-*.ps1` runs **idempotently** (a second run yields no
   diff) and a single component is independently vendorable ([MAU-LAW-3](#MAU-LAW-3)).
3. Canonical source is edited in `Components/`/`Themes/` only; no derived/downstream copy is hand-edited
   ([MAU-LAW-2](#MAU-LAW-2)); no asset duplicated into `Ideas/` ([MAU-LAW-5](#MAU-LAW-5)).
4. A shipped content change is published behind the **next whole-number tag**, never by mutating an
   existing one ([MAU-LAW-6](#MAU-LAW-6)).
5. The verifying evidence is named in the story (test token, or the build/sync command + observed result).

## 9. Glossary {#MAU-§9}
- **Component** — self-contained bundle under `Components/<Name>/`; the catalog atom.
- **Theme** — composed bundle under `Themes/<Name>/` (`deps.json` lists its component deps).
- **Subscriber** — consuming property declared in `subscribers.json`.
- **Subscription** — one component entry (with overrides) on a subscriber.
- **Marker block** — `BEGIN/END MINDATTIC.UIUX:<MARKER>` region a sync regenerates.
- **Splice-in-place** — delivery mode that rewrites only the marker block in a subscriber file.
- **`.idea` package** — a packed RCL artifact (`*.V1.idea`) produced by `build.ps1` via `ma-idea`.
- **`idea.assets.json`** — per-Ideas-project manifest mapping canonical `src` → package `wwwroot/` `dest`.
- **ma-idea / Abstractions** — the packer + SDK base types, owned by the sibling `MindAttic.Ideas` repo.
- **jsDelivr CDN** — `cdn.jsdelivr.net/gh/mindattic/MindAttic.UiUx@<ref>/<path>`, the runtime delivery.
- **`subscribers.json`** — canonical components-registry + subscriber map (L5 data).
- **`Vn` tag** — a whole-number release tag; immutable once published ([MAU-LAW-6](#MAU-LAW-6)).
</content>
</invoke>
