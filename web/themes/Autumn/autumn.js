// ╔═══════════════════════════════════════════════════════════════════════════════╗
// ║  AUTUMN THEME                                                                 ║
// ║  Falling leaves with wind physics and rotation                                ║
// ╠═══════════════════════════════════════════════════════════════════════════════╣
// ║  Leaves fall under a combined gravity + wind force.  Wind is computed by      ║
// ║  getWind() — a tanh-shaped oscillator that spends most of its time at the     ║
// ║  extremes (sustained gusts) and transitions sharply through zero.  Leaves     ║
// ║  also receive a small per-leaf random turbulence each frame, so each one      ║
// ║  drifts on a slightly different path even in the same wind field.             ║
// ║                                                                               ║
// ║  Each leaf has a tumble speed (angular velocity) that is damped by drag,      ║
// ║  so they spin fast when falling fast and slow when nearly hovering.  The      ║
// ║  leaf shape is a simple 4-point bezier — small enough to draw 40+ per frame   ║
// ║  without any performance impact.                                              ║
// ║                                                                               ║
// ║  Colours: a palette of 8 autumn hues (deep red → burnt orange → yellow).      ║
// ║  Each leaf picks one on spawn and keeps it for its lifetime.                  ║
// ╚═══════════════════════════════════════════════════════════════════════════════╝
// Leaves fall with per-leaf randomized terminal velocity, rotation rate, and
// horizontal drift. A global wind force adds a shared lateral component that
// shifts all leaves together, simulating gusts. Leaves wrap from bottom to top.
function createAutumnLayer() {
  const colors=['#c05c18','#e8762a','#d4380a','#f0943c','#8b3a10','#f5b942'];
  const leaves = Array.from({length:AppSettings.leaf_count},()=>({
    x:Math.random()*canvas.width, y:Math.random()*canvas.height*1.2-40,
    size:12+Math.random()*20, color:colors[Math.floor(Math.random()*colors.length)],
    vx:(Math.random()-0.5)*1.5, vy:0.8+Math.random()*1.2,
    rotation:Math.random()*Math.PI*2, rotSpeed:(Math.random()-0.5)*0.05,
    swing:Math.random()*Math.PI*2, swingSpeed:0.03+Math.random()*0.03,
    flipAngle:Math.random()*Math.PI*2, flipSpeed:0.02+Math.random()*0.04,
    tiltAngle:Math.random()*Math.PI*2, tiltSpeed:0.015+Math.random()*0.03,
    baseRotSpeed:(Math.random()-0.5)*0.05, baseFlipSpeed:0.02+Math.random()*0.04,
    baseTiltSpeed:0.015+Math.random()*0.03,
    spinActive:1, spinTimer: 60+Math.random()*200,
  }));
  function drawLeaf(c,l) {
    c.save(); c.translate(l.x,l.y); c.rotate(l.rotation);
    c.scale(Math.cos(l.flipAngle), Math.cos(l.tiltAngle));
    const s = l.size;
    c.beginPath(); c.moveTo(0,-s);
    c.bezierCurveTo(s*0.8,-s*0.5, s*0.9,s*0.2, 0,s*0.6);
    c.bezierCurveTo(-s*0.9,s*0.2, -s*0.8,-s*0.5, 0,-s);
    c.fillStyle=l.color; c.fill();
    c.beginPath(); c.moveTo(0,s*0.6); c.lineTo(0,s);
    c.strokeStyle='#8b4513'; c.lineWidth=1.5; c.stroke();
    c.restore();
  }
  let t = Math.random() * 1000;
  function getWind() {
    return Math.sin(t*0.0007)*0.8 + Math.sin(t*0.003)*0.4 + Math.sin(t*0.011+2.1)*0.2 + Math.pow(Math.sin(t*0.0004),5)*2;
  }
  return {
    draw(c, alpha) {
      c.save(); c.globalAlpha = alpha;
      t++;
      const wind = getWind();
      leaves.forEach(l=>{
        l.swing += l.swingSpeed;
        l.spinTimer--;
        if(l.spinTimer<=0){ l.spinActive=l.spinActive?0:1; l.spinTimer=80+Math.random()*300; }
        const st=l.spinActive?1:0, sm=l.spinActive?0.02:0.04;
        l.rotSpeed+=(l.baseRotSpeed*st-l.rotSpeed)*sm;
        l.flipSpeed+=(l.baseFlipSpeed*st-l.flipSpeed)*sm;
        l.tiltSpeed+=(l.baseTiltSpeed*st-l.tiltSpeed)*sm;
        l.flipAngle+=l.flipSpeed; l.tiltAngle+=l.tiltSpeed;
        l.x+=l.vx+Math.sin(l.swing)*0.8+wind; l.y+=l.vy;
        l.rotation+=l.rotSpeed+wind*0.01;
        l.flipAngle+=Math.abs(wind)*0.008;
        if(l.y>canvas.height+40){ l.y=-40; l.x=Math.random()*canvas.width; l.spinActive=1; l.spinTimer=60+Math.random()*200; }
        if(l.x<-60) l.x=canvas.width+30;
        if(l.x>canvas.width+60) l.x=-30;
        drawLeaf(c,l);
      });
      c.restore();
    },
    cleanup(){}
  };
}
function startAutumn() {
  const layer = createAutumnLayer();
  function draw(){ ctx.clearRect(0,0,canvas.width,canvas.height); layer.draw(ctx,1); fxAnim=requestAnimationFrame(draw); }
  draw();
}

