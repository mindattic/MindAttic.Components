// ╔═══════════════════════════════════════════════════════════════════════════════╗
// ║  FOREST THEME                                                                 ║
// ║  Lonely train ride through a night pine forest — 6 parallax depth layers      ║
// ╠═══════════════════════════════════════════════════════════════════════════════╣
// ║  Rendering order, back to front (painter's algorithm):                        ║
// ║    1. Stars ........ 150 static dots + 30 individually twinkling points       ║
// ║    2. Shooting stars  occasional streaks that fade in/out on random arcs      ║
// ║    3. Rolling hills .. 3 bezier silhouettes drifting at different speeds      ║
// ║    4. Mist bands .... 6 wispy radial blobs, one per depth layer               ║
// ║    5. Tree strips ... 6 parallax layers × 20 cluster types — the forest wall  ║
// ║    6. Ground fog .... bottom gradient fading to solid darkness                ║
// ║                                                                               ║
// ║  drawPineTree() builds one tree silhouette as a compound canvas path: a       ║
// ║  trunk rect + 4-5 triangle tiers with randomised width/height ratios.         ║
// ║  20 pre-baked cluster arrangements each combine 2-5 trees at varied offsets   ║
// ║  and scales.  Each parallax strip stamps those clusters side-by-side so the   ║
// ║  strips tile seamlessly.  Scale is ±30% randomised per stamp.                 ║
// ║                                                                               ║
// ║  Parallax speeds: layer 0 (background) is slowest; layer 5 (foreground) is    ║
// ║  fastest — giving genuine depth perception with no 3D math.                   ║
// ╚═══════════════════════════════════════════════════════════════════════════════╝
// Rendering order, back to front:
//   1. Stars      — 150 static dots + 30 twinkling points; body bg is the night sky
//   2. Hills      — 3 rolling bezier silhouettes drifting slowly; sells depth
//   3. Tree strips — 6 parallax layers, 20 cluster types, 2–3.5× scale, tight packing
//   4. Mid mist   — wispy radial blobs floating between mid-canopy layers
//   5. Ground mist — heavier blobs pooling at the forest floor
//   6. Ground fog  — bottom gradient fading into darkness
//
// drawPineTree builds one silhouette as a compound canvas path (trunk + tier
// triangles). All 20 clusters call it at varied offsets/scales so each strip
// position stamps a unique arrangement. Scale is randomised ±30% per stamp for
// organic variety. Gaps are tight so the strips tessellate into a wall of forest.
//
// Stars are generated once on start/resize and stored; twinkling is driven by
// sin(t * speed + phase) so each star pulses independently at its own rate.
//
// Hills use quadratic-bezier-through-midpoints to generate smooth rolling
// silhouettes. Three layers drift at slightly different speeds (far = slowest).
function startForest() {
  const W = () => canvas.width, H = () => canvas.height;
  let t = 0;

  // ── PINE TREE SILHOUETTE ─────────────────────────────────────────────────
  // Draws a single organic pine silhouette as one closed path.
  // The crown is traced as a continuous outline — up the left side, over the
  // apex, and back down the right — with jagged branch spikes at each level.
  //
  // Each branch level has two anchor points:
  //   notch: a recess cutting back toward the trunk between branches
  //   tip:   a spike projecting outward (can exceed the taper envelope)
  //
  // Both notch and tip positions carry small Math.random() offsets baked in at
  // strip-build time (never recalculated per frame), so every tree is uniquely
  // shaped but stable. `tiers` controls branch density: tiers*3+2 levels.
  function drawPineTree(c, cx, base, h, w, tiers) {
    const trunkW  = Math.max(1.5, w * 0.08);
    const trunkH  = h * 0.13;
    const crownH  = h - trunkH;
    const crownBase = base - trunkH;
    const n = tiers * 3 + 2; // branch levels — more tiers = more jagged detail

    // Trunk (separate subpath, filled with the same fill call)
    c.rect(cx - trunkW / 2, crownBase, trunkW, trunkH);

    // Build branch profile once (right-side x values; mirrored for left side).
    // Working bottom-to-top so index 0 = lowest branches.
    const pts = [];
    for (let i = 0; i < n; i++) {
      const t    = i / n;                        // 0 = bottom, 1 = near-apex
      const env  = (w / 2) * Math.pow(1 - t, 0.52);  // convex taper — wide spreading base, narrows to apex
      const dy   = crownH / n;                   // height span of one level

      // Notch: recess between this branch and the one below it.
      // x sits at 35–55% of the envelope; y is slightly above the level base.
      const notchX = env * (0.35 + Math.random() * 0.20);
      const notchY = crownBase - crownH * t + dy * (0.10 + Math.random() * 0.18);

      // Tip: branch end, can poke 0–28% beyond the taper envelope.
      // y is slightly above the level base for a natural droop.
      const tipX = env * (0.78 + Math.random() * 0.28);
      const tipY = crownBase - crownH * t - dy * (0.04 + Math.random() * 0.22);

      pts.push({ notchX, notchY, tipX, tipY });
    }

    // Trace the crown outline: left side going up → apex → right side going down.
    c.moveTo(cx - trunkW / 2, crownBase);
    for (const p of pts) {
      c.lineTo(cx - p.notchX, p.notchY); // notch (left, going up)
      c.lineTo(cx - p.tipX,   p.tipY);   // spike tip (left, pointing outward)
    }
    c.lineTo(cx, base - h);               // apex
    for (let i = pts.length - 1; i >= 0; i--) {
      const p = pts[i];
      c.lineTo(cx + p.tipX,   p.tipY);   // spike tip (right, pointing outward)
      c.lineTo(cx + p.notchX, p.notchY); // notch (right, going down)
    }
    c.lineTo(cx + trunkW / 2, crownBase);
    c.closePath();
  }

  // ── 20 CLUSTER ARRANGEMENTS ──────────────────────────────────────────────
  const CLUSTERS = [
    // 0 — twin tall pines
    (c,cx,b,s)=>{
      drawPineTree(c,cx-s*12,b,      s*80,s*30,4);
      drawPineTree(c,cx+s*10,b-s*5,  s*70,s*26,4);
    },
    // 1 — dense thicket (4 shorter)
    (c,cx,b,s)=>{
      drawPineTree(c,cx-s*22,b,      s*50,s*22,3);
      drawPineTree(c,cx-s*8, b,      s*62,s*26,4);
      drawPineTree(c,cx+s*8, b-s*3,  s*55,s*24,3);
      drawPineTree(c,cx+s*24,b,      s*48,s*20,3);
    },
    // 2 — lone sentinel + 2 small flankers
    (c,cx,b,s)=>{
      drawPineTree(c,cx,      b,     s*95,s*32,5);
      drawPineTree(c,cx-s*20, b,     s*44,s*18,3);
      drawPineTree(c,cx+s*22, b,     s*40,s*16,3);
    },
    // 3 — scattered grove (3 spaced)
    (c,cx,b,s)=>{
      drawPineTree(c,cx-s*28,b,      s*65,s*24,4);
      drawPineTree(c,cx,     b-s*4,  s*75,s*28,4);
      drawPineTree(c,cx+s*30,b,      s*58,s*22,3);
    },
    // 4 — ridge line (5 graduated heights)
    (c,cx,b,s)=>{
      [[-32,45,18],[-16,58,22],[0,72,28],[16,60,22],[30,42,17]]
        .forEach(([dx,h,w])=>drawPineTree(c,cx+s*dx,b,s*h,s*w,3));
    },
    // 5 — tall cathedral (3 tight, tall, 5-tier)
    (c,cx,b,s)=>{
      drawPineTree(c,cx-s*14,b-s*4,  s*88,s*24,5);
      drawPineTree(c,cx,     b,      s*100,s*26,5);
      drawPineTree(c,cx+s*15,b-s*6,  s*82,s*23,5);
    },
    // 6 — spire pair (2 extremely tall narrow 6-tier)
    (c,cx,b,s)=>{
      drawPineTree(c,cx-s*8, b,      s*110,s*20,6);
      drawPineTree(c,cx+s*9, b-s*8,  s*98, s*18,6);
    },
    // 7 — wide canopy row (4 medium, broad spread)
    (c,cx,b,s)=>{
      drawPineTree(c,cx-s*36,b,      s*58,s*28,3);
      drawPineTree(c,cx-s*12,b-s*2,  s*66,s*30,3);
      drawPineTree(c,cx+s*14,b,      s*62,s*28,3);
      drawPineTree(c,cx+s*38,b-s*3,  s*54,s*26,3);
    },
    // 8 — staggered ascent (4 trees stepping up left→right)
    (c,cx,b,s)=>{
      drawPineTree(c,cx-s*30,b,      s*45,s*20,3);
      drawPineTree(c,cx-s*10,b-s*2,  s*60,s*22,3);
      drawPineTree(c,cx+s*10,b-s*5,  s*74,s*25,4);
      drawPineTree(c,cx+s*28,b-s*9,  s*88,s*28,4);
    },
    // 9 — mega solo (one enormous 6-tier pine)
    (c,cx,b,s)=>{
      drawPineTree(c,cx,b,           s*118,s*40,6);
    },
    // 10 — triple medium (3 even height, evenly spaced)
    (c,cx,b,s)=>{
      drawPineTree(c,cx-s*24,b,      s*70,s*26,4);
      drawPineTree(c,cx,     b,      s*70,s*26,4);
      drawPineTree(c,cx+s*24,b,      s*70,s*26,4);
    },
    // 11 — foreground wall (5 large, tight)
    (c,cx,b,s)=>{
      [[-38,72,28],[-19,82,30],[0,90,32],[19,78,28],[38,68,26]]
        .forEach(([dx,h,w])=>drawPineTree(c,cx+s*dx,b,s*h,s*w,4));
    },
    // 12 — asymmetric (1 very tall + 2 short off to the right)
    (c,cx,b,s)=>{
      drawPineTree(c,cx-s*10,b,      s*95,s*34,5);
      drawPineTree(c,cx+s*18,b,      s*38,s*16,2);
      drawPineTree(c,cx+s*30,b-s*3,  s*32,s*14,2);
    },
    // 13 — deep forest (6 overlapping trees)
    (c,cx,b,s)=>{
      drawPineTree(c,cx-s*30,b,      s*55,s*22,3);
      drawPineTree(c,cx-s*14,b-s*3,  s*72,s*26,4);
      drawPineTree(c,cx+s*2, b,      s*68,s*24,4);
      drawPineTree(c,cx+s*18,b-s*5,  s*80,s*28,4);
      drawPineTree(c,cx-s*4, b-s*8,  s*58,s*22,3);
      drawPineTree(c,cx+s*34,b,      s*50,s*20,3);
    },
    // 14 — V-formation (2 pines diverging outward)
    (c,cx,b,s)=>{
      drawPineTree(c,cx-s*18,b,      s*82,s*28,4);
      drawPineTree(c,cx+s*20,b,      s*78,s*26,4);
    },
    // 15 — sharp peaks (3 very narrow tall cones, 6–7 tiers)
    (c,cx,b,s)=>{
      drawPineTree(c,cx-s*20,b,      s*90, s*18,6);
      drawPineTree(c,cx,     b-s*4,  s*105,s*20,7);
      drawPineTree(c,cx+s*22,b,      s*85, s*18,6);
    },
    // 16 — broad stand (2 wide pines, lots of crown)
    (c,cx,b,s)=>{
      drawPineTree(c,cx-s*16,b,      s*72,s*44,4);
      drawPineTree(c,cx+s*18,b-s*4,  s*66,s*40,4);
    },
    // 17 — wandering row (5 trees, loose and varied)
    (c,cx,b,s)=>{
      [[-38,52,20],[-18,68,24],[2,60,22],[20,76,26],[40,55,20]]
        .forEach(([dx,h,w])=>drawPineTree(c,cx+s*dx,b,s*h,s*w,3));
    },
    // 18 — old growth (2 massive 5-tier, very wide)
    (c,cx,b,s)=>{
      drawPineTree(c,cx-s*18,b,      s*100,s*46,5);
      drawPineTree(c,cx+s*20,b-s*6,  s*92, s*42,5);
    },
    // 19 — nursery (5 small saplings)
    (c,cx,b,s)=>{
      [[-24,28,12],[-12,32,13],[0,36,14],[12,30,13],[24,26,11]]
        .forEach(([dx,h,w])=>drawPineTree(c,cx+s*dx,b,s*h,s*w,3));
    },
  ];

  // ── SIX PARALLAX LAYERS ──────────────────────────────────────────────────
  // y > 1.0 means the tree base sits below the bottom of the screen so only
  // the upper crown is visible — reinforces the "riding past a tree wall" feel.
  // ── SETTINGS ─────────────────────────────────────────────────────────────
  const TREE_SCROLL_SPEED = 1.0;  // multiplier — 1.0 = original, 2.0 = double speed

  const layerDefs = [
    { speed:0.08 * TREE_SCROLL_SPEED, scale:0.55, gap:36,  y:0.68, fill:'rgba(8,18,8,0.14)'    },
    { speed:0.19 * TREE_SCROLL_SPEED, scale:0.85, gap:45,  y:0.76, fill:'rgba(10,22,10,0.22)'  },
    { speed:0.45 * TREE_SCROLL_SPEED, scale:1.25, gap:55,  y:0.86, fill:'rgba(14,30,14,0.34)'  },
    { speed:0.88 * TREE_SCROLL_SPEED, scale:1.90, gap:68,  y:0.96, fill:'rgba(18,40,18,0.54)'  },
    { speed:1.60 * TREE_SCROLL_SPEED, scale:2.80, gap:85,  y:1.14, fill:'rgba(22,50,22,0.78)'  },
    { speed:2.90 * TREE_SCROLL_SPEED, scale:3.90, gap:108, y:1.28, fill:'rgba(28,65,22,0.95)'  },
  ];

  function buildStrip(L) {
    const stripW = Math.max(W() * 2, 400);
    const stripH = Math.round(L.scale * 165) + 10;
    const oc = document.createElement('canvas');
    oc.width = stripW; oc.height = stripH;
    const c = oc.getContext('2d');
    c.fillStyle = L.fill;
    let x = L.gap * Math.random();
    while (x < stripW + L.scale * 80) {
      const type = Math.floor(Math.random() * CLUSTERS.length);
      const sc   = L.scale * (0.70 + Math.random() * 0.60);
      c.beginPath();
      CLUSTERS[type](c, x, stripH - 2, sc);
      c.fill();
      x += L.gap * (0.32 + Math.random() * 0.85);
    }
    return { canvas:oc, width:stripW, height:stripH, scroll:Math.random()*stripW, ...L };
  }

  let strips = layerDefs.map(buildStrip);
  let lastW = W(), lastH = H();

  // ── STARS ────────────────────────────────────────────────────────────────
  // Generated once per (re)size. Static stars are constant alpha dots;
  // twinklers pulsate via sin(t * speed + phase) — each at its own period.
  function initStars(w, h) {
    const skyH = Math.round(h * 0.56);
    return {
      static: Array.from({length:150}, ()=>({
        x: Math.random()*w, y: Math.random()*skyH,
        r: 0.25 + Math.random()*1.0,
        a: 0.25 + Math.random()*0.65,
      })),
      twinkle: Array.from({length:30}, ()=>({
        x:      Math.random()*w, y: Math.random()*skyH*0.92,
        r:      0.7 + Math.random()*1.8,
        peakA:  0.55 + Math.random()*0.45,
        // stagger first fire so they don't all go at once
        nextAt: Math.floor(Math.random()*800),
        dur:    55 + Math.floor(Math.random()*90),
        tAt:    -9999,
      })),
    };
  }
  let starData = initStars(W(), H());

  // ── SHOOTING STARS ───────────────────────────────────────────────────────
  // Intermittent streaks across the upper sky. Tip advances along a shallow
  // downward trajectory; a linear gradient fades the tail to transparent.
  // Alpha uses a sine envelope — fast fade-in, gentle fade-out.
  let shootingStars = [];
  let nextShootAt   = 180 + Math.floor(Math.random() * 420);

  function spawnShootingStar(w, h) {
    const angle = (0.18 + Math.random() * 0.18) * Math.PI; // ~30–50° below horizontal
    const spd   = 5 + Math.random() * 5;
    return {
      x:   Math.random() * w * 0.80 + w * 0.05,
      y:   Math.random() * h * 0.40,
      vx:  Math.cos(angle) * spd,
      vy:  Math.sin(angle) * spd,
      len: 90 + Math.random() * 130,
      r:   0.8 + Math.random() * 0.6,
      life: 0,
      dur:  40 + Math.floor(Math.random() * 28),
    };
  }


  // ── MIST (six depth bands, each locked to a parallax layer speed) ────────
  // Each blob's base speed matches the tree strip at that depth, so mist at a
  // given depth slides past at the same rate as the trees behind it.
  // Front bands are low, thick, and fast; back bands are high, wispy, and slow.
  const MIST_BANDS = [
    { speed:0.08, baseY:0.56, yJit:0.08, n:10, wBase:360, hBase:5,  opBase:0.003, opJit:0.005, col:'180,210,185' },
    { speed:0.19, baseY:0.63, yJit:0.07, n:12, wBase:310, hBase:8,  opBase:0.004, opJit:0.008, col:'170,200,176' },
    { speed:0.45, baseY:0.70, yJit:0.07, n:14, wBase:280, hBase:12, opBase:0.007, opJit:0.011, col:'160,195,165' },
    { speed:0.88, baseY:0.77, yJit:0.07, n:16, wBase:270, hBase:17, opBase:0.011, opJit:0.017, col:'150,185,155' },
    { speed:1.60, baseY:0.84, yJit:0.07, n:20, wBase:300, hBase:26, opBase:0.018, opJit:0.027, col:'135,175,140' },
    { speed:2.90, baseY:0.90, yJit:0.08, n:26, wBase:340, hBase:40, opBase:0.026, opJit:0.040, col:'118,160,122' },
  ];
  const mistBlobs = MIST_BANDS.flatMap(B => {
    const maxW = B.wBase * 2.3;
    return Array.from({length:B.n}, () => ({
      // Pre-warm: spread x across the full loop span [-maxW … W()+maxW]
      // so blobs are already distributed everywhere on first frame.
      x:      -maxW + Math.random()*(W() + maxW*2),
      y:      H()*(B.baseY + Math.random()*B.yJit),
      w:      B.wBase + Math.random()*B.wBase*1.3,
      h:      B.hBase + Math.random()*B.hBase*1.6,
      speed:  B.speed*(0.72 + Math.random()*0.56),
      op:     B.opBase + Math.random()*B.opJit,
      baseY:  B.baseY, yJit:B.yJit, col:B.col,
    }));
  });

  // ── ROLLING HILLS ────────────────────────────────────────────────────────
  // Three layers of bezier silhouettes drifting slowly leftward.
  // quadraticCurveTo through midpoints → smooth continuous hill profile.
  // Far hills drift slowest; nearest hills fastest — secondary parallax.
  const hillLayers = [
    { baseY:0.70, amp:0.022, wave:0.18, phaseSpeed:0.0015, color:'rgba(3,9,4,0.93)'  },
    { baseY:0.74, amp:0.026, wave:0.14, phaseSpeed:0.0025, color:'rgba(4,12,5,0.95)' },
    { baseY:0.78, amp:0.030, wave:0.11, phaseSpeed:0.0040, color:'rgba(5,15,6,0.97)' },
  ];
  function drawHills(w, h) {
    hillLayers.forEach(L => {
      const pts = [];
      const n = 14;
      for (let i = 0; i <= n; i++) {
        pts.push({
          x: (i / n) * (w + 60) - 30,
          y: h * L.baseY + Math.sin(i * L.wave * Math.PI + t * L.phaseSpeed) * h * L.amp,
        });
      }
      ctx.beginPath();
      ctx.moveTo(pts[0].x, pts[0].y);
      for (let i = 0; i < pts.length - 1; i++) {
        const mx = (pts[i].x + pts[i+1].x) / 2;
        const my = (pts[i].y + pts[i+1].y) / 2;
        ctx.quadraticCurveTo(pts[i].x, pts[i].y, mx, my);
      }
      const last = pts[pts.length - 1];
      ctx.lineTo(last.x, last.y);
      ctx.lineTo(w + 30, h); ctx.lineTo(-30, h);
      ctx.closePath();
      ctx.fillStyle = L.color;
      ctx.fill();
    });
  }


  function draw() {
    const w = W(), h = H();
    if (w !== lastW || h !== lastH) {
      strips     = layerDefs.map(buildStrip);
      starData   = initStars(w, h);
lastW = w; lastH = h;
    }
    ctx.clearRect(0, 0, w, h);

    // Static stars
    starData.static.forEach(s => {
      ctx.beginPath();
      ctx.arc(s.x, s.y, s.r, 0, Math.PI*2);
      ctx.fillStyle = `rgba(210,220,255,${s.a})`;
      ctx.fill();
    });
    // Twinkling stars — fire occasionally, sine-envelope fade in/out
    starData.twinkle.forEach(s => {
      if (t >= s.nextAt && t > s.tAt + s.dur) {
        s.tAt   = t;
        s.nextAt = t + s.dur + 280 + Math.floor(Math.random()*1100);
      }
      const elapsed = t - s.tAt;
      if (elapsed < 0 || elapsed > s.dur) return;
      const a = s.peakA * Math.sin((elapsed / s.dur) * Math.PI);
      if (a < 0.02) return;
      ctx.beginPath();
      ctx.arc(s.x, s.y, s.r, 0, Math.PI*2);
      ctx.fillStyle = `rgba(225,238,255,${a.toFixed(2)})`;
      ctx.fill();
    });

    // Shooting stars
    if (t >= nextShootAt) {
      shootingStars.push(spawnShootingStar(w, h));
      nextShootAt = t + 300 + Math.floor(Math.random() * 560);
    }
    shootingStars = shootingStars.filter(s => s.life <= s.dur);
    shootingStars.forEach(s => {
      s.life++;
      const prog  = s.life / s.dur;
      const alpha = Math.sin(prog * Math.PI);
      if (alpha < 0.01) return;
      const tx = s.x + s.vx * s.life;
      const ty = s.y + s.vy * s.life;
      const ang = Math.atan2(s.vy, s.vx);
      const tailLen = s.len * Math.min(1, prog * 4);
      const bx = tx - Math.cos(ang) * tailLen;
      const by = ty - Math.sin(ang) * tailLen;
      const gr = ctx.createLinearGradient(bx, by, tx, ty);
      gr.addColorStop(0,    `rgba(210,230,255,0)`);
      gr.addColorStop(0.65, `rgba(220,235,255,${(alpha * 0.30).toFixed(3)})`);
      gr.addColorStop(1,    `rgba(245,250,255,${(alpha * 0.85).toFixed(3)})`);
      ctx.save();
      ctx.lineWidth   = s.r * 1.4;
      ctx.strokeStyle = gr;
      ctx.beginPath(); ctx.moveTo(bx, by); ctx.lineTo(tx, ty); ctx.stroke();
      ctx.beginPath(); ctx.arc(tx, ty, s.r, 0, Math.PI * 2);
      ctx.fillStyle = `rgba(245,252,255,${(alpha * 0.95).toFixed(3)})`;
      ctx.fill();
      ctx.restore();
    });

    // Rolling hills (behind all trees)
    drawHills(w, h);

    // Tree strips (back to front)
    strips.forEach(S => {
      S.scroll = (S.scroll + S.speed) % S.width;
      const drawY = h * S.y - S.height;
      ctx.drawImage(S.canvas, -S.scroll, drawY);
      ctx.drawImage(S.canvas, S.width - S.scroll, drawY);
    });


    // Mist blobs — each locked to its parallax depth speed
    mistBlobs.forEach(m => {
      m.x += m.speed;
      if (m.x > w + m.w) {
        // Teleport to well off the left edge with a random stagger so blobs
        // drift in at different times and never bunch up at x=0.
        m.x = -(m.w + Math.random() * w * 0.7);
        m.y = h * (m.baseY + Math.random() * m.yJit);
      }
      const mg = ctx.createRadialGradient(m.x+m.w/2, m.y, 0, m.x+m.w/2, m.y, m.w/2);
      mg.addColorStop(0, `rgba(${m.col},${m.op})`);
      mg.addColorStop(1, `rgba(${m.col},0)`);
      ctx.fillStyle = mg;
      ctx.fillRect(m.x, m.y - m.h/2, m.w, m.h);
    });

    // Deep ground fog — broad fade starting higher up
    const fog = ctx.createLinearGradient(0, h*0.74, 0, h);
    fog.addColorStop(0,    'rgba(4,10,4,0)');
    fog.addColorStop(0.50, 'rgba(4,10,4,0.22)');
    fog.addColorStop(1,    'rgba(4,10,4,0.96)');
    ctx.fillStyle = fog;
    ctx.fillRect(0, h*0.74, w, h*0.26);
    // Heavy foreground fog bank right at the screen edge
    const fogHeavy = ctx.createLinearGradient(0, h*0.86, 0, h);
    fogHeavy.addColorStop(0, 'rgba(6,16,6,0)');
    fogHeavy.addColorStop(1, 'rgba(6,16,6,0.90)');
    ctx.fillStyle = fogHeavy;
    ctx.fillRect(0, h*0.86, w, h*0.14);

    t += 1;
    fxAnim = requestAnimationFrame(draw);
  }
  draw();
}

