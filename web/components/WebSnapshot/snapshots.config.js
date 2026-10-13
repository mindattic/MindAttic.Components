// Snapshot targets. Add an entry per site. `name` is the output file stem
// (previews/<name>.b64.txt). All other fields map to webSnapshot() options.

module.exports = [
  {
    name: 'ryandebraal',
    url: 'https://ryandebraal.com',
    width: 150,
    height: 225,
    align: 'top-center',
    viewport: { width: 1280, height: 1600 },
  },
  {
    name: 'github-mindattic',
    url: 'https://github.com/mindattic',
    width: 150,
    height: 225,
    align: 'top-left',
    viewport: { width: 1280, height: 1600 },
  },
  {
    name: 'mindatticcares',
    url: 'https://mindatticcares.com',
    width: 150,
    height: 225,
    align: 'top-center',
    viewport: { width: 1280, height: 1600 },
  },
];
