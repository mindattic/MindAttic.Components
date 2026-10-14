---
codex: 1
project: MindAttic.Components
code: COM
layer: stories
status: living
updated: 2026-10-13
---

# MindAttic.Components — User Stories
> ✅ done (shipped & tested) · 🟡 partial · ⬜ planned. Every ✅ cites the test.

## Priority backlog
_Nothing recorded yet._

## Tickets
> Permanent log of every change and fix — this repo's ticket system. Rows are append-only: never
> delete or reword one; only move its Status (open → closed / wont-do) and fill in Ref. Status is
> a word, not a glyph, so tickets don't count toward the story index. Keep this section last.

| ID | Date | Type | Status | Summary | Ref |
|---|---|---|---|---|---|
| COM-T1 | 2026-10-13 | docs | closed | Start the permanent ticket log in USER_STORIES.md | — |
| COM-T2 | 2026-10-13 | feature | closed | Sand theme dunes build 90% slower (SAND_PILE 1.5 -> 0.15) so the scene stays open longer | web/themes/Sand/sand.js |
| COM-T3 | 2026-10-13 | fix | closed | Site build pins the newest V<n> tag of the repo by default; sites.json cdnTag removed so a hand rebuild can't repin a stale tag | web/build/build-sites.mjs; web/tests/package (183 passed) |
| COM-T4 | 2026-10-13 | feature | open | Migrate ryandebraal.com's skill-tooltip placement (lazy build on hover, flip below, keep on screen, --tt-arrow-x) into the Tag component as a script, so the page supplies only the tip text and inner styling | ryandebraal.com/index.htm init tooltip delegation; web/components/Tag |
| COM-T5 | 2026-10-13 | feature | open | MindAttic.Components.DotNet: framework-neutral design tokens package with thin per-framework packages (Wpf now; MAUI/Blazor when that work starts) | deferred design decision |
