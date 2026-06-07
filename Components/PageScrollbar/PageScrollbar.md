# PageScrollbar

Replaces the browser's native page scrollbar with a themed, draggable overlay
scrollbar — a single fixed `<div>` track + thumb on the right edge. Zero
dependencies, no build step.

The native scrollbar is hidden **only after** the script adds `.ma-sb-active`
to `<html>`, so a no-JS client keeps its normal scrollbar (progressive
enhancement). Namespaced under `.ma-scrollbar*` / `.ma-sb-*`.

---

## Layout

```
PageScrollbar/
├── page-scrollbar.css     # overlay track + thumb; hides native bar when active
├── page-scrollbar.js      # sizing, drag, click-to-page, scroll/resize/mutation sync
├── PageScrollbar.razor    # optional Blazor wrapper (SSR track, flash-free)
├── index.htm              # standalone demo
└── PageScrollbar.md        # this file
```

---

## Usage (HTML / any framework)

```html
<link rel="stylesheet" href="page-scrollbar.css">
<script src="page-scrollbar.js" defer></script>
```

That's it — the script self-mounts. Via jsDelivr:

```html
<link rel="stylesheet" href="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.UiUx@<tag>/Components/PageScrollbar/page-scrollbar.css">
<script src="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.UiUx@<tag>/Components/PageScrollbar/page-scrollbar.js" defer></script>
```

---

## Usage (Blazor)

Sync `PageScrollbar.razor` into your app, load the CSS/JS once at the root, then
drop one instance in your layout so the track is server-rendered (no flash):

```razor
@* MainLayout.razor *@
<main>@Body</main>
<PageScrollbar />
```

The JS reuses the server-rendered `.ma-scrollbar` element if present, or creates
one if absent. No interop or service registration.

---

## Behavior

- **Thumb size** = `viewport / content` ratio (min 28px); **position** = scroll ratio.
- **Drag** the thumb to scroll; **click** the track to smooth-scroll toward that point.
- Recomputes on `scroll`, `resize`, `fonts.ready`, and a `MutationObserver` on
  `<body>` (catches async content growth). Hidden automatically when the page
  doesn't overflow.

---

## Theming

```css
:root {
  --ma-sb-width: 14px;          /* hit area / track width  */
  --ma-sb-thumb-w: 8px;         /* thumb width             */
  --ma-sb-thumb-w-hov: 10px;    /* thumb width on hover    */
  --ma-sb-thumb-bg: #30363d;
  --ma-sb-thumb-hover: #dc3545;
  --ma-sb-min-thumb: 28px;
}
```

---

## Notes

- Overlay model (does not reserve layout width), like macOS. Content sits under
  the thin track; widen `--ma-sb-width` or add right padding if that matters.
- Idempotent (`window.__maScrollbar` guard).
- Pairs well with [Tooltip](../Tooltip/Tooltip.md) — both are attribute/markup
  driven and theme via CSS variables.
