---
codex: 1
project: MindAttic.UiUx
code: MAU
layer: stories
status: living
updated: 2026-06-07
---

# MindAttic.UiUx — User Stories
> ✅ done (shipped & tested) · 🟡 partial · ⬜ planned · 🗑️ cut. Every ✅ cites the test.
>
> **Note:** this repo has **no automated test project** (see [BIBLE §6](BIBLE.md#MAU-§6)). Per
> [HOUSE-LAW-8](../MindAttic.HouseRules.md#HOUSE-LAW-8) no story is marked ✅ on a test it cannot name.
> Capabilities that exist but are only manually/downstream-verified are 🟡, with the build/sync command
> that would demonstrate them noted as *evidence*.

## Epic A — Authoring & catalog
- **MAU-US-A1 🟡** As a component author, I can add a self-contained component under `Components/<Name>/`
  (source + `.json` config + `.md` doc) without touching any other component, so the catalog grows by
  addition. *Given a new folder, When I register it in `subscribers.json` `components`, Then it is
  shippable.* *(Present for 7 components; no test harness — manual.)*
- **MAU-US-A2 🟡** As an author, I can compose a Theme under `Themes/<Name>/` that references components
  via `deps.json`, so a property can adopt a whole look at once. *(Present: `Themes/Cyberspace/`; manual.)*
- **MAU-US-A3 🟡** As an author, I can regenerate SacredGeometry shape posters with
  `build-previews.mjs`, which doubles as a smoke test, so the 1024-shape catalog is self-checking.
  *(Script present; not run this session.)*

## Epic B — Distribution
- **MAU-US-B1 🟡** As a subscriber maintainer, I can enroll/unenroll a property in a component by editing
  one `subscriptions` line in `subscribers.json`, so enrollment is declarative
  ([MAU-LAW-1](BIBLE.md#MAU-LAW-1)). *Given a line added, When the next sync runs, Then the marker block
  appears/disappears.* *(Manual — no automated assertion in-repo.)*
- **MAU-US-B2 🟡** As a maintainer, I can run `sync/sync-all.ps1` locally and have only the
  `BEGIN/END MINDATTIC.UIUX` marker blocks change, idempotently ([MAU-LAW-2](BIBLE.md#MAU-LAW-2)).
  *(Evidence: re-run yields no diff; not captured this session.)*
- **MAU-US-B3 🟡** As a runtime-loader subscriber (via `MindAttic.Deploy`), I can pull any component file
  from `cdn.jsdelivr.net/gh/mindattic/MindAttic.UiUx@<Vn>/...` pinned to an immutable tag
  ([MAU-LAW-6](BIBLE.md#MAU-LAW-6)). *(Verified out-of-repo by the CDN; nothing to run here.)*
- **MAU-US-B4 🟡** As a maintainer, on push to `main` the GitHub Action opens cross-repo PRs into the
  three splice subscribers. *(Workflow committed; not exercised this session.)*

## Epic C — `.idea` packaging
- **MAU-US-C1 ⬜** As a `.idea` consumer, I can run `build.ps1 -Build <Name> -Output idea` and get a
  packed `*.V1.idea` whose `wwwroot/` is staged from canonical UiUx assets
  ([MAU-LAW-5](BIBLE.md#MAU-LAW-5)). *Currently blocked by SDK drift: `Plugin.*` fail (`CS0246 PluginBase`)
  and `Theme.Cyberspace` fails (`CS0117 ContentKind.Plugin`); only `Control.Textbox` builds. See
  [RFC 0001](rfc/0001-pluginbase-sdk-drift.md) and [BIBLE §6](BIBLE.md#MAU-§6).*
- **MAU-US-C2 🟡** As a consumer, I can run `build.ps1 -Build <Name> -Output standalone` to copy the raw
  canonical assets verbatim, so I can vendor without packaging. *(Path does not invoke the failing SDK
  build; not run this session.)*
- **MAU-US-C3 ⬜** As a consumer, I can run `build.ps1 -Build <Name> -Output blazor` to get a Blazor RCL
  wrapper. *Stub: warns "not yet implemented".*

## Priority backlog
1. **MAU-US-C1** — reconcile `Ideas/Plugin.*` with the current `MindAttic.Ideas.Abstractions` so the
   `.idea` build compiles (the one hard-broken path). → [RFC 0001](rfc/0001-pluginbase-sdk-drift.md).
2. Add a minimal in-repo verification harness (sync idempotency check + a `.idea` build smoke) so Epic B
   and C stories can graduate to ✅ with a named test.
3. **MAU-US-C3** — implement the `blazor` output target in `build.ps1`.
4. Author the auth-visual components (UserLogin/UserCircle/UserTimeout) the Ideas/Tutor subscribers are
   pre-registered for.

### Audit log
No stories have been changed from an original spec yet. (When a story's ask changes, the original text is
preserved here verbatim, marked "(original spec — audit log)".)
</content>
