// Actual exported React application in sandboxed Chromium; all API replies are synthetic.
// No model, native SQL, account credentials or device qualification is involved.
const a = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const os = require('node:os');
const http = require('node:http');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '../..');
const web = path.join(root, 'apps/web/out');
const api = process.env.QA_PENTA_BROWSER_API_ORIGIN || 'http://127.0.0.1:49142';
const parsed = new URL(api);
a.equal(parsed.protocol, 'http:'); a.equal(parsed.hostname, '127.0.0.1');
a.equal(parsed.origin, api); a.ok(Number(parsed.port) >= 1024);
const executable = process.env.QA_BROWSER_EXECUTABLE;
a.ok(executable && fs.existsSync(executable), 'Select installed Chromium');
a.ok(fs.existsSync(path.join(web, 'penta.html')), 'Build the exact current static application first');
const evidence = path.join(root, '.build-check/penta-conversation-resilience-browser');
fs.mkdirSync(evidence, { recursive: true });
const run = path.join(evidence, new Date().toISOString().replaceAll(':', '-') + '-' + process.pid);
fs.mkdirSync(run);
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
const guid = n => String(n).padStart(8, '0') + '-1111-4111-8111-111111111111';
const academy = guid(1), learner = guid(2), lesson = guid(3), batch = guid(4);
const asOf = '2026-11-18T09:00:00Z', future = '2026-11-19T09:00:00Z';
const sentinel = 'C51 verified synthetic record';
const actors = [
  { role: 'AcademyAdmin', workspace: 'AcademyAdmin', route: '/penta', manual: '/dashboard', kind: 'academy' },
  { role: 'Owner', workspace: 'AcademyAdmin', route: '/penta', manual: '/dashboard', kind: 'academy' },
  { role: 'Teacher', workspace: 'Teacher', route: '/teacher/ai', manual: '/teacher', kind: 'teacher' },
  { role: 'Student', workspace: 'Portal', route: '/portal/ai', manual: '/portal', kind: 'portal' },
  { role: 'Guardian', workspace: 'Portal', route: '/portal/ai', manual: '/portal', kind: 'portal' },
  { role: 'PlatformOwner', workspace: 'Platform', route: '/platform/ai', manual: '/platform', kind: 'platform' },
];
const scenarios = ['verified', 'empty', 'denied', 'offline', 'retry', 'foreign-scope', 'malformed',
  'wrong-content-type', 'invalid-json', 'cacheable', 'oversized', 'epoch-late', 'token-late',
  'sign-out', 'cross-tab-sign-out', 'unrelated-session', 'unsupported', 'readiness-offline',
  'readiness-cacheable', 'duplicate-submit'];
const configFor = actor => ({
  academy: { input: '#finance-chat-prompt', prompt: 'Find student Mira', title: 'Matching academy records',
    primary: '/api/academies/' + academy + '/penta/student-search', ready: '/api/academies/' + academy + '/penta/lookup-readiness' },
  teacher: { input: '#penta-attendance-prompt', prompt: 'Mark attendance for Piano', title: 'Assigned class matches',
    primary: '/api/teacher/penta/attendance/sessions', ready: '/api/teacher/penta/attendance/readiness' },
  portal: { input: '#portal-penta-prompt', prompt: 'When is my next class?', title: 'Next class',
    primary: '/api/portal/penta/students/' + learner + '/next-class', ready: '/api/portal/penta/learners' },
  platform: { input: '#platform-penta-prompt', prompt: 'Show support queue', title: 'Support queue',
    primary: '/api/platform/penta/support', ready: '/api/platform/penta/readiness' },
}[actor.kind]);
const mime = { '.html': 'text/html', '.js': 'application/javascript', '.css': 'text/css', '.json': 'application/json',
  '.txt': 'text/plain', '.svg': 'image/svg+xml', '.ico': 'image/x-icon', '.woff2': 'font/woff2' };
const server = http.createServer((req, res) => {
  try {
    if (req.method !== 'GET') { res.writeHead(405); return res.end(); }
    const relative = decodeURIComponent(new URL(req.url, 'http://127.0.0.1').pathname);
    if (relative === '/__qa_empty.html') { res.writeHead(200, { 'Content-Type': 'text/html' }); return res.end('<!doctype html><title>Owned storage probe</title>'); }
    let file = path.resolve(web, '.' + relative);
    if (!file.startsWith(web + path.sep) && file !== web) throw Error('Outside static export');
    if (file === web) file = path.join(web, 'index.html');
    else if (!path.extname(file)) file += '.html';
    if (!fs.existsSync(file) || !fs.statSync(file).isFile()) { res.writeHead(404); return res.end(); }
    res.writeHead(200, { 'Content-Type': mime[path.extname(file)] || 'application/octet-stream' });
    fs.createReadStream(file).pipe(res);
  } catch { res.writeHead(404); res.end(); }
});
class CDP {
  constructor(socket) {
    this.socket = socket; this.id = 0; this.pending = new Map(); this.handlers = new Map();
    socket.addEventListener('message', event => {
      const msg = JSON.parse(event.data);
      if (msg.id) {
        const item = this.pending.get(msg.id);
        if (item) { clearTimeout(item.timer); this.pending.delete(msg.id); msg.error ? item.reject(Error(msg.error.message)) : item.resolve(msg.result); }
      } else this.handlers.get(msg.sessionId + ':' + msg.method)?.(msg.params);
    });
  }
  send(method, params = {}, sessionId) {
    const id = ++this.id;
    return new Promise((resolve, reject) => {
      const timer = setTimeout(() => { this.pending.delete(id); reject(Error('CDP timeout ' + method)); }, 15000);
      this.pending.set(id, { resolve, reject, timer });
      this.socket.send(JSON.stringify({ id, method, params, ...(sessionId ? { sessionId } : {}) }));
    });
  }
  on(sid, method, fn) { this.handlers.set(sid + ':' + method, fn); }
  forget(sid) { for (const key of this.handlers.keys()) if (key.startsWith(sid + ':')) this.handlers.delete(key); }
}
function primaryValue(kind, empty = false) {
  if (kind === 'academy') return { academyId: academy, tool: 'student.search.v1', capability: 'executor', source: 'Students',
    maxRows: 10, hasMore: false, asOfUtc: asOf, timeZone: 'UTC', rows: empty ? [] : [
      { sourceId: learner, displayName: sentinel, isActive: true, sourcePath: '/student-management?studentId=' + learner }] };
  if (kind === 'teacher') return { academyId: academy, sessionId: null, hasMore: false, rows: empty ? [] : [
    { id: lesson, batchId: batch, batchName: sentinel, startUtc: future }] };
  if (kind === 'portal') return { academyId: academy, studentId: learner, kind: 'next-class', asOfUtc: asOf, sourcePath: '/portal',
    rows: empty ? [] : [{ batchName: sentinel, startUtc: future, endUtc: '2026-11-19T10:00:00Z', deliveryMode: 'InPerson' }] };
  return { scope: 'PlatformOwner', kind: 'support', asOfUtc: asOf, sourcePath: '/platform/control',
    metrics: { open: empty ? 0 : 7, highOrUrgent: empty ? 0 : 2 } };
}
async function main() {
  let child, socket, cdp, profile, origin, started = false, failure = null, cleanupError = null;
  const results = [];
  fs.writeFileSync(path.join(run, 'result.json'), JSON.stringify({ status: 'RUNNING', scope: 'Synthetic API only' }));
  try {
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve)); started = true;
    origin = 'http://127.0.0.1:' + server.address().port;
    profile = fs.mkdtempSync(path.join(os.tmpdir(), 'c51-conversation-'));
    child = spawn(executable, ['--headless', '--disable-background-networking', '--no-first-run', '--remote-debugging-port=0', '--user-data-dir=' + profile, 'about:blank'], { stdio: ['ignore', 'ignore', 'pipe'] });
    const endpoint = await new Promise((resolve, reject) => {
      let log = ''; const timer = setTimeout(() => reject(Error('Chromium startup timeout')), 15000);
      child.once('error', error => { clearTimeout(timer); reject(error); });
      child.stderr.on('data', data => { log += data; const m = log.match(/DevTools listening on (ws:\/\/[^\s]+)/); if (m) { clearTimeout(timer); resolve(m[1]); } });
    });
    socket = new WebSocket(endpoint);
    await new Promise((resolve, reject) => { socket.addEventListener('open', resolve, { once: true }); socket.addEventListener('error', reject, { once: true }); });
    cdp = new CDP(socket);
    async function check(actor, width, scenario) {
      const conf = configFor(actor), name = actor.role + '/' + width + '/' + scenario;
      const { browserContextId } = await cdp.send('Target.createBrowserContext');
      const { targetId } = await cdp.send('Target.createTarget', { url: 'about:blank', browserContextId });
      const { sessionId: sid } = await cdp.send('Target.attachToTarget', { targetId, flatten: true });
      const requests = [], errors = [], pendingHandlers = new Set();
      const access = 'synthetic-c51-' + actor.role, epoch = 'synthetic-c51-epoch';
      let primaryCount = 0, held = null, closing = false;
      const value = async expression => {
        const r = await cdp.send('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true }, sid);
        if (r.exceptionDetails) throw Error(r.exceptionDetails.exception?.description || r.exceptionDetails.text);
        return r.result.value;
      };
      const wait = async expression => {
        for (let i = 0; i < 120; i++) { if (await value(expression)) return; await sleep(50); }
        throw Error('DOM wait: ' + expression);
      };
      const shown = `Boolean(document.querySelector('section[aria-label="${conf.title}"]'))`;
      const ready = `Boolean(document.querySelector('${conf.input}'))`;
      const ask = async text => {
        await value(`(()=>{const n=document.querySelector(${JSON.stringify(conf.input)});if(!n)throw Error('Composer absent');Object.getOwnPropertyDescriptor(HTMLInputElement.prototype,'value').set.call(n,${JSON.stringify(text)});n.dispatchEvent(new Event('input',{bubbles:true}));})()`);
        await wait(`!document.querySelector('${conf.input}').closest('form').querySelector('button[type="submit"]').disabled`);
        await value(`document.querySelector('${conf.input}').closest('form').requestSubmit()`);
      };
      const reply = async (requestId, status, data, opts = {}) => {
        const headers = [{ name: 'Content-Type', value: opts.contentType || 'application/json' },
          { name: 'Cache-Control', value: opts.cached ? 'public, max-age=60' : 'no-store' },
          { name: 'Access-Control-Allow-Origin', value: origin }, { name: 'Access-Control-Allow-Headers', value: 'Authorization, Content-Type, Accept' },
          { name: 'Access-Control-Allow-Methods', value: 'GET, OPTIONS' }];
        const raw = opts.raw ?? JSON.stringify(data);
        await cdp.send('Fetch.fulfillRequest', { requestId, responseCode: status, responseHeaders: headers,
          body: status === 204 ? '' : Buffer.from(raw).toString('base64') }, sid);
      };
      async function intercept({ requestId, request }) {
        try {
          const url = new URL(request.url);
          if (url.origin === origin) return await cdp.send('Fetch.continueRequest', { requestId }, sid);
          a.equal(url.origin, api, 'Unexpected external destination');
          if (request.method === 'OPTIONS') return await reply(requestId, 204, {});
          requests.push({ method: request.method, path: url.pathname, query: url.search });
          a.equal(request.method, 'GET', 'Read matrix must never send a mutation');
          a.equal(Object.entries(request.headers).find(([k]) => k.toLowerCase() === 'authorization')?.[1], 'Bearer ' + access);
          if (url.pathname === '/api/auth/session') return await reply(requestId, 200, {
            workspace: actor.workspace, academyId: academy, roles: [actor.role], isPlatformOwner: actor.kind === 'platform' });
          if (url.pathname === conf.primary) {
            primaryCount++;
            if (actor.kind === 'academy') a.equal(url.searchParams.get('q'), 'Mira');
            if (actor.kind === 'teacher') a.equal(url.searchParams.get('q'), 'Piano');
            let data = primaryValue(actor.kind, scenario === 'empty');
            if (scenario === 'foreign-scope') data = { ...data, ...(actor.kind === 'platform' ? { scope: 'AcademyAdmin' } : { academyId: guid(99) }) };
            if (scenario === 'malformed') {
              if (actor.kind === 'platform') data.metrics.highOrUrgent = 8;
              else if (actor.kind === 'academy') data.rows[0].sourcePath = '/platform/control';
              else data.rows[0].privateEmail = 'never-display@example.invalid';
            }
            const opts = { cached: scenario === 'cacheable', contentType: scenario === 'wrong-content-type' ? 'text/html' : undefined,
              raw: scenario === 'invalid-json' ? '{invalid-json' : scenario === 'oversized' ? ' '.repeat(66000) + JSON.stringify(data) : undefined };
            const status = scenario === 'denied' ? 403 : scenario === 'offline' || scenario === 'retry' && primaryCount === 1 ? 503 : 200;
            if (['epoch-late', 'token-late', 'duplicate-submit'].includes(scenario)) {
              a.equal(primaryCount, 1, 'Only one request may be pending');
              held = () => reply(requestId, status, data, opts); return;
            }
            return await reply(requestId, status, data, opts);
          }
          if (url.pathname === conf.ready) {
            if (scenario === 'readiness-offline') return await reply(requestId, 503, {});
            const data = actor.kind === 'academy' ? { protocol: 'academy-lookup.v1', academyId: academy }
              : actor.kind === 'teacher' ? { protocol: 'teacher-attendance.v1', academyId: academy, requiresConfirmation: true }
                : actor.kind === 'portal' ? { protocol: 'portal-reads.v1', academyId: academy, hasMore: false,
                  learners: [{ id: learner, name: 'Synthetic learner', academic: true, finance: true }] }
                  : { protocol: 'platform-reads.v1', scope: 'PlatformOwner' };
            return await reply(requestId, 200, data, { cached: scenario === 'readiness-cacheable' });
          }
          if (actor.kind === 'academy' && [
            '/api/academies/' + academy + '/penta/chat/finance',
            '/api/academies/' + academy + '/penta/class-scheduling/browser/readiness',
          ].includes(url.pathname)) return await reply(requestId, 404, {});
          throw Error('Unexpected endpoint ' + url.pathname);
        } catch (error) {
          if (!(closing && /Session with given id not found|Invalid InterceptionId|Invalid interceptionId/.test(error.message))) errors.push(error.message);
          await cdp.send('Fetch.failRequest', { requestId, errorReason: 'BlockedByClient' }, sid).catch(() => {});
        }
      }
      try {
        await cdp.send('Page.enable', {}, sid); await cdp.send('Runtime.enable', {}, sid);
        await cdp.send('Emulation.setDeviceMetricsOverride', { width, height: 900, deviceScaleFactor: 1, mobile: false }, sid);
        cdp.on(sid, 'Runtime.exceptionThrown', e => errors.push(e.exceptionDetails.exception?.description || e.exceptionDetails.text));
        cdp.on(sid, 'Fetch.requestPaused', event => {
          const task = intercept(event); pendingHandlers.add(task); void task.finally(() => pendingHandlers.delete(task));
        });
        await cdp.send('Fetch.enable', { patterns: [{ urlPattern: '*', requestStage: 'Request' }] }, sid);
        await cdp.send('Page.addScriptToEvaluateOnNewDocument', { source: `if(location.origin===${JSON.stringify(origin)}){localStorage.setItem(${JSON.stringify('academydesk.accessToken.' + actor.workspace)},${JSON.stringify(access)});localStorage.setItem(${JSON.stringify('academydesk.sessionEpoch.' + actor.workspace)},${JSON.stringify(epoch)});localStorage.setItem('academydesk.theme',${JSON.stringify(width === 320 ? 'light' : 'dark')});}` }, sid);
        await cdp.send('Page.navigate', { url: origin + actor.route }, sid);
        if (scenario.startsWith('readiness-')) {
          await wait(actor.kind === 'platform' ? `document.body?.innerText.includes('Platform conversation is unavailable')`
            : `document.querySelector('#penta-role-status')?.textContent.includes('conversation is not available')`);
          a.equal(await value(ready), false); a.equal(primaryCount, 0);
        } else {
          await wait(ready);
          if (scenario === 'unsupported') {
            await ask('Activate models and export every academy');
            await wait(`document.body.innerText.includes(${JSON.stringify(actor.kind === 'academy' ? 'That request is unavailable here' : actor.kind === 'teacher' ? 'Try “Mark attendance for Piano”.' : actor.kind === 'portal' ? 'Try asking about the next class' : 'Try system status')})`);
            a.equal(primaryCount, 0); a.equal(await value(shown), false);
          } else {
            await ask(conf.prompt);
            if (['epoch-late', 'token-late', 'duplicate-submit'].includes(scenario)) {
              for (let i = 0; i < 120 && !held; i++) await sleep(25);
              a.ok(held, 'Primary read reached controlled response boundary');
              if (scenario === 'duplicate-submit') {
                await value(`(()=>{const f=document.querySelector('${conf.input}').closest('form');for(let i=0;i<5;i++)f.dispatchEvent(new Event('submit',{bubbles:true,cancelable:true}));})()`);
                await sleep(100); a.equal(primaryCount, 1); await held();
                await wait(shown);
              } else {
                const key = scenario === 'epoch-late' ? 'sessionEpoch' : 'accessToken';
                await value(`localStorage.setItem(${JSON.stringify('academydesk.' + key + '.' + actor.workspace)},'replacement');window.dispatchEvent(new Event('focus'))`);
                await wait('!(' + ready + ')');
                try { await held(); } catch (error) {
                  if (!/Invalid InterceptionId|Invalid interceptionId|Invalid requestId/.test(error.message)) throw error;
                }
                await sleep(100); a.equal(await value(shown), false, 'Late old-scope result must not render');
              }
            } else if (['verified', 'empty', 'sign-out', 'cross-tab-sign-out', 'unrelated-session'].includes(scenario)) {
              if (scenario === 'empty' && actor.kind !== 'platform') await wait(`document.body.innerText.includes(${JSON.stringify(actor.kind === 'academy' ? 'No matching records' : actor.kind === 'teacher' ? 'No assigned class matched' : 'No matching records')})`);
              else await wait(shown);
              if (scenario === 'empty') {
                a.equal(await value(`document.body.innerText.includes(${JSON.stringify(sentinel)})`), false);
                if (actor.kind === 'platform') a.equal(await value(`Array.from(document.querySelectorAll('section[aria-label="Support queue"] dd')).every(n=>n.textContent==='0')`), true);
              } else if (actor.kind !== 'platform') a.equal(await value(`document.body.innerText.includes(${JSON.stringify(sentinel)})`), true);
              if (scenario === 'sign-out') {
                await value(`window.dispatchEvent(new CustomEvent('academydesk:portal-sign-out',{detail:${JSON.stringify(actor.workspace)}}))`);
                await wait('!(' + ready + ')'); a.equal(await value(shown), false);
              }
              if (scenario === 'cross-tab-sign-out') {
                const other = await cdp.send('Target.createTarget', { url: origin + '/__qa_empty.html', browserContextId });
                const { sessionId: otherSid } = await cdp.send('Target.attachToTarget', { targetId: other.targetId, flatten: true });
                try {
                  for (let i = 0; i < 100; i++) {
                    const v = await cdp.send('Runtime.evaluate', { expression: 'location.origin', returnByValue: true }, otherSid);
                    if (v.result.value === origin) break; await sleep(25);
                  }
                  await cdp.send('Runtime.evaluate', { expression: `localStorage.removeItem(${JSON.stringify('academydesk.accessToken.' + actor.workspace)})` }, otherSid);
                  await wait('!(' + ready + ')'); a.equal(await value(shown), false);
                } finally { await cdp.send('Target.closeTarget', { targetId: other.targetId }); }
              }
              if (scenario === 'unrelated-session') {
                const other = actor.workspace === 'Portal' ? 'Teacher' : 'Portal';
                await value(`window.dispatchEvent(new CustomEvent('academydesk:portal-session-changed',{detail:${JSON.stringify(other)}}))`);
                await sleep(100); a.equal(await value(ready), true); a.equal(await value(shown), true);
              }
            } else {
              const closes = scenario === 'denied' && ['teacher', 'platform'].includes(actor.kind);
              if (closes) await wait('!(' + ready + ')');
              else await wait(`document.body.innerText.includes(${JSON.stringify(actor.kind === 'teacher' ? 'Attendance could not be verified' : actor.kind === 'portal' && scenario === 'denied' ? 'Access to this learner or information has changed' : 'could not verify')})`);
              a.equal(await value(shown), false, 'Unverified records must not render');
              a.equal(await value(`document.body.innerText.includes(${JSON.stringify(sentinel)})`), false);
              if (scenario === 'retry') { await ask(conf.prompt); await wait(shown); a.equal(primaryCount, 2); }
            }
            if (scenario !== 'retry') a.equal(primaryCount, 1);
          }
        }
        const ended = ['epoch-late', 'sign-out', 'cross-tab-sign-out'].includes(scenario);
        if (ended) {
          a.equal(await value(`document.querySelector('#peer-signout-title')?.textContent`), 'Session ended');
          a.equal(await value(`Boolean(document.querySelector('a[href="/login"]'))`), true, 'Ended session requires fresh sign-in');
          a.equal(await value(ready), false); a.equal(await value(shown), false);
        } else a.equal(await value(`Boolean(document.querySelector('a[href="${actor.manual}"]'))`), true, 'Authorized Manual fallback link remains');
        a.equal(await value('document.documentElement.scrollWidth<=innerWidth+1'), true, 'No horizontal page overflow');
        if (!ended) a.equal(await value(`document.querySelector('main[data-penta-theme]')?.getAttribute('data-penta-theme')`), width === 320 ? 'prism' : 'orbit');
        a.equal(requests.some(r => r.method !== 'GET'), false);
        await Promise.all([...pendingHandlers]); a.deepEqual(errors, []);
        if (scenario === 'verified') {
          const shot = await cdp.send('Page.captureScreenshot', { format: 'png' }, sid);
          fs.writeFileSync(path.join(run, actor.role + '-' + width + '.png'), Buffer.from(shot.data, 'base64'));
        }
        results.push({ case: name, pass: true, primaryCount, requests, errors });
      } catch (error) {
        results.push({ case: name, pass: false, error: error.message, primaryCount, requests, errors });
        fs.writeFileSync(path.join(run, name.replaceAll('/', '-') + '.txt'), await value('document.body.innerText').catch(() => 'Page unavailable'));
      } finally {
        closing = true;
        await cdp.send('Target.disposeBrowserContext', { browserContextId }).catch(error => errors.push('Context cleanup: ' + error.message));
        await Promise.all([...pendingHandlers]); cdp.forget(sid);
        if (errors.length && results.at(-1)?.case === name) results.at(-1).pass = false;
        fs.writeFileSync(path.join(run, 'progress.json'), JSON.stringify(results, null, 2));
        console.log(name + ': ' + (results.find(x => x.case === name).pass ? 'PASS' : 'FAIL'));
      }
    }
    // Sequential contexts prevent profile/fixture sharing; every role gets a fresh context.
    const filter = process.env.QA_C51_CASE_FILTER ? new RegExp(process.env.QA_C51_CASE_FILTER) : null;
    const planned = actors.flatMap(actor => [1280, 320].flatMap(width => scenarios.map(scenario => ({ actor, width, scenario }))))
      .filter(({ actor, width, scenario }) => !filter || filter.test(actor.role + '/' + width + '/' + scenario));
    a.ok(planned.length, 'The selected matrix must contain cases');
    for (const { actor, width, scenario } of planned) await check(actor, width, scenario);
    a.equal(results.length, planned.length);
    a.equal(results.filter(r => !r.pass).length, 0, 'All browser cases must pass');
  } catch (error) { failure = error.message; }
  finally {
    if (cdp) await cdp.send('Browser.close').catch(() => {}); socket?.close();
    if (child && child.exitCode === null) { child.kill('SIGTERM'); await Promise.race([new Promise(resolve => child.once('exit', resolve)), sleep(2000)]); }
    if (started) { server.closeAllConnections(); await new Promise(resolve => server.close(resolve)); }
    if (profile) try {
      if (path.dirname(profile) !== fs.realpathSync(os.tmpdir()) || !/^c51-conversation-[A-Za-z0-9]+$/.test(path.basename(profile)) || fs.realpathSync(profile) !== profile) throw Error('Refused nonowned profile cleanup');
      fs.rmSync(profile, { recursive: true, maxRetries: 3, retryDelay: 100 });
    } catch (error) { cleanupError = error.message; }
    const result = { status: failure || cleanupError ? 'FAIL' : 'PASS', failure, cleanupError, sandboxDisabled: false,
      ownedProfileRemoved: !cleanupError, cases: results.length, passed: results.filter(x => x.pass).length,
      failed: results.filter(x => !x.pass).length, results,
      filter: process.env.QA_C51_CASE_FILTER || null,
      scope: 'Real Chromium/exported React app with synthetic intercepted API only; no live Identity/SQL/model/device qualification.' };
    fs.writeFileSync(path.join(run, 'result.json'), JSON.stringify(result, null, 2));
    console.log(JSON.stringify({ status: result.status, cases: result.cases, passed: result.passed, failed: result.failed, evidence: run }));
    if (failure || cleanupError) throw Error(failure || cleanupError);
  }
}
main().catch(error => { console.error(error.message); process.exitCode = 1; });
