# ThemeToggle

A sun/moon switch between the `dark` and `light` themes. One `<button>`, one script.

```html
<button class="ma-theme-toggle"></button>
<link rel="stylesheet" href=".../components/ThemeToggle/theme-toggle.css">
<script src=".../components/ThemeToggle/theme-toggle.js"></script>
```

`theme-toggle.js` fills the button with the two icons, wires the click, sets `data-theme` on
`<html>`, and remembers the choice in `localStorage` under `ma-theme`. The sun shows while the dark
theme is active (click it for light); the moon shows while light is active.

To avoid a flash of the wrong theme on load, apply the saved choice before the body paints:

```html
<script>
  try {
    var t = localStorage.getItem('ma-theme');
    if (!t) t = matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark';
    document.documentElement.setAttribute('data-theme', t);
  } catch (e) {}
</script>
```

| API | What |
|---|---|
| `MindAtticThemeToggle.init()` | Wire every `.ma-theme-toggle` on the page; idempotent, runs itself on DOM ready |
| `MindAtticThemeToggle.set(theme[, persist])` | Force `'dark'` or `'light'`; `persist=false` skips saving |
| `MindAtticThemeToggle.toggle()` | Flip the current theme |
| `MindAtticThemeToggle.initialTheme()` | The theme that would be used first (saved → OS → dark) |

Fires a `ma-theme-change` event on `document` (`e.detail.theme`) after every change, so a page can
re-draw a canvas or chart. Resolution order with no saved choice: OS `prefers-color-scheme`, else
dark. Follows the OS live only until the user picks a side.

Pairs with the `Dark` and `Light` themes in `web/themes/`. Consumers: RusNet (its report toolbar).
