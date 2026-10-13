# Hyperspace

The standard library of **159 higher-dimensional shapes** (the exhibits of the
[Hyperspace](https://github.com/mindattic/Hyperspace) page) and the **Hyperspace Reader**, a Cyberspace
scanner window that locks on to one of them and fails to make sense of it.

`hyperspace.js` is pure data and maths: each shape is a frozen record plus a geometry generator, and
projection is a pure function of `(shape, t)`. A Three.js scene and a 2D canvas draw from the same
calls. `hyperspace-reader.js` is the only part that touches the DOM.

---

## Layout

```
Hyperspace/
├── hyperspace.js          # the shape library (UMD: browser <script> AND node require())
├── hyperspace-reader.js   # the Hyperspace Reader window (UMD, injects its own <style id="hsr-style">)
├── index.htm              # QA harness: a live grid of every shape; click one to scan it
└── Hyperspace.md
```

No build step and no CSS file: load the scripts from jsDelivr like SacredGeometry.

```html
<script src="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Web@V12/MindAttic.Web.Shared/Components/Hyperspace/hyperspace.js"></script>
<script src="https://cdn.jsdelivr.net/gh/mindattic/MindAttic.Web@V12/MindAttic.Web.Shared/Components/Hyperspace/hyperspace-reader.js"></script>
```

`hyperspace.js` must load before anything that uses `window.Hyperspace`. The Reader looks the library
up when `show()` is called and returns `null` if it is missing. The Hyperspace page loads the library
first from the CDN, with a fallback to `../MindAttic.Web.Shared/` (the same MindAttic.Web checkout) for local development.

---

## Shape record

`Hyperspace.shapes` is a frozen array in gallery order (index 0 to 158; `row`/`col` place it in the
hall, ten exhibits to a row, sixteen rows).

| Field | Type | Meaning |
|---|---|---|
| `id` | string | Stable kebab-case id derived once from the name (`5-cell`, `tesseract`, `e8-polytope-421`, `boys-surface`) and written into the source, so it does not change if a name is edited. |
| `index` | number | Position in the catalog and the gallery. |
| `name`, `tag` | string | Display name and classification line from the plaque. |
| `family` | string | Category key: `reg`, `unif`, `five`, `ncube`, `curve`, `duo`, `rot`, `phys`, `prism`, `star`, `comb`. |
| `familyLabel`, `color` | string | The family's label and colour (see Palette). |
| `dim` | number | The native dimension the plaque states. |
| `schlafli` | string | The plaque's symbol (a Schläfli symbol for polytopes, otherwise the notation the page uses, such as `S³` or `K²`). |
| `symmetry` | string or null | The plaque's `Symmetry` fact when it states one. |
| `stated` | object | The plaque's counts, where it gives them: `vertices`, `edges`, `faces`, `cells` (numbers or strings, exactly as written). |
| `facts` | object | Every plaque fact, verbatim (keys use `_` for spaces). |
| `rot` | `[i, j, speed, offset][]` | The rotation planes the exhibit animates: angle `t * speed + offset` in the `(i, j)` plane, `t` in seconds. |
| `blurb`, `article` | string | Plaque text and article (HTML). |
| `refs`, `cites` | string[] | Reference keys and the resolved APA-7 citations (HTML). |
| `row`, `col` | number | Gallery coordinates. |

The plaque states what the object is; the wireframe is what the page draws. Frames too large to animate
whole are
cut down and say so in a `Frame` fact: 2₄₁ draws a sample of 6000 edges, 1₄₂ the 953 vertices nearest one
pole, the lattices their kissing configuration (Barnes–Wall around one contact). `stats(id)` reports what
is actually drawn.

The other constructed exhibits draw their true frames, and the spec checks their counts against the
plaque. Uniform polytopes and the E₆/E₇/E₈ Gosset polytopes come from `wythoff`: a seed point reflected
through the mirrors of a Coxeter diagram (`gen.diagrams`), at distance 1 from every ringed mirror.
The star polychora share the 600-cell's or the 120-cell's vertices; `starPolychoron` picks the chord
class whose neighbours form the vertex figure. Quasicrystals store each point as
`[physical..., internal...]`, so the projection drops internal space first.

## Palette

The eleven family colours were solved as one set against the gallery floor (`#06061A`). Every pair is at
least 15 OKLab ΔE×100 apart for normal vision and 8 under simulated protanopia and deuteranopia
(Machado 2009). Every colour has OKLCH lightness 0.48 to 0.67, chroma of at least 0.10 and at least 3:1
contrast with the floor. The spec re-checks the pair distances. Yellow and mint cannot reach that
lightness band, so `duo` is a bronze and `phys` a mauve.

---

## API

```js
Hyperspace.count                 // 159
Hyperspace.shapes                // frozen records (above)
Hyperspace.ids()                 // ['5-cell', 'tesseract', ...]
Hyperspace.get(idOrIndex)        // record; RangeError if unknown
Hyperspace.has(id)               // boolean
Hyperspace.families              // { reg: { key, label, color }, ... }
Hyperspace.refs                  // { coxeter: '<citation html>', ... }

Hyperspace.geometry(id)          // { verts, edges, embedDim, dropped }, cached; do not mutate
Hyperspace.stats(id)             // { vertices, edges, embedDim } of the drawn wireframe
Hyperspace.pose(id, t)           // native-dimension vertices rotated through the exhibit's planes
Hyperspace.extent(id)            // worst-case projected 3D radius over a full rotation sweep (cached)
Hyperspace.project3D(id, t, o)   // [[x,y,z]...], worst-case radius normalised to o.radius (default 1)
Hyperspace.project2D(id, t, o)   // [[x,y,depth]...], y up, the worst-case pose filling the unit disc
Hyperspace.draw(ctx, id, t, o)   // stroke onto a 2D canvas context, depth-faded
Hyperspace.details(id)           // plain-text description lines (used by the Reader)

Hyperspace.rotate(verts, i, j, a)        // rotate in the (i, j) plane (returns a copy)
Hyperspace.projectTo3D(verts, dist=3.2)  // perspective-project nD -> 3D, one axis at a time
Hyperspace.projectDown(verts, toDim, d)  // the same down to any dimension (the page's explorers use 2)
Hyperspace.gen                           // the generators: nCube, nSimplex, nOrthoplex, nDemicube,
                                         // cell24, cell600, duoprism, glome, hopfFibration, wythoff,
                                         // starPolychoron, kissing, elserSloane, hopfHigher, polyprism, ...
```

**Three.js.** The gallery fills a `LineSegments` buffer each frame from
`projectTo3D(Hyperspace.pose(id, t))`, scaled by `TARGET_R / Hyperspace.extent(id)` so the shape fits its
cage at every angle.

**2D canvas.** `project2D` adds the gallery's slow turn about the vertical axis (`yaw = 0.15 t` unless
`o.yaw` is given), a tilt (`o.pitch`, default 0.35 rad) and a perspective divide (`o.focal`, default 3.5),
and scales the result so the worst-case pose fills the unit disc. (`extent` samples the sweep, so a pose
between samples can reach about 5% further.) `draw` options: `size` (box in px, default 110), `color`
(default the family colour), `alpha`, `lineWidth`, `fill` (fraction of the half-size the unit disc
fills, default 0.9), plus the `project2D` options. Edges are bucketed into four depth bands, so a frame
costs four strokes.

**Undefined sample points.** Dini's surface samples `log(tan(a/2))` past `a = π`, where it is undefined;
the page's version drew nothing there. `geometry()` drops such points and the edges that touch them
(`dropped` counts them; 242 for `dini-surface`, 0 for every other shape), so every projected coordinate
is finite and the drawn result is unchanged.

---

## Hyperspace Reader

```js
var h = HyperspaceReader.show({
  x: 120, y: 80,          // px, or any CSS length such as '30%' (relative to host)
  shape: 'tesseract',     // id, index or record; omitted -> random
  host: someElement,      // default document.body (then the window is position: fixed)
  ttl: 8000,              // optional: close automatically after this many ms
  opacity: 0.9,           // optional: sets --hsr-opacity
  reducedMotion: false    // optional: default follows prefers-reduced-motion
});
h.el;        // the window element (data-state: acquiring | live | closing | off; data-threshold)
h.shape;     // the shape record
h.close();   // runs the shutdown; returns a Promise that resolves once the DOM is removed
HyperspaceReader.closeAll();  // Promise
HyperspaceReader.active();    // open readers
HyperspaceReader.READOUTS;    // the instrument pool (101 entries)
HyperspaceReader.FLAGS;       // the flag pool
```

What it does:

1. **Acquire** (about 0.45 s): the window unfolds like a Cyberspace console, the scope shows static and
   a radar sweep, and the readouts are blank.
2. **Lock**: the shape fades in on the scope as a rotating wireframe whose tilt keeps wandering, with a
   faint temporal echo (skipped above 3000 edges) and occasional glitches: a red chromatic ghost, torn
   scan lines and a scrambled object label.
3. **Live**: the Dimensional Threshold bar climbs towards a level that rises with the shape's
   dimension, jitters, and spikes (it can read `OVERFLOW`); the window flashes red at 85% and above.
   Five readouts from the pool show at a time, one is swapped every 1.3 to 2.1 s and settles out of
   scrambled text, and values drift. Shape-derived instruments (`VERTEX ECHO`, `NATIVE AXES`, ...)
   echo the real numbers with sensor error, and about 7% of readings come back as `NaN`, `∞` or `ERR`.
   `UNRESOLVED` plus two more flags appear, and the shape's details scroll past quickly, interleaved
   with scanner log lines.
4. **Shutdown** (`close()`, about 1.1 s): the readouts freeze and grey out, the details collapse, the
   flags drop, the bar drains, the scope object shrinks to a dot, a power-down line types out
   (`> HSR-7A :: NO CONCLUSION. POWERING DOWN`), the window folds to a bright line and vanishes, and the
   DOM is removed.

**Cost.** One `requestAnimationFrame` loop per open reader. The scope redraws at about 30 fps; text
changes are a few `textContent` writes, and the bar moves with `clip-path`. Nothing runs after close.

**Reduced motion.** No rotation, jitter, spikes, glitches, scan noise or scrolling: the scope draws one
still pose, readouts and detail lines change every few seconds, and close is a 0.3 s fade after the
power-down line.

**Styling.** The window uses the Cyberspace palette (teal ink, blue chrome, amber and red alarms) and
the engine's `Courier New` monospace. It has `pointer-events: none` so it never blocks the page.

---

## In Cyberspace

The Cyberspace engine uses the Reader as its READER effect: `consoleBg._demo.spawnHyperspaceReader(origin)`
opens one in `.console-bg-host` on a random shape with a ttl of 6 to 11 s, and the auto-spawner fires it about
2.5% of the time (`FX_READER`, `RATE_READER`). Load `hyperspace.js` and `hyperspace-reader.js` before
`console-bg.js` (deferred, in that order); `sync-mindattic-com.ps1` emits them as deferred jsDelivr tags for
mindattic.com. See [Cyberspace](../Cyberspace/Cyberspace.md).

## Harness

Open `index.htm` (directly or through any static server): every shape animates in a grid (only the
tiles on screen), clicking a tile scans it, and the toolbar spawns readers, closes them, runs an
automatic spawn loop and toggles reduced motion.

## Tests

`tests/specs/components/hyperspace.spec.mjs` (project `components`) checks that every shape loads and
projects to finite 2D coordinates, that the Reader opens, animates, shows the threshold bar and the
scrolling details and removes its DOM on close, and that the Hyperspace page renders from the library.

## Editing

Edit `hyperspace.js` here (the Hyperspace page reads it; never copy it back into the page). Keep ids
stable: they are how consumers address shapes. Ship through the next whole-number tag and repin the
Hyperspace page.

Adding shapes: append records to `RAW` under a `// ===== ROW n` comment (declaration order is gallery
order; the hall grows a row per ten shapes on its own). Then update `COUNT` in `hyperspace.spec.mjs`, and
the count in the Hyperspace page's gallery intro, `Hyperspace/README.md`, the MindAttic.Web.Shared README,
`docs/BIBLE.md`, `docs/USER_STORIES.md`, `docs/data/components.json` and the repo README. A new family
needs a colour that keeps the Palette guarantees; the spec checks them.
