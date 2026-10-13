// ╔═══════════════════════════════════════════════════════════════════════════════╗
// ║  SAKURA THEME                                                                 ║
// ║  Falling cherry blossom petals with gentle drift                              ║
// ╚═══════════════════════════════════════════════════════════════════════════════╝
function createSakuraLayer() {
  const colors = ['#ffd6e1','#ffc0d0','#ffb0c4','#ffe6ee','#ffafc8','#fff0f5'];
  const baseCount = (typeof AppSettings !== 'undefined' && AppSettings.leaf_count) ? AppSettings.leaf_count : 40;
  const count = Math.round(baseCount * 2.25);
  const petals = Array.from({length: count}, () => ({
    x: Math.random() * canvas.width,
    y: Math.random() * canvas.height * 1.2 - 40,
    size: 6 + Math.random() * 10,
    color: colors[Math.floor(Math.random() * colors.length)],
    vx: (Math.random() - 0.5) * 0.7,
    vy: 0.45 + Math.random() * 0.85,
    rot: Math.random() * Math.PI * 2,
    rotSpeed: (Math.random() - 0.5) * 0.04,
    swing: Math.random() * Math.PI * 2,
    swingSpeed: 0.018 + Math.random() * 0.026,
    flip: Math.random() * Math.PI * 2,
    flipSpeed: 0.012 + Math.random() * 0.024,
  }));
  function drawPetal(c, p) {
    c.save();
    c.translate(p.x, p.y);
    c.rotate(p.rot);
    c.scale(1, Math.cos(p.flip));
    const s = p.size;
    // 5 petal lobes around a small center
    c.fillStyle = p.color;
    for (let i = 0; i < 5; i++) {
      const a = (i * 2 * Math.PI) / 5;
      const px = Math.cos(a) * s * 0.55;
      const py = Math.sin(a) * s * 0.55;
      c.beginPath();
      c.ellipse(px, py, s * 0.55, s * 0.38, a, 0, Math.PI * 2);
      c.fill();
    }
    // Tiny stamen highlight
    c.fillStyle = 'rgba(255,235,150,0.7)';
    c.beginPath();
    c.arc(0, 0, s * 0.18, 0, Math.PI * 2);
    c.fill();
    c.restore();
  }
  let t = Math.random() * 1000;
  function getBreeze() {
    return Math.sin(t * 0.0009) * 0.5 + Math.sin(t * 0.0033 + 1.3) * 0.25;
  }
  return {
    draw(c, alpha) {
      c.save();
      c.globalAlpha = alpha;
      t++;
      const wind = getBreeze();
      petals.forEach(p => {
        p.swing += p.swingSpeed;
        p.flip += p.flipSpeed;
        p.x += p.vx + Math.sin(p.swing) * 0.55 + wind;
        p.y += p.vy;
        p.rot += p.rotSpeed + wind * 0.005;
        if (p.y > canvas.height + 30) {
          p.y = -30;
          p.x = Math.random() * canvas.width;
        }
        if (p.x < -40) p.x = canvas.width + 20;
        if (p.x > canvas.width + 40) p.x = -20;
        drawPetal(c, p);
      });
      c.restore();
    },
    cleanup() {}
  };
}

const sakuraSeen = [];
// 20 procedurally-generated sakura backgrounds (1024×640 JPEGs on the CDN).
// Each ends with a #N stamp in the bottom-right corner so the user can
// identify which one is showing.
const SAKURA_BGS = bgSet('sakura', 20);
function startSakura() {
  // Pick an unused-this-cycle background variant.
  const idx = pickUnusedIdx(SAKURA_BGS, sakuraSeen, 'sakura');
  document.body.style.setProperty('--sakura-bg', `url("${SAKURA_BGS[idx]}")`);
  document.body.dataset.sakuraBg = String(idx + 1);
  window.currentSakuraBg = idx + 1;
  console.log(`[sakura] background #${idx + 1} of ${SAKURA_BGS.length} (${sakuraSeen.length} of ${SAKURA_BGS.length} this cycle)`);
  const layer = createSakuraLayer();
  function draw() {
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    layer.draw(ctx, 1);
    fxAnim = requestAnimationFrame(draw);
  }
  draw();
}

