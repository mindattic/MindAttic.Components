// Tiny static file server used in LOCAL mode. Serves each MindAttic.Web site folder under /<domain>/ so that
// http://127.0.0.1:4173/mindattic.com/index.htm is the working-tree page. Read-only, no dependencies.
import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';
import { SITES, SITES_ROOT } from './paths.mjs';

const TYPES = {
  '.htm': 'text/html; charset=utf-8', '.html': 'text/html; charset=utf-8', '.css': 'text/css; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8', '.json': 'application/json', '.png': 'image/png', '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg', '.gif': 'image/gif', '.ico': 'image/x-icon', '.svg': 'image/svg+xml', '.woff2': 'font/woff2',
  '.txt': 'text/plain; charset=utf-8',
};
const roots = Object.fromEntries(Object.values(SITES).map((s) => [s.dir, path.join(SITES_ROOT, s.dir)]));
const port = Number(process.env.TEST_PORT || 4173);

http.createServer((req, res) => {
  const url = new URL(req.url, 'http://x');
  if (url.pathname === '/__health') { res.writeHead(200); res.end('ok'); return; }
  const parts = decodeURIComponent(url.pathname).split('/').filter(Boolean);
  const root = roots[parts[0]];
  if (!root) { res.writeHead(404); res.end('unknown site'); return; }
  let file = path.join(root, ...parts.slice(1));
  if (!file.startsWith(root)) { res.writeHead(403); res.end('forbidden'); return; }
  if (fs.existsSync(file) && fs.statSync(file).isDirectory()) file = path.join(file, 'index.htm');
  if (!fs.existsSync(file)) { res.writeHead(404); res.end('not found'); return; }
  res.writeHead(200, { 'content-type': TYPES[path.extname(file).toLowerCase()] || 'application/octet-stream', 'cache-control': 'no-store' });
  fs.createReadStream(file).pipe(res);
}).listen(port, '127.0.0.1', () => console.log(`static server on http://127.0.0.1:${port} (sites from ${SITES_ROOT})`));
