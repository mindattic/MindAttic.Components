// ╔═══════════════════════════════════════════════════════════════════════════════╗
// ║  SYNTHWAVE THEME                                                              ║
// ║  Perspective grid · wireframe mountains · sliced retro sun · Outrun road      ║
// ╠═══════════════════════════════════════════════════════════════════════════════╣
// ║  Four layers, back to front:                                                  ║
// ║                                                                               ║
// ║  Sky: deep-purple → hot-pink → orange → yellow gradient.  CSS linear-         ║
// ║  gradient, repainted each frame over the canvas background.                   ║
// ║                                                                               ║
// ║  Sun: large circle with horizontal stripe gaps drawn as a series of arc()     ║
// ║  segments — the classic 1980s Outrun/city-pop aesthetic.  Stacked concentric  ║
// ║  glow circles at decreasing alpha add the neon bloom effect.                  ║
// ║                                                                               ║
// ║  Wireframe mountains: two triangulated grids (left and right of the road).    ║
// ║  buildWireframeMtns() generates a 12×6 vertex grid per side, stored as        ║
// ║  screen-space {x,y} points.  Heights use Math.pow(outerFrac, 0.65) so peaks   ║
// ║  taper smoothly to zero at the inner edge (the road gap).  Triangles are      ║
// ║  filled with near-black and edged magenta → cyan, depth-faded.                ║
// ║                                                                               ║
// ║  Perspective grid: 18 horizontal rows × 14 vertical columns, all converging   ║
// ║  at the vanishing point.  Row depths use p² warping (q = p*p) so grid lines   ║
// ║  near the horizon are denser and near the viewer are wider apart — correct    ║
// ║  perspective with one line of math.  The grid scrolls by advancing a y-phase  ║
// ║  each frame, creating a "flying forward at speed" illusion.                   ║
// ║                                                                               ║
// ║  No WebGL.  No shaders.  All 2D canvas trig.                                  ║
// ╚═══════════════════════════════════════════════════════════════════════════════╝
// Two rendering layers per frame:
//
//   Perspective grid: horizontal lines converge at a vanishing point at the
//     center-horizon. Vertical lines are spaced evenly in perspective space
//     (closer to viewer = wider apart). The grid animates by advancing a y-phase
//     each frame — creates a "moving forward at speed" illusion from 2D math.
//     Line brightness increases toward the horizon (distance fade, inverted).
//
//   The sun: a circle with horizontal dark bands drawn across it in slices —
//     the classic 80s Outrun aesthetic. Glow is stacked concentric circles at
//     decreasing alpha. The color shifts from deep magenta at top to orange-yellow
//     at the horizon via a CSS-variable-driven gradient on the background.
//
// The whole scene is a flat canvas. No WebGL. No shaders. Just trig and rAF.
function startSynthwave() {
  const W = () => canvas.width, H = () => canvas.height;
  let t = 0;

  // Wireframe mesh mountains — triangulated grids on each side of the road,
  // drawn with blue/cyan edge lines and a near-black fill so the sky shows
  // through the peaks while the terrain reads as solid 3D.
  function buildWireframeMtns() {
    const w = canvas.width, h = canvas.height, hor = h * 0.55;
    return [-1, 1].map(side => {
      const xL = side < 0 ? -12 : w * 0.52;
      const xR = side < 0 ? w * 0.48 : w + 12;
      const cols = 12, rows = 6;
      const grid = [];
      for (let r = 0; r <= rows; r++) {
        const rf = r / rows;  // 0 = top peaks, 1 = base at horizon
        const row = [];
        for (let c = 0; c <= cols; c++) {
          const xf = c / cols;
          const x = xL + (xR - xL) * xf;
          if (r === rows) {
            row.push({ x, y: hor + 3 });  // base row sits on the horizon
          } else {
            const outerFrac = side < 0 ? (1 - xf) : xf; // 0 at road gap, 1 at screen edge
            const maxH = hor * 0.44 * Math.pow(1 - rf, 0.65) * Math.pow(outerFrac, 0.65);
            row.push({ x, y: hor - maxH * (0.40 + Math.random() * 0.60) });
          }
        }
        grid.push(row);
      }
      return grid;
    });
  }
  const mtnGrids = buildWireframeMtns();

  function draw() {
    const w = W(), h = H();
    const horizon = h * 0.55;

    // ── Sky gradient: deep purple → hot pink → orange → yellow ──
    const sky = ctx.createLinearGradient(0, 0, 0, horizon);
    sky.addColorStop(0,   '#1a0030');
    sky.addColorStop(0.3, '#4a0060');
    sky.addColorStop(0.55,'#ff2d95');
    sky.addColorStop(0.75,'#ff6b35');
    sky.addColorStop(0.9, '#ffb347');
    sky.addColorStop(1,   '#ffe066');
    ctx.fillStyle = sky;
    ctx.fillRect(0, 0, w, horizon);

    // ── Sun: big circle with horizontal slice gaps ──
    const sunR = Math.min(w, h) * 0.18;
    const sunX = w / 2;
    // Sunrise: starts as a thin arc just above the horizon, rises to full height
    // over ~60s (t reaches 150 at 0.04/frame). Smoothstep easing: slow start,
    // accelerates through the horizon, settles gently at maximum height.
    const sunRiseT = Math.min(1, t / 150);
    const sunEased = sunRiseT * sunRiseT * (3 - 2 * sunRiseT); // smoothstep
    // At sunEased=0: center is 0.85 radii below horizon (thin sliver visible)
    // At sunEased=1: center is 0.3 radii above horizon (current max position)
    const sunY = horizon + sunR * (0.85 - 1.15 * sunEased);
    // Sun glow
    const glow = ctx.createRadialGradient(sunX, sunY, sunR * 0.5, sunX, sunY, sunR * 2);
    glow.addColorStop(0, 'rgba(255,200,100,0.3)');
    glow.addColorStop(1, 'rgba(255,200,100,0)');
    ctx.fillStyle = glow;
    ctx.fillRect(0, 0, w, horizon);

    // Draw sun with horizontal slices cut out
    ctx.save();
    ctx.beginPath();
    ctx.rect(0, 0, w, horizon); // clip to sky
    ctx.clip();
    const sliceCount = 8;
    const sliceGap = 3 + Math.floor(sunR / 25);
    for (let row = -sunR; row < sunR; row += sliceGap * 2) {
      ctx.beginPath();
      // For each horizontal band, find circle intersection
      const rowTop = row;
      const rowBot = Math.min(row + sliceGap, sunR);
      // Clip to circle: x² + y² = R²
      for (let y = rowTop; y <= rowBot; y++) {
        const xHalf = Math.sqrt(Math.max(0, sunR * sunR - y * y));
        if (y === rowTop) ctx.moveTo(sunX - xHalf, sunY + y);
        ctx.lineTo(sunX - xHalf, sunY + y);
      }
      for (let y = rowBot; y >= rowTop; y--) {
        const xHalf = Math.sqrt(Math.max(0, sunR * sunR - y * y));
        ctx.lineTo(sunX + xHalf, sunY + y);
      }
      ctx.closePath();
      // Gradient across the sun disc
      const sg = ctx.createLinearGradient(0, sunY - sunR, 0, sunY + sunR);
      sg.addColorStop(0, '#ff2d95');
      sg.addColorStop(0.5, '#ff6b35');
      sg.addColorStop(1, '#ffe066');
      ctx.fillStyle = sg;
      ctx.fill();
    }
    ctx.restore();

    // ── Wireframe mountains ──────────────────────────────────────────────────
    ctx.save();
    ctx.beginPath(); ctx.rect(0, 0, w, horizon + 4); ctx.clip();
    mtnGrids.forEach(grid => {
      const nR = grid.length - 1, nC = grid[0].length - 1;
      for (let r = 0; r < nR; r++) {
        const depthFrac = 1 - r / nR;  // 1 = near top (bright), 0 = near base (dim)
        for (let c = 0; c < nC; c++) {
          const tl = grid[r][c],   tr = grid[r][c+1];
          const bl = grid[r+1][c], br = grid[r+1][c+1];
          // Fully opaque dark purple fill — blocks warm sky so edges read true
          ctx.fillStyle = `rgb(12,0,28)`;
          ctx.beginPath(); ctx.moveTo(tl.x,tl.y); ctx.lineTo(tr.x,tr.y); ctx.lineTo(br.x,br.y); ctx.closePath(); ctx.fill();
          ctx.beginPath(); ctx.moveTo(tl.x,tl.y); ctx.lineTo(br.x,br.y); ctx.lineTo(bl.x,bl.y); ctx.closePath(); ctx.fill();
          // Edges: hot magenta at peaks → electric blue at base
          const er = Math.round(255 * depthFrac + 80 * (1 - depthFrac));
          const eg = 0;
          const eb = Math.round(120 * depthFrac + 255 * (1 - depthFrac));
          ctx.strokeStyle = `rgba(${er},${eg},${eb},${(0.30 + depthFrac * 0.55).toFixed(2)})`;
          ctx.lineWidth = 0.75;
          ctx.beginPath();
          ctx.moveTo(tl.x,tl.y); ctx.lineTo(tr.x,tr.y);
          ctx.moveTo(tr.x,tr.y); ctx.lineTo(br.x,br.y);
          ctx.moveTo(br.x,br.y); ctx.lineTo(bl.x,bl.y);
          ctx.moveTo(bl.x,bl.y); ctx.lineTo(tl.x,tl.y);
          ctx.moveTo(tl.x,tl.y); ctx.lineTo(br.x,br.y);  // diagonal
          ctx.stroke();
        }
      }
    });
    ctx.restore();

    // ── Ground: dark purple with perspective grid ──
    const groundGrad = ctx.createLinearGradient(0, horizon, 0, h);
    groundGrad.addColorStop(0, '#2a0845');
    groundGrad.addColorStop(1, '#0e0418');
    ctx.fillStyle = groundGrad;
    ctx.fillRect(0, horizon, w, h - horizon);

    // ── Unified perspective grid ──────────────────────────────────────────────
    // Draw horizontal lines AND the vertical column segments connecting adjacent
    // rows together so the cells form a scrolling lattice rather than two
    // independent sets of lines.
    const numH    = 18;   // depth rows
    const numCols = 14;   // columns (0..numCols), spread from edge to edge
    const groundH = h - horizon;
    const scrollBase = t * 0.05;

    for (let row = 0; row < numH; row++) {
      const p0 = ((row       / numH) + scrollBase) % 1.0;
      const p1raw = (((row + 1) / numH) + scrollBase) % 1.0;
      // On wrap, extend to 1.0 so the segment reaches the screen bottom
      // rather than vanishing, eliminating the flicker gap.
      const p1 = p1raw < p0 ? 1.0 : p1raw;
      const q0 = p0 * p0, q1 = p1 * p1;
      const y0 = horizon + groundH * q0;
      const y1 = horizon + groundH * q1;
      if (y0 > h + 40) continue;  // well past viewport, skip
      const alpha = Math.min(0.80, p1 * 0.62 + 0.05);

      // Horizontal line at the near edge of this cell
      ctx.strokeStyle = `rgba(90,130,255,${alpha.toFixed(2)})`;
      ctx.lineWidth = 0.5 + q1 * 2.0;
      ctx.beginPath(); ctx.moveTo(0, y0); ctx.lineTo(w, y0); ctx.stroke();

      // Vertical segments — each column connects (x@p0,y0) → (x@p1,y1)
      for (let col = 0; col <= numCols; col++) {
        const wx = (col / numCols) * 2 - 1;        // world x: -1..+1
        const x0 = w / 2 + wx * (w / 2) * q0;
        const x1 = w / 2 + wx * (w / 2) * q1;
        const ca = (alpha * (0.22 + Math.abs(wx) * 0.55)).toFixed(2);
        ctx.strokeStyle = `rgba(90,130,255,${ca})`;
        ctx.lineWidth = 0.4 + q1 * 1.1;
        ctx.beginPath(); ctx.moveTo(x0, y0); ctx.lineTo(x1, y1); ctx.stroke();
      }
    }

    // ── Road perspective ──────────────────────────────────────────────────
    // Vanishing point at centre of horizon.  Road edges spread to ~30% of
    // screen width at the bottom.  A single dashed centre line scrolls
    // toward the viewer with quadratic (p²) warp for correct perspective.
    const vp    = { x: w / 2, y: horizon };
    const roadW = w * 0.30;  // half-width at screen bottom

    // Left and right road edges
    ctx.strokeStyle = 'rgba(255,60,200,0.80)';
    ctx.lineWidth   = 2.0;
    ctx.beginPath(); ctx.moveTo(vp.x, vp.y); ctx.lineTo(vp.x - roadW, h); ctx.stroke();
    ctx.beginPath(); ctx.moveTo(vp.x, vp.y); ctx.lineTo(vp.x + roadW, h); ctx.stroke();

    t += 0.040; // drive speed — increase for faster forward motion

    // Scanline overlay for CRT feel
    ctx.fillStyle = 'rgba(0,0,0,0.04)';
    for (let y = 0; y < h; y += 3) {
      ctx.fillRect(0, y, w, 1);
    }

    fxAnim = requestAnimationFrame(draw);
  }
  draw();
}

