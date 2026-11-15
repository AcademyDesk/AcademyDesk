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
const key = 'academydesk.penta.enrollment-creation-recovery.v1';
const dated = /^\d{4}-\d\d-\d\dT.+Z$/;
const startDate = '2027-01-01';

async function main() {
  const browser = await chromium.launch({ executablePath: process.env.QA_BROWSER_EXECUTABLE,
    headless: true, chromiumSandbox: true });
  try {
    await journey(browser, 'Active', 'AcademyAdmin', 390, true);
    await journey(browser, 'Waiting', 'Owner', 1440, false);
    console.log('PENTA ENROLLMENT LIVE SUMMARY 6 browser groups PASS');
  } finally {
    await browser.close();
  }
}

async function journey(browser, name, role, width, interrupt) {
  const context = await browser.newContext({ viewport: { width, height: 844 }, timezoneId: 'Asia/Kolkata' });
  try {
    const page = await context.newPage();
    const records = [];
    page.on('request', request => {
      const url = new URL(request.url());
      if (url.origin === api && url.pathname.includes('/penta/enrollments/browser-') && request.method() !== 'OPTIONS')
        records.push({ path: url.pathname, method: request.method(), body: request.postData() });
    });
    const dialog = page.getByRole('region', { name: 'Protected enrollment creation' });
    const review = dialog.getByRole('button', { name: 'Review exact enrollment', exact: true });
    const approve = dialog.getByRole('button', { name: 'Approve exact enrollment', exact: true });
    const execute = dialog.getByRole('button', { name: 'Create approved enrollment', exact: true });
    const done = dialog.getByRole('button', { name: 'Done', exact: true });
    const uncertain = dialog.getByText(/enrollment outcome could not be confirmed/);
    const find = page.getByLabel('Ask PENTA to find a student or batch');
    const writes = () => records.filter(record => record.method !== 'GET');

    await page.goto(web + '/login');
    await page.getByLabel('User name', { exact: true }).fill('qa-enroll-' + name.toLowerCase() + '@example.invalid');
    await page.getByLabel('Password', { exact: true }).fill('Synthetic!39Ab');
    const identity = page.waitForResponse(response => response.url() === api + '/api/auth/session' && response.status() === 200);
    await page.getByRole('button', { name: 'Sign in', exact: true }).click();
    await page.waitForURL(web + '/penta');
    const profile = await (await identity).json();
    assert.ok(profile.roles.includes(role));
    await page.getByRole('link', { name: 'Enrollment assistant', exact: true }).click();
    await page.waitForURL(web + '/penta/enrollments');
    await find.fill('Find student Enrollment ' + name);
    await page.getByRole('button', { name: 'Send request', exact: true }).click();
    await page.getByRole('button', { name: 'Select Enrollment ' + name, exact: true }).click();
    await find.fill('Find batch Enrollment ' + name + ' Piano');
    await page.getByRole('button', { name: 'Send request', exact: true }).click();
    await page.getByRole('button', { name: 'Select Enrollment ' + name + ' Piano', exact: true }).click();
    await page.getByLabel('Start date').fill(startDate);
    const status = role === 'Owner' ? 'Waitlisted' : 'Active';
    await page.getByLabel('Requested status').selectOption(status);
    await page.getByRole('button', { name: 'Check eligibility', exact: true }).click();
    await page.getByText('Eligibility checked — approval still required', { exact: true }).waitFor();
    assert.deepEqual(writes(), []);
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth), true);

    const errors = [];
    let prepareCount = 0, approvalCount = 0, executionCount = 0, receipt;
    if (interrupt) {
      await page.route(url => url.origin === api && url.pathname.includes('/penta/enrollments/browser-conversations/'), async route => {
        const path = new URL(route.request().url()).pathname;
        const method = route.request().method();
        if (method === 'GET' || method === 'OPTIONS' || !path.includes('/drafts/')) return route.continue();
        try {
          if (method === 'PUT') { prepareCount++; assert.equal(prepareCount, 1); }
          else if (path.endsWith('/approve')) { approvalCount++; assert.equal(approvalCount, 1); }
          else {
            assert.ok(path.endsWith('/execute'));
            executionCount++;
            if (executionCount === 1) return route.abort('failed');
            assert.equal(executionCount, 2);
          }
          const result = await route.fetch({ maxRetries: 0 });
          assert.equal(result.status(), path.endsWith('/approve') ? 200 : 201);
          const saved = await result.json();
          assert.match(saved.expiresAtUtc, dated);
          if (path.endsWith('/execute')) {
            receipt = saved.receipt;
            assert.match(receipt.completedAtUtc, dated);
            assert.equal(receipt.enrollment.status, 'Active');
          }
        } catch (error) { errors.push(error); }
        await route.abort('failed');
      });
    }

    async function reloadTo(locator) {
      const before = records.length;
      await page.reload();
      await locator.waitFor();
      assert.ok(records.slice(before).some(record => record.path.includes('/drafts/')));
      assert.ok(records.slice(before).every(record => record.method === 'GET'));
      if (errors.length) throw errors[0];
    }

    await review.click();
    if (interrupt) { await uncertain.waitFor(); await reloadTo(approve); }
    await approve.waitFor();
    assert.equal(await execute.count(), 0);
    assert.deepEqual(writes().map(record => record.method), ['PUT', 'PUT']);
    assert.match(await dialog.locator('dl').innerText(), new RegExp(startDate + '[\\s\\S]+' + status));
    console.log('PENTA ENROLLMENT LIVE CASE ' + role + ' exact ' + status + ' review PASS');

    await approve.click();
    if (interrupt) { await uncertain.waitFor(); await reloadTo(execute); }
    await execute.waitFor();
    assert.equal(JSON.parse(writes().at(-1).body).intent, 'ApproveEnrollmentCreate');
    assert.equal(writes().filter(record => record.path.endsWith('/execute')).length, 0);

    await execute.click();
    if (interrupt) {
      await uncertain.waitFor();
      assert.equal(await done.count(), 0);
      await reloadTo(execute);
      await execute.click();
      await uncertain.waitFor();
      if (errors.length) throw errors[0];
      assert.equal(executionCount, 2);
      assert.ok(receipt?.receiptId);
      assert.deepEqual(writes().at(-1), writes().at(-2));
      await reloadTo(dialog.getByText(receipt.receiptId, { exact: true }));
      console.log('PENTA ENROLLMENT LIVE CASE approval and execution losses recover with GET and an explicit same-key retry PASS');
    }
    await dialog.getByRole('heading', { name: 'Enrollment created' }).waitFor();
    if (!interrupt) {
      assert.equal(writes().filter(record => record.path.endsWith('/execute')).length, 1);
      await reloadTo(done);
    }
    const pointer = await page.evaluate(storageKey => JSON.parse(sessionStorage.getItem(storageKey)), key);
    assert.deepEqual(Object.keys(pointer).sort(), ['academyId', 'conversationId', 'epoch', 'requestId', 'startRequestId']);
    assert.ok(writes().at(-1).path.endsWith('/' + pointer.conversationId + '/drafts/' + pointer.requestId + '/execute'));
    await done.click();
    await dialog.getByRole('heading', { name: 'Enrollment created' }).waitFor({ state: 'detached' });
    assert.equal(await page.evaluate(storageKey => sessionStorage.getItem(storageKey), key), null);
    assert.equal(await dialog.getByRole('link', { name: 'Open Manual Enrollments' }).getAttribute('href'), '/enrollments');
    console.log('PENTA ENROLLMENT LIVE CASE ' + role + ' receipt and safe cleanup PASS');
    if (!interrupt) console.log('PENTA ENROLLMENT LIVE CASE Owner single explicit approval and execution PASS');
  } finally {
    await context.close();
  }
}

main().catch(error => { console.error(error.message); process.exitCode = 1; });
