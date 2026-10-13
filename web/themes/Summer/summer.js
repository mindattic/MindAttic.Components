// ╔═══════════════════════════════════════════════════════════════════════════════╗
// ║  SUMMER THEME                                                                 ║
// ║  Wandering bees with a flower-claiming state machine                          ║
// ╠═══════════════════════════════════════════════════════════════════════════════╣
// ║  Each bee is a tiny state machine:                                            ║
// ║    IDLE ......... hover in place, wings beating at randomised speed           ║
// ║    FLYING ....... travel toward a target flower using sinusoidal steering     ║
// ║    LANDING ...... settle onto the flower centre, wings slow                   ║
// ║    FEEDING ...... stay on flower for a randomised duration then release       ║
// ║                                                                               ║
// ║  Flowers are procedurally drawn canvas paths (6-petal daisy silhouettes)      ║
// ║  scattered at random positions.  A flower can only be claimed by one bee      ║
// ║  at a time — other bees queue or pick a different flower, mimicking real      ║
// ║  foraging behaviour without any explicit collision detection.                 ║
// ║                                                                               ║
// ║  Bee body, wings, antennae, and stinger are all drawn with arc/bezier calls   ║
// ║  — no images, no sprites.                                                     ║
// ╚═══════════════════════════════════════════════════════════════════════════════╝
// 10 bees, 7 initial flowers. Each bee runs a 3-state machine:
//
//   'wander' → steers with randomized turn rate, biased away from center
//              (so bees stay in the left/right thirds and off the resume)
//   'approach' → locks onto nearest unclaimed bloom, flies toward it
//   'onflower' → lands, "harvests", then flower fades ('visited' phase)
//                and bee returns to 'wander'
//
// `claimedBy` on each flower prevents two bees from targeting the same bloom.
// New flowers spawn every 200 frames if fewer than 9 are active.
// Wings animate via a Math.sin on wobble — faster in flight, slower when landed.
// The bee body is an ellipse + 3 stripe ellipses + 2 wing ellipses. No images.
function createSummerLayer() {
  const W = ()=>canvas.width, H = ()=>canvas.height;
  const FLOWER_DETECT = 120;
  function flowerX() {
    return Math.random()<0.5
      ? 30 + Math.random()*(W()*0.28)
      : W()*0.72 + Math.random()*(W()*0.28-30);
  }
  const COLORS = ['#ff6b8a','#ff9f43','#ffd32a','#ff6348','#eccc68','#ff4757','#a29bfe','#fd79a8'];
  const flowers = [];
  function spawnFlower() {
    flowers.push({
      x: flowerX(), y: 60 + Math.random()*(H()-100),
      r: AppSettings.flower_min_size + Math.random() * (AppSettings.flower_max_size - AppSettings.flower_min_size),
      color: COLORS[Math.floor(Math.random()*COLORS.length)],
      alpha:0, phase:'fadein', age:0,
      petalAngle:Math.random()*Math.PI,
      claimedBy:null,
    });
  }
  for(let i=0;i<7;i++) spawnFlower();
  const bees = Array.from({length:10},()=>{
    const side = Math.random()<0.5;
    return {
      x: side ? Math.random()*W()*0.3 : W()*0.7+Math.random()*W()*0.3,
      y: 60+Math.random()*(H()-80),
      vx:(Math.random()-0.5)*2, vy:(Math.random()-0.5)*2,
      angle:Math.random()*Math.PI*2,
      turnRate:0, targetTurn:(Math.random()-0.5)*0.06,
      speed:2.2+Math.random()*1.8,
      wobble:0, wobbleS:0.13+Math.random()*0.1,
      size:10+Math.random()*6,
      wanderTimer:0,
      state:'wander', target:null, lx:0, ly:0, landTimer:0,
    };
  });
  let fc=0, noSpawn=false;

  function drawFlower(c,f) {
    if(f.alpha<=0) return;
    c.save(); c.globalAlpha*=f.alpha;
    const isH = f.claimedBy && f.phase==='visited';
    for(let i=0;i<6;i++){
      const a=f.petalAngle+(i/6)*Math.PI*2;
      c.beginPath();
      c.ellipse(f.x+Math.cos(a)*f.r*0.65,f.y+Math.sin(a)*f.r*0.65,f.r*0.5,f.r*0.3,a,0,Math.PI*2);
      c.fillStyle=isH?'#ccc':f.color; c.fill();
    }
    c.beginPath(); c.arc(f.x,f.y,f.r*0.38,0,Math.PI*2);
    c.fillStyle=isH?'#aaa':'#fff176'; c.fill();
    c.beginPath(); c.arc(f.x,f.y,f.r*0.22,0,Math.PI*2);
    c.fillStyle=isH?'#999':'#f9a825'; c.fill();
    c.restore();
  }
  function drawBee(c,b,facing) {
    c.save(); c.translate(b.x,b.y);
    if(b.state==='onflower') c.translate(0,Math.sin(b.wobble*3)*2);
    c.rotate(facing+Math.PI/2);
    c.beginPath(); c.ellipse(0,0,b.size*0.45,b.size*0.7,0,0,Math.PI*2);
    c.fillStyle='#f5c400'; c.fill();
    [-4,-1,2].forEach(off=>{
      c.beginPath(); c.fillStyle='rgba(20,10,0,0.75)';
      c.ellipse(0,off*b.size*0.18,b.size*0.44,b.size*0.13,0,0,Math.PI*2);
      c.fill();
    });
    const fs=b.state==='onflower'?1.5:5;
    [[-1,-1],[1,-1]].forEach(([sx,sy])=>{
      c.beginPath();
      c.ellipse(sx*b.size*0.65,sy*b.size*0.2,b.size*0.55,b.size*0.28,Math.PI*0.18,0,Math.PI*2);
      c.fillStyle=`rgba(200,240,255,${0.45+Math.sin(b.wobble*fs)*0.3})`; c.fill();
    });
    c.restore();
  }

  return {
    setNoSpawn(v){ noSpawn=v; flowers.forEach(f=>{ if(f.phase==='bloom') f.phase='visited'; }); },
    draw(c, alpha) {
      c.save(); c.globalAlpha = alpha;
      fc++;
      if(!noSpawn && fc%200===0 && flowers.filter(f=>f.phase!=='done').length<9) spawnFlower();
      for(let i=flowers.length-1;i>=0;i--) if(flowers[i].phase==='done') flowers.splice(i,1);
      flowers.forEach(f=>{
        f.age++;
        if(f.phase==='fadein'){ f.alpha=Math.min(1,f.alpha+0.012); if(f.alpha>=1) f.phase='bloom'; }
        else if(f.phase==='visited'){ f.alpha=Math.max(0,f.alpha-0.008); if(f.alpha<=0) f.phase='done'; }
        drawFlower(c,f);
      });
      bees.forEach(b=>{
        b.wobble+=b.wobbleS;
        if(b.state==='wander') {
          b.wanderTimer++;
          if(b.wanderTimer>40+Math.random()*80) {
            b.targetTurn = (Math.random()-0.5)*0.08;
            b.wanderTimer=0;
            const inCenter = b.x>W()*0.3 && b.x<W()*0.7;
            if(inCenter) b.targetTurn += (b.x<W()*0.5 ? -0.02 : 0.02);
          }
          b.turnRate += (b.targetTurn - b.turnRate) * 0.08;
          b.angle += b.turnRate;
          b.x += Math.cos(b.angle)*b.speed;
          b.y += Math.sin(b.angle)*b.speed;
          if(b.x<40) b.angle += 0.05;
          else if(b.x>W()-40) b.angle -= 0.05;
          if(b.y<40) b.angle += (Math.cos(b.angle)>0?0.04:-0.04);
          else if(b.y>H()-40) b.angle += (Math.cos(b.angle)>0?-0.04:0.04);
          b.x=Math.max(10,Math.min(W()-10,b.x));
          b.y=Math.max(10,Math.min(H()-10,b.y));
          if(!noSpawn) {
            const blooms=flowers.filter(f=>f.phase==='bloom'&&f.alpha>0.7&&!f.claimedBy);
            let nearest=null, nearDist=Infinity;
            blooms.forEach(f=>{ const d=Math.hypot(f.x-b.x,f.y-b.y); if(d<FLOWER_DETECT&&d<nearDist){nearDist=d;nearest=f;} });
            if(nearest){ nearest.claimedBy=b; b.target=nearest; b.state='chase'; }
          }
          drawBee(c,b, b.angle);
        } else if(b.state==='chase') {
          const tf=b.target;
          if(!tf||tf.phase==='done'||(tf.claimedBy&&tf.claimedBy!==b)){ b.state='wander'; b.target=null; return; }
          const dx=tf.x-b.x, dy=tf.y-b.y, dist=Math.hypot(dx,dy);
          const desiredAngle=Math.atan2(dy,dx);
          let da=desiredAngle-b.angle; while(da>Math.PI)da-=Math.PI*2; while(da<-Math.PI)da+=Math.PI*2;
          const closeness = 1-Math.min(1,dist/FLOWER_DETECT);
          b.angle+=da*(0.06+0.44*closeness*closeness);
          const chaseSpeed=1.8+Math.min(2,dist*0.012);
          b.x+=Math.cos(b.angle)*chaseSpeed; b.y+=Math.sin(b.angle)*chaseSpeed;
          drawBee(c,b,b.angle);
          if(dist<8){ b.state='land'; b.lx=tf.x; b.ly=tf.y; b.landTimer=0; b.landAngle=b.angle; tf.phase='visited'; }
        } else if(b.state==='land') {
          b.x=b.lx+Math.sin(b.wobble*1.8)*2.5; b.y=b.ly; b.landTimer++;
          drawBee(c,b,b.landAngle);
          if(b.landTimer>140){ b.state='liftoff'; b.landTimer=0; }
        } else if(b.state==='liftoff') {
          if(b.landTimer===0){ b.liftAngle=b.angle+(Math.random()<0.5?1:-1)*(0.8+Math.random()*1.2); b.turnRate=0; b.targetTurn=(Math.random()-0.5)*0.04; }
          const t=Math.min(1,b.landTimer/40);
          let da=b.liftAngle-b.angle; while(da>Math.PI)da-=Math.PI*2; while(da<-Math.PI)da+=Math.PI*2;
          b.angle+=da*0.06;
          b.x+=Math.cos(b.angle)*b.speed*(0.3+t*0.7); b.y+=Math.sin(b.angle)*b.speed*(0.3+t*0.7);
          b.landTimer++;
          drawBee(c,b,b.angle);
          if(b.landTimer>60){ b.state='wander'; b.target=null; b.targetTurn=(Math.random()-0.5)*0.06; }
        }
      });
      c.restore();
    },
    cleanup(){}
  };
}
function startSummer() {
  const layer = createSummerLayer();
  function draw(){ ctx.clearRect(0,0,canvas.width,canvas.height); layer.draw(ctx,1); fxAnim=requestAnimationFrame(draw); }
  draw();
}


