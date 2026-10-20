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
├── outfit-font.html      # marker comment + usage note (banner for the font-inline build block)
├── outfit-font.css       # two @font-face declarations (base64 woff2 inline)
├── outfit-font.cdn.css   # the same declarations pointing at fonts/*.woff2 by CDN URL
├── outfit-font.json      # descriptor: applyToSelector (html, body), fontFamily, fallback
└── fonts/                # outfit-latin.woff2, outfit-latin-ext.woff2 (the served files)
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

**Activating on the whole page** — a `font-inline` build block reads
`applyToSelector` from `outfit-font.json` (`html, body`) and emits the rule;
a `font-faces` block declares its own `apply` pairs in `sites.json`.

Or via jsDelivr:

```html
<link rel="stylesheet" href="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Components@V3/web/components/OutfitFont/outfit-font.css">
```

---

## Site build delivery

The site build (`web/build/build-sites.mjs`, configured in `web/sites.json`)
delivers Outfit two ways:

- **Inline (`font-inline`)** — `MindAttic.Psst/{privacy,terms}.htm` get
  `outfit-font.css` (base64) between their
  `<!-- BEGIN MINDATTIC.COMPONENTS:OUTFITFONT -->` markers, so the legal pages
  make no network request.
- **By CDN URL (`font-faces`)** — mindattic.com, ryandebraal.com and
  mindatticcares.com get the `@font-face` rules from `outfit-font.cdn.css` in
  their `FONTS` block, pointing at `fonts/*.woff2` at the pinned tag: smaller
  than inlining and cached across sites. Preload the files from `<head>` with
  `crossorigin` (files and rules in
  [`docs/web/ASSETS.md`](../../../docs/web/ASSETS.md)).

Edit here only. Downstream copies are derived artifacts. The original TTF is
kept in `web/assets/archive/fonts/outfit/` (in the repo, not served).
