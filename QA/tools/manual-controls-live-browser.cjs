// Real browser and Identity, with an owned SQL fixture. The only transport
// intervention drops a real committed response to test unconfirmed-save UX.
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const { createHash } = require('node:crypto');
const { execFileSync } = require('node:child_process');

const root = path.resolve(__dirname, '../..');
function required(name) {
  const value = process.env[name];
  assert.ok(value, `Missing ${name}`);
  return value;
}
function origin(name) {
  const value = required(name);
  const url = new URL(value);
  assert.equal(url.hostname, '127.0.0.1');
  assert.equal(url.protocol, 'http:');
  assert.ok(Number(url.port) >= 1024);
  assert.equal(value, url.origin);
  return value;
}
const web = origin('QA_MANUAL_LIVE_ORIGIN');
const api = origin('QA_MANUAL_LIVE_API');
assert.notEqual(web, api);
const run = required('QA_MANUAL_LIVE_RUN');
assert.match(run, /^[a-f0-9]{32}$/);
const fixtureRoot = path.resolve(required('QA_MANUAL_LIVE_FIXTURE_ROOT'));
assert.equal(fs.readFileSync(path.join(fixtureRoot, '.qa-owner'), 'utf8').split(/\r?\n/)[0], run);
const fixture = JSON.parse(fs.readFileSync(path.join(fixtureRoot, 'manual-browser-fixture.json'), 'utf8'));
assert.equal(fixture.run, run);
assert.equal(fixture.api, api);
assert.equal(fixture.origin, web);
const proof = JSON.parse(fs.readFileSync(required('QA_MANUAL_LIVE_PROOF'), 'utf8'));
assert.equal(proof.run, run);
assert.equal(proof.sourceSha, execFileSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim());
const hash = file => createHash('sha256').update(fs.readFileSync(file)).digest('hex');
for (const file of ['src/app/finance-governance/page.tsx', 'src/app/teacher/page.tsx', 'src/app/login/page.tsx', 'src/lib/api.ts']) {
  assert.equal(hash(path.join(root, 'apps/web', file)), proof.sourceHashes[file]);
  assert.equal(hash(path.join(proof.webSource, file)), proof.sourceHashes[file]);
}
assert.ok(proof.artifacts.length > 100);
const artifactRoot = path.resolve(proof.artifactRoot);
for (const { path: relative, sha256 } of proof.artifacts) {
  const file = path.resolve(artifactRoot, relative);
  assert.ok(file.startsWith(artifactRoot + path.sep));
  assert.equal(hash(file), sha256);
}
const output = path.resolve(required('QA_MANUAL_LIVE_OUTPUT'));
assert.equal(fs.readdirSync(output).length, 0);
const playwright = require(required('QA_MANUAL_LIVE_PLAYWRIGHT'));
const responses = [], requests = [], errors = [], checks = [];
const responsePath = response => new URL(response.url()).pathname;
const matches = (method, endpoint) => response => new URL(response.url()).origin === api && responsePath(response) === endpoint && response.request().method() === method;

async function check(phase, savedId) {
  const response = await fetch(api + '/__qa/manual-check', {
    method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ run, phase, savedId }), signal: AbortSignal.timeout(15000),
  });
  assert.equal(response.status, 200, phase + ' fresh SQL verification');
  assert.equal((await response.json()).phase, phase);
  checks.push(phase);
  console.log(`MANUALBROWSER UI/SQL PASS ${phase}`);
}

async function login(browser, kind, destination, viewport) {
  const context = await browser.newContext({ viewport, timezoneId: 'Asia/Kolkata' });
  await context.route('**/*', route => {
    const url = new URL(route.request().url());
    return url.origin === web || url.origin === api || url.protocol === 'data:' ? route.continue() : route.abort('blockedbyclient');
  });
  const page = await context.newPage();
  page.setDefaultTimeout(12000);
  page.on('pageerror', error => errors.push({ kind, message: error.message }));
  page.on('request', request => {
    const url = new URL(request.url());
    if (url.origin === api) requests.push({ kind, method: request.method(), path: url.pathname });
  });
  page.on('response', response => {
    if (new URL(response.url()).origin === api) responses.push({ kind, path: responsePath(response), status: response.status() });
  });
  await page.goto(web + '/login?returnTo=' + encodeURIComponent(destination));
  await page.getByRole('button', { name: 'Show password' }).click();
  await page.getByRole('button', { name: 'Hide password' }).click();
  await page.getByLabel('User name', { exact: true }).fill(`qa-${kind}-a@example.invalid`);
  await page.getByLabel('Password', { exact: true }).fill('Synthetic!39Ab');
  const [response] = await Promise.all([
    page.waitForResponse(matches('POST', '/api/auth/login')),
    page.getByRole('button', { name: 'Sign in', exact: true }).click(),
  ]);
  assert.equal(response.status(), 200);
  await page.waitForURL(url => url.pathname === destination);
  return page;
}

async function decide(page, proposal, action, answer, phase, expectedWrite) {
  const before = requests.filter(x => x.method === 'PATCH' && x.path.endsWith('/approval')).length;
  const panel = page.getByText(proposal.reason, { exact: true }).locator('xpath=../../..');
  const pending = expectedWrite ? page.waitForResponse(matches('PATCH', `/api/academies/${fixture.academy}/finance-adjustments/${proposal.id}/approval`)) : null;
  const handleDialog = page.waitForEvent('dialog').then(async dialog => {
    assert.equal(dialog.type(), 'prompt');
    if (answer === null) await dialog.dismiss();
    else await dialog.accept(answer);
  });
  const [decision] = await Promise.all([pending, handleDialog, panel.getByRole('button', { name: action, exact: true }).click()]);
  if (decision) {
    assert.equal(decision.status(), 200);
    await page.getByText(`Adjustment ${action === 'Approve' ? 'approved' : 'rejected'} and recorded in the control register.`, { exact: true }).waitFor();
    await page.getByText(proposal.reason, { exact: true }).waitFor({ state: 'detached' });
  } else if (answer !== null) {
    await page.getByText('A rejection reason is required.', { exact: true }).waitFor();
  }
  assert.equal(requests.filter(x => x.method === 'PATCH' && x.path.endsWith('/approval')).length - before, Number(expectedWrite));
  await check(phase);
}

async function fill(form, phase, assessment = false, date = '') {
  await form.getByRole('button', { name: 'Assigned batch' }).click();
  const options = form.getByRole('option');
  await form.getByRole('option', { name: 'Synthetic Batch', exact: true }).waitFor();
  assert.equal(await options.count(), 2, 'Only the placeholder and assigned batch may be offered');
  await form.getByRole('option', { name: 'Synthetic Batch', exact: true }).click();
  assert.equal(await form.locator('[name=batchId]').inputValue(), fixture.batch);
  await form.locator('[name=title]').fill('Live ' + phase);
  if (assessment) {
    await form.locator('[name=maxScore]').fill('12.5');
    await form.locator('[name=scheduledAtUtc]').fill(date);
  }
}

async function save(page, form, kind, button, phase, date = '') {
  await fill(form, phase, kind === 'assessments', date);
  const [accepted] = await Promise.all([
    page.waitForResponse(matches('POST', '/api/teacher/' + kind)),
    form.getByRole('button', { name: button, exact: true }).click(),
  ]);
  assert.equal(accepted.status(), 200);
  await page.getByText('Saved for your assigned batch.', { exact: true }).waitFor();
  await page.waitForFunction(el => el.value === '', await form.locator('[name=title]').elementHandle());
  await check(phase, (await accepted.json()).id);
}

async function main() {
  let browser;
  let passed = false;
  let failure = null;
  try {
    browser = await playwright.chromium.launch({ executablePath: required('QA_BROWSER_EXECUTABLE'), headless: true, chromiumSandbox: true });
    const finance = await login(browser, 'admin', '/finance-governance', { width: 1280, height: 900 });
    const { approval, named, rejection } = fixture.proposals;
    await decide(finance, approval, 'Approve', null, 'finance-approve-cancel', false);
    await decide(finance, rejection, 'Reject', null, 'finance-reject-cancel', false);
    await decide(finance, rejection, 'Reject', '', 'finance-reject-empty', false);
    await decide(finance, rejection, 'Reject', '   ', 'finance-reject-whitespace', false);
    await decide(finance, approval, 'Approve', '', 'finance-approved-empty', true);
    await decide(finance, named, 'Approve', 'Live approval note', 'finance-approved-note', true);
    await decide(finance, rejection, 'Reject', 'Live rejection reason', 'finance-rejected', true);
    await finance.context().close();

    const teacher = await login(browser, 'teacher', '/teacher', { width: 390, height: 844 });
    await teacher.locator('.learner-bottom-nav').getByRole('button', { name: /Homework/ }).click();
    const forms = teacher.locator('.teacher-homework-panels form');
    const homework = forms.nth(0), lesson = forms.nth(1), assessment = forms.nth(2);
    await save(teacher, homework, 'assignments', 'Assign homework', 'teacher-homework');
    await save(teacher, lesson, 'lesson-plans', 'Save lesson plan', 'teacher-lesson');
    await save(teacher, assessment, 'assessments', 'Create assessment', 'teacher-assessment-blank');
    await save(teacher, assessment, 'assessments', 'Create assessment', 'teacher-assessment-dated', '2026-11-07T17:30');

    await fill(homework, 'teacher-denied');
    const [denied] = await Promise.all([
      teacher.waitForResponse(matches('POST', '/api/teacher/assignments')),
      homework.locator('[name=batchId]').evaluate((input, id) => { input.value = id; input.form.requestSubmit(); }, fixture.foreignBatch),
    ]);
    assert.equal(denied.status(), 400);
    await teacher.getByText('The teaching record could not be saved.', { exact: true }).waitFor();
    assert.equal(await homework.locator('[name=title]').inputValue(), 'Live teacher-denied');
    await check('teacher-denied');

    await fill(assessment, 'teacher-unconfirmed', true);
    let forwarded = 0, committedId;
    await teacher.route(api + '/api/teacher/assessments', async route => {
      if (route.request().method() !== 'POST') return route.continue();
      forwarded++;
      try {
        if (forwarded === 1) {
          const actual = await route.fetch({ maxRedirects: 0, maxRetries: 0 });
          assert.equal(actual.status(), 200);
          committedId = (await actual.json()).id;
        }
      } catch (error) { errors.push({ kind: 'lost-response-control', message: error.message }); }
      await route.abort('connectionreset');
    });
    await assessment.getByRole('button', { name: 'Create assessment', exact: true }).click();
    await teacher.getByText('The save could not be confirmed. Check your teaching records before trying again.', { exact: true }).waitFor();
    assert.equal(await assessment.locator('[name=title]').inputValue(), 'Live teacher-unconfirmed');
    assert.equal(forwarded, 1);
    assert.match(committedId, /^[a-f0-9-]{36}$/);
    await check('teacher-unconfirmed', committedId);
    assert.equal(requests.filter(x => x.kind === 'teacher' && x.method === 'POST' && /^\/api\/teacher\/(assignments|assessments|lesson-plans)$/.test(x.path)).length, 6);
    assert.equal(requests.filter(x => x.method === 'PATCH' && x.path.endsWith('/approval')).length, 3);
    assert.deepEqual(responses.filter(x => x.status === 429), []);
    assert.deepEqual(errors, []);
    assert.equal(checks.length, 13);
    passed = true;
  } catch (error) {
    failure = error.message;
    console.error(failure);
    process.exitCode = 1;
  } finally {
    if (browser) await browser.close();
    fs.writeFileSync(path.join(output, 'result.json'), JSON.stringify({ run, passed, failure, checks, requests, responses, errors, responsesMocked: false, tokenInjection: false }, null, 2), { flag: 'wx' });
  }
}
main().catch(error => { console.error(error.message); process.exitCode = 1; });
