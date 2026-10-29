// Real browser/print-layout/PDF checks against an exported app and intercepted
// synthetic API responses. This does not prove Identity, SQL or bank settlement.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const http = require('node:http');
const { execFileSync } = require('node:child_process');
const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE || 'playwright-core');
const root = path.resolve(__dirname, '../..');
const exported = path.join(root, 'apps/web/out');
const output = path.join(root, '.build-check/payroll-print-browser');
const apiOrigin = 'http://127.0.0.1:49142';
if (!fs.existsSync(path.join(exported, 'payroll.html'))) throw Error('Build the static app with NEXT_PUBLIC_API_URL=http://127.0.0.1:49142 first.');
if (!process.env.QA_BROWSER_EXECUTABLE) throw Error('An explicitly selected local Chrome executable is required.');
fs.mkdirSync(output, { recursive: true });
const rows = ['one', 'two'].map(id => ({ id, payrollProfileId: 'profile-' + id, workerName: 'Saved Worker ' + id,
  workerType: 'Staff', payslipNumber: 'PS-SYNTHETIC-' + id, periodLabel: 'October synthetic ' + id,
  grossAmount: 1000, deductions: 100, netAmount: 900, currency: 'INR', status: 'Paid', paymentMethod: 'BankTransfer',
  reference: 'REF-SYNTHETIC-' + id, paidAtUtc: '2026-10-28T10:00:00Z' }));
const types = { '.html': 'text/html', '.js': 'application/javascript', '.css': 'text/css', '.json': 'application/json', '.woff2': 'font/woff2', '.svg': 'image/svg+xml', '.ico': 'image/x-icon' };
const server = http.createServer((req, res) => {
  if (req.method !== 'GET') { res.writeHead(405); return res.end(); }
  let file;
  try {
    const relative = decodeURIComponent(new URL(req.url, 'http://127.0.0.1').pathname);
    file = path.resolve(exported, '.' + relative);
    if (file !== exported && !file.startsWith(exported + path.sep)) throw Error();
    if (file === exported) file = path.join(exported, 'index.html');
    else if (!path.extname(file)) file += '.html';
    res.writeHead(200, { 'Content-Type': types[path.extname(file)] || 'application/octet-stream', 'Cache-Control': 'no-store' });
    fs.createReadStream(file).on('error', () => res.destroy()).pipe(res);
  } catch { res.writeHead(404); res.end(); }
});

async function main() {
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const origin = 'http://127.0.0.1:' + server.address().port;
  const browser = await chromium.launch({ executablePath: process.env.QA_BROWSER_EXECUTABLE,
    headless: true, args: ['--no-sandbox', '--disable-background-networking', '--disable-component-update'] });
  try {
    for (const viewport of [{ width: 1280, height: 900 }, { width: 390, height: 844 }]) {
      const context = await browser.newContext({ viewport });
      let mode = 'normal', readCount = 0, writes = 0, held, release;
      const requests = [], errors = [];
      await context.route('**/*', async route => {
        const request = route.request(), url = new URL(request.url());
        if (url.origin === origin) return route.continue();
        if (url.origin !== apiOrigin) return route.abort();
        assert.equal(request.headers().authorization, undefined, 'Synthetic transport must never receive account credentials');
        requests.push({ method: request.method(), path: url.pathname });
        if (request.method() !== 'GET') writes++;
        let body = [], status = 200;
        if (url.pathname === '/api/academies') body = [{ id: 'synthetic', name: 'Synthetic payroll academy', subscriptionStatus: 'Active', enabledModulesJson: '["Core","Finance"]' }];
        else if (url.pathname === '/api/auth/session') body = { displayName: 'Synthetic QA Admin', roles: ['AcademyAdmin'] };
        else if (url.pathname.endsWith('/payroll/payouts')) {
          readCount++;
          if (mode === 'held') await held;
          if (mode === 'fail') { status = 503; body = { message: 'Synthetic read failure' }; }
          else if (mode === 'missing') body = [];
          else body = mode === 'fresh' ? [rows[0], { ...rows[1], workerName: 'Fresh Saved Worker two', grossAmount: 10.05, deductions: 10.04, netAmount: 0.01, currency: 'USD', sessionsCovered: 3 }] : rows;
        }
        return route.fulfill({ status, contentType: 'application/json', headers: { 'Access-Control-Allow-Origin': origin }, body: JSON.stringify(body) });
      });
      const page = await context.newPage(); page.on('pageerror', error => errors.push(error.message));
      await page.addInitScript(() => {
        window.__qaPrints = [];
        window.print = () => {
          const paper = document.querySelector('.payroll-payslip');
          if (!paper) throw Error('No selected payslip in committed DOM');
          const clone = document.documentElement.cloneNode(true);
          clone.querySelectorAll('script').forEach(script => script.remove());
          const base = document.createElement('base'); base.href = location.origin; clone.querySelector('head').prepend(base);
          window.__qaPrints.push({ id: paper.dataset.payoutId, text: paper.innerText, html: '<!DOCTYPE html>' + clone.outerHTML });
        };
      });
      await page.goto(origin + '/payroll');
      const buttons = page.getByRole('button', { name: 'Print / Save PDF', exact: true });
      await buttons.first().waitFor(); assert.equal(await buttons.count(), 2);
      mode = 'fail'; await buttons.nth(1).click();
      await page.getByRole('status').filter({ hasText: 'could not be loaded' }).waitFor();
      assert.equal(await page.evaluate(() => window.__qaPrints.length), 0);
      mode = 'missing'; await buttons.nth(1).click();
      await page.waitForFunction(() => [...document.querySelectorAll('button')].some(b => b.textContent === 'Print / Save PDF' && !b.disabled));
      assert.equal(await page.evaluate(() => window.__qaPrints.length), 0);
      mode = 'held'; held = new Promise(resolve => { release = resolve; });
      const beforeReads = readCount; await buttons.first().click();
      await page.waitForFunction(() => [...document.querySelectorAll('button')].filter(b => b.textContent === 'Preparing payslip…').length === 2);
      assert.equal(readCount, beforeReads + 1); assert.equal(await page.evaluate(() => window.__qaPrints.length), 0);
      release(); await page.waitForFunction(() => window.__qaPrints.length === 1);
      mode = 'fresh'; await buttons.nth(1).click(); await page.waitForFunction(() => window.__qaPrints.length === 2);
      const prints = await page.evaluate(() => window.__qaPrints);
      assert.equal(prints[0].id, 'one'); assert.equal(prints[1].id, 'two');
      for (let i = 0; i < prints.length; i++) {
        const capture = prints[i], name = viewport.width + '-' + capture.id;
        const printPage = await context.newPage(); await printPage.setContent(capture.html, { waitUntil: 'networkidle' });
        await printPage.emulateMedia({ media: 'print' }); await printPage.evaluate(() => document.fonts.ready);
        const paper = printPage.locator('.payroll-payslip'); assert.equal(await paper.isVisible(), true);
        assert.equal(await printPage.locator('.payroll-register').isVisible(), false);
        const boxes = await paper.evaluate(el => ({ width: el.getBoundingClientRect().width, scroll: el.scrollWidth, height: el.getBoundingClientRect().height }));
        assert.ok(boxes.scroll <= boxes.width + 1, 'Payslip content must not overflow horizontally');
        const pdf = path.join(output, name + '.pdf'); await printPage.pdf({ path: pdf, format: 'A4', printBackground: true });
        const contents = execFileSync('pdftotext', [pdf, '-'], { encoding: 'utf8' });
        const info = execFileSync('/usr/bin/pdfinfo', [pdf], { encoding: 'utf8' }); assert.match(info, /Pages:\s+1\b/);
        assert.ok(contents.includes('PS-SYNTHETIC-' + capture.id));
        assert.ok(!contents.includes('PS-SYNTHETIC-' + (capture.id === 'one' ? 'two' : 'one')));
        assert.ok(!contents.includes('Set up payout profile')); assert.ok(!contents.includes('Academy administrator'));
        for (const value of ['October synthetic ' + capture.id, '28/10/2026', 'Gross pay', 'Deductions', 'Net payout', 'Paid', 'BankTransfer', 'REF-SYNTHETIC-' + capture.id]) assert.ok(contents.includes(value), value);
        if (capture.id === 'one') for (const value of ['Saved Worker one', '1,000.00', '100.00', '900.00', 'INR']) assert.ok(contents.includes(value), value);
        if (capture.id === 'two') for (const value of ['Fresh Saved Worker two', '10.05', '10.04', '0.01', 'USD']) assert.ok(contents.includes(value), value);
        fs.writeFileSync(path.join(output, name + '.txt'), contents);
        await printPage.screenshot({ path: path.join(output, name + '.png'), fullPage: true }); await printPage.close();
        console.log('PAYROLL PRINT PDF PASS:' + name + '; selected identity, exact amounts, one A4 page, other records excluded.');
      }
      assert.equal(writes, 0); assert.deepEqual(errors, []);
      fs.writeFileSync(path.join(output, viewport.width + '-requests.json'), JSON.stringify(requests, null, 2));
      console.log('PAYROLL PRINT BROWSER PASS:' + viewport.width + '; failed/missing fresh reads deny print; pending read locks controls; retry/row selection works; no API writes or page errors.');
      await context.close();
    }
    console.log('PAYROLL PRINT BROWSER REGRESSION PASS:2 viewports,4 real PDFs; synthetic transport only, SQL/auth/settlement acceptance separate.');
  } finally { await browser.close(); }
}
main().catch(error => { console.error(error); process.exitCode = 1; }).finally(() => server.close());
