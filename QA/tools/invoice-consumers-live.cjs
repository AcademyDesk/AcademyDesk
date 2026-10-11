// Exported production pages -> loopback relay -> real Identity/API/disposable SQL.
// Status/payment mutations below use real APIs; no invoice-status editor exists in this UI.
const fs = require('node:fs'), path = require('node:path'), http = require('node:http'), assert = require('node:assert/strict');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const [api, academy, student, ...ids] = process.argv.slice(2), target = new URL(api);
assert.equal(target.hostname, '127.0.0.1'); assert.equal(target.protocol, 'http:'); assert.ok(target.port); assert.equal(ids.length, 4);
for (const id of [academy, student, ...ids]) assert.match(id, /^[a-f0-9-]{36}$/);
const root = path.resolve(__dirname, '../..'), web = fs.realpathSync(path.join(root, 'apps/web/out'));
const evidence = path.join(root, 'QA/EVIDENCE', 'invoice-consumers-live-' + Date.now()); fs.mkdirSync(evidence, { recursive: true });
const server = http.createServer((req, res) => {
  try {
    assert.ok(['GET', 'HEAD'].includes(req.method)); let file = path.resolve(web, '.' + decodeURIComponent(new URL(req.url, 'http://localhost').pathname));
    if (!path.extname(file)) file += '.html';
    const segment = /^__next\.([\w.-]+)\.__PAGE__\.txt$/.exec(path.basename(file));
    if (!fs.existsSync(file) && segment) { const parts = segment[1].split('.'); file = path.join(path.dirname(file), '__next.' + parts[0], ...parts.slice(1), '__PAGE__.txt'); }
    file = fs.realpathSync(file); assert.ok(file.startsWith(web + path.sep));
    res.writeHead(200, { 'Content-Type': ({ '.html': 'text/html', '.css': 'text/css', '.js': 'application/javascript', '.txt': 'text/plain', '.woff2': 'font/woff2', '.ico': 'image/x-icon' })[path.extname(file)] || 'application/octet-stream' });
    if (req.method === 'HEAD') res.end(); else fs.createReadStream(file).pipe(res);
  } catch { res.writeHead(404).end(); }
});
const money = value => new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR' }).format(value);
(async () => {
  await new Promise(r => server.listen(0, '127.0.0.1', r)); const origin = 'http://127.0.0.1:' + server.address().port, checks = []; let browser;
  try {
    browser = await chromium.launch({ headless: true, executablePath: process.env.QA_BROWSER_EXECUTABLE || 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
    let index = 0;
    for (const width of [320, 1440]) for (const theme of ['light', 'dark']) {
      const invoiceId = ids[index++], context = await browser.newContext({ viewport: { width, height: 1000 } });
      const page = await context.newPage(), errors = [], responses = [], mutations = [];
      const login = async email => { const r = await context.request.post(api + '/api/auth/login', { data: { email, password: 'Synthetic!39Ab' } }); assert.equal(r.status(), 200); return (await r.json()).accessToken; };
      const adminToken = await login('qa-admin-a@example.invalid'), studentToken = await login('qa-consumer-student@example.invalid');
      assert.ok(adminToken && studentToken); const headers = { Authorization: 'Bearer ' + adminToken };
      const base = `/api/academies/${academy}`, statusPath = `${base}/invoices/${invoiceId}/status`;
      await context.addInitScript(({ theme, adminToken, studentToken }) => {
        localStorage.setItem('academydesk.theme', theme); localStorage.setItem('academydesk.accessToken.AcademyAdmin', adminToken); localStorage.setItem('academydesk.accessToken.Portal', studentToken);
      }, { theme, adminToken, studentToken });
      page.on('pageerror', e => errors.push(e.message)); page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
      await context.route('**/*', async route => {
        const req = route.request(), url = new URL(req.url());
        if (url.pathname.startsWith('/api/') && ['localhost', '127.0.0.1'].includes(url.hostname)) {
          assert.ok(['GET', 'OPTIONS'].includes(req.method()), 'UI unexpectedly attempted a write');
          const response = await route.fetch({ url: api + url.pathname + url.search }); assert.equal(response.status(), 200, 'UI read failed: ' + url.pathname);
          if ([base + '/invoices', base + '/finance-governance/collections', `/api/portal/students/${student}`].includes(url.pathname)) responses.push({ path: url.pathname, body: await response.json() });
          return route.fulfill({ response, headers: { ...response.headers(), 'access-control-allow-origin': origin, 'access-control-allow-headers': 'Authorization,Content-Type' } });
        }
        if (url.origin === origin || url.protocol === 'data:') return route.continue(); errors.push('Unexpected external request'); return route.abort('blockedbyclient');
      });
      const read = async () => { const r = await context.request.get(api + base + '/invoices', { headers }); assert.equal(r.status(), 200); return (await r.json()).find(x => x.id === invoiceId); };
      const initial = await read(); assert.equal(initial.status, 'PartiallyPaid'); assert.equal(initial.paidAmount, 600); assert.equal(initial.balance, 400);
      const mutate = async (method, route, data, expected) => {
        const r = await context.request.fetch(api + route, { method, headers, data }); assert.equal(r.status(), expected);
        mutations.push({ method, route, data, status: r.status() });
        if (expected === 400) assert.equal((await r.json()).message, 'Invoice status must match the collected payments and adjusted balance.');
      };
      const verify = async (status, balance, stage) => {
        const before = responses.length;
        await page.goto(origin + '/invoices'); const row = page.locator('.invoices-directory tbody tr').filter({ hasText: initial.invoiceNumber }); await row.waitFor();
        assert.equal(await row.locator('.invoice-status').innerText(), status);
        const list = responses.slice(before).find(x => x.path === base + '/invoices').body, current = list.find(x => x.id === invoiceId);
        assert.equal(current.status, status); assert.equal(current.balance, balance); assert.equal(current.paidAmount, 1000 - balance);
        for (const [label, value] of [['Issued invoices', list.length], ['Paid invoices', list.filter(x => x.status === 'Paid').length], ['Open invoices', list.filter(x => !['Paid', 'Cancelled'].includes(x.status)).length]]) {
          const tile = page.locator('.invoices-kpis article').filter({ hasText: label }); assert.equal(await tile.locator('strong').innerText(), String(value));
        }
        await row.getByRole('button', { name: 'Preview / Print', exact: true }).click();
        const previewStatus = page.locator('.invoice-paper-bill > div').filter({ hasText: 'Payment status' }); assert.equal(await previewStatus.locator('b').innerText(), status);
        assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), 'Invoice page overflow');
        await page.screenshot({ path: path.join(evidence, `${width}-${theme}-${stage}-invoices.png`), fullPage: true });
        const collectionStart = responses.length;
        await page.goto(origin + '/finance-governance');
        await page.waitForFunction(() => !document.body.textContent.includes('Loading finance controls'));
        const queueResponse = responses.slice(collectionStart).find(x => x.path === base + '/finance-governance/collections'); assert.ok(queueResponse);
        const queue = queueResponse.body, collection = queue.find(x => x.id === invoiceId);
        const section = page.locator('section.surface-panel').filter({ has: page.getByRole('heading', { name: 'Overdue collections', exact: true }) });
        assert.equal(await section.getByText(initial.invoiceNumber, { exact: true }).count(), balance > 0 ? 1 : 0);
        const overdue = page.locator('.finance-control-kpis section').filter({ hasText: 'Overdue invoices' }); assert.equal(await overdue.locator('b').innerText(), String(queue.length));
        if (balance > 0) { assert.equal(collection.balance, balance); assert.equal(collection.paidAmount, 600); assert.equal(collection.adjustedAmount, 0); assert.equal(collection.daysOverdue, 1); const card = section.locator('div.rounded.border').filter({ has: page.getByText(initial.invoiceNumber, { exact: true }) }); await card.getByText('Remaining due: ' + money(balance), { exact: true }).waitFor(); }
        else assert.equal(collection, undefined);
        assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), 'Collection page overflow');
        await page.screenshot({ path: path.join(evidence, `${width}-${theme}-${stage}-collections.png`), fullPage: true });
        const studentStart = responses.length;
        await page.goto(origin + '/portal'); await page.getByText('Fees and payments', { exact: true }).waitFor({ state: 'hidden' });
        await page.getByRole('heading', { name: /^Good day,/ }).waitFor();
        const nav = page.locator(width === 320 ? '.learner-bottom-nav' : '.enterprise-sidebar nav'); await nav.getByRole('button', { name: /More$/ }).click();
        const fees = page.locator('section').filter({ has: page.getByRole('heading', { name: 'Fees and payments', exact: true }) }).last(); await fees.waitFor();
        const portal = responses.slice(studentStart).find(x => x.path === `/api/portal/students/${student}`).body.invoices.find(x => x.id === invoiceId);
        assert.equal(portal.status, status); assert.equal(portal.balance, balance);
        const display = initial.invoiceNumber + ' · ' + new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 }).format(balance);
        await fees.getByText(display, { exact: true }).waitFor();
        const downloadReady = page.waitForEvent('download');
        await fees.locator('.learner-download-row').filter({ hasText: initial.invoiceNumber }).getByRole('button', { name: 'View / download invoice', exact: true }).click();
        const download = await downloadReady; await download.saveAs(path.join(evidence, `${width}-${theme}-${stage}-invoice.html`));
        const document = fs.readFileSync(await download.path(), 'utf8');
        const documentMoney = value => new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value);
        assert.ok(document.includes(`Paid: INR ${documentMoney(1000 - balance)}`)); assert.ok(document.includes(`Balance: INR ${documentMoney(balance)}`)); assert.ok(document.includes(`Status: ${status}`));
        await page.getByLabel('Payment status', { exact: true }).selectOption(balance > 0 ? 'Outstanding' : 'Paid'); await fees.getByText(display, { exact: true }).waitFor();
        await page.getByLabel('Payment status', { exact: true }).selectOption(balance > 0 ? 'Paid' : 'Outstanding'); assert.equal(await fees.getByText(display, { exact: true }).count(), 0);
        await page.getByLabel('Payment status', { exact: true }).selectOption('All'); await fees.getByText(display, { exact: true }).waitFor();
        await page.screenshot({ path: path.join(evidence, `${width}-${theme}-${stage}-student.png`), fullPage: true });
        assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1));
      };
      await mutate('PATCH', statusPath, { status: ' partiallypaid ' }, 200);
      await mutate('PATCH', statusPath, { status: 'Paid' }, 400);
      await verify('PartiallyPaid', 400, 'partial');
      await mutate('POST', base + '/payments', { invoiceId, amount: 400 }, 201);
      await mutate('PATCH', statusPath, { status: ' paid ' }, 200);
      await verify('Paid', 0, 'paid');
      const final = await read(); assert.equal(final.paidAmount, 1000); assert.equal(final.balance, 0); assert.equal(final.status, 'Paid'); assert.deepEqual(errors, []);
      checks.push({ width, theme, invoiceId, mutations, final, pageReads: responses.map(x => x.path), consoleErrors: errors });
      await context.close(); console.log(`PASS: INVOICE CONSUMERS ${width} ${theme}; invoice/KPI/preview, collection queue/KPI, Student status/balance/filter; partial400 -> paid0`);
    }
    fs.writeFileSync(path.join(evidence, 'result.json'), JSON.stringify({ journeys: checks.length, pageStates: checks.length * 6, mutations: checks.reduce((n, x) => n + x.mutations.length, 0), checks }, null, 2));
    console.log('PASS: INVOICE CONSUMERS four linked journeys / 24 page-states evidence=' + evidence);
  } finally { if (browser) await browser.close(); await new Promise(r => server.close(r)); }
})().catch(error => { fs.writeFileSync(path.join(evidence, 'failure.txt'), error.stack); console.error(error); process.exitCode = 1; });
