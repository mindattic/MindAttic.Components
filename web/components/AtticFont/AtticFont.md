# AtticFont

The **Attic** display face, inlined as a base64 woff2. Single `@font-face`,
weight normal, style normal, `font-display: swap`. No network request — the
font ships with the document.

Used for headings and the wordmark across MindAttic web properties.

---

## Layout

```
AtticFont/
├── attic-font.html      # marker comment + usage note (banner for the font-inline build block)
├── attic-font.css       # the @font-face declaration (base64 woff2 inline)
├── attic-font.cdn.css   # the same @font-face pointing at fonts/attic.woff2 by CDN URL
├── attic-font.json      # descriptor: fontFamily, fallback (no applyToSelector)
└── fonts/attic.woff2    # the served font file
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

**Per-site apply rule** — a page that wants Attic on a specific element (e.g.
`.site-name` on mindattic.com) declares it in its `sites.json` `font-faces`
block as an `apply` pair (`[".site-name", "AtticFont"]`), and the site build
emits the rule. The component's `attic-font.json` deliberately has no
`applyToSelector`: each page declares its own.

Or via jsDelivr:

```html
<link rel="stylesheet" href="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Components@V3/web/components/AtticFont/attic-font.css">
```

---

## Site build delivery

The site build (`web/build/build-sites.mjs`, configured in `web/sites.json`)
writes Attic into `mindattic.com` through its `FONTS` block (`font-faces`):
the `@font-face` rule from `attic-font.cdn.css`, pointing at
`fonts/attic.woff2` by pinned CDN URL, plus the `--font-attic` variable and the
`.site-name` apply rule. No target currently inlines Attic as base64
(`font-inline`).

Edit here only. Downstream copies are derived artifacts.

Prefer the CDN woff2 over the inline base64 file: it is smaller and cached
across sites. Preload it from `<head>` with `crossorigin` (files and rules in
[`docs/web/ASSETS.md`](../../../docs/web/ASSETS.md)). The original TTF is kept
in `web/assets/archive/fonts/attic/` (in the repo, not served).
