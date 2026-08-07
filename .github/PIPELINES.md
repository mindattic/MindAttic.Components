# Delivery pipelines

MindAttic.UiUx is a source-of-truth repo. Content reaches subscribers two ways:

1. **jsDelivr CDN** — every file is served at a versioned URL. `MindAttic.Deploy` consumes the CDN for every catalog landing page (IdiotProof, GridGame2026, MindAttic.Legion, MediaButler, MindAttic.Vault, TaxRateCollector, ThinkTank, Tutor, MindAttic.Psst index) and the Claudia / ChiMesh long-form HTML builds. That's the default path for anything new.
2. **Marker-block sync** (GitHub Action + local PowerShell) — for the three subscribers that need build-time splice into hand-authored files: `mindattic.com/index.htm`, `Prose/wwwroot/`, and `MindAttic.Psst/{terms,privacy}.htm`.

If you are wiring up a new subscriber, prefer pipeline 1. Pipeline 2 is reserved for cases where the subscriber genuinely needs content inlined inside files it also hand-authors.

## Pipeline 1 — jsDelivr CDN (runtime)

Every file in this repo is served by [jsDelivr](https://www.jsdelivr.com/) at a versioned URL — zero setup, global edge cache.

**URL shape:**

```
https://cdn.jsdelivr.net/gh/mindattic/MindAttic.UiUx@<ref>/<ComponentFolder>/<file>
```

Where `<ref>` is any of:

| Ref form | Example | Behavior |
|---|---|---|
| Tag (recommended for prod) | `@v1.0.0` | Immutable; cached forever |
| Branch | `@main` | Cached ~7 days unless purged |
| Commit SHA | `@a1b2c3d` | Immutable; cached forever |

Component folder names are case-sensitive on GitHub (`Cyberspace`, `OutfitFont`, `AtticFont`, `PinFooter`, `BackHomeM`, `WebSnapshot`). Theme folders too (`Themes/Cyberspace` — the sole theme since Hardware was retired 2026-05-29).

**Examples:**

```html
<!-- pinned, production -->
<script src="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.UiUx@v1.0.0/Components/Cyberspace/console-bg.js"></script>
<link  rel="stylesheet" href="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.UiUx@v1.0.0/Components/Cyberspace/frontpage.css">

<!-- bleeding edge (dev only) -->
<script src="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.UiUx@main/Components/Cyberspace/console-bg.js"></script>
```

**Cutting a release:**

```bash
git tag v1.0.0
git push --tags
# jsDelivr serves the new tag immediately
```

To propagate a release to every `MindAttic.Deploy`-rendered subscriber, bump `componentsVersion` in `MindAttic.Deploy/projects.json` and run that repo's deploy.

**Purging the cache (rare, only needed for branch refs):**

`https://purge.jsdelivr.net/gh/mindattic/MindAttic.UiUx@main/Components/Cyberspace/console-bg.js` — GET to purge.

## Pipeline 2 — GitHub Actions cross-repo sync (in-repo copies)

`.github/workflows/sync-subscribers.yml` runs on every push to `main` that touches any component folder, `subscribers.json`, `sync/**`, or the workflow itself.

It targets only the three splice-in-place subscribers:

- **mindattic.com** — marker blocks in `index.htm` are inlined for first-paint perf and to keep the page self-contained for FTPS upload by `MindAttic.Deploy`.
- **Prose** — Blazor's `wwwroot/js/*` is part of the build output; carrying the JS locally keeps the offline dev loop fast and gives the .csproj a deterministic input.
- **MindAttic.Psst (terms.htm + privacy.htm)** — small legal pages that are hand-authored around the OutfitFont marker block; `index.htm` is NOT touched (it's rendered by `MindAttic.Deploy` from `MindAttic.Psst/README.md`).

For each subscriber it:

1. Checks out MindAttic.UiUx
2. Checks out the subscriber repo (using a fine-grained PAT)
3. Runs the matching `sync/sync-*.ps1` script with `-ContentRoot` and subscriber paths supplied
4. Opens (or updates) a PR titled "Sync from MindAttic.UiUx" on branch `auto/sync-components`

## One-time setup (PAT for cross-repo PRs)

The Action needs write access to the three subscriber repos. Create a **fine-grained personal access token**:

1. Go to https://github.com/settings/personal-access-tokens/new
2. Repository access: **All repositories owned by `mindattic`** — covers every current and future subscriber automatically
3. Repository permissions:
   - **Contents**: Read and write
   - **Pull requests**: Read and write
   - **Metadata**: Read-only (auto-included)
4. Expiration: pick whatever you're comfortable rotating (e.g. 1 year)
5. Generate token, copy the value

Then in this repo (MindAttic.UiUx):

1. Go to https://github.com/mindattic/MindAttic.UiUx/settings/secrets/actions
2. **New repository secret**
3. Name: `SUBSCRIBER_REPO_TOKEN`
4. Value: paste the PAT

The workflow will now succeed.

## Pipeline 3 — PowerShell `sync/*.ps1` (local dev fallback)

For fast iteration without pushing to GitHub, the `sync/sync-all.ps1` script does the same work locally against your working copies of the three subscriber repos. Run it after any edit under a component folder:

```powershell
powershell -File sync/sync-all.ps1
```

`MindAttic.Deploy` also invokes the individual `sync-mindattic-com.ps1` and `sync-prose.ps1` scripts as `preDeploy` hooks so the bundle is fresh before each FTPS upload.

## Choosing a pipeline per subscriber

| Subscriber kind | Recommended runtime source | Why |
|---|---|---|
| `mindattic.com` | Inlined marker block (kept fresh by Action / `sync-mindattic-com.ps1`) | Static HTML site; inlining = zero-RTT first paint, also avoids a CDN dependency in the FTPS-uploaded artifact |
| `Prose` (Blazor) | Local `wwwroot/js/*.js` + `app.css` marker blocks (kept fresh by Action / `sync-prose.ps1`) | Blazor build needs deterministic input; offline-dev friendly |
| `MindAttic.Psst` legal pages | Inlined marker blocks in `terms.htm` + `privacy.htm` (kept fresh by Action / `sync-mindattic-psst.ps1`) | Same as mindattic.com — these pages get FTPS-uploaded as self-contained HTML |
| Any catalog landing page | jsDelivr CDN, pinned via `MindAttic.Deploy/projects.json:componentsVersion` | No PR overhead; `MindAttic.Deploy` renders these from each project's `README.md` and pulls fonts/effects from the CDN at runtime |
| Claudia / ChiMesh | jsDelivr CDN, same path as catalog landing pages | They render long-form READMEs with the Cyberspace theme + the parts-picker augmentation; CDN keeps each guide page small |
| Anything new | jsDelivr CDN | Only fall back to pipeline 2 if the subscriber needs hand-authored content interleaved with the components |
