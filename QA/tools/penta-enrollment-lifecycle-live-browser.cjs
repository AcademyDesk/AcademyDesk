const assert = require('node:assert/strict');
const { chromium } = require(process.env.QA_FINANCE_CHAT_PLAYWRIGHT);

function loopback(value) {
  const url = new URL(value);
  assert.equal(url.protocol, 'http:');
  assert.equal(url.hostname, '127.0.0.1');
  assert.ok(Number(url.port) >= 1024 && url.href === url.origin + '/');
  return url.origin;
}

const web = loopback(process.env.QA_FINANCE_CHAT_WEB);
const api = loopback(process.env.QA_FINANCE_CHAT_API);
assert.notEqual(web, api);
const apiSegment = '/penta/enrollments/lifecycle-browser/';
const key = 'academydesk.penta.enrollment-lifecycle-recovery.v1';
const date = '2027-02-01';
const reason = 'Synthetic family break';

async function main() {
  const browser = await chromium.launch({ executablePath: process.env.QA_BROWSER_EXECUTABLE, headless: true, chromiumSandbox: true });
  try {
    await verify(browser, 'Update', 'AcademyAdmin', 390);
    await verify(browser, 'Transfer', 'Owner', 1440);
    console.log('PENTA ENROLLMENT LIFECYCLE LIVE SUMMARY 4 browser groups PASS');
  } finally {
    await browser.close();
  }
}

async function verify(browser, kind, role, width) {
  const context = await browser.newContext({ viewport: { width, height: 844 }, timezoneId: 'Asia/Kolkata' });
  try {
    const page = await context.newPage();
    const requests = [];
    page.on('request', request => {
      const url = new URL(request.url());
      if (url.origin === api && url.pathname.includes(apiSegment) && request.method() !== 'OPTIONS')
        requests.push({ method: request.method(), path: url.pathname, body: request.postData() });
    });
    await page.goto(web + '/login?returnTo=%2Fpenta%2Fenrollments%2Fmanage');
    await page.getByLabel('User name', { exact: true }).fill('qa-lifecycle-' + kind.toLowerCase() + '@example.invalid');
    await page.getByLabel('Password', { exact: true }).fill('Synthetic!39Ab');
    const identity = page.waitForResponse(response => response.url() === api + '/api/auth/session' && response.status() === 200);
    await page.getByRole('button', { name: 'Sign in', exact: true }).click();
    await page.waitForURL(web + '/penta/enrollments/manage');
    assert.ok((await (await identity).json()).roles.includes(role));

    const control = page.getByRole('region', { name: 'Protected enrollment changes' });
    const button = name => control.getByRole('button', { name, exact: true });
    const completed = control.getByRole('heading', { name: 'Enrollment change completed' });
    await control.getByLabel('Find an enrollment by learner or batch name').fill(kind);
    await button('Find enrollments').click();
    await button('Select Lifecycle ' + kind + ' in Lifecycle ' + kind + ' Piano').click();
    if (kind === 'Update') {
      await control.getByLabel('Requested status').selectOption('Paused');
      await control.getByLabel(/Lifecycle reason/).fill(reason);
      await control.getByLabel(/End date/).fill(date);
    } else {
      await control.getByLabel('Change type').selectOption('transfer');
      await control.getByLabel('Find a target batch').fill('Lifecycle Target Violin');
      const batchResult = page.waitForResponse(response => new URL(response.url()).pathname.endsWith('/penta/batch-search') && response.request().method() === 'GET');
      await button('Find target batches').click();
      const response = await batchResult;
      assert.equal(response.status(), 200);
      assert.match(response.headers()['cache-control'] ?? '', /no-store/);
      await button('Choose Lifecycle Target Violin').click();
      await control.getByLabel('Transfer date').fill(date);
    }

    const status = kind === 'Update' ? 'Paused' : 'Active';
    await button('Review exact change').click();
    await button('Approve exact change').waitFor();
    assert.equal(await button('Execute approved change').count(), 0);
    const review = control.getByRole('region', { name: 'Exact enrollment change review' });
    await review.getByText(date, { exact: true }).waitFor();
    if (kind === 'Update') await review.getByText(reason, { exact: true }).waitFor();
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth), true);
    const writes = () => requests.filter(request => request.method !== 'GET');
    assert.deepEqual(writes().map(request => request.method), ['PUT', 'PUT']);
    await button('Approve exact change').click();
    await button('Execute approved change').waitFor();
    assert.equal(JSON.parse(writes().at(-1).body).intent, 'ApproveEnrollment' + kind);
    assert.equal(writes().filter(request => request.path.endsWith('/execute')).length, 0);
    console.log('PENTA LIFECYCLE LIVE CASE exact ' + role + ' ' + kind + ' review and explicit approval PASS');

    let receipt, error, executions = 0;
    const uncertain = control.getByText(/could not be confirmed/);
    if (kind === 'Transfer') {
      await page.route(url => url.origin === api && url.pathname.includes(apiSegment) && url.pathname.endsWith('/execute'), async route => {
        if (route.request().method() !== 'POST') return route.continue();
        try {
          executions++;
          if (executions === 2) {
            const response = await route.fetch({ maxRetries: 0 });
            assert.equal(response.status(), 201);
            receipt = (await response.json()).receipt;
            assert.equal(receipt.enrollment.status, status);
          } else assert.equal(executions, 1);
        } catch (cause) { error = cause; }
        await route.abort('failed');
      });
    }

    async function reloadTo(locator) {
      const before = requests.length;
      await page.reload();
      await locator.waitFor();
      const recovery = requests.slice(before);
      assert.ok(recovery.some(request => request.path.includes('/drafts/')));
      assert.ok(recovery.every(request => request.method === 'GET'));
    }

    await button('Execute approved change').click();
    if (kind === 'Transfer') {
      await uncertain.waitFor();
      assert.equal(await completed.count(), 0);
      await reloadTo(button('Execute approved change'));
      await button('Execute approved change').click();
      await uncertain.waitFor();
      if (error) throw error;
      assert.ok(receipt?.receiptId);
      assert.deepEqual(writes().at(-1), writes().at(-2));
      await reloadTo(control.getByText(receipt.receiptId, { exact: true }));
    } else {
      await completed.waitFor();
      await reloadTo(completed);
    }
    const details = completed.locator('..').locator('dl');
    await details.getByText(status, { exact: true }).waitFor();
    await details.getByText(date, { exact: true }).waitFor();
    const pointer = await page.evaluate(storageKey => JSON.parse(sessionStorage.getItem(storageKey)), key);
    assert.deepEqual(Object.keys(pointer).sort(), ['academyId', 'conversationId', 'epoch', 'requestId', 'startRequestId']);
    assert.ok(writes().at(-1).path.endsWith('/' + pointer.conversationId + '/drafts/' + pointer.requestId + '/execute'));
    await button('Done').click();
    await completed.waitFor({ state: 'detached' });
    assert.equal(await page.evaluate(storageKey => sessionStorage.getItem(storageKey), key), null);
    console.log('PENTA LIFECYCLE LIVE CASE ' + kind + ' receipt, read-only recovery and pointer cleanup PASS');
  } finally {
    await context.close();
  }
}

main().catch(error => { console.error(error.message); process.exitCode = 1; });
