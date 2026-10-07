// Exact local static-export preview for the real Mini / SQL browser fixture.
// Serves only apps/web/out; no development server refreshes, proxy or mutations.
const http = require('node:http'), fs = require('node:fs'), path = require('node:path');
const root = fs.realpathSync(path.resolve(__dirname, '../../apps/web/out'));
const mime = { '.html': 'text/html', '.js': 'application/javascript', '.css': 'text/css', '.json': 'application/json', '.txt': 'text/plain', '.svg': 'image/svg+xml', '.png': 'image/png', '.ico': 'image/x-icon', '.woff2': 'font/woff2' };
http.createServer((request, response) => {
  if (request.headers.host !== '127.0.0.1:49542' || !['GET', 'HEAD'].includes(request.method)) { response.writeHead(400).end(); return; }
  try {
    const relative = decodeURIComponent(new URL(request.url, 'http://127.0.0.1:49542').pathname).replace(/^\/+/, '');
    let file = path.resolve(root, relative || 'index.html');
    if (file !== root && !file.startsWith(root + path.sep)) { response.writeHead(404).end(); return; }
    if (!path.extname(file) && fs.existsSync(file + '.html')) file += '.html';
    if (fs.existsSync(file) && fs.statSync(file).isDirectory()) file = path.join(file, 'index.html');
    // Next 16's Windows export emits nested segment files while its client
    // requests dot-encoded names. Serve the actual generated file, not a mock.
    const segment = /^__next\.([a-zA-Z0-9_-]+)\.__PAGE__\.txt$/.exec(path.basename(file));
    if (!fs.existsSync(file) && segment)
      file = path.join(path.dirname(file), '__next.' + segment[1], '__PAGE__.txt');
    const exact = fs.realpathSync(file);
    if (!exact.startsWith(root + path.sep) || !fs.statSync(exact).isFile()) { response.writeHead(404).end(); return; }
    response.writeHead(200, { 'Content-Type': mime[path.extname(exact)] || 'application/octet-stream', 'Cache-Control': 'no-store', 'X-Content-Type-Options': 'nosniff' });
    if (request.method === 'HEAD') response.end(); else fs.createReadStream(exact).pipe(response);
  } catch { response.writeHead(404).end(); }
}).listen(49542, '127.0.0.1', () => console.log('PENTA STATIC PREVIEW http://127.0.0.1:49542; exact built apps/web/out; local only'));
