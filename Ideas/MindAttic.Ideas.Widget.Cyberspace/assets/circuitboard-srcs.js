// Target glue (NOT canonical UiUx source): points console-bg.js at the circuitboard textures bundled
// in THIS Plugin's wwwroot, served under /_ideas/Plugin/cyberspace/1/. Must load BEFORE console-bg.js,
// which reads window.__cyberspaceCircuitboardSrcs at init (else it falls back to /api/media/* and 404s).
window.__cyberspaceCircuitboardSrcs = [
  '/_ideas/Plugin/cyberspace/1/assets/circuitboard.00.png',
  '/_ideas/Plugin/cyberspace/1/assets/circuitboard.01.png',
  '/_ideas/Plugin/cyberspace/1/assets/circuitboard.02.png'
];
