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

For CDN-loaded subscribers (every page rendered by `MindAttic.Deploy`), the
per-page selector is applied by the theme's CSS, not by this component.

Or via jsDelivr:

```html
<link rel="stylesheet" href="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.UiUx@v1.0.0/Components/AtticFont/attic-font.css">
```

---

## Sync delivery

Two subscribers receive Attic via marker-block splice:

- `mindattic.com/index.htm` — inlined between
  `<!-- BEGIN MINDATTIC.UIUX:ATTICFONT --> … <!-- END … -->` markers by
  `sync/sync-mindattic-com.ps1`.
- `Prose/wwwroot/app.css` — rewritten between
  `/* == BEGIN MINDATTIC.UIUX:ATTICFONT.CSS == */` markers by
  `sync/sync-prose.ps1`.

Every other subscriber (catalog landing pages, Claudia, ChiMesh) gets
Attic from the jsDelivr CDN at runtime — pulled via `<link>` tags emitted
by `MindAttic.Deploy/template/index.template.htm` against the
`componentsVersion` pinned in `MindAttic.Deploy/projects.json`. Per-page
selectors (`#claudia`, `#chimesh`, `#idiotproof`, …) are applied by the
theme's CSS, not by this component.

Edit here only. Downstream copies are derived artifacts.
