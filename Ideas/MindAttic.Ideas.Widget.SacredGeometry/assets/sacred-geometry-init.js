/* sacred-geometry-init.js — generic auto-init driver for the SacredGeometry Plugin.
 *
 * Target glue (NOT canonical UiUx source): bundled in this Plugin's wwwroot and loaded AFTER
 * sacred-geometry.js. It animates every <canvas data-sacred-shape="N"> on the page with shape N,
 * so a consuming page ships ZERO JavaScript — it just emits the canvas. Generalized from the original
 * Legion persona-geometry.js: per-element shape via data-sacred-shape, optional opaque backdrop via
 * data-sacred-bg (default: transparent — the element's CSS background shows through), optional phase
 * step via data-sacred-spin (default 0.01).
 *
 * Only on-screen canvases animate (IntersectionObserver) — essential for a grid of up to 1000 cards —
 * and a MutationObserver attaches/detaches as the host's interactive-server DOM diffing re-renders.
 * Each frame re-reads data-sacred-shape so a canvas reused for a different item animates correctly.
 * Requires window.SacredGeometry; if it never loads, canvases simply stay blank.
 */
(function () {
    'use strict';

    function start(SG) {
        var visible = new Set();
        var sized = new WeakSet();

        function sizeCanvas(cv) {
            var dpr = window.devicePixelRatio || 1;
            var w = Math.max(1, Math.round(cv.clientWidth));
            var h = Math.max(1, Math.round(cv.clientHeight));
            var pxW = Math.round(w * dpr);
            var pxH = Math.round(h * dpr);
            if (pxW > 0 && pxH > 0 && (cv.width !== pxW || cv.height !== pxH)) { cv.width = pxW; cv.height = pxH; }
            sized.add(cv);
        }

        var io = new IntersectionObserver(function (entries) {
            entries.forEach(function (e) {
                var cv = e.target;
                if (e.isIntersecting) {
                    if (!sized.has(cv) || cv.width === 0) sizeCanvas(cv);
                    if (cv._sgPhase == null) cv._sgPhase = (parseInt(cv.dataset.sacredShape, 10) % 100) / 100 * 6.283;
                    visible.add(cv);
                } else {
                    visible.delete(cv);
                    var ctx = cv.getContext('2d');
                    if (ctx) ctx.clearRect(0, 0, cv.width, cv.height);
                }
            });
        }, { rootMargin: '150px' });

        var SEL = 'canvas[data-sacred-shape]';
        function attach(cv) { if (!cv._sgAttached) { cv._sgAttached = true; io.observe(cv); } }
        function detach(cv) { if (cv._sgAttached) { cv._sgAttached = false; io.unobserve(cv); visible.delete(cv); } }
        function scan(root) {
            if (!root || root.nodeType !== 1) return;
            if (root.matches && root.matches(SEL)) attach(root);
            var found = root.querySelectorAll ? root.querySelectorAll(SEL) : [];
            for (var i = 0; i < found.length; i++) attach(found[i]);
        }
        function unscan(root) {
            if (!root || root.nodeType !== 1) return;
            if (root.matches && root.matches(SEL)) detach(root);
            var found = root.querySelectorAll ? root.querySelectorAll(SEL) : [];
            for (var i = 0; i < found.length; i++) detach(found[i]);
        }

        scan(document.body);
        new MutationObserver(function (muts) {
            muts.forEach(function (m) {
                m.addedNodes.forEach(scan);
                m.removedNodes.forEach(unscan);
            });
        }).observe(document.body, { childList: true, subtree: true });

        function frame() {
            visible.forEach(function (cv) {
                if (!cv.isConnected) { detach(cv); return; }
                if (!cv.width) sizeCanvas(cv);
                var idx = parseInt(cv.dataset.sacredShape, 10);
                if (!(idx >= 0 && idx < SG.count)) return;
                var ctx = cv.getContext('2d');
                var spin = parseFloat(cv.dataset.sacredSpin); if (!(spin > 0)) spin = 0.01;
                cv._sgPhase += spin;
                var bg = cv.dataset.sacredBg;
                if (bg) { ctx.fillStyle = bg; ctx.fillRect(0, 0, cv.width, cv.height); }
                else { ctx.clearRect(0, 0, cv.width, cv.height); }
                // The shape is square; the box may not be. Draw it "cover"-style — sized to the larger
                // edge and centered — so the geometry is never stretched.
                var side = Math.max(cv.width, cv.height);
                ctx.save();
                ctx.translate((cv.width - side) / 2, (cv.height - side) / 2);
                SG.draw(ctx, idx, cv._sgPhase, { size: side });
                ctx.restore();
            });
            requestAnimationFrame(frame);
        }
        requestAnimationFrame(frame);
    }

    function boot(tries) {
        if (window.SacredGeometry && typeof window.SacredGeometry.draw === 'function') { start(window.SacredGeometry); return; }
        if (tries > 100) return;
        setTimeout(function () { boot(tries + 1); }, 200);
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', function () { boot(0); });
    else boot(0);
})();
