// Exported application in a loopback browser; synthetic API only, not live SQL/auth proof.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http'), assert = require('node:assert/strict');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(__dirname, '../..'), web = fs.realpathSync(path.join(root, 'apps/web/out'));
const evidence = path.join(root, 'QA/EVIDENCE', 'governance-periods-feedback-browser-' + Date.now()); fs.mkdirSync(evidence, { recursive: true });
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

    for (const domain of ['governance', 'periods']) for (const width of [320, 1440]) for (const theme of ['light', 'dark']) for (const action of (domain === 'governance' ? ['scheme', 'prerequisite', 'status'] : ['year', 'term', 'closeYear', 'closeTerm'])) for (const scenario of ['normal', 'refresh-failure', 'rejected', 'uncertain-committed']) {
      const gov = domain === 'governance', context = await browser.newContext({ viewport: { width, height: 900 } }), page = await context.newPage(), calls = [], errors = [];
      page.on('pageerror', e => errors.push(e.message)); page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
      await context.addInitScript(theme => { localStorage.setItem('academydesk.theme', theme); localStorage.setItem('academydesk.accessToken.AcademyAdmin', 'synthetic-feedback-token'); }, theme);
      const schemes = [{ id: 'scheme', name: 'Synthetic scheme', passingPercent: 0, bandsJson: '[]', isActive: true }], prerequisites = [];
      const years = [{ id: 'year', name: 'Synthetic year', startDate: '2026-09-01', endDate: '2027-08-31', isCurrent: true, isClosed: false }];
      const terms = [{ id: 'term', academicYearId: 'year', name: 'Synthetic term', startDate: '2026-10-01', endDate: '2026-12-31', isClosed: false }];
      const expectedPath = { scheme: '/academic-governance/grading-schemes', prerequisite: '/academic-governance/prerequisites', status: '/grading-schemes/scheme/status', year: '/academic-periods/years', term: '/academic-periods/terms', closeYear: '/academic-periods/years/year/close', closeTerm: '/academic-periods/terms/term/close' }[action];
      await context.route('**/*', async route => {
        const req = route.request(), url = new URL(req.url()), method = req.method();
        if (url.pathname.startsWith('/api/') && ['localhost', '127.0.0.1'].includes(url.hostname)) {
          const raw = req.postData(), body = raw ? JSON.parse(raw) : undefined; calls.push({ path: url.pathname, method, body });
          const headers = { 'Access-Control-Allow-Origin': origin };
          if (['POST', 'PATCH'].includes(method)) {
            assert.equal(url.pathname, '/api/academies/owned' + expectedPath); assert.equal(method, ['scheme', 'prerequisite', 'year', 'term'].includes(action) ? 'POST' : 'PATCH');
            if (scenario !== 'rejected') {
              if (action === 'scheme') schemes.push({ ...body, id: 'created', isActive: true });
              if (action === 'prerequisite') prerequisites.push({ ...body, id: 'created' });
              if (action === 'status') schemes[0].isActive = body.isActive;
              if (action === 'year') years.push({ ...body, id: 'created', isClosed: false });
              if (action === 'term') terms.push({ ...body, id: 'created', isClosed: false });
              if (action === 'closeYear') { years[0].isClosed = true; terms[0].isClosed = true; }
              if (action === 'closeTerm') terms[0].isClosed = true;
            }
            return route.fulfill({ status: scenario === 'rejected' ? 400 : scenario === 'uncertain-committed' ? 500 : 201, headers, contentType: 'application/json', body: JSON.stringify({ message: 'Synthetic policy refusal.' }) });
          }
          assert.equal(method, 'GET');
          if (scenario === 'refresh-failure' && url.pathname.startsWith('/api/academies/owned/') && calls.some(c => ['POST', 'PATCH'].includes(c.method))) return route.fulfill({ status: 503, headers, body: '{}' });
          const data = {
            '/api/academies': [{ id: 'owned', name: 'Synthetic academy', enabledModulesJson: '["Core","Sales","AcademicGovernance"]' }],
            '/api/auth/session': { academyId: 'owned', displayName: 'Synthetic admin', roles: ['AcademyAdmin'], isPlatformOwner: false }, '/api/portal/announcements': [],
            '/api/academies/owned/courses': [{ id: 'course', name: 'Synthetic course' }, { id: 'required', name: 'Synthetic required' }],
            '/api/academies/owned/academic-governance/grading-schemes': schemes,
            '/api/academies/owned/academic-governance/prerequisites': prerequisites,
            '/api/academies/owned/academic-periods': { years, terms },
          };
          assert.ok(Object.hasOwn(data, url.pathname), url.pathname); return route.fulfill({ status: 200, headers, contentType: 'application/json', body: JSON.stringify(data[url.pathname]) });
        }
        if (url.origin === origin || url.protocol === 'data:') return route.continue(); return route.abort('blockedbyclient');
      });
      await page.goto(origin + '/academic-' + domain); await page.getByRole('button', { name: gov ? 'Deactivate' : 'Close year', exact: true }).waitFor();
      const forms = page.locator('form'), first = forms.nth(0), second = forms.nth(1);
      await first.locator('input[name="name"]').fill(' Draft first '); await second.locator('input[name="name"]').count().then(async count => { if (count) await second.locator('input[name="name"]').fill(' Draft second '); });
      if (gov) {
        await first.locator('input[name="passingPercent"]').fill('0'); await first.locator('textarea').fill('');
        for (const [index, label] of [[0, 'Synthetic course'], [1, 'Synthetic required']]) { await second.locator('.standard-select-trigger').nth(index).click(); await page.getByRole('option', { name: label, exact: true }).click(); }
      } else {
        await first.locator('input[name="isCurrent"]').check();
        await second.locator('.standard-select-trigger').click(); await page.getByRole('option', { name: 'Synthetic year', exact: true }).click();
        for (const [form, name, month, day] of [[first, 'year-start', 8, 1], [first, 'year-end', 11, 31], [second, 'term-start', 9, 1], [second, 'term-end', 11, 31]]) {
          await form.locator('.standard-date-field').filter({ has: page.locator('input[name="' + name + '"]') }).locator('.standard-date-trigger').click();
          await page.getByLabel('Select year').selectOption('2026'); await page.getByLabel('Select month').selectOption(String(month));
          await page.locator('.standard-date-days button[data-current="true"]').filter({ hasText: new RegExp('^' + day + '$') }).click();
        }
      }
      const labels = { scheme: 'Create scheme', prerequisite: 'Save prerequisite', status: 'Deactivate', year: 'Create year', term: 'Create term', closeYear: 'Close year', closeTerm: 'Close term' };
      await page.getByRole('button', { name: labels[action], exact: true }).click();
      const failed = ['rejected', 'uncertain-committed'].includes(scenario), success = { scheme: 'Grading scheme created.', prerequisite: 'Course prerequisite saved.', status: 'Grading scheme made inactive.', year: 'Academic year created.', term: 'Term created.', closeYear: 'Period closed and retained for audit.', closeTerm: 'Period closed and retained for audit.' }[action], notice = page.getByRole('status');
      await notice.filter({ hasText: failed ? scenario === 'rejected' ? 'Synthetic policy refusal.' : 'could not be confirmed' : success }).waitFor(); await page.waitForFunction(() => !document.querySelector('fieldset').disabled);
      const resetFirst = !failed && ['scheme', 'year'].includes(action), resetSecond = !failed && ['prerequisite', 'term'].includes(action);
      assert.equal(await first.locator('input[name="name"]').inputValue(), resetFirst ? '' : ' Draft first ');
      if (gov) {
        assert.equal(await first.locator('input[name="passingPercent"]').inputValue(), resetFirst ? '' : '0');
        assert.equal(await first.locator('textarea').inputValue(), resetFirst ? '[]' : '');
        for (const [name, value] of [['courseId', 'course'], ['requiredCourseId', 'required']]) assert.equal(await second.locator('input[name="' + name + '"]').inputValue(), resetSecond ? '' : value);
      } else {
        assert.equal(await second.locator('input[name="name"]').inputValue(), resetSecond ? '' : ' Draft second ');
        assert.equal(await first.locator('input[name="isCurrent"]').isChecked(), !resetFirst);
        for (const [form, name, value, reset] of [[first, 'year-start', '2026-09-01', resetFirst], [first, 'year-end', '2026-12-31', resetFirst], [second, 'academicYearId', 'year', resetSecond], [second, 'term-start', '2026-10-01', resetSecond], [second, 'term-end', '2026-12-31', resetSecond]]) assert.equal(await form.locator('input[name="' + name + '"]').inputValue(), reset ? '' : value);
      }
      const writes = calls.filter(c => ['POST', 'PATCH'].includes(c.method)); assert.equal(writes.length, 1);
      const expectedBody = { scheme: { name: ' Draft first ', passingPercent: 0, bandsJson: '[]' }, prerequisite: { courseId: 'course', requiredCourseId: 'required' }, status: { isActive: false }, year: { name: ' Draft first ', startDate: '2026-09-01', endDate: '2026-12-31', isCurrent: true }, term: { academicYearId: 'year', name: ' Draft second ', startDate: '2026-10-01', endDate: '2026-12-31' }, closeYear: undefined, closeTerm: undefined }[action];
      assert.deepEqual(writes[0].body, expectedBody);
      if (scenario === 'refresh-failure') assert.match(await notice.textContent(), /could not be refreshed.*Do not repeat/);
      assert.equal(await notice.getAttribute('aria-live'), 'polite'); await notice.scrollIntoViewIfNeeded();
      assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
      const expectedError = scenario === 'rejected' ? '400' : scenario === 'refresh-failure' ? '503' : scenario === 'uncertain-committed' ? '500' : null;
      assert.deepEqual(errors.filter(x => !expectedError || !x.startsWith('Failed to load resource:') || !x.includes(expectedError)), []);
      await page.screenshot({ path: path.join(evidence, domain + '-' + width + '-' + theme + '-' + action + '-' + scenario + '.png'), fullPage: true });
      checks.push({ domain, width, theme, action, scenario, calls, notice: await notice.textContent() }); console.log('PASS ' + [domain, width, theme, action, scenario].join(' ')); await context.close();
    }
    assert.equal(checks.length, 112); fs.writeFileSync(path.join(evidence, 'result.json'), JSON.stringify({ checks, transport: 'synthetic API; not SQL/auth/tenant acceptance', physicalDevices: 'NOT RUN' }, null, 2)); console.log(JSON.stringify({ passed: checks.length, evidence }));
  } catch (e) { fs.writeFileSync(path.join(evidence, 'failed.json'), JSON.stringify({ checks, failure: e.message }, null, 2)); throw e; }
  finally { if (browser) await browser.close(); await new Promise(r => server.close(r)); }
})().catch(e => { console.error(e); process.exitCode = 1; server.close(); });
