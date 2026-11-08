// Real payroll form and authenticated API on the owned disposable SQL host.
const assert = require('node:assert/strict');
const crypto = require('node:crypto');
const fs = require('node:fs');
const path = require('node:path');

const web = 'http://127.0.0.1:54998';
const api = 'http://127.0.0.1:54999';
const run = process.env.QA_PAYROLL_RUN;
assert.match(run ?? '', /^[a-f0-9]{32}$/);
const root = path.resolve(process.env.QA_PAYROLL_FIXTURE_ROOT);
assert.equal(fs.readFileSync(path.join(root, '.qa-owner'), 'utf8').split(/\r?\n/)[0], run);
const fixture = JSON.parse(fs.readFileSync(path.join(root, 'payroll-acceptance-fixture.json'), 'utf8'));
assert.deepEqual([fixture.run, fixture.origin, fixture.api], [run, web, api]);
const proof = JSON.parse(fs.readFileSync(process.env.QA_PAYROLL_PROOF, 'utf8'));
assert.deepEqual([proof.run, proof.origin, proof.api, proof.responsesMocked], [run, web, api, false]);
assert.match(proof.sourceSha, /^[a-f0-9]{40}$/);
const hash = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
for (const file of ['src/app/payroll/page.tsx', 'src/app/login/page.tsx', 'src/lib/api.ts']) {
  assert.equal(hash(path.join(proof.webSource, file)), proof.sourceHashes[file], file);
}
const artifactRoot = fs.realpathSync(proof.artifactRoot);
assert.ok(proof.artifacts.length > 100);
for (const file of proof.artifacts) {
  const target = fs.realpathSync(path.join(artifactRoot, file.path));
  assert.ok(target.startsWith(artifactRoot + path.sep));
  assert.equal(hash(target), file.sha256, file.path);
}

const output = path.resolve(process.env.QA_PAYROLL_OUTPUT);
const { chromium } = require(process.env.QA_PAYROLL_PLAYWRIGHT);
const payouts = '/api/academies/' + fixture.academy + '/payroll/payouts';
const expectedNotice = 'A regular payroll statement already exists for this worker and pay period. Additional amounts require a separately approved payroll adjustment.';
const responses = [];
const errors = [];
const checks = [];
let currentStep = 'login';
let observedNotice = null;

function matches(method, pathname) {
  return response => {
    const url = new URL(response.url());
    return url.origin === api && url.pathname === pathname && response.request().method() === method;
  };
}
async function verifySql(phase, payoutId) {
  const response = await fetch(api + '/__qa/payroll-acceptance', {
    method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ run, phase, payoutId }),
    signal: AbortSignal.timeout(15000),
  });
  assert.equal(response.status, 200, 'Fresh SQL phase ' + phase);
  assert.equal((await response.json()).phase, phase);
  checks.push(phase);
  console.log('PAYROLLBROWSER UI/SQL PASS phase=' + phase);
}
async function choose(page, label) {
  await page.locator('[name="payrollProfileId"]').locator('..').locator('button').first().click();
  await page.getByRole('option', { name: label, exact: true }).click();
}

async function main() {
  const browser = await chromium.launch({ executablePath: process.env.QA_BROWSER_EXECUTABLE, headless: true, chromiumSandbox: true });
  const context = await browser.newContext({ viewport: { width: 390, height: 844 }, timezoneId: 'Asia/Kolkata' });
  try {
    await context.route('**/*', route => {
      const url = new URL(route.request().url());
      return url.origin === web || url.origin === api || url.protocol === 'data:' ? route.continue() : route.abort('blockedbyclient');
    });
    await context.addInitScript(() => {
      window.__qaPrints = [];
      window.print = () => {
        const article = document.querySelector('.payroll-payslip');
        window.__qaPrints.push({ id: article?.dataset.payoutId, text: article?.innerText });
      };
    });
    const page = await context.newPage();
    page.setDefaultTimeout(15000);
    page.on('pageerror', error => errors.push(error.message));
    page.on('response', response => {
      const url = new URL(response.url());
      if (url.origin === api) responses.push({ path: url.pathname, method: response.request().method(), status: response.status() });
    });
    await page.goto(web + '/login?returnTo=%2Fpayroll');
    await page.getByLabel('User name', { exact: true }).fill('qa-admin-a@example.invalid');
    await page.getByLabel('Password', { exact: true }).fill('Synthetic!39Ab');
    const [login] = await Promise.all([
      page.waitForResponse(matches('POST', '/api/auth/login')),
      page.getByRole('button', { name: 'Sign in', exact: true }).click(),
    ]);
    assert.equal(login.status(), 200);
    await page.waitForURL(url => url.pathname === '/payroll');
    const form = page.locator('form.payroll-panel').nth(1);
    const submit = form.getByRole('button', { name: 'Record payout & generate payslip' });
    const period = form.locator('[name="periodLabel"]');
    const deductions = form.locator('[name="deductions"]');
    const reference = form.locator('[name="reference"]');
    const payoutPosts = () => responses.filter(row => row.path === payouts && row.method === 'POST');

    currentStep = 'zero-net UI and API guard';
    await choose(page, 'Synthetic Teacher · Monthly ₹1,000.25');
    await period.fill(fixture.period);
    await deductions.fill('1000.25');
    await reference.fill(fixture.reference);
    await submit.click();
    await page.getByText('Deductions must be non-negative and less than gross pay.', { exact: true }).waitFor();
    assert.equal(payoutPosts().length, 0);
    const zero = await page.evaluate(async ({ url, profile, period }) => {
      const token = localStorage.getItem('academydesk.accessToken.AcademyAdmin');
      if (!token) throw Error('Signed-in browser session missing');
      const response = await fetch(url, { method: 'POST', headers: { Authorization: 'Bearer ' + token, 'Content-Type': 'application/json' },
        body: JSON.stringify({ payrollProfileId: profile, periodLabel: period, deductions: 1000.25 }) });
      return { status: response.status, body: await response.json() };
    }, { url: api + payouts, profile: fixture.profile, period: fixture.period });
    assert.equal(zero.status, 400);
    assert.match(zero.body.message, /net payout must be positive/);
    await verifySql(1);

    currentStep = 'one-cent payout';
    await deductions.fill('1000.24');
    const [write] = await Promise.all([page.waitForResponse(matches('POST', payouts)), submit.click()]);
    assert.equal(write.status(), 201);
    const saved = await write.json();
    assert.deepEqual([saved.payrollProfileId, saved.grossAmount, saved.deductions, saved.netAmount, saved.workerName],
      [fixture.profile, 1000.25, 1000.24, .01, 'Synthetic Teacher']);
    await page.getByText('Payout recorded and payslip generated.', { exact: true }).waitFor();
    const row = page.locator('.payroll-register tbody tr').filter({ hasText: fixture.period });
    await row.getByText('₹0.01', { exact: true }).waitFor();
    assert.equal(await row.count(), 1);
    await verifySql(2, saved.id);

    currentStep = 'fresh saved payslip';
    await page.setViewportSize({ width: 1440, height: 900 });
    const [read] = await Promise.all([
      page.waitForResponse(matches('GET', payouts)),
      row.getByRole('button', { name: 'Print / Save PDF', exact: true }).click(),
    ]);
    assert.equal(read.status(), 200);
    await page.getByText('Payslip print dialog closed.', { exact: true }).waitFor();
    const printed = await page.evaluate(() => window.__qaPrints);
    assert.equal(printed.length, 1);
    assert.equal(printed[0].id, saved.id);
    for (const value of [saved.payslipNumber, 'Synthetic Teacher', fixture.period, fixture.reference, '₹1,000.25', '₹1,000.24', '₹0.01'])
      assert.ok(printed[0].text.includes(value), 'Missing saved payslip value: ' + value);
    await verifySql(3);

    currentStep = 'duplicate payout explanation';
    await choose(page, 'Synthetic Teacher · Monthly ₹1,200.25');
    await period.fill('  november 2026  ');
    await deductions.fill('10.01');
    await reference.fill('Synthetic duplicate retained');
    const [duplicate] = await Promise.all([page.waitForResponse(matches('POST', payouts)), submit.click()]);
    assert.equal(duplicate.status(), 409);
    assert.equal((await duplicate.json()).message, expectedNotice);
    const notice = page.getByRole('status').filter({ hasText: /payout could not|payroll statement already exists/i });
    await notice.waitFor();
    observedNotice = (await notice.textContent()).trim();
    assert.equal(await period.inputValue(), '  november 2026  ');
    assert.equal(await deductions.inputValue(), '10.01');
    assert.equal(await reference.inputValue(), 'Synthetic duplicate retained');
    assert.equal(await row.count(), 1);
    assert.deepEqual(payoutPosts().map(row => row.status), [400, 201, 409]);
    await verifySql(4);
    assert.equal(observedNotice, expectedNotice);
    assert.ok(responses.every(row => row.status < 400 || row.path === payouts && [400, 409].includes(row.status)));
    assert.deepEqual(errors, []);
    fs.writeFileSync(path.join(output, 'result.json'), JSON.stringify({ run, passed: true, checks, responses, errors, observedNotice, printCalls: printed.length }, null, 2), { flag: 'wx' });
  } finally {
    await context.close();
    await browser.close();
  }
}

main().catch(error => {
  fs.writeFileSync(path.join(output, 'failure.json'), JSON.stringify({ run, currentStep, checks, responses, errors, observedNotice }, null, 2), { flag: 'wx' });
  console.error(currentStep, error.stack);
  process.exitCode = 1;
});
