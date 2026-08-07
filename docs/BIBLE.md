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
and a cross-repo GitHub Actions PR.

## 2. The product promise {#MAU-§2}
- **One source of truth, three delivery modes.** A component is authored once under `Components/`
  (or `Themes/`); the CDN, the sync scripts, and the GitHub Action all read that same source — nothing
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
- **NOT the owner of the `.idea` runtime/SDK.** If `.idea` packaging is reintroduced, any `Ideas/`
  projects would only *consume* the sibling `MindAttic.Ideas` Abstractions + `ma-idea` packer at
  compile time; the SDK lives in that repo. The `Ideas/` subtree was removed as of 2026-06-07 (see MAU-A3).

## 4. Architecture canon {#MAU-§4}

```
                       Components/  +  Themes/        <-- single source of truth (raw css/js/html/json)
                              |
        +---------------------+----------------------+
        |                     |                      |
   jsDelivr CDN          sync/*.ps1            .github Action
   @Vn / @main          (splice-in-place)     (cross-repo PRs)
        |                     |                      |
   MindAttic.Deploy   mindattic.com / SS /    same 5 splice repos
   (runtime loader)   Psst / Ideas / Tutor    (PR on push to main)
                      via subscribers.json
```

### 4.1 Projects / top-level layout
- `Components/` — canonical component source. Each is self-contained: `<name>.{html,css,js}` +
  optional `<name>.json` config + `<FolderName>.md` doc. Catalog (13): Cyberspace, SacredGeometry,
  OutfitFont, AtticFont, PinFooter, BackHomeM, WebSnapshot, PageScrollbar, Textbox, Tooltip, UserLogin,
  UserCircle, UserTimeout. (See `README.md` and `docs/data/components.json` for the full per-component
  table.) Auth-visual components (UserLogin, UserCircle, UserTimeout) are authored and wired into
  `subscribers.json`.
- `Themes/` — composed bundles built from components (currently `Themes/Cyberspace/`: `theme.css`,
  `body-prelude.html`, `deps.json`).
- `sync/` — PowerShell splice scripts + `sync-all.ps1` umbrella, all dot-sourcing `_subscribers.ps1`.
- `subscribers.json` — canonical `components` registry + `subscribers` map. Cited as L5 data; see
  [§4.2](#MAU-§4) and `docs/data/`.
- `build.ps1` — build CLI (retained for `standalone` output; `idea` and `blazor` outputs are stubs
  without the `Ideas/` subtree — see §6).
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

### 4.3 Key services (VERBS)
- **sync** (`sync/sync-*.ps1`, umbrella `sync-all.ps1`) — splice a component's bundle into a subscriber's
  marker block, idempotently. `_subscribers.ps1`'s `Get-Subscriber` reads `subscribers.json`.
- **bootstrap** (`sync/bootstrap-*.ps1`) — one-shot inserts (texture pull, `app.css` marker seeding).
- **build** (`build.ps1`) — copy raw canonical assets for `standalone` output. The `idea` and `blazor`
  output targets are stubs (the `Ideas/` RCL subtree was removed — see §6 and MAU-A3).
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

### MAU-LAW-5 — Canonical assets are never duplicated into packaging subtrees {#MAU-LAW-5}
If an `Ideas/*` or other packaging project is reintroduced, it must declare canonical UiUx assets via a
manifest (`idea.assets.json` or equivalent) and stage them at build time. Raw source under `Components/`
and `Themes/` is always the single source of truth and is never copied into source control under any
packaging subtree. (The `Ideas/` subtree was removed as of 2026-06-07; this law governs any future
reinstatement — see MAU-A3.)

### MAU-LAW-6 — Published CDN tags are immutable {#MAU-LAW-6}
Never mutate a published whole-number tag (`V1`, `V2`, …). Ship the next number alongside it; subscribers
pin the exact one (e.g. `MindAttic.Deploy/projects.json:componentsVersion`, or
`sync-mindattic-com.ps1 -CyberspaceCdnTag`). This refines [HOUSE-LAW-1](../MindAttic.HouseRules.md#HOUSE-LAW-1).

## 6. Verified state {#MAU-§6}
Status legend: ✅ done (verified) · 🟡 partial · ⬜ planned · 🗑️ cut · living.

- 🟡 **Component catalog (`Components/`).** Thirteen self-contained components present with source + docs
  (Cyberspace, SacredGeometry, OutfitFont, AtticFont, PinFooter, BackHomeM, WebSnapshot, PageScrollbar,
  Textbox, Tooltip, UserLogin, UserCircle, UserTimeout). Auth-visual trio (UserLogin/UserCircle/UserTimeout)
  authored and wired into `subscribers.json` as of 2026-06-07. No automated test suite in this repo;
  correctness is verified manually / downstream. Marked 🟡 (present, not test-proven here).
- 🟡 **Distribution (`sync/`, CDN, Action).** Five sync scripts (`sync-mindattic-com.ps1`,
  `sync-prose.ps1`, `sync-mindattic-psst.ps1`, `sync-ideas.ps1`, `sync-tutor.ps1`) + `sync-all.ps1`
  umbrella present; the GitHub Action workflow is committed. Not exercised in this session — 🟡 (no green
  run captured here).
- 🗑️ **`.idea` packaging build (`build.ps1` + `Ideas/*`).** The `Ideas/` RCL subtree was **removed** from
  this repo as of 2026-06-07 (see MAU-A3). The `build.ps1` `idea` and `blazor` output targets are now
  stubs (warnings only); the `standalone` copy path is unaffected. The previous build evidence
  (CS0246/CS0117 SDK drift from the 2026-06-07 Codex install session) is superseded by the removal. If
  `.idea` packaging is reintroduced, establish fresh build evidence at that time.
  See [RFC 0001](rfc/0001-pluginbase-sdk-drift.md) for the original SDK-drift context.
- ⬜ **Tests.** There is **no test project** in this repo (the only `*.test.*` hits are inside
  `Components/WebSnapshot/node_modules/`). The nearest thing to a smoke test is SacredGeometry's
  `build-previews.mjs`. No `✅` story may claim test verification until a test exists.

**Build evidence (2026-06-07, full-sync):** No `.idea` build applicable (`Ideas/` subtree absent). No
automated test project (`dotnet test` n/a). Component source tree verified on disk: 13 components. Sync
scripts: 5 subscriber scripts + `sync-all.ps1` umbrella, all present.

## 7. Active frontier {#MAU-§7}
- **`Ideas/` subtree removed (MAU-A3).** The `.idea` packaging path is gone. `build.ps1 -Output idea`
  and `-Output blazor` are now stubs. [RFC 0001](rfc/0001-pluginbase-sdk-drift.md) is superseded by the
  removal; kept for historical reference. Reintroducing `.idea` packaging requires a new RFC.
- **Six new components landed** (PageScrollbar, Textbox, Tooltip, UserLogin, UserCircle, UserTimeout).
  Auth-visual trio is authored and wired into `subscribers.json`; all 13 components documented in
  `docs/data/components.json`.
- `README.md` component table and `CLAUDE.md` layout section are not yet updated to list the new
  components — that is a docs-tier task, not tracked as a story.
- Epics and backlog: see [USER_STORIES.md](USER_STORIES.md).

## 8. Quality bar {#MAU-§8}
A feature is **done** (✅) only when:
1. It builds clean where a build applies (`build.ps1` produces the artifact; or a future packaging subtree
   compiles) — per [HOUSE-LAW-8](../MindAttic.HouseRules.md#HOUSE-LAW-8).
2. For a component change, the relevant `sync/sync-*.ps1` runs **idempotently** (a second run yields no
   diff) and a single component is independently vendorable ([MAU-LAW-3](#MAU-LAW-3)).
3. Canonical source is edited in `Components/`/`Themes/` only; no derived/downstream copy is hand-edited
   ([MAU-LAW-2](#MAU-LAW-2)); no asset duplicated into any packaging subtree ([MAU-LAW-5](#MAU-LAW-5)).
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
- **jsDelivr CDN** — `cdn.jsdelivr.net/gh/mindattic/MindAttic.UiUx@<ref>/<path>`, the runtime delivery.
- **`subscribers.json`** — canonical components-registry + subscriber map (L5 data).
- **`Vn` tag** — a whole-number release tag; immutable once published ([MAU-LAW-6](#MAU-LAW-6)).
</content>
</invoke>
