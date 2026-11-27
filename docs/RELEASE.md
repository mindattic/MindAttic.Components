---
layer: release
status: living
---
# MindAttic.Components — Release charter

> The one finish line this repo is working toward. Every open ticket in this repo sits under one of the epics
> below, and every epic is part of the milestone. Work that does not fit is out of scope until this milestone is
> reached (owner, 2026-11-27, MAT-T319).

## 1. What this is
MindAttic's shared UI kit: the web package behind the MindAttic sites, a Unity package, the
`MindAttic.Components.Tokens` palette, and `MindAttic.Components.Wpf`, the NuGet package every MindAttic desktop
app (Orchestrator, Tickets Desktop, Launcher, Vault desktop, MW5 Save Editor) takes its WPF controls from.
The bible for the web package is [docs/web/BIBLE.md](web/BIBLE.md).

## 2. The milestone: MindAttic.Components One library, one button {#milestone}
- **Milestone:** MS-10, "MindAttic.Components One library, one button"
- **Published means:** `MindAttic.Components.Wpf` on nuget.org (and the local feed via
  `dotnet/tools/publish-wpf.ps1`), released only from a commit whose CI run is green.
- **The natural stopping point:** the owner rule holds in the code: WPF controls come from MindAttic.Components,
  and `MaButton` is the one property-configurable button. Every legacy button style is a pixel-perfect `MaButton`
  preset with no template of its own; the consumers have a library piece for every local style they still keep;
  and the package is tested on NUnit with CI gating the publish. At that point the library is complete for its
  real consumers, and new controls wait for a consumer to ask.

## 3. Done when (acceptance checks)
1. build — `MindAttic.Components.slnx` builds clean on main.
2. exists — `docs/RELEASE.md` (this charter) is in place.
3. signoff — the owner runs Tickets Desktop and the Orchestrator with no local control styles left, and every
   converted button looks pixel-identical to before.

Also tracked: no `Button`/`ToggleButton` ControlTemplate in `Theme.xaml` outside the `MaButton` control, and the
package published on nuget.org with the presets.

## 4. Epics, in priority order
| Priority | Epic (ticket) | What it delivers | Features |
|---|---|---|---|
| 900 | M.COMP-T244 One Button | Every legacy Button/ToggleButton style becomes a pixel-perfect `MaButton` preset | M.COMP-T247 Pixel-perfect baselines (990), M.COMP-T248 Button presets (980) |
| 800 | M.COMP-T245 Consumer parity, no local styles | The pieces Tickets Desktop's `Theme/Styles.xaml` and the Orchestrator still keep locally | M.COMP-T249 Combo and chip (890), M.COMP-T250 Lists and grids (880), M.COMP-T251 Menus (870) |
| 700 | M.COMP-T246 Release hygiene | NUnit tests, CI gating the nuget.org publish, a current README | M.COMP-T252 NUnit tests (790), M.COMP-T253 CI and publish gate (780), M.COMP-T254 Docs current (770) |

Tasks: baselines M.COMP-T255; presets M.COMP-T256 (command buttons), M.COMP-T257 (tile and stat), M.COMP-T258
(pill and pin toggles), M.COMP-T119 (MaStatCard height); combo/chip M.COMP-T120, M.COMP-T202; lists M.COMP-T55,
M.COMP-T200, M.COMP-T201; menus M.COMP-T59, M.COMP-T191; NUnit M.COMP-T198; CI M.COMP-T203, M.COMP-T204; docs
M.COMP-T259.

## 5. Out of scope (not before this milestone)
- JS (`MindAttic.Components.Js`) and Blazor parity components for every WPF control — no consumer asked; the
  Blazor package does not exist.
- Design-system "brainstorm" components with no consumer (radio, toggle switch, accordion, breadcrumb,
  pagination, data grid, card, alert, skeleton, stepper, rating, file upload, typeahead combobox, date picker,
  colour picker, segmented control, popover, empty state).
- Open-ended bughunts and audit sweeps.
- New web or Unity package features without a consumer ticket.
- `MindAttic.Components.SIDEBAR`, a second checkout of this repo on a stale `SIDEBAR` branch; it gets no work.

## 6. Current verified state
- `MindAttic.Components.Wpf` 50.0.0 (`MindAttic.Components.Wpf.csproj`), with the `MaButton` control ("the one
  button": shape, per-state colours, accent, toggle/group) in `Controls/MaButton.cs`.
- nuget.org metadata and `dotnet/tools/push-wpf-nugetorg.ps1` are in place (M.COMP-T173).
- `MaCheck` themed CheckBox (094d538), full-width `MaSeparator` (a40cfd8), `MaPicker`, `MaTree`, `MaPanel` /
  `MaPanelGroup` with drag reorder, `MaSlider`, `MaTabControl` sidebar mode are shipped.
- Still open: legacy button styles carry their own templates (`MaToolButton`, `MaTextButton`, `MaButton` style,
  `MaPrimaryButton`, `MaPillChoice`, `MaPillToggle`, `MaPinToggle`); Wpf.Tests is xUnit; there is no CI workflow
  that runs tests.

## 7. Decision record
- 2026-11-27 — charter drafted from README/BIBLE/docs/tickets; Legion vote: **Approve**, plurality quorum
  reached, 9/9 voters answered, consensus 0.89. Dissent (1): make sure MaButton is fully tested before release
  and put consumer parity first — kept the order, since the presets are the owner's stated rule and each one is
  proven against its baseline.
- Triage: 72 open → done 4, duplicate 1, wont-do 56 (incl. the two container stories M.COMP-T118 and
  M.COMP-T129), kept 11, new tasks 5. SIDEBAR's CS-T10 (exhaustive bughunt) closed wont-do as a duplicate of
  M.COMP-T212.
