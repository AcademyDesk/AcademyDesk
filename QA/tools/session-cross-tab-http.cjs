'use strict';
// Called only by the owned RefreshConcurrency SQL fixture. Native Identity/API
// responses in real browser tabs; probe HTML is QA-only, not the React login UI.
const fs = require('node:fs'), path = require('node:path'), crypto = require('node:crypto'), a = require('node:assert/strict');
const ts = require('../../apps/web/node_modules/typescript');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const defer = () => { let resolve; const promise = new Promise(r => resolve = r); return { promise, resolve }; };
(async () => {
  let input = ''; for await (const chunk of process.stdin) input += chunk;
  const fixture = JSON.parse(input), origin = new URL(fixture.origin);
  a.equal(origin.hostname, '127.0.0.1'); a.equal(origin.protocol, 'http:'); a.equal(origin.origin, fixture.origin);
  const source = path.resolve('apps/web/src/lib/api.ts');
  const helper = 'const process={env:{NEXT_PUBLIC_API_URL:location.origin}};\n' + ts.transpileModule(fs.readFileSync(source, 'utf8'), { compilerOptions: { module: ts.ModuleKind.ES2022, target: ts.ScriptTarget.ES2022 } }).outputText;
  const evidence = path.resolve('QA/EVIDENCE', 'session-cross-tab-native-' + Date.now()); fs.mkdirSync(evidence, { recursive: true });
  const checks = [], browser = await chromium.launch({ headless: true, executablePath: process.env.QA_BROWSER_EXECUTABLE || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  try {
    for (const [workspace, scenario] of [['AcademyAdmin', 'overlap'], ['Teacher', 'overlap'], ['AcademyAdmin', 'late401'], ['Teacher', 'expired-refresh'], ['AcademyAdmin', 'replacement']]) {
      const item = fixture.sessions.find(x => x.workspace === workspace); a.ok(item);
      const context = await browser.newContext(), first = await context.newPage(), second = await context.newPage();
      const calls = [], errors = [], refreshSeen = defer(), holdRefresh = defer(), holdLate = defer(), lateSeen = defer();
      try {
        for (const page of [first, second]) page.on('pageerror', () => errors.push('unexpected browser exception'));
        await context.route('**/*', async route => {
          const request = route.request(), url = new URL(request.url()); a.equal(url.origin, origin.origin);
          if (url.pathname === '/qa/api.js') return route.fulfill({ contentType: 'application/javascript', body: helper });
          if (['/reports', '/teacher/ai'].includes(url.pathname)) return route.fulfill({ contentType: 'text/html', body: '<!doctype html><html><head><title>Owned native session probe</title></head><body><h1>Disposable native session probe</h1><script type="module">window.qaApi=await import("/qa/api.js");window.qaReady=true;</script></body></html>' });
          a.ok(['/api/auth/session', '/api/auth/refresh'].includes(url.pathname));
          const response = await route.fetch({ timeout: 20_000, maxRetries: 0, maxRedirects: 0 });
          calls.push({ tab: request.frame().page() === first ? 'first' : 'second', path: url.pathname, method: request.method(), status: response.status() });
          a.notEqual(response.status(), 429, 'No limiter relaxation or429 acceptance');
          if (url.pathname === '/api/auth/refresh') { refreshSeen.resolve(); if (scenario !== 'expired-refresh') await holdRefresh.promise; }
          else if (response.status() === 401 && request.frame().page() === second && scenario === 'late401') { lateSeen.resolve(); await holdLate.promise; }
          await route.fulfill({ response });
        });
        const route = workspace === 'Teacher' ? '/teacher/ai' : '/reports';
        await first.goto(origin.origin + route); await first.waitForFunction(() => window.qaReady);
        await first.evaluate(({ workspace, access, refresh }) => window.qaApi.beginPortalSession(workspace, access, refresh), { workspace, access: item.expiredAccess, refresh: scenario === 'expired-refresh' ? item.expiredRefresh : item.refreshToken });
        await second.goto(origin.origin + route); await second.waitForFunction(() => window.qaReady);
        a.equal(await first.evaluate(() => Boolean(navigator.locks?.request)), true);
        const epoch = await first.evaluate(w => window.qaApi.portalSessionEpoch(w), workspace);
        const start = page => page.evaluate(() => {
          window.qaResult = null;
          window.qaApi.academyApi('/api/auth/session').then(async response => {
            const profile = response.ok ? await response.json() : null;
            // Retain only exact scope predicates, never credentials or profile PII.
            window.qaResult = { status: response.status, workspace: profile?.workspace, academyId: profile?.academyId };
          });
        });
        const result = async (page, expected) => {
          await page.waitForFunction(() => window.qaResult !== null);
          const value = await page.evaluate(() => window.qaResult); a.equal(value.status, expected);
          if (expected === 200) { a.equal(value.workspace, workspace); a.equal(value.academyId, fixture.academyId); }
        };
        if (scenario === 'late401') { await start(second); await lateSeen.promise; }
        await start(first); await refreshSeen.promise;
        if (scenario !== 'late401') {
          await start(second);
          if (scenario !== 'expired-refresh') await second.waitForFunction(async name => (await navigator.locks.query()).pending.some(lock => lock.name === name), 'academydesk:session-refresh:' + workspace);
        }
        if (scenario === 'replacement') await second.evaluate(({ workspace, access, refresh }) => window.qaApi.beginPortalSession(workspace, access, refresh), { workspace, access: item.expiredAccess, refresh: item.refreshToken });
        holdRefresh.resolve(); await result(first, ['expired-refresh', 'replacement'].includes(scenario) ? 401 : 200);
        holdLate.resolve(); await result(second, ['expired-refresh', 'replacement'].includes(scenario) ? 401 : 200);
        const refreshes = calls.filter(x => x.path === '/api/auth/refresh'), reads = calls.filter(x => x.path === '/api/auth/session');
        a.equal(refreshes.length, scenario === 'expired-refresh' ? 2 : 1);
        a.ok(refreshes.every(x => x.status === (scenario === 'expired-refresh' ? 401 : 200)));
        a.equal(reads.filter(x => x.status === 401).length, 2);
        a.equal(reads.filter(x => x.status === 200).length, ['expired-refresh', 'replacement'].includes(scenario) ? 0 : 2);
        if (scenario === 'replacement') a.notEqual(await second.evaluate(w => window.qaApi.portalSessionEpoch(w), workspace), epoch);
        else a.equal(await second.evaluate(w => window.qaApi.portalSessionEpoch(w), workspace), epoch);
        a.deepEqual(errors, []); const check = { workspace, scenario, passed: true, requests: calls.length, refreshes: refreshes.length, calls, unexpectedPageErrors: errors.length, credentialsRecorded: false };
        checks.push(check); console.log('REFRESHLOCK BROWSER HTTP ' + JSON.stringify(check));
      } finally { holdRefresh.resolve(); holdLate.resolve(); await context.close(); }
    }
    a.equal(checks.length, 5); a.equal(checks.reduce((sum, x) => sum + x.requests, 0), 22);
  } finally {
    await browser.close();
    fs.writeFileSync(path.join(evidence, 'result.json'), JSON.stringify({ checks, passed: checks.length, expected: 5, sourceSha256: crypto.createHash('sha256').update(fs.readFileSync(source)).digest('hex'), scope: 'actual api.ts/native Edge Web Locks/native disposable Identity+SQL HTTP; minimal probe pages, not React login/physical-device/server revocation acceptance', productionApproved: false }, null, 2));
    console.log('REFRESHLOCK BROWSER RECEIPT ' + JSON.stringify({ evidence, passed: checks.length }));
  }
})().catch(() => { console.error('REFRESHLOCK BROWSER FAIL native assertion/transport; credentials omitted'); process.exitCode = 1; });
