// ╔═══════════════════════════════════════════════════════════════════════════════╗
// ║  SAND THEME                                                                   ║
// ║  Sandstorm with swirling vortices, 1800 grit particles, and growing dunes     ║
// ╠═══════════════════════════════════════════════════════════════════════════════╣
// ║  Three systems working together:                                              ║
// ║                                                                               ║
// ║  Grit particles (1800): each particle has position, velocity, mass, and       ║
// ║  alpha.  Every frame: wind force is applied, 3 vortices exert rotational      ║
// ║  pull, gravity pulls down.  Particles are pre-warmed before the first frame   ║
// ║  (physics run 180 ticks with no draw) so the scene is already a full storm    ║
// ║  on load rather than building up from nothing.                                ║
// ║                                                                               ║
// ║  Wind model: getWind() returns tanh(4.2 × sin(t × 0.00072)) × 10.             ║
// ║  tanh of a slow sine spends most of its time at ±1 — long sustained gusts     ║
// ║  with sharp transitions through zero.  A gust + turbulence term adds          ║
// ║  moment-to-moment variation on top of the direction driver.                   ║
// ║                                                                               ║
// ║  Vortices (3): each has an orbital strength and wanders slowly across the     ║
// ║  screen via independent sine offsets.  Particles within range are deflected   ║
// ║  tangentially — produces the dust-devil spirals.                              ║
// ║                                                                               ║
// ║  Dunes: 90-column height array, grows at SAND_PILE units/frame, smoothed      ║
// ║  each frame (8 passes, aggressive neighbour weights).  Wind drifts the dune   ║
// ║  profile sideways over time, carving natural crescent shapes.  Drawn as a     ║
// ║  smooth bezier polyline with a sandy fill gradient.                           ║
// ╚═══════════════════════════════════════════════════════════════════════════════╝
// Particles are pre-warmed (physics run before first frame) so the scene is
// already chaotic on load.  Dunes grow continuously at SAND_PILE = 1 unit/frame
// per column, smoothed each frame for flowing bezier silhouettes.  Wind drifts
// the dune profile sideways over time, carving natural crescent shapes.
function startSand() {
  const W = () => canvas.width, H = () => canvas.height;
  const GRIT_N    = 1800;
  const SAND_PILE = 1.5;
  const DUNE_COLS = 90;   // horizontal sample columns for dune profile
  let t = 0;

  function getWind() {
    // tanh of a slow sine spends most of its time at ±1 and transitions
    // sharply through zero — gives long strong-direction runs with brief shifts.
    const direction = Math.tanh(4.2 * Math.sin(t * 0.00072)) * 10.0;
    const gust      = Math.sin(t * 0.0021 + 1.7) * 2.8;
    const turb      = Math.sin(t * 0.0089 + 3.3) * 1.1;
    return direction + gust + turb;
  }

  const vortices = Array.from({length: 3}, () => ({
    ox: Math.random(), oy: 0.2 + Math.random() * 0.6,
    px: Math.random() * 200, py: Math.random() * 200,
    str: (Math.random() < 0.5 ? 1 : -1) * (18000 + Math.random() * 22000),
    wanderX: 0.08 + Math.random() * 0.12, wanderY: 0.10 + Math.random() * 0.14,
    spdX: 0.00031 + Math.random() * 0.00042, spdY: 0.00028 + Math.random() * 0.00038,
  }));

  function stepGrit(g, w, h, wind) {
    vortices.forEach(v => {
      const dx = g.x - v.x, dy = g.y - v.y;
      const d2 = dx*dx + dy*dy + 2500;
      const fac = v.str / (d2 * Math.sqrt(d2) * 0.001 + 1) / d2;
      g.vx += -dy * fac * 0.00004;
      g.vy +=  dx * fac * 0.00004;
    });
    // Floor of 3.5 means particles never stall during wind-direction transitions
    g.vx += (Math.abs(wind) + 3.5) * 0.018 * g.windSign;
    g.vy += Math.sin(t * 0.031 + g.x * 0.008) * 0.55
           + Math.sin(t * 0.071 + g.y * 0.006) * 0.38
           + Math.sin(t * 0.173 + g.x * 0.014) * 0.22;  // third harmonic for extra chaos
    const spd = Math.sqrt(g.vx*g.vx + g.vy*g.vy);
    if (spd > 20) { const s = 20/spd; g.vx *= s; g.vy *= s; }
    // Minimum speed — no particle ever drifts to a visible standstill
    else if (spd > 0 && spd < 2.0) { const s = 2.0/spd; g.vx *= s; g.vy *= s; }
    g.x += g.vx; g.y += g.vy;
    if (g.x >  w+30) g.x = -30;
    if (g.x < -30)   g.x =  w+30;
    if (g.y >  h+20) g.y = -20;
    if (g.y < -20)   g.y =  h+20;
  }

  // Two crosswind streams: first half spawn left→right (windSign +1),
  // second half spawn right→left (windSign -1). Storm intensity drives both;
  // direction is fixed per stream so they visibly cross mid-screen.
  const w0 = W(), h0 = H();
  const grits = Array.from({length: GRIT_N}, (_, idx) => {
    const windSign = idx < GRIT_N / 2 ? 1 : -1;
    const switcher = Math.random() < 0.25;
    return {
      x:   windSign === 1 ? Math.random() * w0 * 0.3 : w0 * 0.7 + Math.random() * w0 * 0.3,
      y:   Math.random() * h0,
      vx:  (1.5 + Math.random() * 3) * windSign,
      vy:  (Math.random() - 0.5) * 4,
      r:   0.4 + Math.random() * 1.4, op: 0.25 + Math.random() * 0.55,
      len: 3 + Math.random() * 9,
      layer:    Math.ceil(Math.random() * 3),
      windSign,
      switcher,
      switchAt: switcher ? Math.floor(Math.random() * 900) : Infinity,
    };
  });

  // Pre-warm: run 350 frames of physics before first draw
  for (let i = 0; i < 350; i++) {
    t++;
    const wind = getWind();
    vortices.forEach(v => {
      v.x = (v.ox + Math.sin(t * v.spdX + v.px) * v.wanderX) * w0;
      v.y = (v.oy + Math.sin(t * v.spdY + v.py) * v.wanderY) * h0;
    });
    grits.forEach(g => stepGrit(g, w0, h0, wind));
  }

  // Three separate dunes — back accumulates fastest (distance effect),
  // each has its own ground baseline so they sit at different screen heights.
  // baseYFrac: fraction of h where the dune "ground" sits.
  // maxFrac:   max dune height above that baseline.
  // drizzle:   background growth rate (back dunes drizzle fastest).
  // pileScale: multiplier on SAND_PILE impact deposit.
  const dunes = [
    { h: new Float32Array(DUNE_COLS), acc: new Float32Array(DUNE_COLS), baseYFrac: 1.00, maxFrac: 0.38, drizzle: 0.12, pileScale: 1.40 },
    { h: new Float32Array(DUNE_COLS), acc: new Float32Array(DUNE_COLS), baseYFrac: 1.00, maxFrac: 0.24, drizzle: 0.07, pileScale: 1.00 },
    { h: new Float32Array(DUNE_COLS), acc: new Float32Array(DUNE_COLS), baseYFrac: 1.00, maxFrac: 0.14, drizzle: 0.04, pileScale: 0.70 },
  ];

  function updateDunes(w, h, wind) {
    dunes.forEach(D => {
      const maxH  = h * D.maxFrac;
      const baseY = h * D.baseYFrac;
      for (let i = 0; i < DUNE_COLS; i++) {
        D.acc[i] += SAND_PILE * D.drizzle * (0.5 + Math.random());
        if (D.acc[i] >= 1) { D.h[i] += Math.floor(D.acc[i]); D.acc[i] %= 1; }
        D.h[i] = Math.min(D.h[i], maxH);
      }
      // Smooth × 8 — heavy passes flatten spikes into broad rolling dunes
      for (let pass = 0; pass < 8; pass++) {
        for (let i = 1; i < DUNE_COLS - 1; i++)
          D.h[i] = D.h[i] * 0.40 + (D.h[i-1] + D.h[i+1]) * 0.30;
      }
      // Wind drift
      if (Math.abs(wind) > 0.8 && t % 3 === 0) {
        const leak = Math.min(0.06, Math.abs(wind) * 0.007);
        if (wind > 0) {
          for (let i = DUNE_COLS - 1; i > 0; i--)
            D.h[i] = D.h[i] * (1 - leak) + D.h[i-1] * leak;
        } else {
          for (let i = 0; i < DUNE_COLS - 1; i++)
            D.h[i] = D.h[i] * (1 - leak) + D.h[i+1] * leak;
        }
      }
    });
  }

  function drawDuneLayer(heights, w, baseY, grad) {
    const n = heights.length;
    const pts = Array.from({length: n}, (_, i) => ({
      x: (i / (n - 1)) * w,
      y: baseY - heights[i],
    }));
    ctx.beginPath();
    ctx.moveTo(-10, baseY + 4);
    ctx.lineTo(pts[0].x, pts[0].y);
    for (let i = 0; i < n - 1; i++) {
      const mx = (pts[i].x + pts[i+1].x) * 0.5;
      const my = (pts[i].y + pts[i+1].y) * 0.5;
      ctx.quadraticCurveTo(pts[i].x, pts[i].y, mx, my);
    }
    ctx.lineTo(pts[n-1].x, pts[n-1].y);
    ctx.lineTo(w + 10, baseY + 4);
    ctx.closePath();
    ctx.fillStyle = grad;
    ctx.fill();
  }

  function draw() {
    const w = W(), h = H();
    ctx.clearRect(0, 0, w, h);
    t++;

    const wind    = getWind();
    const windAbs = Math.abs(wind);

    vortices.forEach(v => {
      v.x = (v.ox + Math.sin(t * v.spdX + v.px) * v.wanderX) * w;
      v.y = (v.oy + Math.sin(t * v.spdY + v.py) * v.wanderY) * h;
    });

    // Dunes (drawn behind grit) — back → mid → front, each at its own baseline
    updateDunes(w, h, wind);
    const duneColors = [
      ['rgba(228,205,162,0.84)', 'rgba(212,185,138,0.92)'],
      ['rgba(205,175,128,0.88)', 'rgba(188,158,108,0.95)'],
      ['rgba(178,148,100,0.91)', 'rgba(158,128,78,0.98)'],
    ];
    dunes.forEach((D, di) => {
      const baseY = h * D.baseYFrac;
      const g2 = ctx.createLinearGradient(0, baseY - h * D.maxFrac, 0, baseY);
      g2.addColorStop(0, duneColors[di][0]);
      g2.addColorStop(1, duneColors[di][1]);
      drawDuneLayer(D.h, w, baseY, g2);
    });

    // Grit particles — each hits only its assigned dune layer
    grits.forEach(g => {
      stepGrit(g, w, h, wind);

      // Switchers: reassign to a different layer every 5–15 s (300–900 frames)
      if (t >= g.switchAt) {
        let nl; do { nl = Math.ceil(Math.random() * 3); } while (nl === g.layer);
        g.layer   = nl;
        g.switchAt = t + 300 + Math.floor(Math.random() * 600);
      }

      const xi   = Math.max(0, Math.min(DUNE_COLS - 1, Math.floor(g.x / w * DUNE_COLS)));
      const D    = dunes[g.layer - 1];
      const surf = h * D.baseYFrac - D.h[xi];
      if (g.y >= surf) {
        D.h[xi] = Math.min(D.h[xi] + SAND_PILE * D.pileScale * (g.switcher ? 3 : 1), h * D.maxFrac);
        g.x  = Math.random() * w; g.y = Math.random() * h * 0.55;
        g.vx = (1.5 + Math.random() * 3) * g.windSign;
        g.vy = (Math.random() - 0.5) * 4;
        return;
      }

      const spd   = Math.sqrt(g.vx*g.vx + g.vy*g.vy);
      const alpha = Math.min(0.90, g.op * (0.4 + windAbs * 0.09));
      if (alpha < 0.03) return;
      const ang       = Math.atan2(g.vy, g.vx);
      const streakLen = g.len * (0.5 + spd * 0.18);
      const ex = g.x - Math.cos(ang) * streakLen;
      const ey = g.y - Math.sin(ang) * streakLen;
      const gr = ctx.createLinearGradient(ex, ey, g.x, g.y);
      const rb = 175 + Math.random() * 25 | 0;
      const gb = 150 + Math.random() * 22 | 0;
      const bb = 95  + Math.random() * 22 | 0;
      gr.addColorStop(0, `rgba(${rb},${gb},${bb},0)`);
      gr.addColorStop(1, `rgba(${rb},${gb},${bb},${alpha.toFixed(3)})`);
      ctx.save();
      ctx.lineWidth = g.r; ctx.strokeStyle = gr;
      ctx.beginPath(); ctx.moveTo(ex, ey); ctx.lineTo(g.x, g.y); ctx.stroke();
      ctx.restore();
    });

    // Atmospheric haze
    const hazeA = Math.min(0.22, windAbs * 0.025).toFixed(3);
    const haze  = ctx.createLinearGradient(0, 0, 0, h);
    haze.addColorStop(0,   `rgba(215,190,145,0)`);
    haze.addColorStop(0.5, `rgba(215,190,145,${hazeA})`);
    haze.addColorStop(1,   `rgba(198,170,118,${hazeA})`);
    ctx.fillStyle = haze;
    ctx.fillRect(0, 0, w, h);

    fxAnim = requestAnimationFrame(draw);
  }
  draw();
}

