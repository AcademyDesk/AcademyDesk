// Production-export DOM + synthetic transport. No live SQL, model, customer data or physical-device claim.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http'), assert = require('node:assert/strict');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(__dirname, '../..'), web = fs.realpathSync(path.join(root, 'apps/web/out'));
const evidence = path.join(root, 'QA/EVIDENCE', 'music-feedback-browser-' + Date.now()); fs.mkdirSync(evidence, { recursive: true });
const server = http.createServer((req, res) => { try { assert.ok(['GET', 'HEAD'].includes(req.method)); let file = path.resolve(web, '.' + decodeURIComponent(new URL(req.url, 'http://localhost').pathname)); if (!path.extname(file)) file += '.html'; const segment = /^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file)); if (!fs.existsSync(file) && segment) { const parts = segment[1].split('.'); file = path.join(path.dirname(file), '__next.' + parts[0], ...parts.slice(1), '__PAGE__.txt'); } file = fs.realpathSync(file); assert.ok(file.startsWith(web + path.sep)); res.writeHead(200, { 'Content-Type': ({ '.html': 'text/html', '.css': 'text/css', '.js': 'application/javascript', '.txt': 'text/plain', '.woff2': 'font/woff2', '.ico': 'image/x-icon' })[path.extname(file)] || 'application/octet-stream' }); if (req.method === 'HEAD') res.end(); else fs.createReadStream(file).pipe(res); } catch { res.writeHead(404).end(); } });
(async () => {
  await new Promise(r => server.listen(0, '127.0.0.1', r)); const origin = 'http://127.0.0.1:' + server.address().port, checks = []; let browser;
  try {
    browser = await chromium.launch({ headless: true, executablePath: process.env.QA_BROWSER_EXECUTABLE || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
    for (const width of [320, 1440]) for (const theme of ['light', 'dark']) for (const kind of ['piece', 'assign', 'update']) for (const scenario of ['normal', 'refresh-failure', 'rejected', 'uncertain-committed']) {
      const context = await browser.newContext({ viewport: { width, height: 1000 } }), page = await context.newPage(), errors = [], writes = [];
      let release, sawWrite, recovering = false; const held = new Promise(r => release = r), started = new Promise(r => sawWrite = r);
      const stored = { id: 'progress', studentId: 'student', musicPieceId: 'piece', status: 'Assigned', targetDate: '2026-10-20', score: 0, notes: 'Keep synthetic notes' };
      const pieces = [{ id: 'piece', title: 'Synthetic piece', difficulty: 'Beginner' }], progress = [stored];
      page.on('pageerror', e => errors.push(e.message)); page.on('console', m => { if (m.type() === 'error' && !/(400|500|503) \((Bad Request|Internal Server Error|Service Unavailable)\)/.test(m.text())) errors.push(m.text()); });
      await context.addInitScript(theme => { localStorage.setItem('academydesk.theme', theme); localStorage.setItem('academydesk.accessToken.AcademyAdmin', 'synthetic-music-feedback-token'); }, theme);
      await context.route('**/*', async route => {
        const req = route.request(), url = new URL(req.url());
        if (url.pathname.startsWith('/api/') && ['localhost', '127.0.0.1'].includes(url.hostname)) {
          const headers = { 'Access-Control-Allow-Origin': origin };
          if (['POST', 'PATCH'].includes(req.method())) {
            assert.equal(req.method(), kind === 'update' ? 'PATCH' : 'POST'); assert.equal(url.pathname, '/api/academies/owned/' + (kind === 'piece' ? 'music-pieces' : 'music-progress' + (kind === 'update' ? '/progress' : '')));
            const body = req.postDataJSON(); writes.push(body); sawWrite(); await held;
            if (scenario !== 'rejected') { if (kind === 'piece') pieces.push({ ...body, id: 'created' }); else if (kind === 'assign') progress.push({ ...body, id: 'created', status: 'Assigned' }); else Object.assign(stored, body); }
            return route.fulfill({ status: scenario === 'rejected' ? 400 : scenario === 'uncertain-committed' ? 500 : kind === 'update' ? 200 : 201, headers, contentType: 'application/json', body: '{}' });
          }
          assert.equal(req.method(), 'GET');
          if (url.pathname.endsWith('/music-progress') && writes.length && scenario === 'refresh-failure' && !recovering) return route.fulfill({ status: 503, headers, body: '{}' });
          const data = { '/api/academies': [{ id: 'owned', name: 'Synthetic academy', enabledModulesJson: '["Core","AcademicGovernance"]' }], '/api/auth/session': { academyId: 'owned', displayName: 'Synthetic admin', roles: ['AcademyAdmin'], isPlatformOwner: false }, '/api/portal/announcements': [], '/api/academies/owned/students': [{ id: 'student', firstName: 'Synthetic', lastName: 'Learner' }], '/api/academies/owned/music-pieces': pieces, '/api/academies/owned/music-progress': progress };
          assert.ok(Object.hasOwn(data, url.pathname), url.pathname); return route.fulfill({ status: 200, headers, contentType: 'application/json', body: JSON.stringify(data[url.pathname]) });
        }
        if (url.origin === origin || url.protocol === 'data:') return route.continue(); errors.push('Unexpected external request'); return route.abort('blockedbyclient');
      });
      await page.goto(origin + '/music'); const row = page.locator('.music-register li').first(); await row.waitFor(); await page.getByPlaceholder('Piece title').fill('Draft piece'); await page.getByPlaceholder('Optional composer').fill('Draft composer');
      await page.locator('.standard-date-trigger').click(); await page.getByLabel('Select year').selectOption('2026'); await page.getByLabel('Select month').selectOption('10'); await page.locator('.standard-date-days button[data-current="true"]').filter({ hasText: /^2$/ }).click();
      const selectedDate = await page.locator('input[name="target"]').inputValue(); assert.equal(selectedDate, '2026-11-02');
      if (kind === 'update') { await row.getByRole('button', { name: 'Assigned', exact: true }).click(); await page.getByRole('option', { name: 'Mastered', exact: true }).click(); }
      else await page.getByRole('button', { name: kind === 'piece' ? 'Add piece' : 'Assign piece', exact: true }).click();
      await started; assert.equal(await page.getByRole('button', { name: 'Refresh workspace', exact: true }).isDisabled(), true); assert.equal(await row.getByRole('button', { name: 'Assigned', exact: true }).isDisabled(), true);
      assert.equal(await page.locator('form fieldset:disabled').count(), 2); await page.evaluate(() => document.querySelectorAll('form').forEach(f => f.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true })))); release();
      const success = kind === 'piece' ? 'Repertoire piece added.' : kind === 'assign' ? 'Piece assigned to student.' : 'Progress updated to Mastered.';
      const notice = page.locator('.music-message'); await notice.filter({ hasText: scenario === 'uncertain-committed' ? 'could not be confirmed' : scenario === 'rejected' ? 'could not' : success }).waitFor();
      assert.equal(writes.length, 1); const expected = kind === 'piece' ? { title: 'Draft piece', composer: 'Draft composer', instrument: 'Piano', genre: null, difficulty: 'Beginner', durationMinutes: null } : kind === 'assign' ? { studentId: 'student', musicPieceId: 'piece', targetDate: selectedDate, notes: null } : { status: 'Mastered', targetDate: '2026-10-20', score: 0, notes: 'Keep synthetic notes' }; assert.deepEqual(writes[0], expected);
      if (['refresh-failure', 'uncertain-committed'].includes(scenario)) {
        await notice.filter({ hasText: scenario === 'refresh-failure' ? 'do not repeat' : 'before retrying' }).waitFor(); assert.equal(await page.getByRole('button', { name: 'Add piece', exact: true }).isDisabled(), true);
        recovering = true; await page.getByRole('button', { name: 'Refresh workspace', exact: true }).click(); await notice.filter({ hasText: 'workspace refreshed' }).waitFor();
      }
      await page.waitForFunction(() => !document.querySelector('form fieldset')?.disabled); assert.equal(await notice.getAttribute('role'), 'status'); assert.equal(await notice.getAttribute('aria-live'), 'polite');
      const confirmed = ['normal', 'refresh-failure'].includes(scenario); assert.equal(await page.getByPlaceholder('Piece title').inputValue(), confirmed && kind === 'piece' ? '' : 'Draft piece'); assert.equal(await page.locator('input[name="target"]').inputValue(), confirmed && kind === 'assign' ? '' : selectedDate);
      if (kind === 'update') await row.getByRole('button', { name: scenario === 'rejected' ? 'Assigned' : 'Mastered', exact: true }).waitFor();
      assert.equal(writes.length, 1); assert.deepEqual(errors, []); assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
      if (kind === 'update' && scenario === 'normal') await page.screenshot({ path: path.join(evidence, `${width}-${theme}.png`), fullPage: true });
      checks.push({ width, theme, kind, scenario, writes: writes.length }); await context.close(); console.log(`PASS MUSIC FEEDBACK ${width} ${theme} ${kind} ${scenario}`);
    }
    fs.writeFileSync(path.join(evidence, 'result.json'), JSON.stringify({ passed: checks.length, checks }, null, 2)); console.log('MUSIC FEEDBACK BROWSER PASS ' + checks.length + ' evidence=' + evidence);
  } finally { if (browser) await browser.close(); await new Promise(r => server.close(r)); }
})().catch(error => { fs.writeFileSync(path.join(evidence, 'failure.txt'), error.stack); console.error(error); process.exitCode = 1; });
