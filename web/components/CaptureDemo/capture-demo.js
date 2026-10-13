/* MindAttic.Components - CaptureDemo/capture-demo.js
   When should a page run its hands-free demo? When ?demo=1 is in the URL, or when the
   MindAttic.Capture bridge (window.mindattic.capture) says the session is being filmed.
   The tour itself belongs to each page; this only decides when to start it:

     MindAtticCaptureDemo.armed(function startMyTour() { ... });  */
window.MindAtticCaptureDemo = (function () {
  function forced() { return /(?:^|[?&])demo=1(?:&|$)/.test(location.search); }
  function armed(go) {
    if (forced()) { go(); return; }
    var bridge = window.mindattic && window.mindattic.capture;
    if (!bridge || typeof bridge.status !== 'function') return;
    // `recording` is the StartRecording/StopRecording on-demand clip toggle, not whether
    // `-capture video` is actually filming the session — that's `tapeRunning`. The capture
    // engine also starts asynchronously (window must appear, then hardware encoder probing),
    // so poll instead of checking once, or a slow-starting recorder means the demo never runs.
    var tries = 0;
    (function poll() {
      bridge.status().then(function (s) {
        if (s && (s.tapeRunning || s.recording)) { go(); return; }
        if (++tries < 40) setTimeout(poll, 500);
      }).catch(function () { if (++tries < 40) setTimeout(poll, 500); });
    })();
  }
  return { forced: forced, armed: armed };
})();
