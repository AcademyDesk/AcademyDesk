const assert = require('node:assert/strict');
const { chromium } = require(process.env.QA_FINANCE_CHAT_PLAYWRIGHT);

const web = origin(process.env.QA_FINANCE_CHAT_WEB);
const api = origin(process.env.QA_FINANCE_CHAT_API);
assert.notEqual(web, api);
const conversationPath = '/penta/invoice-issuance/conversations/';
const storageKey = 'academydesk.penta.invoice-recovery';
const due = new Date(Date.now() + 70 * 86_400_000).toISOString().slice(0, 10);

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
      if (url.origin === api && url.pathname.includes(conversationPath) && request.method() !== 'OPTIONS')
        requests.push({ path: url.pathname, method: request.method(), body: request.postData() });
    });
    await page.goto(web + '/login?returnTo=%2Fpenta');
    await page.getByLabel('User name', { exact: true }).fill('qa-admin-a@example.invalid');
    await page.getByLabel('Password', { exact: true }).fill('Synthetic!39Ab');
    await page.getByRole('button', { name: 'Sign in', exact: true }).click();
    await page.waitForURL(web + '/penta');

    const chat = page.getByRole('region', { name: 'Finance chat' });
    const invoice = chat.getByRole('region', { name: 'Issue an invoice' });
    async function open() {
      await chat.getByRole('button', { name: 'Prepare an invoice', exact: true }).click();
      await invoice.getByLabel('Student name or number').fill('Invoice Browser');
      const [response] = await Promise.all([
        page.waitForResponse(response => response.url().startsWith(api + '/') &&
          response.url().endsWith('/penta/invoice-issuance/options') && response.request().method() === 'POST'),
        invoice.getByRole('button', { name: 'Find students', exact: true }).click(),
      ]);
      assert.equal(response.status(), 200);
      assert.match(response.headers()['cache-control'], /no-store/);
      const result = await response.json();
      assert.equal(result.options.length, 1);
      assert.deepEqual(Object.keys(result.options[0]).sort(), ['detail', 'id', 'label']);
      await invoice.getByRole('button', { name: /Invoice Browser Learner/ }).click();
      await invoice.getByLabel('Due date', { exact: true }).fill(due);
    }
    async function feeReview() {
      await open();
      await invoice.getByLabel('Billing type').selectOption('feePlan');
      await invoice.getByLabel('Fee plan name').fill('QA Browser Fee');
      await invoice.getByRole('button', { name: 'Find fee plans', exact: true }).click();
      await invoice.getByRole('button', { name: /QA Browser Fee Plan/ }).click();
      await invoice.getByRole('button', { name: 'Review invoice', exact: true }).click();
    }

    await open();
    await invoice.getByLabel('Invoice amount').fill('9999999999999999.99');
    await invoice.getByRole('button', { name: 'Review invoice', exact: true }).click();
    await invoice.getByText('INR 9999999999999999.99', { exact: true }).waitFor();
    await invoice.getByRole('button', { name: 'Cancel', exact: true }).click();
    assert.equal(requests.filter(row => row.method === 'POST').length, 0);
    console.log('PENTA INVOICE BROWSER CASE private student lookup and exact one-time review PASS');

    await feeReview();
    await invoice.getByRole('button', { name: 'Approve exact invoice', exact: true }).waitFor();
    assert.deepEqual((await invoice.locator('dd').allTextContents()).slice(0, 3), [
      'Invoice Browser Learner · QA-INV-BROWSER', 'QA Browser Fee Plan', 'USD 132.01',
    ]);
    assert.equal(await invoice.getByRole('button', { name: 'Issue approved invoice' }).count(), 0);
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth), true);
    await invoice.getByRole('button', { name: 'Approve exact invoice', exact: true }).click();
    const issue = invoice.getByRole('button', { name: 'Issue approved invoice', exact: true });
    await issue.waitFor();
    assert.match(requests.at(-1).path, /\/approve$/);
    assert.equal(JSON.parse(requests.at(-1).body).intent, 'ApproveIssueInvoice');
    console.log('PENTA INVOICE BROWSER CASE fee-plan terms and explicit approval PASS');

    let executeCalls = 0;
    let receipt;
    let transportError;
    const execution = url => url.origin === api && url.pathname.includes(conversationPath) && url.pathname.endsWith('/execute');
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

    await issue.click();
    await invoice.getByText(/invoice outcome could not be verified/).waitFor();
    assert.equal(await invoice.getByRole('button', { name: 'Retry this approved invoice' }).count(), 0);
    await invoice.getByRole('button', { name: 'Check invoice status' }).click();
    const retry = invoice.getByRole('button', { name: 'Retry this approved invoice' });
    await retry.waitFor();
    assert.equal(executeCalls, 1);
    assert.equal(requests.at(-1).method, 'GET');
    console.log('PENTA INVOICE BROWSER CASE interrupted request recovers approval with GET PASS');

    await retry.click();
    await invoice.getByText(/invoice outcome could not be verified/).waitFor();
    if (transportError) throw transportError;
    assert.equal(executeCalls, 2);
    assert.equal(receipt.amountExact, '132.01');
    assert.equal(receipt.dueDate, due);
    const executions = requests.filter(request => request.path.endsWith('/execute'));
    assert.deepEqual(executions[0], executions[1]);
    const pointer = await page.evaluate(key => JSON.parse(sessionStorage.getItem(key)), storageKey);
    assert.deepEqual(Object.keys(pointer).sort(), ['academyId', 'conversationId', 'epoch', 'requestId']);
    assert.ok(executions[1].path.endsWith('/' + pointer.conversationId + '/drafts/' + pointer.requestId + '/execute'));
    console.log('PENTA INVOICE BROWSER CASE explicit same-draft retry commits with dropped response PASS');

    const beforeReload = requests.length;
    await page.reload();
    await invoice.getByText(receipt.receiptId, { exact: true }).waitFor();
    await invoice.getByText(receipt.invoiceNumber, { exact: true }).waitFor();
    assert.equal(executeCalls, 2);
    assert.deepEqual(requests.slice(beforeReload).map(request => request.method), ['GET']);
    await invoice.getByRole('button', { name: 'Done', exact: true }).click();
    await invoice.waitFor({ state: 'detached' });
    assert.equal(await page.evaluate(key => sessionStorage.getItem(key), storageKey), null);
    console.log('PENTA INVOICE BROWSER CASE reload recovers the original invoice and receipt PASS');

    await feeReview();
    await invoice.getByText('A matching invoice already exists. Review it in Manual Invoices.', { exact: true }).waitFor();
    assert.equal(await invoice.getByRole('button', { name: 'Approve exact invoice' }).count(), 0);
    await invoice.getByRole('link', { name: 'Open Manual Invoices' }).click();
    await page.waitForURL(url => url.origin === web && url.pathname === '/invoices');
    assert.equal(executeCalls, 2);
    console.log('PENTA INVOICE BROWSER CASE duplicate refusal and Manual navigation PASS');
    console.log('PENTA INVOICE BROWSER SUMMARY 6 live browser checks PASS');
  } finally {
    await browser.close();
  }
}

main().catch(error => { console.error(error.message); process.exitCode = 1; });
