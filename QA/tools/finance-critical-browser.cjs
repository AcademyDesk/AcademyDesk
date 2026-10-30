// Real UI, Identity and native HTTP against an owned synthetic SQL fixture.
// No response fulfilment, injected tokens, external providers or customer data.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const http = require('node:http');
const root = path.resolve(__dirname, '../..');
const types = { '.html': 'text/html', '.js': 'application/javascript', '.css': 'text/css', '.json': 'application/json', '.txt': 'text/plain', '.woff': 'font/woff', '.woff2': 'font/woff2', '.svg': 'image/svg+xml', '.png': 'image/png', '.ico': 'image/x-icon' };
const paymentPath = pathname => /^\/api\/academies\/[^/]+\/payments$/.test(pathname);
const invoicePath = pathname => /^\/api\/academies\/[^/]+\/invoices$/.test(pathname);
const normalize = value => value.replace(/[\u200e\u200f]/g, '').replace(/\s+/g, ' ').trim();
const escapeRegex = value => value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');

function loopbackOrigin(value, label) {
  const url = new URL(value);
  assert.equal(url.protocol, 'http:', `${label} must use the isolated HTTP host`);
  assert.ok(['127.0.0.1', 'localhost'].includes(url.hostname), `${label} must be loopback`);
  assert.ok(Number(url.port) > 1024 && Number(url.port) <= 65535, `${label} needs an explicit nonprivileged port`);
  assert.equal(url.origin, value, `${label} must contain only the origin`);
  return url;
}

function staticServer(exported) {
  const inside = file => file === exported || file.startsWith(exported + path.sep);
  return http.createServer((request, response) => {
    if (!['GET', 'HEAD'].includes(request.method)) { response.writeHead(405); response.end(); return; }
    try {
      const relative = decodeURIComponent(new URL(request.url, 'http://127.0.0.1').pathname);
      assert.ok(!relative.includes('\0') && !relative.includes('\\') && !relative.split('/').includes('..'));
      let file = path.resolve(exported, '.' + relative);
      assert.ok(inside(file));
      if (file === exported) file = path.join(exported, 'index.html');
      else if (!path.extname(file)) file = file.replace(/[\\/]+$/, '') + '.html';
      file = fs.realpathSync(file);
      assert.ok(inside(file) && fs.statSync(file).isFile());
      response.writeHead(200, { 'Content-Type': types[path.extname(file)] || 'application/octet-stream', 'Cache-Control': 'no-store', 'X-Content-Type-Options': 'nosniff' });
      if (request.method === 'HEAD') response.end();
      else fs.createReadStream(file).on('error', () => response.destroy()).pipe(response);
    } catch { response.writeHead(404); response.end(); }
  });
}

async function login(page, testCase, apiOrigin, webOrigin) {
  await page.goto(webOrigin + '/login');
  // These real state changes wait for hydration before form submission.
  await page.getByRole('button', { name: 'Show password', exact: true }).click();
  await page.getByRole('button', { name: 'Hide password', exact: true }).click();
  await page.getByLabel('User name', { exact: true }).fill(testCase.email);
  await page.getByLabel('Password', { exact: true }).fill(testCase.password);
  const native = page.waitForResponse(response => new URL(response.url()).origin === apiOrigin
    && new URL(response.url()).pathname === '/api/auth/login' && response.request().method() === 'POST');
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  assert.equal((await native).status(), 200, 'Native Identity login must succeed');
  await page.waitForURL(url => url.pathname === '/dashboard');
  await page.waitForLoadState('load');
  assert.equal(await page.evaluate(() => Boolean(localStorage.getItem('academydesk.accessToken.AcademyAdmin'))
    && Boolean(localStorage.getItem('academydesk.refreshToken.AcademyAdmin'))), true, 'UI login must create the AcademyAdmin session');
}

async function main() {
  assert.ok(process.env.QA_FINANCE_BROWSER_FIXTURE, 'An owned synthetic fixture file is required');
  const fixture = JSON.parse(fs.readFileSync(process.env.QA_FINANCE_BROWSER_FIXTURE, 'utf8'));
  const web = loopbackOrigin(fixture.webOrigin, 'webOrigin');
  loopbackOrigin(fixture.apiOrigin, 'apiOrigin');
  assert.notEqual(fixture.webOrigin, fixture.apiOrigin);
  assert.ok(Array.isArray(fixture.cases) && fixture.cases.length >= 2, 'Independent desktop and phone fixtures are required');
  assert.ok(fixture.cases.some(item => item.viewport?.width >= 1000) && fixture.cases.some(item => item.viewport?.width <= 430), 'Both desktop and phone coverage are required');
  const names = new Set(), invoices = new Set();
  for (const item of fixture.cases) {
    assert.ok(typeof item.name === 'string' && /^[A-Za-z0-9_-]{1,64}$/.test(item.name) && !names.has(item.name), 'Case names must be unique safe artifact names');
    names.add(item.name);
    assert.ok(typeof item.email === 'string' && item.email.endsWith('@example.invalid'), 'Only explicitly synthetic accounts are allowed');
    assert.ok(typeof item.password === 'string' && item.password.length > 0, 'Synthetic login password required');
    assert.ok(typeof item.invoiceNumber === 'string' && item.invoiceNumber.length > 0 && typeof item.invoiceId === 'string' && item.invoiceId.length > 0 && !invoices.has(item.invoiceId), 'Each viewport needs its own invoice');
    invoices.add(item.invoiceId);
    assert.ok(Number.isFinite(item.partialAmount) && item.partialAmount > 0 && Number.isFinite(item.expectedBalanceAfter) && item.expectedBalanceAfter > 0, 'A positive partial payment with remaining balance is required');
    assert.ok(Number.isInteger(item.viewport.width) && item.viewport.width >= 320 && Number.isInteger(item.viewport.height) && item.viewport.height >= 500, 'Valid desktop/phone viewport required');
  }
  assert.ok(process.env.QA_PLAYWRIGHT_MODULE && process.env.QA_BROWSER_EXECUTABLE, 'Explicit Playwright module and local browser executable required');
  const { chromium } = require(process.env.QA_PLAYWRIGHT_MODULE);
  const exported = fs.realpathSync(process.env.QA_FINANCE_BROWSER_WEB_ROOT || path.join(root, 'apps/web/out'));
  for (const file of ['login.html', 'payments.html']) assert.ok(fs.existsSync(path.join(exported, file)), 'Build the real exported app before running finance browser acceptance');
  const output = path.resolve(process.env.QA_FINANCE_BROWSER_OUTPUT || path.join(root, '.build-check/finance-critical-browser'));
  fs.mkdirSync(output, { recursive: true });
  const observations = [];
  const server = staticServer(exported);
  let browser;
  const redact = value => {
    let result = String(value);
    for (const item of fixture.cases) result = result.split(item.password).join('[redacted]');
    return result.replace(/eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+/g, '[redacted-token]').slice(0, 1200);
  };
  try {
    await new Promise((resolve, reject) => { server.once('error', reject); server.listen(Number(web.port), web.hostname, resolve); });
    browser = await chromium.launch({ executablePath: process.env.QA_BROWSER_EXECUTABLE, headless: true,
      args: ['--no-sandbox', '--disable-background-networking', '--disable-component-update'] });
    console.log('FINANCEBROWSER READY real-ui=true real-identity=true native-http=true synthetic-only=true');
    for (const item of fixture.cases) {
      const context = await browser.newContext({ viewport: item.viewport, serviceWorkers: 'block' });
      const events = [], errors = [], writes = [];
      let blockedExternalRequests = 0, mutationStarted = false;
      try {
        await context.route('**/*', route => {
          const origin = new URL(route.request().url()).origin;
          if (origin === fixture.webOrigin || origin === fixture.apiOrigin) return route.continue();
          blockedExternalRequests++;
          return route.abort('blockedbyclient');
        });
        const page = await context.newPage();
        page.setDefaultTimeout(30000);
        page.on('pageerror', error => errors.push(redact(error.message)));
        page.on('request', request => {
          const url = new URL(request.url());
          if (url.origin === fixture.apiOrigin && paymentPath(url.pathname) && request.method() === 'POST') {
            mutationStarted = true;
            const body = request.postDataJSON();
            writes.push({ invoiceId: body.invoiceId, amount: body.amount });
          }
        });
        page.on('response', response => {
          const url = new URL(response.url());
          if (url.origin === fixture.apiOrigin) events.push({ method: response.request().method(), path: url.pathname, status: response.status() });
        });
        await login(page, item, fixture.apiOrigin, fixture.webOrigin);
        console.log(`FINANCEBROWSER PASS case=${item.name} real-login native=200`);
        await page.goto(fixture.webOrigin + '/payments');
        const select = page.getByRole('combobox', { name: 'Invoice', exact: true });
        await page.waitForFunction(() => {
          const input = document.querySelector('select[aria-label="Invoice"]');
          return input && !input.disabled;
        });
        const option = select.locator('option').filter({ hasText: new RegExp('^' + escapeRegex(item.invoiceNumber) + '\\s*·') });
        assert.equal(await option.count(), 1, 'The exact synthetic invoice option must exist');
        assert.equal(await option.getAttribute('value'), item.invoiceId, 'Invoice number and identifier must agree');
        await select.selectOption(item.invoiceId);
        await page.getByRole('spinbutton', { name: 'Payment amount', exact: true }).fill(String(item.partialAmount));
        const submit = page.getByRole('button', { name: 'Record payment', exact: true });
        await submit.waitFor();
        assert.equal(await submit.isEnabled(), true);
        const nativePayment = page.waitForResponse(response => new URL(response.url()).origin === fixture.apiOrigin
          && paymentPath(new URL(response.url()).pathname) && response.request().method() === 'POST');
        const refreshedInvoices = page.waitForResponse(response => mutationStarted && new URL(response.url()).origin === fixture.apiOrigin
          && invoicePath(new URL(response.url()).pathname) && response.request().method() === 'GET');
        // Two events in the same JS task exercise the real React submit handler
        // before a disabled-button rerender can hide a duplicate-submit race.
        await submit.evaluate(button => {
          const form = button.form;
          if (!form || !form.reportValidity()) throw Error('Payment form is not valid');
          form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
          form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
        });
        assert.equal((await nativePayment).status(), 201, 'Native payment creation must return201');
        const canonical = await refreshedInvoices;
        assert.equal(canonical.status(), 200, 'Canonical invoice refresh must succeed');
        const rows = await canonical.json();
        const invoice = rows.find(row => row.id === item.invoiceId);
        assert.ok(invoice && invoice.invoiceNumber === item.invoiceNumber, 'Canonical refresh must contain the exact invoice');
        assert.equal(invoice.balance, item.expectedBalanceAfter, 'Canonical API balance must equal the owned SQL fixture expectation');
        if (item.currency) assert.equal(invoice.currency, item.currency);
        await page.getByRole('status').filter({ hasText: 'Payment saved and balances refreshed.' }).waitFor();
        await page.waitForLoadState('networkidle');
        assert.deepEqual(writes, [{ invoiceId: item.invoiceId, amount: item.partialAmount }], 'Same-tick duplicate submit must issue exactly one native payment POST');
        const balanceRow = page.locator('.payments-directory li').filter({ has: page.locator('p').filter({ hasText: new RegExp('^' + escapeRegex(item.invoiceNumber) + '\\s*·\\s*Due') }) });
        assert.equal(await balanceRow.count(), 1);
        const expectedMoney = await page.evaluate(({ amount, currency }) => new Intl.NumberFormat('en-IN', { style: 'currency', currency }).format(amount), { amount: item.expectedBalanceAfter, currency: invoice.currency });
        assert.equal(normalize(await balanceRow.locator('b.text-cyan-200').innerText()), normalize('Balance ' + expectedMoney), 'Visible balance must match the refreshed canonical balance');
        const overflow = await page.evaluate(() => ({ viewport: document.documentElement.clientWidth, document: document.documentElement.scrollWidth }));
        assert.ok(overflow.document <= overflow.viewport + 1, 'Payments page must fit the desktop/phone viewport');
        assert.deepEqual(errors, [], 'No uncaught page errors during the real finance journey');
        await page.screenshot({ path: path.join(output, item.name + '.png'), fullPage: true });
        observations.push({ case: item.name, viewport: item.viewport, accepted: true, loginStatus: 200, paymentPosts: writes.length, paymentStatus: 201,
          canonicalBalance: invoice.balance, currency: invoice.currency, overflow, blockedExternalRequests, events, pageErrors: errors });
        console.log(`FINANCEBROWSER PASS case=${item.name} partial-payment duplicate-submit native-posts=1 native=201 canonical-refresh=200 visible-balance=verified overflow=none`);
      } finally { await context.close(); }
    }
    fs.writeFileSync(path.join(output, 'observations.json'), JSON.stringify({ at: new Date().toISOString(), browser: await browser.version(), realIdentity: true, nativeHttp: true, mockedResponses: 0, observations }, null, 2));
    console.log(`FINANCEBROWSER SUMMARY cases=${observations.length} accepted=true portal-balances=not-run`);
  } catch (error) {
    fs.writeFileSync(path.join(output, 'incomplete-observations.json'), JSON.stringify({ at: new Date().toISOString(), observations, error: redact(error.message) }, null, 2));
    throw Error(redact(error.message));
  } finally {
    if (browser) await browser.close();
    await new Promise(resolve => server.close(resolve));
  }
}

main().catch(error => { console.error('FINANCEBROWSER FAILED ' + error.message); process.exitCode = 1; });
