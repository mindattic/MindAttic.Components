// ╔═══════════════════════════════════════════════════════════════════════════════╗
// ║  CYBERPUNK THEME                                                              ║
// ║  Neon acid rain · electric arcs · sparks · dystopian city skyline             ║
// ╠═══════════════════════════════════════════════════════════════════════════════╣
// ║  Five layered effects, drawn back-to-front every frame:                       ║
// ║                                                                               ║
// ║  City skyline: three OffscreenCanvas layers pre-baked at startup.  Each       ║
// ║  building is one of 5 procedural archetypes (supertall, pyramid, dome,        ║
// ║  industrial stack, standard tower) with randomised proportions.  Windows      ║
// ║  are drawn with a flicker system — each has a random "reliability" value      ║
// ║  that determines how often its neon light cuts out.                           ║
// ║                                                                               ║
// ║  Searchlights: 4 cone beams sweeping from rooftop anchors, drawn behind       ║
// ║  buildings so they cast light onto the sky without lighting the buildings.    ║
// ║                                                                               ║
// ║  Acid rain: 160 streaks, angled ~38° from vertical (consistent with the       ║
// ║  eternal crosswind of a dystopian city).  Hot-pink, cyan, and acid-green.     ║
// ║                                                                               ║
// ║  Electric arcs: jagged polylines (8-14 segments) between random endpoints,    ║
// ║  emitted at ~0.8% chance/frame.  Each arc flickers for ~20 frames and         ║
// ║  showers sparks from its tip on birth.                                        ║
// ║                                                                               ║
// ║  Neon fog + CRT scan-lines applied as the final compositing step.             ║
// ╚═══════════════════════════════════════════════════════════════════════════════╝
// Five layered effects drawn to canvas each frame:
//
//   City skyline: pre-baked offscreen canvas of darkened skyscrapers with
//     glowing magenta/cyan windows. Scrolls slowly left for parallax depth.
//
//   Neon rain: 160 streaks in hot-pink, cyan, acid-green. Each has randomized
//     speed, width, length, and alpha. Gradient from transparent→opaque so
//     rain trails feel like streaks, not solid lines.
//
//   Sparks: short-lived particles that erupt randomly or from arc endpoints.
//     They obey gravity and fade linearly. Drawn with shadowBlur for a glow halo.
//
//   Electric arcs: occasional jagged polylines (lightning style) between two
//     random points. Emitted at ~0.8% chance per frame; 8–14 segments each.
//     Flicker out over ~20 frames. Emit sparks at their endpoint on birth.
//
//   Neon fog: subtle magenta vignette at top + cyan reflection glow at bottom
//     (the classic wet-street look). Applied last so it tints everything.
//
// CRT scan-lines (1px gap every 3px) applied as the very last step.
function startCyberpunk() {
  const W = () => canvas.width, H = () => canvas.height;
  let frame = 0;

  const cpSearchlights = Array.from({length:4}, () => ({
    x: Math.random(),
    angle: -0.4 + Math.random()*0.8,
    speed: (Math.random()<0.5?1:-1) * (0.003 + Math.random()*0.005),
    range: 0.35 + Math.random()*0.35,
    center: -0.2 + Math.random()*0.4,
    alpha: 0.06 + Math.random()*0.07,
    color: Math.random()<0.5 ? '200,220,255' : '255,200,255',
    phase: Math.random() * Math.PI * 2,
  }));

  // ── Acid rain: uniform cyan, severe angle (≈38° from vertical) ────────────
  // dx/dy ratio sets the lean. tan(38°)≈0.78, so horizontal drift is 78% of
  // vertical speed — looks like a hard wind-driven storm, not gentle drizzle.
  const RAIN_COLOR = '#00ffff';
  const rain = Array.from({length:320}, () => ({
    x: Math.random() * ((canvas.width||1920) + 300) - 150,
    y: Math.random() * (canvas.height||900),
    len: 14 + Math.random() * 32,
    speed: 12 + Math.random() * 14,  // faster
    angle: 0.18 + Math.random() * 0.22, // per-drop angle variation (erratic pattern)
    alpha: 0.18 + Math.random() * 0.42,
    width: 0.5 + Math.random() * 1.0,
  }));

  // ── Electric arcs ──────────────────────────────────────────────────────────
  let arcs = [];
  function emitArc() {
    const w=W(), h=H();
    const sx = Math.random()*w;
    const sy = Math.random()*h*0.08; // spawn near top of screen
    const ex = sx + (Math.random()-0.5)*240;
    const ey = sy + 80 + Math.random()*280;
    const color = Math.random()<0.5 ? '#ff00ff' : '#00ffff';
    const steps = 8 + Math.floor(Math.random()*8);
    const segs = [];
    for (let i=0; i<=steps; i++) {
      const t=i/steps;
      const jitter = 38*(1-Math.abs(t-0.5)*1.8);
      segs.push([
        sx+(ex-sx)*t + (Math.random()-0.5)*jitter,
        sy+(ey-sy)*t + (Math.random()-0.5)*jitter*0.45,
      ]);
    }
    arcs.push({segs, color, life:1.0});
  }

  // ── City skyline offscreen canvas ──────────────────────────────────────────
  // Building types: pyramids, domes, mega-corp supertalls, industrial stacks,
  // and asymmetric chamfered towers. A separate foreground canvas enables
  // parallax by scrolling at a different rate than the background.
  //
  // The offscreen canvas is w*2 wide — double the viewport — so the seamless
  // scroll works by drawing the canvas twice side-by-side. As scroll offset
  // advances, one copy exits left while the other enters right. Wrapping via
  // modulo keeps the offset in bounds. It's the same trick used in infinite
  // runners and old-school platformer parallax since the SNES era.
  //
  // Buildings are generated once at startup (and on resize) via a left-to-right
  // walk with a random roll per building. Procedural, not authored.
  //
  // Alternative: pre-author a handful of distinct skyline "segments" and tile
  // them — more control over aesthetic, less variety. SVG paths would also
  // work and be infinitely scalable, but adding parallax + animated windows to
  // an SVG scroll gets complex fast.
  //
  // The window-flicker system tracks pixel positions of every rendered window
  // and randomly darkens them each frame. Is there a lighter-weight approach
  // that achieves the same effect? Almost certainly — what would you try?
  function buildSkyline(w, h) {
    const oc = document.createElement('canvas');
    oc.width = w*2; oc.height = h;
    const c = oc.getContext('2d');

    // Sky left transparent so searchlights drawn on main canvas show through

    // Collected window positions for dynamic flicker in the main draw loop
    const wins = [];
    // Tall building rooftop anchors for searchlights
    const rooftops = [];

    // Helper: neon window grid on a rectangular face
    function drawWindows(c, bx, by, bw, bh) {
      for (let wy=by+10; wy<by+bh-8; wy+=13) {
        for (let wx=bx+6; wx<bx+bw-6; wx+=15) {
          if (Math.random()<0.42) {
            const cyan = Math.random()<0.4;
            const a = 0.4 + Math.random()*0.5;
            const wc = cyan ? `rgba(0,220,255,${a})` : `rgba(255,0,200,${a})`;
            c.fillStyle = wc;
            c.fillRect(wx, wy, 5, 7);
            const g = c.createRadialGradient(wx+2,wy+3,0, wx+2,wy+3,11);
            g.addColorStop(0, cyan?`rgba(0,200,255,0.13)`:`rgba(255,0,200,0.13)`);
            g.addColorStop(1,'rgba(0,0,0,0)');
            c.fillStyle=g; c.fillRect(wx-7,wy-5,20,18);
            wins.push({x:wx, y:wy});
          }
        }
      }
    }

    // Helper: building silhouette fill color
    // Buildings stay in the purple family — each picks a slightly different shade
    // (hue 255–285, lightness 2–9%) so they read as the same material in different
    // lighting rather than wildly different substances.
    const MAT_HUES = [255, 260, 265, 270, 275, 280, 285];
    let bgHue = 270;
    function bCol(c) { c.fillStyle=`hsl(${bgHue},${28+Math.random()*18}%,${3+Math.random()*6}%)`; }
    function bStroke(c,a=0.5) { c.strokeStyle=`rgba(${Math.random()<0.5?'255,0,200':'0,200,255'},${a})`; c.lineWidth=0.7; }
    // Occasional empty lot — ~20% chance of a wider gap so layers don't look like a solid wall
    function gap() { return Math.random() < 0.20 ? 30 + Math.random()*60 : 0; }

    let x = 0;
    while (x < oc.width) {
      bgHue = MAT_HUES[Math.floor(Math.random()*MAT_HUES.length)];
      const roll = Math.random();

      // ── TYPE: Pyramid / zigggurat ──────────────────────────────────────────
      if (roll < 0.16) {
        const bw = 90 + Math.random()*110;
        const bh = h*0.28 + Math.random()*h*0.28;
        const by = h - bh;
        const steps = 5 + Math.floor(Math.random()*5);
        bCol(c);
        // Solid triangle base
        c.beginPath();
        c.moveTo(x, h);
        c.lineTo(x+bw/2, by);
        c.lineTo(x+bw, h);
        c.closePath(); c.fill();
        // Stepped faces
        bStroke(c,0.4);
        for (let s=1; s<steps; s++) {
          const t = s/steps;
          const sw = bw*(1-t);
          const sy2 = h - bh*t;
          c.beginPath();
          c.moveTo(x+(bw-sw)/2, sy2);
          c.lineTo(x+(bw+sw)/2, sy2);
          c.stroke();
        }
        // Step-edge warning lights — small amber dots at each tier, registered for flicker
        for (let s=1; s<steps; s++) {
          const t = s/steps;
          const sw2 = bw*(1-t);
          const sy2 = h - bh*t;
          const lx = Math.round(x+(bw-sw2)/2) - 2;
          const rx = Math.round(x+(bw+sw2)/2) - 2;
          c.fillStyle = 'rgba(255,110,0,0.88)';
          c.fillRect(lx, sy2-4, 4, 4);
          c.fillRect(rx, sy2-4, 4, 4);
          wins.push({x:lx, y:sy2-4});
          wins.push({x:rx, y:sy2-4});
        }
        // Glowing capstone
        const g = c.createRadialGradient(x+bw/2, by, 0, x+bw/2, by, 18);
        g.addColorStop(0,'rgba(255,0,200,0.7)'); g.addColorStop(1,'rgba(0,0,0,0)');
        c.fillStyle=g; c.beginPath(); c.arc(x+bw/2, by, 18, 0, Math.PI*2); c.fill();
        // Capstone registered for flicker too
        wins.push({x: Math.round(x+bw/2)-3, y: by-3});
        x += bw + 8 + Math.random()*15 + gap();

      // ── TYPE: Dome structure ───────────────────────────────────────────────
      } else if (roll < 0.23) {
        const bw = 70 + Math.random()*80;
        const baseH = h*0.1 + Math.random()*h*0.12;
        const domeR = bw * 0.52 - 0.6; // subtract half stroke-width so arc fits flush
        const basY = h - baseH;
        const domeCY = basY - domeR*0.6;
        // Base block
        bCol(c); c.fillRect(x, basY, bw, baseH);
        drawWindows(c, x, basY, bw, baseH);
        // Dome silhouette
        const dcx = x+bw/2;
        bCol(c);
        c.beginPath(); c.arc(dcx, basY, domeR, Math.PI, 0); c.closePath(); c.fill();
        // Geodesic grid (clipped to dome shape)
        c.save();
        c.beginPath(); c.arc(dcx, basY, domeR-0.5, Math.PI, 0); c.closePath(); c.clip();
        // Horizontal latitude rings
        c.lineWidth = 0.7;
        const rings = 4;
        for (let ri=1; ri<rings; ri++) {
          const t = ri/rings;
          const ry = basY - t*domeR;
          const hw = Math.sqrt(Math.max(0, domeR*domeR - (t*domeR)*(t*domeR)));
          c.strokeStyle = `rgba(0,210,255,${0.12+t*0.08})`;
          c.beginPath(); c.moveTo(dcx-hw, ry); c.lineTo(dcx+hw, ry); c.stroke();
        }
        // Diagonal struts — two crossing directions → triangulated panels
        c.strokeStyle = 'rgba(0,180,220,0.13)'; c.lineWidth = 0.6;
        const step = domeR * 0.38;
        for (let s=-4; s<=4; s++) {
          const ox = s * step;
          // Left-leaning (↗)
          c.beginPath(); c.moveTo(dcx+ox-domeR, basY); c.lineTo(dcx+ox+domeR, basY-domeR*1.8); c.stroke();
          // Right-leaning (↖)
          c.beginPath(); c.moveTo(dcx+ox+domeR, basY); c.lineTo(dcx+ox-domeR, basY-domeR*1.8); c.stroke();
        }
        c.restore();
        // Dome neon outline — kept dim so it reads as silhouette, not bright ring
        c.strokeStyle='rgba(0,160,200,0.18)'; c.lineWidth=1.0;
        c.beginPath(); c.arc(dcx, basY, domeR, Math.PI, 0); c.stroke();
        x += bw + 6 + Math.random()*12 + gap();

      // ── TYPE: Industrial stack / factory ──────────────────────────────────
      } else if (roll < 0.30) {
        const numStacks = 1 + Math.floor(Math.random()*3);
        const baseW = 40 + numStacks*25 + Math.random()*40;
        const baseH = h*0.08 + Math.random()*h*0.15;
        bCol(c); c.fillRect(x, h-baseH, baseW, baseH);
        // Stacks
        for (let s=0; s<numStacks; s++) {
          const sx2 = x + 12 + s*(baseW/numStacks) + Math.random()*8;
          const sw = 8 + Math.random()*10;
          const sh = baseH*0.55 + Math.random()*h*0.07;
          bCol(c); c.fillRect(sx2, h-baseH-sh, sw, sh);
          // Rim light at top — registered for flicker
          c.fillStyle='rgba(255,80,0,0.7)';
          c.fillRect(sx2, h-baseH-sh, sw, 3);
          wins.push({x: Math.round(sx2 + sw/2) - 3, y: h-baseH-sh});
        }
        x += baseW + 6 + Math.random()*10;

      // ── TYPE: Mega-corp supertall ──────────────────────────────────────────
      } else if (roll < 0.40) {
        const bw = 38 + Math.random()*36;
        const bh = h*0.5 + Math.random()*h*0.3;
        const by = h - bh;
        // Main shaft
        bCol(c); c.fillRect(x, by, bw, bh);
        drawWindows(c, x, by, bw, bh);
        // Distinctive crown: X-brace + orb
        const cx2 = x+bw/2, cY = by-12;
        c.strokeStyle='rgba(255,0,200,0.65)'; c.lineWidth=1.8;
        c.beginPath(); c.moveTo(x, cY); c.lineTo(x+bw, cY-20); c.stroke();
        c.beginPath(); c.moveTo(x+bw, cY); c.lineTo(x, cY-20); c.stroke();
        c.beginPath(); c.moveTo(cx2, cY-20); c.lineTo(cx2, cY-50-Math.random()*30); c.stroke();
        // Orb
        const og = c.createRadialGradient(cx2, cY-50, 0, cx2, cY-50, 14);
        og.addColorStop(0,'rgba(255,0,200,0.9)'); og.addColorStop(1,'rgba(0,0,0,0)');
        c.fillStyle=og; c.beginPath(); c.arc(cx2, cY-50, 14, 0, Math.PI*2); c.fill();
        rooftops.push({x: cx2, y: cY-50});
        x += bw + 6 + Math.random()*12 + gap();

      // ── TYPE: Standard tower (most common) — asymmetric chamfered variants ──
      } else {
        const bw = 26 + Math.random()*70;
        const bh = h*0.1 + Math.random()*h*0.48;
        const by = h - bh;
        // Randomly clip a top corner for a futuristic asymmetric profile
        const clip = Math.random();
        bCol(c);
        c.beginPath();
        if (clip < 0.28) {
          // Top-left corner clipped
          const cut = bw * (0.18 + Math.random()*0.28);
          c.moveTo(x+cut, by); c.lineTo(x+bw, by);
          c.lineTo(x+bw, h); c.lineTo(x, h); c.lineTo(x, by+cut*0.8);
        } else if (clip < 0.52) {
          // Top-right corner clipped
          const cut = bw * (0.18 + Math.random()*0.28);
          c.moveTo(x, by); c.lineTo(x+bw-cut, by);
          c.lineTo(x+bw, by+cut*0.8); c.lineTo(x+bw, h); c.lineTo(x, h);
        } else if (clip < 0.64) {
          // Both top corners clipped (trapezoid top)
          const cl = bw*(0.10+Math.random()*0.18), cr = bw*(0.10+Math.random()*0.18);
          c.moveTo(x+cl, by); c.lineTo(x+bw-cr, by);
          c.lineTo(x+bw, by+cr*0.7); c.lineTo(x+bw, h);
          c.lineTo(x, h); c.lineTo(x, by+cl*0.7);
        } else {
          // Plain rectangle
          c.rect(x, by, bw, bh);
        }
        c.closePath(); c.fill();
        // Neon edge trace on clipped faces
        if (clip < 0.64) {
          bStroke(c, 0.35); c.stroke();
        }
        drawWindows(c, x, by, bw, bh);
        if (bh > h*0.35) rooftops.push({x: x+bw/2, y: by});
        // Setback tier on taller buildings
        if (bh > h*0.3 && Math.random()<0.5) {
          const tw = bw*0.65, th = bh*0.35;
          const tx = x+(bw-tw)/2, ty = by;
          bCol(c); c.fillRect(tx, ty, tw, th);
          drawWindows(c, tx, ty, tw, th);
        }
        // Antennas — most towers get one or more
        if (Math.random()<0.72) {
          const numAnt = Math.random()<0.35 ? 2 + Math.floor(Math.random()*2) : 1;
          for (let a=0; a<numAnt; a++) {
            const ax = x + bw*0.2 + Math.random()*bw*0.6;
            const aLen = 10 + Math.random()*40;
            c.strokeStyle=`rgba(${Math.random()<0.5?'255,0,200':'0,200,255'},${0.4+Math.random()*0.4})`;
            c.lineWidth = 0.8 + Math.random()*0.6;
            c.beginPath(); c.moveTo(ax, by); c.lineTo(ax, by-aLen); c.stroke();
            // Blinking tip
            if (Math.random()<0.6) {
              c.fillStyle='rgba(255,30,80,0.9)';
              c.beginPath(); c.arc(ax, by-aLen, 1.5, 0, Math.PI*2); c.fill();
            }
          }
        }
        x += bw + 2 + Math.random()*8 + gap();
      }
    }

    // Wet-street neon reflection at base
    const sg = c.createLinearGradient(0,h*0.83,0,h);
    sg.addColorStop(0,'rgba(0,0,0,0)');
    sg.addColorStop(0.35,'rgba(80,0,100,0.16)');
    sg.addColorStop(0.7,'rgba(0,60,100,0.13)');
    sg.addColorStop(1,'rgba(10,0,20,0.38)');
    c.fillStyle=sg; c.fillRect(0,h*0.83,oc.width,h*0.17);

    // ── Foreground: closer buildings on separate canvas (parallax layer) ──────
    const fgOC = document.createElement('canvas');
    fgOC.width = oc.width; fgOC.height = h;
    const cf = fgOC.getContext('2d');
    const fgRects = [];
    let fx = -10;
    while (fx < fgOC.width) {
      const bw = 18 + Math.random()*55;
      const bh = h*0.04 + Math.random()*h*0.17;
      const by = h - bh;
      const fgHue = MAT_HUES[Math.floor(Math.random()*MAT_HUES.length)];
      cf.fillStyle = `hsl(${fgHue},${18+Math.random()*14}%,${7+Math.random()*8}%)`;
      // Polygon silhouette — same chamfered corner variants as background towers
      const fgClip = Math.random();
      cf.beginPath();
      if (fgClip < 0.28) {
        const cut = bw * (0.18 + Math.random()*0.28);
        cf.moveTo(fx+cut, by); cf.lineTo(fx+bw, by);
        cf.lineTo(fx+bw, h); cf.lineTo(fx, h); cf.lineTo(fx, by+cut*0.8);
      } else if (fgClip < 0.52) {
        const cut = bw * (0.18 + Math.random()*0.28);
        cf.moveTo(fx, by); cf.lineTo(fx+bw-cut, by);
        cf.lineTo(fx+bw, by+cut*0.8); cf.lineTo(fx+bw, h); cf.lineTo(fx, h);
      } else if (fgClip < 0.64) {
        const cl = bw*(0.10+Math.random()*0.18), cr = bw*(0.10+Math.random()*0.18);
        cf.moveTo(fx+cl, by); cf.lineTo(fx+bw-cr, by);
        cf.lineTo(fx+bw, by+cr*0.7); cf.lineTo(fx+bw, h);
        cf.lineTo(fx, h); cf.lineTo(fx, by+cl*0.7);
      } else {
        cf.rect(fx, by, bw, bh);
      }
      cf.closePath(); cf.fill();
      if (fgClip < 0.64 && Math.random() < 0.5) {
        cf.strokeStyle = Math.random()<0.5 ? 'rgba(255,0,200,0.25)' : 'rgba(0,200,255,0.25)';
        cf.lineWidth = 0.8;
        cf.stroke();
      }
      // Windows on foreground buildings
      drawWindows(cf, fx, by, bw, bh);
      // Antennas on foreground buildings — most get 1-3
      if (Math.random()<0.82) {
        const numAnt = Math.random()<0.45 ? 2 + Math.floor(Math.random()*2) : 1;
        for (let a=0; a<numAnt; a++) {
          const ax = fx + bw*0.1 + Math.random()*bw*0.8;
          const aLen = 5 + Math.random()*22;
          cf.strokeStyle=`rgba(${Math.random()<0.5?'255,0,200':'0,200,255'},${0.30+Math.random()*0.35})`;
          cf.lineWidth = 0.7 + Math.random()*0.5;
          cf.beginPath(); cf.moveTo(ax, by); cf.lineTo(ax, by-aLen); cf.stroke();
          if (Math.random()<0.5) {
            cf.fillStyle='rgba(255,30,80,0.85)';
            cf.beginPath(); cf.arc(ax, by-aLen, 1.2, 0, Math.PI*2); cf.fill();
          }
        }
      }
      fgRects.push({x:fx, y:by, w:bw, h:bh});
      fx += bw + 1 + Math.random()*5 + gap();
    }

    // ── Medium: mid-distance layer between bg and fg (parallax 0.25) ─────────
    const mdOC = document.createElement('canvas');
    mdOC.width = oc.width; mdOC.height = h;
    const cm = mdOC.getContext('2d');
    let mx = -5;
    while (mx < mdOC.width) {
      const bw = 22 + Math.random()*62;
      const bh = h*0.08 + Math.random()*h*0.26;
      const by = h - bh;
      const mdHue = MAT_HUES[Math.floor(Math.random()*MAT_HUES.length)];
      cm.fillStyle = `hsl(${mdHue},${22+Math.random()*16}%,${5+Math.random()*6}%)`;
      const mdClip = Math.random();
      cm.beginPath();
      if (mdClip < 0.28) {
        const cut = bw*(0.18+Math.random()*0.28);
        cm.moveTo(mx+cut,by); cm.lineTo(mx+bw,by); cm.lineTo(mx+bw,h); cm.lineTo(mx,h); cm.lineTo(mx,by+cut*0.8);
      } else if (mdClip < 0.52) {
        const cut = bw*(0.18+Math.random()*0.28);
        cm.moveTo(mx,by); cm.lineTo(mx+bw-cut,by); cm.lineTo(mx+bw,by+cut*0.8); cm.lineTo(mx+bw,h); cm.lineTo(mx,h);
      } else if (mdClip < 0.64) {
        const cl = bw*(0.10+Math.random()*0.18), cr = bw*(0.10+Math.random()*0.18);
        cm.moveTo(mx+cl,by); cm.lineTo(mx+bw-cr,by); cm.lineTo(mx+bw,by+cr*0.7); cm.lineTo(mx+bw,h); cm.lineTo(mx,h); cm.lineTo(mx,by+cl*0.7);
      } else {
        cm.rect(mx, by, bw, bh);
      }
      cm.closePath(); cm.fill();
      if (mdClip < 0.64 && Math.random() < 0.4) {
        cm.strokeStyle = Math.random()<0.5 ? 'rgba(255,0,200,0.20)' : 'rgba(0,200,255,0.20)';
        cm.lineWidth = 0.7; cm.stroke();
      }
      drawWindows(cm, mx, by, bw, bh);
      if (Math.random() < 0.70) {
        const numAnt = Math.random()<0.35 ? 2+Math.floor(Math.random()*2) : 1;
        for (let a=0; a<numAnt; a++) {
          const ax = mx + bw*0.1 + Math.random()*bw*0.8;
          const aLen = 6 + Math.random()*28;
          cm.strokeStyle=`rgba(${Math.random()<0.5?'255,0,200':'0,200,255'},${0.28+Math.random()*0.32})`;
          cm.lineWidth = 0.7+Math.random()*0.5;
          cm.beginPath(); cm.moveTo(ax,by); cm.lineTo(ax,by-aLen); cm.stroke();
          if (Math.random()<0.5) {
            cm.fillStyle='rgba(255,30,80,0.85)';
            cm.beginPath(); cm.arc(ax,by-aLen,1.3,0,Math.PI*2); cm.fill();
          }
        }
      }
      mx += bw + 1 + Math.random()*7 + gap();
    }

    return {bgOC: oc, mdOC, fgOC, wins, fgRects, rooftops};
  }

  let skyData = buildSkyline(W(), H());
  let skyBG = skyData.bgOC, skyMD = skyData.mdOC, skyFG = skyData.fgOC;
  let winStates = skyData.wins.map(() => ({off:false, timer:500 + Math.floor(Math.random()*10000)}));
  let bgScroll = 0, mdScroll = 0, fgScroll = 0;

  // Anchor each searchlight to a tall building rooftop
  function assignSearchlightRooftops() {
    const rts = skyData.rooftops;
    if (!rts.length) return;
    rts.sort((a,b) => a.y - b.y); // lowest y = tallest
    const pool = rts.slice(0, Math.min(rts.length, 12));
    cpSearchlights.forEach(sl => {
      const rt = pool[Math.floor(Math.random()*pool.length)];
      sl.rtX = rt.x; sl.rtY = rt.y;
    });
  }
  assignSearchlightRooftops();

  // ── Drifting toxic clouds (top third of screen) ────────────────────────────
  // Low, fast-moving smog banks — sickly purple-green, layered opacity.
  // Each cloud is a cluster of overlapping ellipses on a shared x-offset.
  const clouds = Array.from({length:18}, (_,i) => ({
    x: Math.random() * (canvas.width||1920),
    y: 10 + Math.random() * (canvas.height||900) * 0.28,
    speed: 0.12 + Math.random() * 0.28,
    scale: 0.6 + Math.random() * 1.2,
    alpha: 0.06 + Math.random() * 0.10,
    // Each cloud is a small array of blob offsets
    blobs: Array.from({length:4+Math.floor(Math.random()*5)}, () => ({
      dx: (Math.random()-0.5)*160, dy: (Math.random()-0.5)*28,
      rx: 60+Math.random()*160, ry: 18+Math.random()*32,
    })),
    // Hue: toxic green-purple range
    hue: 260 + Math.random()*80,
  }));

  function drawClouds(w, h) {
    for (const cl of clouds) {
      cl.x += cl.speed;
      if (cl.x > w + 200) cl.x = -200;
      ctx.save();
      ctx.globalAlpha = cl.alpha;
      for (const b of cl.blobs) {
        const g = ctx.createRadialGradient(
          cl.x+b.dx, cl.y+b.dy, 0,
          cl.x+b.dx, cl.y+b.dy, b.rx*cl.scale
        );
        g.addColorStop(0, `hsla(${cl.hue},60%,28%,1)`);
        g.addColorStop(0.5, `hsla(${cl.hue},50%,18%,0.6)`);
        g.addColorStop(1, 'rgba(0,0,0,0)');
        ctx.fillStyle = g;
        ctx.beginPath();
        ctx.ellipse(cl.x+b.dx, cl.y+b.dy, b.rx*cl.scale, b.ry*cl.scale, 0, 0, Math.PI*2);
        ctx.fill();
      }
      ctx.restore();
    }
  }

  function loop() {
    if(document.documentElement.getAttribute('data-theme')!=='cyberpunk') return;
    const w=W(), h=H();
    frame++;

    // Rebuild skyline on resize
    if(skyBG.height !== h) {
      skyData = buildSkyline(w, h);
      skyBG = skyData.bgOC; skyMD = skyData.mdOC; skyFG = skyData.fgOC;
      winStates = skyData.wins.map(() => ({off:false, timer:500 + Math.floor(Math.random()*10000)}));
      assignSearchlightRooftops();
    }

    // ── Dark sky base ──
    ctx.fillStyle = '#03000a';
    ctx.fillRect(0, 0, w, h);

    // ── Searchlights — cone beams from rooftop anchors, behind buildings ──
    for (const sl of cpSearchlights) {
      sl.angle += sl.speed + Math.sin(frame * 0.017 + sl.phase) * 0.0012
                           + Math.sin(frame * 0.041 + sl.phase * 2.3) * 0.0006;
      if (sl.angle > sl.center+sl.range || sl.angle < sl.center-sl.range) sl.speed *= -1;
      // Screen x: rooftop follows bg scroll
      let sx = sl.rtX - bgScroll;
      if (sx < -60) sx += skyBG.width;
      if (sx > w + 60) sx -= skyBG.width;
      const sy = sl.rtY;
      const len = h * 0.7;
      const spread = 0.055;
      const ex = sx + Math.sin(sl.angle) * len;
      const ey = sy - len * Math.cos(sl.angle);
      const ex1 = sx + Math.sin(sl.angle - spread) * len;
      const ey1 = sy - len * Math.cos(sl.angle - spread);
      const ex2 = sx + Math.sin(sl.angle + spread) * len;
      const ey2 = sy - len * Math.cos(sl.angle + spread);
      const slg = ctx.createLinearGradient(sx, sy, ex, ey);
      slg.addColorStop(0, `rgba(${sl.color},${sl.alpha})`);
      slg.addColorStop(1, `rgba(${sl.color},0)`);
      ctx.save();
      ctx.fillStyle = slg;
      ctx.beginPath();
      ctx.moveTo(sx, sy);   // 1px tip at rooftop
      ctx.lineTo(ex1, ey1);
      ctx.lineTo(ex2, ey2);
      ctx.closePath();
      ctx.fill();
      ctx.restore();
    }

    // ── City background (drawn over searchlights so buildings occlude them) ──
    bgScroll = (bgScroll + 0.15) % skyBG.width;
    ctx.drawImage(skyBG, -bgScroll, 0, skyBG.width, h);
    ctx.drawImage(skyBG, skyBG.width - bgScroll, 0, skyBG.width, h);

    // ── Window flicker: unreliable power grid ──
    ctx.fillStyle = '#03000a';
    for (let i = 0; i < winStates.length; i++) {
      const ws = winStates[i];
      ws.timer--;
      if (ws.timer <= 0) {
        ws.off = !ws.off;
        ws.timer = ws.off ? (2 + Math.floor(Math.random()*20)) : (2000 + Math.floor(Math.random()*9000));
      }
      if (!ws.off) continue;
      const win = skyData.wins[i];
      // Skip windows behind foreground buildings (depth culling)
      const wy = win.y;
      let hidden = false;
      for (const fg of skyData.fgRects) {
        const fgSx = fg.x - fgScroll;
        const fgSx2 = fg.x + skyFG.width - fgScroll;
        const wx1 = win.x - bgScroll; const wx2 = win.x + skyBG.width - bgScroll;
        if ((wx1 >= fgSx && wx1 < fgSx+fg.w && wy >= fg.y) ||
            (wx2 >= fgSx2 && wx2 < fgSx2+fg.w && wy >= fg.y)) { hidden=true; break; }
      }
      if (hidden) continue;
      const sx1 = win.x - bgScroll, sx2 = win.x + skyBG.width - bgScroll;
      if (sx1 > -6 && sx1 < w+6) ctx.fillRect(sx1, wy, 6, 8);
      if (sx2 > -6 && sx2 < w+6) ctx.fillRect(sx2, wy, 6, 8);
    }

    // ── Medium buildings (mid-distance, parallax between bg and fg) ──
    mdScroll = (mdScroll + 0.25) % skyMD.width;
    ctx.drawImage(skyMD, -mdScroll, 0, skyMD.width, h);
    ctx.drawImage(skyMD, skyMD.width - mdScroll, 0, skyMD.width, h);

    // ── Foreground buildings (parallax — scrolls faster than bg) ──
    fgScroll = (fgScroll + 0.38) % skyFG.width;
    ctx.drawImage(skyFG, -fgScroll, 0, skyFG.width, h);
    ctx.drawImage(skyFG, skyFG.width - fgScroll, 0, skyFG.width, h);
    // Sparse window flicker on fg buildings (random dark patches in building zone)
    if (Math.random() < 0.18) {
      ctx.fillStyle = '#06000d';
      ctx.fillRect(Math.random()*w, h*0.76 + Math.random()*h*0.22, 5, 7);
    }

    // ── Drifting toxic clouds ──
    drawClouds(w, h);

    // ── Angled cyan rain ──
    ctx.shadowBlur = 4; ctx.shadowColor = RAIN_COLOR;
    for (const r of rain) {
      r.x += r.speed * r.angle;
      r.y += r.speed;
      // Wrap: exit right or bottom → re-enter from left/top
      if (r.y > h + r.len || r.x > w + 100) {
        r.y = -r.len - Math.random()*h*0.5;
        r.x = Math.random()*(w + 200) - 200;
      }
      const ex = r.x + r.len * r.angle;
      const ey = r.y + r.len;
      const rg = ctx.createLinearGradient(r.x, r.y, ex, ey);
      rg.addColorStop(0, 'rgba(0,0,0,0)');
      rg.addColorStop(1, `rgba(0,255,255,${r.alpha})`);
      ctx.strokeStyle = rg;
      ctx.lineWidth = r.width;
      ctx.beginPath(); ctx.moveTo(r.x, r.y); ctx.lineTo(ex, ey); ctx.stroke();
    }
    ctx.shadowBlur = 0;

    // ── Electric arcs ──
    if(Math.random()<0.025) emitArc();
    arcs = arcs.filter(a => a.life > 0);
    for (const a of arcs) {
      a.life -= 0.055;
      ctx.save();
      ctx.globalAlpha = a.life * (0.7 + Math.random()*0.3);
      ctx.strokeStyle = a.color;
      ctx.lineWidth = 1 + a.life;
      ctx.shadowBlur = 10 + Math.random()*8; ctx.shadowColor = a.color;
      ctx.beginPath();
      a.segs.forEach(([x,y],i) => i===0 ? ctx.moveTo(x,y) : ctx.lineTo(x,y));
      ctx.stroke();
      ctx.restore();
    }

    // ── Neon fog ──
    const topFog = ctx.createLinearGradient(0,0,0,h*0.32);
    topFog.addColorStop(0,'rgba(100,0,120,0.20)');
    topFog.addColorStop(1,'rgba(0,0,0,0)');
    ctx.fillStyle=topFog; ctx.fillRect(0,0,w,h*0.32);

    const botFog = ctx.createLinearGradient(0,h*0.78,0,h);
    botFog.addColorStop(0,'rgba(0,0,0,0)');
    botFog.addColorStop(1,'rgba(0,60,100,0.22)');
    ctx.fillStyle=botFog; ctx.fillRect(0,h*0.78,w,h*0.22);

    // ── CRT scan lines ──
    ctx.fillStyle='rgba(0,0,0,0.06)';
    for(let y=0;y<h;y+=3) ctx.fillRect(0,y,w,1);

    // ── Resume border: slow neon surge with realistic flicker ──
    // Very slow surge cycle (~3 bpm = one full glow every 20s): 8s gentle rise,
    // 12s long fade. Between surges the sign holds a dim baseline.
    // Flicker is voltage-noise style — mostly steady, occasionally buzzing.
    if (frame % 2 === 0) {
      const sec = frame / 60;
      function _h(n) { const s=Math.sin(n*127.1+311.7)*43758.5453; return s-Math.floor(s); }
      function _sn(t) { const i=Math.floor(t),f=t-i,s=f*f*(3-2*f); return _h(i)+(_h(i+1)-_h(i))*s; }

      // ~3 bpm surge (one cycle ≈ 20 seconds)
      const bpm = 3.0;
      const phase = (sec * bpm / 60) % 1;
      // Slow bell: rise over first 40% of cycle, decay over remaining 60%
      const beatEnv = phase < 0.4
        ? Math.pow(phase / 0.4, 1.6)            // ~8s smooth rise to peak
        : Math.pow(1 - (phase - 0.4) / 0.6, 2.4); // ~12s long slow fade

      // Realistic neon flicker: voltage-noise dips — stable most of the time,
      // occasionally buzzing at irregular micro-intervals
      const flickerTendency = _sn(sec * 0.09); // drifts slowly 0..1
      const isFlickering = flickerTendency > 0.80; // ~20% of time in flicker mode
      const fastNoise = _sn(sec * 3.1 + 7.4);
      const flicker = isFlickering
        ? 0.35 + fastNoise * 0.55               // erratic during buzz
        : 1.0 - _sn(sec * 1.8 + 2.2) * 0.05;  // near-steady, tiny wander

      // Dim baseline so it never goes fully dark
      const base = 0.07 + _sn(sec * 0.11) * 0.05;

      const g = Math.min(1, (base + beatEnv * 0.85) * flicker);
      const resumeEl = document.getElementById('resume');
      if (resumeEl) {
        resumeEl.style.boxShadow =
          `0 0 ${4+g*36}px rgba(255,0,255,${(0.08+g*0.55).toFixed(2)}),`+
          `0 0 ${12+g*110}px rgba(255,0,255,${(0.01+g*0.18).toFixed(2)}),`+
          `inset 0 0 ${6+g*32}px rgba(80,0,120,${(0.02+g*0.13).toFixed(2)})`;
        resumeEl.style.borderColor = g > 0.80 ? '#ff88ff' : g > 0.35 ? '#ff00ff' : '#cc00cc';
      }
    }

    fxAnim = requestAnimationFrame(loop);
  }

  loop();
  startCyberpunkEffects();
}

// ── CYBERPUNK TAG EFFECTS: neon flicker animations ───────────────────────────
// Tags behave like damaged neon signs: random flicker bursts, brief outages,
// occasional power-surge overbright.

let cpEffectTimer = null;

function stopCyberpunkEffects() {
  if(cpEffectTimer){ clearTimeout(cpEffectTimer); cpEffectTimer=null; }
  document.querySelectorAll('.tag.cp-flicker,.tag.cp-out,.tag.cp-surge').forEach(t =>
    t.classList.remove('cp-flicker','cp-out','cp-surge')
  );
}

function startCyberpunkEffects() {
  function tick() {
    if(document.documentElement.getAttribute('data-theme')!=='cyberpunk') return;
    const tags = Array.from(document.querySelectorAll('.tag'));
    if(!tags.length){ cpEffectTimer=setTimeout(tick,600); return; }
    const n = Math.max(1, Math.round((2 + Math.floor(Math.random()*4)) * fxScale()));
    for(let i=0; i<n; i++){
      const tag = tags[Math.floor(Math.random()*tags.length)];
      const r = Math.random();
      const cls = r<0.45?'cp-flicker':r<0.72?'cp-out':'cp-surge';
      tag.classList.remove('cp-flicker','cp-out','cp-surge');
      void tag.offsetWidth;
      tag.classList.add(cls);
      const dur = cls==='cp-out'?3200:cls==='cp-surge'?1400:1000;
      setTimeout(()=>tag.classList.remove(cls), dur+80);
    }
    cpEffectTimer = setTimeout(tick, (60+Math.random()*560) / fxScale());
  }
  tick();
}

