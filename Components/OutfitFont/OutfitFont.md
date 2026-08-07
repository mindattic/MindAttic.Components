# OutfitFont

The **Outfit** variable font (Google Fonts, weights 100–900, variable axis),
inlined as base64 woff2. Two `@font-face` declarations:

- Latin range (`U+0000-00FF`, …)
- Latin-Extended range (`U+0100-02BA`, …)

No network request — the font ships with the document.

Used as the default body face across MindAttic web properties.

---

## Layout

```
OutfitFont/
├── outfit-font.html   # marker comment + usage note
└── outfit-font.css    # two @font-face declarations (base64 woff2 inline)
```

---

## Usage

```html
<link rel="stylesheet" href="outfit-font.css">

<style>
  /* Two equivalent ways to activate Outfit on an element: */
  body { font-family: 'Outfit', system-ui, sans-serif; }    /* explicit stack */
  body { font-family: var(--font-outfit); }                 /* same thing, via the CSS variable the file exposes */

  /* Variable axis — any weight 100..900 works */
  .light { font-weight: 200; }
  .bold  { font-weight: 700; }
</style>
```

**Gotcha** — bare `font-family: Outfit;` (no quotes, no fallback) fails
silently in some browsers / CSS contexts. Always use the quoted form with a
generic fallback (`'Outfit', system-ui, sans-serif`) or the
`var(--font-outfit)` shorthand the CSS file exposes.

**Activating on the whole page** — the sync pipeline reads
`applyToSelector` from `outfit-font.json` (default: `html, body`) and emits
the rule automatically for any subscriber that opts into OutfitFont
through `subscribers.json`.

Or via jsDelivr:

```html
<link rel="stylesheet" href="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.UiUx@v1.0.0/Components/OutfitFont/outfit-font.css">
```

---

## Sync delivery

Three subscribers receive Outfit via marker-block splice:

- `mindattic.com/index.htm` — inlined between
  `<!-- BEGIN MINDATTIC.UIUX:OUTFITFONT --> … <!-- END … -->` markers by
  `sync/sync-mindattic-com.ps1`.
- `Prose/wwwroot/app.css` — rewritten between
  `/* == BEGIN MINDATTIC.UIUX:OUTFITFONT.CSS == */` markers by
  `sync/sync-prose.ps1`.
- `MindAttic.Psst/{terms,privacy}.htm` — inlined by
  `sync/sync-mindattic-psst.ps1`.

Every other subscriber (catalog landing pages, Claudia, ChiMesh) gets
Outfit from the jsDelivr CDN at runtime — pulled via `<link>` tags emitted
by `MindAttic.Deploy/template/index.template.htm` against the
`componentsVersion` pinned in `MindAttic.Deploy/projects.json`.

Edit here only. Downstream copies are derived artifacts.
