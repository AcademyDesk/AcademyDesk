// Isolated browser transport probe, NOT the application/SQL/Identity backend.
// Serves the actual compiled native handoff helper and a labelled synthetic
// 68 MiB attachment. No dev/Azure database, token, profile or data is read.
const http = require('node:http');
const crypto = require('node:crypto');
const fs = require('node:fs');
const ts = require('../../apps/web/node_modules/typescript');
const run = crypto.randomUUID();
const contentPath = '/api/class-media/' + run + '/content';
const filename = 'academydesk-qa-native-' + run + '.bin';
const length = 68 * 1024 * 1024;
const chunk = Buffer.alloc(64 * 1024, 0x5a);
const expected = crypto.createHash('sha256'); for (let n = 0; n < length; n += chunk.length) expected.update(chunk);
let origin, apiOrigin, ticket, expires;
function fail(res, code) { res.writeHead(code, { 'Cache-Control': 'no-store' }); res.end(); }
async function body(req) { const parts = []; let size = 0; for await (const part of req) { size += part.length; if (size > 4096) throw Error('Oversize body'); parts.push(part); } return Buffer.concat(parts).toString(); }
const api = http.createServer(async (req, res) => {
  try {
    if (req.headers.origin !== origin) return fail(res, 403);
    res.setHeader('Access-Control-Allow-Origin', origin); res.setHeader('Cache-Control', 'no-store');
    if (req.method === 'OPTIONS') { res.setHeader('Access-Control-Allow-Headers', 'Content-Type'); res.setHeader('Access-Control-Allow-Methods', 'POST'); return fail(res, 204); }
    if (req.method === 'POST' && req.url === '/api/class-material-downloads/tickets') {
      if (JSON.parse(await body(req)).path !== contentPath) return fail(res, 400);
      ticket = crypto.randomBytes(32).toString('base64url'); expires = Date.now() + 60000;
      res.setHeader('Content-Type', 'application/json'); res.end(JSON.stringify({ ticket, expiresInSeconds: 60 }));
      console.log('QA_BROWSER synthetic ticket issued; account authentication is NOT exercised by this probe.'); return;
    }
    if (req.method !== 'POST' || req.url !== contentPath || !req.headers['content-length'] || Number(req.headers['content-length']) > 4096 || req.headers['content-type'] !== 'application/x-www-form-urlencoded') return fail(res, 401);
    const fields = new URLSearchParams(await body(req));
    if (fields.size !== 1 || fields.get('ticket') !== ticket || Date.now() > expires) return fail(res, 401);
    console.log('QA_BROWSER native Origin/content-length/form credential PASS; streaming 71303168 synthetic bytes.');
    res.writeHead(200, { 'Content-Type': 'application/octet-stream', 'Content-Length': length, 'Content-Disposition': `attachment; filename="${filename}"`, 'X-Content-Type-Options': 'nosniff' });
    let sent = 0;
    function write() { while (sent < length) { sent += chunk.length; if (!res.write(chunk)) { res.once('drain', write); return; } } res.end(); }
    res.on('finish', () => console.log('QA_BROWSER stream response finished; disk persistence must be independently checked.'));
    write();
  } catch { fail(res, 400); }
});
const compiled = ts.transpileModule(fs.readFileSync('apps/web/src/lib/private-material.ts', 'utf8'), { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 } }).outputText;
const page = http.createServer((req, res) => {
  if (req.url !== '/') return fail(res, 404);
  res.writeHead(200, { 'Content-Type': 'text/html', 'Cache-Control': 'no-store' });
  res.end(`<!doctype html><html><head><title>AcademyDesk isolated native download probe</title></head><body><h1>Isolated private-download transport probe</h1><p>Synthetic 68 MiB file. Actual handoff helper; synthetic API, no account/SQL/portal test.</p><button id="download">Download synthetic 68 MiB attachment</button><p id="status" role="status">Ready</p><script>
  const module={exports:{}};const exports=module.exports;function require(){return {apiUrl:${JSON.stringify(apiOrigin)},academyApi:(path,options)=>fetch(${JSON.stringify(apiOrigin)}+path,options)}}
  ${compiled}
  document.getElementById('download').onclick=async()=>{const button=document.getElementById('download');button.disabled=true;try{const ticket=await module.exports.nativeMaterialTicket(${JSON.stringify(contentPath)},new AbortController().signal);module.exports.handoffNativeMaterial(${JSON.stringify(contentPath)},ticket);document.getElementById('status').textContent='Handed to browser. Confirm the saved file independently.'}catch{document.getElementById('status').textContent='Probe failed.'}finally{button.disabled=false}};
  </script></body></html>`);
});
api.listen(0, '127.0.0.1', () => {
  apiOrigin = 'http://127.0.0.1:' + api.address().port;
  page.listen(0, '127.0.0.1', () => {
    origin = 'http://127.0.0.1:' + page.address().port;
    console.log(JSON.stringify({ probe: 'native-download-only', run, url: origin, filename, length, sha256: expected.digest('hex') }));
  });
});
