// Owned Chromium contexts and synthetic intercepted loopback APIs for browser QA.
// This infrastructure never opens a real backend or substitutes for SQL/model acceptance.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path');
const os = require('node:os'), http = require('node:http');
const { spawn } = require('node:child_process');
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
const root = path.resolve(__dirname, '../..'), exported = path.join(root, 'apps/web/out');
const mime = { '.html': 'text/html', '.js': 'application/javascript', '.css': 'text/css', '.txt': 'text/plain',
  '.json': 'application/json', '.svg': 'image/svg+xml', '.ico': 'image/x-icon', '.woff2': 'font/woff2' };
class CDP {
  constructor(socket) {
    this.socket = socket; this.id = 0; this.pending = new Map(); this.handlers = new Map();
    socket.addEventListener('message', event => {
      const msg = JSON.parse(event.data);
      if (msg.id) { const p = this.pending.get(msg.id); if (p) { clearTimeout(p.timer); this.pending.delete(msg.id); msg.error ? p.reject(Error(msg.error.message)) : p.resolve(msg.result); } }
      else this.handlers.get(msg.sessionId + ':' + msg.method)?.(msg.params);
    });
  }
  send(method, params = {}, sid) {
    const id = ++this.id;
    return new Promise((resolve, reject) => {
      const timer = setTimeout(() => { this.pending.delete(id); reject(Error('CDP timeout ' + method)); }, 15000);
      this.pending.set(id, { resolve, reject, timer });
      this.socket.send(JSON.stringify({ id, method, params, ...(sid ? { sessionId: sid } : {}) }));
    });
  }
  on(sid, method, fn) { this.handlers.set(sid + ':' + method, fn); }
  forget(sid) { for (const k of this.handlers.keys()) if (k.startsWith(sid + ':')) this.handlers.delete(k); }
}
async function start({ academyId, evidence, prefix = 'penta-browser-' }) {
  const api = process.env.QA_PENTA_BROWSER_API_ORIGIN || 'http://127.0.0.1:49142';
  const u = new URL(api); a.equal(u.protocol, 'http:'); a.equal(u.hostname, '127.0.0.1'); a.equal(u.origin, api); a.ok(Number(u.port) >= 1024);
  const executable = process.env.QA_BROWSER_EXECUTABLE;
  a.ok(executable && fs.existsSync(executable), 'Select installed Chromium');
  a.ok(fs.existsSync(path.join(exported, 'penta.html')), 'Build current static application');
  a.match(prefix, /^[a-z0-9-]+-$/); fs.mkdirSync(evidence, { recursive: true });
  const run = path.join(evidence, new Date().toISOString().replaceAll(':', '-') + '-' + process.pid); fs.mkdirSync(run);
  const server = http.createServer((req, res) => {
    try {
      if (req.method !== 'GET') { res.writeHead(405); return res.end(); }
      const relative = decodeURIComponent(new URL(req.url, 'http://127.0.0.1').pathname);
      let file = path.resolve(exported, '.' + relative);
      if (!file.startsWith(exported + path.sep) && file !== exported) throw Error('Outside export');
      if (file === exported) file = path.join(exported, 'index.html'); else if (!path.extname(file)) file += '.html';
      if (!fs.existsSync(file) || !fs.statSync(file).isFile()) { res.writeHead(404); return res.end(); }
      res.writeHead(200, { 'Content-Type': mime[path.extname(file)] || 'application/octet-stream' }); fs.createReadStream(file).pipe(res);
    } catch { res.writeHead(404); res.end(); }
  });
  let child, socket, cdp, profile, listening = false, cleanupError = null;
  const contexts = new Map();
  async function close() {
    const errors = [];
    for (const dispose of [...contexts.values()]) try { await dispose(); } catch (e) { errors.push(e.message); }
    if (cdp) await cdp.send('Browser.close').catch(() => {}); socket?.close();
    if (child && child.exitCode === null) { child.kill('SIGTERM'); await Promise.race([new Promise(r => child.once('exit', r)), sleep(2000)]); }
    if (listening) { server.closeAllConnections(); await new Promise(r => server.close(r)); listening = false; }
    if (profile) try {
      if (path.dirname(profile) !== fs.realpathSync(os.tmpdir()) || !path.basename(profile).startsWith(prefix) || fs.realpathSync(profile) !== profile) throw Error('Refused nonowned profile cleanup');
      fs.rmSync(profile, { recursive: true, maxRetries: 3, retryDelay: 100 }); profile = null;
    } catch (e) { errors.push(e.message); }
    cleanupError = errors.length ? errors.join('; ') : null;
    return { sandboxDisabled: false, ownedProfileRemoved: !profile, contextsRemaining: contexts.size, cleanupError };
  }
  try {
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve)); listening = true;
    const origin = 'http://127.0.0.1:' + server.address().port;
    profile = fs.mkdtempSync(path.join(os.tmpdir(), prefix));
    child = spawn(executable, ['--headless', '--disable-background-networking', '--no-first-run', '--remote-debugging-port=0', '--user-data-dir=' + profile, 'about:blank'], { windowsHide: true, stdio: ['ignore', 'ignore', 'pipe'] });
    const endpoint = await new Promise((resolve, reject) => {
      let log = '', settled = false;
      const finish = (error, value) => {
        if (settled) return; settled = true; clearTimeout(timer); clearInterval(poll);
        fs.writeFileSync(path.join(run, 'browser-startup.txt'), log);
        if (error) reject(error); else resolve(value);
      };
      const timer = setTimeout(() => finish(Error('Chromium startup timeout')), 45000);
      // Windows Chromium can publish the endpoint through its owned profile
      // without forwarding the startup line through the launcher stderr pipe.
      const poll = setInterval(() => {
        try {
          const [port, socketPath] = fs.readFileSync(path.join(profile, 'DevToolsActivePort'), 'utf8').trim().split(/\r?\n/);
          if (/^[0-9]+$/.test(port) && Number(port) >= 1024 && Number(port) <= 65535 && /^\/devtools\/browser\/[a-zA-Z0-9-]+$/.test(socketPath))
            finish(null, `ws://127.0.0.1:${port}${socketPath}`);
        } catch { /* The owned browser has not published its endpoint yet. */ }
      }, 100);
      child.once('error', e => finish(e));
      child.once('exit', () => { if (!settled) finish(Error('Chromium exited before publishing its endpoint')); });
      child.stderr.on('data', data => { log += data; const m = log.match(/DevTools listening on (ws:\/\/[^\s]+)/); if (m) finish(null, m[1]); });
    });
    socket = new WebSocket(endpoint);
    await new Promise((resolve, reject) => { socket.addEventListener('open', resolve, { once: true }); socket.addEventListener('error', reject, { once: true }); }); cdp = new CDP(socket);
    async function page(actor, width, handler, bootstrap = '') {
      const { browserContextId } = await cdp.send('Target.createBrowserContext');
      const { targetId } = await cdp.send('Target.createTarget', { url: 'about:blank', browserContextId });
      const { sessionId: sid } = await cdp.send('Target.attachToTarget', { targetId, flatten: true });
      const requests = [], errors = [], tasks = new Set(); let closing = false;
      const token = 'synthetic-c52-' + actor.role, epoch = 'synthetic-c52-epoch';
      const evaluate = async expression => {
        const r = await cdp.send('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true }, sid);
        if (r.exceptionDetails) throw Error(r.exceptionDetails.exception?.description || r.exceptionDetails.text);
        return r.result.value;
      };
      const wait = async (expression, message = expression) => { for (let i = 0; i < 120; i++) { if (await evaluate(expression)) return; await sleep(50); } throw Error('DOM wait: ' + message); };
      const fill = async (selector, text) => {
        await wait(`Boolean(document.querySelector(${JSON.stringify(selector)}))`);
        await evaluate(`(()=>{const n=document.querySelector(${JSON.stringify(selector)});const prototype=n instanceof HTMLTextAreaElement?HTMLTextAreaElement.prototype:HTMLInputElement.prototype;Object.getOwnPropertyDescriptor(prototype,'value').set.call(n,${JSON.stringify(text)});n.dispatchEvent(new Event('input',{bubbles:true}));})()`);
      };
      const button = label => `Array.from(document.querySelectorAll('button')).find(n=>n.textContent.trim()===${JSON.stringify(label)})`;
      const click = async label => { await wait(`Boolean(${button(label)}&&!(${button(label)}).disabled)`); await evaluate(`(${button(label)}).click()`); };
      const submit = async (selector, text, label = 'Send') => { await fill(selector, text); await click(label); };
      const reload = async () => {
        const old = await evaluate('window.__c52DocumentId'); await cdp.send('Page.reload', {}, sid);
        await wait(`Boolean(window.__c52DocumentId&&window.__c52DocumentId!==${JSON.stringify(old)})`, 'a new document after reload');
      };
      async function intercept({ requestId, request }) {
        try {
          const url = new URL(request.url);
          if (url.origin === origin) return await cdp.send('Fetch.continueRequest', { requestId }, sid);
          a.equal(url.origin, api, 'Unexpected external destination');
          let response;
          if (request.method === 'OPTIONS') response = { status: 204 };
          else {
            a.equal(Object.entries(request.headers).find(([k]) => k.toLowerCase() === 'authorization')?.[1], 'Bearer ' + token);
            const req = { method: request.method, path: url.pathname, query: Object.fromEntries(url.searchParams), body: request.postData ? JSON.parse(request.postData) : undefined };
            requests.push(req);
            response = req.path === '/api/auth/session' ? { body: { workspace: actor.workspace, academyId, roles: [actor.role], isPlatformOwner: false } } : await handler(req);
            a.ok(response, 'Unexpected endpoint ' + req.method + ' ' + req.path);
          }
          if (response.fail) return await cdp.send('Fetch.failRequest', { requestId, errorReason: response.fail }, sid);
          await cdp.send('Fetch.fulfillRequest', { requestId, responseCode: response.status ?? 200, responseHeaders: [
            { name: 'Content-Type', value: 'application/json' }, { name: 'Cache-Control', value: 'no-store' },
            { name: 'Access-Control-Allow-Origin', value: origin }, { name: 'Access-Control-Allow-Headers', value: 'Authorization, Content-Type, Accept' },
            { name: 'Access-Control-Allow-Methods', value: 'GET, PUT, POST, OPTIONS' },
          ], body: response.status === 204 ? '' : Buffer.from(JSON.stringify(response.body ?? {})).toString('base64') }, sid);
        } catch (e) {
          if (!(closing && /Session with given id not found|Invalid InterceptionId|Invalid interceptionId/.test(e.message))) errors.push(e.message);
          await cdp.send('Fetch.failRequest', { requestId, errorReason: 'BlockedByClient' }, sid).catch(() => {});
        }
      }
      async function dispose() {
        closing = true; await cdp.send('Target.disposeBrowserContext', { browserContextId });
        contexts.delete(browserContextId); await Promise.all([...tasks]); cdp.forget(sid);
      }
      contexts.set(browserContextId, dispose);
      await cdp.send('Page.enable', {}, sid); await cdp.send('Runtime.enable', {}, sid);
      await cdp.send('Emulation.setDeviceMetricsOverride', { width, height: 1000, deviceScaleFactor: 1, mobile: false }, sid);
      cdp.on(sid, 'Runtime.exceptionThrown', e => errors.push(e.exceptionDetails.exception?.description || e.exceptionDetails.text));
      cdp.on(sid, 'Fetch.requestPaused', e => { const task = intercept(e); tasks.add(task); void task.finally(() => tasks.delete(task)); });
      await cdp.send('Fetch.enable', { patterns: [{ urlPattern: '*', requestStage: 'Request' }] }, sid);
      await cdp.send('Page.addScriptToEvaluateOnNewDocument', { source: `if(location.origin===${JSON.stringify(origin)}){window.__c52DocumentId=crypto.randomUUID();if(!sessionStorage.getItem('c52.initialized')){sessionStorage.setItem('c52.initialized','1');localStorage.setItem(${JSON.stringify('academydesk.accessToken.' + actor.workspace)},${JSON.stringify(token)});localStorage.setItem(${JSON.stringify('academydesk.sessionEpoch.' + actor.workspace)},${JSON.stringify(epoch)});localStorage.setItem('academydesk.theme',${JSON.stringify(width === 320 ? 'light' : 'dark')});${bootstrap}}}` }, sid);
      await cdp.send('Page.navigate', { url: origin + actor.route }, sid);
      return { evaluate, wait, fill, click, submit, reload, requests, errors, close: dispose, button,
        text: () => evaluate('document.body?.innerText||""'),
        screenshot: async name => { const r = await cdp.send('Page.captureScreenshot', { format: 'png' }, sid); fs.writeFileSync(path.join(run, name + '.png'), Buffer.from(r.data, 'base64')); },
        assertClean: async () => { await Promise.all([...tasks]); a.deepEqual(errors, []); a.equal(await evaluate('document.documentElement.scrollWidth<=innerWidth+1'), true); },
      };
    }
    return { page, close, run, sleep };
  } catch (e) { await close(); throw e; }
}
module.exports = { start, sleep };
