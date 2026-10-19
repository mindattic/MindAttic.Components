---
codex: 1
project: MindAttic.Components
code: M.COMP
layer: stories
status: living
updated: 2026-10-13
---
<!-- GENERATED from MindAttic.Workflow/data/workflow.db by `workflow db render`. Do not edit: this file is rewritten from the database; change stories and tickets with the workflow CLI. -->

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
| M.COMP-T1 | 2026-10-13 | docs | closed | Start the permanent ticket log in USER_STORIES.md | — |
| M.COMP-T2 | 2026-10-13 | feature | closed | Sand theme dunes build 90% slower (SAND_PILE 1.5 -> 0.15) so the scene stays open longer | web/themes/Sand/sand.js |
| M.COMP-T3 | 2026-10-13 | fix | closed | Site build pins the newest V<n> tag of the repo by default; sites.json cdnTag removed so a hand rebuild can't repin a stale tag | web/build/build-sites.mjs; web/tests/package (183 passed) |
| M.COMP-T4 | 2026-10-13 | feature | open | Migrate ryandebraal.com's skill-tooltip placement (lazy build on hover, flip below, keep on screen, --tt-arrow-x) into the Tag component as a script, so the page supplies only the tip text and inner styling | ryandebraal.com/index.htm init tooltip delegation; web/components/Tag |
| M.COMP-T5 | 2026-10-13 | feature | open | MindAttic.Components.DotNet: framework-neutral design tokens package with thin per-framework packages (Wpf now; MAUI/Blazor when that work starts) | deferred design decision |
| M.COMP-T6 | 2026-10-16 | chore | closed | install.ps1 adds this repo's 15 .prose/commands stubs for the shared global commands, so any LLM client finds them | mindattic-agent-standard/install.ps1 run 2026-10-16 |
| M.COMP-T7 | 2026-10-16 | chore | open | Bug hunt: run an exhaustive /bughunt over the whole repo and fix every real bug found (each fix under its own fix ticket), then hunt again; close this only when a full pass finds nothing | user request 2026-10-16: approved repo sweep |
| M.COMP-T8 | 2026-10-16 | chore | open | Run the full unit test suite and fix every failure (each under its own fix ticket) until it passes; put the passing run in --ref. No test project: close this wont-do saying so | user request 2026-10-16: approved repo sweep |
| M.COMP-T9 | 2026-10-16 | chore | open | Clean up code and docs: remove dead code, stale comments and outdated or duplicated docs, and simplify where behaviour doesn't change (/simplify); tests stay green | user request 2026-10-16: approved repo sweep |
| M.COMP-T10 | 2026-10-16 | docs | open | Bring README.md up to date with the code as it is now (what it is, setup, usage, commands, current state), following codex-standard/README-STYLE.md; README.htm regenerated if the repo has one | user request 2026-10-16: approved repo sweep |
| M.COMP-T11 | 2026-10-16 | chore | open | Move the WPF pieces Orchestrator and Workflow Desktop both copy into MindAttic.Components.Wpf: Mvvm (ObservableObject, RelayCommand, CollectionSync), the WCAG AA overrides (McRec, field border), FocusRing/Heading/SectionLabel/Card/PillChoice/PillToggle styles, persistent tooltips, zoom, and a high-contrast brush swap | M.FLOW M.FLOW-T64 |
| M.COMP-T12 | 2026-10-16 | feature | open | MindAttic.Components.Wpf takes the controls MindAttic.Orchestrator had to build itself, so every app is made of library controls: a pill single-choice row (ListBox; Orchestrator App.xaml PillChoice), a pill on/off toggle (ToggleButton; PillToggle), a card (Border; Card), a stat tile and its small variant (Button on McTileButton; StatCard/SmallStat), a keyboard focus ring (FocusRing), text styles Heading/SectionLabel/Muted, and a line chart (Orchestrator's UtilizationChart: time axis, percent axis, reference lines, segments coloured per point, a dotted projection, theme brushes); bump the package to the next whole version | — |
| M.COMP-T13 | 2026-10-18 | docs | closed | Update USER_STORIES.md | — |
