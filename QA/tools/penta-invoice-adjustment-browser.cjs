const assert = require('node:assert/strict');
const { chromium } = require(process.env.QA_FINANCE_CHAT_PLAYWRIGHT);

const web = origin(process.env.QA_FINANCE_CHAT_WEB);
const api = origin(process.env.QA_FINANCE_CHAT_API);
assert.notEqual(web, api);
const adjustmentPath = '/penta/invoice-adjustments/conversations/';
const storageKey = 'academydesk.penta.adjustment-recovery';
const reason = 'QA scholarship ' + 'a'.repeat(225);

function origin(value) {
  const url = new URL(value);
  assert.equal(url.protocol, 'http:');
  assert.equal(url.hostname, '127.0.0.1');
  assert.ok(url.port && url.href === url.origin + '/');
  return url.origin;
}

async function main() {
  const browser = await chromium.launch({
    executablePath: process.env.QA_BROWSER_EXECUTABLE, headless: true, chromiumSandbox: true,
  });
  try {
    const page = await browser.newPage({ viewport: { width: 390, height: 844 }, timezoneId: 'Asia/Kolkata' });
    const requests = [];
    page.on('request', request => {
      const url = new URL(request.url());
      if (url.origin === api && url.pathname.includes(adjustmentPath) && request.method() !== 'OPTIONS')
        requests.push({ path: url.pathname, method: request.method(), body: request.postData() });
    });

    await page.goto(web + '/login?returnTo=%2Fpenta');
    await page.getByLabel('User name', { exact: true }).fill('qa-admin-a@example.invalid');
    await page.getByLabel('Password', { exact: true }).fill('Synthetic!39Ab');
    await page.getByRole('button', { name: 'Sign in', exact: true }).click();
    await page.waitForURL(web + '/penta');

    const chat = page.getByRole('region', { name: 'Finance chat' });
    const form = chat.getByRole('region', { name: 'Propose an invoice adjustment' });
    async function open() {
      await chat.getByRole('button', { name: 'Show overdue invoices' }).click();
      await chat.getByRole('row', { name: /QA-BROWSER-ADJUSTMENT/ }).getByRole('button', { name: 'Invoice', exact: true }).click();
      await chat.getByRole('button', { name: 'Propose invoice adjustment', exact: true }).click();
    }

    await open();
    await page.waitForFunction(() => document.activeElement?.id === 'penta-adjustment-amount');
    const amount = form.getByLabel('Reduction amount');
    const explanation = form.getByLabel('Reason for the reduction');
    const review = form.getByRole('button', { name: 'Review adjustment' });
    await form.getByLabel('Adjustment type').selectOption('Scholarship');
    await explanation.fill(reason);
    await amount.fill('125.001');
    await review.click();
    await form.getByText(/up to two decimal places/).waitFor();
    assert.deepEqual(requests, []);
    await amount.fill('125.01');
    await review.click();

    const authorize = form.getByRole('button', { name: 'Authorize submission' });
    await authorize.waitFor();
    assert.deepEqual(await form.locator('dd').allTextContents(), [
      'QA-BROWSER-ADJUSTMENT', 'Scholarship', 'USD 125.01', 'USD 200.00', 'USD 74.99', reason,
    ]);
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth), true);
    const submit = form.getByRole('button', { name: 'Submit for Manual approval' });
    assert.equal(await submit.count(), 0);
    console.log('PENTA ADJUSTMENT BROWSER CASE exact mobile review and input validation PASS');

    await authorize.click();
    await submit.waitFor();
    assert.deepEqual(requests.map(request => request.method), ['PUT', 'PUT', 'POST']);
    assert.match(requests[2].path, /\/approve$/);
    assert.equal(JSON.parse(requests[2].body).intent, 'ApproveSubmitInvoiceAdjustment');
    console.log('PENTA ADJUSTMENT BROWSER CASE authorization precedes submission PASS');

    let executionCount = 0;
    let receipt;
    let transportError;
    await page.route(url => url.origin === api && url.pathname.includes(adjustmentPath) && url.pathname.endsWith('/execute'), async route => {
      if (route.request().method() !== 'POST') { await route.continue(); return; }
      try {
        executionCount++;
        if (executionCount === 2) {
          const result = await route.fetch({ maxRetries: 0 });
          assert.equal(result.status(), 201);
          receipt = (await result.json()).receipt;
          assert.ok(receipt?.receiptId);
        } else assert.equal(executionCount, 1);
      } catch (error) { transportError = error; }
      await route.abort('failed');
    });

    const uncertain = form.getByText(/adjustment outcome could not be verified/);
    const retry = form.getByRole('button', { name: 'Retry this authorized proposal' });
    await submit.click();
    await uncertain.waitFor();
    assert.equal(executionCount, 1);
    assert.equal(await retry.count(), 0);
    await form.getByRole('button', { name: 'Check proposal status' }).click();
    await retry.waitFor();
    assert.equal(executionCount, 1);
    assert.equal(requests.at(-1).method, 'GET');
    console.log('PENTA ADJUSTMENT BROWSER CASE interrupted submission recovers the authorized draft with GET PASS');

    await retry.click();
    await uncertain.waitFor();
    if (transportError) throw transportError;
    assert.equal(executionCount, 2);
    assert.equal(receipt.statusAtSubmission, 'PendingApproval');
    assert.equal(receipt.amountExact, '125.01');
    const executions = requests.filter(request => request.path.endsWith('/execute'));
    assert.deepEqual(executions[0], executions[1]);
    const pointer = await page.evaluate(key => JSON.parse(sessionStorage.getItem(key)), storageKey);
    assert.deepEqual(Object.keys(pointer).sort(), ['academyId', 'conversationId', 'epoch', 'requestId']);
    assert.ok(executions[1].path.endsWith('/' + pointer.conversationId + '/drafts/' + pointer.requestId + '/execute'));
    console.log('PENTA ADJUSTMENT BROWSER CASE explicit same-draft retry submits exactly once PASS');

    const beforeReload = requests.length;
    await page.reload();
    await form.getByText(receipt.receiptId, { exact: true }).waitFor();
    await form.getByText('Proposal submitted for Manual approval.', { exact: true }).waitFor();
    assert.deepEqual(requests.slice(beforeReload).map(request => request.method), ['GET']);
    assert.equal(executionCount, 2);
    await form.getByRole('button', { name: 'Done', exact: true }).click();
    await form.waitFor({ state: 'detached' });
    assert.equal(await page.evaluate(key => sessionStorage.getItem(key), storageKey), null);
    console.log('PENTA ADJUSTMENT BROWSER CASE reload recovers the original pending receipt and Done clears the pointer PASS');

    await open();
    await form.getByLabel('Adjustment type').selectOption('Scholarship');
    await amount.fill('125.01');
    await explanation.fill('QA alternative explanation');
    await review.click();
    await form.getByText('A matching adjustment is already pending. Review it in Manual Finance Adjustments.').waitFor();
    assert.equal(await authorize.count(), 0);
    const manual = form.getByRole('link', { name: 'Open Manual Finance Adjustments' });
    await manual.click();
    await page.waitForURL(web + '/finance-adjustments');
    await page.getByRole('heading', { name: 'Adjustment request register' }).waitFor();
    await page.getByText(reason, { exact: true }).waitFor();
    console.log('PENTA ADJUSTMENT BROWSER CASE duplicate refusal and navigation to the Manual register PASS');
    console.log('PENTA ADJUSTMENT BROWSER SUMMARY 6 live browser checks PASS');
  } finally {
    await browser.close();
  }
}

main().catch(error => { console.error(error.message); process.exitCode = 1; });
