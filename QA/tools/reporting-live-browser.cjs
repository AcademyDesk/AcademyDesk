// Real browser, Identity and SQL. Status changes use the authenticated API
// because the reconciliation screen currently presents those states read-only.
const assert = require('node:assert/strict');
const crypto = require('node:crypto');
const fs = require('node:fs');
const path = require('node:path');

const web = 'http://127.0.0.1:54998';
const api = 'http://127.0.0.1:54999';
const run = process.env.QA_REPORTING_RUN;
assert.match(run ?? '', /^[a-f0-9]{32}$/);
const root = path.resolve(process.env.QA_REPORTING_FIXTURE_ROOT);
assert.equal(fs.readFileSync(path.join(root, '.qa-owner'), 'utf8').split(/\r?\n/)[0], run);
const fixture = JSON.parse(fs.readFileSync(path.join(root, 'reporting-fixture.json'), 'utf8'));
assert.equal(fixture.run, run);
assert.equal(fixture.origin, web);
assert.equal(fixture.api, api);
const proof = JSON.parse(fs.readFileSync(process.env.QA_REPORTING_PROOF, 'utf8'));
assert.equal(proof.run, run);
assert.equal(proof.origin, web);
assert.equal(proof.api, api);
assert.equal(proof.responsesMocked, false);
assert.match(proof.sourceSha, /^[a-f0-9]{40}$/);
const hash = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
for (const file of ['src/app/reports/page.tsx', 'src/app/finance-reconciliation/page.tsx', 'src/lib/reports-finance.ts', 'src/app/login/page.tsx', 'src/lib/api.ts']) {
  assert.equal(hash(path.join(proof.webSource, file)), proof.sourceHashes[file], file);
}
const artifactRoot = fs.realpathSync(proof.artifactRoot);
assert.ok(proof.artifacts.length > 100);
for (const file of proof.artifacts) {
  const target = fs.realpathSync(path.join(artifactRoot, file.path));
  assert.ok(target.startsWith(artifactRoot + path.sep));
  assert.equal(hash(target), file.sha256, file.path);
}

const output = path.resolve(process.env.QA_REPORTING_OUTPUT);
const { chromium } = require(process.env.QA_REPORTING_PLAYWRIGHT);
const base = '/api/academies/' + fixture.academy;
const errors = [];
const responses = [];
const checks = [];
const downloads = [];
let currentStep = 'setup';

async function verifySql(phase) {
  const response = await fetch(api + '/__qa/reporting', {
    method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ run, phase }),
    signal: AbortSignal.timeout(15000),
  });
  assert.equal(response.status, 200, 'Fresh reporting SQL phase ' + phase);
  assert.equal((await response.json()).phase, phase);
  checks.push(phase);
  console.log('REPORTING UI/SQL PASS phase=' + phase);
}

async function request(page, route, method = 'GET', payload) {
  return page.evaluate(async ({ url, method, payload }) => {
    const bearer = localStorage.getItem('academydesk.accessToken.AcademyAdmin');
    if (!bearer) throw Error('Signed-in browser session missing');
    const response = await fetch(url, {
      method, cache: 'no-store', headers: { Authorization: 'Bearer ' + bearer, 'Content-Type': 'application/json' },
      body: payload === undefined ? undefined : JSON.stringify(payload),
    });
    const raw = await response.text();
    return { status: response.status, value: raw ? JSON.parse(raw) : null };
  }, { url: api + base + route, method, payload });
}

function tile(page, label) {
  return page.locator('.workspace-reports-kpis button').filter({ has: page.getByText(label, { exact: true }) });
}
function money(amount, currency) {
  return new Intl.NumberFormat('en-IN', { style: 'currency', currency, currencyDisplay: 'code' }).format(amount);
}
function normalize(s) {
  return s.replace(/\s+/g, ' ').trim();
}
async function assertMetric(page, label, values) {
  const metric = tile(page, label).locator('strong');
  await metric.waitFor();
  assert.equal(normalize(await metric.textContent()), normalize(values.join(' · ')), label);
}
async function dialog(page, label, title, values, absent = []) {
  await tile(page, label).click();
  const modal = page.getByRole('dialog');
  await modal.getByRole('heading', { name: title, exact: true }).waitFor();
  const content = normalize(await modal.textContent());
  for (const value of values) assert.ok(content.includes(normalize(value)), 'Missing modal content: ' + value);
  for (const value of absent) assert.ok(!content.includes(normalize(value)), 'Unexpected modal content: ' + value);
  await modal.getByRole('button', { name: /close/i }).click();
  await modal.waitFor({ state: 'hidden' });
}

async function download(page, trigger, label, filename) {
  const [file] = await Promise.all([page.waitForEvent('download'), trigger.click()]);
  assert.equal(file.suggestedFilename(), filename);
  const destination = path.join(output, label + '.csv');
  assert.equal(fs.existsSync(destination), false);
  await file.saveAs(destination);
  const content = fs.readFileSync(destination, 'utf8');
  downloads.push({ label, sha256: hash(destination) });
  return content;
}

async function reconciliation(page, status, evidence) {
  await page.goto(web + '/finance-reconciliation');
  const ledger = page.locator('.reconciliation-ledger');
  await ledger.locator('[name="reconciliationStatus"]').locator('..').locator('button').first().click();
  await page.getByRole('option', { name: 'All payments', exact: true }).click();
  const row = ledger.locator('tbody tr').filter({ hasText: fixture.invoiceNumber }).filter({ hasText: '₹200.00' });
  await row.locator('.reconciliation-status').filter({ hasText: new RegExp('^' + status + '$') }).waitFor();
  assert.equal(await row.getByRole('button', { name: /reconcile/i }).count(), 0);
  if (evidence) await row.locator('.reconciliation-evidence').filter({ hasText: evidence }).waitFor();
}

async function main() {
  const browser = await chromium.launch({ executablePath: process.env.QA_BROWSER_EXECUTABLE, headless: true, chromiumSandbox: true });
  const context = await browser.newContext({ viewport: { width: 320, height: 900 }, timezoneId: 'Asia/Kolkata', acceptDownloads: true });
  try {
    let interrupted = false;
    await context.route('**/*', route => {
      const url = new URL(route.request().url());
      if (url.origin !== web && url.origin !== api && url.protocol !== 'data:' && url.protocol !== 'blob:') return route.abort('blockedbyclient');
      if (!interrupted && url.href === api + base + '/expenses' && route.request().method() === 'GET') {
        interrupted = true;
        return route.abort('failed');
      }
      return route.continue();
    });
    const page = await context.newPage();
    page.setDefaultTimeout(15000);
    page.on('pageerror', error => errors.push(error.message));
    page.on('response', response => {
      const url = new URL(response.url());
      if (url.origin === api) responses.push({ path: url.pathname, method: response.request().method(), status: response.status() });
    });

    currentStep = 'login and retry';
    await page.goto(web + '/login?returnTo=%2Freports');
    await page.getByLabel('User name', { exact: true }).fill('qa-admin-a@example.invalid');
    await page.getByLabel('Password', { exact: true }).fill('Synthetic!39Ab');
    const [login] = await Promise.all([
      page.waitForResponse(response => {
        const url = new URL(response.url());
        return url.origin === api && url.pathname === '/api/auth/login' && response.request().method() === 'POST';
      }),
      page.getByRole('button', { name: 'Sign in', exact: true }).click(),
    ]);
    assert.equal(login.status(), 200);
    await page.waitForURL(url => url.pathname === '/reports');
    await page.getByRole('alert').filter({ hasText: 'Reports could not be loaded. Check your connection and try again.' }).waitFor();
    assert.equal(interrupted, true);
    assert.equal(await page.getByRole('region', { name: 'Operational metrics' }).count(), 0);
    await page.getByRole('button', { name: 'Try again', exact: true }).click();

    currentStep = 'reports financial cards, modals and CSV';
    await assertMetric(page, 'Collected', [money(870.15, 'INR'), money(30.35, 'USD')]);
    await assertMetric(page, 'Outstanding', [money(0, 'EUR'), money(0, 'INR'), money(70.15, 'USD')]);
    await assertMetric(page, 'Expenses', [money(20.05, 'INR'), money(5.25, 'USD')]);
    await dialog(page, 'Collected', 'Collected fee payments', [fixture.invoiceNumber, fixture.usdNumber, money(30.35, 'USD')], [money(100, 'USD')]);
    await dialog(page, 'Outstanding', 'Student fees and payroll records', [fixture.usdNumber, money(70.15, 'USD'), 'Synthetic reporting worker', money(90, 'EUR')], [fixture.invoiceNumber, 'C20-CANCELLED']);
    await dialog(page, 'Expenses', 'Expenses', ['Synthetic INR expense', money(20.05, 'INR'), 'Synthetic USD expense', money(5.25, 'USD')]);
    const financeExport = page.locator('.workspace-reports-panel').filter({ has: page.getByRole('heading', { name: 'Finance export' }) });
    const exportCard = name => page.locator('.workspace-reports-export-card').filter({ has: page.getByText(name, { exact: true }) });
    const first = await download(page, financeExport.getByRole('button', { name: 'Download CSV' }), 'finance-invoices', 'academydesk-invoices.csv');
    const second = await download(page, exportCard('Invoices').getByRole('button', { name: /download csv/i }), 'export-invoices', 'academydesk-invoices.csv');
    assert.equal(first, second);
    assert.ok(first.startsWith('"Invoice","Student","Amount","Currency","Due date"\r\n'));
    assert.match(first, /"'=C20-USD","[^"]*","100\.55","USD"/);
    assert.doesNotMatch(first, /C20-FOREIGN/);
    const expenses = await download(page, exportCard('Expenses').getByRole('button', { name: /download csv/i }), 'export-expenses', 'academydesk-expenses.csv');
    assert.ok(expenses.startsWith('"Expense amount","Currency"\r\n'));
    assert.match(expenses, /"20\.05","INR"/);
    assert.match(expenses, /"5\.25","USD"/);

    currentStep = 'currency readback and legacy write guard';
    async function readInvoice(id, status, balance, paidAmount) {
      const response = await request(page, '/invoices');
      assert.equal(response.status, 200);
      assert.equal(response.value.some(row => row.invoiceNumber === 'C20-FOREIGN'), false);
      const item = response.value.find(row => row.id === id);
      assert.deepEqual([item.status, item.balance, item.paidAmount], [status, balance, paidAmount]);
    }
    await readInvoice(fixture.usdInvoice, 'PartiallyPaid', 70.15, 30.35);
    const [dashboard, summary] = await Promise.all([request(page, '/dashboard'), request(page, '/finance-governance/summary')]);
    assert.equal(dashboard.status, 200);
    assert.equal(summary.status, 200);
    assert.deepEqual(dashboard.value.byCurrency, summary.value.byCurrency);
    assert.equal(summary.value.byCurrency.find(group => group.currency === 'USD').outstanding, 70.15);
    await verifySql(1);

    const legacy = await request(page, '/payments', 'POST', { invoiceId: fixture.usdInvoice, amount: 70.15, method: 'UPI' });
    assert.deepEqual(legacy, { status: 400, value: { message: 'Payment exceeds the invoice balance.' } });
    await verifySql(2);

    currentStep = 'payment void, rejected restore and valid restore';
    await page.setViewportSize({ width: 1440, height: 900 });
    const route = '/payments/' + fixture.payment;
    await reconciliation(page, 'Reconciled', 'Synthetic original bank evidence');
    assert.equal((await request(page, route + '/status', 'PATCH', { status: 'Voided' })).status, 200);
    await reconciliation(page, 'Voided');
    await readInvoice(fixture.invoice, 'PartiallyPaid', 200, 600);
    await verifySql(3);
    assert.equal((await request(page, route + '/status', 'PATCH', { status: 'Reconciled' })).status, 400);
    assert.equal((await request(page, route + '/reconcile', 'PATCH', { reference: '' })).status, 400);
    await verifySql(4);
    assert.equal((await request(page, route + '/reconcile', 'PATCH', { reference: fixture.paymentReference })).status, 200);
    await reconciliation(page, 'Reconciled', fixture.paymentReference);
    await readInvoice(fixture.invoice, 'Paid', 0, 800);
    await verifySql(5);
    assert.equal(responses.filter(row => row.status === 400).length, 3);
    assert.ok(responses.every(row => row.status < 400 || row.status === 400));
    assert.deepEqual(errors, []);
    fs.writeFileSync(path.join(output, 'result.json'), JSON.stringify({ run, passed: true, checks, downloads, responses, errors }, null, 2), { flag: 'wx' });
  } finally {
    await context.close();
    await browser.close();
  }
}

main().catch(error => {
  fs.writeFileSync(path.join(output, 'failure.json'), JSON.stringify({ run, currentStep, checks, downloads, responses, errors }, null, 2), { flag: 'wx' });
  console.error(currentStep, error.stack);
  process.exitCode = 1;
});
