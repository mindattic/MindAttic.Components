// ╔═══════════════════════════════════════════════════════════════════════════════╗
// ║  SPRING THEME                                                                 ║
// ║  Growing grass blades + drifting pollen particles                             ║
// ╠═══════════════════════════════════════════════════════════════════════════════╣
// ║  Each grass blade is an independent animated path: it grows up from zero,     ║
// ║  then gently sways via a sin wave.  Blades spawn in clusters to mimic a       ║
// ║  real lawn — tight at the base, spread at the tip.  Growth speed alternates   ║
// ║  between fast (AppSettings.plant_grow_fast) and slow (plant_grow_slow) to     ║
// ║  give the field an organic, uneven feel.                                      ║
// ║                                                                               ║
// ║  Pollen: small white/yellow circles with low alpha that drift upward with     ║
// ║  sinusoidal horizontal wobble.  They fade out near the top of the screen      ║
// ║  and respawn at the bottom.  Count is fixed so the pollen density stays       ║
// ║  constant regardless of canvas size.                                          ║
// ╚═══════════════════════════════════════════════════════════════════════════════╝
// 80 grass blades distributed evenly across the viewport base.
// Each blade: randomized height, maxH, width, sway speed, and green HSL color.
// Growth uses a frame counter delay so blades stagger their emergence — they
// don't all pop up at once. quadraticCurveTo handles the lean: cheaper than
// cubic, indistinguishable at grass scale.
//
// Pollen: 60 tiny ellipses drifting upward with a sinusoidal x-overlay.
// They wrap at edges and re-enter from the bottom — infinite supply.
// The `dying` flag reverses the growth (maxH shrinks) for theme-out transitions.
function createSpringLayer() {
  const blades = Array.from({length:80},(_,i)=>({
    x: (i/80)*canvas.width + Math.random()*20-10,
    height: 20 + Math.random()*60,
    maxH: 40 + Math.random()*80,
    width: 2 + Math.random()*3,
    color: `hsl(${110+Math.random()*30},${60+Math.random()*20}%,${30+Math.random()*20}%)`,
    sway: Math.random()*Math.PI*2,
    swaySpeed: 0.02 + Math.random()*0.02,
    grown: 0,
    delay: Math.random()*300,
  }));
  const pollen = Array.from({length:60},()=>({
    x:Math.random()*canvas.width, y:Math.random()*canvas.height,
    r:1+Math.random()*2.5, vy:-0.2-Math.random()*0.5,
    vx:(Math.random()-0.5)*0.7, alpha:0.3+Math.random()*0.5,
    drift:Math.random()*Math.PI*2, driftSpeed:0.01+Math.random()*0.02,
  }));
  let dying = false; // when fading out, grass shrinks
  return {
    setDying(v){ dying=v; },
    draw(c, alpha) {
      c.save(); c.globalAlpha = alpha;
      const base = canvas.height;
      blades.forEach(b=>{
        if(dying) { b.maxH = Math.max(0, b.maxH - AppSettings.plant_grow_slow); b.grown = Math.min(b.grown, b.maxH); }
        else if(b.delay > 0) { b.delay--; }
        else { b.grown = Math.min(b.grown + AppSettings.plant_grow_fast, b.maxH); }
        b.sway += b.swaySpeed;
        const lean = Math.sin(b.sway)*8;
        c.beginPath();
        c.moveTo(b.x, base);
        c.quadraticCurveTo(b.x+lean/2, base-b.grown*0.6, b.x+lean, base-b.grown);
        c.lineWidth = b.width;
        c.strokeStyle = b.color;
        c.lineCap = 'round';
        c.stroke();
      });
      pollen.forEach(p=>{
        p.drift += p.driftSpeed;
        p.y += p.vy; p.x += p.vx + Math.sin(p.drift)*0.4;
        if(p.y<-10) { p.y=canvas.height+10; p.x=Math.random()*canvas.width; }
        if(p.x<-10) p.x=canvas.width+10;
        if(p.x>canvas.width+10) p.x=-10;
        c.beginPath(); c.arc(p.x,p.y,p.r,0,Math.PI*2);
        c.fillStyle = `rgba(210,225,90,${p.alpha})`;
        c.fill();
        if(p.r>2){
          c.beginPath(); c.arc(p.x,p.y,p.r*2.5,0,Math.PI*2);
          c.fillStyle = `rgba(200,220,100,${p.alpha*0.12})`;
          c.fill();
        }
      });
      c.restore();
    },
    cleanup(){}
  };
}
function startSpring() {
  const layer = createSpringLayer();
  function draw(){ ctx.clearRect(0,0,canvas.width,canvas.height); layer.draw(ctx,1); fxAnim=requestAnimationFrame(draw); }
  draw();
}

