# AtticFont

The **Attic** display face, inlined as a base64 woff2. Single `@font-face`,
weight normal, style normal, `font-display: swap`. No network request — the
font ships with the document.

Used for headings and the wordmark across MindAttic web properties.

---

## Layout

```
AtticFont/
├── attic-font.html   # marker comment + usage note
└── attic-font.css    # the @font-face declaration (base64 woff2 inline)
```

---

## Usage

```html
<link rel="stylesheet" href="attic-font.css">

<style>
  /* Two equivalent ways to activate Attic on an element: */
  h1, .title { font-family: 'Attic', serif; }      /* explicit stack */
  h1, .title { font-family: var(--font-attic); }   /* same thing, via the CSS variable the file exposes */
</style>
```

**Gotcha** — bare `font-family: Attic;` (no quotes, no fallback) fails
silently in some browsers / CSS contexts. Always use the quoted form with a
generic fallback (`'Attic', serif`) or the `var(--font-attic)` shorthand
the CSS file exposes.

**Per-subscriber apply rule** — splice-in-place subscribers want Attic on
a specific element (e.g. `.site-name` for mindattic.com). That mapping
lives in `subscribers.json` as the `applyToSelector` field on each
subscription, so the sync pipeline emits the right rule automatically.
The component's `attic-font.json` deliberately has no default
`applyToSelector` — each subscriber declares its own.

For CDN-loaded consumers (the sites), the selector is applied by the
page's own CSS, not by this component.

Or via jsDelivr:

```html
<link rel="stylesheet" href="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Web@V12/MindAttic.Web.Shared/Components/AtticFont/attic-font.css">
```

---

## Sync delivery

No active subscriber currently receives Attic via marker-block splice. The
splice contract is still supported: an `html-inline` subscriber gets it between
`<!-- BEGIN MINDATTIC.UIUX:ATTICFONT --> … <!-- END … -->` markers, a
`blazor-wwwroot` subscriber between
`/* == BEGIN MINDATTIC.UIUX:ATTICFONT.CSS == */` markers. `mindattic.com` is
not enrolled (it loads the font from the CDN), and the Prose subscribers are
retired (see `subscribers.json`).

Edit here only. Downstream copies are derived artifacts.

---

## Loading it from the CDN instead of inlining

Sites that are not enrolled for splicing (for example `mindattic.com` and `ryandebraal.com`) load the same
font as a plain woff2 file from the shared asset package, which is smaller than inlining it and is cached
across sites: `https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Web@<tag>/MindAttic.Web.Shared/fonts/attic/` (files and rules in
[`docs/ASSETS.md`](../../docs/ASSETS.md)). Preload the file from `<head>` with `crossorigin`. The original
TTF is kept in `archive/fonts/attic/` (in the repo, not served).
