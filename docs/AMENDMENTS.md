---
codex: 1
project: MindAttic.UiUx
code: MAU
layer: amendments
status: living
updated: 2026-06-07
---

# MindAttic.UiUx — Amendments (append-only; amendment wins over the bible)

> Append-only change log. Never rewrite an amendment; supersede it with a new one. Beyond ~25, fold into
> [BIBLE](BIBLE.md) and start a new epoch (note the git tag). History stays in git.

## MAU-A1 — Adopt the Codex documentation standard (supersedes —)
Installed the MindAttic Codex canonical-documentation layout in this repo: `docs/BIBLE.md` (L0),
`docs/AMENDMENTS.md` (L1), `docs/USER_STORIES.md` (L2), `docs/rfc/` (design notes), `docs/data/` (L5
canon-as-data), the generated `docs/BIBLE.digest.md`, `tools/codex.ps1` (doctor + digest), and the
`.claude/` SessionStart digest-injection hook. The BIBLE §5 Laws **inherit**
[`MindAttic.HouseRules.md`](../MindAttic.HouseRules.md) by reference and add the project-specific laws
MAU-LAW-1..6. No application/source code was changed. *Migration:* none required — there were no
pre-existing canon docs (`docs/`, `ARCHITECTURE.md`, etc.) to fold in; all content was authored fresh
from the repo's `README.md`, `CLAUDE.md`, and the actual `Components/`, `Themes/`, `sync/`, and `Ideas/`
trees.

## MAU-A2 — Record the broken `.idea` plugin build as verified state (supersedes —)
Captured the real, currently-failing `.idea` build in [BIBLE §6](BIBLE.md#MAU-§6) rather than asserting
"done". Verified 2026-06-07 against the sibling SDK at `../MindAttic.Ideas`: `Plugin.*` (6 projects) fail
with `CS0246: 'PluginBase' could not be found` (its `Bases.cs` has `ThemeBase`/`ControlBase`/`IdeaBase`
only); `Theme.Cyberspace` fails with `CS0117: 'ContentKind' does not contain a definition for 'Plugin'`;
`Control.Textbox` builds clean (`ControlBase`). The Abstractions API was refactored around the "Plugin"
concept (both base class and `ContentKind` enum). Tracked for resolution in
[RFC 0001](rfc/0001-pluginbase-sdk-drift.md). *No fix applied* (Codex install does not modify
application/source code).
</content>
