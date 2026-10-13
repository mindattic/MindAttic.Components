// ╔═══════════════════════════════════════════════════════════════════════════════╗
// ║  § 5  CANVAS FX                                                               ║
// ║  11 animated themes — all vanilla Canvas 2D, no libraries, no WebGL           ║
// ╠═══════════════════════════════════════════════════════════════════════════════╣
// ║  Themes with canvas animations:                                               ║
// ║    Spring ...... growing grass blades + pollen drift                          ║
// ║    Summer ...... bees with flower-claiming state machine                      ║
// ║    Autumn ...... leaves with wind physics and tumble                          ║
// ║    Winter ...... snowflakes + accumulating SVG snow pile                      ║
// ║    Neko ........ single desktop pet — 1998 state machine, 32 base64 sprites   ║
// ║    Neko Theme .. 10 autonomous cats, independent AI                           ║
// ║    Matrix ...... green column rain — classic dimming-trail trick              ║
// ║    Forest ...... 6-layer parallax pine forest, stars, mist, shooting stars    ║
// ║    Ocean ....... rising bubbles + caustic light shafts                        ║
// ║    Cyberpunk ... neon city skyline, acid rain, arcs, sparks, CRT scanlines    ║
// ║    Synthwave ... perspective grid, wireframe mountains, sliced retro sun      ║
// ║    Sand ........ sandstorm, 3 vortices, 1800 grit particles, growing dunes    ║
// ║                                                                               ║
// ║  One fullscreen <canvas id="fx-canvas"> fixed at z-index 0 behind the         ║
// ║  resume card.  Each theme owns it exclusively — stopFX() cancels the          ║
// ║  rAF loop, clears the canvas, and nulls all particle arrays before the        ║
// ║  next theme starts.  No particle pool shared across themes.                   ║
// ╚═══════════════════════════════════════════════════════════════════════════════╝
//
// Architecture: one fullscreen <canvas> fixed behind the resume (#fx-canvas,
// z-index 0). Each theme owns it exclusively — no layering, no compositing.
// The canvas is transparent by default; themes that need a background (Cyberpunk,
// Synthwave) clear it themselves each frame.
//
// Each FX system is a self-contained function (startSpring, startSummer, etc.)
// that closes over its own state. There's no shared particle pool — each system
// allocates what it needs and the GC cleans up on stopFX().
//
// One alternative worth thinking about: Web Workers + OffscreenCanvas would move
// the animation loop off the main thread entirely, preventing any frame jank from
// DOM work. The tradeoff is that workers can't touch the DOM, so things like the
// neko sprite and border glow updates would need a message-passing bridge.
//
// Another: a WebGL renderer (Three.js or raw shaders) would handle hundreds of
// thousands of particles at full framerate. For the current particle counts
// (≤400 rain drops, ≤200 snowflakes) the 2D canvas API is fine — WebGL would
// be meaningful engineering for no visible gain.
//
// CSS animations were considered for simple effects like falling leaves, but
// Canvas wins when you need physics (wind, accumulation, flocking) that CSS
// can't express without JavaScript anyway.
//
// If you were building this, would you separate the animation runtime into a
// module, or keep it inline like this?
const canvas = document.getElementById('fx-canvas');
const ctx = canvas.getContext('2d');
let particles = [];

// ── ADAPTIVE QUALITY ─────────────────────────────────────────────────────────
// Slow machines drop the FX canvas render resolution in steps. canvas.width/height
// are shimmed to report CSS-pixel (logical) size and ctx is pre-scaled, so every
// theme keeps drawing in CSS pixels and needs no changes; CSS stretches the result.
const FX_SCALES = [1, 0.75, 0.5, 0.35];
let fxLevel = 0;
try { fxLevel = Math.min(FX_SCALES.length - 1, Math.max(0, parseInt(localStorage.getItem('fx-quality-level'), 10) || 0)); } catch(e) {}
function fxScale() { return FX_SCALES[fxLevel]; }

const _canvasW = Object.getOwnPropertyDescriptor(HTMLCanvasElement.prototype, 'width');
const _canvasH = Object.getOwnPropertyDescriptor(HTMLCanvasElement.prototype, 'height');
let fxLogicalW = window.innerWidth, fxLogicalH = window.innerHeight;
function applyFxScale() {
  const sc = fxScale();
  _canvasW.set.call(canvas, Math.max(1, Math.round(fxLogicalW * sc)));
  _canvasH.set.call(canvas, Math.max(1, Math.round(fxLogicalH * sc)));
  ctx.setTransform(sc, 0, 0, sc, 0, 0);
}
Object.defineProperty(canvas, 'width',  { configurable: true, get() { return fxLogicalW; }, set(v) { fxLogicalW = v; applyFxScale(); } });
Object.defineProperty(canvas, 'height', { configurable: true, get() { return fxLogicalH; }, set(v) { fxLogicalH = v; applyFxScale(); } });

function resizeCanvas() {
  fxLogicalW = window.innerWidth;
  fxLogicalH = window.innerHeight;
  applyFxScale();
}
window.addEventListener('resize', resizeCanvas);
resizeCanvas();

// FPS governor: sampled once per second while a theme animation is running.
// Two slow seconds (<45fps) step quality down; after 20 smooth seconds it probes
// one step back up; each step down doubles the wait before the next probe.
(function fxGovernor() {
  let frames = 0, windowStart = 0, last = 0, slow = 0, smooth = 0, probeWait = 20;
  function setLevel(lvl) {
    fxLevel = lvl;
    try { localStorage.setItem('fx-quality-level', String(lvl)); } catch(e) {}
    applyFxScale();
    slow = smooth = 0;
  }
  function tick(now) {
    requestAnimationFrame(tick);
    const dt = now - last;
    last = now;
    if (!fxAnim || dt > 250) { frames = 0; windowStart = now; return; }
    frames++;
    if (now - windowStart < 1000) return;
    const fps = frames * 1000 / (now - windowStart);
    frames = 0; windowStart = now;
    if (fps < 45) {
      smooth = 0;
      if (++slow >= 2 && fxLevel < FX_SCALES.length - 1) { setLevel(fxLevel + 1); probeWait = Math.min(probeWait * 2, 320); }
    } else if (fps >= 56) {
      slow = 0;
      if (++smooth >= probeWait && fxLevel > 0) setLevel(fxLevel - 1);
    }
  }
  requestAnimationFrame(tick);
})();

function stopFX() {
  if (fxAnim) { cancelAnimationFrame(fxAnim); fxAnim = null; }
  ctx.clearRect(0,0,canvas.width,canvas.height);
  particles = [];
  stopCyberpunkEffects();
  stopNekoTheme();
  // Reset any inline styles set by cyberpunk border flicker
  const resumeEl = document.getElementById('resume');
  if (resumeEl) { resumeEl.style.boxShadow = ''; resumeEl.style.borderColor = ''; }
  // Clean up snow pile SVG
  const snowSvg = document.getElementById('snow-pile-svg');
  if (snowSvg) snowSvg.remove();
}

const ASSET_BASE = '{{THEMES}}/';
// The theme backgrounds are JPEGs on the CDN, named <theme>-01.jpg … <theme>-NN.jpg.
// Only the one that gets picked is ever downloaded.
function bgSet(theme, count) {
  const urls = [];
  for (let i = 1; i <= count; i++) urls.push(ASSET_BASE + theme.charAt(0).toUpperCase() + theme.slice(1) + '/' + theme + '-' + (i < 10 ? '0' : '') + i + '.jpg');
  return urls;
}
// Pick a random index that hasn't been used in the current cycle.
// When every entry has been used once, the seen array resets so the
// rotation begins again — guaranteeing each background is shown exactly
// once before any repeat.
function pickUnusedIdx(arr, seen, label) {
  if (seen.length >= arr.length) {
    seen.length = 0;
    console.log(`[${label}] cycled through all ${arr.length} — reshuffling`);
  }
  const available = [];
  for (let i = 0; i < arr.length; i++) if (!seen.includes(i)) available.push(i);
  const idx = available[Math.floor(Math.random() * available.length)];
  seen.push(idx);
  return idx;
}

