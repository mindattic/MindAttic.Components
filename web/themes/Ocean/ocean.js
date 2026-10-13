// ╔═══════════════════════════════════════════════════════════════════════════════╗
// ║  OCEAN THEME                                                                  ║
// ║  Rising bubbles + caustic underwater light shafts                             ║
// ╠═══════════════════════════════════════════════════════════════════════════════╣
// ║  Two particle systems composited on the same canvas frame:                    ║
// ║                                                                               ║
// ║  Light shafts: wide, skewed radial-gradient columns that drift left/right     ║
// ║  via sinusoidal phase offsets.  Multiple overlapping gradients at low alpha   ║
// ║  simulate underwater caustic light patterns — the kind you'd see on a reef    ║
// ║  on a sunny day.  They shift each frame rather than flashing.                 ║
// ║                                                                               ║
// ║  Bubbles: 40 particles rising from random depths.  Each bubble has:           ║
// ║    - Radius that varies with depth (small at surface, larger from deep)       ║
// ║    - Sinusoidal x-wobble on ascent (bubbles never rise perfectly straight)    ║
// ║    - Alpha fade as the bubble approaches the surface                          ║
// ║    - Respawn at a random depth when it exits the top                          ║
// ║                                                                               ║
// ║  No images, no SVG, no shaders.  Pure 2D Canvas API + trigonometry.           ║
// ╚═══════════════════════════════════════════════════════════════════════════════╝
// Two particle systems composited on the same canvas frame:
//
//   Light rays: wide, alpha-gradient vertical columns that drift slowly left/right
//     via a sinusoidal phase offset. Multiple overlapping radial gradients simulate
//     underwater caustic light patterns. Redrawn each frame at low alpha — they
//     shift rather than flash.
//
//   Bubbles: particles rising from random depths, each with:
//     - Variable radius (small near surface, larger from depth)
//     - Sinusoidal x-wobble on ascent (bubbles never rise perfectly straight)
//     - Alpha fade as they near the surface (they "dissolve" rather than pop)
//     - Respawn at bottom when they exit the top
//
// No images. No SVG. Pure Canvas API + math.
function startOcean() {
  const W = () => canvas.width, H = () => canvas.height;
  const bubbles = Array.from({length: 40}, () => spawnBubble(W(), H(), true));
  // ── GOD RAYS ───────────────────────────────────────────────────────
  // Many thin rays fanning down from a single off-screen "sun" above the
  // water surface. Each ray is randomly clustered in angle so rays group
  // together (like real scattered light) rather than evenly spaced.
  // Soft drift, varying widths, additive blend, length-based fade.
  const NUM_RAYS = 18;
  const shafts = Array.from({length: NUM_RAYS}, () => {
    // Cluster angles around a few "lanes" so groups of rays appear together
    const cluster = Math.floor(Math.random() * 5);  // 5 angle clusters
    const clusterCenter = (cluster - 2) * 0.18;     // ±0.36 rad (~±21°)
    const angleOffset = clusterCenter + (Math.random() - 0.5) * 0.18;
    return {
      angleOffset,                                  // base offset from straight-down
      driftSpd: 0.00015 + Math.random() * 0.0004,
      driftAmp: 0.012 + Math.random() * 0.025,      // very small drift
      phase: Math.random() * Math.PI * 2,
      // Width at the far end of the ray (very narrow at source)
      farWidth: 28 + Math.random() * 70,
      // Per-ray brightness varies — most subtle, a few brighter "feature" rays
      opacity: (0.018 + Math.random() * 0.025) * (Math.random() < 0.18 ? 2.4 : 1),
      pulseSpd: 0.002 + Math.random() * 0.004,
      pulsePhase: Math.random() * Math.PI * 2,
    };
  });
  // Source point: a bit above the top of the canvas, slightly off-center.
  // Re-randomized once per session for variety between page loads.
  const sourceXFrac = 0.45 + Math.random() * 0.20;
  const sourceYFrac = -0.25 + Math.random() * 0.10;
  let ft = 0;

  function spawnBubble(w, h, init) {
    return {
      x: Math.random() * w,
      y: init ? Math.random() * h : h + 10 + Math.random() * 40,
      r: 3 + Math.random() * 12,
      speed: 0.3 + Math.random() * 0.8,
      wobbleAmp: 10 + Math.random() * 20,
      wobbleSpd: 0.008 + Math.random() * 0.012,
      wobbleOff: Math.random() * Math.PI * 2,
      opacity: 0.15 + Math.random() * 0.25,
      t: 0,
    };
  }

  function draw() {
    const w = W(), h = H();
    ctx.clearRect(0, 0, w, h);

    // Depth darkness overlay — darker the deeper you go
    const depthGrad = ctx.createLinearGradient(0, 0, 0, h);
    depthGrad.addColorStop(0,   'rgba(0,8,20,0)');
    depthGrad.addColorStop(0.3, 'rgba(0,6,16,0.1)');
    depthGrad.addColorStop(0.6, 'rgba(0,4,12,0.25)');
    depthGrad.addColorStop(0.85,'rgba(0,2,8,0.45)');
    depthGrad.addColorStop(1,   'rgba(0,0,4,0.65)');
    ctx.fillStyle = depthGrad;
    ctx.fillRect(0, 0, w, h);

    // ── GOD RAYS — radiate from a single off-screen sun source ──────
    // Each ray is a thin wedge from (sourceX, sourceY) fanning downward.
    // Additive 'lighter' blending so overlapping rays brighten naturally.
    // Length-based gradient fade, soft small drift, occasional pulse.
    ft++;
    const sMax = document.documentElement.scrollHeight - window.innerHeight;
    const sDep = sMax > 0 ? Math.min(1, window.scrollY / sMax) : 0;
    const shaftDim = 1 - sDep * 0.85;
    const srcX = w * sourceXFrac;
    const srcY = h * sourceYFrac;
    const rayLen = h * 1.6; // long enough to extend past bottom edge
    ctx.save();
    ctx.globalCompositeOperation = 'lighter';
    shafts.forEach(s => {
      const drift = Math.sin(ft * s.driftSpd + s.phase) * s.driftAmp;
      const angle = Math.PI / 2 + s.angleOffset + drift; // ~straight down
      const pulse = 0.55 + 0.45 * Math.sin(ft * s.pulseSpd + s.pulsePhase);
      const alpha = s.opacity * pulse * shaftDim;
      // End point of the ray's central line
      const endX = srcX + Math.cos(angle) * rayLen;
      const endY = srcY + Math.sin(angle) * rayLen;
      // Perpendicular unit vector for ray width
      const px = -Math.sin(angle);
      const py = Math.cos(angle);
      // Narrow wedge near source, fans out at the far end
      const topW = 4;
      const botW = s.farWidth;
      // Build the wedge polygon
      ctx.beginPath();
      ctx.moveTo(srcX - topW * px, srcY - topW * py);
      ctx.lineTo(srcX + topW * px, srcY + topW * py);
      ctx.lineTo(endX + botW * px, endY + botW * py);
      ctx.lineTo(endX - botW * px, endY - botW * py);
      ctx.closePath();
      // Length-based gradient: brightest at source, fades along ray
      const grad = ctx.createLinearGradient(srcX, srcY, endX, endY);
      grad.addColorStop(0.00, `rgba(180,235,255,${alpha * 1.8})`);
      grad.addColorStop(0.18, `rgba(140,220,250,${alpha * 1.2})`);
      grad.addColorStop(0.45, `rgba(80,180,225,${alpha * 0.55})`);
      grad.addColorStop(0.72, `rgba(40,120,170,${alpha * 0.18})`);
      grad.addColorStop(1.00, 'rgba(20,60,100,0)');
      ctx.fillStyle = grad;
      ctx.fill();
    });
    ctx.globalCompositeOperation = 'source-over';
    ctx.restore();

    // Draw and update bubbles — opacity scaled by depth
    bubbles.forEach(b => {
      b.t += b.wobbleSpd;
      b.y -= b.speed;
      const wx = b.x + Math.sin(b.t + b.wobbleOff) * b.wobbleAmp;
      const depthFade = 1 - (b.y / h) * 0.7; // brighter near top, dimmer at bottom

      // Bubble body
      ctx.beginPath();
      ctx.arc(wx, b.y, b.r, 0, Math.PI * 2);
      ctx.strokeStyle = `rgba(144,224,239,${b.opacity * depthFade})`;
      ctx.lineWidth = 1;
      ctx.stroke();

      // Inner glow
      const ig = ctx.createRadialGradient(wx - b.r * 0.3, b.y - b.r * 0.3, b.r * 0.1, wx, b.y, b.r);
      ig.addColorStop(0, `rgba(200,240,255,${b.opacity * 0.6 * depthFade})`);
      ig.addColorStop(1, 'rgba(200,240,255,0)');
      ctx.fillStyle = ig;
      ctx.fill();

      // Specular highlight
      ctx.beginPath();
      ctx.arc(wx - b.r * 0.25, b.y - b.r * 0.3, b.r * 0.25, 0, Math.PI * 2);
      ctx.fillStyle = `rgba(255,255,255,${b.opacity * 0.7 * depthFade})`;
      ctx.fill();

      // Respawn when off top
      if (b.y + b.r < -10) {
        Object.assign(b, spawnBubble(w, h, false));
      }
    });

    // Scroll-depth darkness — the further you scroll, the deeper and darker
    const scrollMax = document.documentElement.scrollHeight - window.innerHeight;
    const scrollDepth = scrollMax > 0 ? Math.min(1, window.scrollY / scrollMax) : 0;
    const darkAlpha = scrollDepth * 0.7; // up to 70% black at the very bottom
    if (darkAlpha > 0.01) {
      ctx.fillStyle = `rgba(0,2,8,${darkAlpha})`;
      ctx.fillRect(0, 0, w, h);
    }

    fxAnim = requestAnimationFrame(draw);
  }
  draw();
}

