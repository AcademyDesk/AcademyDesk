// Real Chromium/React/DOM, intercepted synthetic auth and empty Teacher fixture only. No Identity/SQL/model acceptance.
const a = require('node:assert/strict'), fs = require('node:fs'), path = require('node:path'), os = require('node:os'), http = require('node:http');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '../..'), out = path.join(root, 'apps/web/out');
const evidence = path.join(root, '.build-check/penta-entry-browser');
const executable = process.env.QA_BROWSER_EXECUTABLE;
if (!executable || !fs.existsSync(path.join(out, 'penta.html'))) throw Error('Select Chromium and build the local static app first.');
fs.mkdirSync(evidence, { recursive: true });
const resultFile=path.join(evidence,'result.json');
if(fs.existsSync(resultFile)) fs.renameSync(resultFile,path.join(evidence,'result-previous-'+Date.now()+'.json'));
fs.writeFileSync(resultFile,JSON.stringify({status:'RUNNING'}));
const types = { '.html': 'text/html', '.js': 'application/javascript', '.css': 'text/css', '.txt': 'text/plain', '.json': 'application/json', '.woff2': 'font/woff2', '.ico': 'image/x-icon', '.svg': 'image/svg+xml' };
const server = http.createServer((req, res) => {
  try {
    if (req.method !== 'GET') { res.writeHead(405); return res.end(); }
    const relative = decodeURIComponent(new URL(req.url, 'http://127.0.0.1').pathname);
    let file = path.resolve(out, '.' + relative);
    if (!file.startsWith(out + path.sep) && file !== out) throw Error('path');
    if (file === out) file = path.join(out, 'index.html'); else if (!path.extname(file)) file += '.html';
    if (!fs.existsSync(file)) { res.writeHead(404); return res.end(); }
    res.writeHead(200, { 'Content-Type': types[path.extname(file)] || 'application/octet-stream' }); fs.createReadStream(file).pipe(res);
  } catch { res.writeHead(404); res.end(); }
});
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
class CDP {
  constructor(socket) { this.socket = socket; this.id = 0; this.pending = new Map(); this.handlers = new Map(); socket.addEventListener('message', event => { const msg = JSON.parse(event.data); if (msg.id) { const item = this.pending.get(msg.id); if (item) { clearTimeout(item.timer); this.pending.delete(msg.id); msg.error ? item.reject(Error(msg.error.message)) : item.resolve(msg.result); } } else this.handlers.get(msg.sessionId + ':' + msg.method)?.(msg.params); }); }
  send(method, params = {}, sessionId) { const id = ++this.id; return new Promise((resolve, reject) => { const timer = setTimeout(() => { this.pending.delete(id); reject(Error('CDP timeout ' + method)); }, 15000); this.pending.set(id, { resolve, reject, timer }); this.socket.send(JSON.stringify({ id, method, params, ...(sessionId ? { sessionId } : {}) })); }); }
  on(session, name, fn) { this.handlers.set(session + ':' + name, fn); }
}
async function main() {
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const origin = 'http://127.0.0.1:' + server.address().port, profile = fs.mkdtempSync(path.join(os.tmpdir(), 'c3-chromium-'));
  const child = spawn(executable, ['--headless', '--no-sandbox', '--disable-background-networking', '--no-first-run', '--remote-debugging-port=0', '--user-data-dir=' + profile, 'about:blank'], { stdio: ['ignore', 'ignore', 'pipe'] });
  let log = '', socket, cdp, runtimeErrors = [], results = [], writes = [], requests = [], success=false, failure=null;
  try {
    const endpoint = await new Promise((resolve, reject) => { const timer = setTimeout(() => reject(Error('Chromium startup timeout')), 15000); child.once('error', reject); child.stderr.on('data', data => { log += data; const match = log.match(/DevTools listening on (ws:\/\/[^\s]+)/); if (match) { clearTimeout(timer); resolve(match[1]); } }); });
    socket = new WebSocket(endpoint); await new Promise((resolve, reject) => { socket.addEventListener('open', resolve, { once: true }); socket.addEventListener('error', reject, { once: true }); }); cdp = new CDP(socket);
    const cases = [
      { workspace: 'AcademyAdmin', route: '/penta', roles: ['Owner'], manual: '/dashboard', label: 'Academy administration' },
      { workspace: 'Teacher', route: '/teacher/ai', roles: ['Teacher'], manual: '/teacher', label: 'Teacher' },
      { workspace: 'Portal', route: '/portal/ai', roles: ['Guardian'], manual: '/portal', label: 'Student / Guardian' },
      { workspace: 'Platform', route: '/platform/ai', roles: ['PlatformOwner'], manual: '/platform', label: 'Platform Owner' },
    ];
    async function page(config) {
      const { targetId } = await cdp.send('Target.createTarget', { url: 'about:blank' }); const { sessionId: sid } = await cdp.send('Target.attachToTarget', { targetId, flatten: true });
      await cdp.send('Page.enable', {}, sid); await cdp.send('Runtime.enable', {}, sid);
      await cdp.send('Emulation.setDeviceMetricsOverride', { width: config.width || 1280, height: 900, deviceScaleFactor: 1, mobile: false }, sid);
      cdp.on(sid, 'Runtime.exceptionThrown', event => runtimeErrors.push(event.exceptionDetails.text + ':' + (event.exceptionDetails.exception?.description || '')));
      cdp.on(sid, 'Runtime.consoleAPICalled', event => { if(event.type==='error') runtimeErrors.push('Console: '+event.args.map(arg=>arg.value??arg.description??'').join(' ')); });
      cdp.on(sid, 'Fetch.requestPaused', async ({ requestId, request }) => {
        try {
          const url = new URL(request.url);
          if (url.origin === origin) return await cdp.send('Fetch.continueRequest', { requestId }, sid);
          if (url.origin !== 'http://127.0.0.1:49142') return await cdp.send('Fetch.failRequest', { requestId, errorReason: 'BlockedByClient' }, sid);
          requests.push({ method: request.method, path: url.pathname });
          if (request.method === 'OPTIONS') return await fulfill(204, {});
          const authPath = url.pathname === '/api/auth/login' || url.pathname === '/api/auth/refresh';
          if (request.method !== 'GET' && !authPath) writes.push(url.pathname);
          if (url.pathname === '/api/auth/login') return await fulfill(200, { accessToken: 'synthetic-access', refreshToken: 'synthetic-refresh' });
          if (config.route==='/teacher' && url.pathname==='/api/teacher/me') return await fulfill(200,{userId:'00000000-0000-0000-0000-000000000001',firstName:'Synthetic',lastName:'Teacher',batches:[],sessions:[]});
          if (config.route==='/teacher' && ['/api/portal/announcements','/api/portal/notifications'].includes(url.pathname)) return await fulfill(200,[]);
          if (url.pathname !== '/api/auth/session') return await fulfill(401, {});
          if (config.holdProfile) await config.holdProfile;
          if (config.status) return await fulfill(config.status, {});
          return await fulfill(200, config.badProfile ? { workspace: config.workspace, roles: ['WrongRole'], isPlatformOwner: false } : { workspace: config.workspace, roles: config.roles, isPlatformOwner: config.workspace === 'Platform' });
          async function fulfill(code, body) { await cdp.send('Fetch.fulfillRequest', { requestId, responseCode: code, responseHeaders: [{ name: 'Content-Type', value: 'application/json' }, { name: 'Access-Control-Allow-Origin', value: origin }, { name: 'Access-Control-Allow-Headers', value: 'Authorization, Content-Type' }, { name: 'Access-Control-Allow-Methods', value: 'GET, POST, OPTIONS' }], body: code===204?'':Buffer.from(JSON.stringify(body)).toString('base64') }, sid); }
        } catch (error) { runtimeErrors.push('Intercept: ' + error.message); }
      });
      await cdp.send('Fetch.enable', { patterns: [{ urlPattern: '*', requestStage: 'Request' }] }, sid);
      await cdp.send('Page.addScriptToEvaluateOnNewDocument', { source: `if(location.origin===${JSON.stringify(origin)}){localStorage.clear();localStorage.setItem('academydesk.theme',${JSON.stringify(config.theme || 'dark')});localStorage.setItem(${JSON.stringify('academydesk.accessToken.' + config.workspace)},'synthetic-access');localStorage.setItem(${JSON.stringify('academydesk.refreshToken.' + config.workspace)},'synthetic-refresh');}` }, sid);
      const evaluate = async expression => { const value = await cdp.send('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true }, sid); if (value.exceptionDetails) throw Error(value.exceptionDetails.text); return value.result.value; };
      const wait = async expression => { for (let i = 0; i < 100; i++) { if (await evaluate(expression)) return; await sleep(50); } throw Error('DOM wait failed: ' + expression); };
      await cdp.send('Page.navigate', { url: origin + config.route }, sid);
      return { sid, targetId, evaluate, wait, close: () => cdp.send('Target.closeTarget', { targetId }) };
    }
    for (const item of cases) for (const width of [1280, 390]) {
      const p = await page({ ...item, width, theme: width === 390 ? 'light' : 'dark' });
      try {
        await p.wait(`document.querySelector('#penta-role-status')?.textContent.includes('conversation is not available')`);
        a.ok(await p.evaluate(`document.body.innerText.includes('Conversations not available')`));
        a.equal(await p.evaluate(`document.querySelector('a[href="${item.manual}"]')!==null`), true);
        a.equal(await p.evaluate(`document.documentElement.scrollWidth<=innerWidth+1`), true);
        a.equal(await p.evaluate(`document.querySelector('[data-penta-theme]')?.getAttribute('data-penta-theme')`), width === 390 ? 'prism' : 'orbit');
        a.equal(await p.evaluate(`document.querySelector('textarea')===null`), true);
        if (item.workspace === 'Teacher' && width === 390) { const shot = await cdp.send('Page.captureScreenshot', { format: 'png' }, p.sid); fs.writeFileSync(path.join(evidence, 'teacher-prism-mobile.png'), Buffer.from(shot.data, 'base64')); }
        results.push({ case: item.workspace + '/' + width, pass: true });
      } finally { await p.close(); }
    }
    for (const config of [{ ...cases[1], status: 401, text: 'Sign in to continue' }, { ...cases[1], badProfile: true, text: 'could not be verified' }, { ...cases[1], status: 503, text: 'verification is unavailable' }]) {
      const p = await page(config); try { await p.wait(`document.querySelector('#penta-role-status')?.textContent.includes(${JSON.stringify(config.text)})`); results.push({ case: config.text, pass: true }); } finally { await p.close(); }
    }
    {
      let release; const holdProfile = new Promise(resolve => { release = resolve; }); const start = requests.length;
      const p = await page({ ...cases[1], holdProfile }); try {
        await p.wait(`document.querySelector('#penta-role-status')?.textContent.includes('Checking')`);
        for (let i=0;i<100&&!requests.slice(start).some(r=>r.path==='/api/auth/session');i++) await sleep(25);
        a.ok(requests.slice(start).some(r=>r.path==='/api/auth/session'));
        await p.evaluate(`localStorage.setItem('academydesk.accessToken.Teacher','replacement')`); release();
        await p.wait(`document.querySelector('#penta-role-status')?.textContent.includes('verification is unavailable')`);
        await p.evaluate(`Array.from(document.querySelectorAll('button')).find(b=>b.textContent==='Check workspace again').click()`);
        await p.wait(`document.querySelector('#penta-role-status')?.textContent.includes('conversation is not available')`);
        results.push({ case: 'late profile cannot verify a replaced token; explicit recheck recovers', pass: true });
      } finally { release(); await p.close(); }
    }
    {
      const p = await page({ ...cases[1], route: '/teacher' }); try {
        await p.wait(`document.querySelector('nav[aria-label="Work view"] a[href="/teacher/ai"]')!==null && document.body.innerText.includes('Welcome, Synthetic')`);
        await p.evaluate(`new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))`);
        await p.evaluate(`{const f=document.createElement('form'),i=document.createElement('input');f.id='c3-guard-probe';f.append(i);document.body.append(f);i.value='unsaved synthetic';i.dispatchEvent(new Event('input',{bubbles:true}));document.querySelector('nav[aria-label="Work view"] a').click();}`);
        await p.wait(`Array.from(document.querySelectorAll('[role="dialog"],dialog')).some(d=>d.textContent.includes('Leave Manual'))`);
        a.equal(await p.evaluate(`location.pathname`), '/teacher');
        a.equal(await p.evaluate(`document.querySelector('dialog')?.open`), true);
        await p.evaluate(`Array.from(document.querySelectorAll('button')).find(b=>b.textContent==='Stay in Manual').click()`);
        a.equal(await p.evaluate(`document.querySelector('#c3-guard-probe input')?.value==='unsaved synthetic'`), true);
        await p.evaluate(`document.querySelector('nav[aria-label="Work view"] a').click()`);
        await p.wait(`document.querySelector('dialog')?.open===true`);
        await cdp.send('Input.dispatchKeyEvent',{type:'keyDown',key:'Escape',code:'Escape',windowsVirtualKeyCode:27},p.sid);
        await cdp.send('Input.dispatchKeyEvent',{type:'keyUp',key:'Escape',code:'Escape',windowsVirtualKeyCode:27},p.sid);
        await p.wait(`document.querySelector('dialog')===null`);
        a.equal(await p.evaluate(`document.activeElement?.getAttribute('href')`),'/teacher/ai');
        results.push({ case: 'Manual guard retains synthetic edits; native Escape restores mode-link focus', pass: true });
      } finally { await p.close(); }
    }
    a.equal(writes.length, 0, 'No business write, model or tool request is allowed');
    a.equal(requests.filter(r => /penta|inference|model|chat/.test(r.path)).length, 0);
    a.deepEqual(runtimeErrors, []);
    success=true;
    console.log(results.length + ' actual Chromium/React groups PASS; zero model/tool/business-write calls.');
  } catch(error) { failure=error.message; throw error; } finally {
    if (cdp) await cdp.send('Browser.close').catch(() => {}); socket?.close(); child.kill('SIGTERM');
    if(child.exitCode===null) await Promise.race([new Promise(resolve=>child.once('exit',resolve)),sleep(2000)]);
    server.closeAllConnections(); await new Promise(resolve => server.close(resolve));
    fs.rmSync(profile, { recursive: true, force: true });
    fs.writeFileSync(resultFile,JSON.stringify({status:success?'PASS':'FAIL',failure,results,requests,businessWrites:writes,runtimeErrors,limitation:'Synthetic auth and empty Teacher fixtures; no Identity/SQL/model/provider/physical-device qualification.'},null,2));
  }
}
main().catch(error => { console.error(error); process.exitCode = 1; });
