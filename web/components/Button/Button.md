# Button

The base MindAttic button: `class="ma-btn"` on an `<a>` or `<button>`.

```html
<a class="ma-btn" href="https://example.com">Label</a>
```

| Variable | Default | |
|---|---|---|
| `--ma-btn-u` | `1rem` | Size unit: font size, padding, border and radius all scale from it |
| `--ma-btn-fg` | `#fff` | Label, in every link state |
| `--ma-btn-bg` / `--ma-btn-border` | `#13161b` / `#e5484d` | Rest |
| `--ma-btn-hover-bg` / `--ma-btn-hover-border` | `#e5484d` / `#ff6b70` | Hover and keyboard focus |

Rest-to-hover changes ease over 0.28s, including `box-shadow` and `transform`, so a site can add a
glow or a lift in its own CSS and get the same easing:

```css
.link-btn { --ma-btn-u: 1.4rem; }
.link-btn:hover, .link-btn:focus-visible { transform: translateY(-0.2rem); }
```

Consumers: mindattic.com (its three link buttons add a glow and a small rise on hover).
