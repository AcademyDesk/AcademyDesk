// Exported application in a loopback browser; synthetic API only, not live SQL/auth proof.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http'), assert = require('node:assert/strict');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(__dirname, '../..'), web = fs.realpathSync(path.join(root, 'apps/web/out'));
const evidence = path.join(root, 'QA/EVIDENCE', 'enrollment-promotion-feedback-browser-' + Date.now()); fs.mkdirSync(evidence, { recursive: true });
const mime = { '.html': 'text/html', '.css': 'text/css', '.js': 'application/javascript', '.txt': 'text/plain', '.woff2': 'font/woff2', '.svg': 'image/svg+xml', '.ico': 'image/x-icon' };
const server = http.createServer((req, res) => { try {
  assert.ok(['GET', 'HEAD'].includes(req.method)); let file = path.resolve(web, '.' + decodeURIComponent(new URL(req.url, 'http://localhost').pathname)); if (!path.extname(file)) file += '.html';
  const segment = /^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file)); if (!fs.existsSync(file) && segment) { const parts = segment[1].split('.'); file = path.join(path.dirname(file), '__next.' + parts[0], ...parts.slice(1), '__PAGE__.txt'); }
  file = fs.realpathSync(file); assert.ok(file.startsWith(web + path.sep)); res.writeHead(200, { 'Content-Type': mime[path.extname(file)] || 'application/octet-stream', 'Cache-Control': 'no-store' });
  if (req.method === 'HEAD') res.end(); else fs.createReadStream(file).pipe(res);
} catch { res.writeHead(404).end(); } });
(async () => {
  await new Promise(r => server.listen(0, '127.0.0.1', r)); const origin = 'http://127.0.0.1:' + server.address().port, checks = []; let browser;
  try {
    browser = await chromium.launch({ headless: true, executablePath: process.env.QA_BROWSER_EXECUTABLE || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });

    for (const domain of ['enrollments', 'promotions']) for (const width of [320, 1440]) for (const theme of ['light', 'dark']) for (const action of (domain === 'enrollments' ? ['create', 'status'] : ['create', 'approve', 'reject'])) for (const scenario of ['normal', 'refresh-failure', 'rejected', 'uncertain-committed']) {
      const promo = domain === 'promotions', context = await browser.newContext({ viewport: { width, height: 900 } }), page = await context.newPage(), calls = [], errors = [];
      page.on('pageerror', e => errors.push(e.message)); page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
      page.on('dialog', dialog => dialog.accept(' Decision note '));
      await context.addInitScript(theme => { localStorage.setItem('academydesk.theme', theme); localStorage.setItem('academydesk.accessToken.AcademyAdmin', 'synthetic-feedback-token'); }, theme);
      const enrollments = [{ id: 'enrollment', studentId: 'student', batchId: 'source', startDate: '2026-10-01', endDate: null, status: 'Active' }];
      const promotions = [{ id: 'promotion', studentId: 'student', sourceBatchId: 'source', targetBatchId: 'target', effectiveDate: '2026-10-20', status: 'Pending' }];
      const base = '/api/academies/owned/' + (promo ? 'batch-promotions' : 'enrollments');
      await context.route('**/*', async route => {
        const req = route.request(), url = new URL(req.url()), method = req.method();
        if (url.pathname.startsWith('/api/') && ['localhost', '127.0.0.1'].includes(url.hostname)) {
          const body = req.postData() ? JSON.parse(req.postData()) : undefined; calls.push({ path: url.pathname, method, body }); const headers = { 'Access-Control-Allow-Origin': origin };
          if (['POST', 'PUT', 'PATCH'].includes(method)) {
            assert.equal(url.pathname, base + (action === 'create' ? '' : promo ? '/promotion/decision' : '/enrollment')); assert.equal(method, action === 'create' ? 'POST' : promo ? 'PATCH' : 'PUT');
            if (scenario !== 'rejected') {
              if (action === 'create') (promo ? promotions : enrollments).push({ ...body, id: 'created', ...(promo ? { status: 'Pending' } : {}) });
              else if (promo) { promotions[0].status = body.status; if (body.status === 'Approved') enrollments[0].status = 'Completed'; }
              else enrollments[0].status = body.status;
            }
            return route.fulfill({ status: scenario === 'rejected' ? 400 : scenario === 'uncertain-committed' ? 500 : 201, headers, contentType: 'application/json', body: JSON.stringify({ message: 'Synthetic policy refusal.' }) });
          }
          assert.equal(method, 'GET');
          if (scenario === 'refresh-failure' && url.pathname.startsWith('/api/academies/owned/') && calls.some(c => ['POST', 'PUT', 'PATCH'].includes(c.method))) return route.fulfill({ status: 503, headers, body: '{}' });
          const data = {
            '/api/academies': [{ id: 'owned', name: 'Synthetic academy', enabledModulesJson: '["Core","AcademicGovernance"]' }],
            '/api/auth/session': { academyId: 'owned', displayName: 'Synthetic admin', roles: ['AcademyAdmin'], isPlatformOwner: false }, '/api/portal/announcements': [],
            '/api/academies/owned/students': [{ id: 'student', firstName: 'Synthetic', lastName: 'Learner' }],
            '/api/academies/owned/batches': [{ id: 'source', name: 'Source', capacity: 10, activeEnrolments: 1 }, { id: 'target', name: 'Target', capacity: 10, activeEnrolments: 0 }],
            '/api/academies/owned/enrollments': enrollments, '/api/academies/owned/batch-promotions': promotions,
          };
          assert.ok(Object.hasOwn(data, url.pathname), url.pathname); return route.fulfill({ status: 200, headers, contentType: 'application/json', body: JSON.stringify(data[url.pathname]) });
        }
        if (url.origin === origin || url.protocol === 'data:') return route.continue(); return route.abort('blockedbyclient');
      });
      await page.goto(origin + '/' + (promo ? 'batch-promotions' : 'enrollments') + '?studentId=student');
      await page.getByRole('button', { name: promo ? 'Approve' : 'Enrol student', exact: true }).waitFor();
      const form = page.locator('form').first();
      if (promo) {
        for (const [index, label] of [[0, 'Synthetic Learner'], [1, 'Source'], [2, 'Target · 0/10']]) { await form.locator('.standard-select-trigger').nth(index).click(); await page.getByRole('option', { name: label, exact: true }).click(); }
        await form.locator('textarea').fill(' Draft note ');
      } else { await form.locator('select').nth(0).selectOption('student'); await form.locator('select').nth(1).selectOption('source'); await form.locator('select').nth(2).selectOption('Waitlisted'); }
      await form.locator('.standard-date-trigger').click(); await page.getByLabel('Select year').selectOption('2026'); await page.getByLabel('Select month').selectOption('9'); await page.locator('.standard-date-days button[data-current="true"]').filter({ hasText: /^20$/ }).click();
      if (action === 'create') await page.getByRole('button', { name: promo ? 'Request promotion' : 'Enrol student', exact: true }).click();
      else if (promo) await page.getByRole('button', { name: action === 'approve' ? 'Approve' : 'Reject', exact: true }).click();
      else { await page.locator('li select').selectOption('Paused'); await page.getByLabel('Lifecycle reason (required)', {exact:true}).fill(' Decision note '); await page.getByRole('button', {name:'Save status',exact:true}).click(); }
      const success = promo ? action === 'create' ? 'Promotion request recorded.' : action === 'approve' ? 'Promotion approved.' : 'Promotion rejected.' : action === 'create' ? 'Enrolment created.' : 'Enrolment updated.';
      const failed = ['rejected', 'uncertain-committed'].includes(scenario), notice = page.getByRole('status');
      await notice.filter({ hasText: failed ? scenario === 'rejected' ? 'Synthetic policy refusal.' : 'could not be confirmed' : success }).waitFor(); await page.waitForFunction(() => !document.querySelector('fieldset').disabled);
      const reset = !failed && action === 'create';
      assert.equal(await form.locator('input[name="' + (promo ? 'effective' : 'startDate') + '"]').inputValue(), reset ? '' : '2026-10-20');
      if (promo) { assert.equal(await form.locator('textarea').inputValue(), ' Draft note '); assert.equal(await form.locator('input[name="student"]').inputValue(), 'student'); assert.equal(await form.locator('input[name="source"]').inputValue(), 'source'); assert.equal(await form.locator('input[name="target"]').inputValue(), reset ? '' : 'target'); }
      else { assert.equal(await form.locator('select').nth(0).inputValue(), 'student'); assert.equal(await form.locator('select').nth(1).inputValue(), 'source'); assert.equal(await form.locator('select').nth(2).inputValue(), 'Waitlisted'); }
      const writes = calls.filter(c => ['POST', 'PUT', 'PATCH'].includes(c.method)); assert.equal(writes.length, 1);
      assert.deepEqual(writes[0].body, promo ? action === 'create' ? { studentId: 'student', sourceBatchId: 'source', targetBatchId: 'target', effectiveDate: '2026-10-20', notes: ' Draft note ' } : { status: action === 'approve' ? 'Approved' : 'Rejected', notes: ' Decision note ' } : action === 'create' ? { studentId: 'student', batchId: 'source', startDate: '2026-10-20', status: 'Waitlisted' } : { status: 'Paused', endDate: null, lifecycleReason: 'Decision note' });
      if (scenario === 'refresh-failure') assert.match(await notice.textContent(), /could not be refreshed.*Do not repeat/);
      assert.equal(await notice.getAttribute('aria-live'), 'polite'); await notice.scrollIntoViewIfNeeded(); assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
      const expectedError = scenario === 'rejected' ? '400' : scenario === 'refresh-failure' ? '503' : scenario === 'uncertain-committed' ? '500' : null;
      assert.deepEqual(errors.filter(x => !expectedError || !x.startsWith('Failed to load resource:') || !x.includes(expectedError)), []);
      await page.screenshot({ path: path.join(evidence, domain + '-' + width + '-' + theme + '-' + action + '-' + scenario + '.png'), fullPage: true });
      checks.push({ domain, width, theme, action, scenario, calls, notice: await notice.textContent() }); console.log('PASS ' + [domain, width, theme, action, scenario].join(' ')); await context.close();
    }
    assert.equal(checks.length, 80); fs.writeFileSync(path.join(evidence, 'result.json'), JSON.stringify({ checks, transport: 'synthetic API; not SQL/auth/tenant acceptance; enrollment status now submits lifecycleReason', physicalDevices: 'NOT RUN' }, null, 2)); console.log(JSON.stringify({ passed: checks.length, evidence }));
  } catch (e) { fs.writeFileSync(path.join(evidence, 'failed.json'), JSON.stringify({ checks, failure: e.message }, null, 2)); throw e; }
  finally { if (browser) await browser.close(); await new Promise(r => server.close(r)); }
})().catch(e => { console.error(e); process.exitCode = 1; server.close(); });
