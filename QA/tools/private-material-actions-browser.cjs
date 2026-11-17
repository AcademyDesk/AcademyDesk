// Actual React component in Chromium, isolated synthetic HTTP only; no SQL/model qualification.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path'), os = require('node:os');
const http = require('node:http'), crypto = require('node:crypto'), zlib = require('node:zlib');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '../..'), web = path.join(root, 'apps/web');
const evidence = process.env.QA_MATERIAL_ACTIONS_EVIDENCE || path.join(root, '.build-check/private-material-actions-browser');
const webpack = require(path.join(web, 'node_modules/next/dist/compiled/webpack/webpack')).webpack;
const compilerPath = process.env.QA_WEB_TYPESCRIPT || path.join(web, 'node_modules/typescript');
const delay = ms => new Promise(r => setTimeout(r, ms));
const idA = '11111111-1111-4111-8111-111111111111', idB = '22222222-2222-4222-8222-222222222222';
const fileA = '/api/class-media/' + idA + '/content', fileB = '/api/class-media/' + idB + '/content';
function png(red) {
  function chunk(name, data) {
    const value = Buffer.concat([Buffer.from(name), data]); let crc = 0xffffffff;
    for (const byte of value) { crc ^= byte; for (let i = 0; i < 8; i++) crc = (crc >>> 1) ^ ((crc & 1) ? 0xedb88320 : 0); }
    const size = Buffer.alloc(4), sum = Buffer.alloc(4); size.writeUInt32BE(data.length); sum.writeUInt32BE((crc ^ 0xffffffff) >>> 0);
    return Buffer.concat([size, value, sum]);
  }
  const header = Buffer.alloc(13); header.writeUInt32BE(1); header.writeUInt32BE(1, 4); header[8] = 8; header[9] = 6;
  return Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]), chunk('IHDR', header), chunk('IDAT', zlib.deflateSync(Buffer.from([0, red ? 255 : 0, 0, red ? 0 : 255, 255]))), chunk('IEND', Buffer.alloc(0))]);
}
class CDP {
  constructor(socket) { this.socket = socket; this.id = 0; this.pending = new Map(); this.events = new Map(); socket.addEventListener('message', e => {
    const value = JSON.parse(e.data), pending = this.pending.get(value.id);
    if (pending) { clearTimeout(pending.timer); this.pending.delete(value.id); value.error ? pending.reject(Error(value.error.message)) : pending.resolve(value.result); }
    else this.events.get((value.sessionId || '') + ':' + value.method)?.(value.params);
  }); }
  on(session, name, fn) { this.events.set((session || '') + ':' + name, fn); }
  send(method, params = {}, sessionId) { const id = ++this.id; return new Promise((resolve, reject) => {
    const timer = setTimeout(() => { this.pending.delete(id); reject(Error('CDP timeout ' + method)); }, 15000);
    this.pending.set(id, { resolve, reject, timer }); this.socket.send(JSON.stringify({ id, method, params, ...(sessionId ? { sessionId } : {}) }));
  }); }
}
async function main() {
  fs.mkdirSync(evidence, { recursive: true });
  const run = fs.mkdtempSync(path.join(evidence, 'run-')), results = [], requests = [], errors = [], held = [];
  const images = { [fileA]: png(true), [fileB]: png(false) };
  const variants = ['replace', 'cycle', 'pending-head', 'pending-get', 'native', 'close', 'unmount', 'external-return'];
  let server, browser, socket, cdp, profile, origin, variant, failure = null, cleanupError = null, passed = false;
  function release() { for (const item of held.splice(0)) if (!item.response.destroyed) item.finish(); }
  try {
    server = http.createServer(async (req, res) => {
      try {
        const pathname = new URL(req.url, 'http://127.0.0.1').pathname;
        if (req.method === 'GET' && pathname === '/teacher') { res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' }); return res.end('<!doctype html><html><head><meta charset="utf-8"><title>Isolated actual-component private-file probe</title></head><body><div id="root"></div><script src="/bundle.js"></script></body></html>'); }
        if (req.method === 'GET' && pathname === '/bundle.js') { res.writeHead(200, { 'Content-Type': 'application/javascript; charset=utf-8' }); return res.end(fs.readFileSync(path.join(run, 'bundle.js'))); }
        if (pathname === '/favicon.ico') { res.writeHead(204); return res.end(); }
        requests.push({ method: req.method, path: pathname, variant });
        a.equal(new URL(req.url, origin).search, '', 'No file/account credential in URL');
        res.setHeader('Cache-Control', 'private, no-store');
        if (pathname === '/api/class-material-downloads/tickets' && req.method === 'POST') {
          a.equal(req.headers.authorization, 'Bearer synthetic-teacher');
          let body = ''; for await (const data of req) { body += data; a.ok(body.length <= 4096); }
          const content = JSON.parse(body).path; a.ok(content in images);
          res.setHeader('Content-Type', 'application/json'); return res.end(JSON.stringify({ ticket: 'synthetic-' + (content === fileA ? 'a' : 'b'), expiresInSeconds: 60 }));
        }
        if (!(pathname in images)) throw Error('Unexpected fixture request ' + pathname);
        const image = images[pathname];
        if (req.method === 'POST') {
          a.equal(req.headers.authorization, undefined, 'Native form never sends account bearer');
          let body = ''; for await (const data of req) { body += data; a.ok(body.length <= 4096); }
          const form = new URLSearchParams(body); a.equal(form.size, 1); a.equal(form.get('ticket'), 'synthetic-' + (pathname === fileA ? 'a' : 'b'));
          res.setHeader('Content-Disposition', 'attachment; filename="synthetic-private.png"');
        } else { a.ok(['GET', 'HEAD'].includes(req.method)); a.equal(req.headers.authorization, 'Bearer synthetic-teacher'); }
        const finish = () => { if (res.destroyed) return; res.setHeader('Content-Type', 'application/octet-stream'); res.setHeader('Content-Length', image.length); res.end(req.method === 'HEAD' ? undefined : image); };
        if (pathname === fileA && (variant === 'pending-head' && req.method === 'HEAD' || variant === 'pending-get' && req.method === 'GET')) held.push({ response: res, finish });
        else finish();
      } catch (error) { errors.push(error.message); if (!res.headersSent) res.writeHead(500); res.end(); }
    });
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve)); origin = 'http://127.0.0.1:' + server.address().port;
    fs.writeFileSync(path.join(run, 'entry.tsx'), `import {useLayoutEffect,useState} from 'react';import {createRoot} from 'react-dom/client';import {flushSync} from 'react-dom';import {PrivateMaterialActions} from ${JSON.stringify(path.join(web, 'src/components/private-material-actions.tsx'))};
      function Harness(){const [name,setName]=useState('A'),[visible,setVisible]=useState(true);const files={A:{url:${JSON.stringify(fileA)},title:'Synthetic A'},B:{url:${JSON.stringify(fileB)},title:'Synthetic B'},external:{url:'https://example.invalid/reference',title:'External'}};
      useLayoutEffect(()=>{window.qaMedia={setFile:n=>flushSync(()=>setName(n)),unmount:()=>flushSync(()=>setVisible(false)),mount:n=>flushSync(()=>{setName(n);setVisible(true)})}},[]);
      return <main><h1 id="file-scope">{files[name].title}</h1>{visible&&<PrivateMaterialActions {...files[name]}/>}</main>}
      createRoot(document.querySelector('#root')).render(<Harness/>);`);
    fs.writeFileSync(path.join(run, 'tsx-loader.cjs'), `module.exports=function(source){return require(${JSON.stringify(compilerPath)}).transpileModule(source,{fileName:this.resourcePath,compilerOptions:{target:99,module:99,jsx:4}}).outputText}`);
    await new Promise((resolve, reject) => {
      const compiler = webpack({ mode: 'production', devtool: false, entry: path.join(run, 'entry.tsx'), output: { path: run, filename: 'bundle.js' }, cache: false,
        optimization: { minimize: false }, performance: { hints: false }, resolve: { extensions: ['.tsx', '.ts', '.js'], modules: [path.join(web, 'node_modules'), 'node_modules'], alias: { '@': path.join(web, 'src') } },
        module: { rules: [{ test: /\.tsx?$/, use: path.join(run, 'tsx-loader.cjs') }] }, plugins: [new webpack.DefinePlugin({ 'process.env.NEXT_PUBLIC_API_URL': JSON.stringify(origin) })] });
      compiler.run((error, stats) => { fs.writeFileSync(path.join(run, 'webpack.log'), error ? error.stack : stats.toString({ all: false, errors: true, warnings: true })); compiler.close(() => error || stats.hasErrors() ? reject(error || Error('Component fixture compilation failed')) : resolve()); });
    });
    profile = fs.mkdtempSync(path.join(fs.realpathSync(os.tmpdir()), 'c49-material-'));
    browser = spawn(process.env.QA_BROWSER_EXECUTABLE || '/usr/bin/chromium', ['--headless', '--disable-background-networking', '--no-first-run', '--remote-debugging-port=0', '--user-data-dir=' + profile, 'about:blank'], { stdio: ['ignore', 'ignore', 'pipe'] });
    const endpoint = await new Promise((resolve, reject) => { let log = ''; const timer = setTimeout(() => reject(Error('Chromium startup timed out')), 15000);
      browser.once('error', reject); browser.once('exit', code => reject(Error('Chromium exited ' + code)));
      browser.stderr.on('data', b => { log += b; fs.writeFileSync(path.join(run, 'browser.log'), log); const match = log.match(/DevTools listening on (ws:\/\/\S+)/); if (match) { clearTimeout(timer); resolve(match[1]); } }); });
    socket = new WebSocket(endpoint); await new Promise((resolve, reject) => { socket.addEventListener('open', resolve, { once: true }); socket.addEventListener('error', reject, { once: true }); }); cdp = new CDP(socket);
    const downloads = path.join(run, 'downloads'); fs.mkdirSync(downloads);
    await cdp.send('Browser.setDownloadBehavior', { behavior: 'allowAndName', downloadPath: downloads });
    for (const width of [390, 1280]) for (const next of variants) {
      variant = next; const { targetId } = await cdp.send('Target.createTarget', { url: 'about:blank' }); const { sessionId } = await cdp.send('Target.attachToTarget', { targetId, flatten: true });
      const evaluate = async expression => { const r = await cdp.send('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true, userGesture: true }, sessionId); if (r.exceptionDetails) throw Error(r.exceptionDetails.exception?.description || r.exceptionDetails.text); return r.result.value; };
      const wait = async expression => { for (let i = 0; i < 300; i++) { if (await evaluate(expression)) return; await delay(20); } throw Error('DOM wait timed out: ' + expression); };
      const click = label => evaluate(`Array.from(document.querySelectorAll('button')).find(b=>b.textContent===${JSON.stringify(label)}).click()`);
      const preview = async title => { await click('Preview'); await wait(`Boolean(document.querySelector('img')?.naturalWidth>0&&document.querySelector('img')?.alt===${JSON.stringify(title)})`); };
      const replace = async name => { await evaluate(`window.qaMedia.setFile(${JSON.stringify(name)})`); await wait(`document.querySelector('#file-scope')?.textContent===${JSON.stringify(name === 'external' ? 'External' : 'Synthetic ' + name)}`); };
      try {
        await cdp.send('Page.enable', {}, sessionId); await cdp.send('Runtime.enable', {}, sessionId); await cdp.send('Emulation.setDeviceMetricsOverride', { width, height: 900, deviceScaleFactor: 1, mobile: false }, sessionId);
        cdp.on(sessionId, 'Runtime.exceptionThrown', e => errors.push('Browser: ' + (e.exceptionDetails.exception?.description || e.exceptionDetails.text)));
        cdp.on(sessionId, 'Runtime.consoleAPICalled', e => { if (e.type === 'error') errors.push('Console: ' + e.args.map(x => x.value ?? x.description ?? '').join(' ')); });
        await cdp.send('Page.addScriptToEvaluateOnNewDocument', { source: `if(location.origin===${JSON.stringify(origin)}){localStorage.clear();localStorage.setItem('academydesk.accessToken.Teacher','synthetic-teacher');localStorage.setItem('academydesk.sessionEpoch.Teacher','synthetic-epoch');window.qaCreated=[];window.qaRevoked=[];const create=URL.createObjectURL.bind(URL),revoke=URL.revokeObjectURL.bind(URL);URL.createObjectURL=v=>{const u=create(v);qaCreated.push(u);return u};URL.revokeObjectURL=u=>{qaRevoked.push(u);revoke(u)}}` }, sessionId);
        await cdp.send('Page.navigate', { url: origin + '/teacher' }, sessionId); await wait(`Boolean(window.qaMedia&&document.querySelector('button'))`);
        if (['replace', 'cycle', 'external-return'].includes(variant)) {
          await preview('Synthetic A'); const old = await evaluate('document.querySelector("img").src');
          if (variant === 'external-return') { await replace('external'); a.equal(await evaluate(`document.querySelector('a')?.rel.includes('noreferrer')`), true); a.equal(await evaluate(`Boolean(document.querySelector('.private-material-preview'))`), false); await replace('A'); }
          else await replace('B');
          a.equal(await evaluate(`Boolean(document.querySelector('.private-material-preview'))`), false, 'Previous private preview must leave DOM when file changes');
          await wait(`qaRevoked.includes(${JSON.stringify(old)})`);
          await preview(variant === 'external-return' ? 'Synthetic A' : 'Synthetic B');
          if (variant === 'cycle') { await replace('A'); a.equal(await evaluate(`Boolean(document.querySelector('img'))`), false); await preview('Synthetic A'); }
        } else if (variant === 'pending-head' || variant === 'pending-get') {
          await click('Preview'); for (let i = 0; i < 200 && !held.length; i++) await delay(10); a.ok(held.length, 'Delayed request must be real');
          await replace('B'); await delay(50);
          a.equal(await evaluate(`document.querySelector('[role="status"]')?.textContent||''`), '', 'Old request message must not move to replacement file');
          a.equal(await evaluate(`Array.from(document.querySelectorAll('button')).find(b=>b.textContent==='Preview').disabled`), false);
          release(); await preview('Synthetic B'); a.equal(await evaluate('qaCreated.length'), 1);
        } else if (variant === 'native') {
          await click('Download'); await wait(`document.body?.textContent.includes('Download requested')`); a.equal(await evaluate(`document.querySelectorAll('iframe[name^="material-download-"]').length`), 1);
          await replace('B'); await delay(50); a.equal(await evaluate(`document.querySelectorAll('iframe[name^="material-download-"]').length`), 0);
          a.equal(await evaluate(`document.querySelector('[role="status"]')?.textContent||''`), '', 'Native message belongs only to its source file');
        } else if (variant === 'close') {
          await preview('Synthetic A'); await click('Close preview'); a.equal(await evaluate(`Boolean(document.querySelector('img'))`), false); a.equal(await evaluate('qaCreated.length===qaRevoked.length'), true); await preview('Synthetic A');
        } else if (variant === 'unmount') {
          await preview('Synthetic A'); await evaluate('qaMedia.unmount()'); await wait('qaCreated.length===qaRevoked.length'); await evaluate("qaMedia.mount('B')"); a.equal(await evaluate(`Boolean(document.querySelector('img'))`), false); await preview('Synthetic B');
        }
        a.equal(await evaluate('document.documentElement.scrollWidth<=innerWidth+1'), true);
        if (variant === 'replace') { const screenshot = await cdp.send('Page.captureScreenshot', { format: 'png' }, sessionId); fs.writeFileSync(path.join(run, 'replacement-' + width + '.png'), Buffer.from(screenshot.data, 'base64')); }
        await evaluate('qaMedia.unmount()'); await wait('qaCreated.length===qaRevoked.length'); results.push({ width, variant, pass: true });
      } catch (error) { results.push({ width, variant, pass: false, error: error.message }); }
      finally { release(); await cdp.send('Target.closeTarget', { targetId }).catch(() => {}); console.log(width + ' ' + variant + ': ' + (results.at(-1).pass ? 'PASS' : 'FAIL ' + results.at(-1).error)); }
    }
    a.deepEqual(errors, []); a.equal(results.length, 16); a.equal(results.filter(x => !x.pass).length, 0); passed = true;
  } catch (error) { failure = error.message; throw error; }
  finally {
    release(); if (cdp) await cdp.send('Browser.close').catch(() => {}); socket?.close();
    if (browser && browser.exitCode === null) { browser.kill('SIGTERM'); await Promise.race([new Promise(r => browser.once('exit', r)), delay(2000)]); if (browser.exitCode === null) browser.kill('SIGKILL'); }
    if (server?.listening) { server.closeAllConnections(); await new Promise(r => server.close(r)); }
    if (profile) try { if (path.dirname(profile) !== fs.realpathSync(os.tmpdir()) || !/^c49-material-[A-Za-z0-9]+$/.test(path.basename(profile)) || fs.realpathSync(profile) !== profile) throw Error('Refused nonowned profile cleanup'); fs.rmSync(profile, { recursive: true, force: true, maxRetries: 3, retryDelay: 100 }); } catch (error) { cleanupError = error.message; passed = false; }
    const report = { status: passed ? 'PASS' : 'FAIL', failure, results, errors, requests, sandboxDisabled: false, ownedProfileRemoved: !cleanupError, cleanupError,
      sourceSha256: Object.fromEntries(['apps/web/src/components/private-material-actions.tsx', 'apps/web/src/lib/private-material.ts'].map(p => [p, crypto.createHash('sha256').update(fs.readFileSync(path.join(root, p))).digest('hex')])),
      scope: 'Actual component/React/DOM with synthetic HTTP and bearer. No live Identity/SQL/provider/device/model qualification.' };
    fs.writeFileSync(path.join(run, 'result.json'), JSON.stringify(report, null, 2)); fs.writeFileSync(path.join(evidence, 'latest-result.json'), JSON.stringify({ run, ...report }, null, 2));
    if (cleanupError) throw Error(cleanupError);
  }
}
main().catch(error => { console.error(error.message); process.exitCode = 1; });
