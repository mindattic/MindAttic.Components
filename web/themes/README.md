# Themes

Whole-page themes, one folder each. A theme is a palette plus anything else that changes with it:

| File | What it is |
|---|---|
| `<name>.css` | `[data-theme="<name>"]` custom-property palette (`--bg`, `--bg2`, `--bg3`, `--border`, `--accent`, `--accent2`, `--text`, `--text2`, `--text3`, `--tag-*`, `--rule`), then the theme's own rules (page background, link underline tint, glow, keyframes). `dark` is the default, so its palette is the `:root` block. |
| `<name>.js` | The theme's animated effect, if it has one (`start<Name>()`), drawn on the shared `#fx-canvas` by the ThemeFx engine (`components/ThemeFx`). |
| images | Assets the theme loads (Noir's moon, Sakura's and Sunset's 20 backgrounds), referenced through `{{THEMES}}`, which the build fills with the pinned jsDelivr URL. |

| Theme | Effect |
|---|---|
| Dark, Light | palette only |
| Spring | growing grass and drifting pollen |
| Summer | bees claiming flowers |
| Autumn | leaves with wind and tumble |
| Winter | snowflakes and an accumulating snow pile |
| Matrix | green column rain |
| Neko | ten autonomous desktop-pet cats (sprites in `neko.css`) |
| Ocean | rising bubbles and caustic light |
| Sunset | one of 20 pointillist backgrounds |
| Forest | parallax pine forest, stars, mist |
| Cyberpunk | neon skyline, acid rain, flickering tags |
| Noir | moon arc and star field (CSS only) |
| Sakura | blossoms over one of 20 backgrounds |
| Sand | sandstorm, vortices, growing dunes |
| Synthwave | perspective grid, wireframe mountains, retro sun |

## Using them

The CSS works anywhere: load the theme files, put `data-theme="<name>"` on `<html>`, and style with
the variables.

The effect scripts are written to live inside the host page's own script, sharing its scope (they are
spliced in by `build/build-sites.mjs` as a `raw` block). The host provides, in that scope:

- `let fxAnim` - effects store their `requestAnimationFrame` id here; `stopFX()` cancels it.
- `AppSettings` - tunables the effects read: `flower_max_size`, `flower_min_size`, `leaf_count`,
  `neko_fps`, `neko_scratch_time`, `neko_speed`, `neko_stop_time`, `neko_wash_time`,
  `plant_grow_fast`, `plant_grow_slow`, `snow_flake_count`, `snow_max_height`, `snow_pile_rate_max`,
  `snow_pile_rate_min`, `snow_smooth_speed`, `snow_spread`.
- `<canvas id="fx-canvas">`, fixed full-screen behind the content; `#resume` (the content card the
  Cyberpunk border flicker lights) and `#neko-btn` (Neko's toolbar colour button) when present.

and switches themes by setting `data-theme` and calling `stopFX()` then the theme's `start<Name>()`.

Consumers: ryandebraal.com (all sixteen). The themes came out of that page byte-for-byte; its parity
run in `tests/parity` switches through every one of them.
