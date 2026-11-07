// Actual browser and Identity against the owned SQL harness. Only the selected
// POST response is interrupted. Reads and the committed POST use the real API.
const assert = require('node:assert/strict');
const crypto = require('node:crypto');
const fs = require('node:fs');
const path = require('node:path');

const web = 'http://127.0.0.1:54998';
const api = 'http://127.0.0.1:54999';
const run = process.env.QA_PAYMENT_RUN;
assert.match(run ?? '', /^[a-f0-9]{32}$/);
const root = path.resolve(process.env.QA_PAYMENT_FIXTURE_ROOT);
assert.equal(fs.readFileSync(path.join(root, '.qa-owner'), 'utf8').split(/\r?\n/)[0], run);
const fixture = JSON.parse(fs.readFileSync(path.join(root, 'payment-recovery-fixture.json'), 'utf8'));
assert.equal(fixture.run, run);
assert.equal(fixture.origin, web);
assert.equal(fixture.api, api);
assert.equal(fixture.invoices.length, 2);

const proof = JSON.parse(fs.readFileSync(process.env.QA_PAYMENT_PROOF, 'utf8'));
assert.equal(proof.run, run);
assert.equal(proof.api, api);
assert.equal(proof.origin, web);
assert.equal(proof.responsesMocked, false);
assert.match(proof.sourceSha, /^[a-f0-9]{40}$/);
const hash = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
for (const file of ['src/app/payments/page.tsx', 'src/app/login/page.tsx', 'src/lib/api.ts']) {
  assert.equal(hash(path.join(proof.webSource, file)), proof.sourceHashes[file], file);
}
const artifactRoot = fs.realpathSync(proof.artifactRoot);
assert.ok(proof.artifacts.length > 100);
for (const file of proof.artifacts) {
  const target = fs.realpathSync(path.join(artifactRoot, file.path));
  assert.ok(target.startsWith(artifactRoot + path.sep));
  assert.equal(hash(target), file.sha256, file.path);
}
const output = path.resolve(process.env.QA_PAYMENT_OUTPUT);
const { chromium } = require(process.env.QA_PAYMENT_PLAYWRIGHT);
const invoicesPath = `/api/academies/${fixture.academy}/invoices`;
const paymentsPath = `/api/academies/${fixture.academy}/payments`;
const errors = [];
const responses = [];
const checks = [];
const scenarios = [];

function matches(method, pathname) {
  return response => {
    const url = new URL(response.url());
    return response.request().method() === method && url.origin === api && url.pathname === pathname;
  };
}

async function check(phase, paymentId) {
  const response = await fetch(api + '/__qa/payment-recovery', {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ run, phase, paymentId }), signal: AbortSignal.timeout(15000),
  });
  assert.equal(response.status, 200, 'Fresh SQL check ' + phase);
  assert.equal((await response.json()).phase, phase);
  checks.push(phase);
  console.log('C9PAYMENT UI/SQL PASS phase=' + phase);
}

async function exercise(browser, invoice, commit, firstPhase) {
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 }, timezoneId: 'Asia/Kolkata' });
  let attempted = 0;
  let paymentId;
  try {
    await context.route('**/*', async route => {
      const request = route.request();
      const url = new URL(request.url());
      if (url.origin !== web && url.origin !== api && url.protocol !== 'data:') return route.abort('blockedbyclient');
      if (url.href !== api + paymentsPath || request.method() !== 'POST') return route.continue();
      attempted++;
      try {
        assert.equal(attempted, 1, 'Browser replayed the payment');
        assert.deepEqual(request.postDataJSON(), { invoiceId: invoice.id, amount: 400, method: 'UPI', reference: fixture.reference });
        if (commit) {
          const response = await route.fetch({ maxRedirects: 0, maxRetries: 0 });
          assert.equal(response.status(), 201, 'Real committed payment');
          const saved = await response.json();
          assert.equal(saved.invoiceId, invoice.id);
          assert.equal(saved.amount, 400);
          paymentId = saved.id;
          assert.match(paymentId, /^[a-f0-9-]{36}$/i);
        }
      } catch (error) {
        errors.push(error.message);
      } finally {
        await route.abort('failed');
      }
    });
    const page = await context.newPage();
    page.setDefaultTimeout(15000);
    page.on('pageerror', error => errors.push(error.message));
    page.on('response', response => {
      const url = new URL(response.url());
      if (url.origin === api) responses.push({ path: url.pathname, method: response.request().method(), status: response.status() });
    });

    await page.goto(web + '/login?returnTo=%2Fpayments');
    await page.getByRole('button', { name: 'Show password' }).click();
    await page.getByRole('button', { name: 'Hide password' }).click();
    await page.getByLabel('User name', { exact: true }).fill('qa-admin-a@example.invalid');
    await page.getByLabel('Password', { exact: true }).fill('Synthetic!39Ab');
    const [login] = await Promise.all([
      page.waitForResponse(matches('POST', '/api/auth/login')),
      page.getByRole('button', { name: 'Sign in', exact: true }).click(),
    ]);
    assert.equal(login.status(), 200);
    await page.waitForURL(url => url.pathname === '/payments');
    const directory = page.locator('.payments-directory');
    const row = directory.locator('li').filter({ hasText: invoice.number });
    await row.getByText('Balance ₹400.00', { exact: true }).waitFor();
    const form = page.locator('form.payments-create');
    await form.locator('select').first().selectOption(invoice.id);
    const amount = form.locator('input[type="number"]');
    assert.equal(await amount.inputValue(), '400');
    const reference = form.getByPlaceholder('UPI, cheque, or receipt reference (optional)');
    await reference.fill(fixture.reference);
    await form.getByRole('button', { name: 'Record payment', exact: true }).click();
    await page.getByText(/Save could not be confirmed/).waitFor();
    assert.equal(await amount.inputValue(), '400');
    assert.equal(await reference.inputValue(), fixture.reference);
    assert.equal(await form.getByRole('button', { name: 'Record payment', exact: true }).isDisabled(), true);
    await form.evaluate(element => element.requestSubmit());
    await check(firstPhase, paymentId);
    assert.equal(attempted, 1);

    const [invoiceRead, paymentRead] = await Promise.all([
      page.waitForResponse(matches('GET', invoicesPath)),
      page.waitForResponse(matches('GET', paymentsPath)),
      page.getByRole('button', { name: 'Reload financial records', exact: true }).click(),
    ]);
    assert.equal(invoiceRead.status(), 200);
    assert.equal(paymentRead.status(), 200);
    const current = (await invoiceRead.json()).find(item => item.id === invoice.id);
    const payments = (await paymentRead.json()).filter(item => item.invoiceId === invoice.id);
    assert.equal(current.balance, commit ? 0 : 400);
    assert.equal(current.status, commit ? 'Paid' : 'PartiallyPaid');
    assert.deepEqual(payments.map(item => [item.amount, item.status]).sort(), commit ? [[400, 'Completed'], [600, 'Reconciled']] : [[600, 'Reconciled']]);
    if (commit) assert.equal(payments.find(item => item.status === 'Completed').id, paymentId);
    await row.getByText(commit ? 'Balance ₹0.00' : 'Balance ₹400.00', { exact: true }).waitFor();
    await page.getByRole('button', { name: 'Reload financial records', exact: true }).waitFor({ state: 'hidden' });
    assert.equal(await form.locator('option').filter({ hasText: invoice.number }).count(), commit ? 0 : 1);
    await check(firstPhase + 1);
    assert.equal(attempted, 1);
    assert.deepEqual(errors, []);
    scenarios.push({ invoice: invoice.number, committed: commit, attempted, balance: current.balance, rows: payments.length });
  } finally {
    await context.close();
  }
}

async function main() {
  const browser = await chromium.launch({ executablePath: process.env.QA_BROWSER_EXECUTABLE, headless: true, chromiumSandbox: true });
  try {
    await exercise(browser, fixture.invoices[0], true, 1);
    await exercise(browser, fixture.invoices[1], false, 3);
    assert.ok(responses.every(response => response.status < 400));
    fs.writeFileSync(path.join(output, 'result.json'), JSON.stringify({ run, passed: true, checks, scenarios, responses, errors }, null, 2), { flag: 'wx' });
  } finally {
    await browser.close();
  }
}

main().catch(error => {
  fs.writeFileSync(path.join(output, 'failure.json'), JSON.stringify({ run, checks, scenarios, responses, errors }, null, 2), { flag: 'wx' });
  console.error(error.stack);
  process.exitCode = 1;
});
