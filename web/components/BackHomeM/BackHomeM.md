# BackHomeM

A capital "M" in AtticFont pinned to the upper-left of the viewport, linking
back to `mindattic.com`. No JS — just a styled `<a href>` so it survives
script-disabled browsers and screen readers see it as a normal link.

Built for satellite pages so a visitor can always get back to the hub. No current page consumes it; it is available on the CDN for any satellite page.

---

## Layout

```
BackHomeM/
├── back-home-m.html   # usage comment + reference markup
└── back-home-m.css    # positioning + AtticFont stack + hover/focus state
```

---

## Usage

Add the anchor once anywhere inside `<body>` — leave it empty; the `❮ M`
glyphs are injected via a `::before` pseudo-element so subscribers don't
have to type them:

```html
<link rel="stylesheet" href="back-home-m.css">

<a class="back-home-m" href="https://mindattic.com"
   aria-label="Back to mindattic.com" title="Back to mindattic.com"></a>
```

Requires the `Attic` `@font-face` to be present in the host (the subscriber
already syncs `AtticFont/`). The CSS uses `font-family: 'Attic', serif;` and
falls back to a serif if the font hasn't loaded yet.

Or via jsDelivr:

```html
<link rel="stylesheet" href="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Web@V12/MindAttic.Web.Shared/Components/BackHomeM/back-home-m.css">
```

---

## Behavior

- `position: fixed; top: 5px; left: 5px;` — always anchored to the
  viewport's upper-left regardless of scroll position.
- `font-size: 1.25rem;` — tuned to read as a small return-glyph rather
  than a large display character.
- `::before` content `❮ M` (U+276E + nbsp + M) — injected by CSS so the
  anchor itself can stay empty.
- `z-index: 9999` — sits above page content but below modal layers.
- `font-family: 'Attic', serif;` — uses AtticFont when present, falls back
  to a default serif otherwise.
- Hover / focus: scales to 110%, opacity 0.7 → 1.0.
- Keyboard focus shows a visible outline (currentColor + 2px offset).

---

## Delivery

CDN only: a page includes `back-home-m.css` via a tag-pinned jsDelivr `<link>`
and hand-authors the `<a class="back-home-m">` element in its `<body>`. It is not
spliced by any sync script. The theme `body-prelude.html` files carry only
background-effect scaffolding, not this anchor.

Edit here only. Downstream copies are derived artifacts.
