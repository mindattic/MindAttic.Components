# Tooltip

A standalone, dependency-free, accessible tooltip. One floating element is
reused for every trigger; elements opt in with a `data-tooltip` attribute —
added at any time, even after async/Blazor render. No library, no build step.

Built to replace heavyweight component-library tooltips (e.g. Radzen) whose
global CSS bleeds into a host theme. Everything here is namespaced under
`.ma-tooltip*` and driven by `data-*` attributes, so it cannot infect a host.

---

## Layout

```
Tooltip/
├── tooltip.css     # .ma-tooltip* styles, themed via CSS variables
├── tooltip.js      # delegated hover/focus listeners; one reused floating node
├── Tooltip.razor   # optional Blazor wrapper (sync a copy into your app)
├── index.htm       # standalone demo
└── Tooltip.md      # this file
```

---

## Usage (HTML / any framework)

```html
<link rel="stylesheet" href="tooltip.css">
<script src="tooltip.js" defer></script>

<button data-tooltip="Hello">Hover me</button>
```

Via jsDelivr:

```html
<link rel="stylesheet" href="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.UiUx@<tag>/Components/Tooltip/tooltip.css">
<script src="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.UiUx@<tag>/Components/Tooltip/tooltip.js" defer></script>
```

### Trigger attributes

| Attribute             | Required | Default | Notes                                              |
|-----------------------|----------|---------|----------------------------------------------------|
| `data-tooltip`        | yes      | —       | Text to display.                                   |
| `data-tooltip-pos`    | no       | `top`   | `top` \| `bottom` \| `left` \| `right`; auto-flips. |
| `data-tooltip-delay`  | no       | `0`     | Show delay in ms.                                  |
| `data-tooltip-html`   | no       | —       | Presence = treat `data-tooltip` as trusted HTML.   |

---

## Usage (Blazor)

Sync `Tooltip.razor` into your app's components (this repo has no build step),
load the CSS/JS once at the app root, then either use the wrapper:

```razor
<Tooltip Text="Project tagline" Position="right">
    <button class="my-btn">MindAttic.Legion</button>
</Tooltip>
```

…or skip the wrapper and put the attribute directly on any element:

```razor
<button class="my-btn" data-tooltip="@tagline" data-tooltip-pos="right">@title</button>
```

No JS interop and no service registration — it works under Interactive Server,
WebAssembly, and static SSR alike.

---

## Theming

Override any CSS variable on `:root` (or a scope) to restyle without touching JS:

```css
:root {
  --ma-tooltip-bg: #161b22;
  --ma-tooltip-fg: #e6edf3;
  --ma-tooltip-border: #dc3545;
  --ma-tooltip-maxw: 280px;
  --ma-tooltip-font: 'Outfit', system-ui, sans-serif;
}
```

---

## Accessibility

- Shows on **hover and keyboard focus** (`focusin`/`focusout`).
- Sets `role="tooltip"` and links the active trigger via `aria-describedby`.
- **Esc** dismisses; re-positions on scroll/resize so it tracks the trigger.
- Honors `prefers-reduced-motion`.

---

## Notes

- Idempotent: loading the script twice is a no-op (`window.__maTooltip` guard).
- The floating node is appended to `<body>` with a very high `z-index` and
  `pointer-events: none`, so it never steals hover or sits under app chrome.
