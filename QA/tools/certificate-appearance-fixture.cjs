// Successor synthetic transport. Original accepted fixture remains byte-for-byte intact.
// PNG upload/layout and delayed image observations only; not production upload validation.
const fs = require('node:fs');
const path = require('node:path');
const zlib = require('node:zlib');
const Module = require('node:module');
function crc32(bytes) {
  let crc = 0xffffffff;
  for (const byte of bytes) { crc ^= byte; for (let i = 0; i < 8; i++) crc = (crc >>> 1) ^ ((crc & 1) ? 0xedb88320 : 0); }
  return (crc ^ 0xffffffff) >>> 0;
}
function chunk(kind, bytes) {
  const name = Buffer.from(kind), length = Buffer.alloc(4), crc = Buffer.alloc(4);
  length.writeUInt32BE(bytes.length); crc.writeUInt32BE(crc32(Buffer.concat([name, bytes])));
  return Buffer.concat([length, name, bytes, crc]);
}
const pixels = Buffer.alloc(64 * (1 + 64 * 4));
for (let y = 0; y < 64; y++) for (let x = 0; x < 64; x++) {
  const offset = y * 257 + 1 + x * 4;
  const white = x >= 16 && x < 48 && y >= 16 && y < 48;
  pixels.set(white ? [255, 255, 255, 255] : [15, 108, 189, 255], offset);
}
const header = Buffer.alloc(13); header.writeUInt32BE(64); header.writeUInt32BE(64, 4); header[8] = 8; header[9] = 6;
const png = Buffer.concat([Buffer.from('89504e470d0a1a0a', 'hex'), chunk('IHDR', header), chunk('IDAT', zlib.deflateSync(pixels)), chunk('IEND', Buffer.alloc(0))]);
if (process.argv.includes('--assets')) {
  // Generated binary test data, not a customer logo or a rewritten text file.
  const target = path.resolve(__dirname, '../EVIDENCE/certificate-synthetic-logo.png');
  fs.writeFileSync(target, png); console.log(target); process.exit(0);
}
const original = path.resolve(__dirname, 'certificate-browser-fixture.cjs');
let source = fs.readFileSync(original, 'utf8');
function replaceOnce(before, after) {
  if (source.split(before).length !== 2) throw Error('Accepted fixture extension anchor changed');
  source = source.replace(before, after);
}
replaceOnce("const requests = [], prints = [];", "const requests = [], prints = []; const uploads = [], images = []; let logoSequence = 0; const qaPng = Buffer.from('" + png.toString('base64') + "', 'base64');");
replaceOnce("'GET,POST,OPTIONS'", "'GET,POST,PUT,OPTIONS'");
replaceOnce('{ certificates, requests, prints, readsFail, issueFail }', '{ certificates, requests, prints, readsFail, issueFail, branding, uploads, images }');
replaceOnce("if (req.method === 'GET' && req.url === '/qa/state')", `
    if (req.method === 'GET' && req.url.startsWith('/qa/logo-')) {
      const image = { path: req.url, requestedAt: Date.now(), delay: req.url.includes('delayed') ? 2000 : 0, broken: req.url.includes('broken') };
      images.push(image);
      if (image.delay) await new Promise(resolve => setTimeout(resolve, image.delay));
      image.servedAt = Date.now();
      res.writeHead(image.broken ? 404 : 200, { 'Content-Type': 'image/png', 'Cache-Control': 'no-store' });
      res.end(image.broken ? Buffer.alloc(0) : qaPng); return;
    }
    if (req.method === 'POST' && req.url.endsWith('/certificates/branding/logo')) {
      const chunks = []; let size = 0;
      for await (const value of req) { size += value.length; if (size > 2000000) throw Error('Synthetic bound'); chunks.push(value); }
      const form = await new Request('http://127.0.0.1/upload', { method: 'POST', headers: { 'Content-Type': req.headers['content-type'] }, body: Buffer.concat(chunks) }).formData();
      const file = form.get('logo');
      const accepted = file && typeof file.arrayBuffer === 'function' && file.type === 'image/png' && Buffer.from(await file.arrayBuffer()).equals(qaPng);
      uploads.push({ name: file?.name, type: file?.type, size: file?.size, accepted: Boolean(accepted), at: Date.now() });
      if (!accepted) return json(res, { message: 'Synthetic logo rejected' }, 400);
      branding.logoUrl = '/qa/logo-uploaded-' + ++logoSequence + '.png'; return json(res, branding);
    }
    if (req.method === 'PUT' && req.url.endsWith('/certificates/branding')) {
      Object.assign(branding, await body(req)); return json(res, branding);
    }
    if (req.method === 'GET' && req.url === '/qa/state')`);
replaceOnce("else if (action === 'long-note')", `else if (['fresh-delayed-logo', 'broken-logo', 'no-logo'].includes(action)) branding.logoUrl = action === 'no-logo' ? null : '/qa/logo-' + (action === 'fresh-delayed-logo' ? 'delayed' : 'broken') + '-' + ++logoSequence + '.png';
      else if (action === 'long-note')`);
const fixture = new Module(original, module); fixture.filename = original; fixture.paths = module.paths; fixture._compile(source, original);
