// Exported MusicPage and actual controls; synthetic transport, not SQL/device acceptance.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http'), assert = require('node:assert/strict');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const root = path.resolve(__dirname, '../..'), web = fs.realpathSync(path.join(root, 'apps/web/out')), evidence = path.join(root, 'QA/EVIDENCE', 'music-progress-zero-' + Date.now()); fs.mkdirSync(evidence, { recursive: true });
const server = http.createServer((req, res) => {
  try {
    assert.ok(['GET', 'HEAD'].includes(req.method)); let file = path.resolve(web, '.' + decodeURIComponent(new URL(req.url, 'http://localhost').pathname)); if (!path.extname(file)) file += '.html';
    const segment = /^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file)); if (!fs.existsSync(file) && segment) { const parts = segment[1].split('.'); file = path.join(path.dirname(file), '__next.' + parts[0], ...parts.slice(1), '__PAGE__.txt'); }
    file = fs.realpathSync(file); assert.ok(file.startsWith(web + path.sep)); res.writeHead(200, { 'Content-Type': ({ '.html': 'text/html', '.css': 'text/css', '.js': 'application/javascript', '.txt': 'text/plain', '.woff2': 'font/woff2', '.svg': 'image/svg+xml', '.ico': 'image/x-icon' })[path.extname(file)] || 'application/octet-stream' }); if (req.method === 'HEAD') res.end(); else fs.createReadStream(file).pipe(res);
  } catch { res.writeHead(404).end(); }
});
(async () => {
  await new Promise(r => server.listen(0, '127.0.0.1', r)); const origin = 'http://127.0.0.1:' + server.address().port, checks = []; let browser;
  try {
    browser = await chromium.launch({ headless: true, executablePath: process.env.QA_BROWSER_EXECUTABLE || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
    for (const width of [320, 1440]) for (const mode of ['light', 'dark']) for (const score of [0, 85.5, null]) for (const status of ['Learning', 'Mastered']) for (const denied of [false, true]) {
      const context = await browser.newContext({ viewport: { width, height: 1000 } }), page = await context.newPage(), errors = [], writes = [];
      let stored = { id: 'progress', studentId: 'student', musicPieceId: 'piece', status: 'Assigned', score, targetDate: '2026-10-20', notes: 'Keep synthetic notes' }; const original = { ...stored };
      page.on('pageerror', e => errors.push(e.message)); page.on('console', m => { if (m.type() === 'error' && !(denied && /403 \(Forbidden\)/.test(m.text()))) errors.push(m.text()); });
      await context.addInitScript(mode => { localStorage.setItem('academydesk.theme', mode); localStorage.setItem('academydesk.accessToken.AcademyAdmin', 'synthetic-music-token'); }, mode);
      await context.route('**/*', async route => {
        const req = route.request(), url = new URL(req.url());
        if (url.pathname.startsWith('/api/') && ['localhost', '127.0.0.1'].includes(url.hostname)) {
          const headers = { 'Access-Control-Allow-Origin': origin };
          if (req.method() === 'PATCH') { assert.equal(url.pathname, '/api/academies/owned/music-progress/progress'); const body = req.postDataJSON(); writes.push(body); assert.deepEqual(body, { status, targetDate: original.targetDate, score, notes: original.notes }); if (!denied) stored = { ...stored, ...body }; return route.fulfill({ status: denied ? 403 : 200, headers, contentType: 'application/json', body: JSON.stringify(denied ? { message: 'Synthetic denial' } : stored) }); }
          assert.equal(req.method(), 'GET'); const fixtures = { '/api/academies': [{ id: 'owned', name: 'Synthetic academy', enabledModulesJson: '["Core","AcademicGovernance"]' }], '/api/auth/session': { academyId: 'owned', displayName: 'Synthetic admin', roles: ['AcademyAdmin'], isPlatformOwner: false }, '/api/portal/announcements': [], '/api/academies/owned/students': [{ id: 'student', firstName: 'Synthetic', lastName: 'Learner' }], '/api/academies/owned/music-pieces': [{ id: 'piece', title: 'Synthetic piece', difficulty: 'Beginner' }], '/api/academies/owned/music-progress': [stored] }; assert.ok(Object.hasOwn(fixtures, url.pathname), url.pathname); return route.fulfill({ status: 200, headers, contentType: 'application/json', body: JSON.stringify(fixtures[url.pathname]) });
        }
        if (url.origin === origin || url.protocol === 'data:') return route.continue(); errors.push('Unexpected external request'); return route.abort('blockedbyclient');
      });
      await page.goto(origin + '/music'); const row = page.locator('.music-register li').filter({ hasText: 'Synthetic Learner' }); await row.waitFor(); await row.getByRole('button', { name: 'Assigned', exact: true }).click(); await page.getByRole('option', { name: status, exact: true }).click();
      if (denied) { await page.getByText('Progress could not be updated.', { exact: true }).waitFor(); assert.deepEqual(stored, original); await row.getByRole('button', { name: 'Assigned', exact: true }).waitFor(); } else { await row.getByRole('button', { name: status, exact: true }).waitFor(); assert.equal(stored.score, score); assert.equal(stored.status, status); }
      assert.equal(writes.length, 1); assert.deepEqual(errors, []); assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
      if (score === 0 && status === 'Learning' && !denied) await page.screenshot({ path: path.join(evidence, `${width}-${mode}.png`), fullPage: true });
      checks.push({ width, mode, score, status, denied, writes, stored }); console.log(`PASS MUSIC ${width} ${mode} ${score} ${status} ${denied ? 'denied' : 'saved'}`); await context.close();
    }
    fs.writeFileSync(path.join(evidence, 'result.json'), JSON.stringify({ passed: checks.length, checks }, null, 2)); console.log(`MUSIC ZERO BROWSER PASS ${checks.length} evidence=${evidence}`);
  } finally { if (browser) await browser.close(); await new Promise(r => server.close(r)); }
})().catch(error => { fs.writeFileSync(path.join(evidence, 'failure.txt'), error.stack); console.error(error); process.exitCode = 1; });
