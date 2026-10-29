'use strict';
// Actual api.ts in independent browser tabs and native Web Locks. Minimal local
// probe pages + synthetic intercepted APIs, NOT exported React/Identity/SQL proof.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http'), crypto = require('node:crypto'), a = require('node:assert/strict');
const ts = require('../../apps/web/node_modules/typescript');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(__dirname, '../..'), source = path.join(root, 'apps/web/src/lib/api.ts');
const helper = 'const process={env:{NEXT_PUBLIC_API_URL:location.origin}};\n' + ts.transpileModule(fs.readFileSync(source, 'utf8'), { compilerOptions: { module: ts.ModuleKind.ES2022, target: ts.ScriptTarget.ES2022 } }).outputText;
const evidence = path.join(root, 'QA/EVIDENCE', 'session-cross-tab-' + Date.now()); fs.mkdirSync(evidence, { recursive: true });
const routes = { AcademyAdmin: '/reports', Teacher: '/teacher/ai', Portal: '/portal/ai', Platform: '/platform/ai' };
const deferred = () => { let resolve; const promise = new Promise(r => resolve = r); return { promise, resolve }; };
const server = http.createServer((req, res) => {
  if (req.method !== 'GET') return res.writeHead(405).end();
  const pathname = new URL(req.url, 'http://localhost').pathname;
  if (pathname === '/qa/api.js') return res.writeHead(200, { 'Content-Type': 'application/javascript', 'Cache-Control': 'no-store' }).end(helper);
  if (!Object.values(routes).includes(pathname)) return res.writeHead(404).end();
  res.writeHead(200, { 'Content-Type': 'text/html', 'Cache-Control': 'no-store' }).end('<!doctype html><html><head><meta name="viewport" content="width=device-width, initial-scale=1"><title>Session coordination probe</title></head><body><h1>Local session coordination probe</h1><p>Synthetic records only. No academy data.</p><script type="module">window.qaApi=await import("/qa/api.js");window.qaReady=true;</script></body></html>');
});
const checks = [];
(async () => {
  let browser; await new Promise(resolve => server.listen(0, '127.0.0.1', resolve)); const origin = 'http://127.0.0.1:' + server.address().port;
  try {
    browser = await chromium.launch({ headless: true, executablePath: process.env.QA_BROWSER_EXECUTABLE || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
    for (const workspace of Object.keys(routes)) for (const scenario of ['overlap', 'late401', 'queued-abort', 'holder-close', 'replacement', 'separate-workspaces', 'unsupported', ...(workspace === 'AcademyAdmin' ? ['queue-timeout'] : [])]) {
      const context = await browser.newContext({ viewport: { width: 390, height: 844 } });
      const first = await context.newPage(), second = await context.newPage(), errors = [], calls = [], hold = deferred(), refreshObserved = deferred(), late = deferred();
      let refreshes = 0, firstInitial = 0, secondInitial = 0;
      try {
        for (const page of [first, second]) {
          page.on('pageerror', error => errors.push(error.message));
          page.on('console', message => { if (message.type() === 'error' && !/^Failed to load resource: the server responded with a status of 401 \(/.test(message.text())) errors.push(message.text()); });
        }
        await context.route('**/*', async route => {
          const request = route.request(), url = new URL(request.url()); a.equal(url.origin, origin, 'No external transport');
          if (!url.pathname.startsWith('/api/')) return route.continue();
          const tab = request.frame().page() === first ? 'first' : 'second'; calls.push({ tab, path: url.pathname, method: request.method() });
          if (url.pathname === '/api/auth/refresh') {
            refreshes++; refreshObserved.resolve();
            if (['overlap', 'late401', 'replacement'].includes(scenario)) await hold.promise;
            return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ accessToken: 'synthetic-renewed', refreshToken: 'synthetic-renewed-refresh' }) });
          }
          a.equal(url.pathname, '/api/qa/session'); a.equal(request.method(), 'POST'); a.equal(request.postData(), 'preserve-request-body');
          const status = request.headers().authorization === 'Bearer synthetic-renewed' ? 200 : 401;
          if (status === 401) {
            if (tab === 'first') firstInitial++; else secondInitial++;
            if (scenario === 'late401' && tab === 'second') await late.promise;
          }
          return route.fulfill({ status, contentType: 'application/json', body: '{}' });
        });
        await first.goto(origin + routes[workspace]); await first.waitForFunction(() => window.qaReady);
        await first.evaluate(w => { for (const role of ['AcademyAdmin', 'Teacher', 'Portal', 'Platform']) window.qaApi.beginPortalSession(role, 'synthetic-old', 'synthetic-refresh'); }, workspace);
        const secondWorkspace = scenario === 'separate-workspaces' ? (workspace === 'Teacher' ? 'AcademyAdmin' : 'Teacher') : workspace;
        await second.goto(origin + routes[secondWorkspace]); await second.waitForFunction(() => window.qaReady);
        const epoch = await first.evaluate(w => window.qaApi.portalSessionEpoch(w), workspace);
        a.equal(await first.evaluate(() => Boolean(navigator.locks?.request)), true, 'Native Web Locks required for this runner');
        if (scenario === 'unsupported') await first.evaluate(() => Object.defineProperty(navigator, 'locks', { value: undefined }));
        const start = (page, abortable = false) => page.evaluate(abortable => {
          window.qaAbort = new AbortController(); window.qaResult = null;
          window.qaApi.academyApi('/api/qa/session', { method: 'POST', body: 'preserve-request-body', ...(abortable ? { signal: window.qaAbort.signal } : {}) }).then(response => window.qaResult = response.status);
        }, abortable);
        const pendingLock = async (page, name) => page.waitForFunction(async name => (await navigator.locks.query()).pending.some(lock => lock.name === name), name);
        const lockName = 'academydesk:session-refresh:' + workspace;
        const result = async (page, expected) => { await page.waitForFunction(() => window.qaResult !== null); a.equal(await page.evaluate(() => window.qaResult), expected); };
        if (['queued-abort', 'holder-close', 'queue-timeout'].includes(scenario)) {
          await first.evaluate(name => { window.qaHolding = false; navigator.locks.request(name, () => { window.qaHolding = true; return new Promise(resolve => window.qaRelease = resolve); }); }, lockName);
          await first.waitForFunction(() => window.qaHolding); await start(second, scenario === 'queued-abort'); await pendingLock(second, lockName);
          a.equal(refreshes, 0);
          if (scenario === 'queue-timeout') {
            const started = Date.now(); await result(second, 401); const waited = Date.now() - started;
            a.ok(waited >= 8_000 && waited < 25_000, 'Native queue wait has a bounded timeout'); a.equal(refreshes, 0);
            await first.evaluate(() => window.qaRelease());
            await second.waitForFunction(async () => !(await navigator.locks.query()).pending.length);
            await start(second); await result(second, 200); a.equal(refreshes, 1);
          } else if (scenario === 'queued-abort') {
            await second.evaluate(() => window.qaAbort.abort()); await result(second, 401);
            await first.evaluate(() => window.qaRelease());
            await second.waitForFunction(async () => !(await navigator.locks.query()).pending.length);
            a.equal(refreshes, 0);
            await start(second); await result(second, 200); a.equal(refreshes, 1);
          } else { await first.close(); await result(second, 200); a.equal(refreshes, 1); }
        } else if (scenario === 'unsupported') {
          await start(first); await result(first, 200); a.equal(refreshes, 1);
        } else if (scenario === 'separate-workspaces') {
          await Promise.all([start(first), start(second)]); await result(first, 200); await result(second, 200); a.equal(refreshes, 2);
        } else {
          if (scenario === 'late401') { await start(second); await second.waitForFunction(() => window.qaResult === null); }
          await start(first); await refreshObserved.promise;
          if (scenario !== 'late401') { await start(second); await pendingLock(second, lockName); }
          a.equal(refreshes, 1);
          if (scenario === 'replacement') {
            await second.evaluate(w => window.qaApi.beginPortalSession(w, 'synthetic-old', 'synthetic-refresh'), workspace);
            a.notEqual(await second.evaluate(w => window.qaApi.portalSessionEpoch(w), workspace), epoch);
          }
          hold.resolve(); await result(first, scenario === 'replacement' ? 401 : 200);
          if (scenario === 'late401') late.resolve(); await result(second, scenario === 'replacement' ? 401 : 200);
          a.equal(refreshes, 1); a.equal(calls.filter(x => x.path === '/api/qa/session').length, scenario === 'replacement' ? 2 : 4);
          a.equal(await second.evaluate(w => window.qaApi.portalAccessToken(w), workspace), scenario === 'replacement' ? 'synthetic-old' : 'synthetic-renewed');
        }
        const remainingPage = first.isClosed() ? second : first;
        if (scenario !== 'replacement') a.equal(await remainingPage.evaluate(w => window.qaApi.portalSessionEpoch(w), workspace), epoch);
        for (const role of Object.keys(routes).filter(role => role !== workspace && role !== secondWorkspace)) a.equal(await remainingPage.evaluate(w => window.qaApi.portalAccessToken(w), role), 'synthetic-old');
        a.deepEqual(errors, []); checks.push({ workspace, scenario, passed: true, refreshes, firstInitial, secondInitial, calls, credentialsRecorded: false });
      } catch (error) {
        fs.writeFileSync(path.join(evidence, 'failed.json'), JSON.stringify({ workspace, scenario, error: error.message, errors, calls }, null, 2)); throw error;
      } finally { hold.resolve(); late.resolve(); await context.close(); }
    }
  } finally {
    fs.writeFileSync(path.join(evidence, 'result.json'), JSON.stringify({ passed: checks.length, expected: 29, checks, sourceSha256: crypto.createHash('sha256').update(fs.readFileSync(source)).digest('hex'), scope: 'native Edge tabs/Web Locks with actual helper; minimal QA probe pages, synthetic API, not exported React/SQL/device/revocation', productionApproved: false }, null, 2));
    console.log(JSON.stringify({ evidence, passed: checks.length, expected: 29 })); if (browser) await browser.close(); await new Promise(resolve => server.close(resolve));
  }
})().catch(error => { console.error(error); process.exitCode = 1; });
