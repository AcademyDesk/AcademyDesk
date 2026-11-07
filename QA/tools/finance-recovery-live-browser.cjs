// Real browser, Identity and disposable SQL. The driver drops only the
// responses to the four selected writes after the actual API has succeeded.
const assert = require('node:assert/strict');
const crypto = require('node:crypto');
const fs = require('node:fs');
const path = require('node:path');

const web = 'http://127.0.0.1:54998';
const api = 'http://127.0.0.1:54999';
const run = process.env.QA_FINANCE_RUN;
assert.match(run ?? '', /^[a-f0-9]{32}$/);
const root = path.resolve(process.env.QA_FINANCE_FIXTURE_ROOT);
assert.equal(fs.readFileSync(path.join(root, '.qa-owner'), 'utf8').split(/\r?\n/)[0], run);
const fixture = JSON.parse(fs.readFileSync(path.join(root, 'finance-recovery-fixture.json'), 'utf8'));
assert.equal(fixture.run, run);
assert.equal(fixture.origin, web);
assert.equal(fixture.api, api);

const proof = JSON.parse(fs.readFileSync(process.env.QA_FINANCE_PROOF, 'utf8'));
assert.equal(proof.run, run);
assert.equal(proof.api, api);
assert.equal(proof.origin, web);
assert.equal(proof.responsesMocked, false);
assert.match(proof.sourceSha, /^[a-f0-9]{40}$/);
const hash = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
for (const file of ['src/app/invoices/page.tsx', 'src/app/payroll/page.tsx', 'src/app/finance-reconciliation/page.tsx', 'src/app/login/page.tsx', 'src/lib/api.ts']) {
  assert.equal(hash(path.join(proof.webSource, file)), proof.sourceHashes[file], file);
}
const artifactRoot = fs.realpathSync(proof.artifactRoot);
assert.ok(proof.artifacts.length > 100);
for (const file of proof.artifacts) {
  const target = fs.realpathSync(path.join(artifactRoot, file.path));
  assert.ok(target.startsWith(artifactRoot + path.sep));
  assert.equal(hash(target), file.sha256, file.path);
}

const output = path.resolve(process.env.QA_FINANCE_OUTPUT);
const { chromium } = require(process.env.QA_FINANCE_PLAYWRIGHT);
const base = '/api/academies/' + fixture.academy;
const checks = [];
const scenarios = [];
const responses = [];
const errors = [];

function matches(method, route) {
  return response => {
    const url = new URL(response.url());
    return url.origin === api && url.pathname === route && response.request().method() === method;
  };
}

async function check(phase, saved) {
  const response = await fetch(api + '/__qa/finance-recovery', {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ run, phase, id: saved.id, effectiveFrom: saved.effectiveFrom }),
    signal: AbortSignal.timeout(15000),
  });
  assert.equal(response.status, 200, 'Fresh SQL phase ' + phase);
  assert.equal((await response.json()).phase, phase);
  checks.push(phase);
  console.log('C9FINANCE UI/SQL PASS phase=' + phase);
}

async function choose(page, name, option) {
  await page.locator(`[name="${name}"]`).locator('..').locator('button').first().click();
  await page.getByRole('option', { name: option, exact: true }).click();
}

async function exercise(page, spec, firstPhase) {
  const { name, screen, form, button, method, mutation, read, status } = spec;
  await page.goto(web + screen);
  const draft = await spec.prepare();
  const draftValue = await draft.inputValue();
  let attempted = 0;
  let saved;
  const matcher = api + base + mutation;
  const intercept = async route => {
    const request = route.request();
    if (request.method() !== method) return route.fallback();
    attempted++;
    try {
      assert.equal(attempted, 1, name + ' replayed its financial write');
      spec.request(request.postDataJSON());
      const response = await route.fetch({ maxRedirects: 0, maxRetries: 0 });
      assert.equal(response.status(), status, name + ' real write status');
      saved = status === 201 ? await response.json() : { id: fixture.payment };
      assert.match(saved.id, /^[a-f0-9-]{36}$/i);
    } catch (error) {
      errors.push(error.message);
    } finally {
      await route.abort('failed');
    }
  };
  await page.route(matcher, intercept);
  try {
    const submit = form.getByRole('button', { name: button, exact: true });
    await submit.click();
    await page.getByText(/Save could not be confirmed/).waitFor();
    assert.equal(await draft.inputValue(), draftValue);
    assert.equal(await submit.isDisabled(), true);
    assert.deepEqual(errors, []);
    assert.ok(saved, name + ' did not reach the API');
    await form.evaluate(element => element.requestSubmit());
    await check(firstPhase, saved);
    assert.equal(attempted, 1);

    const [fresh] = await Promise.all([
      page.waitForResponse(matches('GET', base + read)),
      page.getByRole('button', { name: 'Reload financial records', exact: true }).click(),
    ]);
    assert.equal(fresh.status(), 200);
    const freshRows = (await fresh.json()).filter(row => row.id === saved.id);
    assert.equal(freshRows.length, 1);
    await spec.readback(freshRows[0], saved);
    await page.getByRole('button', { name: 'Reload financial records', exact: true }).waitFor({ state: 'hidden' });
    await check(firstPhase + 1, saved);
    assert.equal(attempted, 1);
    assert.deepEqual(errors, []);
    scenarios.push({ name, attempted, status });
    return saved;
  } finally {
    await page.unroute(matcher, intercept);
  }
}

async function main() {
  const browser = await chromium.launch({ executablePath: process.env.QA_BROWSER_EXECUTABLE, headless: true, chromiumSandbox: true });
  try {
    const context = await browser.newContext({ viewport: { width: 1440, height: 900 }, timezoneId: 'Asia/Kolkata' });
    await context.route('**/*', route => {
      const url = new URL(route.request().url());
      return url.origin === web || url.origin === api || url.protocol === 'data:' ? route.continue() : route.abort('blockedbyclient');
    });
    const page = await context.newPage();
    page.setDefaultTimeout(15000);
    page.on('pageerror', error => errors.push(error.message));
    page.on('response', response => {
      const url = new URL(response.url());
      if (url.origin === api) responses.push({ method: response.request().method(), path: url.pathname, status: response.status() });
    });
    await page.goto(web + '/login?returnTo=%2Finvoices');
    await page.getByRole('button', { name: 'Show password' }).click();
    await page.getByRole('button', { name: 'Hide password' }).click();
    await page.getByLabel('User name', { exact: true }).fill('qa-admin-a@example.invalid');
    await page.getByLabel('Password', { exact: true }).fill('Synthetic!39Ab');
    const [login] = await Promise.all([
      page.waitForResponse(matches('POST', '/api/auth/login')),
      page.getByRole('button', { name: 'Sign in', exact: true }).click(),
    ]);
    assert.equal(login.status(), 200);
    await page.waitForURL(url => url.pathname === '/invoices');

    const invoiceForm = page.locator('form.invoices-create');
    await exercise(page, {
      name: 'invoice', screen: '/invoices', form: invoiceForm, button: 'Issue invoice',
      method: 'POST', mutation: '/invoices', read: '/invoices', status: 201,
      async prepare() {
        await invoiceForm.locator('select').first().selectOption(fixture.student);
        await invoiceForm.locator('input[type="date"]').fill('2026-12-15');
        const amount = invoiceForm.locator('input[type="number"]');
        await amount.fill('123.45');
        return amount;
      },
      request(body) {
        assert.deepEqual(body, { studentId: fixture.student, feePlanId: null, amount: 123.45, dueDate: '2026-12-15' });
      },
      async readback(row) {
        assert.equal(row.totalAmount, 123.45);
        assert.equal(row.balance, 123.45);
        assert.equal(row.status, 'Issued');
        await page.locator('.invoices-directory tr').filter({ hasText: row.invoiceNumber }).getByText('₹123.45', { exact: true }).waitFor();
      },
    }, 1);

    const profileForm = page.locator('form.payroll-panel').first();
    let effectiveFrom;
    const profile = await exercise(page, {
      name: 'profile', screen: '/payroll', form: profileForm, button: 'Save payout profile',
      method: 'POST', mutation: '/payroll/profiles', read: '/payroll/profiles', status: 201,
      async prepare() {
        await choose(page, 'workerId', 'Synthetic Teacher');
        const date = profileForm.locator('[name="effectiveFrom"]');
        await date.locator('..').locator('button').first().click();
        await page.locator('.standard-date-popover').getByRole('button', { name: 'Today', exact: true }).click();
        effectiveFrom = await date.inputValue();
        assert.match(effectiveFrom, /^\d{4}-\d{2}-\d{2}$/);
        const amount = profileForm.locator('[name="monthlyAmount"]');
        await amount.fill('1000.25');
        return amount;
      },
      request(body) {
        assert.deepEqual(body, { workerType: 'Teacher', teacherId: fixture.teacher, staffUserId: null, workerName: 'Synthetic Teacher',
          paymentModel: 'Monthly', monthlyAmount: 1000.25, amountPerCycle: null, sessionsPerCycle: null, effectiveFrom, isActive: true });
      },
      async readback(row) {
        assert.equal(row.monthlyAmount, 1000.25);
        assert.equal(row.effectiveFrom, effectiveFrom);
        assert.equal(row.isActive, true);
        await page.locator('.payroll-kpis article').filter({ hasText: 'Active payout profiles' }).getByText('1', { exact: true }).waitFor();
      },
    }, 3);

    const payoutForm = page.locator('form.payroll-panel').nth(1);
    await exercise(page, {
      name: 'payout', screen: '/payroll', form: payoutForm, button: 'Record payout & generate payslip',
      method: 'POST', mutation: '/payroll/payouts', read: '/payroll/payouts', status: 201,
      async prepare() {
        await choose(page, 'payrollProfileId', 'Synthetic Teacher · Monthly ₹1,000.25');
        await payoutForm.locator('[name="periodLabel"]').fill(fixture.period);
        const deductions = payoutForm.locator('[name="deductions"]');
        await deductions.fill('100.10');
        return deductions;
      },
      request(body) {
        assert.deepEqual(body, { payrollProfileId: profile.id, periodLabel: fixture.period, sessionsCovered: null,
          grossAmount: null, deductions: 100.10, paymentMethod: 'BankTransfer', reference: '', paidAtUtc: null });
      },
      async readback(row, saved) {
        assert.equal(row.netAmount, 900.15);
        assert.equal(row.payslipNumber, saved.payslipNumber);
        assert.equal(row.workerName, 'Synthetic Teacher');
        await page.locator('.payroll-register tr').filter({ hasText: fixture.period }).getByText('₹900.15', { exact: true }).waitFor();
      },
    }, 5);

    const reconciliationForm = page.locator('form.reconciliation-form');
    const ledger = page.locator('.reconciliation-ledger tr').filter({ hasText: fixture.invoiceNumber });
    await exercise(page, {
      name: 'reconciliation', screen: '/finance-reconciliation', form: reconciliationForm, button: 'Confirm reconciliation',
      method: 'PATCH', mutation: '/payments/' + fixture.payment + '/reconcile', read: '/payments', status: 200,
      async prepare() {
        await ledger.getByRole('button', { name: 'Reconcile' }).click();
        const evidence = reconciliationForm.locator('input');
        await evidence.fill(fixture.evidence);
        return evidence;
      },
      request(body) { assert.deepEqual(body, { reference: fixture.evidence }); },
      async readback(row) {
        assert.equal(row.amount, 600);
        assert.equal(row.status, 'Reconciled');
        assert.equal(row.reconciliationReference, fixture.evidence);
        await choose(page, 'reconciliationStatus', 'Reconciled (1)');
        const evidence = ledger.locator('.reconciliation-evidence');
        await evidence.waitFor();
        assert.ok((await evidence.textContent()).startsWith(fixture.evidence));
        assert.equal(await ledger.getByRole('button', { name: 'Reconcile' }).count(), 0);
      },
    }, 7);

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
