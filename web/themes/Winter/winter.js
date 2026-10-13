// ╔═══════════════════════════════════════════════════════════════════════════════╗
// ║  WINTER THEME                                                                 ║
// ║  Snowflakes falling + a physically accumulating SVG snow pile                 ║
// ╠═══════════════════════════════════════════════════════════════════════════════╣
// ║  Two systems running simultaneously on the same canvas + a separate SVG:      ║
// ║                                                                               ║
// ║  Snowflakes: canvas particles with randomised size, speed, and horizontal     ║
// ║  drift.  Each flake has a sine-wave wobble offset so adjacent flakes don't    ║
// ║  track each other.  Flakes that exit the viewport respawn at the top.         ║
// ║                                                                               ║
// ║  Snow pile: an SVG element pinned to the page bottom.  A 90-column height     ║
// ║  array tracks the pile depth per column.  When a flake lands (y > pile        ║
// ║  height), addSnow() accumulates depth at that column ± a random spread.       ║
// ║  settleSnow() runs a 1D smoothing pass each frame — neighbouring columns      ║
// ║  average their heights — giving the pile a flowing, organic silhouette.       ║
// ║  buildSnowPath() converts the height array to a closed SVG polyline.          ║
// ║                                                                               ║
// ║  The pile grows continuously until max height, then resets and starts again.  ║
// ╚═══════════════════════════════════════════════════════════════════════════════╝
// Two separate systems working in tandem:
//
//   Canvas: `snow_flake_count` falling particles with randomized size, speed,
//           drift, and sinusoidal horizontal wobble. Standard particle system.
//
//   SVG pile: an absolutely-positioned <svg> at the viewport bottom.
//             Each flake that reaches the ground raises the pile at its x column
//             by `snow_pile_rate`. The pile profile is a smooth polyline built
//             from per-column height values. snow_smooth_speed lerps neighboring
//             columns toward each other every frame — natural snow redistribution.
//             Max pile height is clamped to snow_max_height * viewport height.
//
// The SVG approach for the pile (rather than canvas) means the drift and shaping
// happen in the DOM layer — easier to tweak and visually distinct from the canvas.
function createWinterLayer() {
  const H = canvas.height;
  const SEGMENTS = 80;
  const snowHeight = new Float32Array(SEGMENTS + 1);
  const snowTarget = new Float32Array(SEGMENTS + 1);
  const MAX_SNOW = H * AppSettings.snow_max_height;
  const PILE_RATE_MIN = AppSettings.snow_pile_rate_min, PILE_RATE_MAX = AppSettings.snow_pile_rate_max;
  const SMOOTH_SPEED = AppSettings.snow_smooth_speed;
  const SPREAD = AppSettings.snow_spread;

  // Seed with a small initial snow drift
  for (let i = 0; i <= SEGMENTS; i++) {
    const t = i / SEGMENTS;
    const base = 8 + Math.sin(t*Math.PI*2.3)*4 + Math.sin(t*Math.PI*5.1)*2.5 + Math.random()*1.5;
    snowHeight[i] = base; snowTarget[i] = base;
  }

  let snowSVG = document.getElementById('snow-pile-svg');
  if (!snowSVG) {
    snowSVG = document.createElementNS('http://www.w3.org/2000/svg','svg');
    snowSVG.id = 'snow-pile-svg';
    snowSVG.setAttribute('xmlns','http://www.w3.org/2000/svg');
    snowSVG.style.cssText = 'position:fixed;bottom:0;left:0;width:100%;height:100%;pointer-events:none;z-index:1;';
    snowSVG.setAttribute('preserveAspectRatio','none');
    snowSVG.innerHTML = `
      <defs>
        <linearGradient id="snow-grad" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stop-color="rgba(255,255,255,0.95)"/>
          <stop offset="40%" stop-color="rgba(240,248,255,0.97)"/>
          <stop offset="100%" stop-color="rgba(220,235,250,1)"/>
        </linearGradient>
        <filter id="snow-shadow">
          <feDropShadow dx="0" dy="-2" stdDeviation="3" flood-color="rgba(180,210,240,0.4)"/>
        </filter>
      </defs>
      <path id="snow-drift-back" d="" fill="rgba(200,220,240,0.3)" filter="url(#snow-shadow)"/>
      <path id="snow-drift" d="" fill="url(#snow-grad)"/>
      <path id="snow-drift-highlight" d="" fill="none" stroke="rgba(255,255,255,0.8)" stroke-width="1.5"/>
    `;
    document.body.appendChild(snowSVG);
  }
  const driftPath = document.getElementById('snow-drift');
  const driftBack = document.getElementById('snow-drift-back');
  const driftHighlight = document.getElementById('snow-drift-highlight');
  function updateSVGViewBox() {
    const w = window.innerWidth, h = window.innerHeight;
    snowSVG.setAttribute('viewBox', `0 0 ${w} ${h}`);
  }
  updateSVGViewBox();
  const resizeH = ()=>updateSVGViewBox();
  window.addEventListener('resize', resizeH);

  function buildSnowPath(heights, offsetY) {
    const w = window.innerWidth, h = window.innerHeight, segW = w / SEGMENTS, off = offsetY || 0;
    let d = `M 0,${h} L 0,${h - heights[0] + off}`;
    for (let i = 1; i <= SEGMENTS; i++) {
      const x = i*segW, y = h-heights[i]+off;
      const prevX=(i-1)*segW, prevY=h-heights[i-1]+off;
      d += ` C ${prevX+segW*0.4},${prevY} ${x-segW*0.4},${y} ${x},${y}`;
    }
    d += ` L ${w},${h} Z`; return d;
  }
  function buildHighlightPath(heights) {
    const w = window.innerWidth, h = window.innerHeight, segW = w / SEGMENTS;
    let d = `M 0,${h - heights[0]}`;
    for (let i = 1; i <= SEGMENTS; i++) {
      const x = i*segW, y = h-heights[i], prevY=h-heights[i-1];
      d += ` C ${(i-1)*segW+segW*0.4},${prevY} ${x-segW*0.4},${y} ${x},${y}`;
    }
    return d;
  }
  function addSnow(x, pileRate) {
    const w = window.innerWidth, seg = (x/w)*SEGMENTS, center = Math.round(seg);
    for (let i = Math.max(0,center-3); i <= Math.min(SEGMENTS,center+3); i++) {
      const dist = Math.abs(i-seg);
      snowTarget[i] = Math.min(MAX_SNOW, snowTarget[i] + pileRate * Math.exp(-(dist*dist)/(SPREAD*SPREAD)));
    }
  }
  let settleTimer = 0;
  function settleSnow() {
    settleTimer++;
    if (settleTimer%8!==0) return;
    for (let i=1;i<SEGMENTS;i++) {
      const avg = (snowTarget[i-1]+snowTarget[i]*2+snowTarget[i+1])/4;
      snowTarget[i] = snowTarget[i]*0.97+avg*0.03;
    }
  }

  const flakes = Array.from({length:AppSettings.snow_flake_count},()=>({
    x:Math.random()*canvas.width, y:Math.random()*canvas.height,
    size:1.5+Math.random()*4.5, speed:0.8+Math.random()*2.2,
    drift:Math.random()*Math.PI*2, driftSpeed:0.008+Math.random()*0.018,
    alpha:0.55+Math.random()*0.45, spin:Math.random()*Math.PI*2,
    spinSpeed:(Math.random()-0.5)*0.04,
    arms: Math.random()>0.5,
    pileRate: PILE_RATE_MIN + Math.random()*(PILE_RATE_MAX - PILE_RATE_MIN),
  }));
  function drawFlake(c,f) {
    c.save(); c.translate(f.x,f.y); c.rotate(f.spin);
    if (f.arms && f.size > 3) {
      c.strokeStyle='#ffffff'; c.lineWidth=1.2;
      for(let i=0;i<6;i++){ c.beginPath(); c.moveTo(0,0); c.lineTo(0,-f.size*1.8);
        c.moveTo(-f.size*0.5,-f.size*0.9); c.lineTo(f.size*0.5,-f.size*0.9);
        c.stroke(); c.rotate(Math.PI/3); }
    } else { c.beginPath(); c.arc(0,0,f.size,0,Math.PI*2); c.fillStyle='#ffffff'; c.fill(); }
    c.restore();
  }

  let melting = false;
  return {
    setMelting(v){ melting=v; },
    draw(c, alpha) {
      c.save(); c.globalAlpha = alpha;
      const h = window.innerHeight;
      flakes.forEach(f=>{
        f.drift += f.driftSpeed; f.spin += f.spinSpeed;
        f.x += Math.sin(f.drift)*0.6; f.y += f.speed;
        const seg = Math.max(0,Math.min(SEGMENTS,Math.round((f.x/window.innerWidth)*SEGMENTS)));
        const pileTop = h - snowHeight[seg];
        if (f.y >= pileTop - 2) {
          if(!melting) addSnow(f.x, f.pileRate);
          f.y = -10 - Math.random()*40; f.x = Math.random()*canvas.width;
        }
        c.globalAlpha = alpha * f.alpha;
        drawFlake(c,f);
      });
      c.globalAlpha = alpha;
      // Melt: shrink snow targets
      if(melting) { for(let i=0;i<=SEGMENTS;i++) snowTarget[i] = Math.max(0, snowTarget[i]*0.995); }
      for(let i=0;i<=SEGMENTS;i++) snowHeight[i]+=(snowTarget[i]-snowHeight[i])*SMOOTH_SPEED;
      settleSnow();
      // Update SVG
      snowSVG.style.opacity = alpha;
      const anySnow = snowHeight.some(v=>v>0.5);
      if(anySnow) {
        driftPath.setAttribute('d', buildSnowPath(snowHeight, 0));
        driftBack.setAttribute('d', buildSnowPath(snowHeight, -4));
        driftHighlight.setAttribute('d', buildHighlightPath(snowHeight));
      }
      c.restore();
    },
    cleanup() {
      window.removeEventListener('resize', resizeH);
      const svg = document.getElementById('snow-pile-svg');
      if(svg) svg.remove();
    }
  };
}
function startWinter() {
  const layer = createWinterLayer();
  function draw(){ ctx.clearRect(0,0,canvas.width,canvas.height); layer.draw(ctx,1); fxAnim=requestAnimationFrame(draw); }
  draw();
}


