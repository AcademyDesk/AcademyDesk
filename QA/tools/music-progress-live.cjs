// Production export -> loopback relay -> real Identity/ASP.NET/owned SQL; no API mocks.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http'), assert = require('node:assert/strict');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const [api, academy, progress] = process.argv.slice(2), target = new URL(api); assert.equal(target.hostname, '127.0.0.1'); assert.equal(target.protocol, 'http:'); assert.ok(target.port); for (const id of [academy, progress]) assert.match(id, /^[a-f0-9-]{36}$/);
const root = path.resolve(__dirname, '../..'), web = fs.realpathSync(path.join(root, 'apps/web/out')), evidence = path.join(root, 'QA/EVIDENCE', 'music-progress-live-' + Date.now()); fs.mkdirSync(evidence, { recursive: true });
const server = http.createServer((req, res) => { try { assert.ok(['GET', 'HEAD'].includes(req.method)); let file = path.resolve(web, '.' + decodeURIComponent(new URL(req.url, 'http://localhost').pathname)); if (!path.extname(file)) file += '.html'; const segment = /^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file)); if (!fs.existsSync(file) && segment) { const parts = segment[1].split('.'); file = path.join(path.dirname(file), '__next.' + parts[0], ...parts.slice(1), '__PAGE__.txt'); } file = fs.realpathSync(file); assert.ok(file.startsWith(web + path.sep)); res.writeHead(200, { 'Content-Type': ({ '.html': 'text/html', '.css': 'text/css', '.js': 'application/javascript', '.txt': 'text/plain', '.woff2': 'font/woff2', '.ico': 'image/x-icon' })[path.extname(file)] || 'application/octet-stream' }); if (req.method === 'HEAD') res.end(); else fs.createReadStream(file).pipe(res); } catch { res.writeHead(404).end(); } });
(async () => {
  await new Promise(r => server.listen(0, '127.0.0.1', r)); const origin = 'http://127.0.0.1:' + server.address().port, checks = []; let browser;
  try {
    browser = await chromium.launch({ headless: true, executablePath: process.env.QA_BROWSER_EXECUTABLE || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
    for (const width of [320, 1440]) for (const theme of ['light', 'dark']) {
      const context = await browser.newContext({ viewport: { width, height: 1000 } }), page = await context.newPage(), errors = [], writes = [];
      const login = await context.request.post(api + '/api/auth/login', { data: { email: 'qa-admin-a@example.invalid', password: 'Synthetic!39Ab' } }); assert.equal(login.status(), 200); const { accessToken } = await login.json(); assert.ok(accessToken);
      const headers = { Authorization: 'Bearer ' + accessToken }, list = `/api/academies/${academy}/music-progress`;
      const read = async () => { const r = await context.request.get(api + list, { headers }); assert.equal(r.status(), 200); const rows = await r.json(); assert.equal(rows.length, 1); return rows.find(x => x.id === progress); };
      await context.addInitScript(({ theme, accessToken }) => { localStorage.setItem('academydesk.theme', theme); localStorage.setItem('academydesk.accessToken.AcademyAdmin', accessToken); }, { theme, accessToken });
      page.on('pageerror', e => errors.push(e.message)); page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
      await context.route('**/*', async route => { const req = route.request(), url = new URL(req.url()); if (url.pathname.startsWith('/api/') && ['localhost', '127.0.0.1'].includes(url.hostname)) { const response = await route.fetch({ url: api + url.pathname + url.search }); assert.notEqual(response.status(), 429); if (req.method() === 'PATCH') { assert.equal(url.pathname, list + '/' + progress); writes.push({ body: req.postDataJSON(), status: response.status() }); } return route.fulfill({ response, headers: { ...response.headers(), 'access-control-allow-origin': origin, 'access-control-allow-headers': 'Authorization,Content-Type' } }); } if (url.origin === origin || url.protocol === 'data:') return route.continue(); errors.push('Unexpected external request'); return route.abort('blockedbyclient'); });
      await page.goto(origin + '/music'); const row = page.locator('.music-register li'); await row.waitFor();
      for (let turn = 0; turn < 2; turn++) {
        const before = await read(), status = before.status === 'Learning' ? 'Mastered' : 'Learning'; assert.equal(before.score, 0);
        await row.getByRole('button', { name: before.status, exact: true }).click(); await page.getByRole('option', { name: status, exact: true }).click();
        assert.equal(await row.getByRole('button', { name: before.status, exact: true }).isDisabled(), true); assert.equal(await page.locator('form fieldset:disabled').count(), 2);
        await row.getByRole('button', { name: status, exact: true }).waitFor();
        await page.locator('.music-message').filter({ hasText: `Progress updated to ${status}.` }).waitFor(); assert.equal(await page.locator('.music-message').getAttribute('role'), 'status');
        await page.waitForFunction(() => !document.querySelector('form fieldset')?.disabled);
        const after = await read(); assert.deepEqual(writes[turn], { status: 200, body: { status, targetDate: '2026-10-20', score: 0, notes: 'Retain synthetic notes' } }); assert.equal(after.score, 0); assert.equal(after.status, status); assert.equal(after.notes, 'Retain synthetic notes');
      }
      await page.reload(); await row.getByRole('button', { name: (await read()).status, exact: true }).waitFor(); assert.equal(writes.length, 2); assert.deepEqual(errors, []); assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1)); await page.screenshot({ path: path.join(evidence, `${width}-${theme}.png`), fullPage: true }); checks.push({ width, theme, writes, stored: await read() }); await context.close(); console.log(`PASS: MUSIC LINKED ${width} ${theme}`);
    }
    fs.writeFileSync(path.join(evidence, 'result.json'), JSON.stringify({ passed: checks.length, checks }, null, 2)); console.log('PASS: MUSIC LINKED four journeys evidence=' + evidence);
  } finally { if (browser) await browser.close(); await new Promise(r => server.close(r)); }
})().catch(error => { fs.writeFileSync(path.join(evidence, 'failure.txt'), error.stack); console.error(error); process.exitCode = 1; });
