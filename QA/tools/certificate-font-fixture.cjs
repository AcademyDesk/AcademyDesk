// Keep the accepted browser/appearance transports unchanged; add delayed real Geist bytes.
const fs = require('node:fs'), path = require('node:path'), Module = require('node:module');
const fontPath = path.resolve(__dirname, '../../apps/web/.next/dev/static/media/caa3a2e1cccd8315-s.p.0wgildi0cnwt9.woff2').replace(/\\/g, '/');
if (!fs.existsSync(fontPath)) throw Error('Recorded local Geist Latin font is unavailable; do not substitute a font');
const wrapper = path.resolve(__dirname, 'certificate-appearance-fixture.cjs');
let script = fs.readFileSync(wrapper, 'utf8');
const anchor = 'const fixture = new Module(original, module);';
if (script.split(anchor).length !== 2) throw Error('Appearance fixture extension anchor changed');
script = script.replace(anchor, `
replaceOnce('const requests = [], prints = [];', 'const requests = [], prints = []; const fontRequests = [];');
replaceOnce('{ certificates, requests, prints, readsFail, issueFail, branding, uploads, images }', '{ certificates, requests, prints, readsFail, issueFail, branding, uploads, images, fontRequests }');
replaceOnce("if (req.method === 'GET' && req.url === '/qa/state')", \`
    if (req.method === 'GET' && req.url.startsWith('/qa/font-slow-')) {
      const observation = { path: req.url, requestedAt: Date.now(), delay: 4500 };
      fontRequests.push(observation);
      await new Promise(resolve => setTimeout(resolve, observation.delay));
      const bytes = require('node:fs').readFileSync(${JSON.stringify(fontPath)});
      observation.servedAt = Date.now(); observation.bytes = bytes.length;
      res.writeHead(200, { 'Content-Type': 'font/woff2', 'Cache-Control': 'no-store' }); res.end(bytes); return;
    }
    if (req.method === 'GET' && req.url === '/qa/state')\`);
${anchor}`);
const extension = new Module(wrapper, module); extension.filename = wrapper; extension.paths = module.paths; extension._compile(script, wrapper);
