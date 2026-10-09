// Product static export + explicitly SYNTHETIC intercepted API responses.
// No login, real model, SQL, credentials, customer data or external network.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http'), assert = require('node:assert/strict');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(__dirname, '../..'), web = path.join(root, 'apps/web/out');
const evidence = path.join(root, 'QA/EVIDENCE', `penta-chat-contract-${Date.now()}`);
assert.ok(fs.existsSync(path.join(web, 'penta.html')), 'Build the actual frontend export first');
fs.mkdirSync(evidence, { recursive: true });
const academy = '11111111-1111-4111-8111-111111111111', conversation = '22222222-2222-4222-8222-222222222222', student = '33333333-3333-4333-8333-333333333333';
const mime = { '.html': 'text/html', '.js': 'application/javascript', '.css': 'text/css', '.txt': 'text/plain', '.json': 'application/json', '.woff2': 'font/woff2', '.svg': 'image/svg+xml', '.ico': 'image/x-icon' };
const server = http.createServer((req, res) => {
  if (!['GET', 'HEAD'].includes(req.method)) { res.writeHead(405).end(); return; }
  try {
    let file = path.resolve(web, '.' + decodeURIComponent(new URL(req.url, 'http://localhost').pathname));
    if (!file.startsWith(web + path.sep)) throw Error('Path outside export');
    if (!path.extname(file)) file += '.html';
    const segment = /^__next\.([a-zA-Z0-9_-]+)\.__PAGE__\.txt$/.exec(path.basename(file));
    if (!fs.existsSync(file) && segment) file = path.join(path.dirname(file), '__next.' + segment[1], '__PAGE__.txt');
    file = fs.realpathSync(file);
    assert.ok(file.startsWith(web + path.sep));
    res.writeHead(200, { 'Content-Type': mime[path.extname(file)] || 'application/octet-stream', 'Cache-Control': 'no-store' });
    if (req.method === 'HEAD') res.end(); else fs.createReadStream(file).pipe(res);
  } catch { res.writeHead(404).end(); }
});
(async () => {
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const origin = `http://127.0.0.1:${server.address().port}`;
  const browser = await chromium.launch({ headless: true, executablePath: process.env.QA_BROWSER_EXECUTABLE || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  const checks = [], errors = [], expectedHttpErrors = [];
  let lastPage;
  try {
    const scenarios = ['valid', 'null-row', 'null-balance', 'bad-filters', 'wrong-capability', 'rows-on-error', 'bad-session', 'bad-timezone', 'expired', 'recovery', 'network-interruption', 'stale-version', 'replayed-receipt', 'other-tab-logout', 'same-tab-logout', 'stopped-waiting'];
    for (let i = 0; i < scenarios.length; i++) {
      const scenario = scenarios[i], width = [320, 390, 1440][i % 3];
      const context = await browser.newContext({ viewport: { width, height: 1000 } });
      let creates = 0, turns = 0, firstRequestId, releaseTurn;
      let turnArrived;
      const turnStarted = new Promise(resolve => { turnArrived = resolve; });
      const page = await context.newPage();
      lastPage = page;
      page.on('pageerror', e => errors.push({ scenario, message: e.message }));
      page.on('console', m => {
        if (m.type() !== 'error') return;
        // Chromium logs the deliberately returned 410 even when UI handles it.
        // Retain this exact expected transport log, not arbitrary console errors.
        if (((scenario === 'expired' && m.text() === 'Failed to load resource: the server responded with a status of 410 (Gone)') ||
             (scenario === 'stale-version' && m.text() === 'Failed to load resource: the server responded with a status of 409 (Conflict)') ||
             (scenario === 'network-interruption' && m.text().includes('net::ERR_FAILED'))) &&
            new URL(m.location().url).pathname.endsWith('/turns')) expectedHttpErrors.push({ scenario, message: m.text() });
        else errors.push({ scenario, message: m.text() });
      });
      await context.addInitScript(({ theme }) => {
        localStorage.setItem('academydesk.theme', theme);
        localStorage.setItem('academydesk.accessToken.AcademyAdmin', 'synthetic-test-token');
        window.addEventListener('academydesk:portal-sign-out', event => sessionStorage.setItem('qa-current-tab-signout', event.detail));
      }, { theme: i % 2 ? 'dark' : 'light' });
      await context.route('**/*', async route => {
        const url = new URL(route.request().url());
        if (url.pathname.startsWith('/api/') && ['localhost', '127.0.0.1'].includes(url.hostname)) {
          const response = async (value, status = 200) => route.fulfill({ status, contentType: 'application/json', headers: { 'Access-Control-Allow-Origin': origin }, body: JSON.stringify(value) });
          if (url.pathname === '/api/auth/session') return response({ academyId: academy, displayName: 'Synthetic admin', roles: ['AcademyAdmin'], isPlatformOwner: false });
          if (url.pathname === '/api/academies') return response([{ name: 'Synthetic academy', enabledModulesJson: '["Core","Finance"]' }]);
          if (url.pathname === '/api/portal/announcements') return response([]);
          if (url.pathname.endsWith('/academy-context')) return response({ academyId: academy, name: 'Synthetic academy', timeZone: scenario === 'bad-timezone' ? 'Invalid/Zone' : 'Asia/Kolkata' });
          if (url.pathname.endsWith('/chat/health')) return response({ status: 'Available', provider: 'PENTA Mini', protocol: '0.1', readOnly: true });
          if (url.pathname.endsWith('/chat/conversations')) { creates++; return response(scenario === 'bad-session' ? null : { conversationId: conversation, version: 0, expiresAtUtc: '2099-10-08T10:00:00Z' }, 201); }
          if (url.pathname.endsWith('/turns')) {
            turns++;
            turnArrived();
            if (scenario === 'expired') return response({ error: 'Synthetic expired session' }, 410);
            if (scenario === 'stale-version' && turns === 1) return response({ error: 'Synthetic stale version' }, 409);
            if (scenario === 'network-interruption' && turns === 1) return route.abort('failed');
            if (['other-tab-logout', 'same-tab-logout'].includes(scenario) || scenario === 'stopped-waiting' && turns === 1) {
              await new Promise(resolve => { releaseTurn = resolve; });
              return response({ error: 'Synthetic late reply' }, 409).catch(() => undefined);
            }
            const input = route.request().postDataJSON();
            const receipt = { conversationId: conversation, requestId: input.requestId, version: input.expectedVersion + 1, kind: 'RESULT', message: 'Synthetic verified read.', capability: input.capability, provider: 'PENTA Mini', protocol: '0.1', context: { filters: { balance_status: 'Pending' }, sort_by: null, current_learner_id: null, current_result_ids: [student] }, result: { count: 1, hasMore: false, rows: [{ sourceId: student, displayName: 'Synthetic learner', recordCode: 'SYN-001', subjects: ['Piano'], balances: [{ currency: 'INR', outstanding: 400 }], sourcePath: `/student-management?studentId=${student}` }], asOfUtc: '2026-10-08T10:00:00Z', source: 'Synthetic ledger', balanceScope: 'Synthetic student fees.' } };
            const broken = scenario === 'recovery' ? turns === 1 : true;
            if (scenario === 'null-row' || scenario === 'recovery' && broken) receipt.result.rows = [null];
            if (scenario === 'null-balance') receipt.result.rows[0].balances = [null];
            if (scenario === 'bad-filters') receipt.context.filters = null;
            if (scenario === 'wrong-capability') receipt.capability = 'twin';
            if (scenario === 'rows-on-error') receipt.kind = 'ERROR';
            if (scenario === 'replayed-receipt') {
              if (turns === 1) firstRequestId = input.requestId;
              else receipt.requestId = firstRequestId;
            }
            return response(receipt, 201);
          }
          throw Error('Unexpected synthetic API path ' + url.pathname);
        }
        if (url.origin === origin || url.protocol === 'data:') return route.continue();
        return route.abort('blockedbyclient');
      });
      await page.goto(origin + '/penta');
      const panel = page.getByRole('region', { name: 'PENTA conversation' });
      if (scenario === 'bad-timezone') {
        await panel.getByRole('alert').filter({ hasText: 'Academy context could not be verified' }).waitFor();
        assert.equal(await panel.getByLabel('Message PENTA AI', { exact: true }).isDisabled(), true);
        assert.equal(creates + turns, 0);
      } else {
        await page.getByText('● Mini connected').waitFor();
        const send = async () => { await panel.getByLabel('Message PENTA AI', { exact: true }).fill('Synthetic read request'); await panel.getByRole('button', { name: 'Send ↑', exact: true }).click(); };
        await send();
        if (scenario === 'stopped-waiting') {
          await turnStarted;
          await panel.getByRole('button', { name: 'Stop waiting' }).click();
          await panel.getByRole('alert').filter({ hasText: 'will not be sent again automatically' }).waitFor();
          assert.equal(await panel.getByLabel('Message PENTA AI', { exact: true }).inputValue(), 'Synthetic read request');
          releaseTurn?.();
          await page.waitForTimeout(100);
          assert.equal(await panel.getByRole('link', { name: 'Open Student 360 ↗' }).count(), 0);
          assert.equal(turns, 1);
          await panel.getByRole('button', { name: 'New conversation', exact: true }).click();
          await send();
          await panel.getByRole('link', { name: 'Open Student 360 ↗' }).waitFor();
          assert.equal(creates, 2); assert.equal(turns, 2);
        } else if (['other-tab-logout', 'same-tab-logout'].includes(scenario)) {
          await turnStarted;
          if (scenario === 'other-tab-logout') {
            await page.evaluate(() => window.dispatchEvent(new StorageEvent('storage', { key: 'academydesk.accessToken.AcademyAdmin', oldValue: 'synthetic', newValue: null, storageArea: localStorage })));
            await page.getByRole('heading', { name: 'Session ended' }).waitFor();
            assert.equal(await page.getByRole('link', { name: 'Open Student 360 ↗' }).count(), 0);
            assert.equal(await page.getByLabel('Message PENTA AI', { exact: true }).count(), 0);
          } else {
            await page.getByRole('button', { name: 'Open account menu' }).click();
            await page.getByRole('button', { name: 'Sign out', exact: true }).click();
            await page.waitForURL('**/login');
            assert.equal(await page.evaluate(() => localStorage.getItem('academydesk.accessToken.AcademyAdmin')), null);
            assert.equal(await page.evaluate(() => sessionStorage.getItem('qa-current-tab-signout')), 'AcademyAdmin');
          }
          releaseTurn?.();
          await page.waitForTimeout(100);
          assert.equal(await page.getByRole('link', { name: 'Open Student 360 ↗' }).count(), 0);
        } else if (scenario === 'valid') {
          await panel.getByRole('link', { name: 'Open Student 360 ↗' }).waitFor();
          assert.equal(await panel.getByRole('link', { name: 'Open Student 360 ↗' }).getAttribute('href'), `/student-management?studentId=${student}`);
        } else if (scenario === 'replayed-receipt') {
          await panel.getByRole('link', { name: 'Open Student 360 ↗' }).waitFor();
          await send();
          await panel.getByRole('alert').filter({ hasText: 'could not be verified' }).waitFor();
          assert.equal(await panel.locator('article').count(), 1);
          assert.equal(turns, 2);
        } else {
          await panel.getByRole('alert').waitFor();
          assert.equal(await panel.getByLabel('Message PENTA AI', { exact: true }).isDisabled(), true);
          assert.equal(await panel.getByRole('link', { name: 'Open Student 360 ↗' }).count(), 0);
          assert.equal(turns, scenario === 'bad-session' ? 0 : 1);
          if (scenario === 'expired') assert.match(await panel.getByRole('alert').innerText(), /conversation expired/i);
          if (['network-interruption', 'stale-version'].includes(scenario)) {
            assert.equal(await panel.getByLabel('Message PENTA AI', { exact: true }).inputValue(), 'Synthetic read request');
            await page.waitForTimeout(100);
            assert.equal(creates, 1); assert.equal(turns, 1);
            await panel.getByRole('button', { name: 'New conversation', exact: true }).click();
            await send();
            await panel.getByRole('link', { name: 'Open Student 360 ↗' }).waitFor();
            assert.equal(creates, 2); assert.equal(turns, 2);
          }
          if (scenario === 'recovery') {
            await panel.getByRole('button', { name: 'New conversation', exact: true }).click();
            await send();
            await panel.getByRole('link', { name: 'Open Student 360 ↗' }).waitFor();
            assert.equal(creates, 2); assert.equal(turns, 2);
          }
        }
      }
      assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), 'No horizontal overflow');
      if (['valid', 'null-row', 'null-balance'].includes(scenario)) await page.screenshot({ path: path.join(evidence, `${scenario}-${width}.png`), fullPage: true });
      checks.push({ scenario, width, theme: i % 2 ? 'dark' : 'light', creates, turns, synthetic: true });
      console.log('PASS SYNTHETIC ' + scenario + ' ' + width);
      await context.close();
    }
    assert.deepEqual(errors, [], 'No browser console or rendering errors');
    assert.equal(expectedHttpErrors.length, 3, 'Retain only the deliberate 410, 409 and network transport logs');
    fs.writeFileSync(path.join(evidence, 'result.json'), JSON.stringify({ checks, errors, expectedHttpErrors, liveMini: 'NOT RUN', sql: 'NOT RUN', physicalDevices: 'NOT RUN' }, null, 2));
    console.log(JSON.stringify({ passed: checks.length, evidence }));
  } catch (error) {
    if (lastPage && !lastPage.isClosed()) await lastPage.screenshot({ path: path.join(evidence, 'failure.png'), fullPage: true }).catch(() => {});
    fs.writeFileSync(path.join(evidence, 'failed.json'), JSON.stringify({ checks, errors, expectedHttpErrors, failure: error.message }, null, 2));
    throw error;
  } finally { await browser.close(); await new Promise(resolve => server.close(resolve)); }
})().catch(error => { console.error(error); process.exitCode = 1; server.close(); });
