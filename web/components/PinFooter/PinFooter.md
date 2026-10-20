# PinFooter

Pin-when-short footer. If the document is shorter than the viewport,
elements with the class `pin-when-short` get fixed-positioned to the bottom
of the viewport so the page doesn't end with awkward dead space. As soon as
the content overflows, the element flows normally — a pinned footer over
scrollable content would cover the last line.

Standalone from `Cyberspace/`, so a page can adopt it on its own.

---

## Layout

```
PinFooter/
├── pin-footer.html   # usage comment (no DOM injected by the script)
├── pin-footer.css    # `.pin-when-short.pinned` rule (position: fixed bottom)
└── pin-footer.js     # toggles `.pinned` on resize / mutation / font-ready
```

---

## Usage

Opt in by adding the class to any element you want to pin. Multiple targets
are supported; each toggles independently.

```html
<link  rel="stylesheet" href="pin-footer.css">
<script src="pin-footer.js" defer></script>

<footer class="pin-when-short">© 2026 MindAttic</footer>
```

Or via jsDelivr:

```html
<link  rel="stylesheet" href="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Components@V3/web/components/PinFooter/pin-footer.css">
<script src="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Components@V3/web/components/PinFooter/pin-footer.js" defer></script>
```

---

## How the toggle decides

`pin-footer.js` runs once on `DOMContentLoaded`, then on:

- `window.resize`
- `document.fonts.ready` (catches late reflow when web fonts swap in)
- A `MutationObserver` on `document.body` that re-checks whenever
  `scrollHeight` changes (catches DOM growth after async content loads)

The decision is a single comparison:

```js
hasScrollbar = document.documentElement.scrollHeight > window.innerHeight;
target.classList.toggle('pinned', !hasScrollbar);
```

When `pinned` is on the element, `pin-footer.css` applies:

```css
.pin-when-short.pinned {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
}
```

---

## Delivery

No site currently uses PinFooter and no `web/sites.json` block splices it.
A page loads `pin-footer.css` and `pin-footer.js` by tag-pinned jsDelivr URL
(above), or a `style`/`script` block can be added for it in `sites.json`.

Edit in this folder only — downstream copies are derived artifacts.
