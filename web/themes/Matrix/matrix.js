// ╔═══════════════════════════════════════════════════════════════════════════════╗
// ║  MATRIX THEME                                                                 ║
// ║  Classic green column rain — the dimming-trail technique                      ║
// ╠═══════════════════════════════════════════════════════════════════════════════╣
// ║  Instead of clearing the canvas each frame, a semi-transparent black rect     ║
// ║  is painted over everything (fillStyle = rgba(0,0,0,0.05)).  Old characters   ║
// ║  dim gradually over ~20 frames rather than vanishing instantly — this one     ║
// ║  trick produces the entire "fading trail" effect at negligible cost.          ║
// ║                                                                               ║
// ║  Columns fall at randomised speeds so streams naturally desync over time      ║
// ║  even though they all started at the same y position.  The leading character  ║
// ║  is drawn brighter (#aaffaa bold) and the two characters behind it are        ║
// ║  drawn at progressively lower alpha to reinforce the depth gradient.          ║
// ║                                                                               ║
// ║  Characters are Katakana + a few digits — sampled randomly each frame so      ║
// ║  the column appears to be mutating as it falls.                               ║
// ╚═══════════════════════════════════════════════════════════════════════════════╝
function startMatrix() {
  const cols = Math.floor(canvas.width/16);
  const drops = Array.from({length:cols},()=>Math.random()*-50);
  const chars='アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホ01アイウエオ';
  function draw() {
    ctx.fillStyle='rgba(0,0,0,0.05)'; ctx.fillRect(0,0,canvas.width,canvas.height);
    drops.forEach((y,i)=>{
      const ch = chars[Math.floor(Math.random()*chars.length)];
      const x = i*16;
      // bright leading char
      ctx.fillStyle='#aaffaa'; ctx.font='bold 14px monospace';
      ctx.fillText(ch,x,y*16);
      // trail
      ctx.fillStyle='#00cc33'; ctx.font='13px monospace';
      ctx.fillText(chars[Math.floor(Math.random()*chars.length)],x,(y-1)*16);
      ctx.fillStyle='rgba(0,180,40,0.5)';
      ctx.fillText(chars[Math.floor(Math.random()*chars.length)],x,(y-2)*16);
      if(y*16>canvas.height && Math.random()>0.975) drops[i]=0;
      else drops[i]+=0.4;
    });
    fxAnim = requestAnimationFrame(draw);
  }
  draw();
}

