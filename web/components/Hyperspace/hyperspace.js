/* Hyperspace — the standard library of higher-dimensional shapes.
 *
 * One catalog, several consumers:
 *   - the Hyperspace page (github.com/mindattic/Hyperspace): the walkable gallery (ten exhibits to a row)
 *     (Three.js) and the explorers (2D canvas) build every exhibit from this file;
 *   - hyperspace-reader.js in this folder: the Cyberspace "Hyperspace Reader" scanner window.
 *
 * Each shape is a frozen record (id, name, tag, family, dim, Schläfli symbol, plaque facts, rotation
 * planes, blurb, article, references) plus a geometry generator that returns { verts, edges } in the
 * shape's native dimension. Projection is pure: pose(id, t) rotates the native vertices through the
 * exhibit's planes, project3D() perspective-projects them to normalised 3D for a Three.js scene, and
 * project2D() goes one step further for a 2D canvas. No DOM, no RNG, no Date.
 *
 * The geometry, facts and articles are the Hyperspace page's own data, moved here unchanged.
 *
 * Classic browser <script> AND require()-able under Node (UMD), no bundler.
 */
(function (root, factory) {
    var api = factory();
    if (typeof window !== 'undefined') window.Hyperspace = api;
    if (typeof module !== 'undefined' && module.exports) module.exports = api;
    else if (root) root.Hyperspace = api;
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
    'use strict';

    // =====================================================================================
    // Geometry generators (from the Hyperspace page, unchanged). Each returns { verts, edges }:
    // verts are points in their native dimension, edges are index pairs.
    // =====================================================================================

    const PHI = (1 + Math.sqrt(5)) / 2;

    // ---- generic builders -------------------------------------------------------

    // n-cube: 2^n vertices at (±1)^n; edges join verts differing in one coordinate.
    function nCube(n) {
      const verts = [];
      for (let i = 0; i < (1 << n); i++)
        verts.push(Array.from({ length: n }, (_, k) => (i >> k & 1) ? 1 : -1));
      const edges = [];
      for (let i = 0; i < verts.length; i++)
        for (let d = 0; d < n; d++) { const j = i ^ (1 << d); if (j > i) edges.push([i, j]); }
      return { verts, edges };
    }

    // n-simplex: n+1 mutually equidistant vertices, centred and embedded in n-space.
    function nSimplex(n) {
      // Standard construction: vertices in R^{n+1} as basis vectors, then drop to n-D
      // by centering and using the fact they lie on a hyperplane. We keep them in
      // (n+1)-D coordinates (they differ in exactly the components that matter) and
      // let projection handle the reduction — simplest robust route.
      const raw = [];
      for (let i = 0; i <= n; i++) {
        const p = new Array(n + 1).fill(0); p[i] = 1; raw.push(p);
      }
      // center
      const c = new Array(n + 1).fill(0);
      raw.forEach(p => p.forEach((x, k) => c[k] += x / raw.length));
      const verts = raw.map(p => p.map((x, k) => (x - c[k]) * 1.6));
      const edges = [];
      for (let i = 0; i <= n; i++) for (let j = i + 1; j <= n; j++) edges.push([i, j]);
      return { verts, edges };
    }

    // n-orthoplex (cross-polytope): 2n vertices at ±e_k; every pair joined except antipodes.
    function nOrthoplex(n) {
      const verts = [];
      for (let i = 0; i < n; i++) for (const s of [1, -1]) {
        const p = new Array(n).fill(0); p[i] = s; verts.push(p);
      }
      const edges = [];
      for (let i = 0; i < 2 * n; i++) for (let j = i + 1; j < 2 * n; j++)
        if ((i >> 0) !== (j) && Math.floor(i / 2) !== Math.floor(j / 2)) edges.push([i, j]);
      return { verts, edges };
    }

    // n-demicube: alternated n-cube (half the vertices, those with even coordinate sum).
    function nDemicube(n) {
      const all = nCube(n).verts;
      const verts = all.filter(v => (v.filter(x => x > 0).length % 2) === 0);
      const edges = edgesByNearest(verts, 2); // nearest-neighbour distance^2 = 4? compute
      return { verts, edges: edgesByShortest(verts) };
    }

    // ---- regular 4-polytopes ----------------------------------------------------

    // 24-cell: all permutations of (±1,±1,0,0). 24 verts, edges at squared-distance 2.
    function cell24() {
      const verts = [];
      for (let a = 0; a < 4; a++) for (let b = a + 1; b < 4; b++)
        for (const sa of [1, -1]) for (const sb of [1, -1]) {
          const p = [0, 0, 0, 0]; p[a] = sa; p[b] = sb; verts.push(p);
        }
      return { verts, edges: edgesByShortest(verts) };
    }

    // 16-cell == 4-orthoplex; 8-cell == tesseract == 4-cube; 5-cell == 4-simplex.
    const cell16 = () => nOrthoplex(4);
    const tesseract = () => nCube(4);
    const cell5 = () => nSimplex(4);

    // 600-cell: 120 vertices = even permutations of (±φ,±1,±1/φ,0)/... plus 16-cell + 8 axes.
    // Standard unit-radius coordinates (Coxeter): all from the icosian set.
    function cell600() {
      const v = [];
      const push = p => v.push(p);
      // 8 vertices: (±1,0,0,0) and perms
      for (let i = 0; i < 4; i++) for (const s of [1, -1]) { const p = [0, 0, 0, 0]; p[i] = s; push(p); }
      // 16 vertices: (±1,±1,±1,±1)/2
      for (let s = 0; s < 16; s++) push([0, 1, 2, 3].map(k => ((s >> k & 1) ? 1 : -1) * 0.5));
      // 96 vertices: even permutations of (±φ,±1,±1/φ,0)/2
      const base = [PHI / 2, 0.5, 1 / (2 * PHI), 0];
      const evenPerms = evenPermutations4();
      for (const perm of evenPerms)
        for (const sgn of signMasks(base, perm)) push(sgn);
      // dedupe
      const verts = dedupe(v);
      return { verts, edges: edgesByShortest(verts) };
    }

    // 120-cell: dual of the 600-cell — 600 vertices, 1200 edges, from the H4 diagram ringed at its 5 end.
    const cell120 = () => wythoff(4, H4, ring(4, 0));

    // ---- helpers ----------------------------------------------------------------

    function evenPermutations4() {
      const perms = [];
      const idx = [0, 1, 2, 3];
      const all = permute(idx);
      for (const p of all) if (parity(p) === 0) perms.push(p);
      return perms;
    }
    function permute(arr) {
      if (arr.length <= 1) return [arr];
      const out = [];
      for (let i = 0; i < arr.length; i++) {
        const rest = arr.slice(0, i).concat(arr.slice(i + 1));
        for (const r of permute(rest)) out.push([arr[i], ...r]);
      }
      return out;
    }
    function parity(perm) {
      let p = 0; const a = perm.slice();
      for (let i = 0; i < a.length; i++)
        for (let j = i + 1; j < a.length; j++) if (a[i] > a[j]) p++;
      return p % 2;
    }
    function signMasks(base, perm) {
      // apply perm to base, then all sign combinations on nonzero entries
      const arranged = perm.map(k => base[k]);
      const nz = arranged.map((x, i) => Math.abs(x) > 1e-9 ? i : -1).filter(i => i >= 0);
      const out = [];
      for (let m = 0; m < (1 << nz.length); m++) {
        const p = arranged.slice();
        nz.forEach((idx, b) => { if (m >> b & 1) p[idx] = -p[idx]; });
        out.push(p);
      }
      return out;
    }
    function dedupe(verts) {
      const seen = new Set(), out = [];
      for (const v of verts) {
        const key = v.map(x => x.toFixed(4)).join(',');
        if (!seen.has(key)) { seen.add(key); out.push(v); }
      }
      return out;
    }
    function dist2(a, b) { let s = 0; for (let i = 0; i < a.length; i++) { const d = a[i] - b[i]; s += d * d; } return s; }

    // edges = all pairs at the (globally) shortest vertex-vertex distance.
    function edgesByShortest(verts, tol = 1e-3) {
      let min = Infinity;
      for (let i = 0; i < verts.length; i++) for (let j = i + 1; j < verts.length; j++) {
        const d = dist2(verts[i], verts[j]); if (d > 1e-9 && d < min) min = d;
      }
      const edges = [];
      for (let i = 0; i < verts.length; i++) for (let j = i + 1; j < verts.length; j++)
        if (Math.abs(dist2(verts[i], verts[j]) - min) < tol) edges.push([i, j]);
      return edges;
    }
    function edgesByNearest(verts, k) { return edgesByShortest(verts); }

    // duoprism {p}×{q}: product of a p-gon and a q-gon in 4-space.
    function duoprism(p, q) {
      const verts = [];
      for (let i = 0; i < p; i++) for (let j = 0; j < q; j++) {
        const a = 2 * Math.PI * i / p, b = 2 * Math.PI * j / q;
        verts.push([Math.cos(a), Math.sin(a), Math.cos(b), Math.sin(b)]);
      }
      const edges = [];
      const id = (i, j) => i * q + j;
      for (let i = 0; i < p; i++) for (let j = 0; j < q; j++) {
        edges.push([id(i, j), id((i + 1) % p, j)]);
        edges.push([id(i, j), id(i, (j + 1) % q)]);
      }
      return { verts, edges };
    }

    // ---- curved / parametric surfaces (returned as line nets) -------------------

    // glome (3-sphere) — a net of great circles in 4-space.
    function glome(rings = 8, seg = 40) {
      const verts = [], edges = [];
      let idx = 0;
      for (let r = 0; r < rings; r++) {
        const phi = Math.PI * (r + 0.5) / rings;
        const start = idx;
        for (let s = 0; s < seg; s++) {
          const th = 2 * Math.PI * s / seg;
          verts.push([Math.sin(phi) * Math.cos(th), Math.sin(phi) * Math.sin(th),
                      Math.cos(phi), Math.sin(phi) * Math.sin(th) * Math.cos(th)]);
          edges.push([start + s, start + (s + 1) % seg]); idx++;
        }
      }
      return { verts, edges };
    }

    // Clifford torus — flat torus on S^3: (cosa,sina,cosb,sinb)/√2.
    function cliffordTorus(u = 16, vv = 16) {
      const verts = [], edges = [];
      const id = (i, j) => i * vv + j;
      for (let i = 0; i < u; i++) for (let j = 0; j < vv; j++) {
        const a = 2 * Math.PI * i / u, b = 2 * Math.PI * j / vv;
        verts.push([Math.cos(a) / Math.SQRT2, Math.sin(a) / Math.SQRT2,
                    Math.cos(b) / Math.SQRT2, Math.sin(b) / Math.SQRT2]);
      }
      for (let i = 0; i < u; i++) for (let j = 0; j < vv; j++) {
        edges.push([id(i, j), id((i + 1) % u, j)]);
        edges.push([id(i, j), id(i, (j + 1) % vv)]);
      }
      return { verts, edges };
    }

    // Klein bottle — figure-8 immersion in 4-space.
    function kleinBottle(u = 24, vv = 14) {
      const verts = [], edges = [];
      const id = (i, j) => i * vv + j;
      for (let i = 0; i < u; i++) for (let j = 0; j < vv; j++) {
        const a = 2 * Math.PI * i / u, b = 2 * Math.PI * j / vv;
        const r = 2;
        const x = (r + Math.cos(a / 2) * Math.sin(b) - Math.sin(a / 2) * Math.sin(2 * b)) * Math.cos(a);
        const y = (r + Math.cos(a / 2) * Math.sin(b) - Math.sin(a / 2) * Math.sin(2 * b)) * Math.sin(a);
        const z = Math.sin(a / 2) * Math.sin(b) + Math.cos(a / 2) * Math.sin(2 * b);
        verts.push([x * 0.4, y * 0.4, z * 0.6, Math.cos(a) * 0.3]);
      }
      for (let i = 0; i < u; i++) for (let j = 0; j < vv; j++) {
        edges.push([id(i, j), id((i + 1) % u, j)]);
        edges.push([id(i, j), id(i, (j + 1) % vv)]);
      }
      return { verts, edges };
    }

    // Boy's surface — Bryant–Kusner parametrization, projected.
    function boysSurface(u = 22, vv = 22) {
      const verts = [], edges = [];
      const id = (i, j) => i * vv + j;
      for (let i = 0; i < u; i++) for (let j = 0; j < vv; j++) {
        const z = (i / (u - 1)) * Math.PI; // not exact BK but a smooth RP^2 immersion
        const th = 2 * Math.PI * j / vv;
        const x = Math.sin(z) * Math.cos(th);
        const y = Math.sin(z) * Math.sin(th);
        const w = Math.cos(z);
        verts.push([x, y, Math.cos(2 * z) * 0.5, w]);
      }
      for (let i = 0; i < u; i++) for (let j = 0; j < vv; j++) {
        if (i + 1 < u) edges.push([id(i, j), id(i + 1, j)]);
        edges.push([id(i, j), id(i, (j + 1) % vv)]);
      }
      return { verts, edges };
    }

    // duocylinder ridge — torus where two circles meet: (cosa,sina,cosb,sinb).
    function duocylinder(u = 20, vv = 20) { return cliffordTorus(u, vv); }

    // Hopf fibration — a set of Hopf circles on S^3 coloured by base point.
    function hopfFibration(fibers = 16, seg = 36) {
      const verts = [], edges = [];
      let idx = 0;
      for (let f = 0; f < fibers; f++) {
        const eta = Math.PI * (f + 0.5) / (2 * fibers); // base latitude
        const xi1 = 2 * Math.PI * f / fibers;
        const start = idx;
        for (let s = 0; s < seg; s++) {
          const t = 2 * Math.PI * s / seg;
          verts.push([Math.cos(eta) * Math.cos(xi1 + t), Math.cos(eta) * Math.sin(xi1 + t),
                      Math.sin(eta) * Math.cos(t), Math.sin(eta) * Math.sin(t)]);
          edges.push([start + s, start + (s + 1) % seg]); idx++;
        }
      }
      return { verts, edges };
    }

    // Villarceau circles on a torus embedded in 4-space.
    function villarceau(circles = 14, seg = 40) {
      const verts = [], edges = [];
      let idx = 0;
      const R = 2, r = 1;
      for (let c = 0; c < circles; c++) {
        const off = 2 * Math.PI * c / circles;
        const start = idx;
        for (let s = 0; s < seg; s++) {
          const t = 2 * Math.PI * s / seg;
          // a Villarceau circle is a slanted slice of the torus
          const x = (R + r * Math.cos(t)) * Math.cos(off);
          const y = (R + r * Math.cos(t)) * Math.sin(off);
          const z = r * Math.sin(t);
          verts.push([x * 0.4, y * 0.4, z * 0.4, Math.sin(t + off) * 0.4]);
          edges.push([start + s, start + (s + 1) % seg]); idx++;
        }
      }
      return { verts, edges };
    }

    // RP^3 as antipodal great-circle net on S^3 (identify antipodes visually).
    function rp3(rings = 7, seg = 34) { return glome(rings, seg); }

    // ---- rotation demos (geometry is a tesseract; engine animates the plane) ----
    const rotTesseract = () => nCube(4);

    // ---- lattices / physics (representative point-and-edge frames) --------------

    // E8 root system projected: 240 roots. Two families: ±e_i±e_j (112) and
    // half-integer (±1/2)^8 with even # of minus signs (128).
    function e8Roots() {
      const verts = [];
      for (let i = 0; i < 8; i++) for (let j = i + 1; j < 8; j++)
        for (const si of [1, -1]) for (const sj of [1, -1]) {
          const p = new Array(8).fill(0); p[i] = si; p[j] = sj; verts.push(p);
        }
      for (let m = 0; m < 256; m++) {
        let minus = 0; for (let k = 0; k < 8; k++) if (m >> k & 1) minus++;
        if (minus % 2 === 0) verts.push(Array.from({ length: 8 }, (_, k) => (m >> k & 1) ? -0.5 : 0.5));
      }
      return { verts, edges: edgesByShortest(verts).slice(0, 1200) };
    }

    // 4_21 (E8 polytope) — same 240 roots, the Gosset polytope vertices.
    const e8Polytope = e8Roots;

    // Minkowski light cone — null cone t^2 = x^2+y^2 (+z visual), in 4-space (t,x,y,z).
    function lightCone(rings = 9, seg = 30) {
      const verts = [], edges = [];
      let idx = 0;
      for (let r = 0; r < rings; r++) {
        const t = (r / (rings - 1)) * 2 - 1;
        const rad = Math.abs(t);
        const start = idx;
        for (let s = 0; s < seg; s++) {
          const th = 2 * Math.PI * s / seg;
          verts.push([rad * Math.cos(th), rad * Math.sin(th), t, rad * Math.sin(th) * 0.5]);
          edges.push([start + s, start + (s + 1) % seg]); idx++;
        }
        if (r + 1 < rings) for (let s = 0; s < seg; s += 5) edges.push([start + s, start + seg + s]);
      }
      return { verts, edges };
    }

    // de Sitter — single-sheet hyperboloid -t^2+x^2+y^2+z^2 = 1.
    function deSitter(rings = 9, seg = 30) {
      const verts = [], edges = [];
      let idx = 0;
      for (let r = 0; r < rings; r++) {
        const u = (r / (rings - 1)) * 3 - 1.5;
        const rad = Math.cosh(u);
        const start = idx;
        for (let s = 0; s < seg; s++) {
          const th = 2 * Math.PI * s / seg;
          verts.push([rad * Math.cos(th) * 0.4, rad * Math.sin(th) * 0.4, Math.sinh(u) * 0.4,
                      rad * Math.cos(th) * 0.2]);
          edges.push([start + s, start + (s + 1) % seg]); idx++;
        }
        if (r + 1 < rings) for (let s = 0; s < seg; s += 4) edges.push([start + s, start + seg + s]);
      }
      return { verts, edges };
    }

    // anti-de Sitter — two-sheet / saddle hyperboloid representation.
    function antiDeSitter(rings = 9, seg = 30) {
      const verts = [], edges = [];
      let idx = 0;
      for (let r = 0; r < rings; r++) {
        const u = (r / (rings - 1)) * 3 - 1.5;
        const rad = Math.sinh(Math.abs(u)) + 0.2;
        const start = idx;
        for (let s = 0; s < seg; s++) {
          const th = 2 * Math.PI * s / seg;
          verts.push([rad * Math.cos(th) * 0.4, rad * Math.sin(th) * 0.4, u * 0.5,
                      Math.cosh(u) * 0.2 * Math.sin(th)]);
          edges.push([start + s, start + (s + 1) % seg]); idx++;
        }
        if (r + 1 < rings) for (let s = 0; s < seg; s += 4) edges.push([start + s, start + seg + s]);
      }
      return { verts, edges };
    }

    // Calabi-Yau — cross-section of the Fermat quintic (standard z1^n+z2^n=1 net).
    function calabiYau(n = 5, patches = 8, seg = 12) {
      const verts = [], edges = [];
      let idx = 0;
      for (let k1 = 0; k1 < n; k1++) {
        const start = idx;
        const row = [];
        for (let a = 0; a <= seg; a++) {
          const x = (a / seg) * 2 - 1;
          const theta = (2 * Math.PI * k1 / n);
          // z1 = e^{i k1 2π/n} * coshlike; project real/imag into 4 coords
          const z1r = Math.cos(theta) * Math.cos(x), z1i = Math.sin(theta) * Math.cosh(x) * 0.5;
          const z2r = Math.cos(theta + 1) * Math.sin(x), z2i = Math.sin(theta + 1) * Math.sinh(x) * 0.5;
          verts.push([z1r, z1i, z2r, z2i]); row.push(idx); idx++;
        }
        for (let a = 0; a < row.length - 1; a++) edges.push([row[a], row[a + 1]]);
      }
      return { verts, edges };
    }

    // Penrose / quasicrystal — 5D hypercube lattice points whose 2D shadow tiles aperiodically.
    function penrose(n = 3) {
      // points of a small 5-cube grid; projection to 3D gives quasicrystal shadow
      const verts = [], edges = [];
      const range = [-1, 0, 1];
      const pts = [];
      for (const a of range) for (const b of range) for (const c of range)
        for (const d of range) for (const e of range)
          if (Math.abs(a) + Math.abs(b) + Math.abs(c) + Math.abs(d) + Math.abs(e) <= 2)
            pts.push([a, b, c, d, e]);
      pts.forEach(p => verts.push(p));
      return { verts, edges: edgesByShortest(verts).slice(0, 400) };
    }

    // Leech lattice — minimal-vector frame (representative 24D shadow, capped).
    function leech() {
      // A small representative set: 48 minimal vectors of the form (±4,±4,0^22)/√8 scaled.
      const verts = [];
      for (let i = 0; i < 8; i++) for (let j = i + 1; j < 8; j++)
        for (const si of [1, -1]) for (const sj of [1, -1]) {
          const p = new Array(8).fill(0); p[i] = si; p[j] = sj; verts.push(p);
        }
      return { verts, edges: edgesByShortest(verts).slice(0, 600) };
    }

    // Octonion / Fano-plane multiplication structure as a 7-point, 7-line frame in 8-space.
    function octonions() {
      // Represent e1..e7 as orthonormal axes in R^7 plus the 7 Fano lines as triangles.
      const verts = [];
      for (let i = 0; i < 7; i++) { const p = new Array(7).fill(0); p[i] = 1; verts.push(p); }
      const fano = [[0,1,3],[1,2,4],[2,3,5],[3,4,6],[4,5,0],[5,6,1],[6,0,2]];
      const edges = [];
      for (const [a,b,c] of fano) { edges.push([a,b],[b,c],[c,a]); }
      return { verts, edges };
    }

    // ---- additional 5D builders -------------------------------------------------

    // 5D prism: extrude any 4D polytope along a fifth axis.
    function prism5D(gen4D) {
      const base = gen4D();
      const n = base.verts.length;
      const verts = [];
      for (const v of base.verts) verts.push([...v, -1]);
      for (const v of base.verts) verts.push([...v,  1]);
      const edges = [];
      for (let i = 0; i < n; i++) edges.push([i, i + n]);
      for (const [a, b] of base.edges) {
        edges.push([a, b]);
        edges.push([a + n, b + n]);
      }
      return { verts, edges };
    }

    // Product of a 3D polyhedron and a p-gon, embedded in 5D.
    function polyDuoprism(poly, p) {
      const verts = [], edges = [];
      const id = (i, j) => i * p + j;
      const nv = poly.verts.length;
      for (const v of poly.verts)
        for (let i = 0; i < p; i++) {
          const a = 2 * Math.PI * i / p;
          verts.push([...v, Math.cos(a), Math.sin(a)]);
        }
      for (const [a, b] of poly.edges)
        for (let j = 0; j < p; j++) edges.push([id(a, j), id(b, j)]);
      for (let i = 0; i < nv; i++)
        for (let j = 0; j < p; j++) edges.push([id(i, j), id(i, (j + 1) % p)]);
      return { verts, edges };
    }

    // 4-sphere S^4 in 5-space, drawn as a net of latitude circles.
    function glome5D(rings = 6, seg = 24) {
      const verts = [], edges = [];
      let idx = 0;
      for (let r = 0; r < rings; r++) {
        const phi = Math.PI * (r + 0.5) / rings;
        const c = Math.cos(phi), s = Math.sin(phi);
        const start = idx;
        for (let i = 0; i < seg; i++) {
          const th = 2 * Math.PI * i / seg;
          verts.push([c * Math.cos(th), c * Math.sin(th), c * Math.cos(2 * th) * 0.5,
                      c * Math.sin(2 * th) * 0.5, s]);
          edges.push([start + i, start + (i + 1) % seg]); idx++;
        }
      }
      return { verts, edges };
    }

    // 5D cyclic polytope on the moment curve (t, t^2, t^3, t^4, t^5).
    // C(n,5) is 2-neighborly: every pair of vertices forms an edge.
    function cyclic5D(n = 14) {
      const verts = [];
      for (let i = 0; i < n; i++) {
        const t = (i / (n - 1)) * 2 - 1;
        verts.push([t, t * t, t * t * t, t * t * t * t, t * t * t * t * t]);
      }
      const edges = [];
      for (let i = 0; i < n; i++) for (let j = i + 1; j < n; j++) edges.push([i, j]);
      return { verts, edges };
    }

    // ---- additional builders for the gallery hall --------------------------------

    // 4D prism over a 3D polyhedron.
    function prism4D(poly) {
      const verts = [], edges = [];
      const n = poly.verts.length;
      for (const v of poly.verts) verts.push([...v, -1]);
      for (const v of poly.verts) verts.push([...v,  1]);
      for (let i = 0; i < n; i++) edges.push([i, i + n]);
      for (const [a, b] of poly.edges) {
        edges.push([a, b]);
        edges.push([a + n, b + n]);
      }
      return { verts, edges };
    }

    // Regular 3D polyhedra (for 4D prisms and 5D duoprisms).
    function tetrahedron3D() {
      return { verts:[[1,1,1],[1,-1,-1],[-1,1,-1],[-1,-1,1]],
               edges:[[0,1],[0,2],[0,3],[1,2],[1,3],[2,3]] };
    }
    function cube3D() {
      const verts = [];
      for (let i = 0; i < 8; i++) verts.push([(i & 1) ? 1 : -1, (i & 2) ? 1 : -1, (i & 4) ? 1 : -1]);
      const edges = [];
      for (let i = 0; i < 8; i++) for (let d = 0; d < 3; d++) { const j = i ^ (1 << d); if (j > i) edges.push([i, j]); }
      return { verts, edges };
    }
    function octahedron3D() {
      const verts = [[1,0,0],[-1,0,0],[0,1,0],[0,-1,0],[0,0,1],[0,0,-1]];
      const edges = [];
      for (let i = 0; i < 6; i++) for (let j = i + 1; j < 6; j++) if (Math.floor(i / 2) !== Math.floor(j / 2)) edges.push([i, j]);
      return { verts, edges };
    }
    function icosahedron3D() {
      const verts = [];
      for (const s of [1, -1]) for (const t of [1, -1])
        verts.push([0, s, t * PHI], [t * PHI, 0, s], [s, t * PHI, 0]);
      return { verts, edges: edgesByShortest(verts) };
    }
    function dodecahedron3D() {
      const inv = 1 / PHI;
      const verts = [
        [1, 1, 1], [1, 1, -1], [1, -1, 1], [1, -1, -1],
        [-1, 1, 1], [-1, 1, -1], [-1, -1, 1], [-1, -1, -1],
        [0, inv, PHI], [0, inv, -PHI], [0, -inv, PHI], [0, -inv, -PHI],
        [PHI, 0, inv], [PHI, 0, -inv], [-PHI, 0, inv], [-PHI, 0, -inv],
        [inv, PHI, 0], [inv, -PHI, 0], [-inv, PHI, 0], [-inv, -PHI, 0]
      ];
      return { verts, edges: edgesByShortest(verts) };
    }

    // D4 lattice minimal vectors (the 24-cell vertices).
    function d4Lattice() { return cell24(); }

    // D5 lattice minimal vectors: permutations of (±1,±1,0,0,0).
    function d5Lattice() {
      const verts = [];
      for (let a = 0; a < 5; a++) for (let b = a + 1; b < 5; b++)
        for (const sa of [1, -1]) for (const sb of [1, -1]) {
          const p = [0, 0, 0, 0, 0]; p[a] = sa; p[b] = sb; verts.push(p);
        }
      return { verts, edges: edgesByShortest(verts) };
    }

    // A_n root system: e_i - e_j in R^{n+1}.
    function aRoots(n) {
      const verts = [];
      for (let i = 0; i <= n; i++) for (let j = 0; j <= n; j++) if (i !== j) {
        const p = new Array(n + 1).fill(0); p[i] = 1; p[j] = -1; verts.push(p);
      }
      return { verts, edges: edgesByShortest(verts) };
    }

    // Pseudosphere — tractrix surface of constant negative curvature.
    function pseudosphere(u = 24, vv = 24) {
      const verts = [], edges = [];
      const id = (i, j) => i * vv + j;
      for (let i = 0; i < u; i++) for (let j = 0; j < vv; j++) {
        const t = 0.05 + Math.PI * i / (u - 1) * 0.95;
        const th = 2 * Math.PI * j / vv;
        const x = Math.cos(th) / Math.sinh(t);
        const y = Math.sin(th) / Math.sinh(t);
        const z = t - Math.tanh(t);
        verts.push([x, y, z, Math.cos(th) * 0.3]);
      }
      for (let i = 0; i < u; i++) for (let j = 0; j < vv; j++) {
        if (i + 1 < u) edges.push([id(i, j), id(i + 1, j)]);
        edges.push([id(i, j), id(i, (j + 1) % vv)]);
      }
      return { verts, edges };
    }

    // Dini's surface — a surface of constant negative curvature.
    function diniSurface(u = 22, vv = 22) {
      const verts = [], edges = [];
      const id = (i, j) => i * vv + j;
      for (let i = 0; i < u; i++) for (let j = 0; j < vv; j++) {
        const a = 0.2 + 4 * Math.PI * i / (u - 1);
        const b = 2 * Math.PI * j / vv;
        const r = 0.4;
        const x = r * Math.cos(b) * Math.sin(a);
        const y = r * Math.sin(b) * Math.sin(a);
        const z = r * (Math.cos(a) + Math.log(Math.tan(a / 2))) + 0.3 * b;
        verts.push([x, y, z, Math.cos(a + b) * 0.3]);
      }
      for (let i = 0; i < u; i++) for (let j = 0; j < vv; j++) {
        if (i + 1 < u) edges.push([id(i, j), id(i + 1, j)]);
        edges.push([id(i, j), id(i, (j + 1) % vv)]);
      }
      return { verts, edges };
    }

    // Kuen surface — another constant-negative-curvature surface:
    // (2 cosh v (cos u + u sin u), 2 cosh v (sin u − u cos u), v (cosh² v + u²) − sinh 2v) / (cosh² v + u²).
    function kuenSurface(u = 22, vv = 22) {
      const verts = [], edges = [];
      const id = (i, j) => i * vv + j;
      for (let i = 0; i < u; i++) for (let j = 0; j < vv; j++) {
        const a = -4.5 + 9 * i / (u - 1), b = -3 + 6 * j / (vv - 1);
        const ch = Math.cosh(b), d = ch * ch + a * a;
        const x = 2 * ch * (Math.cos(a) + a * Math.sin(a)) / d;
        const y = 2 * ch * (Math.sin(a) - a * Math.cos(a)) / d;
        const z = b - Math.sinh(2 * b) / d;
        verts.push([x, y, z, Math.sin(a + b) * 0.3]);
      }
      for (let i = 0; i < u; i++) for (let j = 0; j < vv; j++) {
        if (i + 1 < u) edges.push([id(i, j), id(i + 1, j)]);
        if (j + 1 < vv) edges.push([id(i, j), id(i, j + 1)]);
      }
      return { verts, edges };
    }

    // Ammann–Beenker quasicrystal — 8D lattice points projected to 2D/3D.
    function ammannBeenker(n = 2) {
      const verts = [], edges = [];
      const range = [-1, 0, 1];
      for (const a of range) for (const b of range) for (const c of range) for (const d of range)
        for (const e of range) for (const f of range) for (const g of range) for (const h of range)
          if (Math.abs(a) + Math.abs(b) + Math.abs(c) + Math.abs(d) + Math.abs(e) + Math.abs(f) + Math.abs(g) + Math.abs(h) <= 2)
            verts.push([a, b, c, d, e, f, g, h]);
      return { verts, edges: edgesByShortest(verts).slice(0, 600) };
    }

    // ---- Wythoff construction -----------------------------------------------------

    // Simple roots of a Coxeter group from its diagram: links are [i, j, m] for the branches of the
    // diagram (m = 3 unless marked), every other pair commutes. The Gram matrix -cos(pi/m) is positive
    // definite for a finite group; its Cholesky factor gives unit roots, root i using coordinates 0..i.
    function coxeterRoots(n, links) {
      const G = Array.from({ length: n }, (_, i) => Array.from({ length: n }, (_, j) => i === j ? 1 : 0));
      for (const [i, j, m] of links) G[i][j] = G[j][i] = -Math.cos(Math.PI / m);
      const L = Array.from({ length: n }, () => new Array(n).fill(0));
      for (let i = 0; i < n; i++) for (let j = 0; j <= i; j++) {
        let s = G[i][j];
        for (let k = 0; k < j; k++) s -= L[i][k] * L[j][k];
        L[i][j] = i === j ? Math.sqrt(s) : s / L[j][j];
      }
      return L;
    }
    function reflect(v, r) {
      let d = 0; for (let k = 0; k < v.length; k++) d += v[k] * r[k];
      return v.map((x, k) => x - 2 * d * r[k]);
    }
    const vkey = v => v.map(x => (Math.abs(x) < 5e-7 ? 0 : x).toFixed(6)).join(',');
    function orbit(seeds, roots) {
      const seen = new Set(), out = [];
      const queue = seeds.filter(v => { const k = vkey(v); if (seen.has(k)) return false; seen.add(k); return true; });
      for (let q = 0; q < queue.length; q++) {
        out.push(queue[q]);
        for (const r of roots) { const w = reflect(queue[q], r), k = vkey(w); if (!seen.has(k)) { seen.add(k); queue.push(w); } }
      }
      return out;
    }
    // Uniform polytope from a ringed diagram: the seed sits at distance 1 from every ringed mirror and on
    // every unringed one, so all edges have length 2. Scaled to circumradius `radius`.
    function wythoffVerts(n, links, rings, radius = 1) {
      const L = coxeterRoots(n, links), p = new Array(n).fill(0);
      for (let i = 0; i < n; i++) {
        let s = rings[i] ? 1 : 0;
        for (let k = 0; k < i; k++) s -= L[i][k] * p[k];
        p[i] = s / L[i][i];
      }
      return scaleTo(orbit([p], L), radius);
    }
    function wythoff(n, links, rings, radius = 1) {
      const verts = wythoffVerts(n, links, rings, radius);
      return { verts, edges: edgesByShortest(verts) };
    }
    function scaleTo(verts, radius) {
      let m = 0; for (const v of verts) m = Math.max(m, Math.hypot(...v));
      return m > 0 ? verts.map(v => v.map(x => x * radius / m)) : verts;
    }
    // Every root of a crystallographic system: the orbit of its simple roots.
    const rootSystem = (n, links) => orbit(coxeterRoots(n, links), coxeterRoots(n, links));

    const A4 = [[0, 1, 3], [1, 2, 3], [2, 3, 3]];
    const B4 = [[0, 1, 4], [1, 2, 3], [2, 3, 3]];
    const F4 = [[0, 1, 3], [1, 2, 4], [2, 3, 3]];
    const H4 = [[0, 1, 5], [1, 2, 3], [2, 3, 3]];
    const A5 = [[0, 1, 3], [1, 2, 3], [2, 3, 3], [3, 4, 3]];
    const B5 = [[0, 1, 4], [1, 2, 3], [2, 3, 3], [3, 4, 3]];
    // E6, E7, E8: a chain with one branch on its third node. Arms from the branch node: E6 2,2,1;
    // E7 2,3,1; E8 2,4,1. k_ij rings the end of the arm of length k.
    const chain = n => Array.from({ length: n - 1 }, (_, i) => [i, i + 1, 3]);
    const E6 = [...chain(5), [2, 5, 3]];
    const E7 = [...chain(6), [2, 6, 3]];
    const E8 = [...chain(7), [2, 7, 3]];
    const ring = (n, ...on) => Array.from({ length: n }, (_, i) => on.includes(i) ? 1 : 0);

    // Keep at most `max` edges, sampled evenly through the list, for frames too dense to draw whole.
    function capEdges(edges, max) {
      if (edges.length <= max) return edges;
      const out = [], step = edges.length / max;
      for (let i = 0; i < max; i++) out.push(edges[Math.floor(i * step)]);
      return out;
    }
    // The whole shells of vertices nearest one pole (verts[0]), up to `max` vertices: a symmetric patch of
    // a vertex set too large to animate whole.
    function polarCap(verts, max) {
      const pole = verts[0];
      const dot = v => v.reduce((s, x, k) => s + x * pole[k], 0);
      const sorted = verts.slice().sort((a, b) => dot(b) - dot(a));
      let n = 0;
      while (n < sorted.length) {
        let m = n; const d = dot(sorted[n]);
        while (m < sorted.length && Math.abs(dot(sorted[m]) - d) < 1e-6) m++;
        if (m > max && n > 0) break;
        n = m;
      }
      return sorted.slice(0, n);
    }

    // ---- uniform 4-polytopes beyond the Wythoff families ---------------------------

    // Grand antiprism: the 600-cell with two completely orthogonal great decagons removed (100 vertices,
    // 500 edges, all of them 600-cell edges).
    function grandAntiprism() {
      const c = cell600(), V = c.verts;
      const e = c.edges[0], a = V[e[0]], b = V[e[1]];
      // orthonormal basis of the decagon's plane, then of the completely orthogonal plane
      const unit = v => { const m = Math.hypot(...v); return v.map(x => x / m); };
      const dotp = (x, y) => x.reduce((s, xi, k) => s + xi * y[k], 0);
      const u1 = unit(a), u2 = unit(b.map((x, k) => x - dotp(b, u1) * u1[k]));
      const inPlane = (v, p, q) => Math.abs(dotp(v, v) - dotp(v, p) ** 2 - dotp(v, q) ** 2) < 1e-6;
      const others = [];
      for (let k = 0; k < 4; k++) {
        let w = new Array(4).fill(0); w[k] = 1;
        for (const u of [u1, u2, ...others]) { const d = dotp(w, u); w = w.map((x, i) => x - d * u[i]); }
        if (Math.hypot(...w) > 1e-6 && others.length < 2) others.push(unit(w));
      }
      const verts = V.filter(v => !inPlane(v, u1, u2) && !inPlane(v, others[0], others[1]));
      return { verts, edges: edgesByShortest(verts) };
    }

    // Snub 24-cell: the 600-cell with the 24 vertices of an inscribed 24-cell removed, the 96 even
    // permutations of (±φ, ±1, ±1/φ, 0)/2. Its 432 edges are the 600-cell edges that remain.
    function snub24() {
      const verts = cell600().verts.filter(v => v.filter(x => Math.abs(x) > 1e-9).length === 3);
      return { verts, edges: edgesByShortest(verts) };
    }

    // Uniform n-antiprism with edge 2: two n-gons a half-step apart.
    function antiprism3D(n) {
      const r = 1 / Math.sin(Math.PI / n), h = r * Math.sqrt(Math.sin(Math.PI / n) ** 2 - Math.sin(Math.PI / (2 * n)) ** 2);
      const verts = [];
      for (let i = 0; i < n; i++) {
        const a = 2 * Math.PI * i / n, b = a + Math.PI / n;
        verts.push([r * Math.cos(a), r * Math.sin(a), h], [r * Math.cos(b), r * Math.sin(b), -h]);
      }
      return { verts, edges: edgesByShortest(verts) };
    }

    // ---- regular star polychora ---------------------------------------------------

    // A Schläfli-Hess polychoron {p,q,r} shares its vertices with the 600-cell (120) or the 120-cell (600);
    // its edges are the one chord class whose neighbours form the vertex figure {q,r}. Within a face {p} the
    // two neighbours of a vertex lie L·f apart (f = 1 for {3}, φ for {5}, 1/φ for {5/2}), and in the vertex
    // figure each of them has `deg` such partners. The figure's edges are the shortest distance among the
    // neighbours ({3,5}, {5,5/2}, {5,3}, {3,3}) or a longer one ({5/2,5}, {3,5/2}, {5/2,3}): `figLong`.
    const FACE_CHORD = { '3': 1, '5': PHI, '5/2': 1 / PHI };
    function starPolychoron(p, figVerts, deg, base, figLong) {
      const V = base === 600 ? wythoffVerts(4, H4, ring(4, 0)) : cell600().verts;
      const v0 = V[0], chords = {};
      for (let i = 1; i < V.length; i++) { const k = dist2(v0, V[i]).toFixed(5); (chords[k] = chords[k] || []).push(V[i]); }
      for (const k of Object.keys(chords).sort((a, b) => a - b)) {
        const N = chords[k];
        if (N.length !== figVerts) continue;
        const d2 = Number(k) * FACE_CHORD[p] ** 2;
        let shortest = Infinity;
        for (const a of N) for (const b of N) if (a !== b) shortest = Math.min(shortest, dist2(a, b));
        if ((Math.abs(d2 - shortest) > 1e-4) !== !!figLong) continue;
        if (N.every(a => N.filter(b => b !== a && Math.abs(dist2(a, b) - d2) < 1e-4).length === deg)) {
          const L2 = Number(k), edges = [];
          for (let i = 0; i < V.length; i++) for (let j = i + 1; j < V.length; j++)
            if (Math.abs(dist2(V[i], V[j]) - L2) < 1e-4) edges.push([i, j]);
          return { verts: V, edges };
        }
      }
      throw new Error('Hyperspace: no chord class fits {' + p + ',…}');
    }

    // ---- lattices -------------------------------------------------------------------

    // Kissing configuration: one sphere at the origin and the minimal vectors of the lattice it touches.
    // Spokes and the 60° neighbours between touching spheres have the same length, so both are edges.
    function kissing(shell, maxEdges) {
      const verts = scaleTo([new Array(shell[0].length).fill(0), ...shell], 1);
      return { verts, edges: capEdges(edgesByShortest(verts), maxEdges) };
    }
    // Barnes-Wall lattice BW16: x ∈ Z^16 with x mod 2 in the Reed-Muller code RM(1,4) and Σx ≡ 0 (mod 4).
    // Minimal vectors (norm 8): (±2,±2,0^14), 480 of them, and ±1 on a weight-8 codeword with an even
    // number of minus signs, 3840 of them.
    function barnesWallMinimal() {
      const out = [];
      for (let i = 0; i < 16; i++) for (let j = i + 1; j < 16; j++) for (const si of [2, -2]) for (const sj of [2, -2]) {
        const v = new Array(16).fill(0); v[i] = si; v[j] = sj; out.push(v);
      }
      for (let a = 1; a < 16; a++) for (const b of [0, 1]) {
        const support = [];
        for (let x = 0; x < 16; x++) { let f = b; for (let k = 0; k < 4; k++) f ^= (a >> k & 1) & (x >> k & 1); if (f) support.push(x); }
        for (let m = 0; m < 256; m++) {
          let minus = 0; for (let k = 0; k < 8; k++) minus += m >> k & 1;
          if (minus % 2) continue;
          const v = new Array(16).fill(0); support.forEach((x, k) => { v[x] = (m >> k & 1) ? -1 : 1; }); out.push(v);
        }
      }
      return out;
    }
    // Coxeter-Todd lattice K12 as Eisenstein 6-vectors x with all x_i congruent mod θ = √-3 and Σx ≡ 0 (mod 3).
    // Minimal vectors (norm 6): θ(u e_i + v e_j) with v ≡ -u (mod θ), 270 of them, and ±(ω^a1, ..., ω^a6)
    // with Σ ω^ai ≡ 0 (mod 3), 486 of them. Each Eisenstein coordinate a + bω is drawn as (a - b/2, b√3/2).
    function coxeterToddMinimal() {
      const w = [[1, 0], [-0.5, Math.sqrt(3) / 2], [-0.5, -Math.sqrt(3) / 2]];          // 1, ω, ω²
      const mul = (p, q) => [p[0] * q[0] - p[1] * q[1], p[0] * q[1] + p[1] * q[0]];
      const theta = [0, Math.sqrt(3)];
      const out = [], vec = cs => out.push(cs.flat());
      for (let i = 0; i < 6; i++) for (let j = i + 1; j < 6; j++)
        for (const s of [1, -1]) for (let a = 0; a < 3; a++) for (let b = 0; b < 3; b++) {
          const v = Array.from({ length: 6 }, () => [0, 0]);
          v[i] = mul(theta, w[a].map(x => s * x)); v[j] = mul(theta, w[b].map(x => -s * x));
          vec(v);
        }
      for (let m = 0; m < 729; m++) {
        const e = [0, 1, 2, 3, 4, 5].map(k => Math.floor(m / 3 ** k) % 3);
        const n = [0, 1, 2].map(r => e.filter(x => x === r).length);
        if ((n[0] - n[1]) % 3 || (n[1] - n[2]) % 3) continue;
        for (const s of [1, -1]) vec(e.map(a => w[a].map(x => s * x)));
      }
      return out;
    }

    // ---- quasicrystals ----------------------------------------------------------------

    // Points are stored as [parallel..., perpendicular...]: the perspective projection drops the
    // perpendicular (internal-space) coordinates first, leaving the quasicrystal in physical space.

    // Elser-Sloane: the icosian ring (E8) cut down to 4D. A unit icosian has coordinates (a + bφ)/2; the
    // Galois conjugate φ -> 1 - φ gives its internal-space image. Points are reached from the origin by
    // unit-icosian steps, kept while |x| ≤ 2 and the internal image lies in a ball of radius 1; edges are
    // the unit-icosian steps between kept points.
    function elserSloane(rPar = 2, rPerp = 1) {
      const U = [];
      for (let i = 0; i < 4; i++) for (const s of [1, -1]) { const p = [[0, 0], [0, 0], [0, 0], [0, 0]]; p[i] = [2 * s, 0]; U.push(p); }
      for (let m = 0; m < 16; m++) U.push([0, 1, 2, 3].map(k => [(m >> k & 1) ? -1 : 1, 0]));
      const base = [[0, 1], [1, 0], [-1, 1], [0, 0]];                    // φ, 1, 1/φ, 0 (each over 2)
      for (const perm of evenPermutations4()) for (let m = 0; m < 8; m++) {
        let bit = 0;
        U.push(perm.map(k => { const c = base[k]; if (k === 3) return [0, 0]; const s = (m >> bit++ & 1) ? -1 : 1; return [s * c[0], s * c[1]]; }));
      }
      const par = x => x.map(([a, b]) => (a + b * PHI) / 2), perp = x => x.map(([a, b]) => (a + b * (1 - PHI)) / 2);
      const key = x => x.map(c => c.join(':')).join(',');
      const seen = new Map(), pts = [[[0, 0], [0, 0], [0, 0], [0, 0]]]; seen.set(key(pts[0]), 0);
      for (let q = 0; q < pts.length; q++) for (const u of U) {
        const y = pts[q].map((c, k) => [c[0] + u[k][0], c[1] + u[k][1]]), k = key(y);
        if (seen.has(k) || Math.hypot(...par(y)) > rPar + 1e-9 || Math.hypot(...perp(y)) > rPerp + 1e-9) continue;
        seen.set(k, pts.length); pts.push(y);
      }
      const edges = [];
      pts.forEach((x, i) => { for (const u of U) { const j = seen.get(key(x.map((c, k) => [c[0] + u[k][0], c[1] + u[k][1]]))); if (j > i) edges.push([i, j]); } });
      return { verts: scaleTo(pts.map(x => [...par(x), ...perp(x).map(c => c * 0.5)]), 1.2), edges };
    }

    // Icosahedral quasicrystal: Z^6 cut down to 3D. The six basis vectors go to the six icosahedral axes
    // in physical space and to the Galois-conjugate axes in internal space; a lattice point is kept when
    // its internal image falls in the window (the rhombic triacontahedron the unit 6-cube projects to) and
    // |x| ≤ rPar. Edges are the lattice's unit steps, which project to the six icosahedral directions.
    function icosahedralQuasicrystal(rPar = 2.6) {
      const ax = (a, b) => { const v = [[0, a, b], [0, -a, b], [a, b, 0], [-a, b, 0], [b, 0, a], [b, 0, -a]]; return v.map(x => { const m = Math.hypot(...x); return x.map(c => c / m); }); };
      const gPar = ax(1, PHI), gPerp = ax(1, -1 / PHI);
      const cross = (u, v) => [u[1] * v[2] - u[2] * v[1], u[2] * v[0] - u[0] * v[2], u[0] * v[1] - u[1] * v[0]];
      const dot3 = (u, v) => u[0] * v[0] + u[1] * v[1] + u[2] * v[2];
      const faces = [];
      for (let i = 0; i < 6; i++) for (let j = i + 1; j < 6; j++) {
        const nrm = cross(gPerp[i], gPerp[j]);
        faces.push([nrm, gPerp.reduce((s, g) => s + Math.abs(dot3(nrm, g)), 0) / 2]);
      }
      const shift = [0.0123, -0.0217, 0.0311];
      const inWindow = y => faces.every(([nrm, h]) => Math.abs(dot3(nrm, y.map((c, k) => c - shift[k]))) <= h);
      const proj = (n, g) => [0, 1, 2].map(c => n.reduce((s, x, i) => s + x * g[i][c], 0));
      const R = 3, pts = [], index = new Map();
      const n = new Array(6).fill(-R);
      for (;;) {
        const p = proj(n, gPar), q = proj(n, gPerp);
        if (Math.hypot(...p) <= rPar && inWindow(q)) { index.set(n.join(','), pts.length); pts.push([...p, ...q.map(c => c * 0.5)]); }
        let k = 0; while (k < 6 && n[k] === R) n[k++] = -R;
        if (k === 6) break; n[k]++;
      }
      const edges = [];
      for (const [k, i] of index) {
        const m = k.split(',').map(Number);
        for (let d = 0; d < 6; d++) { const s = m.slice(); s[d]++; const j = index.get(s.join(',')); if (j !== undefined) edges.push([i, j]); }
      }
      return { verts: scaleTo(pts, 1.2), edges };
    }

    // ---- higher Hopf fibrations ---------------------------------------------------------

    // Cayley-Dickson product on 2^k components: quaternions at length 4, octonions at length 8.
    function cdMul(a, b) {
      const n = a.length; if (n === 1) return [a[0] * b[0]];
      const h = n / 2, conj = x => x.map((c, i) => i ? -c : c);
      const a1 = a.slice(0, h), a2 = a.slice(h), b1 = b.slice(0, h), b2 = b.slice(h);
      const sub = (x, y) => x.map((c, i) => c - y[i]), add = (x, y) => x.map((c, i) => c + y[i]);
      return [...sub(cdMul(a1, b1), cdMul(conj(b2), a2)), ...add(cdMul(b2, a1), cdMul(a2, conj(b1)))];
    }
    // S^(2n-1) -> S^n for n = 4 (quaternions) or 8 (octonions). The fibre over m is {(m·y, y)/√(1+|m|²) : |y| = 1},
    // a round (n-1)-sphere because left multiplication is linear; each is drawn as three of its great circles.
    function hopfHigher(n, fibers = 9, seg = 28) {
      const verts = [], edges = [];
      for (let f = 0; f < fibers; f++) {
        const r = Math.tan(Math.PI * (f + 0.5) / (2 * fibers)), m = new Array(n).fill(0);
        const a = 2 * Math.PI * f / fibers;
        m[0] = r * Math.cos(a); m[1 + f % (n - 1)] = r * Math.sin(a);
        const k = 1 / Math.sqrt(1 + r * r);
        for (const dir of [1, 2, n - 1]) {
          const start = verts.length;
          for (let s = 0; s < seg; s++) {
            const t = 2 * Math.PI * s / seg, y = new Array(n).fill(0);
            y[0] = Math.cos(t); y[dir] = Math.sin(t);
            verts.push([...cdMul(m, y), ...y].map(c => c * k));
            edges.push([start + s, start + (s + 1) % seg]);
          }
        }
      }
      return { verts, edges };
    }

    // ---- 4D topology ------------------------------------------------------------------------

    // Artin spinning: a knotted arc in the half-space r ≥ 0 with both ends on r = 0, swept round the
    // boundary plane, (x, y, r) -> (x, y, r cos s, r sin s), is a knotted 2-sphere in 4-space. Drawn as copies
    // of the arc at `copies` spin angles plus the circles traced by every fourth arc point.
    function spunKnot(curve, copies = 10, seg = 120) {
      const pts = [];
      for (let i = 0; i < seg; i++) pts.push(curve(2 * Math.PI * i / seg));
      let lo = 0; pts.forEach((p, i) => { if (p[0] < pts[lo][0]) lo = i; });
      const off = 0.25 - pts[lo][0];
      const arc = [];
      for (let i = 1; i < seg; i++) { const p = pts[(lo + i) % seg]; arc.push([p[1], p[2], p[0] + off]); }
      arc.unshift([arc[0][0], arc[0][1], 0]); arc.push([arc[arc.length - 1][0], arc[arc.length - 1][1], 0]);
      const verts = [], edges = [];
      for (let c = 0; c < copies; c++) {
        const s = 2 * Math.PI * c / copies, start = verts.length;
        arc.forEach(([x, y, r]) => verts.push([x, y, r * Math.cos(s), r * Math.sin(s)].map(v => v * 0.3)));
        for (let i = 0; i + 1 < arc.length; i++) edges.push([start + i, start + i + 1]);
      }
      for (let i = 2; i < arc.length - 2; i += 4)
        for (let c = 0; c < copies; c++) edges.push([c * arc.length + i, ((c + 1) % copies) * arc.length + i]);
      return { verts, edges };
    }
    const trefoilCurve = t => [Math.sin(t) + 2 * Math.sin(2 * t), Math.cos(t) - 2 * Math.cos(2 * t), -Math.sin(3 * t)];
    const figureEightCurve = t => [(2 + Math.cos(2 * t)) * Math.cos(3 * t), (2 + Math.cos(2 * t)) * Math.sin(3 * t), Math.sin(4 * t)];

    // Milnor fibre of z² - w³ on S³: the trefoil's Seifert surface F = {(z, w) ∈ S³ : z² - w³ > 0}. Over a
    // polar grid of w, |z|² = 1 - |w|², and z² = w³ + s for the real s > 0 that puts z² on the circle of
    // radius |z|²; the two roots s± and the two square roots of z² give four sheets. Grid points where no
    // such s exists are NaN (dropped by geometry()). The trefoil itself (z² = w³) is the boundary curve.
    function trefoilSeifert(nr = 14, nb = 48) {
      const verts = [], edges = [];
      for (const root of [1, -1]) for (const half of [0, Math.PI]) {
        const id = (i, j) => start + i * nb + j, start = verts.length;
        for (let i = 0; i < nr; i++) for (let j = 0; j < nb; j++) {
          const r = 0.02 + 0.96 * i / (nr - 1), b = 2 * Math.PI * j / nb;
          const A = 1 - r * r, cr = r ** 3 * Math.cos(3 * b), ci = r ** 3 * Math.sin(3 * b), disc = A * A - ci * ci;
          const s = disc >= 0 ? -cr + root * Math.sqrt(disc) : NaN;
          if (!(s > 0)) { verts.push([NaN, NaN, NaN, NaN]); continue; }
          const ang = root > 0 ? Math.atan2(ci, cr + s) : Math.atan2(-ci, -(cr + s)) + Math.PI;
          const al = ang / 2 + half, zr = Math.sqrt(A);
          verts.push([zr * Math.cos(al), zr * Math.sin(al), r * Math.cos(b), r * Math.sin(b)]);
        }
        for (let i = 0; i < nr; i++) for (let j = 0; j < nb; j++) {
          if (i + 1 < nr) edges.push([id(i, j), id(i + 1, j)]);
          edges.push([id(i, j), id(i, (j + 1) % nb)]);
        }
      }
      let r0 = 0.75; for (let k = 0; k < 40; k++) r0 -= (r0 * r0 + r0 ** 3 - 1) / (2 * r0 + 3 * r0 * r0);
      const start = verts.length, n = 96;
      for (let i = 0; i < n; i++) {
        const t = 2 * Math.PI * i / n, zr = Math.sqrt(1 - r0 * r0);
        verts.push([zr * Math.cos(3 * t), zr * Math.sin(3 * t), r0 * Math.cos(2 * t), r0 * Math.sin(2 * t)]);
        edges.push([start + i, start + (i + 1) % n]);
      }
      // a jump between branches would draw a chord across the figure: keep only short grid edges
      return { verts, edges: edges.filter(([a, b]) => !(dist2(verts[a], verts[b]) > 0.09)) };
    }

    // RP² embedded in 4-space by (x, y, z) on S² -> (yz, xz, xy, (x² - y²)/2), a projection of the Veronese
    // surface. Dropping the fourth coordinate gives Steiner's Roman surface with its triple point.
    function romanSurface(u = 20, vv = 32) {
      const verts = [], edges = [], id = (i, j) => i * vv + j;
      for (let i = 0; i < u; i++) for (let j = 0; j < vv; j++) {
        const th = Math.PI * (i + 0.5) / u, ph = 2 * Math.PI * j / vv;
        const x = Math.sin(th) * Math.cos(ph), y = Math.sin(th) * Math.sin(ph), z = Math.cos(th);
        verts.push([y * z * 2, x * z * 2, x * y * 2, (x * x - y * y)]);
      }
      for (let i = 0; i < u; i++) for (let j = 0; j < vv; j++) {
        if (i + 1 < u) edges.push([id(i, j), id(i + 1, j)]);
        edges.push([id(i, j), id(i, (j + 1) % vv)]);
      }
      return { verts, edges };
    }

    // CP² embedded in 8-space as the rank-one projectors z z*/|z|² (trace-free part of a 3x3 Hermitian
    // matrix). Drawn through its toric picture: over points of the moment triangle (|z0|², |z1|², |z2|²) the
    // fibre is a torus (two circles of phase), collapsing to circles on the edges and points at the corners.
    function cp2(steps = 5, seg = 24) {
      const verts = [], edges = [];
      const embed = (p, a1, a2) => {
        const z = [[Math.sqrt(p[0]), 0], [Math.sqrt(p[1]) * Math.cos(a1), Math.sqrt(p[1]) * Math.sin(a1)], [Math.sqrt(p[2]) * Math.cos(a2), Math.sqrt(p[2]) * Math.sin(a2)]];
        const off = (i, j) => [z[i][0] * z[j][0] + z[i][1] * z[j][1], z[i][1] * z[j][0] - z[i][0] * z[j][1]];
        return [(p[0] - p[1]) / Math.SQRT2, (p[0] + p[1] - 2 * p[2]) / Math.sqrt(6), ...off(0, 1), ...off(0, 2), ...off(1, 2)].map(c => c * Math.SQRT2);
      };
      const circle = f => { const start = verts.length; for (let s = 0; s < seg; s++) { verts.push(f(2 * Math.PI * s / seg)); edges.push([start + s, start + (s + 1) % seg]); } };
      for (let i = 0; i <= steps; i++) for (let j = 0; i + j <= steps; j++) {
        const p = [i / steps, j / steps, (steps - i - j) / steps];
        if (p[1] > 0) circle(a => embed(p, a, 0));
        if (p[2] > 0) circle(a => embed(p, 0, a));
        if (p[1] > 0 && p[2] > 0) circle(a => embed(p, a, a));
      }
      return { verts, edges };
    }

    // ---- honeycombs, products and spheres --------------------------------------------------

    // Finite cluster of a periodic tiling: the tiling's vertices within `radius` of the origin, edges at the
    // tiling's edge length.
    function cluster(points, radius) {
      const verts = points.filter(p => Math.hypot(...p) <= radius + 1e-9);
      return { verts: scaleTo(verts, 1.2), edges: edgesByShortest(verts) };
    }
    function latticeBox(R, keep) {
      const out = [], n = [-R, -R, -R, -R];
      for (;;) {
        if (keep(n)) out.push(n.slice());
        let k = 0; while (k < 4 && n[k] === R) n[k++] = -R;
        if (k === 4) return out; n[k]++;
      }
    }
    // {4,3,3,4}: a 2x2x2x2 block of tesseracts (the integer points of [-1,1]^4).
    const tesseractHoneycomb = () => { const verts = latticeBox(1, () => true); return { verts, edges: edgesByShortest(verts) }; };
    // {3,3,4,3}: the D4 lattice (integer points with even sum); every vertex has the 24-cell as vertex figure.
    const cell16Honeycomb = () => cluster(latticeBox(2, n => (n[0] + n[1] + n[2] + n[3]) % 2 === 0), Math.sqrt(6));
    // {3,4,3,3}: the deep holes of D4, Z^4 with odd sum plus (Z + ½)^4, edge 1; a 24-cell around the origin
    // and its neighbours.
    const cell24Honeycomb = () => {
      const odd = latticeBox(2, n => Math.abs(n[0] + n[1] + n[2] + n[3]) % 2 === 1);
      const half = latticeBox(2, n => n.every(x => x < 2)).map(n => n.map(x => x + 0.5));
      return cluster([...odd, ...half], 1.75);
    };

    // Product of regular polygons {n1}×{n2}×...: one vertex per choice of a corner from each.
    function polyprism(...ns) {
      const verts = [], edges = [];
      const total = ns.reduce((a, b) => a * b, 1), digits = i => ns.map((n, k) => Math.floor(i / ns.slice(0, k).reduce((a, b) => a * b, 1)) % n);
      const index = d => d.reduce((s, x, k) => s + x * ns.slice(0, k).reduce((a, b) => a * b, 1), 0);
      for (let i = 0; i < total; i++) verts.push(digits(i).flatMap((x, k) => { const a = 2 * Math.PI * x / ns[k]; return [Math.cos(a), Math.sin(a)]; }));
      for (let i = 0; i < total; i++) { const d = digits(i); ns.forEach((n, k) => { const e = d.slice(); e[k] = (e[k] + 1) % n; edges.push([i, index(e)]); }); }
      return { verts, edges };
    }

    // S^(n-1) in n-space as a net of round circles: ring r lies at height cos(φ) on the last axis, in a plane
    // spanned by (cos ψ, 0, sin ψ, 0, ...) and (0, cos ψ, 0, sin ψ, ...) that turns with r through the other axes.
    function sphereNet(n, rings = 7, seg = 28) {
      const verts = [], edges = [];
      for (let r = 0; r < rings; r++) {
        const phi = Math.PI * (r + 0.5) / rings, c = Math.sin(phi), s = Math.cos(phi), start = verts.length;
        const psi = Math.PI * r / rings, cp = Math.cos(psi), sp = Math.sin(psi);
        for (let i = 0; i < seg; i++) {
          const th = 2 * Math.PI * i / seg, v = new Array(n).fill(0);
          v[0] = c * cp * Math.cos(th); v[2] = c * sp * Math.cos(th);
          v[1] = c * cp * Math.sin(th); v[3] = c * sp * Math.sin(th);
          v[n - 1] = s;
          verts.push(v); edges.push([start + i, start + (i + 1) % seg]);
        }
      }
      return { verts, edges };
    }

    // ---- projection -------------------------------------------------------------

    // Perspective-project an array of nD points down to 3D. `dist` is the eye distance
    // in each successive higher dimension. Returns Float-friendly [x,y,z] triples.
    function projectTo3D(verts, dist = 3.2) {
      let P = verts;
      while (P[0].length > 3) {
        const out = new Array(P.length);
        for (let i = 0; i < P.length; i++) {
          const v = P[i], w = v[v.length - 1];
          const k = 1 / (dist - w);
          const nv = new Array(v.length - 1);
          for (let d = 0; d < v.length - 1; d++) nv[d] = v[d] * k;
          out[i] = nv;
        }
        P = out;
      }
      return P;
    }

    // Rotate every vertex in the (i,j) plane by angle (in place on a copy).
    function rotate(verts, i, j, angle) {
      const c = Math.cos(angle), s = Math.sin(angle);
      const out = new Array(verts.length);
      for (let k = 0; k < verts.length; k++) {
        const v = verts[k].slice();
        const vi = v[i], vj = v[j];
        v[i] = vi * c - vj * s; v[j] = vi * s + vj * c;
        out[k] = v;
      }
      return out;
    }

    // =====================================================================================
    // The exhibits (from the Hyperspace page). Each entry is data: a stable id, the geometry
    // generator, the rotation planes [i, j, speed rad/s, offset], the plaque facts, a blurb, the
    // article (HTML) and its references. Declaration order is the gallery order, ten to a row.
    // =====================================================================================

    const R = {
      coxeter: `Coxeter, H. S. M. (1973). <i>Regular polytopes</i> (3rd ed.). Dover Publications.`,
      schlafli: `Schläfli, L. (1901). Theorie der vielfachen Kontinuität. <i>Denkschriften der Schweizerischen Naturforschenden Gesellschaft, 38</i>, 1–237.`,
      hinton: `Hinton, C. H. (1888). <i>A new era of thought</i>. Swan Sonnenschein.`,
      hopf: `Hopf, H. (1931). Über die Abbildungen der dreidimensionalen Sphäre auf die Kugelfläche. <i>Mathematische Annalen, 104</i>, 637–665. <a href="https://doi.org/10.1007/BF01457962" target="_blank" rel="noopener">https://doi.org/10.1007/BF01457962</a>`,
      lawson: `Lawson, H. B., Jr. (1970). Complete minimal surfaces in S³. <i>Annals of Mathematics, 92</i>(3), 335–374. <a href="https://doi.org/10.2307/1970625" target="_blank" rel="noopener">https://doi.org/10.2307/1970625</a>`,
      milnor: `Milnor, J. (1956). On manifolds homeomorphic to the 7-sphere. <i>Annals of Mathematics, 64</i>(2), 399–405. <a href="https://doi.org/10.2307/1969983" target="_blank" rel="noopener">https://doi.org/10.2307/1969983</a>`,
      banchoff: `Banchoff, T. F. (1990). <i>Beyond the third dimension: Geometry, computer graphics, and higher dimensions</i>. W. H. Freeman.`,
      conway: `Conway, J. H., & Sloane, N. J. A. (1999). <i>Sphere packings, lattices and groups</i> (3rd ed.). Springer. <a href="https://doi.org/10.1007/978-1-4757-6568-7" target="_blank" rel="noopener">https://doi.org/10.1007/978-1-4757-6568-7</a>`,
      penrose: `Penrose, R. (1974). The role of aesthetics in pure and applied mathematical research. <i>Bulletin of the Institute of Mathematics and Its Applications, 10</i>(2), 266–271.`,
      boy: `Boy, W. (1903). Über die Curvatura integra und die Topologie geschlossener Flächen. <i>Mathematische Annalen, 57</i>, 151–184. <a href="https://doi.org/10.1007/BF01444342" target="_blank" rel="noopener">https://doi.org/10.1007/BF01444342</a>`,
      manning: `Manning, H. P. (1914). <i>Geometry of four dimensions</i>. The Macmillan Company.`,
      weeks: `Weeks, J. R. (2002). <i>The shape of space</i> (2nd ed.). Marcel Dekker.`,
      elte: `Elte, E. L. (1912). <i>The semiregular polytopes of the hyperspaces</i>. Hoitsema Brothers.`,
      stillwell: `Stillwell, J. (1992). <i>Geometry of surfaces</i>. Springer. <a href="https://doi.org/10.1007/978-1-4612-0929-4" target="_blank" rel="noopener">https://doi.org/10.1007/978-1-4612-0929-4</a>`,
      desitter: `de Sitter, W. (1917). On the relativity of inertia: Remarks concerning Einstein's latest hypothesis. <i>Proceedings of the Royal Netherlands Academy of Arts and Sciences, 19</i>, 1217–1225.`,
      minkowski: `Minkowski, H. (1909). Raum und Zeit. <i>Physikalische Zeitschrift, 10</i>, 75–88.`,
      maldacena: `Maldacena, J. (1998). The large-N limit of superconformal field theories and supergravity. <i>Advances in Theoretical and Mathematical Physics, 2</i>(2), 231–252. <a href="https://doi.org/10.4310/ATMP.1998.v2.n2.a1" target="_blank" rel="noopener">https://doi.org/10.4310/ATMP.1998.v2.n2.a1</a>`,
      yau: `Yau, S.-T. (1978). On the Ricci curvature of a compact Kähler manifold and the complex Monge–Ampère equation, I. <i>Communications on Pure and Applied Mathematics, 31</i>(3), 339–411. <a href="https://doi.org/10.1002/cpa.3160310304" target="_blank" rel="noopener">https://doi.org/10.1002/cpa.3160310304</a>`,
      candelas: `Candelas, P., Horowitz, G. T., Strominger, A., & Witten, E. (1985). Vacuum configurations for superstrings. <i>Nuclear Physics B, 258</i>, 46–74. <a href="https://doi.org/10.1016/0550-3213(85)90602-9" target="_blank" rel="noopener">https://doi.org/10.1016/0550-3213(85)90602-9</a>`,
      shechtman: `Shechtman, D., Blech, I., Gratias, D., & Cahn, J. W. (1984). Metallic phase with long-range orientational order and no translational symmetry. <i>Physical Review Letters, 53</i>(20), 1951–1953. <a href="https://doi.org/10.1103/PhysRevLett.53.1951" target="_blank" rel="noopener">https://doi.org/10.1103/PhysRevLett.53.1951</a>`,
      leech: `Leech, J. (1967). Notes on sphere packings. <i>Canadian Journal of Mathematics, 19</i>, 251–267. <a href="https://doi.org/10.4153/CJM-1967-017-0" target="_blank" rel="noopener">https://doi.org/10.4153/CJM-1967-017-0</a>`,
      baez: `Baez, J. C. (2002). The octonions. <i>Bulletin of the American Mathematical Society, 39</i>(2), 145–205. <a href="https://doi.org/10.1090/S0273-0979-01-00934-X" target="_blank" rel="noopener">https://doi.org/10.1090/S0273-0979-01-00934-X</a>`,
      gosset: `Gosset, T. (1900). On the regular and semi-regular figures in space of n dimensions. <i>Messenger of Mathematics, 29</i>, 43–48.`,
      clifford: `Clifford, W. K. (1873). Preliminary sketch of biquaternions. <i>Proceedings of the London Mathematical Society, s1-4</i>(1), 381–395. <a href="https://doi.org/10.1112/plms/s1-4.1.381" target="_blank" rel="noopener">https://doi.org/10.1112/plms/s1-4.1.381</a>`,
      villarceau: `Villarceau, Y. (1848). Théorème sur le tore. <i>Nouvelles Annales de Mathématiques, 7</i>, 345–347.`,
      klein: `Klein, F. (1882). <i>Über Riemann's Theorie der algebraischen Funktionen und ihrer Integrale</i>. B. G. Teubner.`,
      hess: `Hess, E. (1883). <i>Einleitung in die Lehre von der Kugelteilung mit besonderer Berücksichtigung ihrer Anwendung auf die Theorie der gleichflächigen und der gleicheckigen Polyeder</i>. B. G. Teubner.`,
      conwayguy: `Conway, J. H., & Guy, M. J. T. (1967). Four-dimensional Archimedean polytopes. In <i>Proceedings of the Colloquium on Convexity, Copenhagen 1965</i> (pp. 38–39). Københavns Universitets Matematiske Institut.`,
      barneswall: `Barnes, E. S., & Wall, G. E. (1959). Some extreme forms defined in terms of Abelian groups. <i>Journal of the Australian Mathematical Society, 1</i>(1), 47–63.`,
      coxetertodd: `Coxeter, H. S. M., & Todd, J. A. (1953). An extreme duodenary form. <i>Canadian Journal of Mathematics, 5</i>, 384–392. <a href="https://doi.org/10.4153/CJM-1953-043-4" target="_blank" rel="noopener">https://doi.org/10.4153/CJM-1953-043-4</a>`,
      elsersloane: `Elser, V., & Sloane, N. J. A. (1987). A highly symmetric four-dimensional quasicrystal. <i>Journal of Physics A: Mathematical and General, 20</i>(18), 6161–6168. <a href="https://doi.org/10.1088/0305-4470/20/18/018" target="_blank" rel="noopener">https://doi.org/10.1088/0305-4470/20/18/018</a>`,
      levine: `Levine, D., & Steinhardt, P. J. (1984). Quasicrystals: A new class of ordered structures. <i>Physical Review Letters, 53</i>(26), 2477–2480. <a href="https://doi.org/10.1103/PhysRevLett.53.2477" target="_blank" rel="noopener">https://doi.org/10.1103/PhysRevLett.53.2477</a>`,
      adams: `Adams, J. F. (1960). On the non-existence of elements of Hopf invariant one. <i>Annals of Mathematics, 72</i>(1), 20–104. <a href="https://doi.org/10.2307/1970147" target="_blank" rel="noopener">https://doi.org/10.2307/1970147</a>`,
      artin: `Artin, E. (1925). Zur Isotopie zweidimensionaler Flächen im R₄. <i>Abhandlungen aus dem Mathematischen Seminar der Universität Hamburg, 4</i>, 174–177.`,
      seifert: `Seifert, H. (1935). Über das Geschlecht von Knoten. <i>Mathematische Annalen, 110</i>, 571–592. <a href="https://doi.org/10.1007/BF01448044" target="_blank" rel="noopener">https://doi.org/10.1007/BF01448044</a>`,
      milnorsing: `Milnor, J. (1968). <i>Singular points of complex hypersurfaces</i>. Princeton University Press.`,
      apery: `Apéry, F. (1987). <i>Models of the real projective plane</i>. Vieweg.`,
      atiyah: `Atiyah, M. F. (1982). Convexity and commuting Hamiltonians. <i>Bulletin of the London Mathematical Society, 14</i>(1), 1–15. <a href="https://doi.org/10.1112/blms/14.1.1" target="_blank" rel="noopener">https://doi.org/10.1112/blms/14.1.1</a>`,
    };

    // Category palette, solved as one set of 11 against the gallery floor (#06061A): every pair is at
    // least 15 OKLab ΔE apart for normal vision and 8 under simulated protanopia and deuteranopia, every
    // colour sits in OKLCH L 0.48–0.67 with chroma ≥ 0.10 and at least 3:1 contrast with the floor.
    const C = {
      reg:   '#9E75FB', // regular 4-polytopes
      unif:  '#298BC2', // uniform polytopes
      five:  '#4DAA7E', // 5-cube family
      ncube: '#147A57', // n-cube / simplex
      curve: '#F133C7', // curved manifolds
      duo:   '#915A01', // duoprisms
      rot:   '#D67924', // rotations
      phys:  '#A46490', // spacetime / lattices
      prism: '#E40C4D', // 5D prisms & products
      star:  '#1E52FE', // regular star polychora
      comb:  '#A815CC', // honeycombs
    };

    // small helper to keep entries compact
    const P = (i, j, sp = 0.4, off = 0) => [i, j, sp, off];

    var RAW = [

    // ===== ROW 1 — Regular 4-polytopes and Platonic prisms =======================
    { id:'5-cell', name:'5-cell', tag:'Regular 4-polytope', schlafli:'{3,3,3}', family:'reg', dim:4,
      gen:cell5, rot:[P(0,3,.5),P(1,2,.32)],
      facts:{Cells:'5 tetrahedra', Vertices:5, Edges:10, Faces:'10 triangles'},
      blurb:'The simplest 4-polytope — the 4D analogue of the tetrahedron.',
      article:`<p>The <b>5-cell</b> (pentachoron, or 4-simplex) is the simplest possible polytope in four
   dimensions, just as the triangle is simplest in 2D and the tetrahedron in 3D. It has five vertices,
   every pair joined by an edge, bounding five tetrahedral cells. It is self-dual: its dual figure is
   another 5-cell.</p>
   <p>Because all ten edges are equal and every vertex touches every other, the 5-cell is the 4D member
   of the simplex family — the minimal convex shape that genuinely occupies its dimension. Schläfli
   catalogued it among the six regular convex polychora in the 1850s, work published posthumously.</p>`,
      refs:['coxeter', 'schlafli'] },

    { id:'tesseract', name:'Tesseract', tag:'Regular 4-polytope', schlafli:'{4,3,3}', family:'reg', dim:4,
      gen:tesseract, rot:[P(0,3,.45),P(1,3,.28)],
      facts:{Cells:'8 cubes', Vertices:16, Edges:32, Faces:'24 squares'},
      blurb:'The 4D hypercube — eight cubes folded around a fourth axis.',
      article:`<p>The <b>tesseract</b> (8-cell or 4-cube) is the four-dimensional hypercube. Sixteen
   vertices sit at every combination of ±1 in four coordinates; thirty-two edges join vertices that
   differ in a single coordinate. Its boundary is eight cubical cells — the way a cube's boundary is
   six squares.</p>
   <p>What you see rotating here is a <i>perspective shadow</i>: the inner cube is not smaller, it is
   simply farther away along the fourth axis. Charles Howard Hinton popularized the tesseract and even
   coined the word in 1888, devising mental exercises to "see" it.</p>`,
      refs:['coxeter', 'hinton', 'banchoff'] },

    { id:'16-cell', name:'16-cell', tag:'Regular 4-polytope', schlafli:'{3,3,4}', family:'reg', dim:4,
      gen:cell16, rot:[P(0,3,.5),P(1,2,.3)],
      facts:{Cells:'16 tetrahedra', Vertices:8, Edges:24, Faces:'32 triangles'},
      blurb:'The 4D cross-polytope — dual of the tesseract.',
      article:`<p>The <b>16-cell</b> (hexadecachoron, or 4-orthoplex) is the dual of the tesseract. Its
   eight vertices lie at ±1 along each of the four axes, and every non-opposite pair is joined, giving
   24 edges and sixteen tetrahedral cells. It is the four-dimensional cross-polytope.</p>
   <p>The 16-cell, tesseract, and 5-cell are the analogues of the octahedron, cube, and tetrahedron.
   Four dimensions, however, has a richer roster — six regular polychora rather than five Platonic
   solids — thanks to two shapes with no 3D counterpart.</p>`,
      refs:['coxeter', 'schlafli'] },

    { id:'24-cell', name:'24-cell', tag:'Regular 4-polytope', schlafli:'{3,4,3}', family:'reg', dim:4,
      gen:cell24, rot:[P(0,3,.4),P(1,2,.36),P(2,3,.2)],
      facts:{Cells:'24 octahedra', Vertices:24, Edges:96, Faces:'96 triangles'},
      blurb:'The 4D-only regular polytope with no 3D analogue.',
      article:`<p>The <b>24-cell</b> (icositetrachoron) is unique to four dimensions — it has no analogue
   among the Platonic solids and no analogue in any higher dimension either. Its 24 vertices are the
   permutations of (±1, ±1, 0, 0), and its boundary is 24 octahedra. Remarkably, it is self-dual.</p>
   <p>Its vertices are exactly the 24 unit <i>Hurwitz quaternions</i> of norm 1, making it the root
   system of the Lie group F₄. It also gives the densest lattice sphere packing in four dimensions,
   the D₄ lattice, where each sphere touches 24 others.</p>`,
      refs:['coxeter', 'conway', 'schlafli'] },

    { id:'120-cell', name:'120-cell', tag:'Regular 4-polytope', schlafli:'{5,3,3}', family:'reg', dim:4,
      gen:cell120, rot:[P(0,3,.3),P(1,2,.22)],
      facts:{Cells:'120 dodecahedra', Vertices:600, Edges:1200, Faces:'720 pentagons'},
      blurb:'120 dodecahedra wrapped around the fourth dimension.',
      article:`<p>The <b>120-cell</b> (hecatonicosachoron) is the 4D analogue of the dodecahedron. Its
   boundary is built from 120 dodecahedral cells meeting three to an edge, with 600 vertices and 1200
   edges. It is the dual of the 600-cell, and its vertex coordinates are built from the golden ratio φ.</p>
   <p>It is the largest of the six regular polychora by cell count and among the most intricate regular
   figures in any dimension.</p>`,
      refs:['coxeter', 'schlafli'] },

    { id:'600-cell', name:'600-cell', tag:'Regular 4-polytope', schlafli:'{3,3,5}', family:'reg', dim:4,
      gen:cell600, rot:[P(0,3,.32),P(1,2,.24),P(2,3,.16)],
      facts:{Cells:'600 tetrahedra', Vertices:120, Edges:720, Faces:'1200 triangles'},
      blurb:'600 tetrahedra and the icosahedral symmetry of 4-space.',
      article:`<p>The <b>600-cell</b> (hexacosichoron) is the 4D analogue of the icosahedron. Its 120
   vertices are the unit <i>icosians</i> — built from the golden ratio — and they coincide with the
   vertices of the icositetrachoron plus two scaled 24-cells. Twenty tetrahedra meet at every vertex.</p>
   <p>Its symmetry group, H₄, has order 14,400 and is the largest finite reflection group acting in
   four dimensions. The 600-cell and 120-cell are dual, the icosahedral pair with no higher-dimensional
   echo.</p>`,
      refs:['coxeter', 'conway', 'schlafli'] },

    { id:'tetrahedral-prism', name:'Tetrahedral prism', tag:'Uniform prism', schlafli:'{3,3}×{ }', family:'reg', dim:4,
      gen:()=>duoprism(3,2), rot:[P(0,3,.4),P(1,2,.3)],
      facts:{Cells:'2 tetrahedra + 4 prisms', Vertices:8, Edges:16, Faces:14},
      blurb:'A tetrahedron extruded into the fourth dimension.',
      article:`<p>A <b>tetrahedral prism</b> is the Cartesian product of a tetrahedron and a line segment —
   the way a triangular prism is a triangle times a segment. Two tetrahedral cells (the "ends") are
   joined by four triangular-prism cells (the "walls"), giving a uniform 4-polytope.</p>
   <p>Prismatic polychora like this form an infinite family: take any uniform polyhedron and extrude it
   along a fourth axis. They bridge the Platonic solids and the genuinely four-dimensional figures.</p>`,
      refs:['coxeter', 'manning'] },

    { id:'octahedral-prism', name:'Octahedral prism', tag:'Uniform prism', schlafli:'{3,4}×{ }', family:'reg', dim:4,
      gen:()=>duoprism(4,2), rot:[P(0,3,.38),P(1,2,.28)],
      facts:{Cells:'2 octahedra + 8 prisms', Vertices:12, Edges:30, Faces:20},
      blurb:'An octahedron extruded along a fourth axis.',
      article:`<p>The <b>octahedral prism</b> pairs two octahedra through eight triangular prisms — the
   product of the octahedron with a segment. Like all uniform prisms it is "almost" three-dimensional:
   one direction is a simple extrusion, the rest is an ordinary Platonic solid.</p>
   <p>Such prisms are the four-dimensional cousins of the everyday prisms and antiprisms, and they help
   classify the full set of convex uniform polychora.</p>`,
      refs:['coxeter', 'manning'] },

    { id:'icosahedral-prism', name:'Icosahedral prism', tag:'Uniform prism', schlafli:'{3,5}×{ }', family:'reg', dim:4,
      gen:()=>prism4D(icosahedron3D()), rot:[P(0,3,.32),P(1,2,.24)],
      facts:{Cells:'2 icosahedra + 20 prisms', Vertices:24, Edges:72, Faces:'20 triangles + 30 squares'},
      blurb:'An icosahedron extruded into the fourth dimension.',
      article:`<p>The <b>icosahedral prism</b> is the Cartesian product of an icosahedron and a line segment.
   Its 24 vertices are two copies of the icosahedron's twelve, and its boundary is two icosahedral cells
   joined by twenty triangular prisms.</p>
   <p>Together with the tetrahedral, octahedral and dodecahedral prisms, it completes the family of
   prisms over the Platonic solids in four dimensions.</p>`,
      refs:['coxeter', 'manning'] },

    { id:'dodecahedral-prism', name:'Dodecahedral prism', tag:'Uniform prism', schlafli:'{5,3}×{ }', family:'reg', dim:4,
      gen:()=>prism4D(dodecahedron3D()), rot:[P(0,3,.28),P(1,2,.22)],
      facts:{Cells:'2 dodecahedra + 12 prisms', Vertices:40, Edges:80, Faces:'24 pentagons + 60 squares'},
      blurb:'A dodecahedron extruded into the fourth dimension.',
      article:`<p>The <b>dodecahedral prism</b> pairs two dodecahedra through twelve pentagonal prisms.
   With forty vertices it is the largest Platonic prism in four dimensions, and it inherits the
   full icosahedral symmetry of its base.</p>
   <p>These prisms are the simplest uniform polychora: take any Platonic solid and extrude it
   along a new axis.</p>`,
      refs:['coxeter', 'manning'] },
    // ===== ROW 2 — Uniform 4-polytopes (truncations & rectifications) ============
    { id:'rectified-5-cell', name:'Rectified 5-cell', tag:'Uniform 4-polytope', schlafli:'t₁{3,3,3}', family:'unif', dim:4,
      gen:()=>wythoff(4,A4,ring(4,1)), rot:[P(0,3,.45),P(1,2,.3)],
      facts:{Cells:'5 tetra + 5 octa', Vertices:10, Edges:30, Faces:30},
      blurb:'The 5-cell with its vertices sliced to mid-edges.',
      article:`<p>The <b>rectified 5-cell</b> (dispentachoron) is what you get by cutting each vertex of a
   5-cell down to the midpoints of its edges. The result has ten vertices, five tetrahedral cells and
   five octahedral cells, and is one of the simplest non-regular uniform polychora.</p>
   <p>Rectification is one of the standard Wythoffian operations that generate uniform polytopes from a
   regular seed, catalogued systematically by Elte and later Coxeter.</p>`,
      refs:['elte', 'coxeter'] },

    { id:'truncated-5-cell', name:'Truncated 5-cell', tag:'Uniform 4-polytope', schlafli:'t₀,₁{3,3,3}', family:'unif', dim:4,
      gen:()=>wythoff(4,A4,ring(4,0,1)), rot:[P(0,3,.4),P(2,3,.26)],
      facts:{Cells:'5 tetra + 5 trunc-tetra', Vertices:20, Edges:40, Faces:30},
      blurb:'Each vertex of the 5-cell shaved to a small tetrahedron.',
      article:`<p>The <b>truncated 5-cell</b> shaves each of the 5-cell's five vertices, replacing them
   with small tetrahedral faces while the original tetrahedral cells become truncated tetrahedra. It
   has twenty vertices and is the 4D analogue of the truncated tetrahedron.</p>
   <p>Truncation, like rectification, is governed by a Coxeter–Dynkin marking; varying which mirrors are
   "active" produces the whole zoo of uniform polychora from a single symmetry group.</p>`,
      refs:['elte', 'coxeter'] },

    { id:'cantellated-5-cell', name:'Cantellated 5-cell', tag:'Uniform 4-polytope', schlafli:'t₀,₂{3,3,3}', family:'unif', dim:4,
      gen:()=>wythoff(4,A4,ring(4,0,2)), rot:[P(0,3,.36),P(1,3,.24)],
      facts:{Cells:'5 cuboctahedra + 5 octahedra + 10 triangular prisms', Vertices:30, Edges:90, Faces:80},
      blurb:'Both edges and vertices of the 5-cell expanded outward.',
      article:`<p>The <b>cantellated 5-cell</b> applies a second-order truncation — beveling both vertices
   and edges of the 5-cell at once. It carries thirty vertices, and its twenty cells are five
   cuboctahedra, five octahedra and ten triangular prisms.</p>
   <p>Cantellation (Coxeter's t₀,₂ operation) is what turns a cube into a rhombicuboctahedron in 3D; in
   4D it produces correspondingly richer uniform figures.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'runcinated-5-cell', name:'Runcinated 5-cell', tag:'Uniform 4-polytope', schlafli:'t₀,₃{3,3,3}', family:'unif', dim:4,
      gen:()=>wythoff(4,A4,ring(4,0,3)), rot:[P(0,3,.3),P(1,2,.3),P(2,3,.18)],
      facts:{Cells:'30 cells', Vertices:20, Edges:60, Faces:70},
      blurb:'A purely 4D expansion that separates the cells of the 5-cell.',
      article:`<p>The <b>runcinated 5-cell</b> uses <i>runcination</i> — a third-order truncation that has
   no analogue below four dimensions. It expands the 5-cell along its cells, pulling them apart and
   filling the gaps with prisms, yielding a highly symmetric uniform polychoron with 30 cells.</p>
   <p>Runcination is the first operation that is genuinely four-dimensional: the index "3" in t₀,₃ refers
   to a 3-face, which only exists once you have four dimensions to work in.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'rectified-tesseract', name:'Rectified tesseract', tag:'Uniform 4-polytope', schlafli:'t₁{4,3,3}', family:'unif', dim:4,
      gen:()=>wythoff(4,B4,ring(4,1)), rot:[P(0,3,.4),P(1,2,.28)],
      facts:{Cells:'8 cuboct + 16 tetra', Vertices:32, Edges:96, Faces:88},
      blurb:'The tesseract with its corners cut to mid-edges.',
      article:`<p>The <b>rectified tesseract</b> truncates the tesseract's sixteen vertices all the way to
   the midpoints of its edges. The eight cubic cells become cuboctahedra and sixteen new tetrahedral
   cells appear where the vertices were, for 32 vertices in all.</p>
   <p>It sits midway between the tesseract and its dual, the 16-cell — rectification carried far enough
   would reach the 16-cell itself.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'truncated-tesseract', name:'Truncated tesseract', tag:'Uniform 4-polytope', schlafli:'t₀,₁{4,3,3}', family:'unif', dim:4,
      gen:()=>wythoff(4,B4,ring(4,0,1)), rot:[P(0,3,.36),P(1,3,.24)],
      facts:{Cells:'8 trunc-cubes + 16 tetra', Vertices:64, Edges:128, Faces:88},
      blurb:'The hypercube with truncated-cube cells.',
      article:`<p>The <b>truncated tesseract</b> shaves each of the tesseract's sixteen vertices, turning
   its eight cubic cells into truncated cubes and adding sixteen tetrahedra. With 64 vertices it is
   among the larger single-operation uniform polychora.</p>
   <p>It is the four-dimensional analogue of the truncated cube, one of the Archimedean solids.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'rectified-24-cell', name:'Rectified 24-cell', tag:'Uniform 4-polytope', schlafli:'t₁{3,4,3}', family:'unif', dim:4,
      gen:()=>wythoff(4,F4,ring(4,1)), rot:[P(0,3,.34),P(1,2,.26),P(2,3,.16)],
      facts:{Cells:'24 cuboct + 24 cubes', Vertices:96, Edges:288, Faces:240},
      blurb:'The self-dual 24-cell, rectified into cuboctahedra.',
      article:`<p>The <b>rectified 24-cell</b> cuts the 24-cell's vertices to mid-edge, turning its 24
   octahedral cells into cuboctahedra and exposing 24 cubic cells. It has 96 vertices and preserves the
   full F₄ symmetry of its parent.</p>
   <p>Because the 24-cell is self-dual and uniquely four-dimensional, its rectification is one of the
   more elegant uniform polychora — all of its symmetry inherited from a shape with no analogue
   anywhere else.</p>`,
      refs:['coxeter', 'conway'] },

    { id:'snub-24-cell', name:'Snub 24-cell', tag:'Uniform 4-polytope', schlafli:'s{3,4,3}', family:'unif', dim:4,
      gen:snub24, rot:[P(0,3,.3),P(1,2,.22),P(2,3,.14)],
      facts:{Cells:'120 tetra + 24 icosa', Vertices:96, Edges:432, Faces:480},
      blurb:'A chiral polytope whose vertices form part of the 600-cell.',
      article:`<p>The <b>snub 24-cell</b> is a chiral (handed) uniform polychoron whose 96 vertices are a
   subset of the 600-cell's. Its cells are 24 icosahedra and 120 tetrahedra. It was discovered by
   Thorold Gosset and is sometimes called the semi-snub polyoctahedron.</p>
   <p>Like the snub cube and snub dodecahedron in 3D, it comes in left- and right-handed forms that are
   mirror images and cannot be rotated into one another.</p>`,
      refs:['gosset', 'coxeter'] },

    { id:'cantellated-tesseract', name:'Cantellated tesseract', tag:'Uniform 4-polytope', schlafli:'t₀,₂{4,3,3}', family:'unif', dim:4,
      gen:()=>wythoff(4,B4,ring(4,0,2)), rot:[P(0,3,.34),P(1,2,.26),P(2,3,.18)],
      facts:{Cells:'8 rhombicuboct + 16 octa + 32 prisms', Vertices:96, Edges:288, Faces:248},
      blurb:'The tesseract with vertices and edges expanded outward.',
      article:`<p>The <b>cantellated tesseract</b> bevels both the vertices and edges of the tesseract
   simultaneously. Its cells are rhombicuboctahedra, octahedra and triangular prisms, and it preserves
   the full hyperoctahedral symmetry of the parent.</p>
   <p>Cantellation is the operation that turns a cube into a rhombicuboctahedron; here it produces
   a correspondingly richer four-dimensional figure.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'runcinated-tesseract', name:'Runcinated tesseract', tag:'Uniform 4-polytope', schlafli:'t₀,₃{4,3,3}', family:'unif', dim:4,
      gen:()=>wythoff(4,B4,ring(4,0,3)), rot:[P(0,3,.3),P(1,2,.24),P(2,3,.16)],
      facts:{Cells:'16 tetra + 32 tri-prisms + 32 cubes', Vertices:64, Edges:192, Faces:208},
      blurb:`A 4D expansion that pulls the tesseract's cells apart.`,
      article:`<p>The <b>runcinated tesseract</b> applies runcination to the tesseract — a third-order
   truncation that separates its eight cubes and fills the gaps with 24 more cubes, 32 triangular prisms and
   16 tetrahedra. It has sixty-four
   vertices and is one of the most symmetrically expanded forms of the 4-cube.</p>`,
      refs:['coxeter', 'elte'] },
    // ===== ROW 3 — The 5-cube (penteract) family =================================
    { id:'penteract', name:'Penteract', tag:'Regular 5-polytope', schlafli:'{4,3,3,3}', family:'five', dim:5,
      gen:()=>nCube(5), rot:[P(0,4,.4),P(1,3,.28),P(2,4,.18)],
      facts:{Dimension:'5D', Vertices:32, Edges:80, '4-faces':'10 tesseracts'},
      blurb:'The 5-dimensional hypercube — ten tesseracts on its boundary.',
      article:`<p>The <b>penteract</b> (5-cube) is the five-dimensional hypercube. Its 32 vertices are every
   combination of ±1 in five coordinates, joined by 80 edges. Its boundary consists of ten tesseracts,
   just as a tesseract is bounded by eight cubes and a cube by six squares.</p>
   <p>The doubling pattern is exact: an n-cube has 2ⁿ vertices, n·2ⁿ⁻¹ edges, and 2n cells of dimension
   n−1. What you see is a fivefold-nested perspective shadow flattened twice over.</p>`,
      refs:['coxeter', 'hinton'] },

    { id:'5-orthoplex', name:'5-orthoplex', tag:'Regular 5-polytope', schlafli:'{3,3,3,4}', family:'five', dim:5,
      gen:()=>nOrthoplex(5), rot:[P(0,4,.42),P(1,3,.3)],
      facts:{Dimension:'5D', Vertices:10, Edges:40, '4-faces':'32 5-cells'},
      blurb:'The 5D cross-polytope — dual of the penteract.',
      article:`<p>The <b>5-orthoplex</b> (pentacross) is the five-dimensional cross-polytope and the dual of
   the penteract. Its ten vertices sit at ±1 along each axis; every non-opposite pair is joined, giving
   40 edges and 32 four-dimensional 5-cell facets.</p>
   <p>The orthoplex family (square, octahedron, 16-cell, …) always has 2n vertices and 2ⁿ simplex
   facets — the most "spiky" of the regular polytopes.</p>`,
      refs:['coxeter', 'schlafli'] },

    { id:'5-simplex', name:'5-simplex', tag:'Regular 5-polytope', schlafli:'{3,3,3,3}', family:'five', dim:5,
      gen:()=>nSimplex(5), rot:[P(0,4,.4),P(1,3,.28)],
      facts:{Dimension:'5D', Vertices:6, Edges:15, '4-faces':'6 5-cells'},
      blurb:'Six mutually equidistant points — the simplest 5-polytope.',
      article:`<p>The <b>5-simplex</b> (hexateron) is the simplest 5-polytope: six vertices, every pair
   joined, bounding six 5-cell facets. It is self-dual and the five-dimensional member of the simplex
   family that runs triangle → tetrahedron → 5-cell → hexateron.</p>
   <p>A k-simplex always has k+1 vertices and the maximum symmetry of any polytope in its dimension —
   the full symmetric group on its vertices.</p>`,
      refs:['coxeter', 'schlafli'] },

    { id:'rectified-penteract', name:'Rectified penteract', tag:'Uniform 5-polytope', schlafli:'t₁{4,3,3,3}', family:'five', dim:5,
      gen:()=>wythoff(5,B5,ring(5,1)), rot:[P(0,4,.36),P(2,3,.24)],
      facts:{Dimension:'5D', Vertices:80, Edges:320, '4-faces':'10 rectified tesseracts + 32 5-cells'},
      blurb:'The penteract cut to its edge-midpoints.',
      article:`<p>The <b>rectified penteract</b> truncates the 5-cube's 32 vertices to the midpoints of its
   edges, producing 80 new vertices — one per original edge. Its facets are rectified tesseracts and
   5-cells.</p>
   <p>Rectification in five dimensions follows the same Wythoffian rule as in lower ones: mark the second
   node of the Coxeter diagram and read off the resulting uniform polytope.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'truncated-penteract', name:'Truncated penteract', tag:'Uniform 5-polytope', schlafli:'t₀,₁{4,3,3,3}', family:'five', dim:5,
      gen:()=>wythoff(5,B5,ring(5,0,1)), rot:[P(0,4,.32),P(1,4,.22)],
      facts:{Dimension:'5D', Vertices:160, Edges:400, '4-faces':'10 truncated tesseracts + 32 5-cells'},
      blurb:'The 5-cube with shaved vertices.',
      article:`<p>The <b>truncated penteract</b> shaves each of the 5-cube's 32 vertices, replacing each
   with a small 5-cell facet while the tesseract cells become truncated tesseracts. It has 160 vertices.</p>
   <p>It is the five-dimensional analogue of the truncated cube and truncated tesseract — the same
   operation applied one dimension higher.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'5-demicube', name:'5-demicube', tag:'Uniform 5-polytope', schlafli:'h{4,3,3,3}', family:'five', dim:5,
      gen:()=>nDemicube(5), rot:[P(0,4,.4),P(1,3,.26)],
      facts:{Dimension:'5D', Vertices:16, Edges:80, '4-faces':'16 5-cells + 10 16-cells', Symmetry:'D₅'},
      blurb:'The alternated penteract — half its vertices removed.',
      article:`<p>The <b>5-demicube</b> (demipenteract) is the penteract with alternate vertices deleted —
   keeping the sixteen whose coordinates have an even number of minus signs. Its facets are sixteen
   5-cells and ten 16-cells, and its symmetry group is D₅.</p>
   <p>Demicubes are the seed of the important Dₙ symmetry family; in five dimensions the demipenteract
   is the first to acquire the extra diagonal symmetry that makes that family special.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'cantellated-penteract', name:'Cantellated penteract', tag:'Uniform 5-polytope', schlafli:'t₀,₂{4,3,3,3}', family:'five', dim:5,
      gen:()=>wythoff(5,B5,ring(5,0,2)), rot:[P(0,4,.3),P(2,4,.2),P(1,3,.16)],
      facts:{Dimension:'5D', Vertices:320, Edges:1280, '4-faces':'10 cantellated tesseracts + 32 rectified 5-cells + 80 tetrahedral prisms', Symmetry:'B₅'},
      blurb:'A second-order bevel of the 5-cube.',
      article:`<p>The <b>cantellated penteract</b> bevels both the vertices and edges of the 5-cube
   simultaneously (Coxeter's t₀,₂). Its 320 vertices bound ten cantellated tesseracts, 32 rectified
   5-cells and 80 tetrahedral prisms.</p>
   <p>It belongs to the B₅ family of uniform 5-polytopes — the same hyperoctahedral symmetry that
   governs the penteract and 5-orthoplex.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'omnitruncated-5-simplex', name:'Omnitruncated 5-simplex', tag:'Uniform 5-polytope', schlafli:'t₀,₁,₂,₃,₄{3,3,3,3}', family:'five', dim:5,
      gen:()=>wythoff(5,A5,ring(5,0,1,2,3,4)), rot:[P(0,4,.3),P(1,3,.22),P(2,4,.14)],
      facts:{Dimension:'5D', Vertices:720, Edges:1800, Symmetry:'A₅', Note:'all mirrors active'},
      blurb:'Every mirror of the 5-simplex turned on at once.',
      article:`<p>The <b>omnitruncated 5-simplex</b> activates every node of the A₅ Coxeter diagram at
   once — the maximal truncation. It has exactly 720 vertices, one for each element of the symmetric
   group S₆, because an omnitruncated simplex is a <i>permutohedron</i>.</p>
   <p>Its vertices are the permutations of (1, 2, 3, 4, 5, 6), and it tiles 5-space — a beautiful link
   between combinatorics and geometry.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'runcinated-penteract', name:'Runcinated penteract', tag:'Uniform 5-polytope', schlafli:'t₀,₃{4,3,3,3}', family:'five', dim:5,
      gen:()=>wythoff(5,B5,ring(5,0,3)), rot:[P(0,4,.32),P(1,3,.24),P(2,4,.16)],
      facts:{Dimension:'5D', Vertices:320, Edges:1440, '4-faces':'10 runcinated tesseracts + 32 rectified 5-cells + 80 octahedral prisms + 80 {4}×{3} duoprisms', Symmetry:'B₅'},
      blurb:'The 5-cube with its cells separated and prisms inserted.',
      article:`<p>The <b>runcinated penteract</b> expands the 5-cube along its 3-faces, pulling the tesseract
   cells apart and bridging the gaps with rectified 5-cells, octahedral prisms and square-triangle
   duoprisms. It is the five-dimensional analogue of
   the runcinated tesseract.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'stericated-penteract', name:'Stericated penteract', tag:'Uniform 5-polytope', schlafli:'t₀,₄{4,3,3,3}', family:'five', dim:5,
      gen:()=>wythoff(5,B5,ring(5,0,4)), rot:[P(0,4,.3),P(1,3,.22),P(2,4,.14)],
      facts:{Dimension:'5D', Vertices:160, Edges:640, '4-faces':'10 tesseracts + 40 cubic prisms + 80 {4}×{3} duoprisms + 80 tetrahedral prisms + 32 5-cells', Symmetry:'B₅'},
      blurb:'The 5-cube expanded along its 4-faces — its tesseract facets.',
      article:`<p>The <b>stericated penteract</b> is the 5-cube expanded along its 4-faces, the highest-order
   truncation possible in five dimensions. Its 160 vertices bound the ten original tesseracts, pulled
   apart, with 32 5-cells where the vertices were and cubic prisms, duoprisms and tetrahedral prisms
   filling the gaps between them.</p>`,
      refs:['coxeter', 'elte'] },
    // ===== ROW 4 — Higher hypercubes, simplices, and the Gosset figure ===========
    { id:'6-cube', name:'6-cube', tag:'Regular 6-polytope', schlafli:'{4,3,3,3,3}', family:'ncube', dim:6,
      gen:()=>nCube(6), rot:[P(0,5,.36),P(1,4,.26),P(2,3,.18)],
      facts:{Dimension:'6D', Vertices:64, Edges:192, '5-faces':'12 penteracts'},
      blurb:'The hexeract — 64 vertices in six dimensions.',
      article:`<p>The <b>6-cube</b> (hexeract) is the six-dimensional hypercube, with 64 vertices, 192 edges,
   and twelve penteract facets. The hypercube family grows explosively: each new dimension doubles the
   vertex count.</p>
   <p>The flattened shadow you see has passed through three perspective divisions to reach 3D, so depth
   along three separate hidden axes is compressed into the same picture.</p>`,
      refs:['coxeter', 'hinton'] },

    { id:'7-cube', name:'7-cube', tag:'Regular 7-polytope', schlafli:'{4,3⁵}', family:'ncube', dim:7,
      gen:()=>nCube(7), rot:[P(0,6,.32),P(1,5,.24),P(2,4,.16)],
      facts:{Dimension:'7D', Vertices:128, Edges:448, '6-faces':'14 hexeracts'},
      blurb:'The hepteract — 128 vertices in seven dimensions.',
      article:`<p>The <b>7-cube</b> (hepteract) has 128 vertices and 448 edges. By now the wireframe is dense
   enough that the projection looks almost like a fog of points, yet the doubling structure is exactly
   the same as a humble square.</p>
   <p>Hypercube graphs Qₙ like this one are central in computer science as models of parallel networks,
   error-correcting codes, and Boolean logic.</p>`,
      refs:['coxeter'] },

    { id:'8-cube', name:'8-cube', tag:'Regular 8-polytope', schlafli:'{4,3⁶}', family:'ncube', dim:8,
      gen:()=>nCube(8), rot:[P(0,7,.3),P(1,6,.22),P(2,5,.15),P(3,4,.1)],
      facts:{Dimension:'8D', Vertices:256, Edges:1024, '7-faces':'16 hepteracts'},
      blurb:'The octeract — 256 vertices in eight dimensions.',
      article:`<p>The <b>8-cube</b> (octeract) carries 256 vertices and 1024 edges. Its vertex set is exactly
   the 256 bytes — every 8-bit string — which is why the 8-cube graph underlies the Hamming(8) code and
   much of digital communication theory.</p>
   <p>Eight dimensions is special: it is where the E₈ lattice and the octonions live, both of which you
   can visit elsewhere in this hall.</p>`,
      refs:['coxeter', 'conway'] },

    { id:'6-simplex', name:'6-simplex', tag:'Regular 6-polytope', schlafli:'{3⁵}', family:'ncube', dim:6,
      gen:()=>nSimplex(6), rot:[P(0,5,.36),P(1,4,.26)],
      facts:{Dimension:'6D', Vertices:7, Edges:21, Symmetry:'A₆'},
      blurb:'Seven mutually equidistant points in six dimensions.',
      article:`<p>The <b>6-simplex</b> (heptapeton) is seven points in six dimensions, every pair the same
   distance apart. It is self-dual with symmetry group S₇. The simplex is always the "tightest" polytope
   — the minimal convex hull that fills its dimension.</p>
   <p>Simplices are the building blocks of triangulation and of the simplex method in optimization; the
   higher-dimensional ones underpin finite-element meshes and topology.</p>`,
      refs:['coxeter', 'schlafli'] },

    { id:'7-simplex', name:'7-simplex', tag:'Regular 7-polytope', schlafli:'{3⁶}', family:'ncube', dim:7,
      gen:()=>nSimplex(7), rot:[P(0,6,.34),P(1,5,.24)],
      facts:{Dimension:'7D', Vertices:8, Edges:28, Symmetry:'A₇'},
      blurb:'Eight mutually equidistant points in seven dimensions.',
      article:`<p>The <b>7-simplex</b> (octaexon) has eight vertices, 28 edges, and the complete symmetry
   group S₈. Every vertex is connected to every other, so its edge graph is the complete graph K₈.</p>
   <p>The contrast with the 7-cube is instructive: both live in nearby dimensions, but the simplex is
   maximally connected and minimal in vertex count, while the cube is sparse and exponentially large.</p>`,
      refs:['coxeter', 'schlafli'] },

    { id:'6-orthoplex', name:'6-orthoplex', tag:'Regular 6-polytope', schlafli:'{3,3,3,3,4}', family:'ncube', dim:6,
      gen:()=>nOrthoplex(6), rot:[P(0,5,.36),P(1,4,.26)],
      facts:{Dimension:'6D', Vertices:12, Edges:60, '5-faces':'64 simplices'},
      blurb:'The 6D cross-polytope — dual of the hexeract.',
      article:`<p>The <b>6-orthoplex</b> (hexacross) is the six-dimensional cross-polytope: twelve vertices
   at ±1 along each axis, 60 edges, and 64 simplex facets. It is the dual of the 6-cube.</p>
   <p>Orthoplexes always realize the ℓ¹ unit ball, while their dual cubes realize the ℓ∞ ball — the two
   extremes that sandwich the round Euclidean sphere in every dimension.</p>`,
      refs:['coxeter', 'conway'] },

    { id:'7-orthoplex', name:'7-orthoplex', tag:'Regular 7-polytope', schlafli:'{3⁴,4}', family:'ncube', dim:7,
      gen:()=>nOrthoplex(7), rot:[P(0,6,.34),P(1,5,.24)],
      facts:{Dimension:'7D', Vertices:14, Edges:84, '6-faces':'128 simplices'},
      blurb:'The 7D cross-polytope — dual of the hepteract.',
      article:`<p>The <b>7-orthoplex</b> (heptacross) has fourteen vertices and 128 six-dimensional simplex
   facets. As the dual of the 7-cube, its facet count (2⁷ = 128) equals the 7-cube's vertex count, a
   duality that holds in every dimension.</p>
   <p>The dense criss-cross of edges you see is every pair of non-opposite vertices — the orthoplex is
   nearly the complete graph, missing only the antipodal links.</p>`,
      refs:['coxeter', 'conway'] },

    { id:'e8-polytope-421', name:'E₈ polytope (4₂₁)', tag:'Gosset polytope', schlafli:'4₂₁', family:'ncube', dim:8,
      gen:e8Polytope, rot:[P(0,7,.26),P(1,6,.2),P(2,5,.14),P(3,4,.1)],
      facts:{Dimension:'8D', Vertices:240, Edges:6720, Symmetry:'E₈ (order 696,729,600)'},
      blurb:'The 240 roots of E₈ — the most symmetric polytope in 8D.',
      article:`<p>The <b>4₂₁ polytope</b>, discovered by Thorold Gosset, has 240 vertices — exactly the
   roots of the exceptional Lie algebra E₈. Each vertex touches 56 others, and its symmetry group has
   696,729,600 elements, the largest of any 8-dimensional polytope.</p>
   <p>Its famous 2D shadow — a nested set of concentric 30-gons — is one of the most reproduced images
   in mathematics. E₈ appears in string theory, the densest 8D sphere packing, and (conjecturally) in
   the physics of certain magnetic materials.</p>`,
      refs:['gosset', 'coxeter', 'conway'] },

    { id:'9-cube', name:'9-cube', tag:'Regular 9-polytope', schlafli:'{4,3⁷}', family:'ncube', dim:9,
      gen:()=>nCube(9), rot:[P(0,8,.28),P(1,7,.2),P(2,6,.14),P(3,5,.1)],
      facts:{Dimension:'9D', Vertices:512, Edges:2304, '8-faces':'18 8-cubes'},
      blurb:'The enneract — 512 vertices in nine dimensions.',
      article:`<p>The <b>9-cube</b> (enneract) has 512 vertices and 2,304 edges. Each new dimension of the
   hypercube family doubles the vertex count, so the 9-cube is already a dense wireframe of points and
   edges.</p>`,
      refs:['coxeter'] },

    { id:'10-orthoplex', name:'10-orthoplex', tag:'Regular 10-polytope', schlafli:'{3⁸,4}', family:'ncube', dim:10,
      gen:()=>nOrthoplex(10), rot:[P(0,9,.3),P(1,8,.22)],
      facts:{Dimension:'10D', Vertices:20, Edges:180, '9-faces':'512 simplices'},
      blurb:'The 10D cross-polytope — twenty vertices joined by 180 edges.',
      article:`<p>The <b>10-orthoplex</b> (decacross) is the ten-dimensional cross-polytope. Its twenty vertices
   sit at ±1 along each axis, and every non-opposite pair is joined, giving 180 edges and 512 simplex
   facets.</p>`,
      refs:['coxeter', 'conway'] },
    // ===== ROW 5 — Curved manifolds and topological surfaces =====================
    { id:'glome-3-sphere', name:'Glome (3-sphere)', tag:'Curved 4-manifold', schlafli:'S³', family:'curve', dim:4,
      gen:()=>glome(8,40), rot:[P(0,3,.4),P(1,3,.24)],
      facts:{Dimension:'surface in 4D', Curvature:'positive, constant', Symmetry:'O(4)'},
      blurb:'The set of all points equidistant from a centre in 4-space.',
      article:`<p>A <b>glome</b>, or 3-sphere (S³), is the set of points at fixed distance from a centre in
   four-dimensional space — the direct analogue of an ordinary sphere. Its surface is three-dimensional:
   a creature living on it could move in three independent directions and never find an edge.</p>
   <p>The 3-sphere is the simplest closed 3-manifold and was central to the Poincaré conjecture, proved
   by Grigori Perelman in 2003. Here it is drawn as a net of great circles.</p>`,
      refs:['weeks', 'banchoff'] },

    { id:'clifford-torus', name:'Clifford torus', tag:'Flat surface in S³', schlafli:'T²⊂S³', family:'curve', dim:4,
      gen:()=>cliffordTorus(18,18), rot:[P(0,2,.4),P(1,3,.4)],
      facts:{Dimension:'2D surface in 4D', Curvature:'zero (intrinsically flat)', Lives_on:'the 3-sphere'},
      blurb:'A torus that is perfectly flat — only possible in 4D.',
      article:`<p>The <b>Clifford torus</b> is a torus that is intrinsically <i>flat</i>: it has zero
   Gaussian curvature everywhere, with no stretching anywhere on its surface. This is impossible for any
   doughnut in ordinary 3-space, but in four dimensions it sits comfortably inside the 3-sphere as the
   product of two equal circles.</p>
   <p>It divides S³ into two congruent solid tori and is the model for the Hopf fibration. Whether it is
   the area-minimizing torus in S³ — the Lawson conjecture — was proven by Simon Brendle in 2013.</p>`,
      refs:['clifford', 'lawson', 'banchoff'] },

    { id:'klein-bottle', name:'Klein bottle', tag:'Non-orientable surface', schlafli:'K²', family:'curve', dim:4,
      gen:()=>kleinBottle(26,14), rot:[P(0,3,.34),P(2,3,.24)],
      facts:{Dimension:'2D surface', Sides:'one (non-orientable)', Embeds_in:'4D without self-intersection'},
      blurb:'A one-sided surface that only embeds cleanly in 4D.',
      article:`<p>The <b>Klein bottle</b> is a closed surface with no inside or outside — a one-sided,
   non-orientable surface. Any model in 3-space must pass through itself, but in four dimensions it
   embeds perfectly cleanly, the self-intersection lifting apart along the fourth axis.</p>
   <p>It can be built by gluing two Möbius strips along their edges, and it is a foundational example in
   topology of how extra dimensions resolve apparent paradoxes of three-dimensional intuition.</p>`,
      refs:['klein', 'stillwell', 'weeks'] },

    { id:'boys-surface', name:"Boy's surface", tag:'Immersed RP²', schlafli:'RP²', family:'curve', dim:4,
      gen:()=>boysSurface(22,22), rot:[P(0,3,.3),P(1,3,.22)],
      facts:{Dimension:'2D surface', Models:'the real projective plane', Discovered:'Werner Boy, 1901'},
      blurb:'An immersion of the projective plane with no edges or creases.',
      article:`<p><b>Boy's surface</b> is a smooth immersion of the real projective plane RP² in
   three-dimensional space, found by Werner Boy in 1901 at David Hilbert's suggestion. Hilbert had
   expected RP² could not be immersed without singular points; Boy proved otherwise.</p>
   <p>It has threefold symmetry and a single triple point. Robert Bryant and Rob Kusner later gave an
   exact analytic parametrization. RP² embeds without self-intersection only in four dimensions or
   higher.</p>`,
      refs:['boy', 'stillwell'] },

    { id:'duocylinder', name:'Duocylinder', tag:'Curved 4-solid', schlafli:'D²×D²', family:'curve', dim:4,
      gen:()=>duocylinder(20,20), rot:[P(0,1,.4),P(2,3,.4)],
      facts:{Dimension:'4D solid', Surface:'two perpendicular tori', Bounded_by:'a flat ridge'},
      blurb:'The product of two disks — a 4D analogue of the cylinder.',
      article:`<p>The <b>duocylinder</b> is the Cartesian product of two flat disks, each in its own pair of
   dimensions. Its boundary is made of two curved cells, joined along a shared "ridge" that is itself a
   Clifford torus — a flat 2D surface where the two tubes meet at a right angle.</p>
   <p>A ball rolling in a duocylinder could spin smoothly in two completely independent circular
   directions at once, a freedom of motion that has no parallel in three dimensions.</p>`,
      refs:['manning', 'banchoff'] },

    { id:'hopf-fibration', name:'Hopf fibration', tag:'Fibered 3-sphere', schlafli:'S³→S²', family:'curve', dim:4,
      gen:()=>hopfFibration(18,36), rot:[P(0,2,.36),P(1,3,.28)],
      facts:{Maps:'S³ onto S²', Fibers:'linked great circles', Discovered:'Heinz Hopf, 1931'},
      blurb:'A way to fill the 3-sphere with linked circles.',
      article:`<p>The <b>Hopf fibration</b>, discovered by Heinz Hopf in 1931, decomposes the 3-sphere into
   a continuous family of circles, one for each point of an ordinary 2-sphere. Any two of these circles
   are linked exactly once — a configuration that cannot be undone.</p>
   <p>It was the first example of a topologically nontrivial fiber bundle and revolutionized topology.
   The Hopf map appears throughout physics, from the quantum states of a single qubit to magnetic
   monopoles and the structure of liquid crystals.</p>`,
      refs:['hopf', 'banchoff'] },

    { id:'villarceau-circles', name:'Villarceau circles', tag:'Circles on a torus', schlafli:'⊂ T²', family:'curve', dim:4,
      gen:()=>villarceau(14,40), rot:[P(0,3,.32),P(1,2,.24)],
      facts:{Lie_on:'an ordinary torus', Count:'a third family of circles', Found:'Yvon Villarceau, 1848'},
      blurb:'The hidden slanted circles inside every torus.',
      article:`<p>Beyond the obvious circles running around a torus the long way and the short way, there are
   two more families of perfect circles hidden inside it — the <b>Villarceau circles</b>, found by
   French astronomer Yvon Villarceau in 1848. Each is obtained by slicing the torus with a cleverly
   tilted plane that grazes it on both sides.</p>
   <p>Every point of a torus lies on exactly four circles: the two obvious ones and two Villarceau
   circles. They are a favorite motif in architecture and in the geometry of the Hopf fibration.</p>`,
      refs:['villarceau', 'banchoff'] },

    { id:'rp3-projective-3-space', name:'RP³ (projective 3-space)', tag:'Closed 3-manifold', schlafli:'RP³', family:'curve', dim:4,
      gen:()=>rp3(7,34), rot:[P(0,3,.34),P(1,2,.22)],
      facts:{Dimension:'3-manifold', Same_as:'SO(3), the rotation group', Model:'antipodal 3-sphere'},
      blurb:'The space of all 3D rotations — a sphere with antipodes glued.',
      article:`<p><b>Real projective 3-space</b> (RP³) is the 3-sphere with every pair of antipodal points
   identified. Remarkably, it is identical to SO(3), the space of all rotations of ordinary
   three-dimensional space — which is why a rotating object's orientations live on this manifold.</p>
   <p>Its nontrivial topology is the reason for the famous "plate trick" or "belt trick": a 360° rotation
   leaves a twist, while a 720° rotation can be undone. This is the geometric root of spin-½ in quantum
   mechanics.</p>`,
      refs:['weeks', 'stillwell'] },

    { id:'pseudosphere', name:'Pseudosphere', tag:'Surface of constant curvature', schlafli:'K = −1', family:'curve', dim:3,
      gen:()=>pseudosphere(22,22), rot:[P(0,3,.3),P(1,2,.22)],
      facts:{Curvature:'negative, constant', Models:'tractrix surface', Dimension:'2D surface'},
      blurb:'A surface with the same negative curvature everywhere.',
      article:`<p>The <b>pseudosphere</b> is a surface of constant negative curvature, the opposite of a
   sphere. It is formed by revolving a tractrix — the curve chased by a dog on a leash — around its axis.</p>
   <p>Locally it is a model of hyperbolic geometry, and it was one of the first surfaces whose
   geometry challenged Euclidean intuition.</p>`,
      refs:['stillwell', 'weeks'] },

    { id:'dini-surface', name:'Dini surface', tag:'Constant negative curvature', schlafli:'K = −1', family:'curve', dim:3,
      gen:()=>diniSurface(22,22), rot:[P(0,3,.28),P(1,2,.2)],
      facts:{Curvature:'negative, constant', Feature:'infinite spiral horn', Dimension:'2D surface'},
      blurb:'A twisting horn of constant negative curvature.',
      article:`<p>The <b>Dini surface</b> is another surface of constant negative curvature, shaped like an
   infinite twisting horn. Like the pseudosphere, it is built from the tractrix, but it spirals endlessly
   rather than closing into a finite shape.</p>`,
      refs:['stillwell', 'weeks'] },
    // ===== ROW 6 — Duoprisms (products of two polygons) ==========================
    { id:'3x3-duoprism', name:'{3}×{3} duoprism', tag:'Duoprism', schlafli:'{3}×{3}', family:'duo', dim:4,
      gen:()=>duoprism(3,3), rot:[P(0,2,.4),P(1,3,.4)],
      facts:{Dimension:'4D', Vertices:9, Cells:'6 triangular prisms', Symmetry:'[3,2,3]'},
      blurb:'The product of two triangles in 4-space.',
      article:`<p>The <b>{3}×{3} duoprism</b> (triangular duoprism) is the Cartesian product of two
   triangles, each spinning in its own pair of dimensions. It has nine vertices and six triangular-prism
   cells, three from each triangle.</p>
   <p>Duoprisms are the simplest genuinely four-dimensional figures that are neither hypercubes nor
   simplices — products of two lower polytopes that need all four axes to exist.</p>`,
      refs:['coxeter', 'manning'] },

    { id:'4x3-duoprism', name:'{4}×{3} duoprism', tag:'Duoprism', schlafli:'{4}×{3}', family:'duo', dim:4,
      gen:()=>duoprism(4,3), rot:[P(0,2,.4),P(1,3,.36)],
      facts:{Dimension:'4D', Vertices:12, Cells:'4 tri-prisms + 3 cubes', Symmetry:'[4,2,3]'},
      blurb:'A square times a triangle.',
      article:`<p>The <b>{4}×{3} duoprism</b> multiplies a square by a triangle. The square contributes three
   cubic cells, the triangle contributes four triangular prisms, for twelve vertices arranged on a flat
   torus in 4-space.</p>
   <p>Each duoprism's vertices lie on a Clifford torus — the same flat surface in S³ that appears
   throughout this row of the gallery — making them the polygonal skeletons of that torus.</p>`,
      refs:['coxeter', 'manning'] },

    { id:'5x3-duoprism', name:'{5}×{3} duoprism', tag:'Duoprism', schlafli:'{5}×{3}', family:'duo', dim:4,
      gen:()=>duoprism(5,3), rot:[P(0,2,.38),P(1,3,.34)],
      facts:{Dimension:'4D', Vertices:15, Cells:'5 tri-prisms + 3 pentagonal prisms', Symmetry:'[5,2,3]'},
      blurb:'A pentagon times a triangle.',
      article:`<p>The <b>{5}×{3} duoprism</b> is the product of a pentagon and a triangle: five triangular
   prisms and three pentagonal prisms enclose its fifteen vertices. Mixing a 5-fold with a 3-fold
   symmetry gives it a pleasingly irregular look as it rotates in two planes at once.</p>
   <p>The general {p}×{q} duoprism has p·q vertices and p+q prism cells, a clean formula that scales to
   any pair of polygons.</p>`,
      refs:['coxeter', 'manning'] },

    { id:'4x4-duoprism', name:'{4}×{4} duoprism', tag:'Duoprism', schlafli:'{4}×{4}', family:'duo', dim:4,
      gen:()=>duoprism(4,4), rot:[P(0,2,.4),P(1,3,.4)],
      facts:{Dimension:'4D', Vertices:16, Cells:'8 cubes', Same_as:'the tesseract'},
      blurb:'A square times a square — secretly the tesseract.',
      article:`<p>The <b>{4}×{4} duoprism</b> is the product of two squares — and that product is exactly the
   <i>tesseract</i>. Its sixteen vertices and eight cubic cells are the 4-cube seen through the lens of
   its two independent square cross-sections.</p>
   <p>This identity — that the hypercube is a duoprism of two squares — is the four-dimensional echo of
   the fact that a cube is a square prism, a "{4}×{ }" product.</p>`,
      refs:['coxeter', 'manning'] },

    { id:'5x5-duoprism', name:'{5}×{5} duoprism', tag:'Duoprism', schlafli:'{5}×{5}', family:'duo', dim:4,
      gen:()=>duoprism(5,5), rot:[P(0,2,.38),P(1,3,.38)],
      facts:{Dimension:'4D', Vertices:25, Cells:'10 pentagonal prisms', Symmetry:'[5,2,5]'},
      blurb:'Two pentagons multiplied together.',
      article:`<p>The <b>{5}×{5} duoprism</b> multiplies two pentagons, giving 25 vertices and ten
   pentagonal-prism cells in a doubly five-fold symmetric figure. Because both factors share the same
   symmetry, it has an extra "duoprismatic" symmetry that swaps the two pentagons.</p>
   <p>Equal-factor duoprisms like this are the most symmetric of their family and tile naturally onto the
   Clifford torus in equal squares.</p>`,
      refs:['coxeter', 'manning'] },

    { id:'6x6-duoprism', name:'{6}×{6} duoprism', tag:'Duoprism', schlafli:'{6}×{6}', family:'duo', dim:4,
      gen:()=>duoprism(6,6), rot:[P(0,2,.36),P(1,3,.36)],
      facts:{Dimension:'4D', Vertices:36, Cells:'12 hexagonal prisms', Symmetry:'[6,2,6]'},
      blurb:'Two hexagons multiplied together.',
      article:`<p>The <b>{6}×{6} duoprism</b> pairs two hexagons into a 36-vertex figure bounded by twelve
   hexagonal prisms. As the polygon order rises, the duoprism's projected shadow approaches the smooth
   surface of a <i>duocylinder</i> — the product of two disks.</p>
   <p>This limiting relationship makes duoprisms the natural polygonal approximations to the curved 4D
   solids elsewhere in the hall.</p>`,
      refs:['coxeter', 'manning'] },

    { id:'8x8-duoprism', name:'{8}×{8} duoprism', tag:'Duoprism', schlafli:'{8}×{8}', family:'duo', dim:4,
      gen:()=>duoprism(8,8), rot:[P(0,2,.34),P(1,3,.34)],
      facts:{Dimension:'4D', Vertices:64, Cells:'16 octagonal prisms', Symmetry:'[8,2,8]'},
      blurb:'Two octagons multiplied together.',
      article:`<p>The <b>{8}×{8} duoprism</b> has 64 vertices arranged from two octagons — coincidentally the
   same vertex count as the 6-cube, though arranged on a torus rather than a cube. Sixteen octagonal
   prisms form its boundary.</p>
   <p>Watch how its rotation, with two independent angular speeds, produces a hypnotic interference
   pattern: this is what a genuine double rotation looks like flattened to a screen.</p>`,
      refs:['coxeter', 'manning'] },

    { id:'10x10-duoprism', name:'{10}×{10} duoprism', tag:'Duoprism', schlafli:'{10}×{10}', family:'duo', dim:4,
      gen:()=>duoprism(10,10), rot:[P(0,2,.32),P(1,3,.32)],
      facts:{Dimension:'4D', Vertices:100, Cells:'20 decagonal prisms', Approaches:'the duocylinder'},
      blurb:'Two decagons — nearly a smooth duocylinder.',
      article:`<p>The <b>{10}×{10} duoprism</b> brings two ten-sided polygons together into a 100-vertex
   figure. With this many sides, its silhouette is almost indistinguishable from the smooth duocylinder,
   the product of two perfect disks.</p>
   <p>The progression {3}×{3} → {4}×{4} → … → {∞}×{∞} is the four-dimensional analogue of how regular
   polygons converge to a circle — here, two circles at once.</p>`,
      refs:['coxeter', 'manning'] },

    { id:'3x4-duoprism', name:'{3}×{4} duoprism', tag:'Duoprism', schlafli:'{3}×{4}', family:'duo', dim:4,
      gen:()=>duoprism(3,4), rot:[P(0,2,.4),P(1,3,.36)],
      facts:{Dimension:'4D', Vertices:12, Cells:'3 cubes + 4 tri-prisms', Symmetry:'[3,2,4]'},
      blurb:'A triangle times a square.',
      article:`<p>The <b>{3}×{4} duoprism</b> multiplies a triangle by a square. Its twelve vertices sit on a
   flat Clifford torus in 4-space, and its boundary is three cubes and four triangular prisms.</p>`,
      refs:['coxeter', 'manning'] },

    { id:'7x7-duoprism', name:'{7}×{7} duoprism', tag:'Duoprism', schlafli:'{7}×{7}', family:'duo', dim:4,
      gen:()=>duoprism(7,7), rot:[P(0,2,.34),P(1,3,.34)],
      facts:{Dimension:'4D', Vertices:49, Cells:'14 heptagonal prisms', Symmetry:'[7,2,7]'},
      blurb:'Two heptagons multiplied together.',
      article:`<p>The <b>{7}×{7} duoprism</b> pairs two heptagons into a 49-vertex figure bounded by fourteen
   heptagonal prisms. Its seven-fold symmetry gives the rotation a steady, seven-beat rhythm.</p>`,
      refs:['coxeter', 'manning'] },
    // ===== ROW 7 — Rotations and the topology of motion in 4D ====================
    { id:'left-isoclinic-rotation', name:'Left isoclinic rotation', tag:'Double rotation', schlafli:'SO(4) left', family:'rot', dim:4,
      gen:rotTesseract, rot:[P(0,1,.4,0),P(2,3,.4,0)],
      facts:{Type:'isoclinic (equal-angle)', Planes:'two, rotating equally', Handedness:'left'},
      blurb:'A 4D rotation that turns two planes at the same rate.',
      article:`<p>In four dimensions a rotation can spin in <i>two</i> independent planes at once. When both
   planes turn through the same angle, the motion is called <b>isoclinic</b>. A left-isoclinic rotation
   is one of two handed varieties, corresponding to multiplication by a unit quaternion on the left.</p>
   <p>Every point of the tesseract here moves along a great circle of the same radius, so the whole
   figure appears to tumble rigidly — a motion with no fixed axis at all, unlike any 3D rotation.</p>`,
      refs:['manning', 'coxeter'] },

    { id:'right-isoclinic-rotation', name:'Right isoclinic rotation', tag:'Double rotation', schlafli:'SO(4) right', family:'rot', dim:4,
      gen:rotTesseract, rot:[P(0,1,.4,0),P(2,3,-.4,0)],
      facts:{Type:'isoclinic (equal-angle)', Planes:'two, opposite sense', Handedness:'right'},
      blurb:'The mirror-handed partner of the left isoclinic rotation.',
      article:`<p>The <b>right isoclinic rotation</b> also turns two planes equally, but in the opposite
   relative sense — corresponding to quaternion multiplication on the right. Together, left and right
   isoclinic rotations generate all of SO(4), the group of 4D rotations.</p>
   <p>This left/right decomposition is special to four dimensions and is the geometric origin of the two
   chiralities of spinors in physics. No other rotation group splits so cleanly.</p>`,
      refs:['manning', 'coxeter'] },

    { id:'clifford-parallels', name:'Clifford parallels', tag:'Parallel great circles', schlafli:'S³', family:'rot', dim:4,
      gen:()=>hopfFibration(20,30), rot:[P(0,2,.4),P(1,3,.4)],
      facts:{Live_on:'the 3-sphere', Property:'equidistant everywhere', Named_for:'W. K. Clifford'},
      blurb:'Lines that stay the same distance apart — but are linked.',
      article:`<p><b>Clifford parallels</b> are great circles on the 3-sphere that remain exactly
   equidistant from one another everywhere — yet, unlike parallel lines in flat space, they are
   <i>linked</i> like rings of a chain. William Kingdon Clifford described them in 1873.</p>
   <p>They are precisely the fibers of the Hopf fibration, and an isoclinic rotation slides each circle
   along itself, carrying the whole family rigidly. They are the truest 4D analogue of parallel lines.</p>`,
      refs:['clifford', 'hopf'] },

    { id:'quaternion-rotation', name:'Quaternion rotation', tag:'Unit quaternions', schlafli:'S³≅SU(2)', family:'rot', dim:4,
      gen:()=>glome(8,40), rot:[P(0,3,.4),P(1,2,.4)],
      facts:{Unit_quaternions:'form a 3-sphere', Double_cover:'of SO(3)', Used_in:'graphics, robotics'},
      blurb:'How 4D unit quaternions encode every 3D rotation.',
      article:`<p>The unit <b>quaternions</b> form a 3-sphere in four-dimensional space, and each one encodes
   a rotation of ordinary 3D space. This is why quaternions are the standard tool for orientation in
   computer graphics, spacecraft attitude control, and robotics — they avoid gimbal lock and interpolate
   smoothly.</p>
   <p>Two antipodal quaternions give the same 3D rotation, so the quaternion sphere is a <i>double cover</i>
   of the rotation group SO(3). That two-to-one relationship is again the source of spin-½.</p>`,
      refs:['clifford', 'stillwell'] },

    { id:'hopf-link', name:'Hopf link', tag:'Linked circles', schlafli:'2-component link', family:'rot', dim:4,
      gen:()=>hopfFibration(2,48), rot:[P(0,2,.36),P(1,3,.28)],
      facts:{Components:'2 circles', Linking_number:1, Simplest:'nontrivial link'},
      blurb:'The two simplest linked circles — a single Hopf pair.',
      article:`<p>The <b>Hopf link</b> is the simplest nontrivial link: two circles passing through one
   another exactly once, with linking number one. It is what you get by taking any two fibers of the
   Hopf fibration.</p>
   <p>You cannot pull the two circles apart without cutting one, yet neither is knotted on its own. The
   Hopf link is foundational in knot theory and appears as the Borromean-ring's simpler cousin.</p>`,
      refs:['hopf', 'stillwell'] },

    { id:'trefoil-in-4d', name:'Trefoil in 4D', tag:'Unknotting in 4D', schlafli:'3₁ knot', family:'rot', dim:4,
      gen:()=>villarceau(3,48), rot:[P(0,3,.34),P(1,2,.26)],
      facts:{Knot:'trefoil (3 crossings)', In_3D:'cannot be undone', In_4D:'every knot unties'},
      blurb:'The simplest knot — and why 4D unties all knots.',
      article:`<p>The <b>trefoil</b> is the simplest nontrivial knot, with three crossings, and in three
   dimensions it can never be untied. But in four dimensions there is room to lift one strand "over" any
   crossing through the extra axis — so <i>every</i> knotted loop of string unties freely.</p>
   <p>This is why knots are a strictly three-dimensional phenomenon. In 4D the interesting objects are
   knotted <i>surfaces</i> (knotted spheres), not knotted loops.</p>`,
      refs:['stillwell', 'banchoff'] },

    { id:'double-rotation', name:'Double rotation', tag:'Generic SO(4) motion', schlafli:'two unequal angles', family:'rot', dim:4,
      gen:rotTesseract, rot:[P(0,1,.5),P(2,3,.22)],
      facts:{Planes:'two invariant planes', Angles:'generally unequal', Fixed_points:'only the centre'},
      blurb:'The most general rotation possible in four dimensions.',
      article:`<p>A generic <b>double rotation</b> in 4D spins through two perpendicular planes at two
   <i>different</i> rates. Only the single centre point stays fixed — there is no rotation axis at all,
   unlike every rotation in three dimensions, which always fixes a whole line.</p>
   <p>When the two rates happen to be equal you recover an isoclinic rotation; when one is zero you get a
   simple rotation. The double rotation is the full, generic case, and the tesseract here shows its
   characteristic two-speed tumble.</p>`,
      refs:['manning', 'coxeter'] },

    { id:'screw-motion-in-4d', name:'Screw motion in 4D', tag:'Rotation + translation', schlafli:'isometry of E⁴', family:'rot', dim:4,
      gen:()=>duoprism(6,4), rot:[P(0,1,.4),P(2,3,.24)],
      facts:{Combines:'rotation with translation', Lives_in:'flat 4-space', Generalizes:'the 3D screw'},
      blurb:'A spinning advance — the 4D version of a screw.',
      article:`<p>A <b>screw motion</b> couples rotation with translation along the rotation's axis. In four
   dimensions, where rotations act in two independent planes, a screw motion can twist in both planes
   while advancing — a far richer family of rigid motions than three dimensions allows.</p>
   <p>Chasles' theorem says every 3D rigid motion is a screw; its 4D generalization classifies the
   isometries of flat four-space, the backbone of crystallography in higher dimensions.</p>`,
      refs:['manning', 'coxeter'] },

    { id:'4d-rotation-group', name:'4D rotation group', tag:'SO(4) demo', schlafli:'SO(4)', family:'rot', dim:4,
      gen:rotTesseract, rot:[P(0,2,.4),P(1,3,.3),P(2,3,.2)],
      facts:{Group:'SO(4)', Decomposition:'left × right isoclinic', Dimension:'6D manifold'},
      blurb:'The six-dimensional space of all 4D rotations.',
      article:`<p>The rotation group <b>SO(4)</b> is six-dimensional: every 4D rotation is a combination of a
   left-isoclinic and a right-isoclinic rotation. This double decomposition is unique to four dimensions
   and underlies the two chiralities of spinors.</p>`,
      refs:['manning', 'coxeter'] },

    { id:'isoclinic-decomposition', name:'Isoclinic decomposition', tag:'SO(4) = S³×S³', schlafli:'left ⊕ right', family:'rot', dim:4,
      gen:rotTesseract, rot:[P(0,1,.35),P(2,3,.35),P(0,3,.2)],
      facts:{Planes:'two independent pairs', Result:'generic SO(4) motion', Decomposition:'isoclinic'},
      blurb:'A 4D rotation split into two paired-plane twists.',
      article:`<p>Any 4D rotation can be decomposed into two isoclinic rotations acting on separate pairs of
   axes. The tesseract here rotates in the (x,y) and (z,w) planes at once, illustrating how SO(4) is
   built from two 3-spheres of rotations.</p>`,
      refs:['manning', 'coxeter'] },
    // ===== ROW 8 — Spacetime, exotic geometry, and great lattices ================
    { id:'minkowski-spacetime', name:'Minkowski spacetime', tag:'Spacetime geometry', schlafli:'R^{1,3}', family:'phys', dim:4,
      gen:()=>lightCone(9,30), rot:[P(0,3,.3),P(1,2,.2)],
      facts:{Signature:'(−,+,+,+)', Defines:'the light cone', Author:'Hermann Minkowski, 1908'},
      blurb:'The four-dimensional stage of special relativity.',
      article:`<p><b>Minkowski spacetime</b> is the geometric arena of special relativity: three space
   dimensions and one time dimension, fused into a single four-dimensional continuum. Its "distance"
   uses a minus sign for time, which is why the geometry is hyperbolic rather than Euclidean.</p>
   <p>The cone you see is the <b>light cone</b> — the set of all light rays through an event. It divides
   spacetime into the causal past, the causal future, and the "elsewhere" no signal can reach. Hermann
   Minkowski unveiled this picture in 1908, giving Einstein's 1905 theory its geometric form.</p>`,
      refs:['minkowski', 'banchoff'] },

    { id:'de-sitter-space', name:'de Sitter space', tag:'Curved spacetime', schlafli:'dS₄', family:'phys', dim:4,
      gen:()=>deSitter(9,30), rot:[P(0,3,.28),P(1,2,.18)],
      facts:{Curvature:'positive, constant', Models:'an expanding universe', Author:'Willem de Sitter, 1917'},
      blurb:'A spacetime of constant positive curvature — an empty, expanding cosmos.',
      article:`<p><b>de Sitter space</b> is the maximally symmetric solution of Einstein's equations with a
   positive cosmological constant and no matter — a model empty universe that expands exponentially. It
   is a four-dimensional hyperboloid embedded in five-dimensional flat space.</p>
   <p>Willem de Sitter found it in 1917 in correspondence with Einstein. It is the best simple model for
   our universe's accelerating expansion, driven by dark energy, and for the inflationary epoch just
   after the Big Bang.</p>`,
      refs:['desitter', 'minkowski'] },

    { id:'anti-de-sitter-space', name:'Anti-de Sitter space', tag:'Curved spacetime', schlafli:'AdS', family:'phys', dim:4,
      gen:()=>antiDeSitter(9,30), rot:[P(0,3,.26),P(1,2,.18)],
      facts:{Curvature:'negative, constant', Famous_for:'the AdS/CFT correspondence', Year:1998},
      blurb:'Negatively curved spacetime — home of the holographic principle.',
      article:`<p><b>Anti-de Sitter space</b> is the constant-negative-curvature counterpart of de Sitter
   space — a saddle-shaped spacetime that, surprisingly, acts like a box: light can reach its boundary
   and return in finite time.</p>
   <p>It became central to physics in 1998 when Juan Maldacena conjectured the <b>AdS/CFT
   correspondence</b>, a duality stating that gravity in an anti-de Sitter interior is exactly equivalent
   to a quantum field theory living on its lower-dimensional boundary — the sharpest known realization of
   the holographic principle.</p>`,
      refs:['maldacena', 'desitter'] },

    { id:'calabi-yau-manifold', name:'Calabi–Yau manifold', tag:'String compactification', schlafli:'CY₃', family:'phys', dim:6,
      gen:()=>calabiYau(5,8,12), rot:[P(0,3,.3),P(1,2,.22),P(2,3,.14)],
      facts:{Real_dimension:6, Curvature:'Ricci-flat, Kähler', Role:'hides 6 of string theory\'s dimensions'},
      blurb:'The curled-up extra dimensions of string theory.',
      article:`<p>A <b>Calabi–Yau manifold</b> is a compact, Ricci-flat Kähler manifold. Eugenio Calabi
   conjectured their existence in the 1950s and Shing-Tung Yau proved it in 1978, winning the Fields
   Medal in part for this result.</p>
   <p>In string theory, the six dimensions beyond our familiar four are thought to be curled up into a
   tiny Calabi–Yau threefold at every point of spacetime. Its precise shape would determine the particle
   spectrum of our universe. The figure shows a 2D slice of the Fermat quintic, the standard visualization.</p>`,
      refs:['yau', 'candelas'] },

    { id:'penrose-quasicrystal', name:'Penrose quasicrystal', tag:'Aperiodic order', schlafli:'5D→2D cut', family:'phys', dim:5,
      gen:()=>penrose(3), rot:[P(0,4,.3),P(1,3,.2),P(2,4,.14)],
      facts:{Symmetry:'5-fold (forbidden for crystals)', Origin:'projection from 5D', Nobel:'Shechtman, 2011'},
      blurb:'A pattern with five-fold symmetry that never repeats.',
      article:`<p>A <b>quasicrystal</b> has long-range order but no repeating unit cell, and can display
   five-fold symmetry — impossible for an ordinary periodic crystal. Roger Penrose's famous aperiodic
   tiling is the two-dimensional model, and it arises naturally as the shadow of a periodic lattice in
   <i>five</i> dimensions projected down to the plane.</p>
   <p>Dan Shechtman discovered real quasicrystalline alloys in 1982, a finding so heretical it was first
   ridiculed; he received the 2011 Nobel Prize in Chemistry. The 5D point cloud here casts just such a
   quasiperiodic shadow.</p>`,
      refs:['penrose', 'shechtman'] },

    { id:'e8-root-system', name:'E₈ root system', tag:'Exceptional Lie algebra', schlafli:'240 roots', family:'phys', dim:8,
      gen:e8Roots, rot:[P(0,7,.24),P(1,6,.18),P(2,5,.12),P(3,4,.08)],
      facts:{Roots:240, Dimension:8, Symmetry_order:'696,729,600'},
      blurb:'The 240 roots of the largest exceptional symmetry.',
      article:`<p>The <b>E₈ root system</b> consists of 240 vectors in eight-dimensional space — the roots
   of the largest exceptional simple Lie algebra. They split into 112 integer roots (±eᵢ±eⱼ) and 128
   half-integer roots, all of equal length, and they generate the densest lattice packing in 8D.</p>
   <p>E₈ is one of the deepest structures in mathematics, surfacing in string theory's heterotic gauge
   group, in the 2007 computer-assisted mapping of its 453,060-dimensional character table, and in the
   measured excitation spectrum of a quantum magnetic chain.</p>`,
      refs:['gosset', 'conway', 'coxeter'] },

    { id:'leech-lattice', name:'Leech lattice', tag:'24D sphere packing', schlafli:'Λ₂₄', family:'phys', dim:8,
      gen:leech, rot:[P(0,7,.22),P(1,6,.16),P(2,5,.1)],
      facts:{Dimension:24, Kissing_number:'196,560', Discovered:'John Leech, 1967'},
      blurb:'The extraordinary 24-dimensional sphere packing.',
      article:`<p>The <b>Leech lattice</b> Λ₂₄ is a remarkable arrangement of points in twenty-four
   dimensions in which each sphere touches 196,560 others — and in 2016 Maryna Viazovska's methods
   confirmed it gives the densest possible sphere packing in 24D.</p>
   <p>Its symmetry group is the Conway group Co₀, with deep ties to the Monster group and "monstrous
   moonshine." Discovered by John Leech in 1967, it has no vectors shorter than length 2 and underlies
   some of the best error-correcting codes known. The frame shown is an 8D representative shadow.</p>`,
      refs:['leech', 'conway'] },

    { id:'octonion-structure', name:'Octonion structure', tag:'Eight-dimensional algebra', schlafli:'𝕆', family:'phys', dim:8,
      gen:octonions, rot:[P(0,4,.3),P(1,5,.22),P(2,6,.14)],
      facts:{Dimension:8, Property:'non-associative', Encoded_by:'the Fano plane'},
      blurb:'The largest normed division algebra — multiplication you can map on the Fano plane.',
      article:`<p>The <b>octonions</b> 𝕆 are the largest of the four normed division algebras (after the
   reals, complex numbers, and quaternions), with eight dimensions. They are <i>non-associative</i>:
   (ab)c need not equal a(bc), the price paid for going one step beyond the quaternions.</p>
   <p>Their multiplication rule is encoded by the <b>Fano plane</b> — seven points and seven lines, drawn
   here as a triangular frame. Octonions are tied to the exceptional Lie groups, to E₈, and to
   speculative descriptions of the three generations of fundamental particles.</p>`,
      refs:['baez', 'conway'] },

    { id:'d4-lattice', name:'D4 lattice', tag:'Sphere packing', schlafli:'D₄', family:'phys', dim:4,
      gen:d4Lattice, rot:[P(0,3,.34),P(1,2,.26),P(2,3,.18)],
      facts:{Minimal_vectors:24, Kissing_number:24, Densest:'in 4D'},
      blurb:'The densest lattice sphere packing in four dimensions.',
      article:`<p>The <b>D₄ lattice</b> is the densest lattice packing of spheres in four dimensions. Its
   24 minimal vectors are exactly the vertices of the 24-cell, and each sphere touches 24 others.</p>
   <p>D₄ appears in the theory of error-correcting codes, in the algebra of quaternions, and as the root
   lattice of the Lie group Spin(8).</p>`,
      refs:['conway', 'coxeter'] },

    { id:'d5-lattice', name:'D5 lattice', tag:'Sphere packing', schlafli:'D₅', family:'phys', dim:5,
      gen:d5Lattice, rot:[P(0,4,.32),P(1,3,.24),P(2,4,.16)],
      facts:{Minimal_vectors:40, Kissing_number:40, Dimension:5},
      blurb:'The five-dimensional D lattice — cousin of the 5-demicube.',
      article:`<p>The <b>D₅ lattice</b> is the five-dimensional member of the Dₙ family. Its 40 minimal
   vectors are the permutations of (±1,±1,0,0,0), and it is the root lattice of the Lie group Spin(10),
   important in grand-unified theories of particle physics.</p>`,
      refs:['conway', 'coxeter'] },
    // ===== ROW 9 — More 5D shapes (prisms, products, and the 4-sphere) ===========
    { id:'5-cell-prism', name:'5-cell prism', tag:'5D prism', schlafli:'{3,3,3}×{ }', family:'prism', dim:5,
      gen:()=>prism5D(cell5), rot:[P(0,4,.4),P(1,3,.3),P(2,4,.2)],
      facts:{Vertices:10, Edges:25, Cells:'2 5-cells + 5 tetra-prisms', Dimension:'5D'},
      blurb:'A 5-cell stretched into the fifth dimension.',
      article:`<p>The <b>5-cell prism</b> is the Cartesian product of the 5-cell with a line segment. It has
   ten vertices — two copies of the 5-cell's five — linked by five vertical edges and two copies of the
   original 5-cell edge skeleton. Its boundary is two 5-cell facets plus five tetrahedral prisms.</p>
   <p>Prisms are the simplest way to lift a lower-dimensional polytope into a higher one: extrude it along
   a new axis. The result is never regular, but it is always uniform when the base is uniform.</p>`,
      refs:['coxeter', 'manning'] },

    { id:'16-cell-prism', name:'16-cell prism', tag:'5D prism', schlafli:'{3,3,4}×{ }', family:'prism', dim:5,
      gen:()=>prism5D(cell16), rot:[P(0,4,.42),P(1,3,.3)],
      facts:{Vertices:16, Edges:56, Cells:'2 16-cells + 16 tetra-prisms', Dimension:'5D'},
      blurb:'The 4D cross-polytope extruded into 5-space.',
      article:`<p>The <b>16-cell prism</b> extrudes the 16-cell along a fifth axis. Its sixteen vertices form
   two parallel 16-cells, joined by sixteen vertical edges. The side walls are sixteen tetrahedral prisms,
   one for each tetrahedral cell of the base.</p>
   <p>Although it has the same vertex count as the tesseract, its structure is different: it is the prism
   over the cross-polytope rather than the 4-cube.</p>`,
      refs:['coxeter', 'manning'] },

    { id:'24-cell-prism', name:'24-cell prism', tag:'5D prism', schlafli:'{3,4,3}×{ }', family:'prism', dim:5,
      gen:()=>prism5D(cell24), rot:[P(0,4,.38),P(1,3,.28),P(2,4,.18)],
      facts:{Vertices:48, Edges:216, Cells:'2 24-cells + 24 octa-prisms', Dimension:'5D'},
      blurb:'The unique self-dual 24-cell, lifted into five dimensions.',
      article:`<p>The <b>24-cell prism</b> takes the only self-dual regular polychoron and extrudes it into
   5-space. Its 48 vertices are two copies of the 24-cell's 24, and its boundary includes two 24-cell
   facets plus 24 octahedral prisms.</p>
   <p>Because the 24-cell itself has no 3D or higher-dimensional analogue, its prism is one of the few
   five-dimensional figures that inherits genuinely four-dimensional symmetry.</p>`,
      refs:['coxeter', 'conway'] },

    { id:'tesseract-prism', name:'Tesseract prism', tag:'5D prism', schlafli:'{4,3,3}×{ }', family:'prism', dim:5,
      gen:()=>prism5D(tesseract), rot:[P(0,4,.4),P(1,3,.28),P(2,4,.18)],
      facts:{Vertices:32, Edges:80, Cells:'2 tesseracts + 8 cubic prisms', Dimension:'5D'},
      blurb:'A tesseract stretched along a fifth axis — not the same as a penteract.',
      article:`<p>The <b>tesseract prism</b> is the product of a tesseract and a line segment. It has 32
   vertices and 80 edges, the same counts as the penteract, but a different cell layout: two tesseract
   facets (the ends) and eight cubic prisms (the walls).</p>
   <p>The penteract, by contrast, has ten tesseract facets and no cubic prisms. The two shapes are an
   instructive pair: same small-scale counts, different five-dimensional structure.</p>`,
      refs:['coxeter', 'hinton'] },

    { id:'600-cell-prism', name:'600-cell prism', tag:'5D prism', schlafli:'{3,3,5}×{ }', family:'prism', dim:5,
      gen:()=>prism5D(cell600), rot:[P(0,4,.3),P(1,3,.22),P(2,4,.14)],
      facts:{Vertices:240, Edges:1560, Cells:'2 600-cells + 600 tetra-prisms', Dimension:'5D'},
      blurb:'The icosahedral 600-cell, extruded into a fifth dimension.',
      article:`<p>The <b>600-cell prism</b> is the five-dimensional prism over the 600-cell. Its 240 vertices
   are two copies of the 600-cell's 120, and its 1,560 edges include two copies of the original 600-cell
   skeleton plus 120 vertical edges.</p>
   <p>The 600-cell already has the largest finite reflection symmetry in four dimensions (H₄); its prism
   inherits that symmetry and extends it with a mirror across the new fifth axis.</p>`,
      refs:['coxeter', 'conway'] },

    { id:'tetrahedron-triangle-duoprism', name:'Tetrahedron–triangle duoprism', tag:'5D duoprism', schlafli:'{3,3}×{3}', family:'prism', dim:5,
      gen:()=>polyDuoprism(tetrahedron3D(), 3), rot:[P(0,3,.4),P(1,4,.36),P(2,3,.22)],
      facts:{Vertices:12, Edges:30, Cells:'4 tri-duoprisms + 3 tetra-prisms', Dimension:'5D'},
      blurb:'A tetrahedron and a triangle multiplied together in 5-space.',
      article:`<p>The <b>tetrahedron–triangle duoprism</b> is the Cartesian product of a tetrahedron and a
   triangle, a genuinely five-dimensional figure. Its twelve vertices are all pairs (tetrahedron vertex,
   triangle vertex), and its thirty edges come from tetrahedron edges × triangle vertices plus triangle
   edges × tetrahedron vertices.</p>
   <p>It is a simple example of a polyhedral-polygonal duoprism: a 3D polyhedron and a 2D polygon whose
   product needs all five dimensions to exist without distortion.</p>`,
      refs:['coxeter', 'manning'] },

    { id:'4-sphere-s4', name:'4-sphere (S⁴)', tag:'Curved 5-manifold', schlafli:'S⁴', family:'prism', dim:5,
      gen:()=>glome5D(6,24), rot:[P(0,4,.3),P(1,3,.24),P(2,4,.16)],
      facts:{Dimension:'surface in 5D', Curvature:'positive, constant', Symmetry:'O(5)'},
      blurb:'The set of all points equidistant from a centre in 5-space.',
      article:`<p>The <b>4-sphere</b> (S⁴, or the 5D glome) is the set of points at fixed distance from a
   centre in five-dimensional space. Its surface is four-dimensional: a creature on it could move in four
   independent directions and never reach an edge.</p>
   <p>It is the next sphere after the ordinary 2-sphere and the 3-sphere, and it appears in the
   classification of simply connected 4-manifolds and in compactifications of higher-dimensional physics.</p>`,
      refs:['weeks', 'banchoff'] },

    { id:'5d-cyclic-polytope', name:'5D cyclic polytope', tag:'Cyclic 5-polytope', schlafli:'C(14,5)', family:'prism', dim:5,
      gen:()=>cyclic5D(14), rot:[P(0,4,.34),P(1,3,.24),P(2,4,.16)],
      facts:{Vertices:14, Construction:'moment curve', Property:'neighborly'},
      blurb:'Vertices on the moment curve — a combinatorially extreme 5-polytope.',
      article:`<p>A <b>cyclic polytope</b> is built by placing vertices on the moment curve
   (t, t², t³, t⁴, t⁵) in five-dimensional space. It is <i>neighborly</i>: every pair of vertices forms an
   edge, making it as edge-dense as any convex 5-polytope can be.</p>
   <p>Cyclic polytopes are universal extremal objects: McMullen's upper bound theorem says no convex
   polytope with the same number of vertices can have more faces.</p>`,
      refs:['coxeter', 'banchoff'] },

    { id:'5-demicube-prism', name:'5-demicube prism', tag:'5D prism', schlafli:'h{4,3,3,3}×{ }', family:'prism', dim:5,
      gen:()=>prism5D(()=>nDemicube(5)), rot:[P(0,4,.36),P(1,3,.26),P(2,4,.16)],
      facts:{Vertices:32, Edges:'~200', Cells:'2 5-demicubes + prisms', Dimension:'5D'},
      blurb:'The alternated 5-cube, extruded into a sixth coordinate.',
      article:`<p>The <b>5-demicube prism</b> is the prism over the 5-demicube. It takes the sixteen vertices
   of the demipenteract, makes two copies, and joins corresponding vertices with vertical edges. The
   result is a 32-vertex uniform 5-polytope with D₅ symmetry extended by a mirror.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'5-orthoplex-prism', name:'5-orthoplex prism', tag:'5D prism', schlafli:'{3,3,3,4}×{ }', family:'prism', dim:5,
      gen:()=>prism5D(()=>nOrthoplex(5)), rot:[P(0,4,.4),P(1,3,.28)],
      facts:{Vertices:20, Edges:100, Cells:'2 5-orthoplexes + 32 prisms', Dimension:'5D'},
      blurb:'The 5D cross-polytope stretched along a sixth axis.',
      article:`<p>The <b>5-orthoplex prism</b> extrudes the 5-orthoplex along a new axis. Its twenty vertices
   are two copies of the pentacross's ten, and its boundary includes two 5-orthoplex facets plus thirty-two
   simplex prisms.</p>`,
      refs:['coxeter', 'manning'] },
    // ===== ROW 10 — Higher forms, demicubes, and quasicrystals ===================
    { id:'8-simplex', name:'8-simplex', tag:'Regular 8-polytope', schlafli:'{3⁷}', family:'ncube', dim:8,
      gen:()=>nSimplex(8), rot:[P(0,7,.3),P(1,6,.22)],
      facts:{Dimension:'8D', Vertices:9, Edges:36, Symmetry:'A₈'},
      blurb:'Nine mutually equidistant points in eight dimensions.',
      article:`<p>The <b>8-simplex</b> (enneazetton) is nine points in eight dimensions, every pair the same
   distance apart. It is self-dual and the eight-dimensional member of the simplex family.</p>`,
      refs:['coxeter', 'schlafli'] },

    { id:'9-simplex', name:'9-simplex', tag:'Regular 9-polytope', schlafli:'{3⁸}', family:'ncube', dim:9,
      gen:()=>nSimplex(9), rot:[P(0,8,.28),P(1,7,.2)],
      facts:{Dimension:'9D', Vertices:10, Edges:45, Symmetry:'A₉'},
      blurb:'Ten mutually equidistant points in nine dimensions.',
      article:`<p>The <b>9-simplex</b> (decazetton) has ten vertices, 45 edges, and complete symmetry S₁₀.
   Every vertex is connected to every other: its edge graph is the complete graph K₁₀.</p>`,
      refs:['coxeter', 'schlafli'] },

    { id:'6-demicube', name:'6-demicube', tag:'Uniform 6-polytope', schlafli:'h{4,3,3,3,3}', family:'ncube', dim:6,
      gen:()=>nDemicube(6), rot:[P(0,5,.34),P(1,4,.24)],
      facts:{Dimension:'6D', Vertices:32, Symmetry:'D₆', Facets:'mixed'},
      blurb:'The alternated 6-cube — half the vertices of the hexeract.',
      article:`<p>The <b>6-demicube</b> (demihexeract) is the 6-cube with alternate vertices removed, keeping
   the 32 vertices with even coordinate parity. It belongs to the D₆ family of uniform 6-polytopes.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'7-demicube', name:'7-demicube', tag:'Uniform 7-polytope', schlafli:'h{4,3⁵}', family:'ncube', dim:7,
      gen:()=>nDemicube(7), rot:[P(0,6,.32),P(1,5,.22)],
      facts:{Dimension:'7D', Vertices:64, Symmetry:'D₇', Facets:'mixed'},
      blurb:'The alternated 7-cube — half the vertices of the hepteract.',
      article:`<p>The <b>7-demicube</b> (demihepteract) keeps the 64 even-parity vertices of the 7-cube. It is
   a D₇ uniform polytope and a close relative of the E₇ root system.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'8-demicube', name:'8-demicube', tag:'Uniform 8-polytope', schlafli:'h{4,3⁶}', family:'ncube', dim:8,
      gen:()=>nDemicube(8), rot:[P(0,7,.3),P(1,6,.2),P(2,5,.14)],
      facts:{Dimension:'8D', Vertices:128, Symmetry:'D₈', Note:'E₈ lives here'},
      blurb:'The alternated 8-cube — the D₈ root lattice.',
      article:`<p>The <b>8-demicube</b> (demiocteract) has 128 vertices and is the root polytope of the D₈
   lattice. It sits inside the E₈ lattice and helps generate the exceptional symmetry group E₈.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'9-demicube', name:'9-demicube', tag:'Uniform 9-polytope', schlafli:'h{4,3⁷}', family:'ncube', dim:9,
      gen:()=>nDemicube(9), rot:[P(0,8,.28),P(1,7,.2),P(2,6,.12)],
      facts:{Dimension:'9D', Vertices:256, Symmetry:'D₉', Facets:'mixed'},
      blurb:'The alternated 9-cube.',
      article:`<p>The <b>9-demicube</b> (demienneract) keeps the 256 even-parity vertices of the 9-cube. It
   continues the Dₙ family into nine dimensions.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'10-demicube', name:'10-demicube', tag:'Uniform 10-polytope', schlafli:'h{4,3⁸}', family:'ncube', dim:10,
      gen:()=>nDemicube(10), rot:[P(0,9,.26),P(1,8,.18),P(2,7,.1)],
      facts:{Dimension:'10D', Vertices:512, Symmetry:'D₁₀', Facets:'mixed'},
      blurb:'The alternated 10-cube.',
      article:`<p>The <b>10-demicube</b> (demidekeract) has 512 vertices — half of the 10-cube. It is the
   ten-dimensional member of the infinite demicube family.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'cube-triangle-duoprism', name:'Cube–triangle duoprism', tag:'5D duoprism', schlafli:'{4,3}×{3}', family:'prism', dim:5,
      gen:()=>polyDuoprism(cube3D(), 3), rot:[P(0,3,.38),P(1,4,.34),P(2,3,.2)],
      facts:{Vertices:24, Edges:60, Cells:'3 cubes + 6 tri-prisms + 4 cube-prisms', Dimension:'5D'},
      blurb:'A cube and a triangle multiplied together in 5-space.',
      article:`<p>The <b>cube–triangle duoprism</b> is the Cartesian product of a cube and a triangle. Its
   24 vertices are all pairs of a cube vertex and a triangle vertex, and its 60 edges come from cube
   edges × triangle vertices plus triangle edges × cube vertices.</p>`,
      refs:['coxeter', 'manning'] },

    { id:'octahedron-triangle-duoprism', name:'Octahedron–triangle duoprism', tag:'5D duoprism', schlafli:'{3,4}×{3}', family:'prism', dim:5,
      gen:()=>polyDuoprism(octahedron3D(), 3), rot:[P(0,3,.4),P(1,4,.36),P(2,3,.22)],
      facts:{Vertices:18, Edges:45, Cells:'3 octahedra + 8 tri-prisms', Dimension:'5D'},
      blurb:'An octahedron and a triangle multiplied together in 5-space.',
      article:`<p>The <b>octahedron–triangle duoprism</b> multiplies an octahedron by a triangle. Its 18 vertices
   and 45 edges form a five-dimensional figure with octahedral and triangular symmetries combined.</p>`,
      refs:['coxeter', 'manning'] },

    { id:'ammann-beenker-quasicrystal', name:'Ammann–Beenker quasicrystal', tag:'Aperiodic order', schlafli:'8D→2D cut', family:'phys', dim:8,
      gen:()=>ammannBeenker(2), rot:[P(0,7,.26),P(1,6,.18),P(2,5,.12),P(3,4,.08)],
      facts:{Symmetry:'8-fold (forbidden for crystals)', Origin:'projection from 8D', Relation:'octagonal Penrose'},
      blurb:'An eight-fold aperiodic pattern projected from 8D.',
      article:`<p>The <b>Ammann–Beenker tiling</b> is an aperiodic pattern with eight-fold rotational symmetry,
   impossible for ordinary crystals. Like the Penrose tiling, it arises as the shadow of a periodic
   lattice in a higher-dimensional space — here, an 8-cube lattice projected down to the plane.</p>`,
      refs:['penrose', 'shechtman'] },

    // ===== ROW 11 — Ten-dimensional regulars, 5-cell truncations, antiprismatic prisms =====
    { id:'10-cube', name:'10-cube', tag:'Regular 10-polytope', schlafli:'{4,3⁸}', family:'ncube', dim:10,
      gen:()=>nCube(10), rot:[P(0,9,.26),P(1,8,.18),P(2,7,.12),P(3,6,.08)],
      facts:{Vertices:1024, Edges:5120, '9-faces':'20 9-cubes', Dimension:'10D'},
      blurb:'The ten-dimensional hypercube: 1024 corners, 5120 edges.',
      article:`<p>The <b>10-cube</b> (dekeract) continues the doubling pattern of the hypercubes: 2¹⁰ = 1024 vertices
   at every combination of ±1 in ten coordinates, and 10·2⁹ = 5120 edges joining vertices that differ in one
   coordinate. Its boundary is twenty 9-cubes, two for each axis.</p>
   <p>Every vertex has ten edges, one per direction, and the long diagonal is √10 times the edge. What you see
   is the shadow left after seven successive perspective projections, each one folding a dimension away.</p>`,
      refs:['coxeter', 'manning'] },

    { id:'10-simplex', name:'10-simplex', tag:'Regular 10-polytope', schlafli:'{3⁹}', family:'ncube', dim:10,
      gen:()=>nSimplex(10), rot:[P(0,9,.28),P(1,8,.2),P(2,7,.12)],
      facts:{Vertices:11, Edges:55, '9-faces':'11 9-simplices', Dimension:'10D'},
      blurb:'Eleven points, every pair joined, the simplest 10D solid.',
      article:`<p>The <b>10-simplex</b> (decayotton) is the minimal polytope of ten dimensions: eleven mutually
   equidistant vertices with every pair joined, 55 edges in all. Remove any one vertex and the other ten span
   a 9-simplex, so its boundary is eleven of those.</p>
   <p>An n-simplex has n + 1 vertices and C(n+1, k+1) faces of dimension k, so its face counts are a row of
   Pascal's triangle. It is self-dual, like every simplex.</p>`,
      refs:['coxeter', 'schlafli'] },

    { id:'8-orthoplex', name:'8-orthoplex', tag:'Regular 8-polytope', schlafli:'{3⁶,4}', family:'ncube', dim:8,
      gen:()=>nOrthoplex(8), rot:[P(0,7,.3),P(1,6,.22),P(2,5,.14)],
      facts:{Vertices:16, Edges:112, '7-faces':'256 7-simplices', Dimension:'8D'},
      blurb:'The eight-dimensional cross-polytope, dual of the 8-cube.',
      article:`<p>The <b>8-orthoplex</b> (octacross) puts a vertex at ±1 on each of eight axes. Every vertex is joined
   to every other except its opposite, giving 112 edges, and its boundary is 2⁸ = 256 seven-dimensional
   simplices, one in each orthant.</p>
   <p>It is the dual of the 8-cube: the cube's 16 facets become the orthoplex's 16 vertices, and the cube's 256
   vertices become its 256 facets.</p>`,
      refs:['coxeter', 'schlafli'] },

    { id:'9-orthoplex', name:'9-orthoplex', tag:'Regular 9-polytope', schlafli:'{3⁷,4}', family:'ncube', dim:9,
      gen:()=>nOrthoplex(9), rot:[P(0,8,.28),P(1,7,.2),P(2,6,.13)],
      facts:{Vertices:18, Edges:144, '8-faces':'512 8-simplices', Dimension:'9D'},
      blurb:'Eighteen vertices on nine axes, almost every pair joined.',
      article:`<p>The <b>9-orthoplex</b> (enneacross) has eighteen vertices, ±1 on nine axes. Each is joined to the
   sixteen that are not its antipode, for 144 edges, and 2⁹ = 512 eight-dimensional simplices make up its
   boundary.</p>
   <p>Orthoplexes grow slowly, 2n vertices in n dimensions, while their duals, the hypercubes, grow
   exponentially. That contrast is what makes the cross-polytope the unit ball of the taxicab norm.</p>`,
      refs:['coxeter', 'schlafli'] },

    { id:'bitruncated-5-cell', name:'Bitruncated 5-cell', tag:'Uniform 4-polytope', schlafli:'t₁,₂{3,3,3}', family:'unif', dim:4,
      gen:()=>wythoff(4,A4,ring(4,1,2)), rot:[P(0,3,.42),P(1,2,.28)],
      facts:{Cells:'10 truncated tetrahedra', Vertices:30, Edges:60, Faces:'20 triangles + 20 hexagons'},
      blurb:'Ten truncated tetrahedra, the 5-cell cut halfway to its dual.',
      article:`<p>The <b>bitruncated 5-cell</b> (decachoron) is truncated so deeply that the 5-cell and its dual
   meet in the middle. All ten cells are truncated tetrahedra, five from the original cells and five from the
   original vertices, and it is cell-transitive: every cell is like every other.</p>
   <p>The frame drawn here is the true one, built by Wythoff's construction: a point is reflected through the
   four mirrors of the 5-cell's symmetry group until it has visited all 30 positions.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'cantitruncated-5-cell', name:'Cantitruncated 5-cell', tag:'Uniform 4-polytope', schlafli:'t₀,₁,₂{3,3,3}', family:'unif', dim:4,
      gen:()=>wythoff(4,A4,ring(4,0,1,2)), rot:[P(0,3,.38),P(1,3,.26)],
      facts:{Cells:'5 truncated octahedra + 10 triangular prisms + 5 truncated tetrahedra', Vertices:60, Edges:120},
      blurb:'Truncated and bevelled: three mirrors active at once.',
      article:`<p>The <b>cantitruncated 5-cell</b> rings three of the four nodes of the 5-cell's Coxeter diagram:
   vertices and edges are both cut back, and the truncation is deep enough that the original cells become
   truncated octahedra. Sixty vertices, each meeting four edges.</p>
   <p>In three dimensions the same operation turns a cube into a truncated cuboctahedron.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'runcitruncated-5-cell', name:'Runcitruncated 5-cell', tag:'Uniform 4-polytope', schlafli:'t₀,₁,₃{3,3,3}', family:'unif', dim:4,
      gen:()=>wythoff(4,A4,ring(4,0,1,3)), rot:[P(0,3,.36),P(1,2,.26),P(2,3,.14)],
      facts:{Cells:'5 truncated tetrahedra + 5 cuboctahedra + 10 triangular prisms + 10 hexagonal prisms', Vertices:60, Edges:150},
      blurb:'Truncation combined with the purely four-dimensional runcination.',
      article:`<p>The <b>runcitruncated 5-cell</b> combines truncation with runcination, the expansion that pulls
   the cells of a 4-polytope apart. Thirty cells of four kinds fill the gaps, and every vertex meets five
   edges.</p>
   <p>Runcination has no analogue in three dimensions, so the shapes it produces are the first genuinely
   four-dimensional members of the Wythoff family.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'omnitruncated-5-cell', name:'Omnitruncated 5-cell', tag:'Uniform 4-polytope', schlafli:'t₀,₁,₂,₃{3,3,3}', family:'unif', dim:4,
      gen:()=>wythoff(4,A4,ring(4,0,1,2,3)), rot:[P(0,3,.34),P(1,2,.24),P(2,3,.14)],
      facts:{Cells:'10 truncated octahedra + 20 hexagonal prisms', Vertices:120, Edges:240},
      blurb:'Every mirror active: the permutohedron of five elements.',
      article:`<p>The <b>omnitruncated 5-cell</b> rings every node of the diagram, so it has one vertex for each of the
   120 elements of the 5-cell's symmetry group. Its vertices are the permutations of (0, 1, 2, 3, 4): it is the
   4D permutohedron, and its edges swap two adjacent values.</p>
   <p>It tiles 4-space by translation alone, the four-dimensional analogue of the truncated octahedron filling
   3-space.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'square-antiprismatic-prism', name:'Square antiprismatic prism', tag:'Uniform prism', schlafli:'s{2,8}×{ }', family:'unif', dim:4,
      gen:()=>prism4D(antiprism3D(4)), rot:[P(0,3,.42),P(1,2,.3)],
      facts:{Cells:'2 square antiprisms + 2 cubes + 8 triangular prisms', Vertices:16, Edges:40},
      blurb:'A square antiprism extruded along a fourth axis.',
      article:`<p>The <b>square antiprismatic prism</b> extrudes a square antiprism, two squares twisted 45° and
   joined by a band of triangles, into the fourth dimension. Its cells are the two antiprisms at the ends, two
   cubes and eight triangular prisms around the sides.</p>
   <p>It belongs to an infinite family, one for every antiprism, which together with the duoprisms are the only
   infinite families of convex uniform 4-polytopes.</p>`,
      refs:['coxeter', 'conwayguy'] },

    { id:'pentagonal-antiprismatic-prism', name:'Pentagonal antiprismatic prism', tag:'Uniform prism', schlafli:'s{2,10}×{ }', family:'unif', dim:4,
      gen:()=>prism4D(antiprism3D(5)), rot:[P(0,3,.4),P(1,2,.28)],
      facts:{Cells:'2 pentagonal antiprisms + 2 pentagonal prisms + 10 triangular prisms', Vertices:20, Edges:50},
      blurb:'A pentagonal antiprism extruded into the fourth dimension.',
      article:`<p>The <b>pentagonal antiprismatic prism</b> extrudes a pentagonal antiprism, the cap-less middle of an
   icosahedron, along a fourth axis. Two antiprisms close the ends and twelve prisms join them.</p>
   <p>The same antiprism, twenty of them stacked in rings, makes the walls of the grand antiprism elsewhere in
   this hall.</p>`,
      refs:['coxeter', 'conwayguy'] },

    // ===== ROW 12 — Tesseract and 24-cell truncations, and the grand antiprism ============
    { id:'bitruncated-tesseract', name:'Bitruncated tesseract', tag:'Uniform 4-polytope', schlafli:'t₁,₂{4,3,3}', family:'unif', dim:4,
      gen:()=>wythoff(4,B4,ring(4,1,2)), rot:[P(0,3,.38),P(1,2,.26)],
      facts:{Cells:'8 truncated octahedra + 16 truncated tetrahedra', Vertices:96, Edges:192},
      blurb:'The tesseract truncated halfway to the 16-cell.',
      article:`<p>The <b>bitruncated tesseract</b> (also the bitruncated 16-cell) sits exactly halfway between the
   tesseract and its dual. The cubes have become truncated octahedra and the 16-cell's tetrahedra have become
   truncated tetrahedra; 96 vertices in all.</p>
   <p>Its vertices are the permutations of (0, ±1, ±2, ±2) up to scale.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'cantitruncated-tesseract', name:'Cantitruncated tesseract', tag:'Uniform 4-polytope', schlafli:'t₀,₁,₂{4,3,3}', family:'unif', dim:4,
      gen:()=>wythoff(4,B4,ring(4,0,1,2)), rot:[P(0,3,.36),P(1,3,.24)],
      facts:{Cells:'8 truncated cuboctahedra + 16 truncated tetrahedra + 32 triangular prisms', Vertices:192, Edges:384},
      blurb:'Cubes become great rhombicuboctahedra.',
      article:`<p>The <b>cantitruncated tesseract</b> rings the first three nodes of the tesseract's diagram. Each cube
   becomes a truncated cuboctahedron, the largest Archimedean cell available, and 192 vertices result.</p>
   <p>It keeps the full order-384 symmetry of the tesseract.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'runcitruncated-tesseract', name:'Runcitruncated tesseract', tag:'Uniform 4-polytope', schlafli:'t₀,₁,₃{4,3,3}', family:'unif', dim:4,
      gen:()=>wythoff(4,B4,ring(4,0,1,3)), rot:[P(0,3,.34),P(1,2,.24),P(2,3,.16)],
      facts:{Cells:'8 truncated cubes + 16 cuboctahedra + 24 octagonal prisms + 32 triangular prisms', Vertices:192, Edges:480},
      blurb:'Truncated cubes pulled apart by octagonal prisms.',
      article:`<p>The <b>runcitruncated tesseract</b> truncates the tesseract and runcinates it at once: the cubes become
   truncated cubes and drift apart, and octagonal prisms fill the space between them. Every vertex meets five
   edges.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'omnitruncated-tesseract', name:'Omnitruncated tesseract', tag:'Uniform 4-polytope', schlafli:'t₀,₁,₂,₃{4,3,3}', family:'unif', dim:4,
      gen:()=>wythoff(4,B4,ring(4,0,1,2,3)), rot:[P(0,3,.32),P(1,2,.24),P(2,3,.14)],
      facts:{Cells:'8 truncated cuboctahedra + 16 truncated octahedra + 24 octagonal prisms + 32 hexagonal prisms', Vertices:384, Edges:768},
      blurb:'One vertex for every symmetry of the tesseract: 384.',
      article:`<p>The <b>omnitruncated tesseract</b> rings all four nodes, so it has one vertex for each of the 384
   symmetries of the tesseract. Its vertices are the permutations of (±1, ±(1+√2), ±(1+2√2), ±(1+3√2)).</p>
   <p>It is the largest uniform polytope in the tesseract family.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'truncated-24-cell', name:'Truncated 24-cell', tag:'Uniform 4-polytope', schlafli:'t₀,₁{3,4,3}', family:'unif', dim:4,
      gen:()=>wythoff(4,F4,ring(4,0,1)), rot:[P(0,3,.36),P(1,2,.26),P(2,3,.14)],
      facts:{Cells:'24 cubes + 24 truncated octahedra', Vertices:192, Edges:384},
      blurb:'The 24-cell with its corners cut off into cubes.',
      article:`<p>The <b>truncated 24-cell</b> cuts each of the 24-cell's vertices, whose figure is a cube, so 24 new cubic
   cells appear while the 24 octahedra become truncated octahedra. Its vertices are the permutations of
   (0, ±1, ±2, ±3).</p>
   <p>Every vertex is surrounded by the same cells as in the cantitruncated 16-cell: the two are the same
   polytope, reached from two different symmetry groups.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'bitruncated-24-cell', name:'Bitruncated 24-cell', tag:'Uniform 4-polytope', schlafli:'t₁,₂{3,4,3}', family:'unif', dim:4,
      gen:()=>wythoff(4,F4,ring(4,1,2)), rot:[P(0,3,.34),P(1,2,.26),P(2,3,.16)],
      facts:{Cells:'48 truncated cubes', Vertices:288, Edges:576},
      blurb:'Forty-eight identical truncated cubes, cell-transitive.',
      article:`<p>The <b>bitruncated 24-cell</b> (tetracontoctachoron) truncates the self-dual 24-cell halfway to its
   dual. Because the 24-cell is self-dual, the result has only one kind of cell: 48 truncated cubes, every one
   alike.</p>
   <p>With the bitruncated 5-cell it is one of only two non-regular convex uniform polychora that are
   cell-transitive.</p>`,
      refs:['coxeter', 'conway'] },

    { id:'cantitruncated-24-cell', name:'Cantitruncated 24-cell', tag:'Uniform 4-polytope', schlafli:'t₀,₁,₂{3,4,3}', family:'unif', dim:4,
      gen:()=>wythoff(4,F4,ring(4,0,1,2)), rot:[P(0,3,.32),P(1,2,.24),P(2,3,.14)],
      facts:{Cells:'24 truncated cuboctahedra + 24 truncated cubes + 96 triangular prisms', Vertices:576, Edges:1152},
      blurb:'Three of four mirrors active in the 24-cell family.',
      article:`<p>The <b>cantitruncated 24-cell</b> truncates and bevels the 24-cell. Its 576 vertices carry the full F₄
   symmetry of order 1152, each vertex image fixed by a single reflection.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'runcitruncated-24-cell', name:'Runcitruncated 24-cell', tag:'Uniform 4-polytope', schlafli:'t₀,₁,₃{3,4,3}', family:'unif', dim:4,
      gen:()=>wythoff(4,F4,ring(4,0,1,3)), rot:[P(0,3,.32),P(1,2,.22),P(2,3,.14)],
      facts:{Vertices:576, Edges:1440, Symmetry:'F₄, order 1152'},
      blurb:'The 24-cell truncated and pulled apart along its cells.',
      article:`<p>The <b>runcitruncated 24-cell</b> combines truncation with runcination in the 24-cell family: truncated
   octahedra drift apart and prisms open between them. 576 vertices, five edges at each.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'omnitruncated-24-cell', name:'Omnitruncated 24-cell', tag:'Uniform 4-polytope', schlafli:'t₀,₁,₂,₃{3,4,3}', family:'unif', dim:4,
      gen:()=>wythoff(4,F4,ring(4,0,1,2,3)), rot:[P(0,3,.3),P(1,2,.22),P(2,3,.12)],
      facts:{Cells:'48 truncated cuboctahedra + 192 hexagonal prisms', Vertices:1152, Edges:2304},
      blurb:'1152 vertices: one for each symmetry of the 24-cell.',
      article:`<p>The <b>omnitruncated 24-cell</b> rings every node of the F₄ diagram, giving one vertex per symmetry of
   the 24-cell: 1152 of them. Its 240 cells are truncated cuboctahedra and hexagonal prisms.</p>
   <p>It is the largest uniform polytope with the 24-cell's symmetry.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'grand-antiprism', name:'Grand antiprism', tag:'Uniform 4-polytope', schlafli:'non-Wythoffian', family:'unif', dim:4,
      gen:grandAntiprism, rot:[P(0,1,.3),P(2,3,.3),P(0,3,.12)],
      facts:{Cells:'20 pentagonal antiprisms + 300 tetrahedra', Vertices:100, Edges:500, Faces:'20 pentagons + 700 triangles'},
      blurb:'The one convex uniform 4-polytope Wythoff could not build.',
      article:`<p>The <b>grand antiprism</b> was found by John Conway and Michael Guy in 1965 by computer search, the only
   convex uniform 4-polytope that is neither a prism nor a product of a Wythoff construction. Take the 600-cell
   and delete the twenty vertices on two completely orthogonal great decagons: what remains is 100 vertices and
   500 edges.</p>
   <p>Its twenty pentagonal antiprisms stack into two linked rings of ten, one around each missing decagon, with
   300 tetrahedra filling the space between them.</p>`,
      refs:['conwayguy', 'coxeter'] },

    // ===== ROW 13 — The ten regular star polychora ======================================
    { id:'icosahedral-120-cell', name:'Icosahedral 120-cell', tag:'Regular star polychoron', schlafli:'{3,5,5/2}', family:'star', dim:4,
      gen:()=>starPolychoron('3',12,5,120,false), rot:[P(0,3,.34),P(1,2,.24)],
      facts:{Cells:'120 icosahedra', Faces:'1200 triangles', Edges:720, Vertices:120},
      blurb:'120 icosahedra, five around every edge, passing through one another.',
      article:`<p>The <b>icosahedral 120-cell</b> (faceted 600-cell) is one of the ten regular star polychora described by
   Schläfli and Hess. Its cells are 120 icosahedra, but five meet at each edge and they interpenetrate, so the
   figure winds around its centre more than once.</p>
   <p>All ten star polychora use the vertices of the 600-cell or the 120-cell. This one also keeps the
   600-cell's 720 edges, so its wireframe is the 600-cell's; only its faces and cells differ.</p>`,
      refs:['hess', 'coxeter'] },

    { id:'small-stellated-120-cell', name:'Small stellated 120-cell', tag:'Regular star polychoron', schlafli:'{5/2,5,3}', family:'star', dim:4,
      gen:()=>starPolychoron('5/2',20,3,120,false), rot:[P(0,3,.32),P(1,2,.24)],
      facts:{Cells:'120 small stellated dodecahedra', Faces:'720 pentagrams', Edges:1200, Vertices:120},
      blurb:'The 120-cell stellated: pentagram faces on 120 spiked cells.',
      article:`<p>The <b>small stellated 120-cell</b> replaces each dodecahedral cell of the 120-cell with a small
   stellated dodecahedron. Its 720 faces are pentagrams and its 1200 edges join each vertex to twenty
   others.</p>
   <p>It shares that edge arrangement with the great grand 120-cell: drawn as wireframes the two are
   identical.</p>`,
      refs:['hess', 'coxeter'] },

    { id:'great-120-cell', name:'Great 120-cell', tag:'Regular star polychoron', schlafli:'{5,5/2,5}', family:'star', dim:4,
      gen:()=>starPolychoron('5',12,5,120,true), rot:[P(0,1,.32),P(2,3,.22)],
      facts:{Cells:'120 great dodecahedra', Faces:'720 pentagons', Edges:720, Vertices:120},
      blurb:'Self-dual: great dodecahedra, five around every edge.',
      article:`<p>The <b>great 120-cell</b> has 120 great dodecahedra for cells, meeting five at an edge. It is self-dual,
   its Schläfli symbol a palindrome, and its Euler characteristic is zero.</p>
   <p>Its edges are those of the 600-cell, so its wireframe matches the 600-cell's exactly.</p>`,
      refs:['hess', 'coxeter'] },

    { id:'grand-120-cell', name:'Grand 120-cell', tag:'Regular star polychoron', schlafli:'{5,3,5/2}', family:'star', dim:4,
      gen:()=>starPolychoron('5',12,5,120,true), rot:[P(0,2,.32),P(1,3,.22)],
      facts:{Cells:'120 dodecahedra', Faces:'720 pentagons', Edges:720, Vertices:120},
      blurb:'Ordinary dodecahedra arranged to wrap 4-space many times over.',
      article:`<p>The <b>grand 120-cell</b> is built from 120 perfectly ordinary dodecahedra, but they meet with a great
   icosahedron as the vertex figure, so the whole figure is a star. It is the dual of the great stellated
   120-cell.</p>
   <p>It too uses the 600-cell's edges.</p>`,
      refs:['hess', 'coxeter'] },

    { id:'great-stellated-120-cell', name:'Great stellated 120-cell', tag:'Regular star polychoron', schlafli:'{5/2,3,5}', family:'star', dim:4,
      gen:()=>starPolychoron('5/2',12,5,120,false), rot:[P(0,3,.3),P(1,2,.22)],
      facts:{Cells:'120 great stellated dodecahedra', Faces:'720 pentagrams', Edges:720, Vertices:120},
      blurb:'The most spiked cells there are, three around every edge.',
      article:`<p>The <b>great stellated 120-cell</b> uses the great stellated dodecahedron, the most stellated of the
   Kepler–Poinsot solids, as its cell, with an icosahedral vertex figure. Each edge is φ times the
   circumradius, the golden ratio again.</p>
   <p>Four star polychora share this edge arrangement.</p>`,
      refs:['hess', 'coxeter'] },

    { id:'grand-stellated-120-cell', name:'Grand stellated 120-cell', tag:'Regular star polychoron', schlafli:'{5/2,5,5/2}', family:'star', dim:4,
      gen:()=>starPolychoron('5/2',12,5,120,false), rot:[P(0,1,.3),P(2,3,.24)],
      facts:{Cells:'120 small stellated dodecahedra', Faces:'720 pentagrams', Edges:720, Vertices:120},
      blurb:'Self-dual, pentagrams everywhere, the densest regular star.',
      article:`<p>The <b>grand stellated 120-cell</b> is self-dual and has the highest density of any regular polychoron:
   its cells wrap around the centre 66 times. Its cells are small stellated dodecahedra and its vertex figure is
   the great dodecahedron.</p>`,
      refs:['hess', 'coxeter'] },

    { id:'great-grand-120-cell', name:'Great grand 120-cell', tag:'Regular star polychoron', schlafli:'{5,5/2,3}', family:'star', dim:4,
      gen:()=>starPolychoron('5',20,3,120,true), rot:[P(0,2,.3),P(1,3,.24)],
      facts:{Cells:'120 great dodecahedra', Faces:'720 pentagons', Edges:1200, Vertices:120},
      blurb:'Great dodecahedra meeting three at an edge.',
      article:`<p>The <b>great grand 120-cell</b> has great dodecahedra for cells, three around each edge, and the great
   stellated dodecahedron as its vertex figure. It is the dual of the great icosahedral 120-cell.</p>
   <p>It shares its 1200 edges with the small stellated 120-cell.</p>`,
      refs:['hess', 'coxeter'] },

    { id:'great-icosahedral-120-cell', name:'Great icosahedral 120-cell', tag:'Regular star polychoron', schlafli:'{3,5/2,5}', family:'star', dim:4,
      gen:()=>starPolychoron('3',12,5,120,true), rot:[P(0,3,.3),P(1,2,.26)],
      facts:{Cells:'120 great icosahedra', Faces:'1200 triangles', Edges:720, Vertices:120},
      blurb:'Great icosahedra for cells, triangles for faces.',
      article:`<p>The <b>great icosahedral 120-cell</b> (great faceted 600-cell) is made of 120 great icosahedra with a
   small stellated dodecahedron as vertex figure. Each edge is φ times the circumradius.</p>`,
      refs:['hess', 'coxeter'] },

    { id:'grand-600-cell', name:'Grand 600-cell', tag:'Regular star polychoron', schlafli:'{3,3,5/2}', family:'star', dim:4,
      gen:()=>starPolychoron('3',12,5,120,true), rot:[P(0,3,.32),P(1,2,.22),P(2,3,.14)],
      facts:{Cells:'600 tetrahedra', Faces:'1200 triangles', Edges:720, Vertices:120},
      blurb:'600 tetrahedra again, but now arranged as a star.',
      article:`<p>The <b>grand 600-cell</b> has the same cell count as the 600-cell, 600 regular tetrahedra, but its vertex
   figure is the great icosahedron, so twenty tetrahedra wind around each vertex several times. It is the only
   star polychoron with simplex cells.</p>`,
      refs:['hess', 'coxeter'] },

    { id:'great-grand-stellated-120-cell', name:'Great grand stellated 120-cell', tag:'Regular star polychoron', schlafli:'{5/2,3,3}', family:'star', dim:4,
      gen:()=>starPolychoron('5/2',4,3,600,false), rot:[P(0,3,.28),P(1,2,.2)],
      facts:{Cells:'120 great stellated dodecahedra', Faces:'720 pentagrams', Edges:1200, Vertices:600},
      blurb:'The final stellation of the 120-cell, on its 600 vertices.',
      article:`<p>The <b>great grand stellated 120-cell</b> is the final stellation of the 120-cell and the only star
   polychoron with 600 vertices: it uses the 120-cell's vertex set, with a tetrahedron as vertex figure, and
   joins each vertex to four others by a chord about 1.85 times the circumradius.</p>
   <p>Its dual is the grand 600-cell. Schläfli found four of the star polychora; Hess completed the list of ten
   in 1883, and with the six convex ones they are all the regular polytopes of four dimensions.</p>`,
      refs:['hess', 'coxeter'] },

    // ===== ROW 14 — Gosset polytopes of E₆, E₇, E₈ and their lattices ====================
    { id:'e6-polytope-221', name:'E₆ polytope (2₂₁)', tag:'Gosset polytope', schlafli:'2₂₁', family:'ncube', dim:6,
      gen:()=>wythoff(6,E6,ring(6,0)), rot:[P(0,5,.32),P(1,4,.24),P(2,3,.16)],
      facts:{Vertices:27, Edges:216, '5-faces':'27 5-orthoplexes + 72 5-simplices', Symmetry:'E₆, order 51,840'},
      blurb:'Twenty-seven vertices, one for each line on a cubic surface.',
      article:`<p>The <b>2₂₁ polytope</b> was found by Thorold Gosset in 1900. Its 27 vertices correspond to the 27 straight
   lines that lie on any smooth cubic surface, and two vertices are joined exactly when their lines do not
   meet; its symmetry group, the Weyl group of E₆, is the symmetry group of that configuration.</p>
   <p>Drawn here by Wythoff's construction from the E₆ Coxeter diagram, ringed at the end of a long arm.</p>`,
      refs:['gosset', 'coxeter'] },

    { id:'e6-polytope-122', name:'E₆ polytope (1₂₂)', tag:'Gosset polytope', schlafli:'1₂₂', family:'ncube', dim:6,
      gen:()=>wythoff(6,E6,ring(6,5)), rot:[P(0,5,.3),P(1,4,.22),P(2,3,.14)],
      facts:{Vertices:72, Edges:720, '5-faces':'54 5-demicubes', Symmetry:'E₆ with central inversion, order 103,680'},
      blurb:'The 72 roots of E₆ as the corners of one polytope.',
      article:`<p>The <b>1₂₂ polytope</b> has the 72 roots of the exceptional Lie algebra E₆ for vertices. Each joins twenty
   others, and its facets are 54 five-dimensional demicubes. Ringing the short arm of the E₆ diagram gives it.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'e7-polytope-321', name:'E₇ polytope (3₂₁)', tag:'Gosset polytope', schlafli:'3₂₁', family:'ncube', dim:7,
      gen:()=>wythoff(7,E7,ring(7,5)), rot:[P(0,6,.3),P(1,5,.22),P(2,4,.14)],
      facts:{Vertices:56, Edges:756, '6-faces':'126 6-orthoplexes + 576 6-simplices', Symmetry:'E₇, order 2,903,040'},
      blurb:'Fifty-six vertices: the smallest picture of E₇.',
      article:`<p>The <b>3₂₁ polytope</b>, another of Gosset's, has 56 vertices, the weights of the smallest representation of
   E₇, and they pair up into the 28 bitangents of a plane quartic curve. Each vertex is joined to 27 others.</p>`,
      refs:['gosset', 'coxeter'] },

    { id:'e7-polytope-231', name:'E₇ polytope (2₃₁)', tag:'Gosset polytope', schlafli:'2₃₁', family:'ncube', dim:7,
      gen:()=>wythoff(7,E7,ring(7,0)), rot:[P(0,6,.28),P(1,5,.2),P(2,4,.14)],
      facts:{Vertices:126, Edges:2016, '6-faces':'56 2₂₁ + 576 6-simplices', Symmetry:'E₇, order 2,903,040'},
      blurb:'The 126 roots of E₇, each touching 32 others.',
      article:`<p>The <b>2₃₁ polytope</b> has the 126 roots of E₇ for vertices, and its facets include 56 copies of the E₆
   polytope 2₂₁. It is the kissing configuration of the E₇ lattice made solid.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'e7-polytope-132', name:'E₇ polytope (1₃₂)', tag:'Gosset polytope', schlafli:'1₃₂', family:'ncube', dim:7,
      gen:()=>wythoff(7,E7,ring(7,6)), rot:[P(0,6,.26),P(1,5,.2),P(2,4,.12)],
      facts:{Vertices:576, Edges:10080, '6-faces':'126 1₂₂ + 56 6-demicubes', Symmetry:'E₇, order 2,903,040'},
      blurb:'576 vertices, 35 edges at every one.',
      article:`<p>The <b>1₃₂ polytope</b> rings the short arm of the E₇ diagram. Its 576 vertices each meet 35 edges, and
   its facets are 126 copies of 1₂₂ and 56 six-dimensional demicubes.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'e8-polytope-241', name:'E₈ polytope (2₄₁)', tag:'Gosset polytope', schlafli:'2₄₁', family:'ncube', dim:8,
      gen:()=>{ const g = wythoff(8,E8,ring(8,0)); return { verts:g.verts, edges:capEdges(g.edges,6000) }; },
      rot:[P(0,7,.24),P(1,6,.18),P(2,5,.12),P(3,4,.08)],
      facts:{Vertices:2160, Edges:69120, '7-faces':'240 2₃₁ + 17280 7-simplices', Frame:'all 2160 vertices, 6000 of the edges'},
      blurb:'2160 vertices of E₈; too many edges to draw, so a sample.',
      article:`<p>The <b>2₄₁ polytope</b> has 2160 vertices, the vectors of the second shell of the E₈ lattice, and 69,120
   edges, 64 at every vertex. The frame shows every vertex but only an evenly spaced sample of 6000 edges.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'e8-polytope-142', name:'E₈ polytope (1₄₂)', tag:'Gosset polytope', schlafli:'1₄₂', family:'ncube', dim:8,
      gen:()=>{ const verts = polarCap(wythoffVerts(8,E8,ring(8,7)),1400); return { verts, edges:capEdges(edgesByShortest(verts),6000) }; },
      rot:[P(0,7,.22),P(1,6,.16),P(2,5,.1),P(3,4,.07)],
      facts:{Vertices:17280, Edges:483840, '7-faces':'2160 1₃₂ + 240 7-demicubes', Frame:'the 953 vertices nearest one pole'},
      blurb:'17,280 vertices; the frame is the cap around one of them.',
      article:`<p>The <b>1₄₂ polytope</b> is the largest of the E₈ family: 17,280 vertices and 483,840 edges, 56 at each
   vertex. Far too large to animate whole, it is drawn as a cap: the whole shells of vertices nearest one
   vertex, 953 of them, with a sample of the edges between them.</p>`,
      refs:['coxeter', 'elte'] },

    { id:'e6-lattice', name:'E₆ lattice', tag:'Sphere packing', schlafli:'E₆', family:'phys', dim:6,
      gen:()=>kissing(rootSystem(6,E6),6000), rot:[P(0,5,.3),P(1,4,.22),P(2,3,.14)],
      facts:{'Kissing number':72, 'Minimal vectors':'the 72 roots of E₆', Packing:'densest lattice in 6D'},
      blurb:'One sphere and the 72 that touch it in six dimensions.',
      article:`<p>The <b>E₆ lattice</b> gives the densest lattice packing of spheres in six dimensions, as Blichfeldt proved. Each sphere
   touches 72 others, centred on the roots of E₆. The frame shows that kissing configuration: spokes from the
   central sphere, and the contacts between touching neighbours, which meet at 60°.</p>`,
      refs:['conway', 'coxeter'] },

    { id:'e7-lattice', name:'E₇ lattice', tag:'Sphere packing', schlafli:'E₇', family:'phys', dim:7,
      gen:()=>kissing(rootSystem(7,E7),6000), rot:[P(0,6,.28),P(1,5,.2),P(2,4,.14)],
      facts:{'Kissing number':126, 'Minimal vectors':'the 126 roots of E₇', Packing:'densest lattice in 7D'},
      blurb:'In seven dimensions each sphere touches 126.',
      article:`<p>The <b>E₇ lattice</b> is the densest lattice packing in seven dimensions, proven optimal among lattices
   by Blichfeldt. Its 126 minimal vectors are the roots of E₇; drawn as a kissing configuration they are the
   2₃₁ polytope with spokes to its centre.</p>`,
      refs:['conway', 'coxeter'] },

    { id:'coxeter-todd-lattice', name:'Coxeter–Todd lattice', tag:'Sphere packing', schlafli:'K₁₂', family:'phys', dim:12,
      gen:()=>kissing(coxeterToddMinimal(),6000), rot:[P(0,11,.24),P(1,10,.18),P(2,9,.12)],
      facts:{'Kissing number':756, Construction:'Eisenstein 6-vectors, congruent mod √−3', Frame:'all 756 contacts, 6000 edges'},
      blurb:'756 spheres around one: the densest known 12D packing.',
      article:`<p>The <b>Coxeter–Todd lattice</b> K₁₂ gives the densest known packing of spheres in twelve dimensions, each
   touching 756 others. Over the Eisenstein integers it is six-dimensional: the vectors whose coordinates are
   all congruent modulo √−3 and whose sum is divisible by 3. Its minimal vectors fall into 270 of one shape and
   486 of another.</p>`,
      refs:['coxetertodd', 'conway'] },

    // ===== ROW 15 — Barnes–Wall, quasicrystals, higher Hopf maps, knotted spheres ===========
    { id:'barnes-wall-lattice', name:'Barnes–Wall lattice', tag:'Sphere packing', schlafli:'Λ₁₆', family:'phys', dim:16,
      gen:()=>kissing(polarCap(barnesWallMinimal(),700),6000), rot:[P(0,15,.22),P(1,14,.16),P(2,13,.1)],
      facts:{'Kissing number':4320, Construction:'Reed–Muller code RM(1,4)', Frame:'one sphere, one contact and the 280 touching both'},
      blurb:'4320 spheres around each one in sixteen dimensions.',
      article:`<p>The <b>Barnes–Wall lattice</b> Λ₁₆ is the densest known packing in sixteen dimensions. Its vectors are the
   integer 16-tuples that reduce modulo 2 to a word of the Reed–Muller code RM(1,4) and whose sum is divisible
   by 4; the shortest are (±2, ±2, 0¹⁴) and ±1 on the eight positions of a codeword.</p>
   <p>Each sphere touches 4320 others. The frame shows the central sphere, one of its neighbours and the 280
   spheres that touch them both.</p>`,
      refs:['barneswall', 'conway'] },

    { id:'elser-sloane-quasicrystal', name:'Elser–Sloane quasicrystal', tag:'Aperiodic order', schlafli:'8D→4D cut', family:'phys', dim:8,
      gen:()=>elserSloane(2,1), rot:[P(0,3,.24),P(1,2,.18),P(3,7,.08)],
      facts:{Symmetry:'H₄, order 14,400', Origin:'E₈ lattice as the icosian ring', Frame:'241 points within radius 2'},
      blurb:'A four-dimensional quasicrystal cut from the E₈ lattice.',
      article:`<p>The <b>Elser–Sloane quasicrystal</b> is the four-dimensional analogue of the Penrose tiling. The E₈ lattice
   can be written as the icosians, quaternions with coordinates in ℤ[φ]; splitting each coordinate by the two
   values of √5 sends it to two copies of 4-space. Keep the points whose second image falls in a ball, and the
   first images form an aperiodic pattern with the 600-cell's symmetry.</p>
   <p>Every point within radius 2 of the origin is shown, joined by unit icosian steps.</p>`,
      refs:['elsersloane', 'conway'] },

    { id:'icosahedral-quasicrystal', name:'Icosahedral quasicrystal', tag:'Aperiodic order', schlafli:'6D→3D cut', family:'phys', dim:6,
      gen:()=>icosahedralQuasicrystal(2.6), rot:[P(0,1,.2),P(0,2,.14),P(2,5,.08)],
      facts:{Symmetry:'icosahedral, with forbidden 5-fold axes', Origin:'Z⁶ cut by a rhombic triacontahedron', Frame:'111 points, 240 edges'},
      blurb:'The 3D Penrose tiling: a cut through the 6-cube lattice.',
      article:`<p>The <b>icosahedral quasicrystal</b> models Shechtman's alloy, whose diffraction showed the five-fold symmetry
   no crystal can have. Map the six axes of ℤ⁶ to the six five-fold axes of an icosahedron and, separately, to
   their Galois conjugates; keep the lattice points whose conjugate image falls in the rhombic triacontahedron the
   unit cube projects to. The result is the three-dimensional Penrose tiling, its edges in six directions.</p>
   <p>The window is shifted a hair off-centre so no lattice point sits on its boundary. The slow rotation that
   mixes physical and internal axes is a phason: the cut itself moving.</p>`,
      refs:['levine', 'shechtman'] },

    { id:'quaternionic-hopf-fibration', name:'Quaternionic Hopf fibration', tag:'Fibered 7-sphere', schlafli:'S⁷→S⁴', family:'curve', dim:8,
      gen:()=>hopfHigher(4), rot:[P(0,4,.3),P(1,5,.22),P(2,6,.14)],
      facts:{Fibre:'S³', Base:'S⁴', Frame:'9 fibres, 3 great circles each'},
      blurb:'The 7-sphere filled with linked 3-spheres.',
      article:`<p>The <b>quaternionic Hopf fibration</b> repeats Hopf's construction with quaternions in place of complex
   numbers. A point (a, b) of the unit 7-sphere in ℍ² maps to a·b⁻¹, a point of the quaternionic line, which is
   a 4-sphere; every fibre is a round 3-sphere, and any two are linked.</p>
   <p>Each fibre is drawn as three of its great circles.</p>`,
      refs:['hopf', 'adams'] },

    { id:'octonionic-hopf-fibration', name:'Octonionic Hopf fibration', tag:'Fibered 15-sphere', schlafli:'S¹⁵→S⁸', family:'curve', dim:16,
      gen:()=>hopfHigher(8), rot:[P(0,8,.26),P(1,9,.18),P(2,10,.12),P(3,15,.08)],
      facts:{Fibre:'S⁷', Base:'S⁸', Frame:'9 fibres, 3 great circles each'},
      blurb:'The last Hopf fibration: 7-spheres filling a 15-sphere.',
      article:`<p>The <b>octonionic Hopf fibration</b> uses the octonions, the largest normed division algebra. They are not
   associative, but they are alternative, which is enough: the fibre over m is {(m·y, y)}, an 8-dimensional plane
   meeting the 15-sphere in a 7-sphere.</p>
   <p>Adams proved in 1960 that there are no more: the fibrations S¹→S¹, S³→S², S⁷→S⁴ and S¹⁵→S⁸ are the only
   ones of their kind, a shadow of there being only four normed division algebras.</p>`,
      refs:['adams', 'baez'] },

    { id:'simple-rotation', name:'Simple rotation', tag:'Rotation in one plane', schlafli:'SO(4), one angle', family:'rot', dim:4,
      gen:rotTesseract, rot:[P(0,3,.4)],
      facts:{'Fixed set':'a whole plane', 'Rotating plane':'x₁x₄', Contrast:'a double rotation fixes only the centre'},
      blurb:'A 4D rotation that leaves an entire plane untouched.',
      article:`<p>In three dimensions every rotation turns about an axis. In four, a <b>simple rotation</b> turns one plane
   and leaves the completely orthogonal plane fixed point by point: here the x₁x₄ plane turns while x₂ and x₃
   never move.</p>
   <p>Compare the isoclinic and double rotations, which turn two planes at once and fix nothing but the
   centre.</p>`,
      refs:['manning', 'banchoff'] },

    { id:'spun-trefoil', name:'Spun trefoil', tag:'Knotted 2-sphere', schlafli:'S²⊂R⁴', family:'rot', dim:4,
      gen:()=>spunKnot(trefoilCurve), rot:[P(2,3,.36),P(0,3,.16)],
      facts:{Construction:'Artin spinning', Group:'same as the trefoil’s', Frame:'10 copies of the arc + spin circles'},
      blurb:'A trefoil arc spun around a plane: a knotted sphere.',
      article:`<p>Knots in 3-space fall open in 4-space, but 2-spheres can knot there. Emil Artin's <b>spinning</b> builds
   one: cut a trefoil open into an arc with both ends on a plane, then spin the arc around that plane through the
   fourth dimension. The swept surface is a 2-sphere, knotted, with the trefoil's knot group.</p>
   <p>The frame shows the arc at ten spin angles and the circles its points trace.</p>`,
      refs:['artin', 'banchoff'] },

    { id:'spun-figure-eight-knot', name:'Spun figure-eight knot', tag:'Knotted 2-sphere', schlafli:'S²⊂R⁴', family:'rot', dim:4,
      gen:()=>spunKnot(figureEightCurve), rot:[P(2,3,.34),P(1,3,.16)],
      facts:{Construction:'Artin spinning', Group:'same as the figure-eight’s', Frame:'10 copies of the arc + spin circles'},
      blurb:'The figure-eight knot spun into a knotted 2-sphere.',
      article:`<p>The <b>spun figure-eight knot</b> applies Artin's construction to the figure-eight knot. Spinning preserves
   the knot group, so this 2-sphere is knotted in 4-space for the same algebraic reason the figure-eight is
   knotted in 3-space.</p>`,
      refs:['artin', 'banchoff'] },

    { id:'trefoil-seifert-surface', name:'Seifert surface of the trefoil', tag:'Milnor fibre', schlafli:'z² − w³ > 0', family:'rot', dim:4,
      gen:()=>trefoilSeifert(), rot:[P(0,1,.3),P(2,3,.2)],
      facts:{Genus:1, Boundary:'the trefoil, z² = w³ on S³', Fibration:'one page of the open book'},
      blurb:'A punctured torus in the 3-sphere whose edge is the trefoil.',
      article:`<p>Every knot bounds an orientable surface, its <b>Seifert surface</b>. For the trefoil there is a beautiful
   one: the trefoil is where z² = w³ on the unit 3-sphere in ℂ², and the points where z² − w³ is a positive real
   form a punctured torus with exactly the trefoil as its boundary.</p>
   <p>Rotating the phase sweeps that surface round the knot, filling the rest of the 3-sphere: Milnor's fibration.
   The frame draws the surface over a polar grid of w, in four sheets, with the trefoil itself as a closed curve.</p>`,
      refs:['seifert', 'milnorsing'] },

    { id:'roman-surface', name:'Roman surface', tag:'Immersed RP²', schlafli:'RP²', family:'curve', dim:4,
      gen:()=>romanSurface(), rot:[P(0,3,.3),P(1,2,.22)],
      facts:{'Triple points':1, 'Double curves':3, 'Pinch points':6},
      blurb:'Steiner’s projective plane, unpinched in the fourth dimension.',
      article:`<p>Jakob Steiner found the <b>Roman surface</b> while in Rome in 1844: the image of the sphere under
   (x, y, z) ↦ (yz, xz, xy). Antipodal points map together, so it is a projective plane, squeezed into 3-space with
   three double lines meeting at a triple point and six pinch points.</p>
   <p>Add a fourth coordinate, (x² − y²)/2, and the map becomes one-to-one on the projective plane: a clean
   embedding in 4-space, a projection of the Veronese surface. The rotation into that axis opens the
   self-intersections.</p>`,
      refs:['apery', 'boy'] },

    // ===== ROW 16 — CP², Kuen, honeycombs, triaprisms and higher spheres ==================
    { id:'complex-projective-plane', name:'Complex projective plane', tag:'Closed 4-manifold', schlafli:'CP²', family:'curve', dim:8,
      gen:()=>cp2(), rot:[P(0,7,.24),P(1,6,.18),P(2,5,.12)],
      facts:{'Real dimension':4, Embedding:'rank-one projectors in R⁸', 'Euler characteristic':3},
      blurb:'Complex lines through the origin of C³, drawn by their tori.',
      article:`<p>The <b>complex projective plane</b> is the set of complex lines through the origin of ℂ³, a closed
   four-dimensional manifold. Sending each line to the matrix that projects onto it embeds it smoothly in eight
   dimensions.</p>
   <p>Its torus action gives a picture: the moment map sends CP² onto a triangle, and over each interior point sits
   a 2-torus, over each edge a circle and over each corner a single point. The frame draws those circles over a
   grid of the triangle.</p>`,
      refs:['atiyah'] },

    { id:'kuen-surface', name:'Kuen surface', tag:'Constant negative curvature', schlafli:'K = −1', family:'curve', dim:3,
      gen:()=>kuenSurface(22,22), rot:[P(0,3,.28),P(1,2,.2)],
      facts:{Curvature:'K = −1 everywhere', Discovered:'Theodor Kuen, 1884', Relation:'pseudosphere, Dini'},
      blurb:'A sheet of constant negative curvature folded into a flower.',
      article:`<p>The <b>Kuen surface</b> is, like the pseudosphere and Dini's surface, a surface of constant curvature −1,
   found by Theodor Kuen in 1884. It can be built from the pseudosphere by a Bäcklund transformation and
   pinches to cusps along its edges.</p>
   <p>Hilbert proved that no complete surface of constant negative curvature fits in 3-space without such
   singularities.</p>`,
      refs:['stillwell'] },

    { id:'tesseractic-honeycomb', name:'Tesseractic honeycomb', tag:'Regular honeycomb', schlafli:'{4,3,3,4}', family:'comb', dim:4,
      gen:tesseractHoneycomb, rot:[P(0,3,.3),P(1,2,.22)],
      facts:{Cells:'tesseracts, 8 at each vertex', Frame:'a 2×2×2×2 block: 16 tesseracts, 81 vertices', Dual:'itself'},
      blurb:'Four-space packed with tesseracts, sixteen shown.',
      article:`<p>The <b>tesseractic honeycomb</b> fills 4-space with tesseracts, four around every square face and sixteen
   at every vertex, just as cubes fill 3-space. It is self-dual. A finite block of 2 × 2 × 2 × 2 cells is shown;
   the pattern continues forever.</p>`,
      refs:['coxeter'] },

    { id:'16-cell-honeycomb', name:'16-cell honeycomb', tag:'Regular honeycomb', schlafli:'{3,3,4,3}', family:'comb', dim:4,
      gen:cell16Honeycomb, rot:[P(0,3,.3),P(1,2,.24),P(2,3,.12)],
      facts:{Vertices:'the D₄ lattice', 'Vertex figure':'24-cell', Frame:'145 vertices within √6 of one'},
      blurb:'Sixteen-cells tiling 4-space on the D₄ lattice.',
      article:`<p>The <b>16-cell honeycomb</b> tiles 4-space with 16-cells. Its vertices are the D₄ lattice, the integer points
   with even coordinate sum, and the 24 neighbours of each vertex form a 24-cell. It is one of only three regular
   honeycombs of Euclidean 4-space, the most of any dimension above two.</p>`,
      refs:['coxeter', 'conway'] },

    { id:'24-cell-honeycomb', name:'24-cell honeycomb', tag:'Regular honeycomb', schlafli:'{3,4,3,3}', family:'comb', dim:4,
      gen:cell24Honeycomb, rot:[P(0,3,.28),P(1,2,.22),P(2,3,.12)],
      facts:{Cells:'24-cells centred on D₄', 'Vertex figure':'tesseract', Frame:'120 vertices within 1.75 of the centre'},
      blurb:'24-cells, the Voronoi cells of D₄, filling 4-space.',
      article:`<p>The <b>24-cell honeycomb</b> fills 4-space with 24-cells, the Voronoi cells of the D₄ lattice. It is dual
   to the 16-cell honeycomb: its vertices are D₄'s deep holes, and each meets sixteen edges whose far ends form a
   tesseract.</p>`,
      refs:['coxeter', 'conway'] },

    { id:'3x4x5-triaprism', name:'{3}×{4}×{5} triaprism', tag:'Triaprism', schlafli:'{3}×{4}×{5}', family:'duo', dim:6,
      gen:()=>polyprism(3,4,5), rot:[P(0,2,.36),P(1,4,.28),P(3,5,.2)],
      facts:{Vertices:60, Edges:180, '5-faces':12, Dimension:'6D'},
      blurb:'A triangle, a square and a pentagon multiplied in 6-space.',
      article:`<p>A <b>triaprism</b> is the product of three polygons, each in its own plane of 6-space. This one takes a
   triangle, a square and a pentagon: 3 × 4 × 5 = 60 vertices, each joined to its neighbours around all three
   polygons.</p>
   <p>Duoprisms multiply two polygons in 4D; the pattern continues in every even dimension.</p>`,
      refs:['coxeter', 'manning'] },

    { id:'3x3x3x3-tetraprism', name:'{3}×{3}×{3}×{3} tetraprism', tag:'Tetraprism', schlafli:'{3}⁴', family:'duo', dim:8,
      gen:()=>polyprism(3,3,3,3), rot:[P(0,2,.32),P(1,4,.24),P(3,6,.16),P(5,7,.1)],
      facts:{Vertices:81, Edges:324, '7-faces':12, Dimension:'8D'},
      blurb:'Four triangles multiplied together in eight dimensions.',
      article:`<p>The <b>{3}⁴ tetraprism</b> is the product of four triangles, one in each of four mutually orthogonal planes
   of 8-space. Its 81 vertices form a 3 × 3 × 3 × 3 grid wrapped into tori, eight edges at each.</p>`,
      refs:['coxeter', 'manning'] },

    { id:'5-sphere-s5', name:'5-sphere (S⁵)', tag:'Curved 6-manifold', schlafli:'S⁵', family:'curve', dim:6,
      gen:()=>sphereNet(6), rot:[P(0,5,.3),P(1,4,.22),P(2,3,.14)],
      facts:{'Embedded in':'R⁶', Volume:'π³ r⁵', Frame:'7 latitude circles'},
      blurb:'The round sphere of six-dimensional space.',
      article:`<p>The <b>5-sphere</b> is the set of points at distance r from a centre in six dimensions. Its volume is
   π³r⁵, and like every odd sphere it carries a nowhere-vanishing tangent field. The frame is a net of latitude
   circles.</p>`,
      refs:['manning', 'banchoff'] },

    { id:'6-sphere-s6', name:'6-sphere (S⁶)', tag:'Curved 7-manifold', schlafli:'S⁶', family:'curve', dim:7,
      gen:()=>sphereNet(7), rot:[P(0,6,.3),P(1,5,.22),P(2,4,.14)],
      facts:{'Embedded in':'R⁷', Volume:'16π³ r⁶ / 15', Frame:'7 latitude circles'},
      blurb:'The 6-sphere, whose complex structure is still an open question.',
      article:`<p>The <b>6-sphere</b> sits in seven dimensions, inside the imaginary octonions, which give it an almost
   complex structure. Whether it admits a genuine complex structure is one of the oldest open problems in
   geometry. Its volume, 16π³r⁶/15, makes the unit 6-sphere the largest unit sphere of any dimension: the volumes
   rise to it and then fall towards zero.</p>`,
      refs:['baez', 'manning'] },

    ];

    // =====================================================================================
    // Catalog assembly
    // =====================================================================================

    // Family labels: the plaque's family line and the Hyperspace page's legend.
    var FAMILY_LABELS = {
        reg:   'Regular 4-polytopes',
        unif:  'Uniform polytopes',
        five:  '5-cube family',
        ncube: 'n-cube / simplex',
        curve: 'Curved manifolds',
        duo:   'Duoprisms',
        rot:   'Rotations',
        phys:  'Spacetime / lattices',
        prism: '5D prisms & products',
        star:  'Star polychora',
        comb:  'Honeycombs'
    };
    var FAMILIES = {};
    Object.keys(C).forEach(function (k) {
        FAMILIES[k] = Object.freeze({ key: k, label: FAMILY_LABELS[k], color: C[k] });
    });
    Object.freeze(FAMILIES);
    Object.freeze(R);

    // Plaque fact keys that state a count, mapped to the record's `stated` field.
    var STATED_KEYS = { Vertices: 'vertices', Edges: 'edges', Faces: 'faces', Cells: 'cells' };

    var BY_ID = {};
    var SHAPES = RAW.map(function (e, index) {
        if (BY_ID[e.id]) throw new Error('Hyperspace: duplicate shape id ' + e.id);
        var stated = {};
        Object.keys(e.facts).forEach(function (k) { if (STATED_KEYS[k]) stated[STATED_KEYS[k]] = e.facts[k]; });
        var rec = {
            id: e.id,
            index: index,
            name: e.name,
            tag: e.tag,
            family: e.family,
            familyLabel: FAMILY_LABELS[e.family],
            color: C[e.family],
            dim: e.dim,
            schlafli: e.schlafli,
            symmetry: (typeof e.facts.Symmetry === 'string') ? e.facts.Symmetry : null,
            stated: Object.freeze(stated),
            facts: Object.freeze(Object.assign({}, e.facts)),
            rot: Object.freeze(e.rot.map(function (p) { return Object.freeze(p.slice()); })),
            blurb: e.blurb,
            article: e.article,
            refs: Object.freeze(e.refs.slice()),
            cites: Object.freeze(e.refs.map(function (k) {
                if (!R[k]) throw new Error('Hyperspace: unknown reference ' + k + ' on ' + e.id);
                return R[k];
            })),
            row: Math.floor(index / 10),
            col: index % 10
        };
        Object.defineProperty(rec, '_gen', { value: e.gen, enumerable: false });
        Object.freeze(rec);
        BY_ID[e.id] = rec;
        return rec;
    });
    Object.freeze(SHAPES);

    function get(ref) {
        if (ref && typeof ref === 'object' && ref.id && BY_ID[ref.id] === ref) return ref;
        if (typeof ref === 'number') {
            if (ref >= 0 && ref < SHAPES.length && Math.floor(ref) === ref) return SHAPES[ref];
            throw new RangeError('Hyperspace: index ' + ref + ' out of range 0..' + (SHAPES.length - 1));
        }
        if (ref && typeof ref === 'object' && ref.id) ref = ref.id;
        if (typeof ref === 'string' && BY_ID[ref]) return BY_ID[ref];
        throw new RangeError('Hyperspace: unknown shape ' + String(ref));
    }

    // ---- geometry (cached) ----------------------------------------------------------
    // A few parametric nets sample points where their formula is undefined (Dini's surface takes
    // log(tan(a/2)) past a = pi). A renderer cannot draw those points, so they are dropped here,
    // together with the edges that touch them; every drawable vertex and edge is unchanged.
    var GEO = {};
    function geometry(ref) {
        var s = get(ref);
        if (GEO[s.id]) return GEO[s.id];
        var raw = s._gen();
        var keep = new Array(raw.verts.length), verts = [], dropped = 0;
        for (var i = 0; i < raw.verts.length; i++) {
            var v = raw.verts[i], ok = true;
            for (var d = 0; d < v.length; d++) if (!isFinite(v[d])) { ok = false; break; }
            if (ok) { keep[i] = verts.length; verts.push(v); } else { keep[i] = -1; dropped++; }
        }
        var edges = [];
        for (var k = 0; k < raw.edges.length; k++) {
            var a = keep[raw.edges[k][0]], b = keep[raw.edges[k][1]];
            if (a >= 0 && b >= 0) edges.push(dropped ? [a, b] : raw.edges[k]);
        }
        var g = { verts: verts, edges: edges, embedDim: verts.length ? verts[0].length : 0, dropped: dropped };
        GEO[s.id] = g;
        return g;
    }

    function stats(ref) {
        var g = geometry(ref);
        return { vertices: g.verts.length, edges: g.edges.length, embedDim: g.embedDim };
    }

    // Rotate the native vertices through every plane the exhibit animates, at time t (seconds).
    function pose(ref, t) {
        var s = get(ref), V = geometry(s).verts;
        for (var r = 0; r < s.rot.length; r++) {
            var p = s.rot[r];
            V = rotate(V, p[0], p[1], t * p[2] + (p[3] || 0));
        }
        return V;
    }

    // Worst-case 3D radius of the projected shape over a rotation sweep long enough for every
    // plane to complete a turn: the normalisation the gallery uses to fit each shape in its cage.
    var EXT = {};
    function extent(ref) {
        var s = get(ref);
        if (EXT[s.id] !== undefined) return EXT[s.id];
        var minSp = Infinity;
        for (var r = 0; r < s.rot.length; r++) minSp = Math.min(minSp, Math.abs(s.rot[r][2]) || 1);
        if (!isFinite(minSp) || minSp <= 0) minSp = 0.3;
        var range = (2 * Math.PI) / minSp;
        var SAMPLES = 160, maxR = 0, base = geometry(s).verts;
        for (var k = 0; k < SAMPLES; k++) {
            var tt = (k / SAMPLES) * range, V = base;
            for (var q = 0; q < s.rot.length; q++) {
                var pl = s.rot[q];
                V = rotate(V, pl[0], pl[1], tt * pl[2] + (pl[3] || 0));
            }
            var P = projectTo3D(V);
            for (var i = 0; i < P.length; i++) {
                var rr = Math.hypot(P[i][0], P[i][1], P[i][2]);
                if (rr > maxR) maxR = rr;
            }
        }
        EXT[s.id] = maxR;
        return maxR;
    }

    // Pure 3D projection: [[x,y,z]...] with the worst-case radius normalised to `radius` (default 1).
    function project3D(ref, t, opts) {
        opts = opts || {};
        var s = get(ref);
        var P = projectTo3D(pose(s, t), opts.dist || 3.2);
        // a 16D shadow is ~1e-7 across after thirteen perspective divides, so only zero is degenerate
        var m = extent(s), k = (opts.radius || 1) / (m > 0 ? m : 1);
        return P.map(function (p) { return [p[0] * k, p[1] * k, p[2] * k]; });
    }

    // Pure 2D projection for a canvas: [[x, y, depth]...], y up, scaled so the worst-case pose fills the
    // unit disc.
    // The 3D figure turns about its vertical axis like a gallery exhibit (yaw = 0.15 rad/s by
    // default), is tilted by `pitch`, then perspective-divided with eye distance `focal`.
    function project2D(ref, t, opts) {
        opts = opts || {};
        var yaw = (opts.yaw !== undefined) ? opts.yaw : t * 0.15;
        var pitch = (opts.pitch !== undefined) ? opts.pitch : 0.35;
        var focal = opts.focal || 3.5;
        var P = project3D(ref, t, { dist: opts.dist });
        var cy = Math.cos(yaw), sy = Math.sin(yaw), cp = Math.cos(pitch), sp = Math.sin(pitch);
        var fit = Math.sqrt(1 - 1 / (focal * focal));   // a point on the unit sphere lands inside the unit disc
        return P.map(function (p) {
            var x = p[0] * cy + p[2] * sy, z = -p[0] * sy + p[2] * cy;
            var y = p[1] * cp - z * sp, z2 = p[1] * sp + z * cp;
            var k = fit * focal / (focal - z2);
            return [x * k, y * k, z2];
        });
    }

    // Stroke a shape onto a 2D canvas context, centred in a `size` box, depth-faded.
    function draw(ctx, ref, t, opts) {
        opts = opts || {};
        var s = get(ref), size = opts.size || 110;
        var g = geometry(s), P = project2D(s, t, opts);
        var half = size / 2, sc = half * (opts.fill || 0.9);
        var rgb = hexToRgb(opts.color || s.color), alpha = (opts.alpha !== undefined) ? opts.alpha : 1;
        ctx.save();
        ctx.lineWidth = opts.lineWidth || 0.8;
        // bucket edges into 4 depth bands so a frame costs four strokes, not one per edge
        var bands = [[], [], [], []];
        for (var i = 0; i < g.edges.length; i++) {
            var a = P[g.edges[i][0]], b = P[g.edges[i][1]];
            var z = (a[2] + b[2]) * 0.5;
            var bi = Math.max(0, Math.min(3, Math.floor((z + 1) * 2)));
            bands[bi].push(a, b);
        }
        for (var bnd = 0; bnd < 4; bnd++) {
            if (!bands[bnd].length) continue;
            ctx.strokeStyle = 'rgba(' + rgb + ',' + (alpha * (0.25 + bnd * 0.25)).toFixed(3) + ')';
            ctx.beginPath();
            var L = bands[bnd];
            for (var j = 0; j < L.length; j += 2) {
                ctx.moveTo(half + L[j][0] * sc, half - L[j][1] * sc);
                ctx.lineTo(half + L[j + 1][0] * sc, half - L[j + 1][1] * sc);
            }
            ctx.stroke();
        }
        ctx.restore();
    }

    function hexToRgb(c) {
        var m = /^#?([0-9a-f]{6})$/i.exec(c || '');
        if (!m) return '167,139,250';
        var n = parseInt(m[1], 16);
        return ((n >> 16) & 255) + ',' + ((n >> 8) & 255) + ',' + (n & 255);
    }

    function stripTags(html) {
        return String(html).replace(/<[^>]*>/g, '').replace(/&amp;/g, '&').replace(/\s+/g, ' ').trim();
    }

    // Plain-text description lines: everything the plaque states plus the wireframe as drawn.
    function details(ref) {
        var s = get(ref), st = stats(s);
        var lines = [
            'ID ' + s.id,
            'OBJECT ' + s.name,
            'CLASS ' + s.tag,
            'FAMILY ' + s.familyLabel,
            'SYMBOL ' + s.schlafli,
            'NATIVE DIM ' + s.dim + 'D'
        ];
        Object.keys(s.facts).forEach(function (k) { lines.push(k.replace(/_/g, ' ').toUpperCase() + ' ' + s.facts[k]); });
        lines.push('WIREFRAME ' + st.vertices + ' V / ' + st.edges + ' E IN R' + st.embedDim);
        s.rot.forEach(function (p) {
            lines.push('ROT PLANE x' + (p[0] + 1) + 'x' + (p[1] + 1) + ' @ ' + p[2] + ' rad/s');
        });
        lines.push(stripTags(s.blurb));
        return lines;
    }

    function projectDown(verts, toDim, dist) {
        var P = verts;
        while (P.length && P[0].length > toDim) {
            var out = new Array(P.length);
            for (var i = 0; i < P.length; i++) {
                var v = P[i], w = v[v.length - 1], k = 1 / (dist - w), nv = new Array(v.length - 1);
                for (var d = 0; d < v.length - 1; d++) nv[d] = v[d] * k;
                out[i] = nv;
            }
            P = out;
        }
        return P;
    }

    return Object.freeze({
        version: 1,
        count: SHAPES.length,
        shapes: SHAPES,
        families: FAMILIES,
        refs: R,
        ids: function () { return SHAPES.map(function (s) { return s.id; }); },
        get: get,
        has: function (id) { return Object.prototype.hasOwnProperty.call(BY_ID, id); },
        geometry: geometry,
        stats: stats,
        pose: pose,
        extent: extent,
        project3D: project3D,
        project2D: project2D,
        draw: draw,
        details: details,
        rotate: rotate,
        projectTo3D: projectTo3D,
        projectDown: projectDown,
        gen: Object.freeze({
            nCube: nCube, nSimplex: nSimplex, nOrthoplex: nOrthoplex, nDemicube: nDemicube,
            cell5: cell5, tesseract: tesseract, cell16: cell16, cell24: cell24, cell120: cell120, cell600: cell600,
            duoprism: duoprism, polyDuoprism: polyDuoprism, prism4D: prism4D, prism5D: prism5D,
            tetrahedron3D: tetrahedron3D, cube3D: cube3D, octahedron3D: octahedron3D,
            icosahedron3D: icosahedron3D, dodecahedron3D: dodecahedron3D,
            glome: glome, glome5D: glome5D, cliffordTorus: cliffordTorus, kleinBottle: kleinBottle,
            boysSurface: boysSurface, duocylinder: duocylinder, hopfFibration: hopfFibration,
            villarceau: villarceau, rp3: rp3, pseudosphere: pseudosphere, diniSurface: diniSurface,
            kuenSurface: kuenSurface, e8Roots: e8Roots, lightCone: lightCone, deSitter: deSitter,
            antiDeSitter: antiDeSitter, calabiYau: calabiYau, penrose: penrose, leech: leech,
            octonions: octonions, cyclic5D: cyclic5D, d4Lattice: d4Lattice, d5Lattice: d5Lattice,
            aRoots: aRoots, ammannBeenker: ammannBeenker, edgesByShortest: edgesByShortest,
            coxeterRoots: coxeterRoots, wythoff: wythoff, wythoffVerts: wythoffVerts, rootSystem: rootSystem,
            diagrams: Object.freeze({ A4: A4, B4: B4, F4: F4, H4: H4, A5: A5, B5: B5, E6: E6, E7: E7, E8: E8 }),
            grandAntiprism: grandAntiprism, snub24: snub24, antiprism3D: antiprism3D, starPolychoron: starPolychoron,
            kissing: kissing, barnesWallMinimal: barnesWallMinimal, coxeterToddMinimal: coxeterToddMinimal,
            elserSloane: elserSloane, icosahedralQuasicrystal: icosahedralQuasicrystal, hopfHigher: hopfHigher,
            spunKnot: spunKnot, trefoilSeifert: trefoilSeifert, romanSurface: romanSurface, cp2: cp2,
            tesseractHoneycomb: tesseractHoneycomb, cell16Honeycomb: cell16Honeycomb, cell24Honeycomb: cell24Honeycomb,
            polyprism: polyprism, sphereNet: sphereNet, capEdges: capEdges, polarCap: polarCap
        })
    });
});
