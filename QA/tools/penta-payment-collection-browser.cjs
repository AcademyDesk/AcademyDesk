const assert = require('node:assert/strict');
const { chromium } = require(process.env.QA_FINANCE_CHAT_PLAYWRIGHT);

const web = origin(process.env.QA_FINANCE_CHAT_WEB);
const api = origin(process.env.QA_FINANCE_CHAT_API);
assert.notEqual(web, api);
const paymentPath = '/penta/payment-collection/conversations/';
const storageKey = 'academydesk.penta.payment-recovery';

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
    const context = await browser.newContext({ viewport: { width: 390, height: 844 }, timezoneId: 'Asia/Kolkata' });
    const page = await context.newPage();
    const requests = [];
    page.on('request', request => {
      const url = new URL(request.url());
      if (url.origin === api && url.pathname.includes(paymentPath) && request.method() !== 'OPTIONS')
        requests.push({ path: url.pathname, method: request.method(), body: request.postData() });
    });
    await page.goto(web + '/login?returnTo=%2Fpenta');
    await page.getByLabel('User name', { exact: true }).fill('qa-admin-a@example.invalid');
    await page.getByLabel('Password', { exact: true }).fill('Synthetic!39Ab');
    await page.getByRole('button', { name: 'Sign in', exact: true }).click();
    await page.waitForURL(web + '/penta');

    const chat = page.getByRole('region', { name: 'Finance chat' });
    await chat.getByRole('button', { name: 'Show overdue invoices' }).click();
    const row = chat.getByRole('row', { name: /QA-BROWSER-PAYMENT/ });
    await row.getByRole('button', { name: 'Invoice', exact: true }).click();
    await chat.getByRole('button', { name: 'Record payment', exact: true }).click();

    const payment = chat.getByRole('region', { name: 'Record a received payment' });
    const input = payment.getByLabel('Payment amount');
    await page.waitForFunction(() => document.activeElement?.id === 'penta-payment-amount');
    await input.fill('200.001');
    await payment.getByRole('button', { name: 'Review payment' }).click();
    await payment.getByText(/up to two decimal places/).waitFor();
    assert.deepEqual(requests, []);
    await input.fill('200');
    await payment.getByRole('button', { name: 'Review payment' }).click();
    await payment.getByRole('button', { name: 'Approve exact payment' }).waitFor();
    assert.deepEqual(await payment.locator('dd').allTextContents(), [
      'QA-BROWSER-PAYMENT', 'USD 200.00', 'Cash', 'USD 200.00', 'USD 0.00',
    ]);
    assert.equal(await payment.getByRole('button', { name: 'Record received payment', exact: true }).count(), 0);
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth), true);
    console.log('PENTA PAYMENT BROWSER CASE valid exact review and mobile controls PASS');

    await payment.getByRole('button', { name: 'Approve exact payment' }).click();
    const record = payment.getByRole('button', { name: 'Record received payment', exact: true });
    await record.waitFor();
    assert.deepEqual(requests.map(request => request.method), ['PUT', 'PUT', 'POST']);
    const approval = requests.at(-1);
    assert.match(approval.path, /\/approve$/);
    assert.equal(JSON.parse(approval.body).intent, 'ApproveRecordReceivedPayment');
    console.log('PENTA PAYMENT BROWSER CASE explicit approval before recording PASS');

    let executeCalls = 0;
    let receipt;
    let transportError;
    const execution = url => url.origin === api && url.pathname.includes(paymentPath) && url.pathname.endsWith('/execute');
    await page.route(execution, async route => {
      if (route.request().method() !== 'POST') { await route.continue(); return; }
      try {
        executeCalls++;
        if (executeCalls === 2) {
          const committed = await route.fetch({ maxRetries: 0 });
          assert.equal(committed.status(), 201);
          receipt = (await committed.json()).receipt;
          assert.ok(receipt?.receiptId);
        } else assert.equal(executeCalls, 1);
      } catch (error) { transportError = error; }
      await route.abort('failed');
    });

    await record.click();
    await payment.getByText(/payment outcome could not be verified/).waitFor();
    assert.equal(executeCalls, 1);
    assert.equal(await payment.getByRole('button', { name: 'Retry this approved payment' }).count(), 0);
    await payment.getByRole('button', { name: 'Check payment status' }).click();
    const retry = payment.getByRole('button', { name: 'Retry this approved payment' });
    await retry.waitFor();
    assert.equal(executeCalls, 1);
    assert.equal(requests.at(-1).method, 'GET');
    console.log('PENTA PAYMENT BROWSER CASE lost request recovers approval with GET PASS');

    await retry.click();
    await payment.getByText(/payment outcome could not be verified/).waitFor();
    if (transportError) throw transportError;
    assert.equal(executeCalls, 2);
    assert.equal(receipt.amountExact, '200.00');
    assert.equal(receipt.remainingBalanceExact, '0.00');
    const executions = requests.filter(request => request.path.endsWith('/execute'));
    assert.deepEqual(executions[0], executions[1]);
    const pointer = await page.evaluate(key => JSON.parse(sessionStorage.getItem(key)), storageKey);
    assert.deepEqual(Object.keys(pointer).sort(), ['academyId', 'conversationId', 'epoch', 'requestId']);
    assert.ok(executions[1].path.endsWith('/' + pointer.conversationId + '/drafts/' + pointer.requestId + '/execute'));
    console.log('PENTA PAYMENT BROWSER CASE same request commits once with dropped response PASS');

    const beforeReload = requests.length;
    await page.reload();
    await payment.getByText(receipt.receiptId, { exact: true }).waitFor();
    await payment.getByText('Payment recorded.', { exact: false }).waitFor();
    assert.equal(executeCalls, 2);
    assert.deepEqual(requests.slice(beforeReload).map(request => request.method), ['GET']);
    assert.equal(await payment.getByRole('link', { name: 'Open Manual Payments' }).getAttribute('href'), '/payments');
    console.log('PENTA PAYMENT BROWSER CASE reload retrieves the committed receipt through GET PASS');

    await payment.getByRole('button', { name: 'Done' }).click();
    await payment.waitFor({ state: 'detached' });
    assert.equal(await page.evaluate(key => sessionStorage.getItem(key), storageKey), null);
    console.log('PENTA PAYMENT BROWSER CASE completed recovery clears the tab pointer PASS');
    console.log('PENTA PAYMENT BROWSER SUMMARY 6 live browser checks PASS');
  } finally {
    await browser.close();
  }
}

main().catch(error => { console.error(error.message); process.exitCode = 1; });
