const assert = require('node:assert/strict');
const { chromium } = require(process.env.QA_FINANCE_CHAT_PLAYWRIGHT);

const web = loopback(process.env.QA_FINANCE_CHAT_WEB);
const api = loopback(process.env.QA_FINANCE_CHAT_API);
assert.notEqual(web, api);
const financeRoute = /\/api\/academies\/[0-9a-f-]+\/penta\/chat\/finance$/i;

function loopback(value) {
  const url = new URL(value);
  assert.equal(url.protocol, 'http:');
  assert.equal(url.hostname, '127.0.0.1');
  assert.ok(url.port && url.href === url.origin + '/');
  return url.origin;
}

async function login(page, email, target) {
  await page.goto(web + '/login?returnTo=' + encodeURIComponent(target));
  await page.getByLabel('User name', { exact: true }).fill(email);
  await page.getByLabel('Password', { exact: true }).fill('Synthetic!39Ab');
  await page.getByRole('button', { name: 'Sign in', exact: true }).click();
  await page.waitForURL(url => url.origin === web && url.pathname === target);
}

async function main() {
  const browser = await chromium.launch({
    executablePath: process.env.QA_BROWSER_EXECUTABLE, headless: true, chromiumSandbox: true,
  });
  const admin = await browser.newContext({ viewport: { width: 390, height: 844 }, timezoneId: 'Asia/Kolkata' });
  const teacher = await browser.newContext();
  try {
    const page = await admin.newPage();
    const requests = [];
    page.on('request', request => requests.push(new URL(request.url())));
    await login(page, 'qa-admin-a@example.invalid', '/penta');
    const chat = page.getByRole('region', { name: 'Finance chat' });
    await chat.waitFor();
    const input = chat.getByLabel('Ask about finance');
    async function post(action, status) {
      const wait = page.waitForResponse(response => response.url().startsWith(api + '/') &&
        financeRoute.test(new URL(response.url()).pathname) && response.request().method() === 'POST');
      await action();
      const response = await wait;
      assert.equal(response.status(), status);
      return response;
    }
    async function ask(text, status) {
      await input.fill(text);
      return post(() => chat.getByRole('button', { name: 'Send', exact: true }).click(), status);
    }

    const expense = await ask('Show recent expenses', 200);
    assert.match(expense.headers()['cache-control'], /no-store/);
    const expenses = chat.getByRole('table', { name: 'Recent expenses' });
    await expenses.getByRole('cell', { name: '0.01', exact: true }).waitFor();
    assert.equal(await expenses.getByRole('row').count(), 21);
    await chat.getByText('More records are available in Manual.', { exact: false }).waitFor();
    assert.equal(await chat.getByRole('link', { name: 'Open Manual Finance' }).getAttribute('href'), '/expenses');
    console.log('PENTA FINANCE BROWSER CASE typed expense read and Manual link PASS');

    await post(() => chat.getByRole('button', { name: 'Show recent payroll' }).click(), 200);
    const payroll = chat.getByRole('table', { name: 'Recent payroll' });
    const largePayout = payroll.getByRole('row', { name: /QA-PENTA-PS-LARGE/ });
    await largePayout.waitFor();
    assert.equal(await largePayout.getByRole('cell').nth(5).textContent(), '9999999999999999.99');
    await payroll.getByRole('cell', { name: 'Legacy unverified', exact: true }).waitFor();
    assert.equal(await page.getByText(/QA-private-preview/).count(), 0);
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth), true);
    console.log('PENTA FINANCE BROWSER CASE exact payroll and mobile overflow PASS');

    await post(() => chat.getByRole('button', { name: 'Show overdue invoices' }).click(), 200);
    await chat.getByRole('table', { name: 'Overdue invoices' }).waitFor();
    const unpaidInvoice = chat.getByRole('row', { name: /QA-PENTA-COLL-INR/ });
    await post(() => unpaidInvoice.getByRole('button', { name: 'Invoice', exact: true }).click(), 200);
    await chat.getByRole('table', { name: /^Invoice / }).waitFor();
    await post(() => chat.getByRole('button', { name: 'Reconciliation', exact: true }).click(), 200);
    await chat.getByRole('heading', { name: 'Reconciliation for QA-PENTA-COLL-INR' }).waitFor();
    await chat.getByText('No records to show.', { exact: true }).waitFor();
    console.log('PENTA FINANCE BROWSER CASE collection to invoice and reconciliation PASS');

    await ask('Send the payroll details', 422);
    await chat.getByText('Try one of the finance prompts below.').waitFor();
    assert.equal(await chat.getByRole('table').count(), 0);
    await ask('show invoice e278d30b-9e32-4c5f-9359-44d8cdf6fb24', 404);
    await chat.getByText('That invoice is unavailable. Check it in Manual Finance.').waitFor();
    await chat.getByRole('link', { name: 'Open Manual Finance' }).click();
    await page.waitForURL(url => url.origin === web && url.pathname === '/finance');
    console.log('PENTA FINANCE BROWSER CASE refusal, missing invoice and live Manual navigation PASS');

    await page.goto(web + '/penta');
    await chat.waitFor();
    await ask('show recent payroll', 200);
    await payroll.waitFor();
    const otherTab = await admin.newPage();
    await login(otherTab, 'qa-admin-a@example.invalid', '/penta');
    await chat.waitFor({ state: 'detached' });
    assert.equal(await page.getByText('9999999999999999.99', { exact: true }).count(), 0);
    await otherTab.close();
    console.log('PENTA FINANCE BROWSER CASE cross-tab login clears previous results PASS');

    const instructor = await teacher.newPage();
    await login(instructor, 'qa-teacher-a@example.invalid', '/teacher/ai');
    await instructor.goto(web + '/penta');
    await instructor.getByRole('heading', { name: 'Workspace verification is unavailable' }).waitFor();
    assert.equal(await instructor.getByLabel('Ask about finance').count(), 0);
    assert.ok(requests.every(url => [web, api].includes(url.origin) || url.protocol === 'data:'));
    console.log('PENTA FINANCE BROWSER CASE teacher has no finance composer PASS');
    console.log('PENTA FINANCE BROWSER SUMMARY 6 real browser checks PASS');
  } finally {
    await browser.close();
  }
}

main().catch(error => { console.error(error.message); process.exitCode = 1; });
