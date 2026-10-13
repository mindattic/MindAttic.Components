# ThemeFx

The engine the theme effects (`web/themes/*/<name>.js`) draw with. It owns the one full-screen
`<canvas id="fx-canvas">` behind the page and gives every theme the same services:

- `canvas`, `ctx` - the canvas and its 2D context. `canvas.width` / `canvas.height` read and write the
  logical (CSS-pixel) size; the backing store follows the adaptive quality level.
- Adaptive quality - an FPS governor samples running effects; two slow seconds (< 45 fps) drop the
  render resolution a step (1, 0.75, 0.5, 0.35), long smooth stretches probe back up. The level is
  remembered in `localStorage` (`fx-quality-level`).
- `particles` - scratch array cleared on every theme change.
- `stopFX()` - cancels the running animation (`fxAnim`), clears the canvas and resets the side effects
  themes leave behind (Cyberpunk's border flicker, Winter's snow pile, Neko's cats).
- `bgSet(theme, count)` / `pickUnusedIdx(...)` - the random-background helper Sakura and Sunset use:
  every background shown once per cycle before any repeats, only the chosen one downloaded.

Like the effects, it is written to sit inside the host page's script (see `web/themes/README.md` for
what the host provides). Consumers: ryandebraal.com.
