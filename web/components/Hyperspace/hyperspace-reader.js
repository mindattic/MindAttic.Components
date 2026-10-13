/* HyperspaceReader — a handheld sci-fi scanner that locks on to a Hyperspace shape it cannot make
 * sense of.
 *
 *   var h = HyperspaceReader.show({ x: 120, y: 80, shape: 'tesseract' });
 *   h.close();                       // runs the shutdown sequence, then removes the DOM; returns a Promise
 *
 * The window shows the shape as a projected, rotating wireframe on a scope, a "Dimensional Threshold"
 * danger bar that climbs, jitters and spikes, a rotating set of instrument readouts drawn from a pool
 * of about 100, the shape's details as a fast-scrolling text span, and flags such as UNRESOLVED and
 * NON-EUCLIDEAN. On close it shuts itself down like a program exiting: the readouts freeze, the text
 * collapses, a power-down line prints, and the window folds to a line and fades.
 *
 * Needs window.Hyperspace (hyperspace.js in this folder) loaded first; without it show() returns null.
 * Injects its own stylesheet once (<style id="hsr-style">), so the script is the only include.
 * One requestAnimationFrame loop per open reader, nothing at all once it is closed. Honours
 * prefers-reduced-motion: no rotation, jitter, spikes, glitches or scrolling; a plain fade on close.
 *
 * Classic browser <script> AND require()-able under Node (UMD), no bundler.
 */
(function (root, factory) {
    var api = factory(root);
    if (typeof window !== 'undefined') window.HyperspaceReader = api;
    if (typeof module !== 'undefined' && module.exports) module.exports = api;
    else if (root) root.HyperspaceReader = api;
})(typeof globalThis !== 'undefined' ? globalThis : this, function (root) {
    'use strict';

    // =====================================================================================
    // Instrument pool. Each entry: [name, kind, ...args]
    //   'n'  number   min, max, decimals, unit
    //   's'  signed   max, decimals, unit
    //   'p'  percent  min, max
    //   'i'  integer  min, max
    //   'x'  hex      digits
    //   'w'  word     choices
    //   'g'  shape-derived: 'v' wireframe vertices, 'e' edges, 'd' native dimension, 'r' rotation
    //        planes, 'a' embedding dimension. Echoed back with sensor error, since the reader cannot
    //        trust what it sees.
    // =====================================================================================
    var READOUTS = [
        ['W-AXIS DRIFT', 'n', 0, 40, 2, 'mm/s'],
        ['TESSERACT FLUX', 'n', 0.1, 99, 1, 'kHz'],
        ['HYPERVOLUME', 'n', 0, 12, 3, 'u⁴'],
        ['CHIRALITY INDEX', 's', 1, 3, ''],
        ['EUCLIDEAN DEVIATION', 'p', 12, 100],
        ['MANIFOLD CURVATURE', 's', 4, 3, 'κ'],
        ['GEODESIC SHEAR', 'n', 0, 3.14, 3, 'rad'],
        ['PARALLAX ERROR', 'n', 0, 900, 0, 'arcsec'],
        ['FOLD COUNT', 'i', 2, 64],
        ['HOLONOMY ANGLE', 'n', 0, 360, 1, '°'],
        ['BETTI SCAN b₁', 'i', 0, 12],
        ['EULER TRACE χ', 's', 8, 0, ''],
        ['SPIN-FOAM DENSITY', 'n', 0, 9, 2, 'ρ'],
        ['CALABI-YAU RESIDUE', 'n', 0, 1, 4, ''],
        ['KALUZA-KLEIN MODE', 'i', 1, 11],
        ['BRANE TENSION', 'n', 0, 500, 1, 'GPa'],
        ['CASIMIR PRESSURE', 'n', 0, 80, 2, 'nPa'],
        ['VACUUM ENERGY Δ', 's', 9, 3, 'eV'],
        ['ZERO-POINT NOISE', 'n', -90, -20, 1, 'dB'],
        ['TACHYON COUNT', 'i', 0, 7],
        ['CAUSALITY MARGIN', 's', 40, 1, 'ms'],
        ['CHRONON SKEW', 'n', 0, 999, 0, 'ps'],
        ['ENTROPY GRADIENT', 's', 5, 2, 'k/s'],
        ['PHASE COHERENCE', 'p', 0, 61],
        ['QUBIT DECOHERENCE', 'n', 0, 90, 1, 'µs'],
        ['ENTANGLEMENT DEPTH', 'i', 1, 24],
        ['WAVEFUNCTION SPREAD', 'n', 0, 6, 2, 'σ'],
        ['EIGENSTATE COLLAPSE', 'p', 0, 100],
        ['HILBERT RANK', 'i', 3, 4096],
        ['SPECTRAL GAP', 'n', 0, 40, 2, 'meV'],
        ['GRAVITON SCATTER', 'i', 0, 9000],
        ['TIDAL STRESS', 'n', 0, 30, 2, 'g/m'],
        ['FRAME DRAG', 'n', 0, 70, 1, 'µrad/s'],
        ['REDSHIFT', 'n', 0, 3, 4, 'z'],
        ['LENSING GAIN', 'n', 0.5, 9, 2, '×'],
        ['HAWKING FLUX', 'n', 0, 999, 0, 'fW'],
        ['TOPOLOGICAL GENUS', 'i', 0, 9],
        ['JONES POLYNOMIAL', 'w', ['t+t³−t⁴', '1', 'q⁻¹+q', 'UNKNOT?', 'UNDEFINED']],
        ['LINKING NUMBER', 's', 4, 0, ''],
        ['WINDING NUMBER', 's', 9, 0, ''],
        ['SYMMETRY BREAK', 'p', 0, 100],
        ['LATTICE STRAIN', 'n', 0, 999, 0, 'ppm'],
        ['QUASI-ORDER Φ', 'n', 1.5, 1.7, 4, ''],
        ['APERIODICITY', 'p', 40, 100],
        ['GOLDEN DRIFT φ−x', 's', 0.2, 4, ''],
        ['VERTEX ECHO', 'g', 'v'],
        ['EDGE ECHO', 'g', 'e'],
        ['NATIVE AXES', 'g', 'd'],
        ['EMBEDDING AXES', 'g', 'a'],
        ['ROTATION PLANES', 'g', 'r'],
        ['PROJECTION LOSS', 'p', 20, 99],
        ['SHADOW FIDELITY', 'p', 1, 48],
        ['HYPERPLANE HITS', 'i', 0, 600],
        ['CROSS-SECTION AREA', 'n', 0, 9, 3, 'u²'],
        ['SLICE OFFSET w', 's', 1.6, 2, 'u'],
        ['SLICE OFFSET v', 's', 1.6, 2, 'u'],
        ['ISOCLINIC RATE', 'n', 0, 1, 3, 'rad/s'],
        ['CLIFFORD PARALLELISM', 'p', 0, 100],
        ['HOPF FIBER COUNT', 'i', 1, 360],
        ['SPINOR PHASE', 'n', 0, 720, 1, '°'],
        ['QUATERNION NORM', 'n', 0.9, 1.1, 4, ''],
        ['OCTONION ASSOCIATOR', 's', 1, 4, ''],
        ['LIE BRACKET RESIDUE', 'n', 0, 1, 5, ''],
        ['ROOT LENGTH VARIANCE', 'n', 0, 0.5, 4, ''],
        ['WEYL ORDER (EST)', 'w', ['120', '1152', '14400', '696729600', '> 10⁹', '???']],
        ['COXETER NUMBER (EST)', 'i', 2, 30],
        ['SCHLÄFLI PARSE', 'p', 0, 38],
        ['DUAL MISMATCH', 'n', 0, 1, 3, ''],
        ['ORIENTABILITY', 'w', ['YES', 'NO', 'BOTH', 'UNDEFINED', 'FLIPPING']],
        ['NON-EUCLIDEAN INDEX', 'n', 0, 9, 2, ''],
        ['ANGLE-SUM EXCESS', 's', 180, 1, '°'],
        ['TRANSPORT ERROR', 'n', 0, 90, 2, '°'],
        ['METRIC SIGNATURE', 'w', ['(−,+,+,+)', '(+,+,+,+)', '(−,−,+,+)', '(?,?,?,?)', '(+,+,+,+,+)']],
        ['LIGHT-CONE TILT', 's', 45, 1, '°'],
        ['THROAT RADIUS', 'n', 0, 99, 1, 'µm'],
        ['EXOTIC MATTER', 's', 9, 3, 'g'],
        ['NEGATIVE-MASS PROBE', 'w', ['NULL', 'POSITIVE', 'NEGATIVE', 'IMAGINARY', 'NO RETURN']],
        ['DARK FLOW', 'n', 0, 900, 0, 'km/s'],
        ['SUBSPACE HARMONIC', 'n', 0, 20000, 0, 'Hz'],
        ['RESONANCE Q', 'n', 1, 9999, 0, ''],
        ['MONOPOLE COUNT', 'i', 0, 3],
        ['AXION FIELD', 'n', 0, 99, 2, 'neV'],
        ['INFLATON RESIDUE', 'n', 0, 1, 4, ''],
        ['STRING TENSION', 'n', 0, 10, 3, 'Gµ'],
        ['DOMAIN WALLS', 'i', 0, 16],
        ['FALSE-VACUUM RISK', 'p', 0, 12],
        ['NUCLEATION RATE', 'n', 0, 9, 3, '/s'],
        ['STRANGELET COUNT', 'i', 0, 40],
        ['PLANCK GRAIN', 'n', 1, 99, 1, 'ℓp'],
        ['MIRROR PARITY', 'w', ['EVEN', 'ODD', 'MIXED', 'INVERTED', 'ERR']],
        ['TEMPORAL ECHO', 'n', 0, 9, 2, 's'],
        ['RETICLE LAG', 'n', 0, 400, 0, 'ms'],
        ['SIGNAL / NOISE', 'n', -12, 9, 1, 'dB'],
        ['SENSOR SATURATION', 'p', 60, 100],
        ['CALIBRATION DRIFT', 'p', 4, 90],
        ['CHECKSUM', 'x', 8],
        ['CLASSIFIER CONFIDENCE', 'p', 0, 9],
        ['PATTERN MATCH', 'p', 0, 4],
        ['CONTAINMENT INTEGRITY', 'p', 8, 70],
        ['OPERATOR EXPOSURE', 'n', 0, 99, 2, 'mSv'],
        ['PROBABILITY CLOUD', 'p', 30, 100]
    ];

    var FLAGS = [
        'NON-EUCLIDEAN', 'NO MATCH', 'TOPOLOGY ??', 'CHIRAL FLIP', 'W-AXIS BLEED', 'SELF-INTERSECTING',
        'CAUSALITY WARN', 'SHADOW ONLY', 'DIM > 3', 'PARITY ERR', 'INSIDE = OUTSIDE', 'UNCLASSIFIED',
        'RECURSIVE', 'ORIENTATION LOST', 'PROJECTION LOSSY', 'ANGLES DO NOT SUM', 'UNBOUNDED',
        'NO STABLE VIEW'
    ];
    var STATUS_LIVE = ['UNRESOLVED', 'NO MATCH', 'RE-SCANNING', 'ANALYZING', 'CANNOT PARSE', 'UNRESOLVED'];
    var NOISE_LINES = [
        '>> resample axis {a} .......... fail',
        '>> project R{d} -> R3 loss {p}%',
        '>> match catalog ......... 0 hits',
        '>> invert w-axis ......... ERR',
        '>> chirality test ........ both',
        '>> parallel transport .... drift',
        '>> lock retry #{n}',
        '0x{h} 0x{h} 0x{h} 0x{h}',
        '>> fold detect ........... {n} folds',
        '>> euclidean fit ......... rejected',
        '>> inside/outside test ... undefined',
        '>> buffer underrun 0x{h}'
    ];

    var CSS = [
        '.hsr{position:absolute;z-index:2;width:282px;box-sizing:border-box;pointer-events:none;',
        'font-family:"Courier New",Courier,monospace;font-size:0.56rem;line-height:1.35;color:rgba(70,210,170,0.82);',
        'background:rgba(8,12,14,0.93);border:1px solid rgba(48,90,200,0.55);border-radius:4px;overflow:hidden;',
        'box-shadow:0 4px 18px rgba(0,0,0,0.45),0 0 14px rgba(48,120,255,0.12),inset 0 0 22px rgba(70,210,170,0.05);',
        'backdrop-filter:blur(3px);opacity:var(--hsr-opacity,0.9);transform-origin:50% 50%;',
        'animation:hsr-in .24s cubic-bezier(.22,1,.36,1) both}',
        '.hsr--fixed{position:fixed}',
        '@keyframes hsr-in{from{opacity:0;transform:scaleY(.06) scaleX(.88)}to{opacity:var(--hsr-opacity,0.9);transform:none}}',
        '.hsr::after{content:"";position:absolute;inset:0;pointer-events:none;',
        'background:repeating-linear-gradient(to bottom,rgba(70,210,170,0.05) 0 1px,transparent 1px 3px)}',
        '.hsr-title{display:flex;align-items:center;gap:5px;padding:2px 7px;font-size:0.5rem;letter-spacing:.06em;',
        'background:rgba(10,16,30,0.95);color:rgba(100,150,230,0.8);border-bottom:1px solid rgba(48,90,200,0.42)}',
        '.hsr-led{width:5px;height:5px;border-radius:50%;background:#46d2aa;box-shadow:0 0 5px #46d2aa;flex:none}',
        '.hsr-title-text{flex:1;white-space:nowrap;overflow:hidden}',
        '.hsr-status{color:rgba(255,200,60,0.92);white-space:nowrap}',
        '.hsr-body{display:flex;gap:7px;padding:6px 7px 4px}',
        '.hsr-scope{position:relative;flex:none;width:118px;height:118px;border:1px solid rgba(70,210,170,0.22);background:rgba(0,10,8,0.6)}',
        '.hsr-scope canvas{display:block;width:118px;height:118px}',
        '.hsr-scope::before,.hsr-scope::after{content:"";position:absolute;width:14px;height:14px;pointer-events:none;',
        'border-color:rgba(255,200,60,0.85);border-style:solid;transition:all .35s cubic-bezier(.22,1,.36,1)}',
        '.hsr-scope::before{left:3px;top:3px;border-width:1px 0 0 1px}',
        '.hsr-scope::after{right:3px;bottom:3px;border-width:0 1px 1px 0}',
        '.hsr--live .hsr-scope::before{left:14px;top:14px}',
        '.hsr--live .hsr-scope::after{right:14px;bottom:14px}',
        '.hsr-side{flex:1;min-width:0;display:flex;flex-direction:column;gap:3px}',
        '.hsr-obj{color:rgba(232,236,240,0.9);font-weight:bold;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
        '.hsr-sub{color:rgba(70,210,170,0.6);white-space:nowrap;overflow:hidden;text-overflow:ellipsis;font-size:0.48rem}',
        '.hsr-ro{display:flex;justify-content:space-between;gap:4px;border-bottom:1px dotted rgba(70,210,170,0.14);white-space:nowrap}',
        '.hsr-ro-k{color:rgba(100,150,230,0.85);overflow:hidden;text-overflow:ellipsis;font-size:0.47rem}',
        '.hsr-ro-v{color:rgba(232,236,240,0.88)}',
        '.hsr-ro--odd .hsr-ro-v{color:rgba(255,90,110,0.95)}',
        '.hsr-thresh{padding:2px 7px 3px}',
        '.hsr-thresh-head{display:flex;justify-content:space-between;font-size:0.5rem;letter-spacing:.05em;color:rgba(255,200,60,0.9)}',
        '.hsr-bar{position:relative;height:7px;margin-top:2px;border:1px solid rgba(255,200,60,0.45);background:rgba(255,200,60,0.06);overflow:hidden}',
        '.hsr-bar-fill{position:absolute;inset:0;clip-path:inset(0 100% 0 0);',
        'background:linear-gradient(90deg,rgba(70,210,170,0.85) 0%,rgba(255,200,60,0.9) 58%,rgba(255,51,85,0.95) 86%)}',
        '.hsr-bar-ticks{position:absolute;inset:0;background:repeating-linear-gradient(90deg,transparent 0 9px,rgba(8,12,14,0.9) 9px 10px)}',
        '.hsr-bar-peak{position:absolute;top:0;bottom:0;width:2px;left:0;background:rgba(255,255,255,0.85)}',
        '.hsr--crit .hsr-thresh-head{color:rgba(255,80,100,1)}',
        '.hsr--crit{border-color:rgba(255,51,85,0.7)}',
        '.hsr--spike{box-shadow:0 0 18px rgba(255,51,85,0.45),inset 0 0 18px rgba(255,51,85,0.12)}',
        '.hsr--spike .hsr-body{transform:translateX(1px)}',
        '.hsr-flags{display:flex;flex-wrap:nowrap;overflow:hidden;gap:3px;padding:0 7px 3px;height:11px}',
        '.hsr-flag{flex:none;white-space:nowrap;font-size:0.45rem;letter-spacing:.05em;padding:0 3px;border:1px solid rgba(255,51,85,0.6);color:rgba(255,90,110,0.95);',
        'animation:hsr-blink 1.1s steps(1) infinite}',
        '.hsr-flag:nth-child(2n){animation-delay:.37s;border-color:rgba(255,200,60,0.55);color:rgba(255,200,60,0.92)}',
        '@keyframes hsr-blink{0%,70%{opacity:1}71%,100%{opacity:.35}}',
        '.hsr-details{height:42px;overflow:hidden;margin:0 7px 5px;padding-top:2px;border-top:1px solid rgba(70,210,170,0.15);transform-origin:50% 0}',
        '.hsr-details-inner{white-space:pre;font-size:0.46rem;line-height:8px;color:rgba(70,210,170,0.7);will-change:transform}',
        '.hsr-foot{height:0;overflow:hidden;padding:0 7px;color:rgba(255,200,60,0.95);white-space:nowrap;font-size:0.5rem}',
        '.hsr--closing .hsr-led{background:#ffc83c;box-shadow:0 0 5px #ffc83c}',
        '.hsr--closing .hsr-ro-v,.hsr--closing .hsr-ro-k{color:rgba(150,160,170,0.55)}',
        '.hsr--closing .hsr-details{transition:transform .32s ease-in,opacity .32s ease-in;transform:scaleY(0);opacity:0}',
        '.hsr--closing .hsr-flags{transition:opacity .25s;opacity:0}',
        '.hsr--closing .hsr-foot{height:12px;padding:1px 7px 3px}',
        '.hsr--closing .hsr-flag{animation:none}',
        '.hsr--off{animation:hsr-off .42s cubic-bezier(.5,0,1,.6) forwards}',
        '@keyframes hsr-off{0%{opacity:var(--hsr-opacity,0.9);transform:none;filter:none}',
        '45%{opacity:var(--hsr-opacity,0.9);transform:scaleX(1) scaleY(.02);filter:brightness(2.4)}',
        '100%{opacity:0;transform:scaleX(0) scaleY(.02);filter:brightness(3)}}',
        '.hsr--reduced,.hsr--reduced .hsr-flag{animation:none}',
        '.hsr--reduced .hsr-scope::before,.hsr--reduced .hsr-scope::after{transition:none}',
        '.hsr--reduced.hsr--closing .hsr-details{transition:none}',
        '.hsr--reduced.hsr--off{animation:none;transition:opacity .3s linear;opacity:0}',
        '@media (max-width:480px){.hsr{width:214px}.hsr-scope,.hsr-scope canvas{width:84px;height:84px}.hsr-details{height:26px}}'
    ].join('\n');

    var open = [];

    function injectStyles(doc) {
        doc = doc || root.document;
        if (!doc || doc.getElementById('hsr-style')) return;
        var st = doc.createElement('style');
        st.id = 'hsr-style';
        st.textContent = CSS;
        (doc.head || doc.documentElement).appendChild(st);
    }

    // ---- small helpers ------------------------------------------------------------------
    function rnd(a, b) { return a + Math.random() * (b - a); }
    function irnd(a, b) { return Math.floor(rnd(a, b + 1)); }
    function pick(arr) { return arr[Math.floor(Math.random() * arr.length)]; }
    function hex(n) { var s = ''; for (var i = 0; i < n; i++) s += '0123456789ABCDEF'[irnd(0, 15)]; return s; }
    function el(tag, cls, text) {
        var e = root.document.createElement(tag);
        if (cls) e.className = cls;
        if (text !== undefined) e.textContent = text;
        return e;
    }
    function css(v) { return (typeof v === 'number') ? v + 'px' : String(v); }
    var GLITCH = '▒░▓█#%&?/\\';
    function scramble(s, amount) {
        var out = '';
        for (var i = 0; i < s.length; i++) out += (s[i] !== ' ' && Math.random() < amount) ? GLITCH[irnd(0, GLITCH.length - 1)] : s[i];
        return out;
    }
    function fill(line, ctx) {
        return line.replace(/\{a\}/g, function () { return 'x' + irnd(1, ctx.dim); })
            .replace(/\{d\}/g, function () { return String(ctx.embed); })
            .replace(/\{p\}/g, function () { return rnd(20, 99).toFixed(1); })
            .replace(/\{n\}/g, function () { return String(irnd(2, 99)); })
            .replace(/\{h\}/g, function () { return hex(4); });
    }

    // One reading of an instrument. `odd` marks values the reader itself does not believe.
    function reading(spec, ctx, prev) {
        var kind = spec[1], v, txt;
        if (Math.random() < 0.07) return { text: pick(['∞', 'NaN', '-0.000', '??.?', '▒▒.▒', 'ERR', 'OVF']), odd: true };
        switch (kind) {
            case 'n':
                v = (prev && typeof prev.v === 'number') ? prev.v + (spec[3] - spec[2]) * rnd(-0.06, 0.06) : rnd(spec[2], spec[3]);
                v = Math.max(spec[2], Math.min(spec[3], v));
                txt = v.toFixed(spec[4]) + (spec[5] ? ' ' + spec[5] : '');
                if (Math.random() < 0.08) txt += ' ±' + (Math.abs(v) * rnd(0.3, 2)).toFixed(spec[4]);
                return { v: v, text: txt };
            case 's':
                v = (prev && typeof prev.v === 'number') ? prev.v + spec[2] * rnd(-0.1, 0.1) : rnd(-spec[2], spec[2]);
                v = Math.max(-spec[2], Math.min(spec[2], v));
                txt = (v >= 0 ? '+' : '−') + Math.abs(v).toFixed(spec[3]) + (spec[4] ? ' ' + spec[4] : '');
                if (Math.random() < 0.05) txt += 'i';
                return { v: v, text: txt, odd: /i$/.test(txt) };
            case 'p':
                v = (prev && typeof prev.v === 'number') ? prev.v + rnd(-3, 3) : rnd(spec[2], spec[3]);
                v = Math.max(spec[2], Math.min(spec[3], v));
                return { v: v, text: v.toFixed(1) + '%' };
            case 'i':
                v = irnd(spec[2], spec[3]);
                return { v: v, text: String(v) };
            case 'x':
                return { text: '0x' + hex(spec[2]) };
            case 'w':
                v = pick(spec[2]);
                return { text: v, odd: /UNDEFINED|ERR|\?|NO RETURN|IMAGINARY/.test(v) };
            case 'g':
                var base = ({ v: ctx.vertices, e: ctx.edges, d: ctx.dim, r: ctx.planes, a: ctx.embed })[spec[2]];
                if (Math.random() < 0.55) return { text: String(base), v: base };
                var off = Math.max(1, Math.round(base * rnd(0.02, 0.4)));
                return { text: String(base + (Math.random() < 0.5 ? -off : off)) + ' ?', v: base, odd: true };
        }
        return { text: '--' };
    }

    // =====================================================================================
    // show()
    // =====================================================================================
    function show(opts) {
        opts = opts || {};
        var H = opts.library || root.Hyperspace;
        if (!H || !root.document) return null;
        var doc = root.document;
        injectStyles(doc);

        var shape;
        try { shape = H.get(opts.shape !== undefined && opts.shape !== null ? opts.shape : irnd(0, H.count - 1)); }
        catch (e) { shape = H.get(irnd(0, H.count - 1)); }
        var st = H.stats(shape);
        var ctxInfo = { vertices: st.vertices, edges: st.edges, embed: st.embedDim, dim: shape.dim, planes: shape.rot.length };

        var reduced = (opts.reducedMotion !== undefined) ? !!opts.reducedMotion
            : !!(root.matchMedia && root.matchMedia('(prefers-reduced-motion: reduce)').matches);

        var host = opts.host || doc.body;
        var fixed = (host === doc.body);

        // ---- DOM ----------------------------------------------------------------------
        var win = el('div', 'hsr' + (fixed ? ' hsr--fixed' : '') + (reduced ? ' hsr--reduced' : ''));
        win.setAttribute('role', 'img');
        win.setAttribute('aria-label', 'Hyperspace Reader scanning ' + shape.name + ': unresolved');
        win.dataset.shape = shape.id;
        win.dataset.state = 'acquiring';
        win.style.left = css(opts.x !== undefined ? opts.x : 24);
        win.style.top = css(opts.y !== undefined ? opts.y : 24);
        if (opts.opacity !== undefined) win.style.setProperty('--hsr-opacity', String(opts.opacity));

        var serial = 'HSR-' + irnd(2, 9) + hex(1);
        var title = el('div', 'hsr-title');
        title.appendChild(el('span', 'hsr-led'));
        title.appendChild(el('span', 'hsr-title-text', 'HYPERSPACE READER // ' + serial));
        var statusEl = el('span', 'hsr-status', 'ACQUIRING');
        title.appendChild(statusEl);
        win.appendChild(title);

        var body = el('div', 'hsr-body');
        var scope = el('div', 'hsr-scope');
        var cv = el('canvas', 'hsr-canvas');
        scope.appendChild(cv);
        body.appendChild(scope);
        var side = el('div', 'hsr-side');
        var objText = 'OBJ ' + shape.id.toUpperCase();
        var objEl = el('div', 'hsr-obj', objText);
        var subEl = el('div', 'hsr-sub', 'SIGNATURE ' + hex(4) + '-' + hex(4));
        side.appendChild(objEl);
        side.appendChild(subEl);
        var ROWS = 5, rows = [];
        for (var r = 0; r < ROWS; r++) {
            var row = el('div', 'hsr-ro');
            var k = el('span', 'hsr-ro-k', '----');
            var v = el('span', 'hsr-ro-v', '--');
            row.appendChild(k); row.appendChild(v);
            side.appendChild(row);
            rows.push({ row: row, k: k, v: v, spec: null, last: null, next: 0, settle: 0 });
        }
        body.appendChild(side);
        win.appendChild(body);

        var thresh = el('div', 'hsr-thresh');
        var th = el('div', 'hsr-thresh-head');
        th.appendChild(el('span', '', 'DIMENSIONAL THRESHOLD'));
        var thVal = el('span', 'hsr-thresh-val', '0.0%');
        th.appendChild(thVal);
        thresh.appendChild(th);
        var bar = el('div', 'hsr-bar');
        var barFill = el('div', 'hsr-bar-fill');
        var barPeak = el('div', 'hsr-bar-peak');
        bar.appendChild(barFill);
        bar.appendChild(el('div', 'hsr-bar-ticks'));
        bar.appendChild(barPeak);
        thresh.appendChild(bar);
        win.appendChild(thresh);

        var flagsEl = el('div', 'hsr-flags');
        win.appendChild(flagsEl);
        var details = el('div', 'hsr-details');
        var detailsInner = el('span', 'hsr-details-inner');
        detailsInner.style.display = 'block';
        details.appendChild(detailsInner);
        win.appendChild(details);
        var foot = el('div', 'hsr-foot');
        win.appendChild(foot);

        host.appendChild(win);

        // ---- canvas -------------------------------------------------------------------
        var dpr = Math.min(root.devicePixelRatio || 1, 2);
        var S = scope.clientWidth || 118;
        cv.width = Math.round(S * dpr); cv.height = Math.round(S * dpr);
        var g = cv.getContext('2d');
        var grid = doc.createElement('canvas');
        grid.width = cv.width; grid.height = cv.height;
        (function drawGrid() {
            var c = grid.getContext('2d');
            c.scale(dpr, dpr);
            c.strokeStyle = 'rgba(70,210,170,0.13)'; c.lineWidth = 0.6;
            for (var i = 1; i <= 3; i++) { c.beginPath(); c.arc(S / 2, S / 2, S * 0.155 * i, 0, Math.PI * 2); c.stroke(); }
            c.beginPath(); c.moveTo(S / 2, 4); c.lineTo(S / 2, S - 4); c.moveTo(4, S / 2); c.lineTo(S - 4, S / 2); c.stroke();
            c.fillStyle = 'rgba(70,210,170,0.3)';
            for (var a = 0; a < 36; a++) {
                var ang = a * Math.PI / 18, rr = S * 0.47;
                c.fillRect(S / 2 + Math.cos(ang) * rr - 0.5, S / 2 + Math.sin(ang) * rr - 0.5, 1, 1);
            }
        })();

        // ---- state --------------------------------------------------------------------
        var t0 = now(), lastFrame = 0, lastDraw = 0, raf = 0;
        var state = 'acquiring';
        var poseT = rnd(0, 20);                 // where in the exhibit's rotation the scan starts
        var target = Math.min(0.97, 0.5 + 0.045 * (shape.dim - 3) + rnd(0, 0.12));
        var level = 0.04, spike = 0, nextSpike = t0 + rnd(1400, 2600), peak = 0;
        var glitchUntil = 0, nextGlitch = t0 + rnd(900, 2000);
        var nextStatus = 0, statusIdx = 0;
        var nextRotate = t0 + 1800, rotateRow = 0;
        var flagQueue = ['UNRESOLVED'].concat(shuffle(FLAGS.slice()).slice(0, 2));
        var nextFlag = t0 + 1000;
        var detailSrc = H.details(shape), detailIdx = 0, detailCount = 0;
        var lineH = 8, visLines = 8, buf = [], scrollOff = 0;
        var collapse = 1;                       // scope object scale, 1 -> 0 on close
        var closePromise = null, resolveClose = null, closed = false, ttlTimer = 0;
        var used = {};

        function shuffle(a) { for (var i = a.length - 1; i > 0; i--) { var j = irnd(0, i); var x = a[i]; a[i] = a[j]; a[j] = x; } return a; }
        function now() { return (root.performance && root.performance.now) ? root.performance.now() : Date.now(); }

        function nextDetailLine() {
            detailCount++;
            if (detailCount % 3 === 0) return fill(pick(NOISE_LINES), ctxInfo);
            var line = detailSrc[detailIdx % detailSrc.length];
            detailIdx++;
            if (!reduced && Math.random() < 0.12) line = scramble(line, 0.25);
            return line;
        }
        for (var bi = 0; bi < visLines + 2; bi++) buf.push(nextDetailLine());
        detailsInner.textContent = buf.join('\n');

        function newSpec() {
            var spec, guard = 0;
            do { spec = pick(READOUTS); guard++; } while (used[spec[0]] && guard < 20);
            used[spec[0]] = true;
            return spec;
        }
        function assignRow(rw, t) {
            if (rw.spec) delete used[rw.spec[0]];
            rw.spec = newSpec();
            rw.last = null;
            rw.k.textContent = rw.spec[0];
            rw.settle = reduced ? 0 : t + 260;
            rw.next = 0;
        }
        function updateRow(rw, t) {
            if (!rw.spec || t < rw.next) return;
            var rd = reading(rw.spec, ctxInfo, rw.last);
            rw.last = rd;
            var txt = rd.text;
            if (t < rw.settle) txt = scramble(txt, 0.7);
            rw.v.textContent = txt;
            rw.row.className = 'hsr-ro' + (rd.odd ? ' hsr-ro--odd' : '');
            rw.next = t + (reduced ? 2000 : (t < rw.settle ? 60 : rnd(140, 320)));
        }

        // ---- scope drawing ------------------------------------------------------------
        function drawScope(t) {
            g.setTransform(dpr, 0, 0, dpr, 0, 0);
            g.clearRect(0, 0, S, S);
            g.drawImage(grid, 0, 0, S, S);
            var el2 = (t - t0) / 1000;
            if (state === 'acquiring' && !reduced) {
                g.fillStyle = 'rgba(70,210,170,0.35)';
                for (var i = 0; i < 90; i++) g.fillRect(Math.random() * S, Math.random() * S, 1, 1);
            }
            var alpha = state === 'acquiring' ? 0 : Math.min(1, (t - t0 - 450) / 500);
            if (alpha > 0 && collapse > 0.01) {
                var tt = reduced ? poseT : poseT + el2;
                var glitching = !reduced && t < glitchUntil;
                g.save();
                g.translate(S / 2, S / 2);
                g.scale(collapse, collapse);
                g.translate(-S / 2, -S / 2);
                // the view never settles: the tilt wanders, and a faint temporal echo trails the
                // object (skipped for very dense wireframes to keep a frame cheap)
                var pitch = reduced ? 0.35 : 0.35 + 0.3 * Math.sin(el2 * 0.7);
                if (!reduced && ctxInfo.edges < 3000) {
                    H.draw(g, shape, tt - 0.45, { size: S, color: '#6496e6', alpha: alpha * 0.22, lineWidth: 0.6, fill: 0.86, pitch: pitch - 0.08 });
                }
                if (glitching) {
                    g.save(); g.translate(rnd(-3, 3), 0);
                    H.draw(g, shape, tt + 0.05, { size: S, color: '#ff3355', alpha: alpha * 0.45, lineWidth: 0.7, fill: 0.86, pitch: pitch });
                    g.restore();
                }
                H.draw(g, shape, tt, { size: S, color: '#46d2aa', alpha: alpha * (glitching ? 0.8 : 1), lineWidth: 0.7, fill: 0.86, pitch: pitch });
                g.restore();
                if (glitching) {
                    for (var s = 0; s < 3; s++) {
                        var y = Math.floor(rnd(0, S - 8)) * dpr, h = Math.floor(rnd(2, 9)) * dpr, dx = rnd(-8, 8) * dpr;
                        g.setTransform(1, 0, 0, 1, 0, 0);
                        g.drawImage(cv, 0, y, cv.width, h, dx, y, cv.width, h);
                    }
                    g.setTransform(dpr, 0, 0, dpr, 0, 0);
                }
            }
            if (state === 'closing') {
                g.fillStyle = 'rgba(232,236,240,' + (0.9 * collapse).toFixed(3) + ')';
                g.beginPath(); g.arc(S / 2, S / 2, 1.5 + 2 * (1 - collapse), 0, Math.PI * 2); g.fill();
                return;
            }
            if (!reduced) {
                var sw = el2 * 2.4;
                g.strokeStyle = 'rgba(70,210,170,0.35)'; g.lineWidth = 1;
                g.beginPath(); g.moveTo(S / 2, S / 2); g.lineTo(S / 2 + Math.cos(sw) * S * 0.47, S / 2 + Math.sin(sw) * S * 0.47); g.stroke();
            }
        }

        // ---- threshold ------------------------------------------------------------------
        function updateThreshold(t, dt) {
            var shown;
            if (state === 'closing') {
                level += (0 - level) * Math.min(1, dt * 6);
                shown = level;
            } else if (reduced) {
                level = state === 'acquiring' ? 0.1 : target;
                shown = level;
            } else {
                level += (target - level) * Math.min(1, dt * 0.55);
                if (t > nextSpike && state === 'live') {
                    spike = rnd(0.12, 0.34);
                    nextSpike = t + rnd(1100, 3000);
                    if (Math.random() < 0.5) glitchUntil = t + rnd(120, 260);
                }
                spike *= Math.exp(-dt * 7);
                var jitter = (Math.random() - 0.5) * 0.05 + Math.sin(t / 97) * 0.012;
                shown = Math.max(0, level + jitter + spike);
            }
            peak = Math.max(shown, peak - dt * 0.08);
            barFill.style.clipPath = 'inset(0 ' + ((1 - Math.min(1, shown)) * 100).toFixed(2) + '% 0 0)';
            barPeak.style.left = (Math.min(1, peak) * 100).toFixed(2) + '%';
            thVal.textContent = (shown > 1 && Math.floor(t / 180) % 2) ? 'OVERFLOW' : (shown * 100).toFixed(1) + '%';
            win.dataset.threshold = shown.toFixed(3);
            var crit = shown >= 0.85, spiking = spike > 0.06;
            if (crit !== win._crit) { win.classList.toggle('hsr--crit', crit); win._crit = crit; }
            if (spiking !== win._spk) { win.classList.toggle('hsr--spike', spiking); win._spk = spiking; }
        }

        // ---- details scroll -------------------------------------------------------------
        function updateDetails(t, dt) {
            if (state === 'closing') return;
            if (reduced) {
                if (t > (win._nextDetail || 0)) {
                    buf.shift(); buf.push(nextDetailLine());
                    detailsInner.textContent = buf.join('\n');
                    win._nextDetail = t + 2500;
                }
                return;
            }
            if (state === 'acquiring') return;
            scrollOff += dt * 52;              // px per second: a fast printout
            while (scrollOff >= lineH) {
                scrollOff -= lineH;
                buf.shift(); buf.push(nextDetailLine());
                detailsInner.textContent = buf.join('\n');
            }
            detailsInner.style.transform = 'translateY(' + (-scrollOff).toFixed(1) + 'px)';
        }

        // ---- main loop ------------------------------------------------------------------
        function frame() {
            raf = 0;
            if (closed) return;
            var t = now();
            var dt = lastFrame ? Math.min(0.1, (t - lastFrame) / 1000) : 0.016;
            lastFrame = t;

            if (state === 'acquiring' && t - t0 > 450) {
                state = 'live';
                win.dataset.state = 'live';
                win.classList.add('hsr--live');
                statusEl.textContent = 'LOCKING';
                nextStatus = t + 600;
                for (var i = 0; i < rows.length; i++) assignRow(rows[i], t);
            }
            if (state === 'live') {
                if (t > nextStatus) {
                    statusEl.textContent = STATUS_LIVE[statusIdx++ % STATUS_LIVE.length];
                    nextStatus = t + (reduced ? 2400 : rnd(900, 1500));
                }
                if (flagQueue.length && t > nextFlag) {
                    flagsEl.appendChild(el('span', 'hsr-flag', flagQueue.shift()));
                    nextFlag = t + (reduced ? 0 : 380);
                }
                if (t > nextRotate) {
                    assignRow(rows[rotateRow % rows.length], t);
                    rotateRow++;
                    nextRotate = t + (reduced ? 3200 : rnd(1300, 2100));
                }
                for (var j = 0; j < rows.length; j++) updateRow(rows[j], t);
                if (!reduced && t > nextGlitch) {
                    glitchUntil = t + rnd(90, 220);
                    nextGlitch = t + rnd(1200, 3200);
                    if (Math.random() < 0.5) objEl.textContent = scramble(objText, 0.35);
                } else if (t > glitchUntil && objEl.textContent !== objText) {
                    objEl.textContent = objText;
                }
            }
            if (state === 'closing' && !reduced) collapse = Math.max(0, collapse - dt * 2.6);

            updateThreshold(t, dt);
            updateDetails(t, dt);
            // the scope redraws at ~30 fps; when reduced, only while something on it changes
            var scopeDue = reduced ? (state !== 'live' || !win._drawnStatic) : (t - lastDraw >= 33);
            if (scopeDue) {
                drawScope(t);
                lastDraw = t;
                if (reduced && state === 'live' && t - t0 > 1000) win._drawnStatic = true;
            }
            raf = root.requestAnimationFrame(frame);
        }
        raf = root.requestAnimationFrame(frame);

        // ---- shutdown -------------------------------------------------------------------
        function close() {
            if (closePromise) return closePromise;
            closePromise = new Promise(function (res) { resolveClose = res; });
            if (ttlTimer) { clearTimeout(ttlTimer); ttlTimer = 0; }
            state = 'closing';
            win.dataset.state = 'closing';
            win.classList.add('hsr--closing');
            win.classList.remove('hsr--spike');
            statusEl.textContent = 'SHUTDOWN';
            objEl.textContent = objText;
            // readouts freeze where they are: no more updates are scheduled once state is 'closing'
            var msg = '> ' + serial + ' :: NO CONCLUSION. POWERING DOWN';
            var finish = function () {
                if (closed) return;
                closed = true;
                if (raf) root.cancelAnimationFrame(raf);
                raf = 0;
                if (win.parentNode) win.parentNode.removeChild(win);
                var ix = open.indexOf(handle);
                if (ix >= 0) open.splice(ix, 1);
                handle.closed = true;
                resolveClose();
            };
            if (reduced) {
                foot.textContent = msg;
                setTimeout(function () {
                    win.dataset.state = 'off';
                    win.classList.add('hsr--off');
                    setTimeout(finish, 320);
                }, 450);
            } else {
                // fixed schedule from the moment close() is called, so a busy page cannot stretch it:
                // the line types out over 0.35 s, the fold starts at 0.65 s, the DOM goes at 1.1 s
                var tc = now();
                var typer = setInterval(function () {
                    var n = Math.min(msg.length, Math.ceil((now() - tc) / 350 * msg.length));
                    foot.textContent = msg.slice(0, n) + (n < msg.length ? '_' : '');
                    if (n >= msg.length) clearInterval(typer);
                }, 24);
                setTimeout(function () {
                    clearInterval(typer);
                    foot.textContent = msg;
                    win.dataset.state = 'off';
                    win.classList.add('hsr--off');
                }, 650);
                setTimeout(finish, 1100);
            }
            return closePromise;
        }

        var handle = { el: win, shape: shape, closed: false, close: close };
        open.push(handle);
        if (opts.ttl > 0) ttlTimer = setTimeout(close, opts.ttl);
        return handle;
    }

    function closeAll() {
        return Promise.all(open.slice().map(function (h) { return h.close(); }));
    }

    return Object.freeze({
        show: show,
        closeAll: closeAll,
        active: function () { return open.length; },
        injectStyles: injectStyles,
        READOUTS: Object.freeze(READOUTS.map(function (r) { return Object.freeze(r.slice()); })),
        FLAGS: Object.freeze(FLAGS.slice())
    });
});
