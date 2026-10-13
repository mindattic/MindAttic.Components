// ╔═══════════════════════════════════════════════════════════════════════════════╗
// ║  NEKO                                                                         ║
// ║  Classic desktop pet — 32 inline sprites, 17 states, no per-frame requests    ║
// ╠═══════════════════════════════════════════════════════════════════════════════╣
// ║  Full port of the 1998 Macintosh/Windows desktop pet Neko98 by David Harvey,  ║
// ║  based on the nekojs JavaScript implementation by Louis Abraham (GPL v3).     ║
// ║                                                                               ║
// ║  THE SPRITES ARE INLINE.  The 32 animation frames are base64 data URIs on     ║
// ║  the CSS classes .neko-f0 through .neko-f31 in <style>.  Each frame is only   ║
// ║  ~1 KB, so inlining beats 32 separate requests and the cat never flickers     ║
// ║  while frames load.  (Bigger art lives on the CDN — see the avatar comment.)  ║
// ║                                                                               ║
// ║  NekoState maps to the original Neko.h enum values exactly.                   ║
// ║  BehaviorMode drives the higher-level action loop between state transitions.  ║
// ║  The behavior model: chase → stop → groom → yawn → sleep → wake → repeat.     ║
// ╚═══════════════════════════════════════════════════════════════════════════════╝
//
// Full port of the 1998 Macintosh/Windows desktop pet. 32 sprite frames,
// 17 animation states, and a complete behavior model:
//
//   She chases the mouse cursor.
//   If she reaches it, she stops and grooms.
//   If you don't move for a while, she yawns and falls asleep (Zs appear).
//   Move the mouse — she snaps awake and chases again.
//
// NekoState maps to the original Neko.h enum values exactly.
// BehaviorMode drives the higher-level action loop between state transitions.
// The frames are 32 CSS classes in the <style> above — one DOM element,
// all CSS, no per-frame image requests.
const NEKO_SPRITE_COUNT = 32;

(function () {
  "use strict";

  // Animation states (matching original Neko.h enum)
  const NekoState = {
    STOP: 0,
    WASH: 1,
    SCRATCH: 2,
    YAWN: 3,
    SLEEP: 4,
    AWAKE: 5,
    U_MOVE: 6, // Up
    D_MOVE: 7, // Down
    L_MOVE: 8, // Left
    R_MOVE: 9, // Right
    UL_MOVE: 10, // Up-Left
    UR_MOVE: 11, // Up-Right
    DL_MOVE: 12, // Down-Left
    DR_MOVE: 13, // Down-Right
    U_CLAW: 14, // Clawing upward (at top boundary)
    D_CLAW: 15, // Clawing downward (at bottom boundary)
    L_CLAW: 16, // Clawing left (at left boundary)
    R_CLAW: 17, // Clawing right (at right boundary)
  };

  // Behavior modes (matching original Action enum)
  const BehaviorMode = {
    CHASE_MOUSE: 0,
    RUN_AROUND_RANDOMLY: 2,
    PACE_AROUND_SCREEN: 3,
    RUN_AROUND: 4,
    RUN_HOME: 5,
  };

  // Animation timing constants (in frames)
  const STOP_TIME = AppSettings.neko_stop_time;
  const WASH_TIME = AppSettings.neko_wash_time;
  const SCRATCH_TIME = AppSettings.neko_scratch_time;
  const YAWN_TIME = 3;
  const AWAKE_TIME = 3;
  const CLAW_TIME = 10;

  // Sprite size
  const SPRITE_SIZE = 32;

  class Neko {
    constructor(options = {}) {
      // Configuration
      this.fps = options.fps || 120; // Target FPS (default 120 for smooth movement)
      // Original used 16 pixels/tick for 640x480 screens (~2.5% of width)
      // Modern screens are ~3x larger, so default to 24 for similar feel
      this.speed = options.speed || 24;
      this.behaviorMode = options.behaviorMode || BehaviorMode.CHASE_MOUSE;
      this.idleThreshold = options.idleThreshold || 6; // Original m_dwIdleSpace = 6
      this.homeX = options.startX || 0;
      this.homeY = options.startY || 0;
      this.onHome = null; // callback when neko arrives home

      // State
      this.state = NekoState.STOP;
      this.tickCount = 0; // Increments every frame (like m_uTickCount)
      this.stateCount = 0; // Increments every 2 original ticks (like m_uStateCount)
      this.sleepCooldownUntil = 0; // timestamp: can't sleep again until this time

      // Position (display position for smooth rendering)
      this.x = options.startX || 0;
      this.y = options.startY || 0;
      // Logic position (updated at original 5 FPS tick rate)
      this.logicX = this.x;
      this.logicY = this.y;
      // Previous logic position (for interpolation)
      this.prevLogicX = this.x;
      this.prevLogicY = this.y;
      // Target tracking — set oldTarget far away so first moveStart check is true
      // This ensures neko immediately starts running toward mouse on spawn
      this.targetX = this.x;
      this.targetY = this.y;
      this.oldTargetX = -99999;
      this.oldTargetY = -99999;
      // Movement deltas (preserved like m_nDX, m_nDY in original)
      this.moveDX = 0;
      this.moveDY = 0;

      // Bounds - clientWidth excludes scrollbar, innerHeight is viewport height
      this.boundsWidth = document.documentElement.clientWidth - SPRITE_SIZE;
      this.boundsHeight = window.innerHeight - SPRITE_SIZE;

      // Mouse tracking - initialize to screen center so neko has a target immediately
      this.mouseX = window.innerWidth / 2;
      this.mouseY = window.innerHeight / 2;
      this.hasMouseMoved = true;

      // DOM element
      this.element = null;
      this.spriteCount = 0;
      this.allowBehaviorChange = options.allowBehaviorChange !== false; // Default true

      // Animation lookup table (maps state to sprite indices)
      // Format: [frame1_index, frame2_index]
      // These MUST match the original C++ m_nAnimation table EXACTLY
      // From Neko.cpp lines 40-57:
      this.animationTable = [
        [28, 28], // STOP: m_nAnimation[STOP][0]=28, [1]=28
        [25, 28], // WASH: m_nAnimation[WASH][0]=25, [1]=28
        [26, 27], // SCRATCH: m_nAnimation[SCRATCH][0]=26, [1]=27
        [29, 29], // YAWN: m_nAnimation[YAWN][0]=29, [1]=29
        [30, 31], // SLEEP: m_nAnimation[SLEEP][0]=30, [1]=31
        [0, 0], // AWAKE: m_nAnimation[AWAKE][0]=0, [1]=0
        [1, 2], // U_MOVE: m_nAnimation[U_MOVE][0]=1, [1]=2
        [9, 10], // D_MOVE: m_nAnimation[D_MOVE][0]=9, [1]=10
        [13, 14], // L_MOVE: m_nAnimation[L_MOVE][0]=13, [1]=14
        [5, 6], // R_MOVE: m_nAnimation[R_MOVE][0]=5, [1]=6
        [15, 16], // UL_MOVE: m_nAnimation[UL_MOVE][0]=15, [1]=16
        [3, 4], // UR_MOVE: m_nAnimation[UR_MOVE][0]=3, [1]=4
        [11, 12], // DL_MOVE: m_nAnimation[DL_MOVE][0]=11, [1]=12
        [7, 8], // DR_MOVE: m_nAnimation[DR_MOVE][0]=7, [1]=8
        [17, 18], // U_CLAW: m_nAnimation[U_CLAW][0]=17, [1]=18
        [23, 24], // D_CLAW: m_nAnimation[D_CLAW][0]=23, [1]=24
        [21, 22], // L_CLAW: m_nAnimation[L_CLAW][0]=21, [1]=22
        [19, 20], // R_CLAW: m_nAnimation[R_CLAW][0]=19, [1]=20
      ];

      // Additional state for behaviors
      this.cornerIndex = 0;
      this.ballX = 0;
      this.ballY = 0;
      this.ballVX = 0;
      this.ballVY = 0;

      this.init();
    }

    init() {
      // Create the neko element with defensive styles to prevent global CSS interference
      this.element = document.createElement("div");
      this.element.className = "neko";
      this.element.style.cssText = `
        position: fixed;
        width: ${SPRITE_SIZE}px;
        height: ${SPRITE_SIZE}px;
        image-rendering: pixelated;
        pointer-events: ${this.allowBehaviorChange ? "auto" : "none"};
        cursor: ${this.allowBehaviorChange ? "pointer" : "default"};
        z-index: 999999;
        left: ${this.x}px;
        top: ${this.y}px;
        margin: 0;
        padding: 0;
        border: none;
        overflow: visible;
        box-sizing: border-box;
        user-select: none;
        -webkit-user-select: none;
      `;

      // Sprite rendered via CSS class swap (no img element needed)
      this.element.classList.add('neko-sprite');

      document.body.appendChild(this.element);

      // Track mouse position
      this._mouseMoveHandler = (e) => {
        this.mouseX = e.clientX;
        this.mouseY = e.clientY;
      };
      document.addEventListener("mousemove", this._mouseMoveHandler);

      // Update bounds on resize
      window.addEventListener("resize", () => {
        this.boundsWidth = document.documentElement.clientWidth - SPRITE_SIZE;
        this.boundsHeight = window.innerHeight - SPRITE_SIZE;
      });

      // Use provided start position, or random if not given
      if (this.x === 0 && this.y === 0) {
        this.x = Math.random() * this.boundsWidth;
        this.y = Math.random() * this.boundsHeight;
      }
      this.logicX = this.x;
      this.logicY = this.y;
      this.prevLogicX = this.x;
      this.prevLogicY = this.y;
      // Initialize target to current position but keep oldTarget far away
      // so first moveStart check is true and neko starts running immediately
      this.targetX = this.x + SPRITE_SIZE / 2;
      this.targetY = this.y + SPRITE_SIZE - 1;
      this.updatePosition();

      // Animation loop
      this.running = false;
      this.intervalId = null;
    }

    start() {
      if (this.running) return;
      this.running = true;

      // Calculate interval from FPS
      // Higher FPS = smoother movement while maintaining same speed
      const interval = 1000 / this.fps;
      this.intervalId = setInterval(() => {
        this.update();
      }, interval);
    }

    stop() {
      this.running = false;
      if (this.intervalId) {
        clearInterval(this.intervalId);
        this.intervalId = null;
      }
    }

    setSprites(count) {
      this.spriteCount = count || 32;
      this.updateSprite();
    }

    updateSprite() {
      if (!this.spriteCount) return;

      // Get the current animation frame index
      // Uses tickCount which is scaled to match original 5 FPS timing
      let frameIndex;
      if (this.state === NekoState.SLEEP) {
        // Slower animation for sleep (toggles every 4 ticks in original = 800ms)
        frameIndex =
          this.animationTable[this.state][(this.tickCount >> 2) & 0x1];
      } else {
        // Normal animation speed (toggles every tick in original = 200ms)
        frameIndex = this.animationTable[this.state][this.tickCount & 0x1];
      }

      // Swap CSS class for the frame
      if (this._currentFrame !== undefined) {
        this.element.classList.remove('neko-f' + this._currentFrame);
      }
      this.element.classList.add('neko-f' + frameIndex);
      this._currentFrame = frameIndex;
    }

    updatePosition() {
      this.element.style.left = Math.round(this.x) + "px";
      this.element.style.top = Math.round(this.y) + "px";
      this.element.style.zIndex = 999000 + Math.round(this.y + SPRITE_SIZE);
    }

    update() {
      // Track time accumulator for original tick timing
      // Original runs at 5 FPS (200ms per tick), we run at this.fps
      // We need to accumulate fractional ticks and process when we hit a full tick
      if (this.tickAccumulator === undefined) this.tickAccumulator = 0;

      const originalFPS = 8; // Bumped from 5 for snappier animations
      this.tickAccumulator += originalFPS / this.fps;

      // Process as many original ticks as have accumulated
      while (this.tickAccumulator >= 1) {
        this.tickAccumulator -= 1;
        // Save previous position before processing tick
        this.prevLogicX = this.logicX;
        this.prevLogicY = this.logicY;
        this.processOriginalTick();
      }

      // Smooth interpolation between logic positions
      // tickAccumulator represents progress (0-1) towards next tick
      const t = this.tickAccumulator;
      this.x = this.prevLogicX + (this.logicX - this.prevLogicX) * t;
      this.y = this.prevLogicY + (this.logicY - this.prevLogicY) * t;

      // Update display position every frame
      this.updatePosition();
    }

    processOriginalTick() {
      // This runs at the original 5 FPS equivalent timing
      // Increment tick counter (like m_uTickCount)
      this.tickCount++;
      if (this.tickCount >= 9999) this.tickCount = 0;

      // Increment state counter every 2 ticks (like original)
      if (this.tickCount % 2 === 0) {
        this.stateCount++;
      }

      // Update behavior based on mode
      switch (this.behaviorMode) {
        case BehaviorMode.CHASE_MOUSE:
          this.chaseMouse();
          break;
        case BehaviorMode.RUN_AROUND_RANDOMLY:
          this.runRandomly();
          break;
        case BehaviorMode.PACE_AROUND_SCREEN:
          this.paceAroundScreen();
          break;
        case BehaviorMode.RUN_AROUND:
          this.runAround();
          break;
        case BehaviorMode.RUN_HOME:
          this.runHome();
          break;
      }

      // Update animation frame
      this.updateSprite();
    }

    chaseMouse() {
      this.runTowards(this.mouseX, this.mouseY);
    }

    runRandomly() {
      // Autonomous cat AI — wander, seek walls, burst, nap
      if (this.catAI === undefined) {
        this.catAI = 'WANDER';
        this.catTimer = 0;
        this.catBurst = 0;
        this.targetX = Math.random() * this.boundsWidth;
        this.targetY = Math.random() * this.boundsHeight;
      }
      const resting = this.state === NekoState.SLEEP || this.state === NekoState.STOP;
      this.catTimer++;

      // During burst: snap to new random targets quickly
      if (this.catAI === 'BURST' && this.catBurst > 0 && resting) {
        this.catBurst--;
        this.targetX = Math.random() * this.boundsWidth;
        this.targetY = Math.random() * this.boundsHeight;
        if (this.catBurst <= 0) { this.catAI = 'WANDER'; this.catTimer = 0; }
      }

      // After idling long enough, pick next macro behavior
      const idleThresh = this.catAI === 'NAP' ? 320 + Math.random()*200 : 100 + Math.random()*100;
      if (resting && this.catTimer > idleThresh) {
        this.catTimer = 0;
        const roll = Math.random();
        if (roll < 0.40) {
          // Wander to a random interior point
          this.catAI = 'WANDER';
          this.targetX = 80 + Math.random() * Math.max(0, this.boundsWidth  - 160);
          this.targetY = 80 + Math.random() * Math.max(0, this.boundsHeight - 160);
        } else if (roll < 0.62) {
          // Seek a wall edge — CLAW triggers naturally when neko reaches it
          this.catAI = 'WALL';
          const w = Math.floor(Math.random() * 4);
          const mx = this.boundsWidth * 0.3 + Math.random() * this.boundsWidth * 0.4;
          const my = this.boundsHeight * 0.3 + Math.random() * this.boundsHeight * 0.4;
          if      (w === 0) { this.targetX = -8;                      this.targetY = my; }
          else if (w === 1) { this.targetX = this.boundsWidth + 8;    this.targetY = my; }
          else if (w === 2) { this.targetX = mx;                      this.targetY = -8; }
          else              { this.targetX = mx;                       this.targetY = this.boundsHeight + 8; }
        } else if (roll < 0.78) {
          // Burst of energy — run to 3–6 random spots fast
          this.catAI = 'BURST';
          this.catBurst = 3 + Math.floor(Math.random() * 4);
          this.targetX = Math.random() * this.boundsWidth;
          this.targetY = Math.random() * this.boundsHeight;
        } else {
          // Nap in place — state machine handles WASH→SCRATCH→YAWN→SLEEP
          this.catAI = 'NAP';
          // Stay on current target so neko grooms where it stands
        }
      }

      this.runTowards(this.targetX, this.targetY);
    }

    runHome() {
      const homeBtn = document.getElementById('neko-btn');
      if (homeBtn) {
        const r = homeBtn.getBoundingClientRect();
        this.homeX = r.left; this.homeY = r.top;
      }
      this.runTowards(this.homeX + SPRITE_SIZE / 2, this.homeY + SPRITE_SIZE - 1);
      const dx = this.logicX - (this.homeX || 0);
      const dy = this.logicY - (this.homeY || 0);
      if (Math.abs(dx) < this.speed + 2 && Math.abs(dy) < this.speed + 2 && this.state === NekoState.STOP) {
        if (this.onHome) this.onHome();
      }
    }

    paceAroundScreen() {
      // Original checks if neko has stopped moving (m_nDX == 0 && m_nDY == 0)
      // We track this via lastMoveDX/DY
      if (this.lastMoveDX === 0 && this.lastMoveDY === 0) {
        this.cornerIndex = ((this.cornerIndex || 0) + 1) % 4;
      }

      // Corners offset by sprite size (matching original)
      // Target positions that result in neko stopping at the corners
      const corners = [
        [SPRITE_SIZE + SPRITE_SIZE / 2, SPRITE_SIZE + SPRITE_SIZE - 1],
        [
          SPRITE_SIZE + SPRITE_SIZE / 2,
          this.boundsHeight - SPRITE_SIZE + SPRITE_SIZE - 1,
        ],
        [
          this.boundsWidth - SPRITE_SIZE + SPRITE_SIZE / 2,
          this.boundsHeight - SPRITE_SIZE + SPRITE_SIZE - 1,
        ],
        [
          this.boundsWidth - SPRITE_SIZE + SPRITE_SIZE / 2,
          SPRITE_SIZE + SPRITE_SIZE - 1,
        ],
      ];

      const target = corners[this.cornerIndex || 0];
      this.runTowards(target[0], target[1]);
    }

    runAround() {
      // Original ball physics with repelling from edges
      const dwBoundingBox = this.speed * 8;

      // Initialize ball if needed (matching original constructor)
      if (this.ballX === 0 && this.ballY === 0) {
        this.ballX = Math.random() * (this.boundsWidth - dwBoundingBox);
        this.ballY = Math.random() * (this.boundsHeight - dwBoundingBox);
        this.ballVX = (Math.random() < 0.5 ? 1 : -1) * (this.speed / 2) + 1;
        this.ballVY = (Math.random() < 0.5 ? 1 : -1) * (this.speed / 2) + 1;
      }

      // Move invisible ball
      this.ballX += this.ballVX;
      this.ballY += this.ballVY;

      // Repel from edges (original logic)
      if (this.ballX < dwBoundingBox) {
        if (this.ballX > 0) this.ballVX++;
        else this.ballVX = -this.ballVX;
      } else if (this.ballX > this.boundsWidth - dwBoundingBox) {
        if (this.ballX < this.boundsWidth) this.ballVX--;
        else this.ballVX = -this.ballVX;
      }

      if (this.ballY < dwBoundingBox) {
        if (this.ballY > 0) this.ballVY++;
        else this.ballVY = -this.ballVY;
      } else if (this.ballY > this.boundsHeight - dwBoundingBox) {
        if (this.ballY < this.boundsHeight) this.ballVY--;
        else this.ballVY = -this.ballVY;
      }

      this.runTowards(this.ballX, this.ballY);
    }

    setState(newState) {
      // Reset counters on state change (like original SetState)
      this.tickCount = 0;
      this.stateCount = 0;
      this.state = newState;
    }

    runTowards(targetX, targetY) {
      // Store old target for MoveStart check
      this.oldTargetX = this.targetX;
      this.oldTargetY = this.targetY;
      this.targetX = targetX;
      this.targetY = targetY;

      // Calculate distance to target (using logic position, not display position)
      const dx = targetX - this.logicX - SPRITE_SIZE / 2; // Stop in middle of cursor
      const dy = targetY - this.logicY - SPRITE_SIZE + 1; // Just above cursor
      const distance = Math.sqrt(dx * dx + dy * dy);

      // Calculate movement delta (like original m_nDX, m_nDY)
      // Store as instance variables so they persist across ticks
      // IMPORTANT: Use integers like original to prevent direction flickering
      // which causes state resets and prevents wall clawing
      if (distance !== 0) {
        if (distance <= this.speed) {
          // Less than top speed - jump the gap
          this.moveDX = Math.trunc(dx);
          this.moveDY = Math.trunc(dy);
        } else {
          // More than top speed - run at top speed
          this.moveDX = Math.trunc((this.speed * dx) / distance);
          this.moveDY = Math.trunc((this.speed * dy) / distance);
        }
      } else {
        this.moveDX = 0;
        this.moveDY = 0;
      }

      // Store for paceAroundScreen check
      this.lastMoveDX = this.moveDX;
      this.lastMoveDY = this.moveDY;

      // Check if target moved (MoveStart equivalent)
      const moveStart = !(
        this.oldTargetX >= this.targetX - this.idleThreshold &&
        this.oldTargetX <= this.targetX + this.idleThreshold &&
        this.oldTargetY >= this.targetY - this.idleThreshold &&
        this.oldTargetY <= this.targetY + this.idleThreshold
      );

      // State machine (matching original RunTowards switch)
      switch (this.state) {
        case NekoState.STOP:
          if (moveStart) {
            this.setState(NekoState.AWAKE);
          } else if (this.stateCount >= STOP_TIME) {
            // Check for wall scratching using preserved moveDX/moveDY
            if (this.moveDX < 0 && this.logicX <= 0) {
              this.setState(NekoState.L_CLAW);
            } else if (this.moveDX > 0 && this.logicX >= this.boundsWidth) {
              this.setState(NekoState.R_CLAW);
            } else if (this.moveDY < 0 && this.logicY <= 0) {
              this.setState(NekoState.U_CLAW);
            } else if (this.moveDY > 0 && this.logicY >= this.boundsHeight) {
              this.setState(NekoState.D_CLAW);
            } else {
              this.setState(NekoState.WASH);
            }
          }
          break;

        case NekoState.WASH:
          if (moveStart) {
            this.setState(NekoState.AWAKE);
          } else if (this.stateCount >= WASH_TIME) {
            this.setState(NekoState.SCRATCH);
          }
          break;

        case NekoState.SCRATCH:
          if (moveStart) {
            this.setState(NekoState.AWAKE);
          } else if (this.stateCount >= SCRATCH_TIME) {
            this.setState(NekoState.YAWN);
          }
          break;

        case NekoState.YAWN:
          if (moveStart) {
            this.setState(NekoState.AWAKE);
          } else if (this.stateCount >= YAWN_TIME) {
            if (Date.now() < this.sleepCooldownUntil) {
              // Cooldown active — can't sleep, wake up and move instead
              this.setState(NekoState.AWAKE);
            } else {
              this.setState(NekoState.SLEEP);
            }
          }
          break;

        case NekoState.SLEEP:
          if (moveStart) {
            this.sleepCooldownUntil = Date.now() + 15000; // can't sleep again for 15s
            this.setState(NekoState.AWAKE);
          }
          break;

        case NekoState.AWAKE:
          if (this._wallBounce && this.tickCount >= 1) {
            // ~100ms surprise then run opposite direction from top/bottom wall
            const bounceDir = this._wallBounce;
            this._wallBounce = null;
            if (bounceDir === 'top') {
              this.targetX = Math.random() * this.boundsWidth;
              this.targetY = this.boundsHeight * 0.5 + Math.random() * this.boundsHeight * 0.5;
            } else {
              this.targetX = Math.random() * this.boundsWidth;
              this.targetY = Math.random() * this.boundsHeight * 0.5;
            }
            this.oldTargetX = -99999; this.oldTargetY = -99999;
            this.catAI = undefined; this.catTimer = 0;
            const bdx = this.targetX - this.logicX - SPRITE_SIZE / 2;
            const bdy = this.targetY - this.logicY - SPRITE_SIZE + 1;
            const bdist = Math.sqrt(bdx * bdx + bdy * bdy);
            if (bdist > 0) {
              this.moveDX = Math.trunc((this.speed * bdx) / bdist);
              this.moveDY = Math.trunc((this.speed * bdy) / bdist);
            }
            this.calcDirection(this.moveDX, this.moveDY);
          } else if (this.tickCount >= 1) { // ~100ms shocked duration
            this.calcDirection(this.moveDX, this.moveDY);
          }
          break;

        case NekoState.U_MOVE:
        case NekoState.D_MOVE:
        case NekoState.L_MOVE:
        case NekoState.R_MOVE:
        case NekoState.UL_MOVE:
        case NekoState.UR_MOVE:
        case NekoState.DL_MOVE:
        case NekoState.DR_MOVE:
          // Calculate new position using preserved moveDX/moveDY
          let newX = this.logicX + this.moveDX;
          let newY = this.logicY + this.moveDY;

          // Check which boundaries are hit (before clamping)
          const hitLeft = newX <= 0;
          const hitRight = newX >= this.boundsWidth;
          const hitTop = newY <= 0;
          const hitBottom = newY >= this.boundsHeight;
          const wasOutside = hitLeft || hitRight || hitTop || hitBottom;

          // Update direction
          this.calcDirection(this.moveDX, this.moveDY);

          // Clamp position
          newX = Math.max(0, Math.min(this.boundsWidth, newX));
          newY = Math.max(0, Math.min(this.boundsHeight, newY));
          const notMoved = newX === this.logicX && newY === this.logicY;

          // Wall reactions
          if (wasOutside && notMoved) {
            if (hitLeft || hitRight) {
              // Scratch the side wall
              this.setState(hitLeft ? NekoState.L_CLAW : NekoState.R_CLAW);
            } else {
              // Surprised by top/bottom — brief shock then bounce away
              this._wallBounce = hitTop ? 'top' : 'bottom';
              this.setState(NekoState.AWAKE);
            }
          } else {
            this.logicX = newX;
            this.logicY = newY;
          }
          break;

        case NekoState.U_CLAW:
        case NekoState.D_CLAW:
        case NekoState.L_CLAW:
        case NekoState.R_CLAW:
          if (moveStart) {
            this.setState(NekoState.AWAKE);
          } else if (this.stateCount >= CLAW_TIME) {
            this.setState(NekoState.SCRATCH);
          }
          break;

        default:
          this.setState(NekoState.STOP);
          break;
      }
    }

    calcDirection(dx, dy) {
      // Calculate direction based on movement delta (like original CalcDirection)
      let newState;

      if (dx === 0 && dy === 0) {
        newState = NekoState.STOP;
      } else {
        const largeX = dx;
        const largeY = -dy; // Y is inverted
        const length = Math.sqrt(largeX * largeX + largeY * largeY);
        const sinTheta = largeY / length;

        const sinPiPer8 = 0.3826834323651;
        const sinPiPer8Times3 = 0.9238795325113;

        if (dx > 0) {
          if (sinTheta > sinPiPer8Times3) {
            newState = NekoState.U_MOVE;
          } else if (sinTheta > sinPiPer8) {
            newState = NekoState.UR_MOVE;
          } else if (sinTheta > -sinPiPer8) {
            newState = NekoState.R_MOVE;
          } else if (sinTheta > -sinPiPer8Times3) {
            newState = NekoState.DR_MOVE;
          } else {
            newState = NekoState.D_MOVE;
          }
        } else {
          if (sinTheta > sinPiPer8Times3) {
            newState = NekoState.U_MOVE;
          } else if (sinTheta > sinPiPer8) {
            newState = NekoState.UL_MOVE;
          } else if (sinTheta > -sinPiPer8) {
            newState = NekoState.L_MOVE;
          } else if (sinTheta > -sinPiPer8Times3) {
            newState = NekoState.DL_MOVE;
          } else {
            newState = NekoState.D_MOVE;
          }
        }
      }

      if (this.state !== newState) {
        this.setState(newState);
      }
    }

    isIdle() {
      return (
        this.state === NekoState.STOP ||
        this.state === NekoState.WASH ||
        this.state === NekoState.SCRATCH ||
        this.state === NekoState.YAWN ||
        this.state === NekoState.SLEEP ||
        this.state === NekoState.AWAKE
      );
    }

    cycleBehavior() {
      // Cycle through behaviors: Chase -> Run Away -> Random -> Pace -> Run Around -> back to Chase
      const behaviors = [
        BehaviorMode.CHASE_MOUSE,
        BehaviorMode.RUN_AROUND_RANDOMLY,
        BehaviorMode.PACE_AROUND_SCREEN,
        BehaviorMode.RUN_AROUND,
      ];
      const currentIndex = behaviors.indexOf(this.behaviorMode);
      const nextIndex = (currentIndex + 1) % behaviors.length;
      this.behaviorMode = behaviors[nextIndex];

      // Reset state to wake the cat up if sleeping
      if (this.state === NekoState.SLEEP) {
        this.setState(NekoState.AWAKE);
      }

      // Show behavior name (optional - can be removed if you don't want this)
      const behaviorNames = [
        "Chase Mouse",
        "Run Away From Mouse",
        "Run Around Randomly",
        "Pace Around Screen",
        "Run Around",
      ];
      console.log(`Neko behavior: ${behaviorNames[nextIndex]}`);
    }

    destroy() {
      if (this._mouseMoveHandler) {
        document.removeEventListener("mousemove", this._mouseMoveHandler);
      }
      if (this.element && this.element.parentNode) {
        this.element.parentNode.removeChild(this.element);
      }
    }
  }

  // Export to global scope
  window.Neko = Neko;
  window.NekoState = NekoState;
  window.BehaviorMode = BehaviorMode;
})();


// ╔═══════════════════════════════════════════════════════════════════════════════╗
// ║  NEKO COLORS & TOOLBAR INTEGRATION                                            ║
// ║  23-colour CSS filter palette + sleeping icon + wake/startle/dismiss          ║
// ╠═══════════════════════════════════════════════════════════════════════════════╣
// ║  Colour tinting uses only CSS filters — no canvas compositing, no recolouring ║
// ║  sprite sheets.  The technique: sepia(1) converts to warm monochrome, then    ║
// ║  saturate() pumps the colour, hue-rotate() shifts the hue wheel, and          ║
// ║  brightness() compensates.  23 named colours are pre-computed as filter       ║
// ║  strings, applied per cat by the Neko theme.                                  ║
// ╚═══════════════════════════════════════════════════════════════════════════════╝
const NEKO_COLORS = {
  orange:    'sepia(1) saturate(8) hue-rotate(345deg) brightness(0.85)',
  ginger:    'sepia(1) saturate(8) hue-rotate(355deg) brightness(0.95)',
  marmalade: 'sepia(1) saturate(12) hue-rotate(340deg) brightness(0.85)',
  red:       'sepia(1) saturate(10) hue-rotate(330deg) brightness(0.75)',
  crimson:   'sepia(1) saturate(12) hue-rotate(320deg) brightness(0.75)',
  hotpink:   'sepia(1) saturate(12) hue-rotate(295deg) brightness(1.0)',
  magenta:   'sepia(1) saturate(10) hue-rotate(270deg) brightness(0.9)',
  purple:    'sepia(1) saturate(10) hue-rotate(250deg) brightness(0.8)',
  lavender:  'sepia(1) saturate(6) hue-rotate(240deg) brightness(1.2)',
  blue:      'sepia(1) saturate(10) hue-rotate(200deg) brightness(0.85)',
  navy:      'sepia(1) saturate(10) hue-rotate(200deg) brightness(0.6)',
  cyan:      'sepia(1) saturate(10) hue-rotate(160deg) brightness(1.0)',
  teal:      'sepia(1) saturate(10) hue-rotate(140deg) brightness(0.8)',
  green:     'sepia(1) saturate(10) hue-rotate(90deg) brightness(0.85)',
  lime:      'sepia(1) saturate(12) hue-rotate(60deg) brightness(1.0)',
  olive:     'sepia(1) saturate(8) hue-rotate(50deg) brightness(0.7)',
  yellow:    'sepia(1) saturate(10) hue-rotate(20deg) brightness(1.1)',
  gold:      'sepia(1) saturate(12) hue-rotate(10deg) brightness(1.0)',
  brown:     'sepia(1) saturate(6) hue-rotate(350deg) brightness(0.6)',
  chocolate: 'sepia(1) saturate(8) hue-rotate(345deg) brightness(0.5)',
  gray:      'saturate(0) brightness(1.0)',
  charcoal:  'saturate(0) brightness(0.5)',
  white:     'saturate(0) brightness(1.8)',
};

// ╔═══════════════════════════════════════════════════════════════════════════════╗
// ║  NEKO THEME                                                                   ║
// ║  10 autonomous cats + 1 classic mouse-chaser — 11 cats total                  ║
// ╠═══════════════════════════════════════════════════════════════════════════════╣
// ║  Cats 1–10: fully autonomous.  Each cycles through random behaviours          ║
// ║  (wander, pace, run) on independent timers.  CHASE_MOUSE is excluded from     ║
// ║  the autonomous behaviour pool — they never spontaneously chase the cursor.   ║
// ║                                                                               ║
// ║  Cat 11: the classic.  Always CHASE_MOUSE, never cycles, uses the user's      ║
// ║  saved neko colour (the same one from the toolbar picker) so it looks like    ║
// ║  "their" cat roaming among the autonomous ones.                               ║
// ║                                                                               ║
// ║  Curve pathing: even-indexed autonomous cats follow a sinusoidal overlay      ║
// ║  on straight movement — gives a feline gait rather than robotic lines.        ║
// ╚═══════════════════════════════════════════════════════════════════════════════╝
// Distinct from the single interactive neko above. This is ambient — 10 cats
// roam the page independently, each running their own behavior cycle:
//
//   wander → pace → chase (nearest other cat) → sleep → repeat (randomized)
//
// Curve pathing: a sinusoidal overlay on straight movement (same technique
// as the summer bees) gives the cats a feline gait rather than robotic lines.
// Each cat picks a random curve amplitude and phase — they never move identically.
let themeNekos = [];
let nekoBehaviorTimers = [];

const NEKO_BEHAVIORS = [
  BehaviorMode.RUN_AROUND_RANDOMLY,
  BehaviorMode.PACE_AROUND_SCREEN,
  BehaviorMode.RUN_AROUND,
];

function randomNekoBehavior(exclude) {
  const pool = NEKO_BEHAVIORS.filter(b => b !== exclude);
  return pool[Math.floor(Math.random() * pool.length)];
}

function cycleSingleNeko(neko) {
  // Pick a new behavior different from the current one
  const newBehavior = randomNekoBehavior(neko.behaviorMode);
  neko.behaviorMode = newBehavior;
  // Reset AI state so runRandomly() re-initializes
  neko.catAI = undefined;
  neko.catTimer = 0;
  neko.catBurst = 0;
  neko.cornerIndex = 0;
  neko.ballX = 0; neko.ballY = 0;
  // Wake up if sleeping
  if (neko.state === NekoState.SLEEP || neko.state === NekoState.STOP ||
      neko.state === NekoState.WASH || neko.state === NekoState.SCRATCH) {
    neko.setState(NekoState.AWAKE);
  }
}

function nekoRestSequence(neko) {
  neko.behaviorMode = -1; // freeze behavior so nothing else overwrites
  neko.catAI = undefined; neko.catTimer = 0;
  const doSleep = Math.random() < 0.55 && Date.now() >= (neko.sleepCooldownUntil || 0);
  if (doSleep) {
    // Yawn → sleep for a random duration → wake and resume cycling
    neko.setState(NekoState.YAWN);
    const tid1 = setTimeout(() => {
      if (!themeNekos.includes(neko)) return;
      neko.setState(NekoState.SLEEP);
      const sleepDuration = 8000 + Math.random() * 16000; // 8–24 s
      const tid2 = setTimeout(() => {
        if (!themeNekos.includes(neko)) return;
        neko.sleepCooldownUntil = Date.now() + 10000;
        neko.setState(NekoState.AWAKE);
        cycleSingleNeko(neko);
        scheduleBehaviorCycle(neko);
      }, sleepDuration);
      nekoBehaviorTimers.push(tid2);
    }, 900);
    nekoBehaviorTimers.push(tid1);
  } else {
    // Scratch for a bit → resume
    neko.setState(NekoState.SCRATCH);
    const scratchDuration = 2000 + Math.random() * 3500;
    const tid = setTimeout(() => {
      if (!themeNekos.includes(neko)) return;
      neko.setState(NekoState.AWAKE);
      cycleSingleNeko(neko);
      scheduleBehaviorCycle(neko);
    }, scratchDuration);
    nekoBehaviorTimers.push(tid);
  }
}

function scheduleBehaviorCycle(neko) {
  const delay = 8000 + Math.random() * 12000; // 8–20 seconds
  const tid = setTimeout(() => {
    if (!themeNekos.includes(neko)) return; // already despawned
    // If already resting from a collision interaction, don't interrupt — just reschedule
    if (isNekoResting(neko)) {
      scheduleBehaviorCycle(neko);
      return;
    }
    // 28% chance: take a spontaneous break (scratch or nap)
    if (Math.random() < 0.28) {
      nekoRestSequence(neko);
    } else {
      cycleSingleNeko(neko);
      scheduleBehaviorCycle(neko);
    }
  }, delay);
  nekoBehaviorTimers.push(tid);
}

function isNekoResting(neko) {
  return neko.state === NekoState.SLEEP || neko.state === NekoState.STOP ||
         neko.state === NekoState.WASH || neko.state === NekoState.SCRATCH ||
         neko.state === NekoState.YAWN;
}

// ── CURVE PATHING: bee-style sinuous movement overlay ─────────────────────────
// Attaches to a neko instance and bends its movement each tick
function attachCurvePathing(neko) {
  neko._curve = {
    angle: Math.random() * Math.PI * 2,
    turnRate: 0,
    targetTurn: (Math.random() - 0.5) * 0.06,
    wanderTimer: 0,
    active: true,
  };
  // Monkey-patch processOriginalTick to inject curve offset
  const origTick = neko.processOriginalTick.bind(neko);
  neko.processOriginalTick = function() {
    const c = this._curve;
    if (!c || !c.active) { origTick(); return; }
    // Only apply curve when neko is actively moving (not sleeping/idle)
    const moving = this.state >= NekoState.U_MOVE && this.state <= NekoState.DR_MOVE;
    if (moving) {
      c.wanderTimer++;
      if (c.wanderTimer > 30 + Math.random() * 60) {
        c.targetTurn = (Math.random() - 0.5) * 0.08;
        c.wanderTimer = 0;
      }
      c.turnRate += (c.targetTurn - c.turnRate) * 0.08;
      c.angle += c.turnRate;
      // Offset the logic position with a perpendicular curve
      const perpX = Math.cos(c.angle + Math.PI / 2) * 1.5;
      const perpY = Math.sin(c.angle + Math.PI / 2) * 1.5;
      origTick();
      this.logicX += perpX;
      this.logicY += perpY;
      // Soft boundary nudge
      if (this.logicX < 20) c.angle += 0.04;
      else if (this.logicX > this.boundsWidth - 20) c.angle -= 0.04;
      if (this.logicY < 20) c.angle += (Math.cos(c.angle) > 0 ? 0.04 : -0.04);
      else if (this.logicY > this.boundsHeight - 20) c.angle += (Math.cos(c.angle) > 0 ? -0.04 : 0.04);
      this.logicX = Math.max(0, Math.min(this.boundsWidth, this.logicX));
      this.logicY = Math.max(0, Math.min(this.boundsHeight, this.logicY));
    } else {
      origTick();
    }
  };
}

function onNekoMouseWake(neko) {
  if (!isNekoResting(neko)) return; // only react if sleeping/idle
  neko.sleepCooldownUntil = Date.now() + 15000; // can't sleep again for 15s
  neko.setState(NekoState.AWAKE);
  neko.catAI = undefined; neko.catTimer = 0;
  // 30% chance: yawn then get up and wander (no sleeping — cooldown active)
  if (Math.random() < 0.3) {
    setTimeout(() => {
      if (!themeNekos.includes(neko)) return;
      neko.setState(NekoState.YAWN);
      setTimeout(() => {
        if (!themeNekos.includes(neko)) return;
        neko.setState(NekoState.AWAKE);
        cycleSingleNeko(neko);
      }, 1200);
    }, 400);
  } else {
    // 70% chance: wake up and change behavior immediately
    cycleSingleNeko(neko);
  }
}

// Cats are pointer-events:none (click-through), so hover-wake is a cursor hit-test.
function onNekoThemeMouseMove(e) {
  themeNekos.forEach(n => {
    if (n.isMouseCat) return;
    const r = n.element.getBoundingClientRect();
    if (e.clientX >= r.left && e.clientX <= r.right && e.clientY >= r.top && e.clientY <= r.bottom) onNekoMouseWake(n);
  });
}

function startNekoTheme() {
  stopNekoTheme();
  document.addEventListener('mousemove', onNekoThemeMouseMove);
  const colorKeys = Object.keys(NEKO_COLORS);
  // Initial behavior spread across 10 autonomous cats (no CHASE_MOUSE — that belongs to cat 11)
  const initialBehaviors = [
    BehaviorMode.RUN_AROUND_RANDOMLY, BehaviorMode.RUN_AROUND_RANDOMLY, BehaviorMode.RUN_AROUND_RANDOMLY, BehaviorMode.RUN_AROUND_RANDOMLY,
    BehaviorMode.PACE_AROUND_SCREEN, BehaviorMode.PACE_AROUND_SCREEN, BehaviorMode.PACE_AROUND_SCREEN,
    BehaviorMode.RUN_AROUND, BehaviorMode.RUN_AROUND, BehaviorMode.RUN_AROUND,
  ];
  // Shuffle
  for (let i = initialBehaviors.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [initialBehaviors[i], initialBehaviors[j]] = [initialBehaviors[j], initialBehaviors[i]];
  }
  // Spread spawn positions across the screen to avoid initial overlap
  for (let i = 0; i < 10; i++) {
    const col = i % 5, row = Math.floor(i / 5);
    const x = (col + 0.5) * (window.innerWidth / 5) - 16 + (Math.random() - 0.5) * 40;
    const y = (row + 0.5) * (window.innerHeight / 2) - 16 + (Math.random() - 0.5) * 40;
    const neko = new Neko({ fps: AppSettings.neko_fps, speed: 10 + Math.random() * 14, startX: x, startY: y, behaviorMode: initialBehaviors[i], allowBehaviorChange: false });
    neko.setSprites(NEKO_SPRITE_COUNT);
    const color = colorKeys[Math.floor(Math.random() * colorKeys.length)];
    neko.element.style.filter = NEKO_COLORS[color];
    neko.element.dataset.themeNeko = '1';
    neko._assignedColor = color; // preserve assigned color
    // Half the nekos get bee-style curving paths, half walk straight
    if (i % 2 === 0) attachCurvePathing(neko);
    // Pre-kick neko into an active moving state so they don't all start idle
    const moveStates = [NekoState.U_MOVE, NekoState.D_MOVE, NekoState.L_MOVE, NekoState.R_MOVE, NekoState.UL_MOVE, NekoState.UR_MOVE, NekoState.DL_MOVE, NekoState.DR_MOVE];
    neko.state = moveStates[Math.floor(Math.random() * moveStates.length)];
    neko.tickCount = Math.floor(Math.random() * 100);
    neko.stateCount = Math.floor(Math.random() * 20);
    // Give each neko a random target to move towards
    neko.targetX = Math.random() * window.innerWidth;
    neko.targetY = Math.random() * window.innerHeight;
    neko.oldTargetX = -99999;
    neko.oldTargetY = -99999;
    neko.start();
    themeNekos.push(neko);
    // Schedule first behavior change with staggered delay
    const initialDelay = 5000 + Math.random() * 10000;
    const tid = setTimeout(() => {
      if (!themeNekos.includes(neko)) return;
      cycleSingleNeko(neko);
      scheduleBehaviorCycle(neko);
    }, initialDelay);
    nekoBehaviorTimers.push(tid);
  }
  // ── Cat 11: classic mouse-chaser — always follows cursor, never cycles behavior ──
  {
    const x = window.innerWidth / 2 + (Math.random() - 0.5) * 80;
    const y = window.innerHeight / 2 + (Math.random() - 0.5) * 80;
    const mouseCat = new Neko({ fps: AppSettings.neko_fps, speed: AppSettings.neko_speed, startX: x, startY: y, behaviorMode: BehaviorMode.CHASE_MOUSE, allowBehaviorChange: false });
    mouseCat.setSprites(NEKO_SPRITE_COUNT);
    // Use the user's saved neko colour so this cat looks like "their" cat
    let savedColor = 'orange';
    try { savedColor = localStorage.getItem('neko-color') || 'orange'; } catch(e) {}
    mouseCat.element.style.filter = NEKO_COLORS[savedColor] || NEKO_COLORS.orange;
    mouseCat.element.dataset.themeNeko = '1';
    mouseCat.isMouseCat = true;
    // No behavior cycling — this one always chases the mouse
    mouseCat.start();
    themeNekos.push(mouseCat);
  }
}

function stopNekoTheme() {
  document.removeEventListener('mousemove', onNekoThemeMouseMove);
  nekoBehaviorTimers.forEach(t => clearTimeout(t));
  nekoBehaviorTimers = [];
  themeNekos.forEach(n => { n.stop(); n.destroy(); });
  themeNekos = [];
}

