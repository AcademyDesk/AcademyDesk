const assert = require('node:assert/strict');
const { chromium } = require(process.env.QA_FINANCE_CHAT_PLAYWRIGHT);

function loopback(value) {
  const url = new URL(value);
  assert.equal(url.protocol, 'http:');
  assert.equal(url.hostname, '127.0.0.1');
  assert.ok(Number(url.port) >= 1024 && url.href === url.origin + '/');
  return url.origin;
}
const web = loopback(process.env.QA_FINANCE_CHAT_WEB), api = loopback(process.env.QA_FINANCE_CHAT_API);
assert.notEqual(web, api);
const root = '/api/teacher/penta/attendance', pointer = 'academydesk.penta.teacher-attendance-recovery.v1';

async function main() {
  const browser = await chromium.launch({ executablePath: process.env.QA_BROWSER_EXECUTABLE, headless: true, chromiumSandbox: true });
  try {
    const context = await browser.newContext({ viewport: { width: 390, height: 844 }, timezoneId: 'Asia/Kolkata' });
    const page = await context.newPage(), calls = [];
    const belongs = url => url.origin === api && url.pathname.startsWith(root + '/');
    page.on('request', request => {
      const url = new URL(request.url());
      if (belongs(url) && request.method() !== 'OPTIONS')
        calls.push({ method: request.method(), path: url.pathname, body: request.postData() });
    });
    await page.goto(web + '/login?returnTo=%2Fteacher%2Fai');
    await page.getByLabel('User name', { exact: true }).fill('qa-attendance-browser@example.invalid');
    await page.getByLabel('Password', { exact: true }).fill('Synthetic!39Ab');
    const identity = page.waitForResponse(response => response.url() === api + '/api/auth/session' && response.status() === 200);
    await page.getByRole('button', { name: 'Sign in', exact: true }).click();
    await page.waitForURL(web + '/teacher/ai');
    assert.ok((await (await identity).json()).roles.includes('Teacher'));
    const chat = page.getByRole('region', { name: 'PENTA attendance conversation' });
    const button = name => chat.getByRole('button', { name, exact: true });
    await chat.getByLabel('Message PENTA about attendance').fill('Mark attendance');
    await button('Send').click();
    await chat.getByRole('alert').getByText(/Mark attendance for Piano/).waitFor();
    await chat.getByLabel('Message PENTA about attendance').fill('Mark attendance for Browser Attendance');
    await button('Send').click();
    const matches = chat.getByRole('region', { name: 'Assigned class matches' });
    await matches.getByRole('button', { name: /^Choose Browser Attendance Piano at/ }).click();
    assert.equal(await matches.getByRole('button').count(), 1);
    await chat.getByLabel(/Find an enrolled learner/).fill('Mira');
    await button('Find learner').click();
    await button('Choose Mira Attendance Test').click();
    assert.equal(await button('Choose Mira Paused Test').count(), 0);
    assert.ok(calls.every(call => call.method === 'GET'));
    console.log('PENTA TEACHER BROWSER CASE prompt clarification and assigned active roster PASS');

    await chat.getByLabel('Attendance for Mira Attendance Test').selectOption('Absent');
    await chat.getByLabel('Attendance note (optional)').fill('Browser attendance note');
    await button('Review attendance').click();
    const confirm = button('Confirm mark and queue notice');
    await confirm.waitFor();
    await chat.getByText('Your attendance for the class has been marked Absent.', { exact: true }).waitFor();
    const mutations = () => calls.filter(call => call.method !== 'GET');
    assert.deepEqual(mutations().map(call => call.method), ['PUT']);
    assert.equal(JSON.parse(mutations().at(-1).body).payload.tool_key, 'AcademyDesk.TeacherAttendance.Mark');
    const savedKey = JSON.parse(await page.evaluate(key => sessionStorage.getItem(key), pointer));
    assert.deepEqual(Object.keys(savedKey).sort(), ['academyId', 'epoch', 'requestId', 'sessionId']);
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth), true);
    console.log('PENTA TEACHER BROWSER CASE exact mark and queued notice require confirmation PASS');

    let receipt, fault;
    await page.route(url => belongs(url) && url.pathname.endsWith('/confirm'), async route => {
      if (route.request().method() !== 'POST') return route.continue();
      try {
        const response = await route.fetch({ maxRetries: 0 });
        assert.equal(response.status(), 201);
        receipt = (await response.json()).receipt;
      } catch (error) { fault = error; }
      await route.abort('failed');
    });
    await confirm.click();
    await chat.getByText(/outcome is unconfirmed/).waitFor();
    if (fault) throw fault;
    assert.equal(receipt.inAppQueued, 1);
    assert.deepEqual(mutations().map(call => call.method), ['PUT', 'POST']);
    assert.equal(JSON.parse(mutations().at(-1).body).intent, 'ConfirmTeacherAttendanceAndQueueInApp');
    const observed = calls.length;
    await page.reload();
    await chat.getByRole('heading', { name: 'Attendance saved' }).waitFor();
    await chat.getByText(receipt.id, { exact: true }).waitFor();
    await chat.getByText(/One in-app notice queued/).waitFor();
    assert.deepEqual(mutations().map(call => call.method), ['PUT', 'POST']);
    assert.ok(calls.slice(observed).some(call => call.method === 'GET' && call.path.endsWith('/drafts/' + savedKey.requestId)));
    console.log('PENTA TEACHER BROWSER CASE lost confirmation recovered by GET after reload PASS');
    console.log('PENTA TEACHER BROWSER SUMMARY 3 browser groups PASS');
    await context.close();
  } finally { await browser.close(); }
}

main().catch(error => { console.error(error.message); process.exitCode = 1; });
