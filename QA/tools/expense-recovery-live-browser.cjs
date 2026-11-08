// Real expense browser and SQL, with bounded transport failures after writes.
const assert = require('node:assert/strict');
const crypto = require('node:crypto');
const fs = require('node:fs');
const path = require('node:path');

const web = 'http://127.0.0.1:54998';
const api = 'http://127.0.0.1:54999';
const run = process.env.QA_EXPENSE_RUN;
assert.match(run ?? '', /^[a-f0-9]{32}$/);
const root = path.resolve(process.env.QA_EXPENSE_FIXTURE_ROOT);
assert.equal(fs.readFileSync(path.join(root, '.qa-owner'), 'utf8').split(/\r?\n/)[0], run);
const fixture = JSON.parse(fs.readFileSync(path.join(root, 'expense-browser-fixture.json'), 'utf8'));
assert.deepEqual([fixture.run, fixture.origin, fixture.api], [run, web, api]);
const proof = JSON.parse(fs.readFileSync(process.env.QA_EXPENSE_PROOF, 'utf8'));
assert.deepEqual([proof.run, proof.origin, proof.api, proof.responsesMocked], [run, web, api, false]);
assert.match(proof.sourceSha, /^[a-f0-9]{40}$/);
const hash = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
for (const [file, digest] of Object.entries(proof.sourceHashes)) assert.equal(hash(path.join(proof.webSource, file)), digest, file);
const artifactRoot = fs.realpathSync(proof.artifactRoot);
assert.ok(proof.artifacts.length > 100);
for (const file of proof.artifacts) {
  const target = fs.realpathSync(path.join(artifactRoot, file.path));
  assert.ok(target.startsWith(artifactRoot + path.sep));
  assert.equal(hash(target), file.sha256, file.path);
}

const output = path.resolve(process.env.QA_EXPENSE_OUTPUT);
const { chromium } = require(process.env.QA_EXPENSE_PLAYWRIGHT);
const expensePath = '/api/academies/' + fixture.academy + '/expenses';
const responses = [];
const errors = [];
const checks = [];
const saved = [];
let currentStep = 'setup';

async function verifySql(phase, expenseId) {
  const response = await fetch(api + '/__qa/expense-recovery', {
    method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ run, phase, expenseId }),
    signal: AbortSignal.timeout(15000),
  });
  assert.equal(response.status, 200, 'Fresh expense SQL phase ' + phase);
  assert.equal((await response.json()).phase, phase);
  checks.push(phase);
  console.log('EXPENSEBROWSER UI/SQL PASS phase=' + phase);
}

async function main() {
  const browser = await chromium.launch({ executablePath: process.env.QA_BROWSER_EXECUTABLE, headless: true, chromiumSandbox: true });
  const context = await browser.newContext({ viewport: { width: 390, height: 844 }, timezoneId: 'Asia/Kolkata' });
  let failRead = false;
  let failedRead = false;
  let attempts = 0;
  try {
    await context.route('**/*', async route => {
      const request = route.request();
      const url = new URL(request.url());
      if (url.origin !== web && url.origin !== api && url.protocol !== 'data:') return route.abort('blockedbyclient');
      if (url.origin !== api || url.pathname !== expensePath) return route.continue();
      if (request.method() === 'GET' && failRead) {
        failRead = false;
        failedRead = true;
        return route.abort('failed');
      }
      if (request.method() !== 'POST') return route.continue();

      attempts++;
      try {
        assert.ok(attempts <= 3, 'An expense was replayed');
        const first = attempts === 1;
        const unsent = attempts === 3;
        assert.deepEqual(request.postDataJSON(), {
          description: unsent ? fixture.unsentDescription : first ? fixture.lostDescription : fixture.savedDescription,
          amount: unsent ? 33.17 : first ? 25.55 : 27.05, currency: 'INR', category: 'General',
          branchId: first || unsent ? null : fixture.branchId, expenseDate: null,
        });
        if (unsent) return route.abort('failed');
        const result = await route.fetch({ maxRetries: 0, maxRedirects: 0 });
        assert.equal(result.status(), 201);
        const row = await result.json();
        assert.match(row.id, /^[a-f0-9-]{36}$/i);
        saved.push(row.id);
        if (first) return route.abort('failed');
        failRead = true;
        return route.fulfill({ response: result });
      } catch (error) {
        errors.push(error.message);
        return route.abort('failed');
      }
    });
    const page = await context.newPage();
    page.setDefaultTimeout(16000);
    page.on('pageerror', error => errors.push(error.message));
    page.on('response', response => {
      const url = new URL(response.url());
      if (url.origin === api) responses.push({ path: url.pathname, method: response.request().method(), status: response.status() });
    });

    currentStep = 'signed-in finance register';
    await page.goto(web + '/login?returnTo=%2Fexpenses');
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
    await page.waitForURL(url => url.pathname === '/expenses');
    const form = page.locator('form.expenses-panel');
    const description = form.getByPlaceholder('e.g. August piano instructor payment');
    const amount = form.getByPlaceholder('Amount in INR');
    const save = form.getByRole('button', { name: 'Record expense', exact: true });
    const register = page.locator('.expenses-register');
    const reload = page.getByRole('button', { name: 'Reload financial records', exact: true });
    await register.getByText('Synthetic USD history', { exact: true }).waitFor();

    currentStep = 'response lost after commit';
    await description.fill(fixture.lostDescription);
    await amount.fill('25.55');
    await form.evaluate(element => { element.requestSubmit(); element.requestSubmit(); });
    await page.getByRole('alert').filter({ hasText: 'Save could not be confirmed. Your entries are retained.' }).waitFor();
    assert.equal(attempts, 1);
    assert.equal(saved.length, 1);
    await verifySql(1, saved[0]);

    currentStep = 'unknown outcome readback and explicit start';
    await reload.click();
    const startNew = page.getByRole('button', { name: 'Start a new expense', exact: true });
    await startNew.waitFor();
    await register.getByText(fixture.lostDescription, { exact: true }).waitFor();
    assert.equal(await description.inputValue(), fixture.lostDescription);
    assert.equal(await amount.inputValue(), '25.55');
    assert.equal(await save.isDisabled(), true);
    assert.equal(await startNew.isEnabled(), true);
    assert.equal(attempts, 1);
    await startNew.click();
    assert.equal(await description.inputValue(), '');
    assert.equal(await amount.inputValue(), '');
    await verifySql(2);

    currentStep = 'acknowledged save followed by a failed register read';
    await page.setViewportSize({ width: 1440, height: 900 });
    await description.fill(fixture.savedDescription);
    await amount.fill('27.05');
    await form.locator('[name="branchId"]').locator('..').locator('button').first().click();
    await page.getByRole('option', { name: 'Synthetic Branch', exact: true }).click();
    await save.click();
    await page.getByRole('alert').filter({ hasText: 'Expense saved, but financial records could not be refreshed.' }).waitFor();
    assert.equal(failedRead, true);
    assert.equal(attempts, 2);
    assert.equal(saved.length, 2);
    await verifySql(3, saved[1]);

    currentStep = 'confirmed write readback without replay';
    await reload.click();
    await page.getByText('Expense saved. Financial records refreshed.', { exact: true }).waitFor();
    const row = register.locator('li').filter({ hasText: fixture.savedDescription });
    await row.getByText(fixture.savedDescription, { exact: true }).waitFor();
    assert.match(await row.textContent(), /Synthetic Branch/);
    assert.equal(await register.getByText(fixture.lostDescription, { exact: true }).count(), 1);
    assert.equal(await row.count(), 1);
    assert.equal(await description.inputValue(), '');
    assert.equal(attempts, 2);
    await verifySql(4);

    currentStep = 'interruption before forwarding the write';
    await description.fill(fixture.unsentDescription);
    await amount.fill('33.17');
    await save.click();
    await page.getByRole('alert').filter({ hasText: 'Save could not be confirmed. Your entries are retained.' }).waitFor();
    assert.equal(attempts, 3);
    assert.equal(saved.length, 2);
    await verifySql(5);

    await reload.click();
    await startNew.waitFor();
    await register.getByText(fixture.savedDescription, { exact: true }).waitFor();
    assert.equal(await register.getByText(fixture.unsentDescription, { exact: true }).count(), 0);
    assert.equal(await description.inputValue(), fixture.unsentDescription);
    assert.equal(await save.isDisabled(), true);
    await startNew.click();
    assert.equal(await description.inputValue(), '');
    assert.equal(attempts, 3);
    await verifySql(6);
    assert.ok(responses.every(row => row.status < 400));
    assert.deepEqual(errors, []);
    fs.writeFileSync(path.join(output, 'result.json'), JSON.stringify({ run, passed: true, checks, attempts, responses, errors }, null, 2), { flag: 'wx' });
  } finally {
    await browser.close();
  }
}

main().catch(error => {
  fs.writeFileSync(path.join(output, 'failure.json'), JSON.stringify({ run, currentStep, checks, responses, errors }, null, 2), { flag: 'wx' });
  console.error(currentStep, error.stack);
  process.exitCode = 1;
});
