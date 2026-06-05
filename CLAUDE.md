# MindAttic.UiUx Project Rules

## Conversation
- A bare "do" / "do it" / "yes" from the user means "continue", "keep going", "proceed". Resume the current task without asking for clarification.

## What this is
- Source-of-truth repo for the **reusable front-end components** (fonts, effects, helpers) used across MindAttic web properties.
- Components live in `Components/` — each is fully self-contained (CSS/JS/HTML/JSON/MD).
- This repo **does not deploy**. Subscribers either pull from the jsDelivr CDN at runtime, or receive splice-in-place updates via three remaining sync scripts (see below).

## Ownership boundary (read before editing `sync/`)
- **MindAttic.Deploy** (`D:/Projects/MindAttic/MindAttic.Deploy`) owns: catalog landing pages (IdiotProof, GridGame2026, MindAttic.Legion, MindAttic.Mobile, MediaButler, MindAttic.Vault, TaxRateCollector, ThinkTank, Tutor, MindAttic.Psst index) and the Claudia/ChiMesh long-form HTML builds. Those subscribers pull components from jsDelivr at runtime; they are **not** spliced from this repo. Do not add `landing-page` or `build-html-js` kinds back to `subscribers.json`, and do not recreate `sync-landing-page.ps1`, `sync-claudia.ps1`, or `sync-chimesh.ps1`. Those were deleted on purpose.
- **MindAttic.UiUx still owns splice-in-place delivery** for these subscribers, because they consume content in formats jsDelivr can't satisfy alone:
  - `mindattic.com/index.htm` — html-inline marker blocks (`sync-mindattic-com.ps1`). One exception: the Cyberspace `console-bg.js` (~580 KB) and `sacred-geometry.js` are loaded from jsDelivr at a pinned tag (`sync-mindattic-com.ps1 -CyberspaceCdnTag`, default `V4`) rather than inlined; everything else in the block is still inline. Bump the tag (and re-tag the repo) when `console-bg.js` or `sacred-geometry.js` changes.
  - `StreetSamurai/v3/StreetSamurai.Blazor/wwwroot/` — JS copy + CSS marker blocks in `app.css` (`sync-streetsamurai.ps1`).
  - `MindAttic.Ideas/src/MindAttic.Ideas.Web/wwwroot/` — same `blazor-wwwroot` model (`sync-ideas.ps1`). Subscriber registered for the shared auth-visual components (UserLogin/UserCircle/UserTimeout); subscriptions are empty until those are authored, so the script is a no-op for now.
  - `Tutor/Tutor.Blazor/wwwroot/` — same `blazor-wwwroot` model (`sync-tutor.ps1`). Registered for the same auth-visual components; no-op until subscribed (Tutor.Blazor has no `wwwroot` yet — create it with the marker pairs when the first subscription lands).
  - `MindAttic.Psst/{terms,privacy}.htm` — html-inline marker blocks (`sync-mindattic-psst.ps1`). The `index.htm` in MindAttic.Psst is rendered by MindAttic.Deploy from `README.md`; this repo does not touch it.

  NOTE: Ideas and Tutor are Blazor *apps* (splice subscribers like StreetSamurai), distinct from the MindAttic.Deploy-owned catalog landing pages — the "Tutor" in the `subscribers.json` `$comment`'s landing-page list refers to Tutor's marketing page on MindAttic.Deploy, not this Blazor app subscriber.

## Layout
- `Components/` — canonical component source (Cyberspace, OutfitFont, AtticFont, BackHomeM, PinFooter, WebSnapshot).
- `sync/` — three PowerShell splice scripts plus `sync-all.ps1` umbrella. Only the three live subscribers above; nothing else.
- `subscribers.json` — declares which components flow to each of the three live subscribers, with per-subscription overrides.
- `.github/workflows/sync-subscribers.yml` — GitHub Action that opens cross-repo PRs against the three live subscribers on push to `main`.

## Versioning (per MindAttic.Ideas rules)
- Tags are **whole numbers only** — `V1`, `V2`, `V3`, … — never SemVer (no `v1.1.1`). See `MindAttic.Ideas/README.md`. The historical SemVer tags map: `v1.0.0` = `V1`, `v1.0.1` = `V2`, `v1.1.0` = `V3`; the next release is `V4` and so on.
- You **never mutate** a published tag; you ship the next whole number alongside it, and subscribers pin the exact one. Bump the number on every content release that subscribers must pick up.

## Delivery pipelines
- **jsDelivr CDN** — `https://cdn.jsdelivr.net/gh/mindattic/MindAttic.UiUx@<Vn>/Components/<file>`. This is how MindAttic.Deploy and any other future runtime-loader subscriber pulls content. Tag the repo (whole number) to ship; subscribers pin the tag in their own configs (e.g. `MindAttic.Deploy/projects.json:componentsVersion`).
- **GitHub Actions cross-repo sync** — on push to `main`, opens PRs into the three splice-in-place subscriber repos.
- **Local PowerShell** — `powershell -File sync/sync-all.ps1` runs the three scripts against your working copies for fast iteration. (Also invoked by `MindAttic.Deploy` as a `preDeploy` hook for `mindattic.com` and `StreetSamurai` so the bundle is fresh before FTPS upload.)

## Editing rule
- Edit only in `Components/`. Push to `main` and let GitHub Actions deliver, or run `sync/sync-all.ps1` locally for fast iteration. Downstream copies are derived artifacts — never hand-edit them.
- Bumping the CDN tag (next whole number `Vn`) is what propagates content to MindAttic.Deploy-rendered subscribers. Bump in `MindAttic.Deploy/projects.json:componentsVersion`, then run that repo's deploy.
