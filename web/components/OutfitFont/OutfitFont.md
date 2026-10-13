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
<link rel="stylesheet" href="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Web@V12/MindAttic.Web.Shared/Components/OutfitFont/outfit-font.css">
```

---

## Sync delivery

One active subscriber receives Outfit via marker-block splice:

- `MindAttic.Psst/{terms,privacy}.htm` — inlined between
  `<!-- BEGIN MINDATTIC.UIUX:OUTFITFONT --> … <!-- END … -->` markers by
  `sync/sync-mindattic-psst.ps1`.

`mindattic.com` is not enrolled (it loads the font from the CDN), and the Prose
subscribers (`blazor-wwwroot`, `/* == BEGIN MINDATTIC.UIUX:OUTFITFONT.CSS == */`
markers) are retired (see `subscribers.json`).

Edit here only. Downstream copies are derived artifacts.

---

## Loading it from the CDN instead of inlining

Sites that are not enrolled for splicing (for example `mindattic.com` and `ryandebraal.com`) load the same
font as a plain woff2 file from the shared asset package, which is smaller than inlining it and is cached
across sites: `https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Web@<tag>/MindAttic.Web.Shared/fonts/outfit/` (files and rules in
[`docs/ASSETS.md`](../../docs/ASSETS.md)). Preload the file from `<head>` with `crossorigin`. The original
TTF is kept in `archive/fonts/outfit/` (in the repo, not served).
