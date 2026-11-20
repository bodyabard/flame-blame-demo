import { createServer } from 'node:http';
import { createReadStream, statSync } from 'node:fs';
import { resolve, extname, sep } from 'node:path';

const root = resolve(process.argv[2] || 'Builds/WebGL');
const port = Number(process.argv[3] || 8080);
const types = { '.html':'text/html; charset=utf-8', '.js':'text/javascript', '.wasm':'application/wasm', '.json':'application/json', '.css':'text/css', '.png':'image/png', '.svg':'image/svg+xml' };
createServer((request, response) => {
  let path;
  let info;
  try {
    const pathname = decodeURIComponent(new URL(request.url, 'http://localhost').pathname);
    path = resolve(root, '.' + (pathname === '/' ? '/index.html' : pathname));
    if (!path.startsWith(root + sep)) throw new Error('Not found');
    info = statSync(path);
    if (!info.isFile()) throw new Error('Not found');
  } catch {
    response.writeHead(404); response.end('Not found'); return;
  }
  response.writeHead(200, {
    'Content-Type': types[extname(path)] || 'application/octet-stream',
    'Content-Length': info.size,
    'Cache-Control':'no-store',
    'Cross-Origin-Opener-Policy':'same-origin',
    'Cross-Origin-Embedder-Policy':'require-corp',
    'X-Content-Type-Options':'nosniff'
  });
  createReadStream(path).pipe(response);
}).listen(port, '127.0.0.1', () => console.log(`Flame & Blame: http://127.0.0.1:${port} (serving ${root})`));
