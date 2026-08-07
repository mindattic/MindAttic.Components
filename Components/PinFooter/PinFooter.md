# PinFooter

Pin-when-short footer. If the document is shorter than the viewport,
elements with the class `pin-when-short` get fixed-positioned to the bottom
of the viewport so the page doesn't end with awkward dead space. As soon as
the content overflows, the element flows normally — a pinned footer over
scrollable content would cover the last line.

Standalone from `Cyberspace/`. Has its own sync marker pair so subscribers
can adopt it independently.

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
<link  rel="stylesheet" href="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.UiUx@v1.0.0/Components/PinFooter/pin-footer.css">
<script src="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.UiUx@v1.0.0/Components/PinFooter/pin-footer.js" defer></script>
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

## Sync delivery

`sync/sync-mindattic-com.ps1` inlines the bundle between
`<!-- BEGIN MINDATTIC.UIUX:PINFOOTER --> … <!-- END … -->` markers in
`mindattic.com/index.htm`. `sync/sync-prose.ps1` copies only the JS into
the Blazor `wwwroot/js/` folder — the subscription is marked `jsOnly`, so no
`app.css` marker block is written (Prose keeps its own pin-footer CSS).

Edit in this folder only — downstream copies are derived artifacts.
